namespace erp_univ_back.Features.Evaluation.SaisirNotes;

using erp_univ_back.Features.Inscription;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public interface ISaisirNotesService
{
    Task<SaisirNotesResponse> ExecuteAsync(Guid evaluationId, SaisirNotesRequest request, CancellationToken ct = default);
}

public class SaisirNotesService(
    ErpUnivDbContext db,
    IValidator<SaisirNotesRequest> validator) : ISaisirNotesService
{
    public async Task<SaisirNotesResponse> ExecuteAsync(Guid evaluationId, SaisirNotesRequest request, CancellationToken ct = default)
    {
        // 1. Validation syntaxique du lot
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.Errors.First().ErrorMessage);
        }

        // 2. Vérification de l'évaluation
        var evaluation = await db.Evaluations
            .FirstOrDefaultAsync(e => e.Id == evaluationId, ct)
            ?? throw new NotFoundException($"L'évaluation avec l'identifiant '{evaluationId}' n'existe pas.");

        // 3. Contrôle du verrouillage (anti-corruption / clôture)
        if (evaluation.EstVerrouillee)
        {
            throw new BadRequestException("Cette évaluation est clôturée et verrouillée. Les notes ne peuvent plus être modifiées.");
        }

        // 4. Détection des doublons au sein du lot soumis
        var etudiantIds = request.Notes.Select(n => n.EtudiantId).ToList();
        if (etudiantIds.Count != etudiantIds.Distinct().Count())
        {
            throw new BadRequestException("La requête contient des doublons pour un même étudiant.");
        }

        // 5. Contrôle du barème
        foreach (var ligne in request.Notes)
        {
            if (!ligne.EstAbsent && ligne.Valeur.HasValue && ligne.Valeur.Value > evaluation.Bareme)
            {
                throw new BadRequestException($"La note '{ligne.Valeur.Value}' dépasse le barème maximal autorisé ({evaluation.Bareme}) pour cette évaluation.");
            }
        }

        // 6. Vérification des inscriptions actives dans la classe et l'année de l'évaluation
        var inscriptions = await db.Inscriptions
            .Include(i => i.Etudiant)
            .Where(i => etudiantIds.Contains(i.EtudiantId) &&
                        i.FiliereNiveauId == evaluation.FiliereNiveauId &&
                        i.AnneeAcademiqueId == evaluation.AnneeAcademiqueId &&
                        i.Statut == StatutInscription.Validee)
            .ToListAsync(ct);

        var inscriptionsByEtudiantId = inscriptions.ToDictionary(i => i.EtudiantId);

        foreach (var etudiantId in etudiantIds)
        {
            if (!inscriptionsByEtudiantId.ContainsKey(etudiantId))
            {
                throw new BadRequestException($"L'étudiant avec l'identifiant '{etudiantId}' n'a pas d'inscription validée pour cette classe et cette année académique.");
            }
        }

        // 7. Récupération des notes déjà existantes pour cet examen
        var existingNotes = await db.Notes
            .Where(n => n.EvaluationId == evaluationId && etudiantIds.Contains(n.EtudiantId))
            .ToListAsync(ct);

        var existingNotesByEtudiantId = existingNotes.ToDictionary(n => n.EtudiantId);

        // 8. Upsert (Mise à jour ou Insertion)
        int totalInseres = 0;
        int totalMisAJour = 0;
        var resultNotes = new List<NoteSaisieResultDto>();

        foreach (var item in request.Notes)
        {
            var inscription = inscriptionsByEtudiantId[item.EtudiantId];
            var etudiant = inscription.Etudiant;
            var valeur = item.EstAbsent ? null : item.Valeur;
            var commentaire = string.IsNullOrWhiteSpace(item.Commentaire) ? null : item.Commentaire.Trim();

            if (existingNotesByEtudiantId.TryGetValue(item.EtudiantId, out var existingNote))
            {
                existingNote.Valeur = valeur;
                existingNote.EstAbsent = item.EstAbsent;
                existingNote.Commentaire = commentaire;
                totalMisAJour++;

                resultNotes.Add(new NoteSaisieResultDto(
                    existingNote.Id,
                    etudiant.Id,
                    etudiant.Matricule,
                    $"{etudiant.Nom} {etudiant.Prenom}".Trim(),
                    inscription.Id,
                    existingNote.Valeur,
                    existingNote.EstAbsent,
                    existingNote.Commentaire,
                    EstNouvelle: false
                ));
            }
            else
            {
                var newNote = new NoteEntity
                {
                    EvaluationId = evaluation.Id,
                    InscriptionId = inscription.Id,
                    EtudiantId = etudiant.Id,
                    Valeur = valeur,
                    EstAbsent = item.EstAbsent,
                    Commentaire = commentaire
                };

                db.Notes.Add(newNote);
                totalInseres++;

                resultNotes.Add(new NoteSaisieResultDto(
                    newNote.Id,
                    etudiant.Id,
                    etudiant.Matricule,
                    $"{etudiant.Nom} {etudiant.Prenom}".Trim(),
                    inscription.Id,
                    newNote.Valeur,
                    newNote.EstAbsent,
                    newNote.Commentaire,
                    EstNouvelle: true
                ));
            }
        }

        await db.SaveChangesAsync(ct);

        return new SaisirNotesResponse(
            evaluation.Id,
            evaluation.Titre,
            evaluation.Bareme,
            request.Notes.Count,
            totalInseres,
            totalMisAJour,
            resultNotes
        );
    }
}

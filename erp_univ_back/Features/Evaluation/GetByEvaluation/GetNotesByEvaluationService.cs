namespace erp_univ_back.Features.Evaluation.GetByEvaluation;

using erp_univ_back.Features.Inscription;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

public interface IGetNotesByEvaluationService
{
    Task<GetNotesByEvaluationResponse> ExecuteAsync(Guid evaluationId, CancellationToken ct = default);
}

public class GetNotesByEvaluationService(ErpUnivDbContext db) : IGetNotesByEvaluationService
{
    public async Task<GetNotesByEvaluationResponse> ExecuteAsync(Guid evaluationId, CancellationToken ct = default)
    {
        // 1. Récupération de l'évaluation et de son contexte
        var evaluation = await db.Evaluations
            .Include(e => e.Matiere)
            .Include(e => e.FiliereNiveau).ThenInclude(fn => fn.Filiere)
            .Include(e => e.FiliereNiveau).ThenInclude(fn => fn.Niveau)
            .Include(e => e.AnneeAcademique)
            .Include(e => e.Professeur)
            .FirstOrDefaultAsync(e => e.Id == evaluationId, ct)
            ?? throw new NotFoundException($"L'évaluation avec l'identifiant '{evaluationId}' n'existe pas.");

        // 2. Récupération de tous les étudiants inscrits et actifs dans la classe
        var inscriptions = await db.Inscriptions
            .Include(i => i.Etudiant)
            .Where(i => i.FiliereNiveauId == evaluation.FiliereNiveauId &&
                        i.AnneeAcademiqueId == evaluation.AnneeAcademiqueId &&
                        i.Statut == StatutInscription.Validee)
            .OrderBy(i => i.Etudiant.Nom)
            .ThenBy(i => i.Etudiant.Prenom)
            .ToListAsync(ct);

        // 3. Récupération des notes déjà enregistrées pour cette évaluation
        var notes = await db.Notes
            .Where(n => n.EvaluationId == evaluationId)
            .ToListAsync(ct);

        var notesByEtudiantId = notes.ToDictionary(n => n.EtudiantId);

        // 4. Construction de la grille complète des étudiants
        var lignes = new List<NoteEtudiantDto>();
        foreach (var inscription in inscriptions)
        {
            var etudiant = inscription.Etudiant;
            if (notesByEtudiantId.TryGetValue(etudiant.Id, out var note))
            {
                lignes.Add(new NoteEtudiantDto(
                    note.Id,
                    etudiant.Id,
                    etudiant.Matricule,
                    etudiant.Nom,
                    etudiant.Prenom,
                    inscription.Id,
                    note.Valeur,
                    note.EstAbsent,
                    note.Commentaire,
                    EstSaisie: true,
                    note.DateModification
                ));
            }
            else
            {
                lignes.Add(new NoteEtudiantDto(
                    null,
                    etudiant.Id,
                    etudiant.Matricule,
                    etudiant.Nom,
                    etudiant.Prenom,
                    inscription.Id,
                    null,
                    false,
                    null,
                    EstSaisie: false,
                    null
                ));
            }
        }

        // 5. Calcul des statistiques de l'évaluation
        var totalInscrits = inscriptions.Count;
        var totalSaisies = notes.Count;
        var totalAbsents = notes.Count(n => n.EstAbsent);
        var notesPresentes = notes
            .Where(n => !n.EstAbsent && n.Valeur.HasValue)
            .Select(n => n.Valeur!.Value)
            .ToList();

        var totalPresents = notesPresentes.Count;
        decimal? moyenneClasse = totalPresents > 0 ? Math.Round(notesPresentes.Average(), 2) : null;
        decimal? noteMin = totalPresents > 0 ? notesPresentes.Min() : null;
        decimal? noteMax = totalPresents > 0 ? notesPresentes.Max() : null;

        var libelleClasse = evaluation.FiliereNiveau.LibelleAffiche
            ?? $"{evaluation.FiliereNiveau.Filiere?.Nom} - {evaluation.FiliereNiveau.Niveau?.Nom}".Trim(' ', '-');

        var professeurNom = evaluation.Professeur != null
            ? $"{evaluation.Professeur.Nom} {evaluation.Professeur.Prenom}".Trim()
            : null;

        var resumeDto = new EvaluationResumeDto(
            evaluation.Id,
            evaluation.Titre,
            evaluation.TypeEvaluation,
            evaluation.Session,
            evaluation.DateEvaluation,
            evaluation.Bareme,
            evaluation.Poids,
            evaluation.EstVerrouillee,
            evaluation.DateVerrouillage,
            evaluation.Matiere.Id,
            evaluation.Matiere.Nom,
            evaluation.Matiere.Code,
            evaluation.FiliereNiveau.Id,
            libelleClasse,
            evaluation.AnneeAcademique.Id,
            evaluation.AnneeAcademique.Libelle,
            evaluation.Professeur?.Id,
            professeurNom
        );

        var statsDto = new StatistiquesEvaluationDto(
            totalInscrits,
            totalSaisies,
            totalPresents,
            totalAbsents,
            moyenneClasse,
            noteMin,
            noteMax
        );

        return new GetNotesByEvaluationResponse(resumeDto, statsDto, lignes);
    }
}

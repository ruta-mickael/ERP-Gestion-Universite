namespace erp_univ_back.Features.Evaluation.GetReleveNotes;

using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

public interface IGetReleveNotesService
{
    Task<GetReleveNotesResponse> ExecuteAsync(Guid inscriptionId, CancellationToken ct = default);
}

public class GetReleveNotesService(ErpUnivDbContext db) : IGetReleveNotesService
{
    public async Task<GetReleveNotesResponse> ExecuteAsync(Guid inscriptionId, CancellationToken ct = default)
    {
        // 1. Récupération de l'inscription et de son contexte complet
        var inscription = await db.Inscriptions
            .Include(i => i.Etudiant)
            .Include(i => i.FiliereNiveau).ThenInclude(fn => fn.Filiere)
            .Include(i => i.FiliereNiveau).ThenInclude(fn => fn.Niveau)
            .Include(i => i.AnneeAcademique)
            .Include(i => i.Etablissement)
            .Include(i => i.Echeances)
            .FirstOrDefaultAsync(i => i.Id == inscriptionId, ct)
            ?? throw new NotFoundException($"L'inscription avec l'identifiant '{inscriptionId}' n'existe pas.");

        // 2. Contrôle de la situation financière
        var montantAttendu = inscription.Echeances.Sum(e => e.MontantAttendu);
        var montantPaye = inscription.Echeances.Sum(e => e.MontantDejaPaye);
        var soldeDu = montantAttendu - montantPaye;
        var estEnRegle = soldeDu <= 0;
        var mentionFinanciere = estEnRegle
            ? "Frais de scolarité en règle"
            : $"Solde impayé de {soldeDu:N2} Ar - Document provisoire";

        var situationFinanciereDto = new SituationFinanciereReleveDto(
            estEnRegle,
            montantAttendu,
            montantPaye,
            soldeDu,
            mentionFinanciere
        );

        // 3. Récupération de la maquette académique (UEs et Matières de la classe)
        var ues = await db.UniteEnseignements
            .Include(u => u.Matieres)
            .Where(u => u.FiliereNiveauId == inscription.FiliereNiveauId)
            .OrderBy(u => u.Semestre)
            .ThenBy(u => u.Code)
            .ToListAsync(ct);

        // 4. Récupération des évaluations programmées pour cette classe et cette année
        var evaluations = await db.Evaluations
            .Where(e => e.FiliereNiveauId == inscription.FiliereNiveauId &&
                        e.AnneeAcademiqueId == inscription.AnneeAcademiqueId)
            .ToListAsync(ct);

        // 5. Récupération des notes de l'étudiant pour cette inscription
        var notes = await db.Notes
            .Where(n => n.InscriptionId == inscription.Id)
            .ToListAsync(ct);

        var notesByEvaluationId = notes.ToDictionary(n => n.EvaluationId);

        // 6. Construction et calcul hiérarchique par Semestre -> UE -> Matière
        var semestresDto = new List<SemestreReleveDto>();
        var tousLesUesDto = new List<UniteEnseignementReleveDto>();

        var semestresGroupes = ues.GroupBy(u => u.Semestre).OrderBy(g => g.Key);

        foreach (var groupeSemestre in semestresGroupes)
        {
            var uesSemestreDto = new List<UniteEnseignementReleveDto>();

            foreach (var ue in groupeSemestre)
            {
                var matieresDto = new List<MatiereReleveDto>();

                foreach (var matiere in ue.Matieres)
                {
                    var evalsMatiere = evaluations
                        .Where(e => e.MatiereId == matiere.Id)
                        .OrderBy(e => e.DateEvaluation)
                        .ToList();

                    var evalNotesDto = new List<EvaluationNoteItemDto>();
                    decimal totalPoidsNotes = 0m;
                    decimal sommePondereeNotes = 0m;

                    foreach (var ev in evalsMatiere)
                    {
                        decimal? noteVal = null;
                        bool estAbsent = false;
                        string? commentaire = null;

                        if (notesByEvaluationId.TryGetValue(ev.Id, out var note))
                        {
                            noteVal = note.Valeur;
                            estAbsent = note.EstAbsent;
                            commentaire = note.Commentaire;

                            if (!estAbsent && noteVal.HasValue && ev.Poids > 0)
                            {
                                sommePondereeNotes += noteVal.Value * ev.Poids;
                                totalPoidsNotes += ev.Poids;
                            }
                        }

                        evalNotesDto.Add(new EvaluationNoteItemDto(
                            ev.Id,
                            ev.Titre,
                            ev.TypeEvaluation,
                            ev.Session,
                            ev.Bareme,
                            ev.Poids,
                            noteVal,
                            estAbsent,
                            commentaire
                        ));
                    }

                    decimal? moyenneMatiere = totalPoidsNotes > 0
                        ? Math.Round(sommePondereeNotes / totalPoidsNotes, 2)
                        : null;

                    matieresDto.Add(new MatiereReleveDto(
                        matiere.Id,
                        matiere.Nom,
                        matiere.Code,
                        matiere.Coefficient,
                        moyenneMatiere,
                        evalNotesDto
                    ));
                }

                // Calcul de la moyenne de l'UE (pondérée par les coefficients des matières)
                var matieresAvecMoyenne = matieresDto.Where(m => m.Moyenne.HasValue).ToList();
                decimal totalCoeffs = matieresAvecMoyenne.Sum(m => m.Coefficient);
                decimal sommePondereeUe = matieresAvecMoyenne.Sum(m => m.Moyenne!.Value * m.Coefficient);

                decimal? moyenneUe = totalCoeffs > 0
                    ? Math.Round(sommePondereeUe / totalCoeffs, 2)
                    : null;

                bool estValidee = moyenneUe.HasValue && moyenneUe.Value >= 10.00m;
                int creditsAcquis = estValidee ? ue.Credit : 0;

                var ueDto = new UniteEnseignementReleveDto(
                    ue.Id,
                    ue.Nom,
                    ue.Code,
                    ue.Credit,
                    ue.Semestre,
                    moyenneUe,
                    estValidee,
                    creditsAcquis,
                    matieresDto
                );

                uesSemestreDto.Add(ueDto);
                tousLesUesDto.Add(ueDto);
            }

            // Calcul de la moyenne du Semestre (pondérée par les crédits des UEs)
            var uesAvecMoyenne = uesSemestreDto.Where(u => u.Moyenne.HasValue).ToList();
            int totalCreditsSemestre = uesSemestreDto.Sum(u => u.Credit);
            int creditsAcquisSemestre = uesSemestreDto.Sum(u => u.CreditsAcquis);

            int totalCreditsPonderes = uesAvecMoyenne.Sum(u => u.Credit);
            decimal sommePondereeSemestre = uesAvecMoyenne.Sum(u => u.Moyenne!.Value * u.Credit);

            decimal? moyenneSemestre = totalCreditsPonderes > 0
                ? Math.Round(sommePondereeSemestre / totalCreditsPonderes, 2)
                : null;

            string? mentionSemestre = moyenneSemestre.HasValue ? ObtenirMention(moyenneSemestre.Value) : null;

            semestresDto.Add(new SemestreReleveDto(
                groupeSemestre.Key,
                totalCreditsSemestre,
                creditsAcquisSemestre,
                moyenneSemestre,
                mentionSemestre,
                uesSemestreDto
            ));
        }

        // 7. Bilan Général Annuel
        int totalCreditsInscrits = tousLesUesDto.Sum(u => u.Credit);
        int totalCreditsAcquis = tousLesUesDto.Sum(u => u.CreditsAcquis);

        var tousLesUesAvecMoyenne = tousLesUesDto.Where(u => u.Moyenne.HasValue).ToList();
        int totalCreditsPonderesAnnee = tousLesUesAvecMoyenne.Sum(u => u.Credit);
        decimal sommePondereeAnnee = tousLesUesAvecMoyenne.Sum(u => u.Moyenne!.Value * u.Credit);

        decimal? moyenneGenerale = totalCreditsPonderesAnnee > 0
            ? Math.Round(sommePondereeAnnee / totalCreditsPonderesAnnee, 2)
            : null;

        string mentionGenerale = moyenneGenerale.HasValue
            ? ObtenirMention(moyenneGenerale.Value)
            : "En cours";

        string decisionJury = moyenneGenerale.HasValue
            ? (moyenneGenerale.Value >= 10.00m ? "Admis" : "Ajourné")
            : "En cours d'évaluation";

        var bilanDto = new BilanAcademiqueDto(
            totalCreditsInscrits,
            totalCreditsAcquis,
            moyenneGenerale,
            mentionGenerale,
            decisionJury
        );

        // 8. Informations Étudiant et Inscription
        var etudiant = inscription.Etudiant;
        var etudiantDto = new EtudiantReleveInfoDto(
            etudiant.Id,
            etudiant.Matricule,
            etudiant.Nom,
            etudiant.Prenom,
            etudiant.DateNaissance,
            etudiant.LieuNaissance,
            etudiant.NumeroTelephone,
            etudiant.Email
        );

        var fn = inscription.FiliereNiveau;
        var classeLibelle = fn.LibelleAffiche ?? $"{fn.Filiere?.Nom} - {fn.Niveau?.Nom}".Trim(' ', '-');

        var inscriptionDto = new InscriptionReleveInfoDto(
            inscription.Id,
            fn.Id,
            fn.Filiere?.Nom ?? string.Empty,
            fn.Niveau?.Nom ?? string.Empty,
            classeLibelle,
            inscription.AnneeAcademiqueId,
            inscription.AnneeAcademique.Libelle,
            inscription.Etablissement.Nom,
            inscription.Etablissement.Logo,
            inscription.Etablissement.Ville
        );

        return new GetReleveNotesResponse(
            etudiantDto,
            inscriptionDto,
            situationFinanciereDto,
            semestresDto,
            bilanDto,
            DateTime.UtcNow
        );
    }

    private static string ObtenirMention(decimal moyenne) => moyenne switch
    {
        >= 16.00m => "Très Bien",
        >= 14.00m => "Bien",
        >= 12.00m => "Assez Bien",
        >= 10.00m => "Passable",
        _ => "Ajourné"
    };
}

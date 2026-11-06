namespace erp_univ_back.Features.Evaluation.Create;

using erp_univ_back.Features.Professeur;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public interface ICreateEvaluationService
{
    Task<CreateEvaluationResponse> ExecuteAsync(CreateEvaluationRequest request, CancellationToken ct = default);
}

public class CreateEvaluationService(
    ErpUnivDbContext db,
    IValidator<CreateEvaluationRequest> validator) : ICreateEvaluationService
{
    public async Task<CreateEvaluationResponse> ExecuteAsync(CreateEvaluationRequest request, CancellationToken ct = default)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.Errors.First().ErrorMessage);
        }

        // 1. Vérification de la matière (et de son UE)
        var matiere = await db.Matieres
            .Include(m => m.UniteEnseignement)
            .FirstOrDefaultAsync(m => m.Id == request.MatiereId, ct)
            ?? throw new NotFoundException($"La matière avec l'identifiant '{request.MatiereId}' n'existe pas.");

        // 2. Vérification de la classe (Filière/Niveau)
        var filiereNiveau = await db.FiliereNiveaux
            .Include(fn => fn.Filiere)
            .Include(fn => fn.Niveau)
            .FirstOrDefaultAsync(fn => fn.Id == request.FiliereNiveauId, ct)
            ?? throw new NotFoundException($"La classe avec l'identifiant '{request.FiliereNiveauId}' n'existe pas.");

        // Cohérence pédagogique : La matière doit appartenir à l'UE de la classe
        if (matiere.UniteEnseignement.FiliereNiveauId != request.FiliereNiveauId)
        {
            throw new BadRequestException($"La matière '{matiere.Nom}' n'appartient pas à la classe sélectionnée ('{filiereNiveau.LibelleAffiche ?? filiereNiveau.Filiere.Nom}').");
        }

        // 3. Vérification de l'année académique
        var anneeAcademique = await db.AnneeAcademiques
            .FirstOrDefaultAsync(a => a.Id == request.AnneeAcademiqueId, ct)
            ?? throw new NotFoundException($"L'année académique avec l'identifiant '{request.AnneeAcademiqueId}' n'existe pas.");

        // 4. Vérification optionnelle du professeur
        ProfesseurEntity? professeur = null;
        if (request.ProfesseurId.HasValue)
        {
            professeur = await db.Professeurs
                .FirstOrDefaultAsync(p => p.Id == request.ProfesseurId.Value, ct)
                ?? throw new NotFoundException($"Le professeur avec l'identifiant '{request.ProfesseurId.Value}' n'existe pas.");
        }

        // 5. Vérification d'anti-doublon pour une évaluation identique
        var cleanTitre = string.IsNullOrWhiteSpace(request.Titre) ? null : request.Titre.Trim();
        var evaluationExiste = await db.Evaluations.AnyAsync(e =>
            e.MatiereId == request.MatiereId &&
            e.FiliereNiveauId == request.FiliereNiveauId &&
            e.AnneeAcademiqueId == request.AnneeAcademiqueId &&
            e.TypeEvaluation == request.TypeEvaluation &&
            e.Session == request.Session &&
            e.Titre == cleanTitre, ct);

        if (evaluationExiste)
        {
            throw new ConflictException("Une évaluation identique existe déjà pour cette matière, classe, session et type d'évaluation.");
        }

        // 6. Création de l'entité (règle stricte : assignation exclusive des Id sans objets de navigation)
        var evaluation = new EvaluationEntity
        {
            MatiereId = request.MatiereId,
            FiliereNiveauId = request.FiliereNiveauId,
            AnneeAcademiqueId = request.AnneeAcademiqueId,
            ProfesseurId = request.ProfesseurId,
            Titre = cleanTitre,
            TypeEvaluation = request.TypeEvaluation,
            Session = request.Session,
            DateEvaluation = DateTime.SpecifyKind(request.DateEvaluation, DateTimeKind.Utc),
            Bareme = request.Bareme,
            Poids = request.Poids
        };

        db.Evaluations.Add(evaluation);
        await db.SaveChangesAsync(ct);

        var libelleClasse = filiereNiveau.LibelleAffiche 
            ?? $"{filiereNiveau.Filiere?.Nom} - {filiereNiveau.Niveau?.Nom}".Trim(' ', '-');

        var professeurNomComplet = professeur != null
            ? $"{professeur.Nom} {professeur.Prenom}".Trim()
            : null;

        return new CreateEvaluationResponse(
            evaluation.Id,
            evaluation.Titre,
            evaluation.TypeEvaluation,
            evaluation.Session,
            evaluation.DateEvaluation,
            evaluation.Bareme,
            evaluation.Poids,
            matiere.Id,
            matiere.Nom,
            matiere.Code,
            filiereNiveau.Id,
            libelleClasse,
            anneeAcademique.Id,
            anneeAcademique.Libelle,
            professeur?.Id,
            professeurNomComplet,
            evaluation.EstVerrouillee,
            evaluation.DateCreation
        );
    }
}

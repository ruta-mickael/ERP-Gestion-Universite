namespace erp_univ_back.Features.Evaluation.Create;

public record CreateEvaluationRequest(
    Guid MatiereId,
    Guid FiliereNiveauId,
    Guid AnneeAcademiqueId,
    DateTime DateEvaluation,
    string? Titre = null,
    TypeEvaluation TypeEvaluation = TypeEvaluation.ControleContinu,
    SessionEvaluation Session = SessionEvaluation.Normale,
    decimal Bareme = 20.00m,
    decimal Poids = 1.00m,
    Guid? ProfesseurId = null
);

public record CreateEvaluationResponse(
    Guid Id,
    string? Titre,
    TypeEvaluation TypeEvaluation,
    SessionEvaluation Session,
    DateTime DateEvaluation,
    decimal Bareme,
    decimal Poids,
    Guid MatiereId,
    string MatiereNom,
    string MatiereCode,
    Guid FiliereNiveauId,
    string FiliereNiveauLibelle,
    Guid AnneeAcademiqueId,
    string AnneeAcademiqueLibelle,
    Guid? ProfesseurId,
    string? ProfesseurNomComplet,
    DateTime DateCreation
);

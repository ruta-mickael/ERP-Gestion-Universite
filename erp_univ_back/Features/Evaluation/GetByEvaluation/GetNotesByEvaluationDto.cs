namespace erp_univ_back.Features.Evaluation.GetByEvaluation;

public record EvaluationResumeDto(
    Guid Id,
    string? Titre,
    TypeEvaluation TypeEvaluation,
    SessionEvaluation Session,
    DateTime DateEvaluation,
    decimal Bareme,
    decimal Poids,
    bool EstVerrouillee,
    DateTime? DateVerrouillage,
    Guid MatiereId,
    string MatiereNom,
    string MatiereCode,
    Guid FiliereNiveauId,
    string FiliereNiveauLibelle,
    Guid AnneeAcademiqueId,
    string AnneeAcademiqueLibelle,
    Guid? ProfesseurId,
    string? ProfesseurNomComplet
);

public record NoteEtudiantDto(
    Guid? NoteId,
    Guid EtudiantId,
    string Matricule,
    string Nom,
    string? Prenom,
    Guid InscriptionId,
    decimal? Valeur,
    bool EstAbsent,
    string? Commentaire,
    bool EstSaisie,
    DateTime? DateDerniereModification
);

public record StatistiquesEvaluationDto(
    int TotalInscrits,
    int TotalNotesSaisies,
    int TotalPresents,
    int TotalAbsents,
    decimal? MoyenneClasse,
    decimal? NoteMin,
    decimal? NoteMax
);

public record GetNotesByEvaluationResponse(
    EvaluationResumeDto Evaluation,
    StatistiquesEvaluationDto Statistiques,
    List<NoteEtudiantDto> Lignes
);

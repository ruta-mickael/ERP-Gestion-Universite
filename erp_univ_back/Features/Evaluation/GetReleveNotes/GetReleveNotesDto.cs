namespace erp_univ_back.Features.Evaluation.GetReleveNotes;

public record EvaluationNoteItemDto(
    Guid EvaluationId,
    string? Titre,
    TypeEvaluation TypeEvaluation,
    SessionEvaluation Session,
    decimal Bareme,
    decimal Poids,
    decimal? NoteObtenue,
    bool EstAbsent,
    string? Commentaire
);

public record MatiereReleveDto(
    Guid MatiereId,
    string Nom,
    string Code,
    int Coefficient,
    decimal? Moyenne,
    List<EvaluationNoteItemDto> Evaluations
);

public record UniteEnseignementReleveDto(
    Guid UeId,
    string Nom,
    string Code,
    int Credit,
    int Semestre,
    decimal? Moyenne,
    bool EstValidee,
    int CreditsAcquis,
    List<MatiereReleveDto> Matieres
);

public record SemestreReleveDto(
    int Semestre,
    int TotalCredits,
    int CreditsAcquis,
    decimal? Moyenne,
    string? Mention,
    List<UniteEnseignementReleveDto> UnitesEnseignement
);

public record SituationFinanciereReleveDto(
    bool EstEnRegle,
    decimal MontantTotalAttendu,
    decimal MontantDejaPaye,
    decimal SoldeRestantDu,
    string MentionFinanciere
);

public record EtudiantReleveInfoDto(
    Guid EtudiantId,
    string Matricule,
    string Nom,
    string? Prenom,
    DateOnly DateNaissance,
    string LieuNaissance,
    string NumeroTelephone,
    string Email
);

public record InscriptionReleveInfoDto(
    Guid InscriptionId,
    Guid FiliereNiveauId,
    string FiliereNom,
    string NiveauNom,
    string ClasseLibelle,
    Guid AnneeAcademiqueId,
    string AnneeAcademiqueLibelle,
    string EtablissementNom,
    string? EtablissementLogo,
    string EtablissementVille
);

public record BilanAcademiqueDto(
    int TotalCreditsInscrits,
    int TotalCreditsAcquis,
    decimal? MoyenneGenerale,
    string Mention,
    string DecisionJury
);

public record GetReleveNotesResponse(
    EtudiantReleveInfoDto Etudiant,
    InscriptionReleveInfoDto Inscription,
    SituationFinanciereReleveDto SituationFinanciere,
    List<SemestreReleveDto> Semestres,
    BilanAcademiqueDto Bilan,
    DateTime DateEdition
);

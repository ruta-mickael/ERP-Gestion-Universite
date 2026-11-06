namespace erp_univ_back.Features.Evaluation.SaisirNotes;

public record LigneSaisieNoteRequest(
    Guid EtudiantId,
    decimal? Valeur = null,
    bool EstAbsent = false,
    string? Commentaire = null
);

public record SaisirNotesRequest(
    List<LigneSaisieNoteRequest> Notes
);

public record NoteSaisieResultDto(
    Guid NoteId,
    Guid EtudiantId,
    string EtudiantMatricule,
    string EtudiantNomComplet,
    Guid InscriptionId,
    decimal? Valeur,
    bool EstAbsent,
    string? Commentaire,
    bool EstNouvelle
);

public record SaisirNotesResponse(
    Guid EvaluationId,
    string? EvaluationTitre,
    decimal Bareme,
    int TotalTraites,
    int TotalInseres,
    int TotalMisAJour,
    List<NoteSaisieResultDto> Notes
);

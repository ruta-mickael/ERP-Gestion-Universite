namespace erp_univ_back.Features.ConfigFinancier.CreateCatalogue;

public record CreateCatalogueRequest(
    Guid AnneeAcademiqueId,
    CategorieFrais Categorie,
    string Libelle,
    decimal MontantTotal,
    MoyenPaiement MoyensPaiementAutorises,
    Guid? CycleId = null,
    Guid? FiliereId = null,
    Guid? NiveauId = null,
    bool EstObligatoirePourDossier = true
);

public record CreateCatalogueResponse(
    Guid Id,
    Guid AnneeAcademiqueId,
    string AnneeAcademiqueLibelle,
    Guid? CycleId,
    string? CycleNom,
    Guid? FiliereId,
    string? FiliereNom,
    Guid? NiveauId,
    string? NiveauNom,
    CategorieFrais Categorie,
    string Libelle,
    decimal MontantTotal,
    MoyenPaiement MoyensPaiementAutorises,
    bool EstObligatoirePourDossier,
    DateTime DateCreation
);

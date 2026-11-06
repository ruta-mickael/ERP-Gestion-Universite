namespace erp_univ_back.Features.ConfigFinancier.AddPlanPaiement;

public record ModeleTrancheItemRequest(
    int NumeroOrdre,
    string Libelle,
    decimal Montant,
    DateTime DateLimite,
    bool ExigePourValiderInscription = false
);

public record AddPlanPaiementRequest(
    Guid FraisCatalogueId,
    string Nom,
    List<ModeleTrancheItemRequest> Tranches
);

public record ModeleTrancheItemResponse(
    Guid Id,
    int NumeroOrdre,
    string Libelle,
    decimal Montant,
    DateTime DateLimite,
    bool ExigePourValiderInscription
);

public record AddPlanPaiementResponse(
    Guid Id,
    Guid FraisCatalogueId,
    string FraisCatalogueLibelle,
    string Nom,
    int NombreTranches,
    bool EstActif,
    decimal MontantTotal,
    List<ModeleTrancheItemResponse> Tranches,
    DateTime DateCreation
);


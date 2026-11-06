namespace erp_univ_back.Features.ConfigFinancier;

public enum CategorieFrais
{
    Concours,
    DroitInscription,
    DroitReinscription,
    FraisFormation,
    VoyageEtude,
    Soutenance,
    Certificat,
    Autre
}

[Flags]
public enum MoyenPaiement
{
    Aucun = 0,
    Especes = 1,
    MVola = 2,
    OrangeMoney = 4,
    AirtelMoney = 8,
    VersementBancaire = 16,
    VirementBancaire = 32,

    // Raccourci pour regrouper tous les Mobile Money (2 + 4 + 8 = 14)
    MobileMoney = MVola | OrangeMoney | AirtelMoney
}

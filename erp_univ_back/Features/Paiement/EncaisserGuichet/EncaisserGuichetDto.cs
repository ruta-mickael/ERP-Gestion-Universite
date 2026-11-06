namespace erp_univ_back.Features.Paiement.EncaisserGuichet;

using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Inscription;

public record EncaisserGuichetRequest(
    Guid? InscriptionId = null,
    string? Matricule = null,
    decimal Montant = 0,
    MoyenPaiement MoyenPaiement = MoyenPaiement.Especes,
    Guid? EcheanceId = null,
    string? ReferenceExterne = null,
    string? NomBanque = null
);

public record EcheanceVentileeResponse(
    Guid Id,
    int NumeroOrdre,
    string Libelle,
    decimal MontantAttendu,
    decimal MontantDejaPaye,
    decimal ResteAPayer,
    bool ExigePourValiderInscription,
    StatutEcheance Statut
);

public record EncaisserGuichetResponse(
    Guid TransactionId,
    Guid InscriptionId,
    Guid EtudiantId,
    string EtudiantMatricule,
    string EtudiantNomComplet,
    decimal MontantVerse,
    MoyenPaiement MoyenPaiement,
    StatutInscription StatutInscription,
    decimal ResteAPayerTotal,
    List<EcheanceVentileeResponse> Echeances,
    DateTime DatePaiement
);

public record SituationFinanciereEtudiantResponse(
    Guid InscriptionId,
    Guid EtudiantId,
    string Matricule,
    string NomComplet,
    string ClasseLibelle,
    string AnneeAcademiqueLibelle,
    StatutInscription StatutInscription,
    decimal TotalAttendu,
    decimal TotalPaye,
    decimal ResteAPayerTotal,
    List<EcheanceVentileeResponse> Echeances
);

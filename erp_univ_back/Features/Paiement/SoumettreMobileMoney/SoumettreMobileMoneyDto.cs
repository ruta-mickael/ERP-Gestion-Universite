namespace erp_univ_back.Features.Paiement.SoumettreMobileMoney;

using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Paiement;

public record SoumettreMobileMoneyRequest(
    string? Matricule = null,
    Guid? InscriptionId = null,
    decimal Montant = 0,
    MoyenPaiement MoyenPaiement = MoyenPaiement.MVola,
    string ReferenceExterne = "",
    DateTime DateOperationDeclaree = default,
    string? NomBanque = null,
    string? PreuveImageUrl = null
);

public record SoumettreMobileMoneyResponse(
    Guid TransactionId,
    Guid InscriptionId,
    Guid EtudiantId,
    string EtudiantMatricule,
    string EtudiantNomComplet,
    decimal Montant,
    MoyenPaiement MoyenPaiement,
    string? NomBanque,
    string ReferenceExterne,
    DateTime DateOperationDeclaree,
    StatutValidationPaiement StatutValidation,
    string? PreuveImageUrl,
    DateTime DateSoumission,
    string Message
);

namespace erp_univ_back.Features.Paiement.ValiderTransaction;

using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Paiement;
using erp_univ_back.Features.Paiement.EncaisserGuichet;

public record ValiderTransactionRequest(
    bool EstValide = true,
    string? MotifRejet = null,
    Guid? EcheanceCibleId = null
);

public record ValiderTransactionResponse(
    Guid TransactionId,
    Guid InscriptionId,
    Guid EtudiantId,
    string EtudiantMatricule,
    string EtudiantNomComplet,
    decimal Montant,
    MoyenPaiement MoyenPaiement,
    string? NomBanque,
    string? ReferenceExterne,
    StatutValidationPaiement StatutValidation,
    DateTime DateOperationDeclaree,
    DateTime? DateValidation,
    string? MotifRejet,
    StatutInscription StatutInscription,
    decimal ResteAPayerTotal,
    List<EcheanceVentileeResponse> Echeances,
    string Message
);

namespace erp_univ_back.Features.Paiement.GetById;

using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Paiement;

public record GetPaiementByIdResponse(
    Guid Id,
    Guid InscriptionId,
    Guid EtudiantId,
    string EtudiantMatricule,
    string EtudiantNomComplet,
    decimal Montant,
    MoyenPaiement MoyenPaiement,
    string? NomBanque,
    string? ReferenceExterne,
    DateTime DateOperationDeclaree,
    StatutValidationPaiement StatutValidation,
    string? PreuveImageUrl,
    DateTime? DateValidation,
    string? MotifRejet,
    DateTime DateCreation,
    string Message
);

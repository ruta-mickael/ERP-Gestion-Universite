using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Etudiant;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.Paiement;

public enum StatutValidationPaiement
{
    EnAttenteVerification,
    Valide,
    Rejete
}

[Table("transactions_paiement")]
public class TransactionPaiementEntity : BaseEntity
{
    public Guid EtablissementId { get; set; }
    public EtablissementEntity Etablissement { get; set; } = null!;

    public Guid InscriptionId { get; set; }
    public InscriptionEntity Inscription { get; set; } = null!;

    public Guid EtudiantId { get; set; }
    public EtudiantEntity Etudiant { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Montant { get; set; }

    public MoyenPaiement MoyenPaiement { get; set; }

    [MaxLength(50)]
    public string? NomBanque { get; set; }

    [MaxLength(100)]
    public string? ReferenceExterne { get; set; }

    public DateTime DateOperationDeclaree { get; set; }

    [MaxLength(500)]
    public string? PreuveImageUrl { get; set; }

    public string? DonneesExtraitesIaJson { get; set; }

    public StatutValidationPaiement StatutValidation { get; set; } = StatutValidationPaiement.EnAttenteVerification;

    public Guid? VerifieParUtilisateurId { get; set; }

    public DateTime? DateValidation { get; set; }

    [MaxLength(300)]
    public string? MotifRejet { get; set; }
}

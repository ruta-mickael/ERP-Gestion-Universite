using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.ConfigFinancier;

[Table("modeles_tranches")]
public class ModeleTrancheEntity : BaseEntity
{
    public Guid EtablissementId { get; set; }
    public EtablissementEntity Etablissement { get; set; } = null!;

    public Guid PlanPaiementId { get; set; }
    public PlanPaiementEntity PlanPaiement { get; set; } = null!;

    public int NumeroOrdre { get; set; }

    [Required]
    [MaxLength(150)]
    public required string Libelle { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Montant { get; set; }

    public DateTime DateLimite { get; set; }

    public bool ExigePourValiderInscription { get; set; }
}

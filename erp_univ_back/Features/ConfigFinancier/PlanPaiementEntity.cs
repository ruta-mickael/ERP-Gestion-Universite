using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.ConfigFinancier;

[Table("plans_paiement")]
public class PlanPaiementEntity : BaseEntity
{
    public Guid EtablissementId { get; set; }
    public EtablissementEntity Etablissement { get; set; } = null!;

    public Guid FraisCatalogueId { get; set; }
    public FraisCatalogueEntity FraisCatalogue { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public required string Nom { get; set; }

    public int NombreTranches { get; set; }

    public bool EstActif { get; set; } = true;

    public ICollection<ModeleTrancheEntity> ModelesTranches { get; set; } = [];
}

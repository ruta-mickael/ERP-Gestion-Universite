using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.Inscription;

[Table("echeances_etudiant")]
public class EcheanceEtudiantEntity : BaseEntity
{
    public Guid EtablissementId { get; set; }
    public EtablissementEntity Etablissement { get; set; } = null!;

    public Guid InscriptionId { get; set; }
    public InscriptionEntity Inscription { get; set; } = null!;

    public Guid FraisCatalogueId { get; set; }
    public FraisCatalogueEntity FraisCatalogue { get; set; } = null!;

    public int NumeroOrdre { get; set; }

    [Required]
    [MaxLength(150)]
    public required string Libelle { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontantAttendu { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontantDejaPaye { get; set; } = 0;

    public DateTime DateLimite { get; set; }

    public bool ExigePourValiderInscription { get; set; }

    public StatutEcheance Statut { get; set; } = StatutEcheance.NonPaye;
}

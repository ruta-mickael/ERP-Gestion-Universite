using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.AnneeAcademique;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Filiere;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Niveau;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.ConfigFinancier;

[Table("frais_catalogue")]
public class FraisCatalogueEntity : BaseEntity
{
    public Guid EtablissementId { get; set; }
    public EtablissementEntity Etablissement { get; set; } = null!;

    public Guid AnneeAcademiqueId { get; set; }
    public AnneeAcademiqueEntity AnneeAcademique { get; set; } = null!;

    // --- Ciblage modulaire ---
    public Guid? CycleId { get; set; }
    public CycleEntity? Cycle { get; set; }

    public Guid? FiliereId { get; set; }
    public FiliereEntity? Filiere { get; set; }

    public Guid? NiveauId { get; set; }
    public NiveauEntity? Niveau { get; set; }

    public CategorieFrais Categorie { get; set; }

    [Required]
    [MaxLength(150)]
    public required string Libelle { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontantTotal { get; set; }

    public MoyenPaiement MoyensPaiementAutorises { get; set; }

    public bool EstObligatoirePourDossier { get; set; }

    public ICollection<PlanPaiementEntity> PlansPaiement { get; set; } = [];
    public ICollection<EcheanceEtudiantEntity> Echeances { get; set; } = [];
}

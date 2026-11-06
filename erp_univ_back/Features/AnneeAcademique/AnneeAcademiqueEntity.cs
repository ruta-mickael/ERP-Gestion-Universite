using System.ComponentModel.DataAnnotations;
using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.AnneeAcademique
{
    public class AnneeAcademiqueEntity : BaseEntity
    {
        [Required]
        [MaxLength(20)]
        public required string Libelle { get; set; } = string.Empty;

        [Required]
        public required DateOnly DateDebut { get; set; }

        [Required]
        public required DateOnly DateFin { get; set; }

        public bool EstActive { get; set; } = false;

        public Guid EtablissementId { get; set; }
        public EtablissementEntity Etablissement { get; set; } = null!;

        public ICollection<InscriptionEntity> Inscriptions { get; set; } = [];
        public ICollection<FraisCatalogueEntity> FraisCatalogues { get; set; } = [];
    }
}

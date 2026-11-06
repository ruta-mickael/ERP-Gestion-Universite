using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.FiliereNiveau;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.Filiere
{
    [Table("filieres")]
    public class FiliereEntity : BaseEntity
    {
        private string _code = string.Empty;
        [Required]
        [MaxLength(20)]
        public required string Code
        {
            get => _code;
            set => _code = value.Trim().ToUpperInvariant();
        }

        [Required]
        [MaxLength(150)]
        public required string Nom { get; set; }

        public string? Description { get; set; }

        public Guid EtablissementId { get; set; }
        public EtablissementEntity Etablissement { get; set; } = null!;

        public ICollection<FiliereNiveauEntity> FiliereNiveaux { get; set; } = [];
        public ICollection<FraisCatalogueEntity> FraisCatalogues { get; set; } = [];
    }
}

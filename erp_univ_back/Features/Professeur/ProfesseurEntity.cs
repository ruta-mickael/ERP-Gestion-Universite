using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using erp_univ_back.Shared.Entities;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Evaluation;

namespace erp_univ_back.Features.Professeur
{
    [Table("professeurs")]
    public class ProfesseurEntity : BaseEntity
    {
        [Required]
        [MaxLength(60)]
        public required string Matricule { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Nom { get; set; }

        [MaxLength(100)]
        public string? Prenom { get; set; }

        [Required]
        [MaxLength(20)]
        public required string NumeroTelephone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [MaxLength(17)]
        public required string NumeroCin { get; set; }

        [Required]
        public required DateOnly DateNaissance { get; set; }

        [Required]
        [MaxLength(100)]
        public required string LieuNaissance { get; set; }

        public required Guid EtablissementId { get; set; }
        public EtablissementEntity Etablissement { get; set; } = null!;

        public ICollection<EvaluationEntity> Evaluations { get; set; } = [];
    }
}
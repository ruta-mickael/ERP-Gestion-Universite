using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Evaluation;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Paiement;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.Etudiant
{
    public enum Role
    {
        Pere,
        Mere,
        Tuteur
    }

    public enum Statut
    {
        Actif,
        Inactif,
        Diplome,
        Abandonne
    }

    [Table("etudiants")]
    public class EtudiantEntity : BaseEntity
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
        public required DateOnly DateNaissance { get; set; }

        [Required]
        [MaxLength(100)]
        public required string LieuNaissance { get; set; }

        [MaxLength(17)]
        public string? NumeroCin { get; set; }

        [Required]
        [MaxLength(20)]
        public required string NumeroTelephone { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        public Guid EtablissementId { get; set; }
        public EtablissementEntity Etablissement { get; set; } = null!;

        public ICollection<EtudiantParentEntity> EtudiantParents { get; set; } = [];
        public ICollection<InscriptionEntity> Inscriptions { get; set; } = [];
        public ICollection<TransactionPaiementEntity> TransactionsPaiement { get; set; } = [];
        public ICollection<NoteEntity> Notes { get; set; } = [];
    }

    [Table("parents")]
    public class ParentEntity : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public required string Nom { get; set; }

        [MaxLength(100)]
        public string? Prenom { get; set; }

        [Required]
        [MaxLength(20)]
        public required string NumeroTelphone { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Profession { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public Guid EtablissementId { get; set; }
        public EtablissementEntity Etablissement { get; set; } = null!;

        public ICollection<EtudiantParentEntity> EtudiantParents { get; set; } = [];
    }

    [Table("etudiant_parent")]
    public class EtudiantParentEntity : BaseEntity
    {
        public Role Role { get; set; } = Role.Pere;

        public Guid ParentId { get; set; }
        public ParentEntity Parent { get; set; } = null!;

        public Guid EtudiantId { get; set; }
        public EtudiantEntity Etudiant { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.AnneeAcademique;
using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Etudiant;
using erp_univ_back.Features.Evaluation;
using erp_univ_back.Features.Filiere;
using erp_univ_back.Features.FiliereNiveau;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Matiere;
using erp_univ_back.Features.Niveau;
using erp_univ_back.Features.Paiement;
using erp_univ_back.Features.Professeur;
using erp_univ_back.Features.UniteEnseignement;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.Etablissement
{
    public enum StatusEtablissement
    {
        Actif,
        Inactif,
        Suspendu
    }

    [Table("etablissements")]
    public class EtablissementEntity : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public required string Nom { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Adresse { get; set; }

        [Required]
        [MaxLength(10)]
        public required string CodePostal { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Ville { get; set; }

        [Required]
        public required string Pays { get; set; }

        [Required]
        [MaxLength(20)]
        public required string Telephone { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(50)]
        public required string Email { get; set; }

        [MaxLength(100)]
        [Url]
        public string? SiteWeb { get; set; }

        [MaxLength(200)]
        public string? Logo { get; set; }

        [MaxLength(30)]
        public string? Sigle { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public StatusEtablissement Statut { get; set; } = StatusEtablissement.Actif;

        public ICollection<EtudiantEntity> Etudiants { get; set; } = [];
        public ICollection<ProfesseurEntity> Professeurs { get; set; } = [];
        public ICollection<AnneeAcademiqueEntity> AnneeAcademiques { get; set; } = [];
        public ICollection<FiliereEntity> Filieres { get; set; } = [];
        public ICollection<NiveauEntity> Niveaux { get; set; } = [];
        public ICollection<FiliereNiveauEntity> FiliereNiveaux { get; set; } = [];
        public ICollection<ParentEntity> Parents { get; set; } = [];
        public ICollection<InscriptionEntity> Inscriptions { get; set; } = [];
        public ICollection<UniteEnseignementEntity> UniteEnseignements { get; set; } = [];
        public ICollection<MatiereEntity> Matieres { get; set; } = [];
        public ICollection<FraisCatalogueEntity> FraisCatalogues { get; set; } = [];
        public ICollection<PlanPaiementEntity> PlansPaiement { get; set; } = [];
        public ICollection<ModeleTrancheEntity> ModelesTranches { get; set; } = [];
        public ICollection<EcheanceEtudiantEntity> EcheancesEtudiant { get; set; } = [];
        public ICollection<TransactionPaiementEntity> TransactionsPaiement { get; set; } = [];
        public ICollection<EvaluationEntity> Evaluations { get; set; } = [];
        public ICollection<NoteEntity> Notes { get; set; } = [];
    }
}

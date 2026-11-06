namespace erp_univ_back.Features.Evaluation;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.AnneeAcademique;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.FiliereNiveau;
using erp_univ_back.Features.Matiere;
using erp_univ_back.Features.Professeur;
using erp_univ_back.Shared.Entities;

[Table("evaluations")]
public class EvaluationEntity : BaseEntity
{
    [MaxLength(150)]
    public string? Titre { get; set; }

    public TypeEvaluation TypeEvaluation { get; set; } = TypeEvaluation.ControleContinu;

    public SessionEvaluation Session { get; set; } = SessionEvaluation.Normale;

    public DateTime DateEvaluation { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Bareme { get; set; } = 20.00m;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Poids { get; set; } = 1.00m;

    public bool EstVerrouillee { get; set; } = false;

    public DateTime? DateVerrouillage { get; set; }

    public Guid MatiereId { get; set; }
    public MatiereEntity Matiere { get; set; } = null!;

    public Guid FiliereNiveauId { get; set; }
    public FiliereNiveauEntity FiliereNiveau { get; set; } = null!;

    public Guid AnneeAcademiqueId { get; set; }
    public AnneeAcademiqueEntity AnneeAcademique { get; set; } = null!;

    public Guid? ProfesseurId { get; set; }
    public ProfesseurEntity? Professeur { get; set; }

    public Guid EtablissementId { get; set; }
    public EtablissementEntity Etablissement { get; set; } = null!;

    public ICollection<NoteEntity> Notes { get; set; } = [];
}

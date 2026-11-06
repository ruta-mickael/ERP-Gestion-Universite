using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Evaluation;
using erp_univ_back.Features.FiliereNiveau;
using erp_univ_back.Features.UniteEnseignement;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.Matiere;

[Table("Matieres")]
public class MatiereEntity : BaseEntity
{
    [Required]
    [MaxLength(150)]
    public required string Nom {get;set;}

    [Required]
    [MaxLength(50)]
    public required string Code { get; set; }

    [Required]
    public required int Coefficient {get;set;} 

    [Required]
    public required Guid UniteEnseignementId {get;set;}
    public UniteEnseignementEntity UniteEnseignement {get;set;} = null!;

    [Required]
    public required Guid EtablissementId {get;set;}
    public EtablissementEntity Etablissement {get;set;} = null!;

    public ICollection<EvaluationEntity> Evaluations { get; set; } = [];
}
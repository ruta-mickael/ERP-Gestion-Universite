namespace erp_univ_back.Features.Evaluation;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Etudiant;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Shared.Entities;

[Table("notes")]
public class NoteEntity : BaseEntity
{
    public Guid EvaluationId { get; set; }
    public EvaluationEntity Evaluation { get; set; } = null!;

    public Guid InscriptionId { get; set; }
    public InscriptionEntity Inscription { get; set; } = null!;

    public Guid EtudiantId { get; set; }
    public EtudiantEntity Etudiant { get; set; } = null!;

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Valeur { get; set; }

    public bool EstAbsent { get; set; } = false;

    [MaxLength(250)]
    public string? Commentaire { get; set; }

    public Guid EtablissementId { get; set; }
    public EtablissementEntity Etablissement { get; set; } = null!;
}

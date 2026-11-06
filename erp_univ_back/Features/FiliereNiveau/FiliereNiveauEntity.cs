namespace erp_univ_back.Features.FiliereNiveau;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Filiere;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Niveau;
using erp_univ_back.Features.UniteEnseignement;
using erp_univ_back.Shared.Entities;

[Table("filiere_niveaux")]
public class FiliereNiveauEntity : BaseEntity
{
    public Guid FiliereId { get; set; }
    public FiliereEntity Filiere { get; set; } = null!;

    public Guid NiveauId { get; set; }
    public NiveauEntity Niveau { get; set; } = null!;

    [MaxLength(150)]
    public string? LibelleAffiche { get; set; }

    public Guid EtablissementId { get; set; }
    public EtablissementEntity Etablissement { get; set; } = null!;

    public ICollection<InscriptionEntity> Inscriptions { get; set; } = [];
    public ICollection<UniteEnseignementEntity> UniteEnseignements { get; set; } = [];
}

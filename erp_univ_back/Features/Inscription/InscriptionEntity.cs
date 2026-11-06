using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.AnneeAcademique;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Etudiant;
using erp_univ_back.Features.Evaluation;
using erp_univ_back.Features.FiliereNiveau;
using erp_univ_back.Features.Paiement;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.Inscription;

public enum TypeInscription
{
    PremiereInscription,
    Reinscription
}

public enum StatutInscription
{
    EnAttentePaiement,
    Validee,
    Inscrit = Validee,
    Suspendue,
    Abandon = Suspendue,
    Annulee,
    Annule = Annulee
}

public enum StatutEcheance
{
    NonPaye,
    Partiel,
    Solde
}

[Table("inscriptions")]
public class InscriptionEntity : BaseEntity
{
    public Guid EtudiantId { get; set; }
    public EtudiantEntity Etudiant { get; set; } = null!;

    public Guid FiliereNiveauId { get; set; }
    public FiliereNiveauEntity FiliereNiveau { get; set; } = null!;

    public Guid AnneeAcademiqueId { get; set; }
    public AnneeAcademiqueEntity AnneeAcademique { get; set; } = null!;

    public TypeInscription TypeInscription { get; set; } = TypeInscription.PremiereInscription;

    public StatutInscription Statut { get; set; } = StatutInscription.Inscrit;

    public Guid EtablissementId { get; set; }
    public EtablissementEntity Etablissement { get; set; } = null!;

    public ICollection<EcheanceEtudiantEntity> Echeances { get; set; } = [];
    public ICollection<TransactionPaiementEntity> TransactionsPaiement { get; set; } = [];
    public ICollection<NoteEntity> Notes { get; set; } = [];
}

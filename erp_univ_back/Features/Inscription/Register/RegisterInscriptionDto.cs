using erp_univ_back.Features.Etudiant.Create;

namespace erp_univ_back.Features.Inscription.Register;

public record RegisterInscriptionRequest(
    CreateEtudiantRequest EtudiantInfos,
    Guid IdFiliereNiveau,
    Guid IdAnneeAcademique,
    List<Guid>? PlansPaiementIds = null
);

public record EcheanceItemResponse(
    Guid Id,
    Guid FraisCatalogueId,
    string Libelle,
    int NumeroOrdre,
    decimal MontantAttendu,
    decimal MontantDejaPaye,
    DateTime DateLimite,
    bool ExigePourValiderInscription,
    StatutEcheance Statut
);

public record RegisterInscriptionResponse(
    Guid Id,
    Guid EtudiantId,
    string EtudiantNom,
    string EtudiantPrenom,
    string EtudiantMatricule,
    Guid FiliereNiveauId,
    string ClasseLibelle,
    Guid AnneeAcademiqueId,
    string AnneeAcademiqueLibelle,
    StatutInscription Statut,
    DateTime DateCreation
)
{
    public List<EcheanceItemResponse> Echeances { get; init; } = [];
}

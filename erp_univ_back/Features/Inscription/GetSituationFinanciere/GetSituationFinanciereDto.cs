namespace erp_univ_back.Features.Inscription.GetSituationFinanciere;

using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Paiement.EncaisserGuichet;

public record SituationFinanciereEtudiantResponse(
    Guid InscriptionId,
    Guid EtudiantId,
    string Matricule,
    string NomComplet,
    string ClasseLibelle,
    string AnneeAcademiqueLibelle,
    StatutInscription StatutInscription,
    decimal TotalAttendu,
    decimal TotalPaye,
    decimal ResteAPayerTotal,
    List<EcheanceVentileeResponse> Echeances
);

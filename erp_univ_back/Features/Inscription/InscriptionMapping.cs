namespace erp_univ_back.Features.Inscription;

using erp_univ_back.Features.Inscription.ChangeStatut;
using erp_univ_back.Features.Inscription.GetByClasse;
using erp_univ_back.Features.Inscription.GetByEtudiant;
using erp_univ_back.Features.Inscription.Register;
using erp_univ_back.Features.Inscription.Reinscription;
using Mapster;

public class InscriptionMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ReinscriptionRequest, InscriptionEntity>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.DateCreation)
            .Ignore(dest => dest.DateModification)
            .Ignore(dest => dest.EtablissementId)
            .Ignore(dest => dest.Etablissement)
            .Ignore(dest => dest.Etudiant)
            .Ignore(dest => dest.FiliereNiveau)
            .Ignore(dest => dest.AnneeAcademique)
            .Map(dest => dest.Statut, _ => StatutInscription.Inscrit);

        config.NewConfig<InscriptionEntity, ReinscriptionResponse>()
            .Map(dest => dest.EtudiantNom, src => src.Etudiant.Nom)
            .Map(dest => dest.EtudiantPrenom, src => src.Etudiant.Prenom ?? "")
            .Map(dest => dest.EtudiantMatricule, src => src.Etudiant.Matricule)
            .Map(dest => dest.ClasseLibelle, src => !string.IsNullOrWhiteSpace(src.FiliereNiveau.LibelleAffiche)
                ? src.FiliereNiveau.LibelleAffiche
                : $"{src.FiliereNiveau.Niveau.Nom} {src.FiliereNiveau.Filiere.Nom}")
            .Map(dest => dest.AnneeAcademiqueLibelle, src => src.AnneeAcademique.Libelle);

        config.NewConfig<InscriptionEntity, RegisterInscriptionResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.EtudiantId, src => src.EtudiantId)
            .Map(dest => dest.EtudiantNom, src => src.Etudiant.Nom)
            .Map(dest => dest.EtudiantPrenom, src => src.Etudiant.Prenom ?? "")
            .Map(dest => dest.EtudiantMatricule, src => src.Etudiant.Matricule)
            .Map(dest => dest.FiliereNiveauId, src => src.FiliereNiveauId)
            .Map(dest => dest.ClasseLibelle, src => !string.IsNullOrWhiteSpace(src.FiliereNiveau.LibelleAffiche)
                ? src.FiliereNiveau.LibelleAffiche
                : $"{src.FiliereNiveau.Niveau.Nom} {src.FiliereNiveau.Filiere.Nom}")
            .Map(dest => dest.AnneeAcademiqueId, src => src.AnneeAcademiqueId)
            .Map(dest => dest.AnneeAcademiqueLibelle, src => src.AnneeAcademique.Libelle)
            .Map(dest => dest.Statut, src => src.Statut)
            .Map(dest => dest.DateCreation, src => src.DateCreation)
            .Map(dest => dest.Echeances, src => src.Echeances.Select(e => new EcheanceItemResponse(
                e.Id,
                e.FraisCatalogueId,
                e.Libelle,
                e.NumeroOrdre,
                e.MontantAttendu,
                e.MontantDejaPaye,
                e.DateLimite,
                e.ExigePourValiderInscription,
                e.Statut
            )).ToList());

        config.NewConfig<InscriptionEntity, GetInscriptionsByClasseResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.EtudiantId, src => src.EtudiantId)
            .Map(dest => dest.Matricule, src => src.Etudiant.Matricule)
            .Map(dest => dest.Nom, src => src.Etudiant.Nom)
            .Map(dest => dest.Prenom, src => src.Etudiant.Prenom)
            .Map(dest => dest.Email, src => src.Etudiant.Email)
            .Map(dest => dest.NumeroTelephone, src => src.Etudiant.NumeroTelephone)
            .Map(dest => dest.Statut, src => src.Statut)
            .Map(dest => dest.DateInscription, src => src.DateCreation);

        config.NewConfig<InscriptionEntity, InscriptionByClasseResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.EtudiantId, src => src.EtudiantId)
            .Map(dest => dest.Matricule, src => src.Etudiant.Matricule)
            .Map(dest => dest.Nom, src => src.Etudiant.Nom)
            .Map(dest => dest.Prenom, src => src.Etudiant.Prenom)
            .Map(dest => dest.Email, src => src.Etudiant.Email)
            .Map(dest => dest.NumeroTelephone, src => src.Etudiant.NumeroTelephone)
            .Map(dest => dest.Statut, src => src.Statut)
            .Map(dest => dest.DateInscription, src => src.DateCreation);

        config.NewConfig<InscriptionEntity, GetInscriptionsByEtudiantResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.FiliereNiveauId, src => src.FiliereNiveauId)
            .Map(dest => dest.FiliereNom, src => src.FiliereNiveau.Filiere.Nom)
            .Map(dest => dest.NiveauNom, src => src.FiliereNiveau.Niveau.Nom)
            .Map(dest => dest.ClasseLibelle, src => !string.IsNullOrWhiteSpace(src.FiliereNiveau.LibelleAffiche)
                ? src.FiliereNiveau.LibelleAffiche
                : $"{src.FiliereNiveau.Niveau.Nom} {src.FiliereNiveau.Filiere.Nom}")
            .Map(dest => dest.AnneeAcademiqueId, src => src.AnneeAcademiqueId)
            .Map(dest => dest.AnneeAcademiqueLibelle, src => src.AnneeAcademique.Libelle)
            .Map(dest => dest.Statut, src => src.Statut)
            .Map(dest => dest.DateInscription, src => src.DateCreation);

        config.NewConfig<InscriptionEntity, InscriptionByEtudiantResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.FiliereNiveauId, src => src.FiliereNiveauId)
            .Map(dest => dest.FiliereNom, src => src.FiliereNiveau.Filiere.Nom)
            .Map(dest => dest.NiveauNom, src => src.FiliereNiveau.Niveau.Nom)
            .Map(dest => dest.ClasseLibelle, src => !string.IsNullOrWhiteSpace(src.FiliereNiveau.LibelleAffiche)
                ? src.FiliereNiveau.LibelleAffiche
                : $"{src.FiliereNiveau.Niveau.Nom} {src.FiliereNiveau.Filiere.Nom}")
            .Map(dest => dest.AnneeAcademiqueId, src => src.AnneeAcademiqueId)
            .Map(dest => dest.AnneeAcademiqueLibelle, src => src.AnneeAcademique.Libelle)
            .Map(dest => dest.Statut, src => src.Statut)
            .Map(dest => dest.DateInscription, src => src.DateCreation);

        config.NewConfig<InscriptionEntity, ChangeStatutInscriptionResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.EtudiantId, src => src.EtudiantId)
            .Map(dest => dest.EtudiantNom, src => src.Etudiant.Nom)
            .Map(dest => dest.EtudiantPrenom, src => src.Etudiant.Prenom ?? "")
            .Map(dest => dest.EtudiantMatricule, src => src.Etudiant.Matricule)
            .Map(dest => dest.FiliereNiveauId, src => src.FiliereNiveauId)
            .Map(dest => dest.ClasseLibelle, src => !string.IsNullOrWhiteSpace(src.FiliereNiveau.LibelleAffiche)
                ? src.FiliereNiveau.LibelleAffiche
                : $"{src.FiliereNiveau.Niveau.Nom} {src.FiliereNiveau.Filiere.Nom}")
            .Map(dest => dest.AnneeAcademiqueId, src => src.AnneeAcademiqueId)
            .Map(dest => dest.AnneeAcademiqueLibelle, src => src.AnneeAcademique.Libelle)
            .Map(dest => dest.Statut, src => src.Statut)
            .Map(dest => dest.DateModification, src => src.DateModification);

        config.NewConfig<InscriptionEntity, ChangeStatutResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.EtudiantId, src => src.EtudiantId)
            .Map(dest => dest.EtudiantNom, src => src.Etudiant.Nom)
            .Map(dest => dest.EtudiantPrenom, src => src.Etudiant.Prenom ?? "")
            .Map(dest => dest.EtudiantMatricule, src => src.Etudiant.Matricule)
            .Map(dest => dest.FiliereNiveauId, src => src.FiliereNiveauId)
            .Map(dest => dest.ClasseLibelle, src => !string.IsNullOrWhiteSpace(src.FiliereNiveau.LibelleAffiche)
                ? src.FiliereNiveau.LibelleAffiche
                : $"{src.FiliereNiveau.Niveau.Nom} {src.FiliereNiveau.Filiere.Nom}")
            .Map(dest => dest.AnneeAcademiqueId, src => src.AnneeAcademiqueId)
            .Map(dest => dest.AnneeAcademiqueLibelle, src => src.AnneeAcademique.Libelle)
            .Map(dest => dest.Statut, src => src.Statut)
            .Map(dest => dest.DateModification, src => src.DateModification);
    }
}

namespace erp_univ_back.Features.Inscription.GetSituationFinanciere;

using erp_univ_back.Features.Paiement.EncaisserGuichet;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

public interface IGetSituationFinanciereService
{
    Task<SituationFinanciereEtudiantResponse> ExecuteAsync(string matricule, CancellationToken ct = default);
}

public class GetSituationFinanciereService(ErpUnivDbContext db) : IGetSituationFinanciereService
{
    public async Task<SituationFinanciereEtudiantResponse> ExecuteAsync(string matricule, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(matricule))
        {
            throw new BadRequestException("Le matricule de l'étudiant est obligatoire.");
        }

        var matriculeNormalise = matricule.Trim().ToUpperInvariant();

        var etudiant = await db.Etudiants
            .FirstOrDefaultAsync(e => e.Matricule == matriculeNormalise, ct)
            ?? throw new NotFoundException($"L'étudiant avec le matricule '{matricule.Trim()}' n'existe pas.");

        var inscription = await db.Inscriptions
            .Include(i => i.Etudiant)
            .Include(i => i.Echeances)
            .Include(i => i.AnneeAcademique)
            .Include(i => i.FiliereNiveau)
                .ThenInclude(fn => fn.Filiere)
            .Include(i => i.FiliereNiveau)
                .ThenInclude(fn => fn.Niveau)
            .Where(i => i.EtudiantId == etudiant.Id)
            .OrderByDescending(i => i.AnneeAcademique.EstActive)
            .ThenByDescending(i => i.DateCreation)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Aucune inscription trouvée pour l'étudiant avec le matricule '{matricule.Trim()}'.");

        var classeLibelle = !string.IsNullOrWhiteSpace(inscription.FiliereNiveau.LibelleAffiche)
            ? inscription.FiliereNiveau.LibelleAffiche
            : $"{inscription.FiliereNiveau.Niveau.Nom} {inscription.FiliereNiveau.Filiere.Nom}";

        var totalAttendu = inscription.Echeances.Sum(e => e.MontantAttendu);
        var totalPaye = inscription.Echeances.Sum(e => e.MontantDejaPaye);
        var resteTotal = totalAttendu - totalPaye;

        var nomComplet = string.IsNullOrWhiteSpace(etudiant.Prenom)
            ? etudiant.Nom
            : $"{etudiant.Nom} {etudiant.Prenom}".Trim();

        return new SituationFinanciereEtudiantResponse(
            InscriptionId: inscription.Id,
            EtudiantId: etudiant.Id,
            Matricule: etudiant.Matricule,
            NomComplet: nomComplet,
            ClasseLibelle: classeLibelle,
            AnneeAcademiqueLibelle: inscription.AnneeAcademique.Libelle,
            StatutInscription: inscription.Statut,
            TotalAttendu: totalAttendu,
            TotalPaye: totalPaye,
            ResteAPayerTotal: resteTotal,
            Echeances: inscription.Echeances
                .OrderBy(e => e.NumeroOrdre)
                .Select(e => new EcheanceVentileeResponse(
                    Id: e.Id,
                    NumeroOrdre: e.NumeroOrdre,
                    Libelle: e.Libelle,
                    MontantAttendu: e.MontantAttendu,
                    MontantDejaPaye: e.MontantDejaPaye,
                    ResteAPayer: e.MontantAttendu - e.MontantDejaPaye,
                    ExigePourValiderInscription: e.ExigePourValiderInscription,
                    Statut: e.Statut
                )).ToList()
        );
    }
}

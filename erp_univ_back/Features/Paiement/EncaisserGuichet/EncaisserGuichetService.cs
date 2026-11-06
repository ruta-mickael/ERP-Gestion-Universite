namespace erp_univ_back.Features.Paiement.EncaisserGuichet;

using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Paiement;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public interface IEncaisserGuichetService
{
    Task<EncaisserGuichetResponse> ExecuteAsync(EncaisserGuichetRequest request, CancellationToken ct = default);
    Task<SituationFinanciereEtudiantResponse> ObtenirSituationParMatriculeAsync(string matricule, CancellationToken ct = default);
}

public class EncaisserGuichetService(
    ErpUnivDbContext db,
    IValidator<EncaisserGuichetRequest> validator) : IEncaisserGuichetService
{
    public async Task<SituationFinanciereEtudiantResponse> ObtenirSituationParMatriculeAsync(string matricule, CancellationToken ct = default)
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

    public async Task<EncaisserGuichetResponse> ExecuteAsync(EncaisserGuichetRequest request, CancellationToken ct = default)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.Errors.First().ErrorMessage);
        }

        // 1. Résolution de l'inscription (soit par InscriptionId, soit par Matricule)
        InscriptionEntity inscription;

        if (request.InscriptionId.HasValue && request.InscriptionId.Value != Guid.Empty)
        {
            inscription = await db.Inscriptions
                .Include(i => i.Etudiant)
                .Include(i => i.Echeances)
                .FirstOrDefaultAsync(i => i.Id == request.InscriptionId.Value, ct)
                ?? throw new NotFoundException($"L'inscription avec l'identifiant '{request.InscriptionId.Value}' n'existe pas.");
        }
        else
        {
            var matriculeNormalise = request.Matricule!.Trim().ToUpperInvariant();

            var etudiant = await db.Etudiants
                .FirstOrDefaultAsync(e => e.Matricule == matriculeNormalise, ct)
                ?? throw new NotFoundException($"L'étudiant avec le matricule '{request.Matricule!.Trim()}' n'existe pas.");

            inscription = await db.Inscriptions
                .Include(i => i.Etudiant)
                .Include(i => i.Echeances)
                .Include(i => i.AnneeAcademique)
                .Where(i => i.EtudiantId == etudiant.Id)
                .OrderByDescending(i => i.AnneeAcademique.EstActive)
                .ThenByDescending(i => i.DateCreation)
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException($"Aucune inscription trouvée pour l'étudiant avec le matricule '{request.Matricule!.Trim()}'.");
        }

        if (inscription.Echeances.Count == 0)
        {
            throw new BadRequestException("Cette inscription ne possède aucun échéancier de paiement.");
        }

        // 2. Calcul de la dette restante totale
        var detteRestante = inscription.Echeances.Sum(e => e.MontantAttendu - e.MontantDejaPaye);
        if (detteRestante <= 0)
        {
            throw new BadRequestException("Toutes les échéances de cette inscription sont déjà intégralement soldées.");
        }

        if (request.Montant > detteRestante)
        {
            throw new BadRequestException($"Le montant versé ({request.Montant:F2} Ar) dépasse le reliquat total dû ({detteRestante:F2} Ar).");
        }

        // 3. Détermination de l'ordre de ventilation
        var tranchesAVentiler = new List<EcheanceEtudiantEntity>();

        if (request.EcheanceId.HasValue)
        {
            var cible = inscription.Echeances.FirstOrDefault(e => e.Id == request.EcheanceId.Value)
                ?? throw new NotFoundException($"L'échéance avec l'identifiant '{request.EcheanceId.Value}' n'appartient pas à cette inscription.");

            if (cible.Statut == StatutEcheance.Solde)
            {
                throw new BadRequestException($"L'échéance '{cible.Libelle}' est déjà intégralement soldée.");
            }

            tranchesAVentiler.Add(cible);
            tranchesAVentiler.AddRange(
                inscription.Echeances
                    .Where(e => e.Id != cible.Id && e.Statut != StatutEcheance.Solde)
                    .OrderBy(e => e.NumeroOrdre)
            );
        }
        else
        {
            tranchesAVentiler.AddRange(
                inscription.Echeances
                    .Where(e => e.Statut != StatutEcheance.Solde)
                    .OrderBy(e => e.NumeroOrdre)
            );
        }

        // 4. Ventilation du montant sur les tranches
        var montantRestant = request.Montant;
        foreach (var echeance in tranchesAVentiler)
        {
            var reliquatTranche = echeance.MontantAttendu - echeance.MontantDejaPaye;
            var aAllouer = Math.Min(montantRestant, reliquatTranche);

            echeance.MontantDejaPaye += aAllouer;
            montantRestant -= aAllouer;

            echeance.Statut = echeance.MontantDejaPaye >= echeance.MontantAttendu
                ? StatutEcheance.Solde
                : StatutEcheance.Partiel;

            if (montantRestant <= 0) break;
        }

        // 5. Bascule automatique de l'inscription à 'Validee' si toutes les tranches prérequises sont soldées
        var tranchesExigees = inscription.Echeances.Where(e => e.ExigePourValiderInscription).ToList();
        var toutesTranchesExigeesSoldees = tranchesExigees.Count > 0 && tranchesExigees.All(e => e.Statut == StatutEcheance.Solde);

        if (toutesTranchesExigeesSoldees && inscription.Statut == StatutInscription.EnAttentePaiement)
        {
            inscription.Statut = StatutInscription.Validee;
        }

        // 6. Enregistrement de la Transaction de Paiement (validée d'office au guichet)
        var transaction = new TransactionPaiementEntity
        {
            InscriptionId = inscription.Id,
            Inscription = inscription,
            EtudiantId = inscription.EtudiantId,
            Etudiant = inscription.Etudiant,
            Montant = request.Montant,
            MoyenPaiement = request.MoyenPaiement,
            NomBanque = request.NomBanque,
            ReferenceExterne = request.ReferenceExterne,
            DateOperationDeclaree = DateTime.UtcNow,
            StatutValidation = StatutValidationPaiement.Valide,
            DateValidation = DateTime.UtcNow
        };

        db.TransactionsPaiement.Add(transaction);
        await db.SaveChangesAsync(ct);

        // 7. Construction du retour
        var nomComplet = string.IsNullOrWhiteSpace(inscription.Etudiant.Prenom)
            ? inscription.Etudiant.Nom
            : $"{inscription.Etudiant.Nom} {inscription.Etudiant.Prenom}".Trim();

        var reliquatFinal = inscription.Echeances.Sum(e => e.MontantAttendu - e.MontantDejaPaye);

        return new EncaisserGuichetResponse(
            TransactionId: transaction.Id,
            InscriptionId: inscription.Id,
            EtudiantId: inscription.EtudiantId,
            EtudiantMatricule: inscription.Etudiant.Matricule,
            EtudiantNomComplet: nomComplet,
            MontantVerse: request.Montant,
            MoyenPaiement: request.MoyenPaiement,
            StatutInscription: inscription.Statut,
            ResteAPayerTotal: reliquatFinal,
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
                )).ToList(),
            DatePaiement: transaction.DateOperationDeclaree
        );
    }
}

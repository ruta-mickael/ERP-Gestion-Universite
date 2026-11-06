namespace erp_univ_back.Features.Paiement.ValiderTransaction;

using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Paiement;
using erp_univ_back.Features.Paiement.EncaisserGuichet;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public interface IValiderTransactionService
{
    Task<ValiderTransactionResponse> ExecuteAsync(Guid transactionId, ValiderTransactionRequest request, CancellationToken ct = default);
}

public class ValiderTransactionService(
    ErpUnivDbContext db,
    IValidator<ValiderTransactionRequest> validator) : IValiderTransactionService
{
    public async Task<ValiderTransactionResponse> ExecuteAsync(Guid transactionId, ValiderTransactionRequest request, CancellationToken ct = default)
    {
        // 1. Validation de la requête
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.Errors.First().ErrorMessage);
        }

        // 2. Recherche de la transaction avec son inscription, étudiant et échéancier
        var transaction = await db.TransactionsPaiement
            .Include(t => t.Etudiant)
            .Include(t => t.Inscription)
                .ThenInclude(i => i.Echeances)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct)
            ?? throw new NotFoundException($"La transaction de paiement avec l'identifiant '{transactionId}' n'existe pas.");

        // 3. Vérification que la transaction est bien en attente de vérification
        if (transaction.StatutValidation != StatutValidationPaiement.EnAttenteVerification)
        {
            throw new BadRequestException($"Cette transaction a déjà été traitée (Statut actuel : {transaction.StatutValidation}).");
        }

        var inscription = transaction.Inscription;
        var now = DateTime.UtcNow;

        // 4. Cas : Rejet de la transaction par le comptable
        if (!request.EstValide)
        {
            transaction.StatutValidation = StatutValidationPaiement.Rejete;
            transaction.MotifRejet = request.MotifRejet?.Trim();
            transaction.DateValidation = now;

            await db.SaveChangesAsync(ct);

            var nomCompletRejet = string.IsNullOrWhiteSpace(transaction.Etudiant.Prenom)
                ? transaction.Etudiant.Nom
                : $"{transaction.Etudiant.Nom} {transaction.Etudiant.Prenom}".Trim();

            var resteAPayerRejet = inscription.Echeances.Sum(e => e.MontantAttendu - e.MontantDejaPaye);

            return new ValiderTransactionResponse(
                TransactionId: transaction.Id,
                InscriptionId: inscription.Id,
                EtudiantId: transaction.EtudiantId,
                EtudiantMatricule: transaction.Etudiant.Matricule,
                EtudiantNomComplet: nomCompletRejet,
                Montant: transaction.Montant,
                MoyenPaiement: transaction.MoyenPaiement,
                NomBanque: transaction.NomBanque,
                ReferenceExterne: transaction.ReferenceExterne,
                StatutValidation: transaction.StatutValidation,
                DateOperationDeclaree: transaction.DateOperationDeclaree,
                DateValidation: transaction.DateValidation,
                MotifRejet: transaction.MotifRejet,
                StatutInscription: inscription.Statut,
                ResteAPayerTotal: resteAPayerRejet,
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
                Message: $"Transaction rejetée. Motif : {transaction.MotifRejet}"
            );
        }

        // 5. Cas : Validation et lettrage sur les échéances
        if (inscription.Echeances.Count == 0)
        {
            throw new BadRequestException("Cette inscription ne possède aucun échéancier de paiement.");
        }

        var detteRestante = inscription.Echeances.Sum(e => e.MontantAttendu - e.MontantDejaPaye);
        if (detteRestante <= 0)
        {
            throw new BadRequestException("Toutes les échéances de cette inscription sont déjà intégralement soldées.");
        }

        // Détermination de l'ordre de ventilation des tranches
        var tranchesAVentiler = new List<EcheanceEtudiantEntity>();

        if (request.EcheanceCibleId.HasValue)
        {
            var cible = inscription.Echeances.FirstOrDefault(e => e.Id == request.EcheanceCibleId.Value)
                ?? throw new NotFoundException($"L'échéance avec l'identifiant '{request.EcheanceCibleId.Value}' n'appartient pas à cette inscription.");

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

        // Ventilation du montant validé sur les échéances
        var montantRestant = transaction.Montant;
        foreach (var echeance in tranchesAVentiler)
        {
            var reliquat = echeance.MontantAttendu - echeance.MontantDejaPaye;
            var aAllouer = Math.Min(montantRestant, reliquat);

            echeance.MontantDejaPaye += aAllouer;
            montantRestant -= aAllouer;

            echeance.Statut = echeance.MontantDejaPaye >= echeance.MontantAttendu
                ? StatutEcheance.Solde
                : StatutEcheance.Partiel;

            if (montantRestant <= 0) break;
        }

        // Bascule automatique de l'inscription à 'Validee' si toutes les tranches prérequises sont soldées
        var tranchesExigees = inscription.Echeances.Where(e => e.ExigePourValiderInscription).ToList();
        var toutesTranchesExigeesSoldees = tranchesExigees.Count > 0 && tranchesExigees.All(e => e.Statut == StatutEcheance.Solde);

        if (toutesTranchesExigeesSoldees && inscription.Statut == StatutInscription.EnAttentePaiement)
        {
            inscription.Statut = StatutInscription.Validee;
        }

        // Mise à jour de la transaction
        transaction.StatutValidation = StatutValidationPaiement.Valide;
        transaction.DateValidation = now;
        transaction.MotifRejet = null;

        await db.SaveChangesAsync(ct);

        // 6. Construction du retour
        var nomComplet = string.IsNullOrWhiteSpace(transaction.Etudiant.Prenom)
            ? transaction.Etudiant.Nom
            : $"{transaction.Etudiant.Nom} {transaction.Etudiant.Prenom}".Trim();

        var reliquatTotalFinal = inscription.Echeances.Sum(e => e.MontantAttendu - e.MontantDejaPaye);

        return new ValiderTransactionResponse(
            TransactionId: transaction.Id,
            InscriptionId: inscription.Id,
            EtudiantId: transaction.EtudiantId,
            EtudiantMatricule: transaction.Etudiant.Matricule,
            EtudiantNomComplet: nomComplet,
            Montant: transaction.Montant,
            MoyenPaiement: transaction.MoyenPaiement,
            NomBanque: transaction.NomBanque,
            ReferenceExterne: transaction.ReferenceExterne,
            StatutValidation: transaction.StatutValidation,
            DateOperationDeclaree: transaction.DateOperationDeclaree,
            DateValidation: transaction.DateValidation,
            MotifRejet: null,
            StatutInscription: inscription.Statut,
            ResteAPayerTotal: reliquatTotalFinal,
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
            Message: "Transaction vérifiée et validée avec succès. Les échéances ont été créditées."
        );
    }
}

namespace erp_univ_back.Features.Paiement.SoumettreMobileMoney;

using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Paiement;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public interface ISoumettreMobileMoneyService
{
    Task<SoumettreMobileMoneyResponse> ExecuteAsync(SoumettreMobileMoneyRequest request, CancellationToken ct = default);
    Task<SoumettreMobileMoneyResponse> ObtenirParIdAsync(Guid id, CancellationToken ct = default);
}

public class SoumettreMobileMoneyService(
    ErpUnivDbContext db,
    IValidator<SoumettreMobileMoneyRequest> validator) : ISoumettreMobileMoneyService
{
    public async Task<SoumettreMobileMoneyResponse> ExecuteAsync(SoumettreMobileMoneyRequest request, CancellationToken ct = default)
    {
        // 1. Validation FluentValidation
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.Errors.First().ErrorMessage);
        }

        // 2. Résolution de l'inscription (par InscriptionId ou Matricule)
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

        // 3. Vérification de l'échéancier et de la dette restante
        if (inscription.Echeances.Count == 0)
        {
            throw new BadRequestException("Cette inscription ne possède aucun échéancier de paiement.");
        }

        var detteRestante = inscription.Echeances.Sum(e => e.MontantAttendu - e.MontantDejaPaye);
        if (detteRestante <= 0)
        {
            throw new BadRequestException("Toutes les échéances de cette inscription sont déjà intégralement soldées.");
        }

        if (request.Montant > detteRestante)
        {
            throw new BadRequestException($"Le montant déclaré ({request.Montant:F2} Ar) dépasse le reliquat total dû ({detteRestante:F2} Ar).");
        }

        // 4. Contrôle Anti-fraude : unicité de la référence pour le moyen de paiement et la banque
        var referenceTrimmed = request.ReferenceExterne.Trim();
        var nomBanqueTrimmed = string.IsNullOrWhiteSpace(request.NomBanque) ? null : request.NomBanque.Trim();

        var refExiste = await db.TransactionsPaiement
            .AnyAsync(t => t.MoyenPaiement == request.MoyenPaiement
                        && t.ReferenceExterne == referenceTrimmed
                        && t.NomBanque == nomBanqueTrimmed, ct);

        if (refExiste)
        {
            throw new ConflictException($"La référence de paiement '{referenceTrimmed}' a déjà été enregistrée pour ce moyen de paiement.");
        }

        // 5. Création de la transaction en attente de vérification
        var dateDeclaree = request.DateOperationDeclaree == default
            ? DateTime.UtcNow
            : request.DateOperationDeclaree.ToUniversalTime();

        var transaction = new TransactionPaiementEntity
        {
            InscriptionId = inscription.Id,
            EtudiantId = inscription.EtudiantId,
            Montant = request.Montant,
            MoyenPaiement = request.MoyenPaiement,
            NomBanque = nomBanqueTrimmed,
            ReferenceExterne = referenceTrimmed,
            DateOperationDeclaree = dateDeclaree,
            PreuveImageUrl = string.IsNullOrWhiteSpace(request.PreuveImageUrl) ? null : request.PreuveImageUrl.Trim(),
            StatutValidation = StatutValidationPaiement.EnAttenteVerification,
            DateValidation = null,
            VerifieParUtilisateurId = null,
            MotifRejet = null
        };

        db.TransactionsPaiement.Add(transaction);
        await db.SaveChangesAsync(ct);

        // 6. Construction de la réponse
        var nomComplet = string.IsNullOrWhiteSpace(inscription.Etudiant.Prenom)
            ? inscription.Etudiant.Nom
            : $"{inscription.Etudiant.Nom} {inscription.Etudiant.Prenom}".Trim();

        return new SoumettreMobileMoneyResponse(
            TransactionId: transaction.Id,
            InscriptionId: inscription.Id,
            EtudiantId: inscription.EtudiantId,
            EtudiantMatricule: inscription.Etudiant.Matricule,
            EtudiantNomComplet: nomComplet,
            Montant: transaction.Montant,
            MoyenPaiement: transaction.MoyenPaiement,
            NomBanque: transaction.NomBanque,
            ReferenceExterne: transaction.ReferenceExterne!,
            DateOperationDeclaree: transaction.DateOperationDeclaree,
            StatutValidation: transaction.StatutValidation,
            PreuveImageUrl: transaction.PreuveImageUrl,
            DateSoumission: transaction.DateCreation,
            Message: "Paiement déclaré avec succès. Il est en attente de vérification par le service de comptabilité."
        );
    }

    public async Task<SoumettreMobileMoneyResponse> ObtenirParIdAsync(Guid id, CancellationToken ct = default)
    {
        var transaction = await db.TransactionsPaiement
            .Include(t => t.Etudiant)
            .Include(t => t.Inscription)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException($"La transaction avec l'identifiant '{id}' n'existe pas.");

        var nomComplet = string.IsNullOrWhiteSpace(transaction.Etudiant.Prenom)
            ? transaction.Etudiant.Nom
            : $"{transaction.Etudiant.Nom} {transaction.Etudiant.Prenom}".Trim();

        var message = transaction.StatutValidation switch
        {
            StatutValidationPaiement.EnAttenteVerification => "Transaction en attente de vérification par la comptabilité.",
            StatutValidationPaiement.Valide => "Transaction vérifiée et validée.",
            StatutValidationPaiement.Rejete => $"Transaction rejetée. Motif : {transaction.MotifRejet}",
            _ => "Statut inconnu"
        };

        return new SoumettreMobileMoneyResponse(
            TransactionId: transaction.Id,
            InscriptionId: transaction.InscriptionId,
            EtudiantId: transaction.EtudiantId,
            EtudiantMatricule: transaction.Etudiant.Matricule,
            EtudiantNomComplet: nomComplet,
            Montant: transaction.Montant,
            MoyenPaiement: transaction.MoyenPaiement,
            NomBanque: transaction.NomBanque,
            ReferenceExterne: transaction.ReferenceExterne ?? string.Empty,
            DateOperationDeclaree: transaction.DateOperationDeclaree,
            StatutValidation: transaction.StatutValidation,
            PreuveImageUrl: transaction.PreuveImageUrl,
            DateSoumission: transaction.DateCreation,
            Message: message
        );
    }
}

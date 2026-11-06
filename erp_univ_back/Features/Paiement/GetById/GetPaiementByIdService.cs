namespace erp_univ_back.Features.Paiement.GetById;

using erp_univ_back.Features.Paiement;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

public interface IGetPaiementByIdService
{
    Task<GetPaiementByIdResponse> ExecuteAsync(Guid id, CancellationToken ct = default);
}

public class GetPaiementByIdService(ErpUnivDbContext db) : IGetPaiementByIdService
{
    public async Task<GetPaiementByIdResponse> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var transaction = await db.TransactionsPaiement
            .Include(t => t.Etudiant)
            .Include(t => t.Inscription)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException($"La transaction de paiement avec l'identifiant '{id}' n'existe pas.");

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

        return new GetPaiementByIdResponse(
            Id: transaction.Id,
            InscriptionId: transaction.InscriptionId,
            EtudiantId: transaction.EtudiantId,
            EtudiantMatricule: transaction.Etudiant.Matricule,
            EtudiantNomComplet: nomComplet,
            Montant: transaction.Montant,
            MoyenPaiement: transaction.MoyenPaiement,
            NomBanque: transaction.NomBanque,
            ReferenceExterne: transaction.ReferenceExterne,
            DateOperationDeclaree: transaction.DateOperationDeclaree,
            StatutValidation: transaction.StatutValidation,
            PreuveImageUrl: transaction.PreuveImageUrl,
            DateValidation: transaction.DateValidation,
            MotifRejet: transaction.MotifRejet,
            DateCreation: transaction.DateCreation,
            Message: message
        );
    }
}

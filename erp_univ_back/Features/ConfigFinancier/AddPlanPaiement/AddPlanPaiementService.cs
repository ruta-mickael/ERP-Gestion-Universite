namespace erp_univ_back.Features.ConfigFinancier.AddPlanPaiement;

using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public interface IAddPlanPaiementService
{
    Task<AddPlanPaiementResponse> ExecuteAsync(AddPlanPaiementRequest request, CancellationToken ct = default);
}

public class AddPlanPaiementService(
    ErpUnivDbContext db,
    IValidator<AddPlanPaiementRequest> validator) : IAddPlanPaiementService
{
    public async Task<AddPlanPaiementResponse> ExecuteAsync(AddPlanPaiementRequest request, CancellationToken ct = default)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.Errors.First().ErrorMessage);
        }

        // 1. Vérification de l'existence du frais de catalogue (filtré par tenant)
        var fraisCatalogue = await db.FraisCatalogues
            .FirstOrDefaultAsync(f => f.Id == request.FraisCatalogueId, ct)
            ?? throw new NotFoundException($"Le frais de catalogue avec l'identifiant '{request.FraisCatalogueId}' n'existe pas.");

        // 2. Règle financière stricte : la somme des tranches DOIT égaler le montant total du catalogue
        var sommeTranches = request.Tranches.Sum(t => t.Montant);
        if (sommeTranches != fraisCatalogue.MontantTotal)
        {
            throw new BadRequestException(
                $"La somme des tranches ({sommeTranches:F2} Ar) ne correspond pas au montant total du frais catalogue ({fraisCatalogue.MontantTotal:F2} Ar)."
            );
        }

        // 3. Vérification des doublons de nom pour ce même frais catalogue
        var nomNormalise = request.Nom.Trim().ToLowerInvariant();
        var existe = await db.PlansPaiement.AnyAsync(p =>
            p.FraisCatalogueId == request.FraisCatalogueId &&
            p.Nom.ToLower() == nomNormalise,
            ct);

        if (existe)
        {
            throw new ConflictException($"Un plan de paiement nommé '{request.Nom.Trim()}' existe déjà pour ce frais de catalogue.");
        }

        // 4. Création du Plan de Paiement
        var plan = new PlanPaiementEntity
        {
            FraisCatalogueId = request.FraisCatalogueId,
            FraisCatalogue = fraisCatalogue,
            Nom = request.Nom.Trim(),
            NombreTranches = request.Tranches.Count,
            EstActif = true
        };

        // 5. Création des tranches modèles associées (triées par numéro d'ordre)
        foreach (var t in request.Tranches.OrderBy(t => t.NumeroOrdre))
        {
            var tranche = new ModeleTrancheEntity
            {
                PlanPaiementId = plan.Id,
                PlanPaiement = plan,
                NumeroOrdre = t.NumeroOrdre,
                Libelle = t.Libelle.Trim(),
                Montant = t.Montant,
                DateLimite = t.DateLimite.ToUniversalTime(),
                ExigePourValiderInscription = t.ExigePourValiderInscription
            };
            plan.ModelesTranches.Add(tranche);
        }

        db.PlansPaiement.Add(plan);
        await db.SaveChangesAsync(ct);

        return new AddPlanPaiementResponse(
            Id: plan.Id,
            FraisCatalogueId: plan.FraisCatalogueId,
            FraisCatalogueLibelle: fraisCatalogue.Libelle,
            Nom: plan.Nom,
            NombreTranches: plan.NombreTranches,
            EstActif: plan.EstActif,
            MontantTotal: fraisCatalogue.MontantTotal,
            Tranches: plan.ModelesTranches
                .OrderBy(t => t.NumeroOrdre)
                .Select(t => new ModeleTrancheItemResponse(
                    Id: t.Id,
                    NumeroOrdre: t.NumeroOrdre,
                    Libelle: t.Libelle,
                    Montant: t.Montant,
                    DateLimite: t.DateLimite,
                    ExigePourValiderInscription: t.ExigePourValiderInscription
                )).ToList(),
            DateCreation: plan.DateCreation
        );
    }
}


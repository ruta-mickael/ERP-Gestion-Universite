namespace erp_univ_back.Features.ConfigFinancier.AddPlanPaiement;

using FluentValidation;

public class AddPlanPaiementValidator : AbstractValidator<AddPlanPaiementRequest>
{
    public AddPlanPaiementValidator()
    {
        RuleFor(x => x.FraisCatalogueId)
            .NotEmpty().WithMessage("L'identifiant du frais de catalogue est obligatoire.");

        RuleFor(x => x.Nom)
            .NotEmpty().WithMessage("Le nom du plan de paiement est obligatoire.")
            .MaximumLength(100).WithMessage("Le nom ne doit pas dépasser 100 caractères.");

        RuleFor(x => x.Tranches)
            .NotEmpty().WithMessage("Le plan de paiement doit comporter au moins une tranche.")
            .Must(t => t != null && t.Count > 0)
            .WithMessage("Le plan de paiement doit comporter au moins une tranche.")
            .Must(tranches => tranches != null && tranches.Select(t => t.NumeroOrdre).Distinct().Count() == tranches.Count)
            .WithMessage("Chaque tranche doit avoir un numéro d'ordre unique.")
            .Must(tranches => tranches != null && tranches.Any(t => t.ExigePourValiderInscription))
            .WithMessage("Au moins une tranche doit être exigée pour valider l'inscription.");

        RuleForEach(x => x.Tranches).SetValidator(new ModeleTrancheItemValidator());
    }
}

public class ModeleTrancheItemValidator : AbstractValidator<ModeleTrancheItemRequest>
{
    public ModeleTrancheItemValidator()
    {
        RuleFor(x => x.NumeroOrdre)
            .GreaterThan(0).WithMessage("Le numéro d'ordre de la tranche doit être supérieur à zéro.");

        RuleFor(x => x.Libelle)
            .NotEmpty().WithMessage("Le libellé de la tranche est obligatoire.")
            .MaximumLength(150).WithMessage("Le libellé de la tranche ne doit pas dépasser 150 caractères.");

        RuleFor(x => x.Montant)
            .GreaterThan(0).WithMessage("Le montant de la tranche doit être supérieur à zéro.");

        RuleFor(x => x.DateLimite)
            .NotEmpty().WithMessage("La date limite de la tranche est obligatoire.");
    }
}


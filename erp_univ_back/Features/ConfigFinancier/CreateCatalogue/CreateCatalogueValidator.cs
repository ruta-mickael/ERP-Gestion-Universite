namespace erp_univ_back.Features.ConfigFinancier.CreateCatalogue;

using FluentValidation;

public class CreateCatalogueValidator : AbstractValidator<CreateCatalogueRequest>
{
    public CreateCatalogueValidator()
    {
        RuleFor(x => x.AnneeAcademiqueId)
            .NotEmpty().WithMessage("L'année académique est obligatoire.");

        RuleFor(x => x.Libelle)
            .NotEmpty().WithMessage("Le libellé du frais est obligatoire.")
            .MaximumLength(150).WithMessage("Le libellé ne doit pas dépasser 150 caractères.");

        RuleFor(x => x.MontantTotal)
            .GreaterThan(0).WithMessage("Le montant total doit être supérieur à zéro.");

        RuleFor(x => x.Categorie)
            .IsInEnum().WithMessage("La catégorie de frais spécifiée est invalide.");

        RuleFor(x => x.MoyensPaiementAutorises)
            .Must(m => m != MoyenPaiement.Aucun)
            .WithMessage("Au moins un moyen de paiement doit être autorisé.");

        RuleFor(x => x.CycleId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("L'identifiant du cycle ne doit pas être vide.");

        RuleFor(x => x.FiliereId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("L'identifiant de la filière ne doit pas être vide.");

        RuleFor(x => x.NiveauId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("L'identifiant du niveau ne doit pas être vide.");
    }
}

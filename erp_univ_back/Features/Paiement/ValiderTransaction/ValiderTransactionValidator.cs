namespace erp_univ_back.Features.Paiement.ValiderTransaction;

using FluentValidation;

public class ValiderTransactionValidator : AbstractValidator<ValiderTransactionRequest>
{
    public ValiderTransactionValidator()
    {
        RuleFor(x => x.MotifRejet)
            .NotEmpty()
            .When(x => !x.EstValide)
            .WithMessage("Le motif du rejet est obligatoire lorsque la transaction est rejetée.")
            .MaximumLength(300)
            .WithMessage("Le motif du rejet ne doit pas dépasser 300 caractères.");
    }
}

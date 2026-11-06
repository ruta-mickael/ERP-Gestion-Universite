namespace erp_univ_back.Features.Paiement.SoumettreMobileMoney;

using erp_univ_back.Features.ConfigFinancier;
using FluentValidation;

public class SoumettreMobileMoneyValidator : AbstractValidator<SoumettreMobileMoneyRequest>
{
    public SoumettreMobileMoneyValidator()
    {
        RuleFor(x => x)
            .Must(x => (x.InscriptionId.HasValue && x.InscriptionId.Value != Guid.Empty) || !string.IsNullOrWhiteSpace(x.Matricule))
            .WithMessage("Le matricule de l'étudiant ou l'identifiant de l'inscription est obligatoire.");

        RuleFor(x => x.Montant)
            .GreaterThan(0)
            .WithMessage("Le montant déclaré doit être supérieur à zéro.");

        RuleFor(x => x.MoyenPaiement)
            .Must(m => m != MoyenPaiement.Aucun && m != MoyenPaiement.Especes)
            .WithMessage("Le moyen de paiement déclaré à distance ne peut pas être 'Aucun' ou 'Especes' (l'espèce se règle uniquement au guichet).");

        RuleFor(x => x.ReferenceExterne)
            .NotEmpty()
            .WithMessage("La référence de la transaction (ex: code de transaction SMS ou numéro de bordereau) est obligatoire.")
            .MaximumLength(100)
            .WithMessage("La référence ne doit pas dépasser 100 caractères.");

        RuleFor(x => x.NomBanque)
            .MaximumLength(50)
            .WithMessage("Le nom de la banque ne doit pas dépasser 50 caractères.");

        RuleFor(x => x.PreuveImageUrl)
            .MaximumLength(500)
            .WithMessage("L'URL du reçu / capture d'écran ne doit pas dépasser 500 caractères.");

        RuleFor(x => x.DateOperationDeclaree)
            .NotEmpty()
            .WithMessage("La date de l'opération déclarée est obligatoire.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .WithMessage("La date de l'opération ne peut pas être située dans le futur.");
    }
}

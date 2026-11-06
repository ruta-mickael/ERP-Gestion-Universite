namespace erp_univ_back.Features.Paiement.EncaisserGuichet;

using erp_univ_back.Features.ConfigFinancier;
using FluentValidation;

public class EncaisserGuichetValidator : AbstractValidator<EncaisserGuichetRequest>
{
    public EncaisserGuichetValidator()
    {
        RuleFor(x => x)
            .Must(x => (x.InscriptionId.HasValue && x.InscriptionId.Value != Guid.Empty) || !string.IsNullOrWhiteSpace(x.Matricule))
            .WithMessage("Vous devez fournir soit le matricule de l'étudiant, soit l'identifiant de son inscription.");

        RuleFor(x => x.InscriptionId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("L'identifiant de l'inscription ne doit pas être vide.");

        RuleFor(x => x.Montant)
            .GreaterThan(0).WithMessage("Le montant à encaisser doit être supérieur à zéro.");

        RuleFor(x => x.MoyenPaiement)
            .Must(m => m != MoyenPaiement.Aucun)
            .WithMessage("Un moyen de paiement valide doit être spécifié.");

        RuleFor(x => x.EcheanceId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("L'identifiant de l'échéance ne doit pas être vide.");

        RuleFor(x => x.ReferenceExterne)
            .MaximumLength(100).WithMessage("La référence externe ne doit pas dépasser 100 caractères.");

        RuleFor(x => x.NomBanque)
            .MaximumLength(50).WithMessage("Le nom de la banque ne doit pas dépasser 50 caractères.");
    }
}

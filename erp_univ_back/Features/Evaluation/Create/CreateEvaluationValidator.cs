namespace erp_univ_back.Features.Evaluation.Create;

using FluentValidation;

public class CreateEvaluationValidator : AbstractValidator<CreateEvaluationRequest>
{
    public CreateEvaluationValidator()
    {
        RuleFor(x => x.MatiereId)
            .NotEmpty()
            .WithMessage("La matière est obligatoire.");

        RuleFor(x => x.FiliereNiveauId)
            .NotEmpty()
            .WithMessage("La classe (filière/niveau) est obligatoire.");

        RuleFor(x => x.AnneeAcademiqueId)
            .NotEmpty()
            .WithMessage("L'année académique est obligatoire.");

        RuleFor(x => x.DateEvaluation)
            .NotEmpty()
            .WithMessage("La date d'évaluation est obligatoire.");

        RuleFor(x => x.Bareme)
            .GreaterThan(0)
            .WithMessage("Le barème doit être strictement supérieur à 0.");

        RuleFor(x => x.Poids)
            .GreaterThan(0)
            .WithMessage("Le poids doit être strictement supérieur à 0.");

        RuleFor(x => x.Titre)
            .MaximumLength(150)
            .WithMessage("Le titre de l'évaluation ne peut pas dépasser 150 caractères.")
            .When(x => !string.IsNullOrEmpty(x.Titre));

        RuleFor(x => x.ProfesseurId)
            .Must(id => id == null || id != Guid.Empty)
            .WithMessage("L'identifiant du professeur est invalide.");
    }
}

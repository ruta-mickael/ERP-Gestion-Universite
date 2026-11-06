namespace erp_univ_back.Features.Evaluation.SaisirNotes;

using FluentValidation;

public class SaisirNotesValidator : AbstractValidator<SaisirNotesRequest>
{
    public SaisirNotesValidator()
    {
        RuleFor(x => x.Notes)
            .NotNull()
            .WithMessage("La liste des notes est obligatoire.")
            .Must(notes => notes != null && notes.Count > 0)
            .WithMessage("La liste des notes ne peut pas être vide.");

        RuleForEach(x => x.Notes).ChildRules(ligne =>
        {
            ligne.RuleFor(n => n.EtudiantId)
                .NotEmpty()
                .WithMessage("L'identifiant de l'étudiant est obligatoire.");

            ligne.RuleFor(n => n.Valeur)
                .GreaterThanOrEqualTo(0)
                .WithMessage("La valeur de la note ne peut pas être négative.")
                .When(n => n.Valeur.HasValue);

            ligne.RuleFor(n => n)
                .Must(n => n.EstAbsent || n.Valeur.HasValue)
                .WithMessage("Une valeur de note est obligatoire si l'étudiant n'est pas marqué absent.");

            ligne.RuleFor(n => n.Commentaire)
                .MaximumLength(250)
                .WithMessage("Le commentaire ne peut pas dépasser 250 caractères.")
                .When(n => !string.IsNullOrEmpty(n.Commentaire));
        });
    }
}

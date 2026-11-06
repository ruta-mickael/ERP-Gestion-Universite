using erp_univ_back.Features.Etudiant.Create;
using FluentValidation;

namespace erp_univ_back.Features.Inscription.Register;

public class RegisterInscriptionValidator : AbstractValidator<RegisterInscriptionRequest>
{
    public RegisterInscriptionValidator()
    {
        RuleFor(x => x.EtudiantInfos)
            .NotNull().WithMessage("Les informations sur l'étudiant sont obligatoires.")
            .SetValidator(new CreateEtudiantRequestValidator());

        RuleFor(x => x.IdAnneeAcademique)
            .NotEmpty().WithMessage("L'identifiant de l'année académique est obligatoire.");

        RuleFor(x => x.IdFiliereNiveau)
            .NotEmpty().WithMessage("L'identifiant de la classe (filière/niveau) est obligatoire.");

        RuleForEach(x => x.PlansPaiementIds)
            .Must(id => id != Guid.Empty)
            .When(x => x.PlansPaiementIds != null)
            .WithMessage("L'identifiant du plan de paiement ne peut pas être vide.");
    }
}

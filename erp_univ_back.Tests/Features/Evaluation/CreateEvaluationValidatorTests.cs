namespace erp_univ_back.Tests.Features.Evaluation;

using erp_univ_back.Features.Evaluation;
using erp_univ_back.Features.Evaluation.Create;
using FluentValidation.TestHelper;
using Xunit;

public class CreateEvaluationValidatorTests
{
    private readonly CreateEvaluationValidator _validator = new();

    [Fact]
    public void Validate_DoitReussir_QuandRequeteEstValide()
    {
        // Arrange
        var request = new CreateEvaluationRequest(
            MatiereId: Guid.NewGuid(),
            FiliereNiveauId: Guid.NewGuid(),
            AnneeAcademiqueId: Guid.NewGuid(),
            DateEvaluation: DateTime.UtcNow.AddDays(7),
            Titre: "Partiel de C#",
            TypeEvaluation: TypeEvaluation.ExamenPartiel,
            Session: SessionEvaluation.Normale,
            Bareme: 20.00m,
            Poids: 0.40m,
            ProfesseurId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DoitEchouer_QuandMatiereIdEstVide()
    {
        var request = new CreateEvaluationRequest(
            MatiereId: Guid.Empty,
            FiliereNiveauId: Guid.NewGuid(),
            AnneeAcademiqueId: Guid.NewGuid(),
            DateEvaluation: DateTime.UtcNow
        );

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MatiereId)
            .WithErrorMessage("La matière est obligatoire.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandFiliereNiveauIdEstVide()
    {
        var request = new CreateEvaluationRequest(
            MatiereId: Guid.NewGuid(),
            FiliereNiveauId: Guid.Empty,
            AnneeAcademiqueId: Guid.NewGuid(),
            DateEvaluation: DateTime.UtcNow
        );

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FiliereNiveauId)
            .WithErrorMessage("La classe (filière/niveau) est obligatoire.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandAnneeAcademiqueIdEstVide()
    {
        var request = new CreateEvaluationRequest(
            MatiereId: Guid.NewGuid(),
            FiliereNiveauId: Guid.NewGuid(),
            AnneeAcademiqueId: Guid.Empty,
            DateEvaluation: DateTime.UtcNow
        );

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.AnneeAcademiqueId)
            .WithErrorMessage("L'année académique est obligatoire.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandDateEvaluationEstVide()
    {
        var request = new CreateEvaluationRequest(
            MatiereId: Guid.NewGuid(),
            FiliereNiveauId: Guid.NewGuid(),
            AnneeAcademiqueId: Guid.NewGuid(),
            DateEvaluation: default
        );

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DateEvaluation)
            .WithErrorMessage("La date d'évaluation est obligatoire.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_DoitEchouer_QuandBaremeEstInferieurOuEgalAZero(decimal baremeInvalide)
    {
        var request = new CreateEvaluationRequest(
            MatiereId: Guid.NewGuid(),
            FiliereNiveauId: Guid.NewGuid(),
            AnneeAcademiqueId: Guid.NewGuid(),
            DateEvaluation: DateTime.UtcNow,
            Bareme: baremeInvalide
        );

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Bareme)
            .WithErrorMessage("Le barème doit être strictement supérieur à 0.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_DoitEchouer_QuandPoidsEstInferieurOuEgalAZero(decimal poidsInvalide)
    {
        var request = new CreateEvaluationRequest(
            MatiereId: Guid.NewGuid(),
            FiliereNiveauId: Guid.NewGuid(),
            AnneeAcademiqueId: Guid.NewGuid(),
            DateEvaluation: DateTime.UtcNow,
            Poids: poidsInvalide
        );

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Poids)
            .WithErrorMessage("Le poids doit être strictement supérieur à 0.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandTitreDepasse150Caracteres()
    {
        var request = new CreateEvaluationRequest(
            MatiereId: Guid.NewGuid(),
            FiliereNiveauId: Guid.NewGuid(),
            AnneeAcademiqueId: Guid.NewGuid(),
            DateEvaluation: DateTime.UtcNow,
            Titre: new string('A', 151)
        );

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Titre)
            .WithErrorMessage("Le titre de l'évaluation ne peut pas dépasser 150 caractères.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandProfesseurIdEstGuidEmpty()
    {
        var request = new CreateEvaluationRequest(
            MatiereId: Guid.NewGuid(),
            FiliereNiveauId: Guid.NewGuid(),
            AnneeAcademiqueId: Guid.NewGuid(),
            DateEvaluation: DateTime.UtcNow,
            ProfesseurId: Guid.Empty
        );

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ProfesseurId)
            .WithErrorMessage("L'identifiant du professeur est invalide.");
    }
}

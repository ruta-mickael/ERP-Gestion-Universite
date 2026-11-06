namespace erp_univ_back.Tests.Features.ConfigFinancier.AddPlanPaiement;

using erp_univ_back.Features.ConfigFinancier.AddPlanPaiement;
using FluentValidation.TestHelper;
using Xunit;

public class AddPlanPaiementValidatorTests
{
    private readonly AddPlanPaiementValidator _validator = new();

    [Fact]
    public void Validate_DoitReussir_QuandRequeteEstValide()
    {
        // Arrange
        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: Guid.NewGuid(),
            Nom: "Paiement Semestriel",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Tranche 1", 600000m, DateTime.UtcNow.AddDays(30), true),
                new ModeleTrancheItemRequest(2, "Tranche 2", 600000m, DateTime.UtcNow.AddDays(120), false)
            ]
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DoitEchouer_QuandFraisCatalogueIdEstVide()
    {
        // Arrange
        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: Guid.Empty,
            Nom: "Plan Test",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Tranche 1", 500000m, DateTime.UtcNow.AddDays(30), true)
            ]
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FraisCatalogueId)
            .WithErrorMessage("L'identifiant du frais de catalogue est obligatoire.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_DoitEchouer_QuandNomEstVide(string? nom)
    {
        // Arrange
        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: Guid.NewGuid(),
            Nom: nom!,
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Tranche 1", 500000m, DateTime.UtcNow.AddDays(30), true)
            ]
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Nom)
            .WithErrorMessage("Le nom du plan de paiement est obligatoire.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandTranchesEstVide()
    {
        // Arrange
        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: Guid.NewGuid(),
            Nom: "Plan Sans Tranche",
            Tranches: []
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tranches)
            .WithErrorMessage("Le plan de paiement doit comporter au moins une tranche.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandTranchesOntNumerosOrdreDoublons()
    {
        // Arrange
        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: Guid.NewGuid(),
            Nom: "Plan Ordre Doublon",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Tranche 1", 300000m, DateTime.UtcNow.AddDays(30), true),
                new ModeleTrancheItemRequest(1, "Tranche 2", 300000m, DateTime.UtcNow.AddDays(60), false)
            ]
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tranches)
            .WithErrorMessage("Chaque tranche doit avoir un numéro d'ordre unique.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandAucuneTrancheExigeePourInscription()
    {
        // Arrange
        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: Guid.NewGuid(),
            Nom: "Plan Sans Tranche Exigée",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Tranche 1", 300000m, DateTime.UtcNow.AddDays(30), false),
                new ModeleTrancheItemRequest(2, "Tranche 2", 300000m, DateTime.UtcNow.AddDays(60), false)
            ]
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tranches)
            .WithErrorMessage("Au moins une tranche doit être exigée pour valider l'inscription.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_DoitEchouer_QuandMontantTrancheEstNegatifOuNul(decimal montant)
    {
        // Arrange
        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: Guid.NewGuid(),
            Nom: "Plan Montant Invalide",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Tranche 1", montant, DateTime.UtcNow.AddDays(30), true)
            ]
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Tranches[0].Montant")
            .WithErrorMessage("Le montant de la tranche doit être supérieur à zéro.");
    }
}


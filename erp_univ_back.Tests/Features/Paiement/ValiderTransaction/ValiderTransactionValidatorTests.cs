namespace erp_univ_back.Tests.Features.Paiement.ValiderTransaction;

using erp_univ_back.Features.Paiement.ValiderTransaction;
using FluentValidation.TestHelper;
using Xunit;

public class ValiderTransactionValidatorTests
{
    private readonly ValiderTransactionValidator _validator = new();

    [Fact]
    public void Validate_DoitReussir_QuandValidationSansMotif()
    {
        // Arrange
        var request = new ValiderTransactionRequest(
            EstValide: true,
            MotifRejet: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_DoitEchouer_QuandRejetSansMotif(string? motif)
    {
        // Arrange
        var request = new ValiderTransactionRequest(
            EstValide: false,
            MotifRejet: motif
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MotifRejet)
            .WithErrorMessage("Le motif du rejet est obligatoire lorsque la transaction est rejetée.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandMotifDepasse300Caracteres()
    {
        // Arrange
        var request = new ValiderTransactionRequest(
            EstValide: false,
            MotifRejet: new string('R', 301)
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MotifRejet)
            .WithErrorMessage("Le motif du rejet ne doit pas dépasser 300 caractères.");
    }

    [Fact]
    public void Validate_DoitReussir_QuandRejetAvecMotifValide()
    {
        // Arrange
        var request = new ValiderTransactionRequest(
            EstValide: false,
            MotifRejet: "Capture d'écran illisible, veuillez renvoyer un reçu net."
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

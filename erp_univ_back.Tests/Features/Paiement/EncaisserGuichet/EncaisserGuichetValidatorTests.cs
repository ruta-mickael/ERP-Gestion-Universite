namespace erp_univ_back.Tests.Features.Paiement.EncaisserGuichet;

using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Paiement.EncaisserGuichet;
using FluentValidation.TestHelper;
using Xunit;

public class EncaisserGuichetValidatorTests
{
    private readonly EncaisserGuichetValidator _validator = new();

    [Fact]
    public void Validate_DoitReussir_QuandInscriptionIdEstFourni()
    {
        // Arrange
        var request = new EncaisserGuichetRequest(
            InscriptionId: Guid.NewGuid(),
            Montant: 400000m,
            MoyenPaiement: MoyenPaiement.Especes
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DoitReussir_QuandMatriculeEstFourni()
    {
        // Arrange
        var request = new EncaisserGuichetRequest(
            Matricule: "ETU-2025-0042",
            Montant: 400000m,
            MoyenPaiement: MoyenPaiement.Especes
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DoitEchouer_QuandNiInscriptionIdNiMatriculeFournis()
    {
        // Arrange
        var request = new EncaisserGuichetRequest(
            Montant: 400000m,
            MoyenPaiement: MoyenPaiement.Especes
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Vous devez fournir soit le matricule de l'étudiant, soit l'identifiant de son inscription.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Validate_DoitEchouer_QuandMontantEstInferieurOuEgalAZero(decimal montant)
    {
        // Arrange
        var request = new EncaisserGuichetRequest(
            Matricule: "ETU-2025-0042",
            Montant: montant
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Montant)
            .WithErrorMessage("Le montant à encaisser doit être supérieur à zéro.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandMoyenPaiementEstAucun()
    {
        // Arrange
        var request = new EncaisserGuichetRequest(
            Matricule: "ETU-2025-0042",
            Montant: 400000m,
            MoyenPaiement: MoyenPaiement.Aucun
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MoyenPaiement)
            .WithErrorMessage("Un moyen de paiement valide doit être spécifié.");
    }
}

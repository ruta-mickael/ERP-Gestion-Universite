namespace erp_univ_back.Tests.Features.Paiement.SoumettreMobileMoney;

using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Paiement.SoumettreMobileMoney;
using FluentValidation.TestHelper;
using Xunit;

public class SoumettreMobileMoneyValidatorTests
{
    private readonly SoumettreMobileMoneyValidator _validator = new();

    [Fact]
    public void Validate_DoitReussir_QuandMatriculeEtDonneesValides()
    {
        // Arrange
        var request = new SoumettreMobileMoneyRequest(
            Matricule: "ETU-2025-0042",
            Montant: 250000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: "MP251106.1430.B89211",
            DateOperationDeclaree: DateTime.UtcNow.AddMinutes(-10)
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DoitReussir_QuandInscriptionIdEtDonneesValides()
    {
        // Arrange
        var request = new SoumettreMobileMoneyRequest(
            InscriptionId: Guid.NewGuid(),
            Montant: 500000m,
            MoyenPaiement: MoyenPaiement.OrangeMoney,
            ReferenceExterne: "OM99887766",
            DateOperationDeclaree: DateTime.UtcNow.AddHours(-1)
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DoitEchouer_QuandNiInscriptionIdNiMatricule()
    {
        // Arrange
        var request = new SoumettreMobileMoneyRequest(
            Montant: 250000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: "MP123456",
            DateOperationDeclaree: DateTime.UtcNow
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Le matricule de l'étudiant ou l'identifiant de l'inscription est obligatoire.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Validate_DoitEchouer_QuandMontantInferieurOuEgalAZero(decimal montant)
    {
        // Arrange
        var request = new SoumettreMobileMoneyRequest(
            Matricule: "ETU-2025-0042",
            Montant: montant,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: "MP123456",
            DateOperationDeclaree: DateTime.UtcNow
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Montant)
            .WithErrorMessage("Le montant déclaré doit être supérieur à zéro.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandMoyenPaiementEstEspeces()
    {
        // Arrange
        var request = new SoumettreMobileMoneyRequest(
            Matricule: "ETU-2025-0042",
            Montant: 250000m,
            MoyenPaiement: MoyenPaiement.Especes,
            ReferenceExterne: "CASH123",
            DateOperationDeclaree: DateTime.UtcNow
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MoyenPaiement)
            .WithErrorMessage("Le moyen de paiement déclaré à distance ne peut pas être 'Aucun' ou 'Especes' (l'espèce se règle uniquement au guichet).");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandMoyenPaiementEstAucun()
    {
        // Arrange
        var request = new SoumettreMobileMoneyRequest(
            Matricule: "ETU-2025-0042",
            Montant: 250000m,
            MoyenPaiement: MoyenPaiement.Aucun,
            ReferenceExterne: "REF123",
            DateOperationDeclaree: DateTime.UtcNow
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MoyenPaiement);
    }

    [Theory]
    [InlineData(MoyenPaiement.MVola)]
    [InlineData(MoyenPaiement.OrangeMoney)]
    [InlineData(MoyenPaiement.AirtelMoney)]
    [InlineData(MoyenPaiement.VersementBancaire)]
    [InlineData(MoyenPaiement.VirementBancaire)]
    public void Validate_DoitReussir_PourMoyensPaiementsDistantsValides(MoyenPaiement moyen)
    {
        // Arrange
        var request = new SoumettreMobileMoneyRequest(
            Matricule: "ETU-2025-0042",
            Montant: 250000m,
            MoyenPaiement: moyen,
            ReferenceExterne: "REF123456",
            DateOperationDeclaree: DateTime.UtcNow,
            NomBanque: moyen is MoyenPaiement.VersementBancaire or MoyenPaiement.VirementBancaire ? "BNI Madagascar" : null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_DoitEchouer_QuandReferenceEstVide(string reference)
    {
        // Arrange
        var request = new SoumettreMobileMoneyRequest(
            Matricule: "ETU-2025-0042",
            Montant: 250000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: reference,
            DateOperationDeclaree: DateTime.UtcNow
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReferenceExterne);
    }

    [Fact]
    public void Validate_DoitEchouer_QuandReferenceDepasse100Caracteres()
    {
        // Arrange
        var request = new SoumettreMobileMoneyRequest(
            Matricule: "ETU-2025-0042",
            Montant: 250000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: new string('A', 101),
            DateOperationDeclaree: DateTime.UtcNow
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReferenceExterne)
            .WithErrorMessage("La référence ne doit pas dépasser 100 caractères.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandDateDeclareeDansLeFutur()
    {
        // Arrange
        var request = new SoumettreMobileMoneyRequest(
            Matricule: "ETU-2025-0042",
            Montant: 250000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: "MP123456",
            DateOperationDeclaree: DateTime.UtcNow.AddDays(3)
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateOperationDeclaree)
            .WithErrorMessage("La date de l'opération ne peut pas être située dans le futur.");
    }
}

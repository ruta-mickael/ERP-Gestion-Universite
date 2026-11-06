namespace erp_univ_back.Tests.Features.ConfigFinancier.CreateCatalogue;

using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.ConfigFinancier.CreateCatalogue;
using FluentValidation.TestHelper;
using Xunit;

public class CreateCatalogueValidatorTests
{
    private readonly CreateCatalogueValidator _validator = new();

    [Fact]
    public void Validate_DoitReussir_QuandRequeteEstValide()
    {
        // Arrange
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.NewGuid(),
            Categorie: CategorieFrais.FraisFormation,
            Libelle: "Écolage Licence 1",
            MontantTotal: 1200000.00m,
            MoyensPaiementAutorises: MoyenPaiement.Especes | MoyenPaiement.MVola,
            CycleId: Guid.NewGuid(),
            FiliereId: Guid.NewGuid(),
            NiveauId: Guid.NewGuid(),
            EstObligatoirePourDossier: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DoitReussir_QuandCiblageEstNull_FraisGlobal()
    {
        // Arrange - Frais global pour toute l'université
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.NewGuid(),
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "Droit d'inscription général",
            MontantTotal: 150000.00m,
            MoyensPaiementAutorises: MoyenPaiement.MobileMoney
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DoitEchouer_QuandAnneeAcademiqueIdEstVide()
    {
        // Arrange
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.Empty,
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "Frais Inscription",
            MontantTotal: 100000m,
            MoyensPaiementAutorises: MoyenPaiement.Especes
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnneeAcademiqueId)
            .WithErrorMessage("L'année académique est obligatoire.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_DoitEchouer_QuandLibelleEstVide(string? libelle)
    {
        // Arrange
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.NewGuid(),
            Categorie: CategorieFrais.DroitInscription,
            Libelle: libelle!,
            MontantTotal: 100000m,
            MoyensPaiementAutorises: MoyenPaiement.Especes
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Libelle)
            .WithErrorMessage("Le libellé du frais est obligatoire.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandLibelleDepasse150Caracteres()
    {
        // Arrange
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.NewGuid(),
            Categorie: CategorieFrais.DroitInscription,
            Libelle: new string('A', 151),
            MontantTotal: 100000m,
            MoyensPaiementAutorises: MoyenPaiement.Especes
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Libelle)
            .WithErrorMessage("Le libellé ne doit pas dépasser 150 caractères.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Validate_DoitEchouer_QuandMontantTotalEstInferieurOuEgalAZero(decimal montant)
    {
        // Arrange
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.NewGuid(),
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "Frais Inscription",
            MontantTotal: montant,
            MoyensPaiementAutorises: MoyenPaiement.Especes
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MontantTotal)
            .WithErrorMessage("Le montant total doit être supérieur à zéro.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandAucunMoyenPaiementAutorise()
    {
        // Arrange
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.NewGuid(),
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "Frais Inscription",
            MontantTotal: 100000m,
            MoyensPaiementAutorises: MoyenPaiement.Aucun
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MoyensPaiementAutorises)
            .WithErrorMessage("Au moins un moyen de paiement doit être autorisé.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandCycleIdEstEmptyGuid()
    {
        // Arrange
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.NewGuid(),
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "Frais Inscription",
            MontantTotal: 100000m,
            MoyensPaiementAutorises: MoyenPaiement.Especes,
            CycleId: Guid.Empty
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CycleId)
            .WithErrorMessage("L'identifiant du cycle ne doit pas être vide.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandFiliereIdEstEmptyGuid()
    {
        // Arrange
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.NewGuid(),
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "Frais Inscription",
            MontantTotal: 100000m,
            MoyensPaiementAutorises: MoyenPaiement.Especes,
            FiliereId: Guid.Empty
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FiliereId)
            .WithErrorMessage("L'identifiant de la filière ne doit pas être vide.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandNiveauIdEstEmptyGuid()
    {
        // Arrange
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.NewGuid(),
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "Frais Inscription",
            MontantTotal: 100000m,
            MoyensPaiementAutorises: MoyenPaiement.Especes,
            NiveauId: Guid.Empty
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NiveauId)
            .WithErrorMessage("L'identifiant du niveau ne doit pas être vide.");
    }
}

namespace erp_univ_back.Tests.Features.Evaluation;

using erp_univ_back.Features.Evaluation.SaisirNotes;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

public class SaisirNotesValidatorTests
{
    private readonly SaisirNotesValidator _validator = new();

    [Fact]
    public void Validate_DoitReussir_QuandRequeteEstValide()
    {
        // Arrange
        var request = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(Guid.NewGuid(), 15.50m, false, "Bien"),
            new LigneSaisieNoteRequest(Guid.NewGuid(), null, true, "Malade")
        ]);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DoitEchouer_QuandListeNotesEstVide()
    {
        var request = new SaisirNotesRequest([]);

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("La liste des notes ne peut pas être vide.");
    }

    [Fact]
    public void Validate_DoitEchouer_QuandEtudiantIdEstVide()
    {
        var request = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(Guid.Empty, 14.00m)
        ]);

        var result = _validator.TestValidate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DoitEchouer_QuandNoteEstNegative()
    {
        var request = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(Guid.NewGuid(), -1.00m)
        ]);

        var result = _validator.TestValidate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DoitEchouer_QuandEtudiantNonAbsentEtNoteNull()
    {
        var request = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(Guid.NewGuid(), null, EstAbsent: false)
        ]);

        var result = _validator.TestValidate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DoitEchouer_QuandCommentaireDepasse250Caracteres()
    {
        var request = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(Guid.NewGuid(), 10.00m, false, new string('X', 251))
        ]);

        var result = _validator.TestValidate(request);
        result.IsValid.Should().BeFalse();
    }
}


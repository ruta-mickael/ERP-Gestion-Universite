namespace erp_univ_back.Tests.Features.Evaluation;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using erp_univ_back.Features.AnneeAcademique.Create;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Etablissement.Create;
using erp_univ_back.Features.Evaluation;
using erp_univ_back.Features.Evaluation.Create;
using erp_univ_back.Features.Filiere.Create;
using erp_univ_back.Features.FiliereNiveau.Create;
using erp_univ_back.Features.Matiere.Create;
using erp_univ_back.Features.Niveau.Create;
using erp_univ_back.Features.Professeur.Create;
using erp_univ_back.Features.UniteEnseignement.Create;
using erp_univ_back.Shared.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public class CreateEvaluationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public CreateEvaluationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "DbTest_Evaluation_Create_" + Guid.NewGuid().ToString();

        var application = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<ErpUnivDbContext>));
                services.RemoveAll(typeof(DbContextOptions));
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<ErpUnivDbContext>));
                services.RemoveAll(typeof(ErpUnivDbContext));

                services.AddDbContext<ErpUnivDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });
            });
        });

        using (var scope = application.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ErpUnivDbContext>();
            db.Database.EnsureCreated();
        }

        _http = application.CreateClient();
    }

    private async Task<Guid> CreerEtablissementAsync(string nom = "Univ Test Eval")
    {
        var request = new CreateEtablissementRequest(
            Nom: $"{nom}_{Guid.NewGuid():N}",
            Adresse: "Campus Universitaire",
            CodePostal: "101",
            Ville: "Antananarivo",
            Pays: "Madagascar",
            Telephone: "+26134" + Random.Shared.Next(10000000, 99999999),
            Email: $"univ_{Guid.NewGuid():N}@test.mg",
            Statut: StatusEtablissement.Actif
        );

        var response = await _http.PostAsJsonAsync("/api/etablissements", request);
        var created = await response.Content.ReadFromJsonAsync<CreateEtablissementResponse>(_jsonOptions);
        return created!.Id;
    }

    private async Task<Guid> CreerAnneeAcademiqueAsync(Guid etablissementId, int anneeDebut = 2025)
    {
        var anneeFin = anneeDebut + 1;
        var request = new CreateAnneeAcademiqueRequest(
            Libelle: $"{anneeDebut}-{anneeFin}",
            DateDebut: new DateOnly(anneeDebut, 9, 1),
            DateFin: new DateOnly(anneeFin, 6, 30),
            EstActive: true
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/annees-academiques");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateAnneeAcademiqueResponse>(_jsonOptions);
        return created!.Id;
    }

    private async Task<(Guid filiereId, Guid niveauId, Guid filiereNiveauId)> CreerFiliereNiveauAsync(
        Guid etablissementId, string filiereCode = "INF", string niveauCode = "L1")
    {
        var filiereReq = new CreateFiliereRequest(filiereCode + "_" + Guid.NewGuid().ToString("N")[..4], "Informatique", null);
        using var filiereMsg = new HttpRequestMessage(HttpMethod.Post, "/api/filieres");
        filiereMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        filiereMsg.Content = JsonContent.Create(filiereReq);
        var filiereRes = await _http.SendAsync(filiereMsg);
        var filiere = await filiereRes.Content.ReadFromJsonAsync<CreateFiliereResponse>(_jsonOptions);

        var niveauReq = new CreateNiveauRequest(niveauCode + "_" + Guid.NewGuid().ToString("N")[..4], "Licence 1", 1);
        using var niveauMsg = new HttpRequestMessage(HttpMethod.Post, "/api/niveaux");
        niveauMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        niveauMsg.Content = JsonContent.Create(niveauReq);
        var niveauRes = await _http.SendAsync(niveauMsg);
        var niveau = await niveauRes.Content.ReadFromJsonAsync<CreateNiveauResponse>(_jsonOptions);

        var fnReq = new CreateFiliereNiveauRequest(filiere!.Id, niveau!.Id);
        using var fnMsg = new HttpRequestMessage(HttpMethod.Post, "/api/filiere-niveaux");
        fnMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        fnMsg.Content = JsonContent.Create(fnReq);
        var fnRes = await _http.SendAsync(fnMsg);
        var fn = await fnRes.Content.ReadFromJsonAsync<CreateFiliereNiveauResponse>(_jsonOptions);

        return (filiere.Id, niveau.Id, fn!.Id);
    }

    private async Task<Guid> CreerUeAsync(Guid etablissementId, Guid filiereNiveauId, string nom = "Algorithmique et Dev")
    {
        var request = new CreateUniteEnseignementRequest(nom, 6, 1, filiereNiveauId, "UE_" + Guid.NewGuid().ToString("N")[..4]);
        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/unites-enseignement");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateUniteEnseignementResponse>(_jsonOptions);
        return created!.Id;
    }

    private async Task<Guid> CreerMatiereAsync(Guid etablissementId, Guid ueId, string nom = "Programmation C#")
    {
        var request = new CreateMatiereRequest(nom, "CS" + Random.Shared.Next(100, 999), 3, ueId);
        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/matieres");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateMatiereResponse>(_jsonOptions);
        return created!.Id;
    }

    private async Task<Guid> CreerProfesseurAsync(Guid etablissementId, string nom = "Rakoto", string prenom = "Jean")
    {
        var rand = Random.Shared.Next(10000000, 99999999);
        var cin = "101" + Random.Shared.Next(100000000, 999999999);
        var request = new CreateProfesseurRequest(
            Nom: nom,
            Prenom: prenom,
            DateNaissance: new DateOnly(1980, 5, 12),
            LieuNaissance: "Antananarivo",
            NumeroTelephone: "+26132" + rand,
            Email: $"prof_{Guid.NewGuid():N}@univ.mg",
            NumeroCin: cin
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/professeurs");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateProfesseurResponse>(_jsonOptions);
        return created!.Id;
    }

    [Fact]
    public async Task CreerEvaluation_DoitRetourner201Created_QuandRequeteEstValide()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var (_, _, fnId) = await CreerFiliereNiveauAsync(etablissementId);
        var ueId = await CreerUeAsync(etablissementId, fnId);
        var matiereId = await CreerMatiereAsync(etablissementId, ueId, "Base de données SQL");
        var profId = await CreerProfesseurAsync(etablissementId, "Randria", "Michel");

        var request = new CreateEvaluationRequest(
            MatiereId: matiereId,
            FiliereNiveauId: fnId,
            AnneeAcademiqueId: anneeId,
            DateEvaluation: DateTime.UtcNow.AddDays(5),
            Titre: "Examen Final SQL",
            TypeEvaluation: TypeEvaluation.ExamenFinal,
            Session: SessionEvaluation.Normale,
            Bareme: 20.00m,
            Poids: 0.60m,
            ProfesseurId: profId
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/evaluations");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateEvaluationResponse>(_jsonOptions);
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.Titre.Should().Be("Examen Final SQL");
        created.TypeEvaluation.Should().Be(TypeEvaluation.ExamenFinal);
        created.Session.Should().Be(SessionEvaluation.Normale);
        created.Bareme.Should().Be(20.00m);
        created.Poids.Should().Be(0.60m);
        created.MatiereId.Should().Be(matiereId);
        created.MatiereNom.Should().Be("Base de données SQL");
        created.FiliereNiveauId.Should().Be(fnId);
        created.AnneeAcademiqueId.Should().Be(anneeId);
        created.ProfesseurId.Should().Be(profId);
        created.ProfesseurNomComplet.Should().Be("Randria Michel");
    }

    [Fact]
    public async Task CreerEvaluation_DoitRetourner404NotFound_QuandMatiereInexistante()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var (_, _, fnId) = await CreerFiliereNiveauAsync(etablissementId);

        var request = new CreateEvaluationRequest(
            MatiereId: Guid.NewGuid(), // Inexistante
            FiliereNiveauId: fnId,
            AnneeAcademiqueId: anneeId,
            DateEvaluation: DateTime.UtcNow
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/evaluations");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreerEvaluation_DoitRetourner404NotFound_QuandClasseInexistante()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var (_, _, fnId) = await CreerFiliereNiveauAsync(etablissementId);
        var ueId = await CreerUeAsync(etablissementId, fnId);
        var matiereId = await CreerMatiereAsync(etablissementId, ueId);

        var request = new CreateEvaluationRequest(
            MatiereId: matiereId,
            FiliereNiveauId: Guid.NewGuid(), // Inexistante
            AnneeAcademiqueId: anneeId,
            DateEvaluation: DateTime.UtcNow
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/evaluations");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreerEvaluation_DoitRetourner404NotFound_QuandAnneeAcademiqueInexistante()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var (_, _, fnId) = await CreerFiliereNiveauAsync(etablissementId);
        var ueId = await CreerUeAsync(etablissementId, fnId);
        var matiereId = await CreerMatiereAsync(etablissementId, ueId);

        var request = new CreateEvaluationRequest(
            MatiereId: matiereId,
            FiliereNiveauId: fnId,
            AnneeAcademiqueId: Guid.NewGuid(), // Inexistante
            DateEvaluation: DateTime.UtcNow
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/evaluations");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreerEvaluation_DoitRetourner404NotFound_QuandProfesseurInexistant()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var (_, _, fnId) = await CreerFiliereNiveauAsync(etablissementId);
        var ueId = await CreerUeAsync(etablissementId, fnId);
        var matiereId = await CreerMatiereAsync(etablissementId, ueId);

        var request = new CreateEvaluationRequest(
            MatiereId: matiereId,
            FiliereNiveauId: fnId,
            AnneeAcademiqueId: anneeId,
            DateEvaluation: DateTime.UtcNow,
            ProfesseurId: Guid.NewGuid() // Inexistant
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/evaluations");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreerEvaluation_DoitRetourner400BadRequest_QuandMatiereNeCorrespondPasALaClasse()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var (_, _, fnId1) = await CreerFiliereNiveauAsync(etablissementId, "INF", "L1");
        var (_, _, fnId2) = await CreerFiliereNiveauAsync(etablissementId, "GESTION", "M1");
        
        var ueId1 = await CreerUeAsync(etablissementId, fnId1);
        var matiereId1 = await CreerMatiereAsync(etablissementId, ueId1, "C# Pour L1");

        // On associe la matière de L1 à la classe de M1
        var request = new CreateEvaluationRequest(
            MatiereId: matiereId1,
            FiliereNiveauId: fnId2, // Classe discordante
            AnneeAcademiqueId: anneeId,
            DateEvaluation: DateTime.UtcNow
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/evaluations");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreerEvaluation_DoitRetourner409Conflict_QuandEvaluationIdentiqueExiste()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var (_, _, fnId) = await CreerFiliereNiveauAsync(etablissementId);
        var ueId = await CreerUeAsync(etablissementId, fnId);
        var matiereId = await CreerMatiereAsync(etablissementId, ueId);

        var request = new CreateEvaluationRequest(
            MatiereId: matiereId,
            FiliereNiveauId: fnId,
            AnneeAcademiqueId: anneeId,
            DateEvaluation: DateTime.UtcNow,
            Titre: "Contrôle Continu 1",
            TypeEvaluation: TypeEvaluation.ControleContinu,
            Session: SessionEvaluation.Normale
        );

        using var msg1 = new HttpRequestMessage(HttpMethod.Post, "/api/evaluations");
        msg1.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg1.Content = JsonContent.Create(request, options: _jsonOptions);
        var res1 = await _http.SendAsync(msg1);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - 2ème tentative avec les mêmes identifiants et titre
        using var msg2 = new HttpRequestMessage(HttpMethod.Post, "/api/evaluations");
        msg2.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg2.Content = JsonContent.Create(request, options: _jsonOptions);
        var res2 = await _http.SendAsync(msg2);

        // Assert
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

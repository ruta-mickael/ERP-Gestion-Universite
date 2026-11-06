namespace erp_univ_back.Tests.Features.Evaluation;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using erp_univ_back.Features.AnneeAcademique.Create;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Etablissement.Create;
using erp_univ_back.Features.Etudiant.Create;
using erp_univ_back.Features.Evaluation;
using erp_univ_back.Features.Evaluation.Create;
using erp_univ_back.Features.Evaluation.SaisirNotes;
using erp_univ_back.Features.Filiere.Create;
using erp_univ_back.Features.FiliereNiveau.Create;
using erp_univ_back.Features.Inscription.Register;
using erp_univ_back.Features.Matiere.Create;
using erp_univ_back.Features.Niveau.Create;
using erp_univ_back.Features.UniteEnseignement.Create;
using erp_univ_back.Shared.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public class SaisirNotesIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _http;
    private readonly WebApplicationFactory<Program> _application;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public SaisirNotesIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "DbTest_Evaluation_SaisirNotes_" + Guid.NewGuid().ToString();

        _application = factory.WithWebHostBuilder(builder =>
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

        using (var scope = _application.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ErpUnivDbContext>();
            db.Database.EnsureCreated();
        }

        _http = _application.CreateClient();
    }

    private async Task<Guid> CreerEtablissementAsync(string nom = "Univ Test Notes")
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

    private async Task<Guid> CreerFiliereNiveauAsync(Guid etablissementId)
    {
        var filiereReq = new CreateFiliereRequest("INF_" + Guid.NewGuid().ToString("N")[..4], "Informatique", null);
        using var filiereMsg = new HttpRequestMessage(HttpMethod.Post, "/api/filieres");
        filiereMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        filiereMsg.Content = JsonContent.Create(filiereReq);
        var filiereRes = await _http.SendAsync(filiereMsg);
        var filiere = await filiereRes.Content.ReadFromJsonAsync<CreateFiliereResponse>(_jsonOptions);

        var niveauReq = new CreateNiveauRequest("L1_" + Guid.NewGuid().ToString("N")[..4], "Licence 1", 1);
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

        return fn!.Id;
    }

    private async Task<Guid> CreerUeAsync(Guid etablissementId, Guid filiereNiveauId)
    {
        var request = new CreateUniteEnseignementRequest("Programmation", 6, 1, filiereNiveauId, "UE_" + Guid.NewGuid().ToString("N")[..4]);
        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/unites-enseignement");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateUniteEnseignementResponse>(_jsonOptions);
        return created!.Id;
    }

    private async Task<Guid> CreerMatiereAsync(Guid etablissementId, Guid ueId)
    {
        var request = new CreateMatiereRequest("C# Objet", "CS" + Random.Shared.Next(100, 999), 3, ueId);
        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/matieres");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateMatiereResponse>(_jsonOptions);
        return created!.Id;
    }

    private async Task<Guid> CreerEvaluationAsync(
        Guid etablissementId, Guid matiereId, Guid fnId, Guid anneeId, decimal bareme = 20.00m)
    {
        var request = new CreateEvaluationRequest(
            MatiereId: matiereId,
            FiliereNiveauId: fnId,
            AnneeAcademiqueId: anneeId,
            DateEvaluation: DateTime.UtcNow,
            Titre: "Examen C#",
            TypeEvaluation: TypeEvaluation.ExamenFinal,
            Session: SessionEvaluation.Normale,
            Bareme: bareme,
            Poids: 1.00m
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/evaluations");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateEvaluationResponse>(_jsonOptions);
        return created!.Id;
    }

    private async Task<(Guid etudiantId, Guid inscriptionId)> InscrireEtudiantAsync(
        Guid etablissementId, Guid fnId, Guid anneeId, string nom = "Rakoto", string prenom = "Jean")
    {
        var rand = Random.Shared.Next(10000000, 99999999);
        var cin = "101" + Random.Shared.Next(100000000, 999999999);
        var request = new RegisterInscriptionRequest(
            EtudiantInfos: new CreateEtudiantRequest(
                Nom: nom,
                Prenom: prenom,
                DateNaissance: new DateOnly(2002, 3, 15),
                LieuNaissance: "Antananarivo",
                NumeroTelephone: "+26134" + rand,
                Email: $"etudiant_{Guid.NewGuid():N}@univ.mg",
                NumeroCin: cin
            ),
            IdFiliereNiveau: fnId,
            IdAnneeAcademique: anneeId
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/inscriptions/register");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var res = await response.Content.ReadFromJsonAsync<RegisterInscriptionResponse>(_jsonOptions);
        return (res!.EtudiantId, res.Id);
    }

    [Fact]
    public async Task SaisirNotes_DoitReussirEnLot_AvecInsertionsEtMisesAJour()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var fnId = await CreerFiliereNiveauAsync(etablissementId);
        var ueId = await CreerUeAsync(etablissementId, fnId);
        var matiereId = await CreerMatiereAsync(etablissementId, ueId);
        var evalId = await CreerEvaluationAsync(etablissementId, matiereId, fnId, anneeId, bareme: 20.00m);

        var (etudiant1, _) = await InscrireEtudiantAsync(etablissementId, fnId, anneeId, "Dupont", "Alice");
        var (etudiant2, _) = await InscrireEtudiantAsync(etablissementId, fnId, anneeId, "Martin", "Bob");
        var (etudiant3, _) = await InscrireEtudiantAsync(etablissementId, fnId, anneeId, "Curie", "Marie");

        // 1er passage : Alice (15.5) et Bob (12.0)
        var premierLot = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(etudiant1, 15.50m, EstAbsent: false, "Bonne copie"),
            new LigneSaisieNoteRequest(etudiant2, 12.00m, EstAbsent: false, "Moyen")
        ]);

        using var msg1 = new HttpRequestMessage(HttpMethod.Post, $"/api/evaluations/{evalId}/notes");
        msg1.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg1.Content = JsonContent.Create(premierLot, options: _jsonOptions);

        // Act 1
        var res1 = await _http.SendAsync(msg1);

        // Assert 1
        res1.StatusCode.Should().Be(HttpStatusCode.OK);
        var corps1 = await res1.Content.ReadFromJsonAsync<SaisirNotesResponse>(_jsonOptions);
        corps1.Should().NotBeNull();
        corps1!.TotalTraites.Should().Be(2);
        corps1.TotalInseres.Should().Be(2);
        corps1.TotalMisAJour.Should().Be(0);

        // 2ème passage : Alice passe à 17.0 (mise à jour), Bob est marqué absent (mise à jour), Marie a 14.5 (insertion)
        var deuxiemeLot = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(etudiant1, 17.00m, EstAbsent: false, "Après révision"),
            new LigneSaisieNoteRequest(etudiant2, null, EstAbsent: true, "Malade justifié"),
            new LigneSaisieNoteRequest(etudiant3, 14.50m, EstAbsent: false, "Bien")
        ]);

        using var msg2 = new HttpRequestMessage(HttpMethod.Post, $"/api/evaluations/{evalId}/notes");
        msg2.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg2.Content = JsonContent.Create(deuxiemeLot, options: _jsonOptions);

        // Act 2
        var res2 = await _http.SendAsync(msg2);

        // Assert 2
        res2.StatusCode.Should().Be(HttpStatusCode.OK);
        var corps2 = await res2.Content.ReadFromJsonAsync<SaisirNotesResponse>(_jsonOptions);
        corps2.Should().NotBeNull();
        corps2!.TotalTraites.Should().Be(3);
        corps2.TotalInseres.Should().Be(1);
        corps2.TotalMisAJour.Should().Be(2);

        // Vérification des données en base
        using var scope = _application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpUnivDbContext>();
        var noteAlice = await db.Notes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.EvaluationId == evalId && n.EtudiantId == etudiant1);
        noteAlice.Should().NotBeNull();
        noteAlice!.Valeur.Should().Be(17.00m);
        noteAlice.Commentaire.Should().Be("Après révision");

        var noteBob = await db.Notes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.EvaluationId == evalId && n.EtudiantId == etudiant2);
        noteBob.Should().NotBeNull();
        noteBob!.EstAbsent.Should().BeTrue();
        noteBob.Valeur.Should().BeNull();
    }

    [Fact]
    public async Task SaisirNotes_DoitEchouer_QuandEvaluationEstVerrouillee()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var fnId = await CreerFiliereNiveauAsync(etablissementId);
        var ueId = await CreerUeAsync(etablissementId, fnId);
        var matiereId = await CreerMatiereAsync(etablissementId, ueId);
        var evalId = await CreerEvaluationAsync(etablissementId, matiereId, fnId, anneeId);
        var (etudiant, _) = await InscrireEtudiantAsync(etablissementId, fnId, anneeId);

        // On verrouille l'évaluation directement en base
        using (var scope = _application.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ErpUnivDbContext>();
            var eval = await db.Evaluations.IgnoreQueryFilters().FirstAsync(e => e.Id == evalId);
            eval.EstVerrouillee = true;
            eval.DateVerrouillage = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var request = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(etudiant, 15.00m)
        ]);

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/evaluations/{evalId}/notes");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaisirNotes_DoitEchouer_QuandNoteDepasseLeBareme()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var fnId = await CreerFiliereNiveauAsync(etablissementId);
        var ueId = await CreerUeAsync(etablissementId, fnId);
        var matiereId = await CreerMatiereAsync(etablissementId, ueId);
        var evalId = await CreerEvaluationAsync(etablissementId, matiereId, fnId, anneeId, bareme: 20.00m);
        var (etudiant, _) = await InscrireEtudiantAsync(etablissementId, fnId, anneeId);

        // 25.00m dépasse le barème de 20.00m
        var request = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(etudiant, 25.00m)
        ]);

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/evaluations/{evalId}/notes");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaisirNotes_DoitEchouer_QuandEtudiantNonInscritDansLaClasse()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var fnId1 = await CreerFiliereNiveauAsync(etablissementId);
        var fnId2 = await CreerFiliereNiveauAsync(etablissementId);
        var ueId = await CreerUeAsync(etablissementId, fnId1);
        var matiereId = await CreerMatiereAsync(etablissementId, ueId);
        var evalId = await CreerEvaluationAsync(etablissementId, matiereId, fnId1, anneeId);

        // Étudiant inscrit dans la classe 2 au lieu de la classe 1
        var (etudiantHorsClasse, _) = await InscrireEtudiantAsync(etablissementId, fnId2, anneeId);

        var request = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(etudiantHorsClasse, 12.00m)
        ]);

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/evaluations/{evalId}/notes");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaisirNotes_DoitEchouer_QuandDoublonDansLaRequete()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var fnId = await CreerFiliereNiveauAsync(etablissementId);
        var ueId = await CreerUeAsync(etablissementId, fnId);
        var matiereId = await CreerMatiereAsync(etablissementId, ueId);
        var evalId = await CreerEvaluationAsync(etablissementId, matiereId, fnId, anneeId);
        var (etudiant, _) = await InscrireEtudiantAsync(etablissementId, fnId, anneeId);

        // Le même étudiant apparaît deux fois dans le même lot
        var request = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(etudiant, 12.00m),
            new LigneSaisieNoteRequest(etudiant, 14.00m)
        ]);

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/evaluations/{evalId}/notes");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaisirNotes_DoitRetourner404_QuandEvaluationInexistante()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var request = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(Guid.NewGuid(), 12.00m)
        ]);

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/evaluations/{Guid.NewGuid()}/notes");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

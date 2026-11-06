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
using erp_univ_back.Features.Evaluation.GetByEvaluation;
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

public class GetNotesByEvaluationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public GetNotesByEvaluationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "DbTest_Evaluation_GetByEval_" + Guid.NewGuid().ToString();

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

    private async Task<Guid> CreerEtablissementAsync(string nom = "Univ Test GetNotes")
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
        var request = new CreateMatiereRequest("Algorithmes", "ALGO" + Random.Shared.Next(10, 99), 3, ueId);
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
            Titre: "Examen Algo",
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

    private async Task<Guid> InscrireEtudiantAsync(
        Guid etablissementId, Guid fnId, Guid anneeId, string nom, string prenom)
    {
        var rand = Random.Shared.Next(10000000, 99999999);
        var cin = "101" + Random.Shared.Next(100000000, 999999999);
        var request = new RegisterInscriptionRequest(
            EtudiantInfos: new CreateEtudiantRequest(
                Nom: nom,
                Prenom: prenom,
                DateNaissance: new DateOnly(2003, 5, 20),
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
        return res!.EtudiantId;
    }

    [Fact]
    public async Task ObtenirNotesParEvaluation_DoitRetourner200OK_AvecGrilleCompleteEtStatistiques()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var fnId = await CreerFiliereNiveauAsync(etablissementId);
        var ueId = await CreerUeAsync(etablissementId, fnId);
        var matiereId = await CreerMatiereAsync(etablissementId, ueId);
        var evalId = await CreerEvaluationAsync(etablissementId, matiereId, fnId, anneeId, bareme: 20.00m);

        var aliceId = await InscrireEtudiantAsync(etablissementId, fnId, anneeId, "Dupont", "Alice");
        var bobId = await InscrireEtudiantAsync(etablissementId, fnId, anneeId, "Martin", "Bob");
        var charlieId = await InscrireEtudiantAsync(etablissementId, fnId, anneeId, "Curie", "Charlie");

        // Saisie des notes : Alice a 16.00, Bob est absent. Charlie n'est pas encore noté.
        var saisieRequest = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(aliceId, 16.00m, EstAbsent: false, "Très bien"),
            new LigneSaisieNoteRequest(bobId, null, EstAbsent: true, "Malade")
        ]);

        using var msgSaisie = new HttpRequestMessage(HttpMethod.Post, $"/api/evaluations/{evalId}/notes");
        msgSaisie.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msgSaisie.Content = JsonContent.Create(saisieRequest, options: _jsonOptions);
        var saisieRes = await _http.SendAsync(msgSaisie);
        saisieRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Récupération de la grille complète
        using var msgGet = new HttpRequestMessage(HttpMethod.Get, $"/api/evaluations/{evalId}/notes");
        msgGet.Headers.Add("X-EtablissementId", etablissementId.ToString());
        var getRes = await _http.SendAsync(msgGet);

        // Assert
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await getRes.Content.ReadFromJsonAsync<GetNotesByEvaluationResponse>(_jsonOptions);
        data.Should().NotBeNull();

        // 1. Informations de l'évaluation
        data!.Evaluation.Id.Should().Be(evalId);
        data.Evaluation.Titre.Should().Be("Examen Algo");
        data.Evaluation.Bareme.Should().Be(20.00m);
        data.Evaluation.EstVerrouillee.Should().BeFalse();

        // 2. Statistiques de la classe
        data.Statistiques.TotalInscrits.Should().Be(3);
        data.Statistiques.TotalNotesSaisies.Should().Be(2);
        data.Statistiques.TotalPresents.Should().Be(1);
        data.Statistiques.TotalAbsents.Should().Be(1);
        data.Statistiques.MoyenneClasse.Should().Be(16.00m);
        data.Statistiques.NoteMin.Should().Be(16.00m);
        data.Statistiques.NoteMax.Should().Be(16.00m);

        // 3. Grille des étudiants (3 lignes)
        data.Lignes.Should().HaveCount(3);

        var ligneAlice = data.Lignes.First(l => l.EtudiantId == aliceId);
        ligneAlice.EstSaisie.Should().BeTrue();
        ligneAlice.Valeur.Should().Be(16.00m);
        ligneAlice.EstAbsent.Should().BeFalse();
        ligneAlice.Commentaire.Should().Be("Très bien");

        var ligneBob = data.Lignes.First(l => l.EtudiantId == bobId);
        ligneBob.EstSaisie.Should().BeTrue();
        ligneBob.Valeur.Should().BeNull();
        ligneBob.EstAbsent.Should().BeTrue();

        var ligneCharlie = data.Lignes.First(l => l.EtudiantId == charlieId);
        ligneCharlie.EstSaisie.Should().BeFalse();
        ligneCharlie.Valeur.Should().BeNull();
        ligneCharlie.EstAbsent.Should().BeFalse();
        ligneCharlie.NoteId.Should().BeNull();
    }

    [Fact]
    public async Task ObtenirNotesParEvaluation_DoitRetourner404NotFound_QuandEvaluationInexistante()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/evaluations/{Guid.NewGuid()}/notes");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ObtenirNotesParEvaluation_DoitIsolerParTenant_QuandAutreEtablissement()
    {
        // Arrange
        var etab1 = await CreerEtablissementAsync("Univ Etab 1");
        var etab2 = await CreerEtablissementAsync("Univ Etab 2");

        var anneeId = await CreerAnneeAcademiqueAsync(etab1);
        var fnId = await CreerFiliereNiveauAsync(etab1);
        var ueId = await CreerUeAsync(etab1, fnId);
        var matiereId = await CreerMatiereAsync(etab1, ueId);
        var evalId = await CreerEvaluationAsync(etab1, matiereId, fnId, anneeId);

        // Tentative d'accès depuis l'établissement 2
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/evaluations/{evalId}/notes");
        msg.Headers.Add("X-EtablissementId", etab2.ToString());

        // Act
        var response = await _http.SendAsync(msg);

        // Assert - Doit retourner 404 (isolation multi-tenant stricte)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

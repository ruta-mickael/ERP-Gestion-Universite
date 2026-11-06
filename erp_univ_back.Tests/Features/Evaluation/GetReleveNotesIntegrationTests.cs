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
using erp_univ_back.Features.Evaluation.GetReleveNotes;
using erp_univ_back.Features.Evaluation.SaisirNotes;
using erp_univ_back.Features.Filiere.Create;
using erp_univ_back.Features.FiliereNiveau.Create;
using erp_univ_back.Features.Inscription;
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

public class GetReleveNotesIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _http;
    private readonly WebApplicationFactory<Program> _application;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public GetReleveNotesIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "DbTest_Evaluation_Releve_" + Guid.NewGuid().ToString();

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

    private async Task<Guid> CreerEtablissementAsync(string nom = "Univ Test Releve")
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

    private async Task<Guid> CreerUeAsync(Guid etablissementId, Guid filiereNiveauId, string nom, int credit, int semestre)
    {
        var request = new CreateUniteEnseignementRequest(nom, credit, semestre, filiereNiveauId, "UE_" + Guid.NewGuid().ToString("N")[..4]);
        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/unites-enseignement");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateUniteEnseignementResponse>(_jsonOptions);
        return created!.Id;
    }

    private async Task<Guid> CreerMatiereAsync(Guid etablissementId, Guid ueId, string nom, int coefficient)
    {
        var request = new CreateMatiereRequest(nom, "M" + Random.Shared.Next(100, 999), coefficient, ueId);
        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/matieres");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateMatiereResponse>(_jsonOptions);
        return created!.Id;
    }

    private async Task<Guid> CreerEvaluationAsync(
        Guid etablissementId, Guid matiereId, Guid fnId, Guid anneeId, string titre, decimal poids, TypeEvaluation type)
    {
        var request = new CreateEvaluationRequest(
            MatiereId: matiereId,
            FiliereNiveauId: fnId,
            AnneeAcademiqueId: anneeId,
            DateEvaluation: DateTime.UtcNow,
            Titre: titre,
            TypeEvaluation: type,
            Session: SessionEvaluation.Normale,
            Bareme: 20.00m,
            Poids: poids
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
                DateNaissance: new DateOnly(2002, 4, 10),
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

    private async Task SaisirNoteAsync(Guid etablissementId, Guid evalId, Guid etudiantId, decimal valeur)
    {
        var request = new SaisirNotesRequest([
            new LigneSaisieNoteRequest(etudiantId, valeur)
        ]);

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/evaluations/{evalId}/notes");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ObtenirReleveNotes_DoitRetourner200OK_AvecCalculsMoyennesMentionsEtCredits()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var fnId = await CreerFiliereNiveauAsync(etablissementId);

        // UE 1 : Algorithmique (6 crédits, Semestre 1)
        var ue1Id = await CreerUeAsync(etablissementId, fnId, "UE Algorithmique", 6, 1);
        var matiere1Id = await CreerMatiereAsync(etablissementId, ue1Id, "C# Fondations", 3);
        var matiere2Id = await CreerMatiereAsync(etablissementId, ue1Id, "Web HTML/CSS", 2);

        // UE 2 : Mathématiques (4 crédits, Semestre 1)
        var ue2Id = await CreerUeAsync(etablissementId, fnId, "UE Mathématiques", 4, 1);
        var matiere3Id = await CreerMatiereAsync(etablissementId, ue2Id, "Algèbre Linéaire", 1);

        // Évaluations de Matière 1 (C#) : CC (0.4) et Examen (0.6)
        var evalCcCsharp = await CreerEvaluationAsync(etablissementId, matiere1Id, fnId, anneeId, "CC C#", 0.40m, TypeEvaluation.ControleContinu);
        var evalExamCsharp = await CreerEvaluationAsync(etablissementId, matiere1Id, fnId, anneeId, "Exam C#", 0.60m, TypeEvaluation.ExamenFinal);

        // Évaluation de Matière 2 (Web) : Exam (1.0)
        var evalExamWeb = await CreerEvaluationAsync(etablissementId, matiere2Id, fnId, anneeId, "Exam Web", 1.00m, TypeEvaluation.ExamenFinal);

        // Évaluation de Matière 3 (Algèbre) : Exam (1.0)
        var evalExamMath = await CreerEvaluationAsync(etablissementId, matiere3Id, fnId, anneeId, "Exam Math", 1.00m, TypeEvaluation.ExamenFinal);

        // Inscription de l'étudiant
        var (etudiantId, inscriptionId) = await InscrireEtudiantAsync(etablissementId, fnId, anneeId, "Andria", "Faly");

        // Saisie des notes :
        // Matière 1 (C#) : CC=14 (0.4) + Exam=16 (0.6) -> Moyenne = 15.20
        await SaisirNoteAsync(etablissementId, evalCcCsharp, etudiantId, 14.00m);
        await SaisirNoteAsync(etablissementId, evalExamCsharp, etudiantId, 16.00m);

        // Matière 2 (Web) : Exam=12 (1.0) -> Moyenne = 12.00
        await SaisirNoteAsync(etablissementId, evalExamWeb, etudiantId, 12.00m);

        // Moyenne UE 1 = (15.20 * 3 + 12.00 * 2) / 5 = (45.6 + 24.0) / 5 = 13.92 -> Validée (6 crédits)

        // Matière 3 (Math) : Exam=15 (1.0) -> Moyenne = 15.00
        // Moyenne UE 2 = 15.00 -> Validée (4 crédits)
        await SaisirNoteAsync(etablissementId, evalExamMath, etudiantId, 15.00m);

        // Moyenne Semestre 1 = (13.92 * 6 + 15.00 * 4) / 10 = (83.52 + 60.00) / 10 = 14.35 -> Mention "Bien"
        // Crédits acquis = 10 / 10

        // Act
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/evaluations/releve/{inscriptionId}");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releve = await response.Content.ReadFromJsonAsync<GetReleveNotesResponse>(_jsonOptions);
        releve.Should().NotBeNull();

        // 1. Informations de l'étudiant et inscription
        releve!.Etudiant.EtudiantId.Should().Be(etudiantId);
        releve.Etudiant.Nom.Should().Be("Andria");
        releve.Inscription.InscriptionId.Should().Be(inscriptionId);
        releve.SituationFinanciere.EstEnRegle.Should().BeTrue();

        // 2. Semestres
        releve.Semestres.Should().HaveCount(1);
        var sem1 = releve.Semestres[0];
        sem1.Semestre.Should().Be(1);
        sem1.TotalCredits.Should().Be(10);
        sem1.CreditsAcquis.Should().Be(10);
        sem1.Moyenne.Should().Be(14.35m);
        sem1.Mention.Should().Be("Bien");

        // 3. UEs du Semestre 1
        sem1.UnitesEnseignement.Should().HaveCount(2);

        var ue1Dto = sem1.UnitesEnseignement.First(u => u.UeId == ue1Id);
        ue1Dto.Credit.Should().Be(6);
        ue1Dto.CreditsAcquis.Should().Be(6);
        ue1Dto.EstValidee.Should().BeTrue();
        ue1Dto.Moyenne.Should().Be(13.92m);

        var m1Dto = ue1Dto.Matieres.First(m => m.MatiereId == matiere1Id);
        m1Dto.Moyenne.Should().Be(15.20m);
        m1Dto.Evaluations.Should().HaveCount(2);

        var m2Dto = ue1Dto.Matieres.First(m => m.MatiereId == matiere2Id);
        m2Dto.Moyenne.Should().Be(12.00m);

        var ue2Dto = sem1.UnitesEnseignement.First(u => u.UeId == ue2Id);
        ue2Dto.Credit.Should().Be(4);
        ue2Dto.CreditsAcquis.Should().Be(4);
        ue2Dto.EstValidee.Should().BeTrue();
        ue2Dto.Moyenne.Should().Be(15.00m);

        // 4. Bilan Académique Général
        releve.Bilan.TotalCreditsInscrits.Should().Be(10);
        releve.Bilan.TotalCreditsAcquis.Should().Be(10);
        releve.Bilan.MoyenneGenerale.Should().Be(14.35m);
        releve.Bilan.Mention.Should().Be("Bien");
        releve.Bilan.DecisionJury.Should().Be("Admis");
    }

    [Fact]
    public async Task ObtenirReleveNotes_DoitSignalerImpayes_QuandEcheanceNonSoldee()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var fnId = await CreerFiliereNiveauAsync(etablissementId);
        var (_, inscriptionId) = await InscrireEtudiantAsync(etablissementId, fnId, anneeId);

        // On injecte manuellement une échéance impayée directement en base
        using (var scope = _application.Services.CreateScope())
        {
            var tenantService = scope.ServiceProvider.GetRequiredService<erp_univ_back.Shared.Services.ITenantService>();
            tenantService.DefinirEtablissement(etablissementId);

            var db = scope.ServiceProvider.GetRequiredService<ErpUnivDbContext>();
            var echeance = new EcheanceEtudiantEntity
            {
                EtablissementId = etablissementId,
                InscriptionId = inscriptionId,
                FraisCatalogueId = Guid.NewGuid(),
                Libelle = "Tranche 1 Écolage",
                NumeroOrdre = 1,
                DateLimite = DateTime.UtcNow.AddDays(-10),
                MontantAttendu = 500000.00m,
                MontantDejaPaye = 200000.00m, // Reste 300 000 Ar
                Statut = StatutEcheance.Partiel,
                ExigePourValiderInscription = true
            };
            db.EcheancesEtudiant.Add(echeance);
            await db.SaveChangesAsync();
        }

        // Act
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/evaluations/releve/{inscriptionId}");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releve = await response.Content.ReadFromJsonAsync<GetReleveNotesResponse>(_jsonOptions);
        releve.Should().NotBeNull();
        releve!.SituationFinanciere.EstEnRegle.Should().BeFalse();
        releve.SituationFinanciere.SoldeRestantDu.Should().Be(300000.00m);
        releve.SituationFinanciere.MentionFinanciere.Should().Contain("Document provisoire");
    }

    [Fact]
    public async Task ObtenirReleveNotes_DoitRetourner404_QuandInscriptionInexistante()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/evaluations/releve/{Guid.NewGuid()}");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ObtenirReleveNotes_DoitIsolerParTenant_QuandAutreEtablissement()
    {
        // Arrange
        var etab1 = await CreerEtablissementAsync("Univ 1");
        var etab2 = await CreerEtablissementAsync("Univ 2");
        var anneeId = await CreerAnneeAcademiqueAsync(etab1);
        var fnId = await CreerFiliereNiveauAsync(etab1);
        var (_, inscriptionId) = await InscrireEtudiantAsync(etab1, fnId, anneeId);

        // Tentative d'accès depuis le tenant 2
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/evaluations/releve/{inscriptionId}");
        msg.Headers.Add("X-EtablissementId", etab2.ToString());

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

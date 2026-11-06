namespace erp_univ_back.Tests.Features.Inscription.GetSituationFinanciere;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using erp_univ_back.Features.AnneeAcademique.Create;
using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.ConfigFinancier.AddPlanPaiement;
using erp_univ_back.Features.ConfigFinancier.CreateCatalogue;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Etablissement.Create;
using erp_univ_back.Features.Etudiant.Create;
using erp_univ_back.Features.Filiere.Create;
using erp_univ_back.Features.FiliereNiveau.Create;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Inscription.GetSituationFinanciere;
using erp_univ_back.Features.Inscription.Register;
using erp_univ_back.Features.Niveau.Create;
using erp_univ_back.Shared.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public class GetSituationFinanciereIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public GetSituationFinanciereIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "DbTest_GetSituationFinanciere_" + Guid.NewGuid().ToString();

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

    private async Task<Guid> CreerEtablissementAsync(string nom = "Univ Situation Test")
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
        var res = await _http.SendAsync(msg);
        var created = await res.Content.ReadFromJsonAsync<CreateAnneeAcademiqueResponse>(_jsonOptions);
        return created!.Id;
    }

    private async Task<(Guid filiereId, Guid niveauId, Guid filiereNiveauId)> CreerClasseAsync(
        Guid etablissementId, string filiereCode = "INF", string niveauCode = "L1")
    {
        var filiereReq = new CreateFiliereRequest(filiereCode + "_" + Random.Shared.Next(100, 999), "Informatique " + filiereCode, null);
        using var filiereMsg = new HttpRequestMessage(HttpMethod.Post, "/api/filieres");
        filiereMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        filiereMsg.Content = JsonContent.Create(filiereReq);
        var filiereRes = await _http.SendAsync(filiereMsg);
        var filiere = await filiereRes.Content.ReadFromJsonAsync<CreateFiliereResponse>(_jsonOptions);

        var niveauReq = new CreateNiveauRequest(niveauCode + "_" + Random.Shared.Next(100, 999), "Licence " + niveauCode, 1);
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

    private async Task<(Guid InscriptionId, string Matricule)> InscrireEtudiantAvecPlan2TranchesAsync(
        Guid etablissementId, decimal tranche1 = 500000m, decimal tranche2 = 700000m)
    {
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var (filiereId, niveauId, fnId) = await CreerClasseAsync(etablissementId, "INF", "L1");

        // 1. Frais
        var total = tranche1 + tranche2;
        var fraisReq = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeId,
            Categorie: CategorieFrais.FraisFormation,
            Libelle: $"Écolage {Guid.NewGuid():N}",
            MontantTotal: total,
            MoyensPaiementAutorises: MoyenPaiement.Especes | MoyenPaiement.MVola,
            FiliereId: filiereId,
            NiveauId: niveauId
        );
        using var fraisMsg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        fraisMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        fraisMsg.Content = JsonContent.Create(fraisReq);
        var fraisRes = await _http.SendAsync(fraisMsg);
        var frais = await fraisRes.Content.ReadFromJsonAsync<CreateCatalogueResponse>(_jsonOptions);

        // 2. Plan
        var planReq = new AddPlanPaiementRequest(
            FraisCatalogueId: frais!.Id,
            Nom: "Semestriel 2x",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Tranche 1", tranche1, DateTime.UtcNow.AddDays(30), true),
                new ModeleTrancheItemRequest(2, "Tranche 2", tranche2, DateTime.UtcNow.AddDays(120), false)
            ]
        );
        using var planMsg = new HttpRequestMessage(HttpMethod.Post, "/api/plans-paiement");
        planMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        planMsg.Content = JsonContent.Create(planReq);
        var planRes = await _http.SendAsync(planMsg);
        var plan = await planRes.Content.ReadFromJsonAsync<AddPlanPaiementResponse>(_jsonOptions);

        // 3. Inscription
        var etudiantInfos = new CreateEtudiantRequest(
            Nom: "ANDRIA",
            Prenom: "Hery",
            DateNaissance: new DateOnly(2001, 8, 20),
            LieuNaissance: "Toamasina",
            NumeroTelephone: "034" + Random.Shared.Next(1000000, 9999999),
            Email: $"hery_{Guid.NewGuid():N}@gmail.com",
            NumeroCin: "101" + Random.Shared.Next(100000000, 999999999),
            Parents: []
        );

        var inscriptionReq = new RegisterInscriptionRequest(
            EtudiantInfos: etudiantInfos,
            IdFiliereNiveau: fnId,
            IdAnneeAcademique: anneeId,
            PlansPaiementIds: [plan!.Id]
        );

        using var inscMsg = new HttpRequestMessage(HttpMethod.Post, "/api/inscriptions/register");
        inscMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        inscMsg.Content = JsonContent.Create(inscriptionReq);
        var inscRes = await _http.SendAsync(inscMsg);
        var insc = await inscRes.Content.ReadFromJsonAsync<RegisterInscriptionResponse>(_jsonOptions);

        return (insc!.Id, insc.EtudiantMatricule);
    }

    [Fact]
    public async Task ObtenirSituation_DoitRetournerSituationFinanciere_QuandMatriculeExiste()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var (_, matricule) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 500000m, 700000m);

        // Act : Appel de l'endpoint dédié de la slice GetSituationFinanciere
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/inscriptions/situation-financiere/{matricule}");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var situation = await response.Content.ReadFromJsonAsync<SituationFinanciereEtudiantResponse>(_jsonOptions);

        situation.Should().NotBeNull();
        situation!.Matricule.Should().Be(matricule);
        situation.TotalAttendu.Should().Be(1200000.00m);
        situation.TotalPaye.Should().Be(0m);
        situation.ResteAPayerTotal.Should().Be(1200000.00m);
        situation.Echeances.Should().HaveCount(2);
        situation.StatutInscription.Should().Be(StatutInscription.EnAttentePaiement);
    }

    [Fact]
    public async Task ObtenirSituation_DoitEchouer404_QuandMatriculeInexistant()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        using var msg = new HttpRequestMessage(HttpMethod.Get, "/api/inscriptions/situation-financiere/ETU-FANTOME-9999");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Isolation_AutreEtablissementNePeutPasAcceder()
    {
        // Arrange
        var etablissementA = await CreerEtablissementAsync("Univ A");
        var etablissementB = await CreerEtablissementAsync("Univ B");

        var (_, matricule) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementA, 500000m, 700000m);

        // Act : Établissement B consulte la situation financière de l'étudiant de l'établissement A
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/inscriptions/situation-financiere/{matricule}");
        msg.Headers.Add("X-EtablissementId", etablissementB.ToString());
        var response = await _http.SendAsync(msg);

        // Assert : 404 car isolé par le tenant filter
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

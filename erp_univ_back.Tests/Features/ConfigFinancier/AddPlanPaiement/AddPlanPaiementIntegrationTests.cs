namespace erp_univ_back.Tests.Features.ConfigFinancier.AddPlanPaiement;

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
using erp_univ_back.Shared.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public class AddPlanPaiementIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public AddPlanPaiementIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "DbTest_AddPlanPaiement_" + Guid.NewGuid().ToString();

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

    private async Task<Guid> CreerEtablissementAsync(string nom = "Univ Plan Test")
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
        var created = await response.Content.ReadFromJsonAsync<CreateAnneeAcademiqueResponse>(_jsonOptions);
        return created!.Id;
    }

    private async Task<Guid> CreerFraisCatalogueAsync(Guid etablissementId, decimal montantTotal = 1200000.00m)
    {
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId);
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeId,
            Categorie: CategorieFrais.FraisFormation,
            Libelle: $"Écolage Annuel {Guid.NewGuid():N}",
            MontantTotal: montantTotal,
            MoyensPaiementAutorises: MoyenPaiement.Especes | MoyenPaiement.MVola
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateCatalogueResponse>(_jsonOptions);
        return created!.Id;
    }

    [Fact]
    public async Task CreerPlanPaiement_DoitRetourner201_QuandPlanEstValide()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var fraisId = await CreerFraisCatalogueAsync(etablissementId, 1200000.00m);

        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: fraisId,
            Nom: "Paiement en 3 Tranches",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Tranche 1 - Rentrée", 400000.00m, DateTime.UtcNow.AddDays(30), true),
                new ModeleTrancheItemRequest(2, "Tranche 2 - Mi-parcours", 400000.00m, DateTime.UtcNow.AddDays(90), false),
                new ModeleTrancheItemRequest(3, "Tranche 3 - Final", 400000.00m, DateTime.UtcNow.AddDays(180), false)
            ]
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/plans-paiement");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<AddPlanPaiementResponse>(_jsonOptions);
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.FraisCatalogueId.Should().Be(fraisId);
        created.Nom.Should().Be("Paiement en 3 Tranches");
        created.NombreTranches.Should().Be(3);
        created.MontantTotal.Should().Be(1200000.00m);
        created.Tranches.Should().HaveCount(3);
        created.Tranches[0].NumeroOrdre.Should().Be(1);
        created.Tranches[0].ExigePourValiderInscription.Should().BeTrue();
    }

    [Fact]
    public async Task CreerPlanPaiement_DoitRetourner400_QuandSommeTranchesNeCorrespondPasAuTotal()
    {
        // Arrange : Total catalogue = 1 200 000, mais les tranches totalisent 1 000 000
        var etablissementId = await CreerEtablissementAsync();
        var fraisId = await CreerFraisCatalogueAsync(etablissementId, 1200000.00m);

        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: fraisId,
            Nom: "Plan Erreur Somme",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Tranche 1", 500000.00m, DateTime.UtcNow.AddDays(30), true),
                new ModeleTrancheItemRequest(2, "Tranche 2", 500000.00m, DateTime.UtcNow.AddDays(90), false)
            ]
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/plans-paiement");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreerPlanPaiement_DoitRetourner404_QuandFraisCatalogueInexistant()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: Guid.NewGuid(),
            Nom: "Plan Sur Frais Inexistant",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Tranche 1", 500000.00m, DateTime.UtcNow.AddDays(30), true)
            ]
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/plans-paiement");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreerPlanPaiement_DoitRetourner409Conflict_QuandMemeNomPlanExistePourCeFrais()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var fraisId = await CreerFraisCatalogueAsync(etablissementId, 1000000.00m);

        var request1 = new AddPlanPaiementRequest(
            FraisCatalogueId: fraisId,
            Nom: "Paiement Comptant",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Totalité", 1000000.00m, DateTime.UtcNow.AddDays(15), true)
            ]
        );

        using var msg1 = new HttpRequestMessage(HttpMethod.Post, "/api/plans-paiement");
        msg1.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg1.Content = JsonContent.Create(request1);
        var res1 = await _http.SendAsync(msg1);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Tente de recréer le même nom de plan (insensible à la casse) pour ce frais
        var request2 = new AddPlanPaiementRequest(
            FraisCatalogueId: fraisId,
            Nom: "  paiement comptant  ",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Totalité", 1000000.00m, DateTime.UtcNow.AddDays(15), true)
            ]
        );

        using var msg2 = new HttpRequestMessage(HttpMethod.Post, "/api/plans-paiement");
        msg2.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg2.Content = JsonContent.Create(request2);

        // Act
        var res2 = await _http.SendAsync(msg2);

        // Assert
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreerPlanPaiement_DoitRetourner400_QuandValidationEchoue()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: Guid.Empty,
            Nom: "",
            Tranches: []
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/plans-paiement");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreerPlanPaiement_DoitIsolerDonnees_EntreEtablissements()
    {
        // Arrange
        var etabA = await CreerEtablissementAsync("Univ A");
        var etabB = await CreerEtablissementAsync("Univ B");

        var fraisA = await CreerFraisCatalogueAsync(etabA, 1000000.00m);

        // Établissement B tente d'ajouter un plan au frais de l'Établissement A
        var request = new AddPlanPaiementRequest(
            FraisCatalogueId: fraisA,
            Nom: "Plan Piraté",
            Tranches:
            [
                new ModeleTrancheItemRequest(1, "Tranche 1", 1000000.00m, DateTime.UtcNow.AddDays(30), true)
            ]
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/plans-paiement");
        msg.Headers.Add("X-EtablissementId", etabB.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert : Etablissement B ne voit pas le frais de l'Etablissement A -> 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}


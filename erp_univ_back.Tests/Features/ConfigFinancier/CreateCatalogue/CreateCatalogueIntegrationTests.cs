namespace erp_univ_back.Tests.Features.ConfigFinancier.CreateCatalogue;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using erp_univ_back.Features.AnneeAcademique.Create;
using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.ConfigFinancier.CreateCatalogue;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Etablissement.Create;
using erp_univ_back.Features.Filiere.Create;
using erp_univ_back.Features.FiliereNiveau.Create;
using erp_univ_back.Features.Niveau;
using erp_univ_back.Features.Niveau.Create;
using erp_univ_back.Shared.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public class CreateCatalogueIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public CreateCatalogueIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "DbTest_CreateCatalogue_" + Guid.NewGuid().ToString();

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

    private async Task<Guid> CreerEtablissementAsync(string nom = "Univ Finance Test")
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

    private async Task<(Guid filiereId, Guid niveauId)> CreerStructureAcademiqueAsync(
        Guid etablissementId, string filiereCode = "INF", string niveauCode = "L1")
    {
        var filiereReq = new CreateFiliereRequest(filiereCode, "Informatique " + filiereCode, null);
        using var filiereMsg = new HttpRequestMessage(HttpMethod.Post, "/api/filieres");
        filiereMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        filiereMsg.Content = JsonContent.Create(filiereReq);
        var filiereRes = await _http.SendAsync(filiereMsg);
        var filiere = await filiereRes.Content.ReadFromJsonAsync<CreateFiliereResponse>(_jsonOptions);

        var niveauReq = new CreateNiveauRequest(niveauCode, "Licence " + niveauCode, 1);
        using var niveauMsg = new HttpRequestMessage(HttpMethod.Post, "/api/niveaux");
        niveauMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        niveauMsg.Content = JsonContent.Create(niveauReq);
        var niveauRes = await _http.SendAsync(niveauMsg);
        var niveau = await niveauRes.Content.ReadFromJsonAsync<CreateNiveauResponse>(_jsonOptions);

        var fnReq = new CreateFiliereNiveauRequest(filiere!.Id, niveau!.Id);
        using var fnMsg = new HttpRequestMessage(HttpMethod.Post, "/api/filiere-niveaux");
        fnMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        fnMsg.Content = JsonContent.Create(fnReq);
        await _http.SendAsync(fnMsg);

        return (filiere.Id, niveau!.Id);
    }

    [Fact]
    public async Task CreerCatalogue_DoitRetourner201_QuandFraisEstGlobal()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId, 2025);

        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeId,
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "Droit d'inscription Général",
            MontantTotal: 150000.00m,
            MoyensPaiementAutorises: MoyenPaiement.Especes | MoyenPaiement.MVola,
            EstObligatoirePourDossier: true
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateCatalogueResponse>(_jsonOptions);
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.Libelle.Should().Be("Droit d'inscription Général");
        created.MontantTotal.Should().Be(150000.00m);
        created.CycleId.Should().BeNull();
        created.FiliereId.Should().BeNull();
        created.NiveauId.Should().BeNull();
        created.AnneeAcademiqueLibelle.Should().Be("2025-2026");
        created.MoyensPaiementAutorises.HasFlag(MoyenPaiement.MVola).Should().BeTrue();
    }

    [Fact]
    public async Task CreerCatalogue_DoitRetourner201_QuandFraisCibleCycleLicence()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId, 2025);

        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeId,
            Categorie: CategorieFrais.FraisFormation,
            Libelle: "Écolage Cycle Licence",
            MontantTotal: 1000000.00m,
            MoyensPaiementAutorises: MoyenPaiement.Especes | MoyenPaiement.MVola,
            CycleId: CycleEntity.LicenceId
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateCatalogueResponse>(_jsonOptions);
        created.Should().NotBeNull();
        created!.CycleId.Should().Be(CycleEntity.LicenceId);
        created.CycleNom.Should().Be("Licence");
    }

    [Fact]
    public async Task CreerCatalogue_DoitRetourner201_QuandFraisCibleFiliereSeule()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId, 2025);
        var (filiereId, _) = await CreerStructureAcademiqueAsync(etablissementId, "GEST", "L1");

        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeId,
            Categorie: CategorieFrais.FraisFormation,
            Libelle: "Frais Spécifiques Filière Gestion",
            MontantTotal: 900000.00m,
            MoyensPaiementAutorises: MoyenPaiement.Especes,
            FiliereId: filiereId
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateCatalogueResponse>(_jsonOptions);
        created.Should().NotBeNull();
        created!.FiliereId.Should().Be(filiereId);
        created.FiliereNom.Should().Contain("GEST");
        created.NiveauId.Should().BeNull();
    }

    [Fact]
    public async Task CreerCatalogue_DoitRetourner201_QuandFraisCibleNiveauSeul()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId, 2025);
        var (_, niveauId) = await CreerStructureAcademiqueAsync(etablissementId, "DROIT", "L1");

        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeId,
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "Droit Entrée Tous L1",
            MontantTotal: 200000.00m,
            MoyensPaiementAutorises: MoyenPaiement.MobileMoney,
            NiveauId: niveauId
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateCatalogueResponse>(_jsonOptions);
        created.Should().NotBeNull();
        created!.NiveauId.Should().Be(niveauId);
        created.FiliereId.Should().BeNull();
    }

    [Fact]
    public async Task CreerCatalogue_DoitRetourner201_QuandFiliereEtNiveauSontTousDeuxFournis()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId, 2025);
        var (filiereId, niveauId) = await CreerStructureAcademiqueAsync(etablissementId, "GL", "L2");

        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeId,
            Categorie: CategorieFrais.FraisFormation,
            Libelle: "Écolage Génie Logiciel L2",
            MontantTotal: 1400000.00m,
            MoyensPaiementAutorises: MoyenPaiement.MobileMoney | MoyenPaiement.VersementBancaire,
            FiliereId: filiereId,
            NiveauId: niveauId,
            EstObligatoirePourDossier: true
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateCatalogueResponse>(_jsonOptions);
        created.Should().NotBeNull();
        created!.FiliereId.Should().Be(filiereId);
        created.NiveauId.Should().Be(niveauId);
        created.MontantTotal.Should().Be(1400000.00m);
    }

    [Fact]
    public async Task CreerCatalogue_DoitRetourner404_QuandFiliereEtNiveauNonAssociesDansEtablissement()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId, 2025);

        // On crée une filière et un niveau distincts sans classe (FiliereNiveau) qui les associe
        var filiereReq = new CreateFiliereRequest("MATH", "Mathématiques", null);
        using var filiereMsg = new HttpRequestMessage(HttpMethod.Post, "/api/filieres");
        filiereMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        filiereMsg.Content = JsonContent.Create(filiereReq);
        var filiereRes = await _http.SendAsync(filiereMsg);
        var filiere = await filiereRes.Content.ReadFromJsonAsync<CreateFiliereResponse>(_jsonOptions);

        var niveauReq = new CreateNiveauRequest("M2", "Master 2", 5);
        using var niveauMsg = new HttpRequestMessage(HttpMethod.Post, "/api/niveaux");
        niveauMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        niveauMsg.Content = JsonContent.Create(niveauReq);
        var niveauRes = await _http.SendAsync(niveauMsg);
        var niveau = await niveauRes.Content.ReadFromJsonAsync<CreateNiveauResponse>(_jsonOptions);

        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeId,
            Categorie: CategorieFrais.FraisFormation,
            Libelle: "Écolage Math M2 Inexistant",
            MontantTotal: 1500000.00m,
            MoyensPaiementAutorises: MoyenPaiement.Especes,
            FiliereId: filiere!.Id,
            NiveauId: niveau!.Id
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert : aucune classe n'associe Math et M2
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreerCatalogue_DoitRetourner409Conflict_QuandMemeConfigurationExiste()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId, 2025);
        var (filiereId, niveauId) = await CreerStructureAcademiqueAsync(etablissementId, "INF", "L1");

        var request1 = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeId,
            Categorie: CategorieFrais.FraisFormation,
            Libelle: "Écolage Annuel",
            MontantTotal: 1200000.00m,
            MoyensPaiementAutorises: MoyenPaiement.Especes,
            FiliereId: filiereId,
            NiveauId: niveauId
        );

        using var msg1 = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg1.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg1.Content = JsonContent.Create(request1);
        var res1 = await _http.SendAsync(msg1);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Tente de recréer un tarif avec le même libellé (insensible à la casse) pour la même configuration
        var request2 = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeId,
            Categorie: CategorieFrais.FraisFormation,
            Libelle: "  écolage annuel  ",
            MontantTotal: 1300000.00m,
            MoyensPaiementAutorises: MoyenPaiement.MVola,
            FiliereId: filiereId,
            NiveauId: niveauId
        );

        using var msg2 = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg2.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg2.Content = JsonContent.Create(request2);

        // Act
        var res2 = await _http.SendAsync(msg2);

        // Assert
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreerCatalogue_DoitRetourner404_QuandAnneeAcademiqueInexistante()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.NewGuid(),
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "Tarif Test",
            MontantTotal: 100000m,
            MoyensPaiementAutorises: MoyenPaiement.Especes
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreerCatalogue_DoitRetourner404_QuandCycleInexistant()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var anneeId = await CreerAnneeAcademiqueAsync(etablissementId, 2025);

        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeId,
            Categorie: CategorieFrais.FraisFormation,
            Libelle: "Frais Cycle Inexistant",
            MontantTotal: 100000m,
            MoyensPaiementAutorises: MoyenPaiement.Especes,
            CycleId: Guid.NewGuid()
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreerCatalogue_DoitRetourner400_QuandValidationEchoue()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: Guid.Empty,
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "",
            MontantTotal: -100m,
            MoyensPaiementAutorises: MoyenPaiement.Aucun
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreerCatalogue_DoitIsolerDonnees_EntreEtablissements()
    {
        // Arrange
        var etabA = await CreerEtablissementAsync("Univ A");
        var etabB = await CreerEtablissementAsync("Univ B");

        var anneeA = await CreerAnneeAcademiqueAsync(etabA, 2025);

        // Établissement B tente d'associer son tarif à l'année académique de l'Établissement A
        var request = new CreateCatalogueRequest(
            AnneeAcademiqueId: anneeA,
            Categorie: CategorieFrais.DroitInscription,
            Libelle: "Tarif Piraté",
            MontantTotal: 100000m,
            MoyensPaiementAutorises: MoyenPaiement.Especes
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        msg.Headers.Add("X-EtablissementId", etabB.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert : Etablissement B ne voit pas l'année de l'Etablissement A -> 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

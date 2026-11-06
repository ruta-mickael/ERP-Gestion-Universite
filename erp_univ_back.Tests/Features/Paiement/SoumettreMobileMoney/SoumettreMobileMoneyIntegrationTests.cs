namespace erp_univ_back.Tests.Features.Paiement.SoumettreMobileMoney;

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
using erp_univ_back.Features.Inscription.Register;
using erp_univ_back.Features.Niveau.Create;
using erp_univ_back.Features.Paiement;
using erp_univ_back.Features.Paiement.SoumettreMobileMoney;
using erp_univ_back.Shared.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public class SoumettreMobileMoneyIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public SoumettreMobileMoneyIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "DbTest_SoumettreMobileMoney_" + Guid.NewGuid().ToString();

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

    private async Task<Guid> CreerEtablissementAsync(string nom = "Univ MM Test")
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
        var resContent = await res.Content.ReadAsStringAsync();
        res.StatusCode.Should().Be(HttpStatusCode.Created, because: resContent);
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
        Guid etablissementId, decimal tranche1 = 600000m, decimal tranche2 = 600000m)
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
            MoyensPaiementAutorises: MoyenPaiement.Especes | MoyenPaiement.MVola | MoyenPaiement.OrangeMoney,
            FiliereId: filiereId,
            NiveauId: niveauId
        );
        using var fraisMsg = new HttpRequestMessage(HttpMethod.Post, "/api/frais-catalogue");
        fraisMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        fraisMsg.Content = JsonContent.Create(fraisReq);
        var fraisRes = await _http.SendAsync(fraisMsg);
        var fraisContent = await fraisRes.Content.ReadAsStringAsync();
        fraisRes.StatusCode.Should().Be(HttpStatusCode.Created, because: fraisContent);
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
        var planContent = await planRes.Content.ReadAsStringAsync();
        planRes.StatusCode.Should().Be(HttpStatusCode.Created, because: planContent);
        var plan = await planRes.Content.ReadFromJsonAsync<AddPlanPaiementResponse>(_jsonOptions);

        // 3. Inscription
        var etudiantInfos = new CreateEtudiantRequest(
            Nom: "RAKOTO",
            Prenom: "Soa",
            DateNaissance: new DateOnly(2002, 5, 10),
            LieuNaissance: "Antananarivo",
            NumeroTelephone: "034" + Random.Shared.Next(1000000, 9999999),
            Email: $"soa_{Guid.NewGuid():N}@gmail.com",
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
        var inscContent = await inscRes.Content.ReadAsStringAsync();
        inscRes.StatusCode.Should().Be(HttpStatusCode.Created, because: inscContent);
        var insc = await inscRes.Content.ReadFromJsonAsync<RegisterInscriptionResponse>(_jsonOptions);

        return (insc!.Id, insc.EtudiantMatricule);
    }

    [Fact]
    public async Task Soumettre_DoitReussir_QuandMatriculeEtMVolaFournis()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var (_, matricule) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);

        var request = new SoumettreMobileMoneyRequest(
            Matricule: matricule,
            Montant: 200000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: "MP251106.1430.B89211",
            DateOperationDeclaree: DateTime.UtcNow.AddMinutes(-5),
            PreuveImageUrl: "https://minio.univ.mg/proofs/mvola_recu_1.png"
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/externe/soumettre");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: content);
        var result = await response.Content.ReadFromJsonAsync<SoumettreMobileMoneyResponse>(_jsonOptions);

        result.Should().NotBeNull();
        result!.TransactionId.Should().NotBeEmpty();
        result.EtudiantMatricule.Should().Be(matricule);
        result.Montant.Should().Be(200000m);
        result.MoyenPaiement.Should().Be(MoyenPaiement.MVola);
        result.ReferenceExterne.Should().Be("MP251106.1430.B89211");
        result.StatutValidation.Should().Be(StatutValidationPaiement.EnAttenteVerification);
        result.PreuveImageUrl.Should().Be("https://minio.univ.mg/proofs/mvola_recu_1.png");
        result.Message.Should().Contain("attente de vérification");
    }

    [Fact]
    public async Task Soumettre_DoitReussir_QuandInscriptionIdEtOrangeMoneyFournis()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var (inscriptionId, matricule) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);

        var request = new SoumettreMobileMoneyRequest(
            InscriptionId: inscriptionId,
            Montant: 300000m,
            MoyenPaiement: MoyenPaiement.OrangeMoney,
            ReferenceExterne: "OM_REF_998877",
            DateOperationDeclaree: DateTime.UtcNow.AddHours(-1)
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/externe/soumettre");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: content);
        var result = await response.Content.ReadFromJsonAsync<SoumettreMobileMoneyResponse>(_jsonOptions);

        result.Should().NotBeNull();
        result!.InscriptionId.Should().Be(inscriptionId);
        result.EtudiantMatricule.Should().Be(matricule);
        result.Montant.Should().Be(300000m);
        result.MoyenPaiement.Should().Be(MoyenPaiement.OrangeMoney);
        result.StatutValidation.Should().Be(StatutValidationPaiement.EnAttenteVerification);
    }

    [Fact]
    public async Task Soumettre_DoitEchouer_Avec409Conflict_QuandMemeReferencePourMemeMoyen()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var (_, matricule) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);

        var refExterne = "MP_DOUBLON_" + Random.Shared.Next(10000, 99999);

        var req1 = new SoumettreMobileMoneyRequest(
            Matricule: matricule,
            Montant: 100000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: refExterne,
            DateOperationDeclaree: DateTime.UtcNow.AddMinutes(-10)
        );

        using (var msg1 = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/externe/soumettre"))
        {
            msg1.Headers.Add("X-EtablissementId", etablissementId.ToString());
            msg1.Content = JsonContent.Create(req1, options: _jsonOptions);
            var resp1 = await _http.SendAsync(msg1);
            var content1 = await resp1.Content.ReadAsStringAsync();
            resp1.StatusCode.Should().Be(HttpStatusCode.OK, because: content1);
        }

        // Act: tentative de soumettre à nouveau la même référence MVola
        var req2 = new SoumettreMobileMoneyRequest(
            Matricule: matricule,
            Montant: 100000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: refExterne,
            DateOperationDeclaree: DateTime.UtcNow.AddMinutes(-5)
        );

        using var msg2 = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/externe/soumettre");
        msg2.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg2.Content = JsonContent.Create(req2, options: _jsonOptions);
        var resp2 = await _http.SendAsync(msg2);

        // Assert
        resp2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Soumettre_DoitAccepter_MemeReferenceSiMoyensDifferents()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var (_, matricule) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);

        var refCommune = "COMMUN_REF_" + Random.Shared.Next(10000, 99999);

        var req1 = new SoumettreMobileMoneyRequest(
            Matricule: matricule,
            Montant: 100000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: refCommune,
            DateOperationDeclaree: DateTime.UtcNow.AddMinutes(-10)
        );

        using (var msg1 = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/externe/soumettre"))
        {
            msg1.Headers.Add("X-EtablissementId", etablissementId.ToString());
            msg1.Content = JsonContent.Create(req1, options: _jsonOptions);
            var resp1 = await _http.SendAsync(msg1);
            var content1 = await resp1.Content.ReadAsStringAsync();
            resp1.StatusCode.Should().Be(HttpStatusCode.OK, because: content1);
        }

        // Act: même référence mais sous OrangeMoney
        var req2 = new SoumettreMobileMoneyRequest(
            Matricule: matricule,
            Montant: 100000m,
            MoyenPaiement: MoyenPaiement.OrangeMoney,
            ReferenceExterne: refCommune,
            DateOperationDeclaree: DateTime.UtcNow.AddMinutes(-5)
        );

        using var msg2 = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/externe/soumettre");
        msg2.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg2.Content = JsonContent.Create(req2, options: _jsonOptions);
        var resp2 = await _http.SendAsync(msg2);

        // Assert
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Soumettre_DoitEchouer_Avec400BadRequest_QuandMontantDepasseDetteRestante()
    {
        // Arrange : dette totale = 1 200 000 Ar
        var etablissementId = await CreerEtablissementAsync();
        var (_, matricule) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);

        var request = new SoumettreMobileMoneyRequest(
            Matricule: matricule,
            Montant: 1500000m, // 1 500 000 > 1 200 000
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: "MP_TROP_999",
            DateOperationDeclaree: DateTime.UtcNow
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/externe/soumettre");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Soumettre_DoitEchouer_Avec404NotFound_QuandEtudiantInexistant()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();

        var request = new SoumettreMobileMoneyRequest(
            Matricule: "ETU-FANTOME-9999",
            Montant: 100000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: "MP_FANTOME_123",
            DateOperationDeclaree: DateTime.UtcNow
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/externe/soumettre");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ObtenirParId_DoitRetournerTransaction_QuandExiste()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var (_, matricule) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);

        var submitReq = new SoumettreMobileMoneyRequest(
            Matricule: matricule,
            Montant: 200000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: "MP_CONSULT_" + Random.Shared.Next(10000, 99999),
            DateOperationDeclaree: DateTime.UtcNow
        );

        using var submitMsg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/externe/soumettre");
        submitMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        submitMsg.Content = JsonContent.Create(submitReq, options: _jsonOptions);
        var submitResp = await _http.SendAsync(submitMsg);
        var created = await submitResp.Content.ReadFromJsonAsync<SoumettreMobileMoneyResponse>(_jsonOptions);

        // Act
        using var getMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/paiements/externe/{created!.TransactionId}");
        getMsg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        var getResp = await _http.SendAsync(getMsg);

        // Assert
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResult = await getResp.Content.ReadFromJsonAsync<SoumettreMobileMoneyResponse>(_jsonOptions);

        getResult.Should().NotBeNull();
        getResult!.TransactionId.Should().Be(created.TransactionId);
        getResult.Montant.Should().Be(200000m);
        getResult.StatutValidation.Should().Be(StatutValidationPaiement.EnAttenteVerification);
        getResult.Message.Should().Contain("attente de vérification");
    }

    [Fact]
    public async Task ObtenirParId_DoitRetourner404_QuandInexistant()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/paiements/externe/{Guid.NewGuid()}");
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

        var (_, matricule) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementA, 600000m, 600000m);

        var submitReq = new SoumettreMobileMoneyRequest(
            Matricule: matricule,
            Montant: 200000m,
            MoyenPaiement: MoyenPaiement.MVola,
            ReferenceExterne: "MP_TENANT_" + Random.Shared.Next(10000, 99999),
            DateOperationDeclaree: DateTime.UtcNow
        );

        using var submitMsg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/externe/soumettre");
        submitMsg.Headers.Add("X-EtablissementId", etablissementA.ToString());
        submitMsg.Content = JsonContent.Create(submitReq, options: _jsonOptions);
        var submitResp = await _http.SendAsync(submitMsg);
        var created = await submitResp.Content.ReadFromJsonAsync<SoumettreMobileMoneyResponse>(_jsonOptions);

        // Act: Établissement B tente d'accéder à la transaction de l'établissement A
        using var getMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/paiements/externe/{created!.TransactionId}");
        getMsg.Headers.Add("X-EtablissementId", etablissementB.ToString());
        var response = await _http.SendAsync(getMsg);

        // Assert: Doit retourner 404 car filtré par le QueryFilter multi-tenant
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

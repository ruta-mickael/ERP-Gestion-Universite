namespace erp_univ_back.Tests.Features.Paiement.EncaisserGuichet;

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
using erp_univ_back.Features.Paiement.EncaisserGuichet;
using erp_univ_back.Shared.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public class EncaisserGuichetIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public EncaisserGuichetIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "DbTest_EncaisserGuichet_" + Guid.NewGuid().ToString();

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

    private async Task<Guid> CreerEtablissementAsync(string nom = "Univ Caisse Test")
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

    private async Task<(Guid filiereId, Guid niveauId, Guid filiereNiveauId)> CreerClasseAsync(
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
        var fnRes = await _http.SendAsync(fnMsg);
        var fn = await fnRes.Content.ReadFromJsonAsync<CreateFiliereNiveauResponse>(_jsonOptions);

        return (filiere.Id, niveau.Id, fn!.Id);
    }

    private async Task<(Guid inscriptionId, RegisterInscriptionResponse response)> InscrireEtudiantAvecPlan2TranchesAsync(
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
            Nom: "RAKOTO",
            Prenom: "Jean",
            DateNaissance: new DateOnly(2002, 5, 10),
            LieuNaissance: "Antananarivo",
            NumeroTelephone: "034" + Random.Shared.Next(1000000, 9999999),
            Email: $"jean_{Guid.NewGuid():N}@gmail.com",
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

        return (insc!.Id, insc);
    }

    [Fact]
    public async Task Encaisser_DoitSolderTranche1EtValiderInscription_QuandMontantEgalATrancheExigee()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var (inscriptionId, inscriptionInitiale) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);

        inscriptionInitiale.Statut.Should().Be(StatutInscription.EnAttentePaiement);

        var request = new EncaisserGuichetRequest(
            InscriptionId: inscriptionId,
            Montant: 600000.00m,
            MoyenPaiement: MoyenPaiement.Especes,
            ReferenceExterne: "RECU-CAISSE-001"
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/guichet/encaisser");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EncaisserGuichetResponse>(_jsonOptions);
        result.Should().NotBeNull();
        result!.MontantVerse.Should().Be(600000.00m);
        result.MoyenPaiement.Should().Be(MoyenPaiement.Especes);
        result.ResteAPayerTotal.Should().Be(600000.00m); // Tranche 2 reste à payer

        // VÉRIFICATION MAJEURE : L'inscription bascule automatiquement à 'Validee' !
        result.StatutInscription.Should().Be(StatutInscription.Validee);

        var tranche1 = result.Echeances.First(e => e.NumeroOrdre == 1);
        tranche1.Statut.Should().Be(StatutEcheance.Solde);
        tranche1.MontantDejaPaye.Should().Be(600000.00m);
        tranche1.ResteAPayer.Should().Be(0m);

        var tranche2 = result.Echeances.First(e => e.NumeroOrdre == 2);
        tranche2.Statut.Should().Be(StatutEcheance.NonPaye);
        tranche2.MontantDejaPaye.Should().Be(0m);
    }

    [Fact]
    public async Task Encaisser_DoitMarquerTranchePartielleEtGarderEnAttente_QuandAcomptePartiel()
    {
        // Arrange : Étudiant doit 600 000 pour la tranche 1, mais ne verse que 400 000 Ar
        var etablissementId = await CreerEtablissementAsync();
        var (inscriptionId, _) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);

        var request = new EncaisserGuichetRequest(
            InscriptionId: inscriptionId,
            Montant: 400000.00m,
            MoyenPaiement: MoyenPaiement.Especes
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/guichet/encaisser");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EncaisserGuichetResponse>(_jsonOptions);
        result.Should().NotBeNull();

        // Inscription reste 'EnAttentePaiement' car la tranche 1 n'est pas encore soldée !
        result!.StatutInscription.Should().Be(StatutInscription.EnAttentePaiement);
        result.ResteAPayerTotal.Should().Be(800000.00m); // 200k sur T1 + 600k sur T2

        var tranche1 = result.Echeances.First(e => e.NumeroOrdre == 1);
        tranche1.Statut.Should().Be(StatutEcheance.Partiel);
        tranche1.MontantDejaPaye.Should().Be(400000.00m);
        tranche1.ResteAPayer.Should().Be(200000.00m);
    }

    [Fact]
    public async Task Encaisser_DoitVentilerSurTranchesMultiples_QuandPaiementDepassePremiereTranche()
    {
        // Arrange : Étudiant verse 800 000 Ar (600k sur Tranche 1 + 200k sur Tranche 2)
        var etablissementId = await CreerEtablissementAsync();
        var (inscriptionId, _) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);

        var request = new EncaisserGuichetRequest(
            InscriptionId: inscriptionId,
            Montant: 800000.00m,
            MoyenPaiement: MoyenPaiement.Especes
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/guichet/encaisser");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EncaisserGuichetResponse>(_jsonOptions);
        result.Should().NotBeNull();
        result!.StatutInscription.Should().Be(StatutInscription.Validee);
        result.ResteAPayerTotal.Should().Be(400000.00m);

        var tranche1 = result.Echeances.First(e => e.NumeroOrdre == 1);
        tranche1.Statut.Should().Be(StatutEcheance.Solde);
        tranche1.MontantDejaPaye.Should().Be(600000.00m);

        var tranche2 = result.Echeances.First(e => e.NumeroOrdre == 2);
        tranche2.Statut.Should().Be(StatutEcheance.Partiel);
        tranche2.MontantDejaPaye.Should().Be(200000.00m);
        tranche2.ResteAPayer.Should().Be(400000.00m);
    }

    [Fact]
    public async Task Encaisser_DoitEchouer400_QuandMontantDepasseDetteTotale()
    {
        // Arrange : Total dû = 1 200 000, mais caissier tente d'encaisser 1 500 000
        var etablissementId = await CreerEtablissementAsync();
        var (inscriptionId, _) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);

        var request = new EncaisserGuichetRequest(
            InscriptionId: inscriptionId,
            Montant: 1500000.00m,
            MoyenPaiement: MoyenPaiement.Especes
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/guichet/encaisser");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Encaisser_DoitEchouer404_QuandInscriptionInexistante()
    {
        // Arrange
        var etablissementId = await CreerEtablissementAsync();
        var request = new EncaisserGuichetRequest(
            InscriptionId: Guid.NewGuid(),
            Montant: 500000m
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/guichet/encaisser");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Encaisser_DoitIsolerDonnees_EntreEtablissements()
    {
        // Arrange
        var etabA = await CreerEtablissementAsync("Univ Caisse A");
        var etabB = await CreerEtablissementAsync("Univ Caisse B");

        var (inscriptionA, _) = await InscrireEtudiantAvecPlan2TranchesAsync(etabA, 600000m, 600000m);

        // Le caissier de l'université B tente d'encaisser l'inscription de l'université A
        var request = new EncaisserGuichetRequest(
            InscriptionId: inscriptionA,
            Montant: 600000m
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/guichet/encaisser");
        msg.Headers.Add("X-EtablissementId", etabB.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert : Inscription de Univ A invisible pour Univ B -> 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Encaisser_DoitReussirDirectementAvecMatriculeSansIdInscription()
    {
        // Arrange : Le caissier n'utilise que le Matricule de l'étudiant
        var etablissementId = await CreerEtablissementAsync();
        var (_, inscriptionInitiale) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);

        var matricule = inscriptionInitiale.EtudiantMatricule;
        matricule.Should().NotBeNullOrWhiteSpace();

        var request = new EncaisserGuichetRequest(
            Matricule: matricule,
            Montant: 600000.00m,
            MoyenPaiement: MoyenPaiement.Especes
        );

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/paiements/guichet/encaisser");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());
        msg.Content = JsonContent.Create(request);

        // Act
        var response = await _http.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EncaisserGuichetResponse>(_jsonOptions);
        result.Should().NotBeNull();
        result!.EtudiantMatricule.Should().Be(matricule);
        result.StatutInscription.Should().Be(StatutInscription.Validee);
        result.Echeances.First(e => e.NumeroOrdre == 1).Statut.Should().Be(StatutEcheance.Solde);
    }

    [Fact]
    public async Task ObtenirSituation_DoitRetournerSituationFinanciereEtEcheances_QuandMatriculeExiste()
    {
        // Arrange : Consultation préalable de l'état financier avant encaissement
        var etablissementId = await CreerEtablissementAsync();
        var (_, inscriptionInitiale) = await InscrireEtudiantAvecPlan2TranchesAsync(etablissementId, 600000m, 600000m);
        var matricule = inscriptionInitiale.EtudiantMatricule;

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/paiements/guichet/situation/{matricule}");
        msg.Headers.Add("X-EtablissementId", etablissementId.ToString());

        // Act
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
}


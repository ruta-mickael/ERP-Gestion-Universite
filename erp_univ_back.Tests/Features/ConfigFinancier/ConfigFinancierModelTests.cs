namespace erp_univ_back.Tests.Features.ConfigFinancier;

using erp_univ_back.Features.AnneeAcademique;
using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Etudiant;
using erp_univ_back.Features.FiliereNiveau;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Niveau;
using erp_univ_back.Features.Paiement;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ConfigFinancierModelTests
{
    private class FakeTenantService : ITenantService
    {
        public Guid? EtablissementId { get; set; } = Guid.NewGuid();
        public void DefinirEtablissement(Guid id) => EtablissementId = id;
        public Guid ObtenirEtablissementId() => EtablissementId ?? Guid.Empty;
    }

    private static ErpUnivDbContext CreerDbContext(FakeTenantService tenantService, string dbName)
    {
        var options = new DbContextOptionsBuilder<ErpUnivDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new ErpUnivDbContext(options, tenantService);
    }

    [Fact]
    public async Task ModeleFinancier_DoitPersisterEtFiltrerParTenant()
    {
        // Arrange
        var tenantService = new FakeTenantService();
        var dbName = "DbTest_ConfigFinancier_" + Guid.NewGuid();

        await using var db = CreerDbContext(tenantService, dbName);

        var etabId = tenantService.EtablissementId!.Value;
        var etab = new EtablissementEntity
        {
            Id = etabId,
            Nom = "Université Test Finance",
            Adresse = "Rue 1",
            CodePostal = "101",
            Ville = "Tana",
            Pays = "MG",
            Telephone = "0340000001",
            Email = "univ@test.mg",
            Statut = StatusEtablissement.Actif
        };
        db.Etablissements.Add(etab);

        var annee = new AnneeAcademiqueEntity
        {
            Id = Guid.NewGuid(),
            EtablissementId = etabId,
            Libelle = "2025-2026",
            DateDebut = new DateOnly(2025, 9, 1),
            DateFin = new DateOnly(2026, 6, 30),
            EstActive = true
        };
        db.AnneeAcademiques.Add(annee);

        // 1. FraisCatalogue
        var frais = new FraisCatalogueEntity
        {
            Id = Guid.NewGuid(),
            EtablissementId = etabId,
            AnneeAcademiqueId = annee.Id,
            CycleId = CycleEntity.LicenceId,
            Categorie = CategorieFrais.FraisFormation,
            Libelle = "Écolage Annuel L1",
            MontantTotal = 1200000.00m,
            MoyensPaiementAutorises = MoyenPaiement.Especes | MoyenPaiement.MVola | MoyenPaiement.VersementBancaire,
            EstObligatoirePourDossier = true
        };
        db.FraisCatalogues.Add(frais);

        // 2. PlanPaiement
        var plan = new PlanPaiementEntity
        {
            Id = Guid.NewGuid(),
            EtablissementId = etabId,
            FraisCatalogueId = frais.Id,
            Nom = "Paiement en 3 Tranches",
            NombreTranches = 3,
            EstActif = true
        };
        db.PlansPaiement.Add(plan);

        // 3. ModeleTranche
        var tranche1 = new ModeleTrancheEntity
        {
            Id = Guid.NewGuid(),
            EtablissementId = etabId,
            PlanPaiementId = plan.Id,
            NumeroOrdre = 1,
            Libelle = "Tranche 1 - Inscription",
            Montant = 400000.00m,
            DateLimite = DateTime.UtcNow.AddDays(30),
            ExigePourValiderInscription = true
        };
        db.ModelesTranches.Add(tranche1);

        // 4. Inscription & Echeance
        var etudiant = new EtudiantEntity
        {
            Id = Guid.NewGuid(),
            EtablissementId = etabId,
            Matricule = "ETU-2025-001",
            Nom = "Rakoto",
            DateNaissance = new DateOnly(2002, 5, 10),
            LieuNaissance = "Antananarivo",
            NumeroTelephone = "0340000002",
            Email = "rakoto@test.mg"
        };
        db.Etudiants.Add(etudiant);

        var inscription = new InscriptionEntity
        {
            Id = Guid.NewGuid(),
            EtablissementId = etabId,
            EtudiantId = etudiant.Id,
            AnneeAcademiqueId = annee.Id,
            FiliereNiveauId = Guid.NewGuid(),
            TypeInscription = TypeInscription.PremiereInscription,
            Statut = StatutInscription.EnAttentePaiement
        };
        db.Inscriptions.Add(inscription);

        var echeance = new EcheanceEtudiantEntity
        {
            Id = Guid.NewGuid(),
            EtablissementId = etabId,
            InscriptionId = inscription.Id,
            FraisCatalogueId = frais.Id,
            NumeroOrdre = 1,
            Libelle = "Tranche 1",
            MontantAttendu = 400000.00m,
            MontantDejaPaye = 0m,
            DateLimite = DateTime.UtcNow.AddDays(30),
            ExigePourValiderInscription = true,
            Statut = StatutEcheance.NonPaye
        };
        db.EcheancesEtudiant.Add(echeance);

        // 5. TransactionPaiement
        var transaction = new TransactionPaiementEntity
        {
            Id = Guid.NewGuid(),
            EtablissementId = etabId,
            InscriptionId = inscription.Id,
            EtudiantId = etudiant.Id,
            Montant = 400000.00m,
            MoyenPaiement = MoyenPaiement.MVola,
            ReferenceExterne = "MVOLA-2025-123456",
            DateOperationDeclaree = DateTime.UtcNow,
            StatutValidation = StatutValidationPaiement.EnAttenteVerification
        };
        db.TransactionsPaiement.Add(transaction);

        await db.SaveChangesAsync();

        // Assert - même tenant voit toutes les entités
        var fraisDb = await db.FraisCatalogues.FirstOrDefaultAsync(f => f.Id == frais.Id);
        fraisDb.Should().NotBeNull();
        fraisDb!.MontantTotal.Should().Be(1200000.00m);
        fraisDb.MoyensPaiementAutorises.HasFlag(MoyenPaiement.MVola).Should().BeTrue();
        fraisDb.MoyensPaiementAutorises.HasFlag(MoyenPaiement.Especes).Should().BeTrue();
        fraisDb.MoyensPaiementAutorises.HasFlag(MoyenPaiement.AirtelMoney).Should().BeFalse();

        var planDb = await db.PlansPaiement.FirstOrDefaultAsync(p => p.Id == plan.Id);
        planDb.Should().NotBeNull();
        planDb!.NombreTranches.Should().Be(3);

        var trancheDb = await db.ModelesTranches.FirstOrDefaultAsync(t => t.Id == tranche1.Id);
        trancheDb.Should().NotBeNull();
        trancheDb!.Montant.Should().Be(400000.00m);
        trancheDb.ExigePourValiderInscription.Should().BeTrue();

        var echeanceDb = await db.EcheancesEtudiant.FirstOrDefaultAsync(e => e.Id == echeance.Id);
        echeanceDb.Should().NotBeNull();
        echeanceDb!.Statut.Should().Be(StatutEcheance.NonPaye);

        var transactionDb = await db.TransactionsPaiement.FirstOrDefaultAsync(t => t.Id == transaction.Id);
        transactionDb.Should().NotBeNull();
        transactionDb!.ReferenceExterne.Should().Be("MVOLA-2025-123456");
        transactionDb.StatutValidation.Should().Be(StatutValidationPaiement.EnAttenteVerification);

        // Assert - isolation multi-tenant
        tenantService.EtablissementId = Guid.NewGuid(); // Changement de tenant
        await using var dbAutreTenant = CreerDbContext(tenantService, dbName);

        var fraisAutreTenant = await dbAutreTenant.FraisCatalogues.FirstOrDefaultAsync(f => f.Id == frais.Id);
        fraisAutreTenant.Should().BeNull();

        var transactionAutreTenant = await dbAutreTenant.TransactionsPaiement.FirstOrDefaultAsync(t => t.Id == transaction.Id);
        transactionAutreTenant.Should().BeNull();
    }
}

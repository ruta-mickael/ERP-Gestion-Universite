namespace erp_univ_back.Shared.Data;

using Microsoft.EntityFrameworkCore;
using erp_univ_back.Features.AnneeAcademique;
using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Features.Etablissement;
using erp_univ_back.Features.Etudiant;
using erp_univ_back.Features.Filiere;
using erp_univ_back.Features.FiliereNiveau;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Features.Matiere;
using erp_univ_back.Features.Niveau;
using erp_univ_back.Features.Paiement;
using erp_univ_back.Features.Professeur;
using erp_univ_back.Features.UniteEnseignement;
using erp_univ_back.Shared.Entities;
using erp_univ_back.Shared.Services;

public class ErpUnivDbContext(DbContextOptions<ErpUnivDbContext> options, ITenantService _tenantService) : DbContext(options)
{
    public DbSet<EtablissementEntity> Etablissements { get; set; } = null!;
    public DbSet<EtudiantEntity> Etudiants { get; set; } = null!;
    public DbSet<EtudiantParentEntity> EtudiantParents { get; set; } = null!;
    public DbSet<ParentEntity> Parents { get; set; } = null!;
    public DbSet<ProfesseurEntity> Professeurs { get; set; } = null!;
    public DbSet<AnneeAcademiqueEntity> AnneeAcademiques { get; set; } = null!;
    public DbSet<FiliereEntity> Filieres { get; set; } = null!;
    public DbSet<CycleEntity> Cycles { get; set; } = null!;
    public DbSet<NiveauEntity> Niveaux { get; set; } = null!;
    public DbSet<FiliereNiveauEntity> FiliereNiveaux { get; set; } = null!;
    public DbSet<InscriptionEntity> Inscriptions { get; set; } = null!;
    public DbSet<UniteEnseignementEntity> UniteEnseignements { get; set; } = null!;
    public DbSet<MatiereEntity> Matieres { get; set; } = null!;

    // --- Module Financier & Paiement ---
    public DbSet<FraisCatalogueEntity> FraisCatalogues { get; set; } = null!;
    public DbSet<PlanPaiementEntity> PlansPaiement { get; set; } = null!;
    public DbSet<ModeleTrancheEntity> ModelesTranches { get; set; } = null!;
    public DbSet<EcheanceEtudiantEntity> EcheancesEtudiant { get; set; } = null!;
    public DbSet<TransactionPaiementEntity> TransactionsPaiement { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // --- 1. Établissement ---
        mb.Entity<EtablissementEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Nom).IsUnique();
            e.Property(x => x.Statut).HasConversion<string>().HasMaxLength(20);
        });

        // --- 2. Étudiant ---
        mb.Entity<EtudiantEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.HasIndex(x => new { x.EtablissementId, x.Matricule }).IsUnique();
            e.HasIndex(x => new { x.EtablissementId, x.NumeroCin }).IsUnique();
            e.HasIndex(x => new { x.EtablissementId, x.NumeroTelephone }).IsUnique();

            e.Property(x => x.NumeroTelephone)
                .HasConversion<string>()
                .HasMaxLength(20);

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.Etudiants)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 3. Professeur ---
        mb.Entity<ProfesseurEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.HasIndex(x => new { x.EtablissementId, x.Matricule }).IsUnique();
            e.HasIndex(x => new { x.EtablissementId, x.NumeroCin }).IsUnique();
            e.HasIndex(x => new { x.EtablissementId, x.NumeroTelephone }).IsUnique();

            e.Property(x => x.NumeroTelephone)
                .HasConversion<string>()
                .HasMaxLength(20);

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.Professeurs)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 4. Parent ---
        mb.Entity<ParentEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.HasIndex(x => new { x.EtablissementId, x.NumeroTelphone }).IsUnique();
            e.Property(x => x.NumeroTelphone).HasMaxLength(20);

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.Parents)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 5. Table de jonction Étudiant <-> Parent ---
        mb.Entity<EtudiantParentEntity>(e =>
        {
            e.HasKey(x => x.Id);

            // Empêche d'associer deux fois le même parent au même étudiant
            e.HasIndex(x => new { x.EtudiantId, x.ParentId }).IsUnique();

            e.HasOne(x => x.Etudiant)
                .WithMany(x => x.EtudiantParents)
                .HasForeignKey(x => x.EtudiantId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Parent)
                .WithMany(x => x.EtudiantParents)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        // --- 6. Année Académique ---
        mb.Entity<AnneeAcademiqueEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.HasIndex(x => new { x.EtablissementId, x.Libelle }).IsUnique();

            e.Property(x => x.Libelle).HasMaxLength(15).IsRequired();

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.AnneeAcademiques)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 7. Filière ---
        mb.Entity<FiliereEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Nom).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(250);

            e.HasIndex(x => new { x.EtablissementId, x.Code }).IsUnique();

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.Filieres)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 8. Cycle ---
        mb.Entity<CycleEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Nom).HasMaxLength(100).IsRequired();
            e.Property(x => x.Ordre).IsRequired();

            e.HasIndex(x => x.Code).IsUnique();

            e.HasData(
                new CycleEntity
                {
                    Id = CycleEntity.LicenceId,
                    Code = "LICENCE",
                    Nom = "Licence",
                    Ordre = 1,
                    DateCreation = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    DateModification = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new CycleEntity
                {
                    Id = CycleEntity.MasterId,
                    Code = "MASTER",
                    Nom = "Master",
                    Ordre = 2,
                    DateCreation = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    DateModification = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new CycleEntity
                {
                    Id = CycleEntity.DoctoratId,
                    Code = "DOCTORAT",
                    Nom = "Doctorat",
                    Ordre = 3,
                    DateCreation = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    DateModification = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });

        // --- 9. Niveau ---
        mb.Entity<NiveauEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Nom).HasMaxLength(100).IsRequired();
            e.Property(x => x.Ordre).IsRequired();

            e.HasIndex(x => new { x.EtablissementId, x.Code }).IsUnique();

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.Niveaux)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Cycle)
                .WithMany(x => x.Niveaux)
                .HasForeignKey(x => x.CycleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 9. Filière-Niveau (Classe) ---
        mb.Entity<FiliereNiveauEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.Property(x => x.LibelleAffiche).HasMaxLength(150);

            e.HasIndex(x => new { x.EtablissementId, x.FiliereId, x.NiveauId }).IsUnique();

            e.HasOne(x => x.Filiere)
                .WithMany(x => x.FiliereNiveaux)
                .HasForeignKey(x => x.FiliereId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Niveau)
                .WithMany(x => x.FiliereNiveaux)
                .HasForeignKey(x => x.NiveauId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.FiliereNiveaux)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 10. Inscription ---
        mb.Entity<InscriptionEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.Property(x => x.Statut)
                .HasConversion<string>()
                .HasMaxLength(40);

            e.Property(x => x.TypeInscription)
                .HasConversion<string>()
                .HasMaxLength(40);

            // Index unique composé anti-doublon (EtablissementId, EtudiantId, FiliereNiveauId, AnneeAcademiqueId)
            e.HasIndex(x => new { x.EtablissementId, x.EtudiantId, x.FiliereNiveauId, x.AnneeAcademiqueId }).IsUnique();

            e.HasOne(x => x.Etudiant)
                .WithMany(x => x.Inscriptions)
                .HasForeignKey(x => x.EtudiantId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.FiliereNiveau)
                .WithMany(x => x.Inscriptions)
                .HasForeignKey(x => x.FiliereNiveauId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AnneeAcademique)
                .WithMany(x => x.Inscriptions)
                .HasForeignKey(x => x.AnneeAcademiqueId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.Inscriptions)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 11. Unité d'Enseignement ---
        mb.Entity<UniteEnseignementEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Nom).HasMaxLength(150).IsRequired();
            e.Property(x => x.Credit).IsRequired();
            e.Property(x => x.Semestre).IsRequired();

            e.HasIndex(x => new { x.EtablissementId, x.Code }).IsUnique();

            e.HasOne(x => x.FiliereNiveau)
                .WithMany(x => x.UniteEnseignements)
                .HasForeignKey(x => x.FiliereNiveauId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.UniteEnseignements)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 12. Matières ---
        mb.Entity<MatiereEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Nom).HasMaxLength(150).IsRequired();
            e.Property(x => x.Coefficient).IsRequired();

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.Matieres)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UniteEnseignement)
                .WithMany(x => x.Matieres)
                .HasForeignKey(x => x.UniteEnseignementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 13. Frais Catalogue ---
        mb.Entity<FraisCatalogueEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.Property(x => x.Libelle).HasMaxLength(150).IsRequired();
            e.Property(x => x.MontantTotal).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Categorie).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.MoyensPaiementAutorises).HasConversion<int>();

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.FraisCatalogues)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AnneeAcademique)
                .WithMany(x => x.FraisCatalogues)
                .HasForeignKey(x => x.AnneeAcademiqueId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Cycle)
                .WithMany(x => x.FraisCatalogues)
                .HasForeignKey(x => x.CycleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Filiere)
                .WithMany(x => x.FraisCatalogues)
                .HasForeignKey(x => x.FiliereId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Niveau)
                .WithMany(x => x.FraisCatalogues)
                .HasForeignKey(x => x.NiveauId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 14. Plans Paiement ---
        mb.Entity<PlanPaiementEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.Property(x => x.Nom).HasMaxLength(100).IsRequired();
            e.Property(x => x.NombreTranches).IsRequired();
            e.Property(x => x.EstActif).HasDefaultValue(true);

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.PlansPaiement)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.FraisCatalogue)
                .WithMany(x => x.PlansPaiement)
                .HasForeignKey(x => x.FraisCatalogueId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 15. Modèles Tranches ---
        mb.Entity<ModeleTrancheEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.Property(x => x.NumeroOrdre).IsRequired();
            e.Property(x => x.Libelle).HasMaxLength(150).IsRequired();
            e.Property(x => x.Montant).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.DateLimite).IsRequired();

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.ModelesTranches)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.PlanPaiement)
                .WithMany(x => x.ModelesTranches)
                .HasForeignKey(x => x.PlanPaiementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 16. Échéances Étudiant ---
        mb.Entity<EcheanceEtudiantEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.Property(x => x.NumeroOrdre).IsRequired();
            e.Property(x => x.Libelle).HasMaxLength(150).IsRequired();
            e.Property(x => x.MontantAttendu).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.MontantDejaPaye).HasColumnType("decimal(18,2)").HasDefaultValue(0);
            e.Property(x => x.DateLimite).IsRequired();
            e.Property(x => x.Statut).HasConversion<string>().HasMaxLength(40);

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.EcheancesEtudiant)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Inscription)
                .WithMany(x => x.Echeances)
                .HasForeignKey(x => x.InscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.FraisCatalogue)
                .WithMany(x => x.Echeances)
                .HasForeignKey(x => x.FraisCatalogueId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- 17. Transactions Paiement ---
        mb.Entity<TransactionPaiementEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.EtablissementId == _tenantService.EtablissementId);

            e.Property(x => x.Montant).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.MoyenPaiement).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.NomBanque).HasMaxLength(50);
            e.Property(x => x.ReferenceExterne).HasMaxLength(100);
            e.Property(x => x.DateOperationDeclaree).IsRequired();
            e.Property(x => x.PreuveImageUrl).HasMaxLength(500);
            e.Property(x => x.StatutValidation).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.MotifRejet).HasMaxLength(300);

            // Index composite anti-fraude avec filtre excluant les ReferenceExterne nulles
            e.HasIndex(x => new { x.EtablissementId, x.MoyenPaiement, x.NomBanque, x.ReferenceExterne })
                .IsUnique()
                .HasFilter("\"ReferenceExterne\" IS NOT NULL");

            e.HasOne(x => x.Etablissement)
                .WithMany(x => x.TransactionsPaiement)
                .HasForeignKey(x => x.EtablissementId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Inscription)
                .WithMany(x => x.TransactionsPaiement)
                .HasForeignKey(x => x.InscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Etudiant)
                .WithMany(x => x.TransactionsPaiement)
                .HasForeignKey(x => x.EtudiantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.DateCreation = DateTime.UtcNow;
                entry.Entity.DateModification = DateTime.UtcNow;

                var hasEtablissementId = entry.Properties.Any(p => p.Metadata.Name == "EtablissementId");

                if (hasEtablissementId)
                {
                    entry.Property("EtablissementId").CurrentValue = _tenantService.ObtenirEtablissementId();
                }
            }
            else
            {
                entry.Entity.DateModification = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}

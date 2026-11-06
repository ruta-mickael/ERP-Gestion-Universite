using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp_univ_back.Migrations
{
    /// <inheritdoc />
    public partial class AjoutModulePaiement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "niveaux",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Statut",
                table: "inscriptions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "TypeInscription",
                table: "inscriptions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Nom",
                table: "filieres",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "filieres",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "etudiant_parent",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.CreateTable(
                name: "frais_catalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtablissementId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnneeAcademiqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiliereNiveauId = table.Column<Guid>(type: "uuid", nullable: true),
                    Categorie = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Cible = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Libelle = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MontantTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MoyensPaiementAutorises = table.Column<int>(type: "integer", nullable: false),
                    EstObligatoirePourDossier = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_frais_catalogue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_frais_catalogue_AnneeAcademiques_AnneeAcademiqueId",
                        column: x => x.AnneeAcademiqueId,
                        principalTable: "AnneeAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_frais_catalogue_etablissements_EtablissementId",
                        column: x => x.EtablissementId,
                        principalTable: "etablissements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_frais_catalogue_filiere_niveaux_FiliereNiveauId",
                        column: x => x.FiliereNiveauId,
                        principalTable: "filiere_niveaux",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transactions_paiement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtablissementId = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudiantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MoyenPaiement = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NomBanque = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenceExterne = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateOperationDeclaree = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreuveImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DonneesExtraitesIaJson = table.Column<string>(type: "text", nullable: true),
                    StatutValidation = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    VerifieParUtilisateurId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateValidation = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MotifRejet = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions_paiement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transactions_paiement_etablissements_EtablissementId",
                        column: x => x.EtablissementId,
                        principalTable: "etablissements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_paiement_etudiants_EtudiantId",
                        column: x => x.EtudiantId,
                        principalTable: "etudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_paiement_inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "echeances_etudiant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtablissementId = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FraisCatalogueId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroOrdre = table.Column<int>(type: "integer", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MontantAttendu = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontantDejaPaye = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    DateLimite = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExigePourValiderInscription = table.Column<bool>(type: "boolean", nullable: false),
                    Statut = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_echeances_etudiant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_echeances_etudiant_etablissements_EtablissementId",
                        column: x => x.EtablissementId,
                        principalTable: "etablissements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_echeances_etudiant_frais_catalogue_FraisCatalogueId",
                        column: x => x.FraisCatalogueId,
                        principalTable: "frais_catalogue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_echeances_etudiant_inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plans_paiement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtablissementId = table.Column<Guid>(type: "uuid", nullable: false),
                    FraisCatalogueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NombreTranches = table.Column<int>(type: "integer", nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans_paiement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plans_paiement_etablissements_EtablissementId",
                        column: x => x.EtablissementId,
                        principalTable: "etablissements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plans_paiement_frais_catalogue_FraisCatalogueId",
                        column: x => x.FraisCatalogueId,
                        principalTable: "frais_catalogue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "modeles_tranches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtablissementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanPaiementId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroOrdre = table.Column<int>(type: "integer", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DateLimite = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExigePourValiderInscription = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modeles_tranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_modeles_tranches_etablissements_EtablissementId",
                        column: x => x.EtablissementId,
                        principalTable: "etablissements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_modeles_tranches_plans_paiement_PlanPaiementId",
                        column: x => x.PlanPaiementId,
                        principalTable: "plans_paiement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_echeances_etudiant_EtablissementId",
                table: "echeances_etudiant",
                column: "EtablissementId");

            migrationBuilder.CreateIndex(
                name: "IX_echeances_etudiant_FraisCatalogueId",
                table: "echeances_etudiant",
                column: "FraisCatalogueId");

            migrationBuilder.CreateIndex(
                name: "IX_echeances_etudiant_InscriptionId",
                table: "echeances_etudiant",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_frais_catalogue_AnneeAcademiqueId",
                table: "frais_catalogue",
                column: "AnneeAcademiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_frais_catalogue_EtablissementId",
                table: "frais_catalogue",
                column: "EtablissementId");

            migrationBuilder.CreateIndex(
                name: "IX_frais_catalogue_FiliereNiveauId",
                table: "frais_catalogue",
                column: "FiliereNiveauId");

            migrationBuilder.CreateIndex(
                name: "IX_modeles_tranches_EtablissementId",
                table: "modeles_tranches",
                column: "EtablissementId");

            migrationBuilder.CreateIndex(
                name: "IX_modeles_tranches_PlanPaiementId",
                table: "modeles_tranches",
                column: "PlanPaiementId");

            migrationBuilder.CreateIndex(
                name: "IX_plans_paiement_EtablissementId",
                table: "plans_paiement",
                column: "EtablissementId");

            migrationBuilder.CreateIndex(
                name: "IX_plans_paiement_FraisCatalogueId",
                table: "plans_paiement",
                column: "FraisCatalogueId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_paiement_EtablissementId_MoyenPaiement_NomBanq~",
                table: "transactions_paiement",
                columns: new[] { "EtablissementId", "MoyenPaiement", "NomBanque", "ReferenceExterne" },
                unique: true,
                filter: "\"ReferenceExterne\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_paiement_EtudiantId",
                table: "transactions_paiement",
                column: "EtudiantId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_paiement_InscriptionId",
                table: "transactions_paiement",
                column: "InscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "echeances_etudiant");

            migrationBuilder.DropTable(
                name: "modeles_tranches");

            migrationBuilder.DropTable(
                name: "transactions_paiement");

            migrationBuilder.DropTable(
                name: "plans_paiement");

            migrationBuilder.DropTable(
                name: "frais_catalogue");

            migrationBuilder.DropColumn(
                name: "TypeInscription",
                table: "inscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "niveaux",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Statut",
                table: "inscriptions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "Nom",
                table: "filieres",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "filieres",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "etudiant_parent",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}

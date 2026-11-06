using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace erp_univ_back.Migrations
{
    /// <inheritdoc />
    public partial class AjoutCyclesEtCiblageFraisCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_frais_catalogue_filiere_niveaux_FiliereNiveauId",
                table: "frais_catalogue");

            migrationBuilder.DropColumn(
                name: "Cible",
                table: "frais_catalogue");

            migrationBuilder.RenameColumn(
                name: "FiliereNiveauId",
                table: "frais_catalogue",
                newName: "NiveauId");

            migrationBuilder.RenameIndex(
                name: "IX_frais_catalogue_FiliereNiveauId",
                table: "frais_catalogue",
                newName: "IX_frais_catalogue_NiveauId");

            migrationBuilder.AddColumn<Guid>(
                name: "CycleId",
                table: "niveaux",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CycleId",
                table: "frais_catalogue",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FiliereId",
                table: "frais_catalogue",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cycles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "cycles",
                columns: new[] { "Id", "Code", "DateCreation", "DateModification", "Nom", "Ordre" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "LICENCE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Licence", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "MASTER", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Master", 2 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "DOCTORAT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doctorat", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_niveaux_CycleId",
                table: "niveaux",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_frais_catalogue_CycleId",
                table: "frais_catalogue",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_frais_catalogue_FiliereId",
                table: "frais_catalogue",
                column: "FiliereId");

            migrationBuilder.CreateIndex(
                name: "IX_cycles_Code",
                table: "cycles",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_frais_catalogue_cycles_CycleId",
                table: "frais_catalogue",
                column: "CycleId",
                principalTable: "cycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_frais_catalogue_filieres_FiliereId",
                table: "frais_catalogue",
                column: "FiliereId",
                principalTable: "filieres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_frais_catalogue_niveaux_NiveauId",
                table: "frais_catalogue",
                column: "NiveauId",
                principalTable: "niveaux",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_niveaux_cycles_CycleId",
                table: "niveaux",
                column: "CycleId",
                principalTable: "cycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_frais_catalogue_cycles_CycleId",
                table: "frais_catalogue");

            migrationBuilder.DropForeignKey(
                name: "FK_frais_catalogue_filieres_FiliereId",
                table: "frais_catalogue");

            migrationBuilder.DropForeignKey(
                name: "FK_frais_catalogue_niveaux_NiveauId",
                table: "frais_catalogue");

            migrationBuilder.DropForeignKey(
                name: "FK_niveaux_cycles_CycleId",
                table: "niveaux");

            migrationBuilder.DropTable(
                name: "cycles");

            migrationBuilder.DropIndex(
                name: "IX_niveaux_CycleId",
                table: "niveaux");

            migrationBuilder.DropIndex(
                name: "IX_frais_catalogue_CycleId",
                table: "frais_catalogue");

            migrationBuilder.DropIndex(
                name: "IX_frais_catalogue_FiliereId",
                table: "frais_catalogue");

            migrationBuilder.DropColumn(
                name: "CycleId",
                table: "niveaux");

            migrationBuilder.DropColumn(
                name: "CycleId",
                table: "frais_catalogue");

            migrationBuilder.DropColumn(
                name: "FiliereId",
                table: "frais_catalogue");

            migrationBuilder.RenameColumn(
                name: "NiveauId",
                table: "frais_catalogue",
                newName: "FiliereNiveauId");

            migrationBuilder.RenameIndex(
                name: "IX_frais_catalogue_NiveauId",
                table: "frais_catalogue",
                newName: "IX_frais_catalogue_FiliereNiveauId");

            migrationBuilder.AddColumn<string>(
                name: "Cible",
                table: "frais_catalogue",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_frais_catalogue_filiere_niveaux_FiliereNiveauId",
                table: "frais_catalogue",
                column: "FiliereNiveauId",
                principalTable: "filiere_niveaux",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

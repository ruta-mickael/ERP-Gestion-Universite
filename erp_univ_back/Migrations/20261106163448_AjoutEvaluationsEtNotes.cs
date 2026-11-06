using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp_univ_back.Migrations
{
    /// <inheritdoc />
    public partial class AjoutEvaluationsEtNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TypeEvaluation = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Session = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DateEvaluation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Bareme = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 20.00m),
                    Poids = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 1.00m),
                    MatiereId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiliereNiveauId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnneeAcademiqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfesseurId = table.Column<Guid>(type: "uuid", nullable: true),
                    EtablissementId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluations_AnneeAcademiques_AnneeAcademiqueId",
                        column: x => x.AnneeAcademiqueId,
                        principalTable: "AnneeAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evaluations_Matieres_MatiereId",
                        column: x => x.MatiereId,
                        principalTable: "Matieres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evaluations_etablissements_EtablissementId",
                        column: x => x.EtablissementId,
                        principalTable: "etablissements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evaluations_filiere_niveaux_FiliereNiveauId",
                        column: x => x.FiliereNiveauId,
                        principalTable: "filiere_niveaux",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evaluations_professeurs_ProfesseurId",
                        column: x => x.ProfesseurId,
                        principalTable: "professeurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudiantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Valeur = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    EstAbsent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Commentaire = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    EtablissementId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notes_etablissements_EtablissementId",
                        column: x => x.EtablissementId,
                        principalTable: "etablissements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notes_etudiants_EtudiantId",
                        column: x => x.EtudiantId,
                        principalTable: "etudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notes_evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notes_inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_AnneeAcademiqueId",
                table: "evaluations",
                column: "AnneeAcademiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_EtablissementId_MatiereId_FiliereNiveauId_Annee~",
                table: "evaluations",
                columns: new[] { "EtablissementId", "MatiereId", "FiliereNiveauId", "AnneeAcademiqueId" });

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_FiliereNiveauId",
                table: "evaluations",
                column: "FiliereNiveauId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_MatiereId",
                table: "evaluations",
                column: "MatiereId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_ProfesseurId",
                table: "evaluations",
                column: "ProfesseurId");

            migrationBuilder.CreateIndex(
                name: "IX_notes_EtablissementId_EvaluationId_EtudiantId",
                table: "notes",
                columns: new[] { "EtablissementId", "EvaluationId", "EtudiantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notes_EtudiantId",
                table: "notes",
                column: "EtudiantId");

            migrationBuilder.CreateIndex(
                name: "IX_notes_EvaluationId",
                table: "notes",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_notes_InscriptionId",
                table: "notes",
                column: "InscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notes");

            migrationBuilder.DropTable(
                name: "evaluations");
        }
    }
}

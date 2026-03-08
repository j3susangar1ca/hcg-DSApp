using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionDocumental.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Catalogos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Seccion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Serie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Subserie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlazoConservacionAnios = table.Column<int>(type: "integer", nullable: false),
                    IsActivo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalogos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FolioOficial = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Remitente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Asunto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RutaRedActual = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HashCriptografico = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EstatusAccion = table.Column<int>(type: "integer", nullable: false),
                    FaseCicloVida = table.Column<int>(type: "integer", nullable: false),
                    IsUrgente = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CadidoId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_Catalogos_CadidoId",
                        column: x => x.CadidoId,
                        principalTable: "Catalogos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Bitacoras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FaseAnterior = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FaseNueva = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaTransaccion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DescripcionEvento = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bitacoras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bitacoras_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bitacoras_DocumentoId",
                table: "Bitacoras",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_CadidoId",
                table: "Documentos",
                column: "CadidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_FolioOficial",
                table: "Documentos",
                column: "FolioOficial",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bitacoras");

            migrationBuilder.DropTable(
                name: "Documentos");

            migrationBuilder.DropTable(
                name: "Catalogos");
        }
    }
}

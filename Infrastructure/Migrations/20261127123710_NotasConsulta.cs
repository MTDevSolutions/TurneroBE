using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NotasConsulta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotasConsulta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PacienteId = table.Column<int>(type: "integer", nullable: false),
                    TurnoId = table.Column<int>(type: "integer", nullable: true),
                    Texto = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadaPor = table.Column<int>(type: "integer", nullable: false),
                    EditadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EditadaPor = table.Column<int>(type: "integer", nullable: true),
                    Anulada = table.Column<bool>(type: "boolean", nullable: false),
                    AnuladaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AnuladaPor = table.Column<int>(type: "integer", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasConsulta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasConsulta_Pacientes",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotasConsulta_Tenants",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotasConsulta_Turnos",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotasConsultaVersiones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    NotaConsultaId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    GuardadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuardadaPor = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasConsultaVersiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasConsultaVersiones_NotasConsulta",
                        column: x => x.NotaConsultaId,
                        principalTable: "NotasConsulta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotasConsultaVersiones_Tenants",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotasConsulta_PacienteId",
                table: "NotasConsulta",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasConsulta_TenantId_PacienteId_CreadaEn",
                table: "NotasConsulta",
                columns: new[] { "TenantId", "PacienteId", "CreadaEn" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_NotasConsulta_TurnoId",
                table: "NotasConsulta",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasConsultaVersiones_TenantId",
                table: "NotasConsultaVersiones",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_NotasConsultaVersiones_NotaConsultaId_Version",
                table: "NotasConsultaVersiones",
                columns: new[] { "NotaConsultaId", "Version" },
                unique: true);

            // Lo escrito sobre un paciente no se pierde: las notas no se borran (se anulan) y las
            // versiones anteriores no se modifican ni se borran. Aunque un error de código o una
            // consulta manual lo intente, la base lo rechaza. TRUNCATE también queda bloqueado.
            migrationBuilder.Sql("""
                CREATE FUNCTION "fn_NotasConsulta_Inalterable"() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'Las notas de consulta y sus versiones no se borran ni se modifican (%: %).', TG_OP, TG_TABLE_NAME
                        USING ERRCODE = 'restrict_violation';
                END;
                $$;

                CREATE TRIGGER "TR_NotasConsulta_SinBorrar"
                    BEFORE DELETE ON "NotasConsulta"
                    FOR EACH ROW EXECUTE FUNCTION "fn_NotasConsulta_Inalterable"();
                CREATE TRIGGER "TR_NotasConsulta_SinTruncate"
                    BEFORE TRUNCATE ON "NotasConsulta"
                    FOR EACH STATEMENT EXECUTE FUNCTION "fn_NotasConsulta_Inalterable"();

                CREATE TRIGGER "TR_NotasConsultaVersiones_Inalterables"
                    BEFORE UPDATE OR DELETE ON "NotasConsultaVersiones"
                    FOR EACH ROW EXECUTE FUNCTION "fn_NotasConsulta_Inalterable"();
                CREATE TRIGGER "TR_NotasConsultaVersiones_SinTruncate"
                    BEFORE TRUNCATE ON "NotasConsultaVersiones"
                    FOR EACH STATEMENT EXECUTE FUNCTION "fn_NotasConsulta_Inalterable"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // DROP TABLE se lleva los triggers; la función queda y se borra al final.
            migrationBuilder.DropTable(
                name: "NotasConsultaVersiones");

            migrationBuilder.DropTable(
                name: "NotasConsulta");

            migrationBuilder.Sql("""DROP FUNCTION IF EXISTS "fn_NotasConsulta_Inalterable"();""");
        }
    }
}

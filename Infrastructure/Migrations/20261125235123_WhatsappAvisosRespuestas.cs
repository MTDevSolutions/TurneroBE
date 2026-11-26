using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WhatsappAvisosRespuestas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecordatorioEncoladoEn",
                table: "Turnos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MensajesEntrantes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    GatewayMensajeId = table.Column<long>(type: "bigint", nullable: false),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    RecibidoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PacienteId = table.Column<int>(type: "integer", nullable: true),
                    TurnoId = table.Column<int>(type: "integer", nullable: true),
                    Resultado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProcesadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensajesEntrantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensajesEntrantes_Pacientes",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MensajesEntrantes_Tenants",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MensajesEntrantes_Turnos",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MensajesWhatsapp",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    TurnoId = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Destino = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Intentos = table.Column<int>(type: "integer", nullable: false),
                    ProximoIntentoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UltimoError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GatewayMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    EnviadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensajesWhatsapp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensajesWhatsapp_Tenants",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MensajesWhatsapp_Turnos",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MensajesEntrantes_PacienteId",
                table: "MensajesEntrantes",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_MensajesEntrantes_TenantId_TurnoId",
                table: "MensajesEntrantes",
                columns: new[] { "TenantId", "TurnoId" });

            migrationBuilder.CreateIndex(
                name: "IX_MensajesEntrantes_TurnoId",
                table: "MensajesEntrantes",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "UX_MensajesEntrantes_GatewayMensajeId",
                table: "MensajesEntrantes",
                column: "GatewayMensajeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MensajesWhatsapp_Estado_ProximoIntentoEn",
                table: "MensajesWhatsapp",
                columns: new[] { "Estado", "ProximoIntentoEn" });

            migrationBuilder.CreateIndex(
                name: "IX_MensajesWhatsapp_TenantId_TurnoId",
                table: "MensajesWhatsapp",
                columns: new[] { "TenantId", "TurnoId" });

            migrationBuilder.CreateIndex(
                name: "IX_MensajesWhatsapp_TurnoId",
                table: "MensajesWhatsapp",
                column: "TurnoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MensajesEntrantes");

            migrationBuilder.DropTable(
                name: "MensajesWhatsapp");

            migrationBuilder.DropColumn(
                name: "RecordatorioEncoladoEn",
                table: "Turnos");
        }
    }
}

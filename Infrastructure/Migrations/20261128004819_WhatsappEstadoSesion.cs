using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WhatsappEstadoSesion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsappEstadoDesde",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsappEstadoSesion",
                table: "Tenants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "WhatsappUltimoAvisoId",
                table: "Tenants",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhatsappEstadoDesde",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "WhatsappEstadoSesion",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "WhatsappUltimoAvisoId",
                table: "Tenants");
        }
    }
}

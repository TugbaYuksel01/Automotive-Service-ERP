using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoServisApi.Migrations
{
    /// <inheritdoc />
    public partial class OdemeveTeslimDetaylariEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeslimEdenKullaniciId",
                table: "Randevular",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TeslimTarihi",
                table: "Randevular",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OdemeYontemi",
                table: "Faturalar",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TahsilEdenKullaniciId",
                table: "Faturalar",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeslimEdenKullaniciId",
                table: "Randevular");

            migrationBuilder.DropColumn(
                name: "TeslimTarihi",
                table: "Randevular");

            migrationBuilder.DropColumn(
                name: "OdemeYontemi",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "TahsilEdenKullaniciId",
                table: "Faturalar");
        }
    }
}

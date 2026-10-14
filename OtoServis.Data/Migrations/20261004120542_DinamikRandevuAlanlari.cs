using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoServisApi.Migrations
{
    /// <inheritdoc />
    public partial class DinamikRandevuAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Durum kolonunu ekle (Varsayılan olarak Bekliyor = 0)
            migrationBuilder.AddColumn<int>(
                name: "Durum",
                table: "Randevular",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Gerçekleşen başlangıç zamanı (Boş geçilebilir / Nullable)
            migrationBuilder.AddColumn<DateTime>(
                name: "GerceklesenBaslangic",
                table: "Randevular",
                type: "datetime2",
                nullable: true);

            // Gerçekleşen bitiş zamanı (Boş geçilebilir / Nullable)
            migrationBuilder.AddColumn<DateTime>(
                name: "GerceklesenBitis",
                table: "Randevular",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alma (Rollback) durumunda sütunları uçuruyoruz
            migrationBuilder.DropColumn(
                name: "Durum",
                table: "Randevular");

            migrationBuilder.DropColumn(
                name: "GerceklesenBaslangic",
                table: "Randevular");

            migrationBuilder.DropColumn(
                name: "GerceklesenBitis",
                table: "Randevular");
        }
    }
}

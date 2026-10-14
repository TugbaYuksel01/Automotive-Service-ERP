using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoServisApi.Migrations
{
    /// <inheritdoc />
    public partial class ErpModulleriEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AracId",
                table: "Randevular",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Araclar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MusteriId = table.Column<int>(type: "int", nullable: false),
                    Plaka = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MarkaModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UretimYili = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Araclar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Faturalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RandevuId = table.Column<int>(type: "int", nullable: false),
                    ToplamParcaMaliyeti = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IscilikUcreti = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KesimTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PdfDosyaYolu = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faturalar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RandevuParcalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RandevuId = table.Column<int>(type: "int", nullable: false),
                    YedekParcaId = table.Column<int>(type: "int", nullable: false),
                    KullanilanMiktar = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RandevuParcalari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YedekParcalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParcaAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StokMiktari = table.Column<int>(type: "int", nullable: false),
                    KritikStokSeviyesi = table.Column<int>(type: "int", nullable: false),
                    UyumluHizmetTipiId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YedekParcalar", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_AracId",
                table: "Randevular",
                column: "AracId");

            migrationBuilder.AddForeignKey(
                name: "FK_Randevular_Araclar_AracId",
                table: "Randevular",
                column: "AracId",
                principalTable: "Araclar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Randevular_Araclar_AracId",
                table: "Randevular");

            migrationBuilder.DropTable(
                name: "Araclar");

            migrationBuilder.DropTable(
                name: "Faturalar");

            migrationBuilder.DropTable(
                name: "RandevuParcalari");

            migrationBuilder.DropTable(
                name: "YedekParcalar");

            migrationBuilder.DropIndex(
                name: "IX_Randevular_AracId",
                table: "Randevular");

            migrationBuilder.DropColumn(
                name: "AracId",
                table: "Randevular");
        }
    }
}

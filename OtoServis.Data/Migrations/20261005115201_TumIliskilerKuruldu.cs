using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoServisApi.Migrations
{
    /// <inheritdoc />
    public partial class TumIliskilerKuruldu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AracPlaka",
                table: "Musteriler");

            migrationBuilder.CreateIndex(
                name: "IX_YedekParcalar_UyumluHizmetTipiId",
                table: "YedekParcalar",
                column: "UyumluHizmetTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_RandevuParcalari_RandevuId",
                table: "RandevuParcalari",
                column: "RandevuId");

            migrationBuilder.CreateIndex(
                name: "IX_RandevuParcalari_YedekParcaId",
                table: "RandevuParcalari",
                column: "YedekParcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_RandevuId",
                table: "Faturalar",
                column: "RandevuId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Araclar_MusteriId",
                table: "Araclar",
                column: "MusteriId");

            migrationBuilder.AddForeignKey(
                name: "FK_Araclar_Musteriler_MusteriId",
                table: "Araclar",
                column: "MusteriId",
                principalTable: "Musteriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_Randevular_RandevuId",
                table: "Faturalar",
                column: "RandevuId",
                principalTable: "Randevular",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RandevuParcalari_Randevular_RandevuId",
                table: "RandevuParcalari",
                column: "RandevuId",
                principalTable: "Randevular",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RandevuParcalari_YedekParcalar_YedekParcaId",
                table: "RandevuParcalari",
                column: "YedekParcaId",
                principalTable: "YedekParcalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YedekParcalar_HizmetTipleri_UyumluHizmetTipiId",
                table: "YedekParcalar",
                column: "UyumluHizmetTipiId",
                principalTable: "HizmetTipleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Araclar_Musteriler_MusteriId",
                table: "Araclar");

            migrationBuilder.DropForeignKey(
                name: "FK_Faturalar_Randevular_RandevuId",
                table: "Faturalar");

            migrationBuilder.DropForeignKey(
                name: "FK_RandevuParcalari_Randevular_RandevuId",
                table: "RandevuParcalari");

            migrationBuilder.DropForeignKey(
                name: "FK_RandevuParcalari_YedekParcalar_YedekParcaId",
                table: "RandevuParcalari");

            migrationBuilder.DropForeignKey(
                name: "FK_YedekParcalar_HizmetTipleri_UyumluHizmetTipiId",
                table: "YedekParcalar");

            migrationBuilder.DropIndex(
                name: "IX_YedekParcalar_UyumluHizmetTipiId",
                table: "YedekParcalar");

            migrationBuilder.DropIndex(
                name: "IX_RandevuParcalari_RandevuId",
                table: "RandevuParcalari");

            migrationBuilder.DropIndex(
                name: "IX_RandevuParcalari_YedekParcaId",
                table: "RandevuParcalari");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_RandevuId",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_Araclar_MusteriId",
                table: "Araclar");

            migrationBuilder.AddColumn<string>(
                name: "AracPlaka",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

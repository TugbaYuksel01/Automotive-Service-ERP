using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoServisApi.Migrations
{
    /// <inheritdoc />
    public partial class UniqueKurallarıEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ParcaAdi",
                table: "YedekParcalar",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Telefon",
                table: "Musteriler",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Ad",
                table: "Istasyonlar",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Ad",
                table: "HizmetTipleri",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Plaka",
                table: "Araclar",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_YedekParcalar_ParcaAdi",
                table: "YedekParcalar",
                column: "ParcaAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Musteriler_Telefon",
                table: "Musteriler",
                column: "Telefon",
                unique: true,
                filter: "[Telefon] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Istasyonlar_Ad",
                table: "Istasyonlar",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HizmetTipleri_Ad",
                table: "HizmetTipleri",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Araclar_Plaka",
                table: "Araclar",
                column: "Plaka",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_YedekParcalar_ParcaAdi",
                table: "YedekParcalar");

            migrationBuilder.DropIndex(
                name: "IX_Musteriler_Telefon",
                table: "Musteriler");

            migrationBuilder.DropIndex(
                name: "IX_Istasyonlar_Ad",
                table: "Istasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_HizmetTipleri_Ad",
                table: "HizmetTipleri");

            migrationBuilder.DropIndex(
                name: "IX_Araclar_Plaka",
                table: "Araclar");

            migrationBuilder.AlterColumn<string>(
                name: "ParcaAdi",
                table: "YedekParcalar",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Telefon",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Ad",
                table: "Istasyonlar",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Ad",
                table: "HizmetTipleri",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Plaka",
                table: "Araclar",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}

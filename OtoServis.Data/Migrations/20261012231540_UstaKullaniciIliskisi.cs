using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoServisApi.Migrations
{
    /// <inheritdoc />
    public partial class UstaKullaniciIliskisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KullaniciId",
                table: "Ustalar",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ustalar_KullaniciId",
                table: "Ustalar",
                column: "KullaniciId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ustalar_Kullanicilar_KullaniciId",
                table: "Ustalar",
                column: "KullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ustalar_Kullanicilar_KullaniciId",
                table: "Ustalar");

            migrationBuilder.DropIndex(
                name: "IX_Ustalar_KullaniciId",
                table: "Ustalar");

            migrationBuilder.DropColumn(
                name: "KullaniciId",
                table: "Ustalar");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoServisApi.Migrations
{
    /// <inheritdoc />
    public partial class AdminSifresiEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Kullanicilar",
                keyColumn: "Id",
                keyValue: 1,
                column: "SifreHash",
                value: "$2a$11$B599TyWE.UfpdESVM/cvROCpi071XP8hyn5ARCEdsZmAz3GSL4bJy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Kullanicilar",
                keyColumn: "Id",
                keyValue: 1,
                column: "SifreHash",
                value: "hashlenmis_sifre_buraya");
        }
    }
}

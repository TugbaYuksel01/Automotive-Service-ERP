using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoServisApi.Migrations
{
    /// <inheritdoc />
    public partial class HizmetSureleriGuncellendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SureDakika",
                table: "HizmetTipleri",
                newName: "UstaSureDakika");

            migrationBuilder.AddColumn<int>(
                name: "LiftSureDakika",
                table: "HizmetTipleri",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LiftSureDakika",
                table: "HizmetTipleri");

            migrationBuilder.RenameColumn(
                name: "UstaSureDakika",
                table: "HizmetTipleri",
                newName: "SureDakika");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoServisApi.Migrations
{
    /// <inheritdoc />
    public partial class UzmanlikAlaniEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GerekenUzmanlik",
                table: "HizmetTipleri",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GerekenUzmanlik",
                table: "HizmetTipleri");
        }
    }
}

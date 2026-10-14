using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoServisApi.Migrations
{
    /// <inheritdoc />
    public partial class MusteriTablosuKaldirildi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

           

            migrationBuilder.DropForeignKey(
                    name: "FK_Araclar_Musteriler_MusteriId",
                    table: "Araclar");

            migrationBuilder.DropForeignKey(
                name: "FK_Randevular_Musteriler_MusteriId",
                table: "Randevular");

            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT Kullanicilar ON;

                -- SQL Server'a 'Korkma, boş gelirse ben geçici bir mail basacağım' garantisi veriyoruz:
                INSERT INTO Kullanicilar (Id, AdSoyad, Telefon, Email, Rol)
                SELECT 
                    Id, 
                    AdSoyad, 
                    Telefon, 
                    ISNULL(Email, 'eski_musteri_' + CAST(Id AS NVARCHAR) + '@otoservis.com'),  
                    'Musteri' 
                FROM Musteriler;

                SET IDENTITY_INSERT Kullanicilar OFF;
            ");

            migrationBuilder.DropTable(
                name: "Musteriler");

            migrationBuilder.DropIndex(
                name: "IX_Randevular_MusteriId",
                table: "Randevular");

            migrationBuilder.DropIndex(
                name: "IX_Araclar_MusteriId",
                table: "Araclar");

            migrationBuilder.AddColumn<int>(
                name: "KullaniciId",
                table: "Randevular",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Telefon",
                table: "Kullanicilar",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KullaniciId",
                table: "Araclar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_KullaniciId",
                table: "Randevular",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Telefon",
                table: "Kullanicilar",
                column: "Telefon",
                unique: true,
                filter: "[Telefon] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Araclar_KullaniciId",
                table: "Araclar",
                column: "KullaniciId");

            migrationBuilder.AddForeignKey(
                name: "FK_Araclar_Kullanicilar_KullaniciId",
                table: "Araclar",
                column: "KullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Randevular_Kullanicilar_KullaniciId",
                table: "Randevular",
                column: "KullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Araclar_Kullanicilar_KullaniciId",
                table: "Araclar");

            migrationBuilder.DropForeignKey(
                name: "FK_Randevular_Kullanicilar_KullaniciId",
                table: "Randevular");

            migrationBuilder.DropIndex(
                name: "IX_Randevular_KullaniciId",
                table: "Randevular");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_Telefon",
                table: "Kullanicilar");

            migrationBuilder.DropIndex(
                name: "IX_Araclar_KullaniciId",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "KullaniciId",
                table: "Randevular");

            migrationBuilder.DropColumn(
                name: "KullaniciId",
                table: "Araclar");

            migrationBuilder.AlterColumn<string>(
                name: "Telefon",
                table: "Kullanicilar",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Musteriler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdSoyad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Musteriler", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_MusteriId",
                table: "Randevular",
                column: "MusteriId");

            migrationBuilder.CreateIndex(
                name: "IX_Araclar_MusteriId",
                table: "Araclar",
                column: "MusteriId");

            migrationBuilder.CreateIndex(
                name: "IX_Musteriler_Telefon",
                table: "Musteriler",
                column: "Telefon",
                unique: true,
                filter: "[Telefon] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Araclar_Musteriler_MusteriId",
                table: "Araclar",
                column: "MusteriId",
                principalTable: "Musteriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Randevular_Musteriler_MusteriId",
                table: "Randevular",
                column: "MusteriId",
                principalTable: "Musteriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

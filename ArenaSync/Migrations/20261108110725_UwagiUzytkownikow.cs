using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaSync.Migrations
{
    /// <inheritdoc />
    public partial class UwagiUzytkownikow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Uwagi",
                columns: table => new
                {
                    IdUwagi = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kategoria = table.Column<int>(type: "INTEGER", nullable: false),
                    Temat = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tresc = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    DataZgloszenia = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AutorId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Odpowiedz = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DataOdpowiedzi = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uwagi", x => x.IdUwagi);
                    table.ForeignKey(
                        name: "FK_Uwagi_AspNetUsers_AutorId",
                        column: x => x.AutorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Uwagi_AutorId",
                table: "Uwagi",
                column: "AutorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Uwagi");
        }
    }
}

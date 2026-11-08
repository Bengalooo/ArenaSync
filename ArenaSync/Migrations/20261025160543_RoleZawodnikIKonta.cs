using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaSync.Migrations
{
    /// <inheritdoc />
    public partial class RoleZawodnikIKonta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DruzynaId",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImieNazwisko",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DruzynaId",
                table: "AspNetUsers",
                column: "DruzynaId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Druzyny_DruzynaId",
                table: "AspNetUsers",
                column: "DruzynaId",
                principalTable: "Druzyny",
                principalColumn: "IdDruzyny",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Druzyny_DruzynaId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DruzynaId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DruzynaId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ImieNazwisko",
                table: "AspNetUsers");
        }
    }
}

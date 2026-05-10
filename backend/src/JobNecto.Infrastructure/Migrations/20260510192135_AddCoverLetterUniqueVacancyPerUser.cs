using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobNecto.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverLetterUniqueVacancyPerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CoverLetters_UserId",
                table: "CoverLetters");

            migrationBuilder.CreateIndex(
                name: "IX_CoverLetters_UserId_VacancyId",
                table: "CoverLetters",
                columns: new[] { "UserId", "VacancyId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CoverLetters_UserId_VacancyId",
                table: "CoverLetters");

            migrationBuilder.CreateIndex(
                name: "IX_CoverLetters_UserId",
                table: "CoverLetters",
                column: "UserId");
        }
    }
}

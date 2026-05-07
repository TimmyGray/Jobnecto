using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobNecto.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverLetterTemplateUniqueNamePerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CoverLetterTemplates_UserId",
                table: "CoverLetterTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_CoverLetterTemplates_UserId_Name",
                table: "CoverLetterTemplates",
                columns: new[] { "UserId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CoverLetterTemplates_UserId_Name",
                table: "CoverLetterTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_CoverLetterTemplates_UserId",
                table: "CoverLetterTemplates",
                column: "UserId");
        }
    }
}

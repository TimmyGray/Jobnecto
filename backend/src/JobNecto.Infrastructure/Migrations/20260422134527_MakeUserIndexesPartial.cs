using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobNecto.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserIndexesPartial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Corrective step: IsDeleted and DeletedAt were missing from the DB because
            // this instance was initialised before those columns were added to Init.
            // Add them idempotently to every entity table that declares soft-delete.
            string[] softDeleteTables = ["Users", "CoverLetters", "CoverLetterTemplates", "Resumes", "Educations", "Vacancies"];
            foreach (var table in softDeleteTables)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ADD COLUMN IF NOT EXISTS \"IsDeleted\" boolean NOT NULL DEFAULT false;");
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ADD COLUMN IF NOT EXISTS \"DeletedAt\" timestamptz NULL;");
            }

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Login",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                table: "Users",
                column: "Login",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Login",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                table: "Users",
                column: "Login",
                unique: true);
        }
    }
}

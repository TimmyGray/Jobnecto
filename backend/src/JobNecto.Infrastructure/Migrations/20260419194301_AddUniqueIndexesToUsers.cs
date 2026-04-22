using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobNecto.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexesToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration only adds unique indexes for Users.Email and Users.Login.
            // Other schema changes (DeletedAt/IsDeleted columns and FK definitions)
            // are already applied in the Init migration and must not be duplicated here.

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only remove the indexes this migration created.
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Login",
                table: "Users");
        }
    }
}

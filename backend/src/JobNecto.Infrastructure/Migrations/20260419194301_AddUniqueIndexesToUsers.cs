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
            migrationBuilder.DropForeignKey(
                name: "FK_CoverLetters_CoverLetters_UserId",
                table: "CoverLetters");

            migrationBuilder.DropForeignKey(
                name: "FK_CoverLetters_CoverLetters_VacancyId",
                table: "CoverLetters");

            migrationBuilder.DropForeignKey(
                name: "FK_CoverLetterTemplates_CoverLetterTemplates_UserId",
                table: "CoverLetterTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_Resumes_Resumes_UserId",
                table: "Resumes");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Vacancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Resumes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Resumes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Educations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Educations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CoverLetterTemplates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CoverLetterTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CoverLetters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CoverLetters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddForeignKey(
                name: "FK_CoverLetters_Users_UserId",
                table: "CoverLetters",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CoverLetters_Vacancies_VacancyId",
                table: "CoverLetters",
                column: "VacancyId",
                principalTable: "Vacancies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CoverLetterTemplates_Users_UserId",
                table: "CoverLetterTemplates",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Resumes_Users_UserId",
                table: "Resumes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoverLetters_Users_UserId",
                table: "CoverLetters");

            migrationBuilder.DropForeignKey(
                name: "FK_CoverLetters_Vacancies_VacancyId",
                table: "CoverLetters");

            migrationBuilder.DropForeignKey(
                name: "FK_CoverLetterTemplates_Users_UserId",
                table: "CoverLetterTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_Resumes_Users_UserId",
                table: "Resumes");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Login",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CoverLetterTemplates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CoverLetterTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CoverLetters");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CoverLetters");

            migrationBuilder.AddForeignKey(
                name: "FK_CoverLetters_CoverLetters_UserId",
                table: "CoverLetters",
                column: "UserId",
                principalTable: "CoverLetters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CoverLetters_CoverLetters_VacancyId",
                table: "CoverLetters",
                column: "VacancyId",
                principalTable: "CoverLetters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CoverLetterTemplates_CoverLetterTemplates_UserId",
                table: "CoverLetterTemplates",
                column: "UserId",
                principalTable: "CoverLetterTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Resumes_Resumes_UserId",
                table: "Resumes",
                column: "UserId",
                principalTable: "Resumes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

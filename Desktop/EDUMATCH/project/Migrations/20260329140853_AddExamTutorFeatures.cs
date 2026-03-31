using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMatch.Migrations
{
    /// <inheritdoc />
    public partial class AddExamTutorFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exams_Sessions_SessionId",
                table: "Exams");

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "ExamSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TutorComment",
                table: "ExamSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SessionId",
                table: "Exams",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "CloseAt",
                table: "Exams",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExamFileUrl",
                table: "Exams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpen",
                table: "Exams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenAt",
                table: "Exams",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TutorId",
                table: "Exams",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_TutorId",
                table: "Exams",
                column: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_AspNetUsers_TutorId",
                table: "Exams",
                column: "TutorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_Sessions_SessionId",
                table: "Exams",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exams_AspNetUsers_TutorId",
                table: "Exams");

            migrationBuilder.DropForeignKey(
                name: "FK_Exams_Sessions_SessionId",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_TutorId",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "ExamSubmissions");

            migrationBuilder.DropColumn(
                name: "TutorComment",
                table: "ExamSubmissions");

            migrationBuilder.DropColumn(
                name: "CloseAt",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "ExamFileUrl",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "IsOpen",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "OpenAt",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "TutorId",
                table: "Exams");

            migrationBuilder.AlterColumn<int>(
                name: "SessionId",
                table: "Exams",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_Sessions_SessionId",
                table: "Exams",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

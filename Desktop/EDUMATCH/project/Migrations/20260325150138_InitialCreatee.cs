using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMatch.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreatee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAnswers_SelectedOptionId",
                table: "SubmissionAnswers",
                column: "SelectedOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmissionAnswers_ExamAnswerOptions_SelectedOptionId",
                table: "SubmissionAnswers",
                column: "SelectedOptionId",
                principalTable: "ExamAnswerOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmissionAnswers_ExamAnswerOptions_SelectedOptionId",
                table: "SubmissionAnswers");

            migrationBuilder.DropIndex(
                name: "IX_SubmissionAnswers_SelectedOptionId",
                table: "SubmissionAnswers");
        }
    }
}

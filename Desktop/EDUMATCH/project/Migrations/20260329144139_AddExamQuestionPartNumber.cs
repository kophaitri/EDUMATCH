using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMatch.Migrations
{
    /// <inheritdoc />
    public partial class AddExamQuestionPartNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PartNumber",
                table: "ExamQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartNumber",
                table: "ExamQuestions");
        }
    }
}

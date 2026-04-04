using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMatch.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicTagAndStudentProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GradeLevelId",
                table: "StudentProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalSessionsCompleted",
                table: "StudentProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopicTag",
                table: "ExamQuestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_GradeLevelId",
                table: "StudentProfiles",
                column: "GradeLevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProfiles_GradeLevels_GradeLevelId",
                table: "StudentProfiles",
                column: "GradeLevelId",
                principalTable: "GradeLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentProfiles_GradeLevels_GradeLevelId",
                table: "StudentProfiles");

            migrationBuilder.DropIndex(
                name: "IX_StudentProfiles_GradeLevelId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "GradeLevelId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "TotalSessionsCompleted",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "TopicTag",
                table: "ExamQuestions");
        }
    }
}

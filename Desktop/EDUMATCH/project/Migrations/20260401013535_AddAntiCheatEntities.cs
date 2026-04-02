using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMatch.Migrations
{
    /// <inheritdoc />
    public partial class AddAntiCheatEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamBehaviorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimestampMs = table.Column<long>(type: "bigint", nullable: false),
                    Meta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamBehaviorLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamBehaviorLogs_ExamSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "ExamSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamSuspiciousScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<float>(type: "real", nullable: false),
                    PasteScore = table.Column<float>(type: "real", nullable: false),
                    TabScore = table.Column<float>(type: "real", nullable: false),
                    TimeScore = table.Column<float>(type: "real", nullable: false),
                    StyleScore = table.Column<float>(type: "real", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReasonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSuspiciousScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSuspiciousScores_ExamSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "ExamSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentWritingProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AvgWordLength = table.Column<float>(type: "real", nullable: false),
                    AvgSentenceLength = table.Column<float>(type: "real", nullable: false),
                    VocabRichness = table.Column<float>(type: "real", nullable: false),
                    PunctuationRatio = table.Column<float>(type: "real", nullable: false),
                    SampleCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentWritingProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentWritingProfiles_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamBehaviorLogs_SubmissionId",
                table: "ExamBehaviorLogs",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSuspiciousScores_SubmissionId",
                table: "ExamSuspiciousScores",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentWritingProfiles_StudentId",
                table: "StudentWritingProfiles",
                column: "StudentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamBehaviorLogs");

            migrationBuilder.DropTable(
                name: "ExamSuspiciousScores");

            migrationBuilder.DropTable(
                name: "StudentWritingProfiles");
        }
    }
}

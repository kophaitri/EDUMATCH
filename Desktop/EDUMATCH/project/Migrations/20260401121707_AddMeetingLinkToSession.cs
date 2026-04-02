using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMatch.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingLinkToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MeetingLink",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingLink",
                table: "Sessions");
        }
    }
}

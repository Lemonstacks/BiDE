using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiDE.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecurityAnswer",
                table: "Students",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityQuestion",
                table: "Students",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityAnswer",
                table: "Instructors",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityQuestion",
                table: "Instructors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityAnswer",
                table: "Admins",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityQuestion",
                table: "Admins",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1,
                columns: new[] { "SecurityAnswer", "SecurityQuestion" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecurityAnswer",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SecurityQuestion",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SecurityAnswer",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "SecurityQuestion",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "SecurityAnswer",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "SecurityQuestion",
                table: "Admins");
        }
    }
}

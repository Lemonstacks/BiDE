using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiDE.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Admins",
                columns: new[] { "AdminId", "Contact", "Email", "FirstName", "LastName", "Password" },
                values: new object[] { 1, "0712345678", "admin@bide.com", "System", "Administrator", "Admin123" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1);
        }
    }
}

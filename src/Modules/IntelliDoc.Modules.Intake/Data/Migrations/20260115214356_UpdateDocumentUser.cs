using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliDoc.Modules.Intake.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocumentUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadedBy",
                schema: "intake",
                table: "Documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadedBy",
                schema: "intake",
                table: "Documents");
        }
    }
}

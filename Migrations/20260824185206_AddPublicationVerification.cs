using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeNetAssist.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicationVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Publications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "Publications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifierRemarks",
                table: "Publications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Publications");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Publications");

            migrationBuilder.DropColumn(
                name: "VerifierRemarks",
                table: "Publications");
        }
    }
}

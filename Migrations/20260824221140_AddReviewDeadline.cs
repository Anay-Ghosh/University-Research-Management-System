using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeNetAssist.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeadlineResolved",
                table: "ResearchProposals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeadlineTask",
                table: "ResearchProposals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewDeadline",
                table: "ResearchProposals",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeadlineResolved",
                table: "ResearchProposals");

            migrationBuilder.DropColumn(
                name: "DeadlineTask",
                table: "ResearchProposals");

            migrationBuilder.DropColumn(
                name: "ReviewDeadline",
                table: "ResearchProposals");
        }
    }
}

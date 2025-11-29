using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTN_STORE.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewReplyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Reply",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReplyDate",
                table: "Reviews",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "Reply",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReplyDate",
                table: "Reviews");
        }
    }
}

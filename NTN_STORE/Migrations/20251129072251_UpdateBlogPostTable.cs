using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTN_STORE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBlogPostTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Thumbnail",
                table: "BlogPosts");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "BlogPosts",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "BlogPosts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "BlogPosts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "BlogPosts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "BlogPosts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Author",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "BlogPosts");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "BlogPosts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AddColumn<string>(
                name: "Thumbnail",
                table: "BlogPosts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

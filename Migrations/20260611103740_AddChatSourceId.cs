using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatInsight.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSourceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SourceId",
                table: "Chats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Chats",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Chats");
        }
    }
}

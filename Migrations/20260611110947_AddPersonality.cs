using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatInsight.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Personalities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    Participant = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    CommunicationStyle = table.Column<string>(type: "text", nullable: false),
                    Traits = table.Column<List<string>>(type: "text[]", nullable: false),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personalities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Personalities_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_SourceId",
                table: "Chats",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Personalities_ChatId_Participant",
                table: "Personalities",
                columns: new[] { "ChatId", "Participant" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Personalities");

            migrationBuilder.DropIndex(
                name: "IX_Chats_SourceId",
                table: "Chats");
        }
    }
}

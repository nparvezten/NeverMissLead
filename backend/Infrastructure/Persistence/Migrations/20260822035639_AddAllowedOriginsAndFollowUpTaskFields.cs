using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeverMissLead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowedOriginsAndFollowUpTaskFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "LeadId",
                table: "follow_up_tasks",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                table: "follow_up_tasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "follow_up_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "AllowedOrigins",
                table: "business_settings",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY['http://localhost:4200']::text[]");

            migrationBuilder.CreateIndex(
                name: "ix_follow_up_tasks_business_id",
                table: "follow_up_tasks",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_follow_up_tasks_ConversationId",
                table: "follow_up_tasks",
                column: "ConversationId");

            migrationBuilder.AddForeignKey(
                name: "FK_follow_up_tasks_conversations_ConversationId",
                table: "follow_up_tasks",
                column: "ConversationId",
                principalTable: "conversations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_follow_up_tasks_conversations_ConversationId",
                table: "follow_up_tasks");

            migrationBuilder.DropIndex(
                name: "ix_follow_up_tasks_business_id",
                table: "follow_up_tasks");

            migrationBuilder.DropIndex(
                name: "IX_follow_up_tasks_ConversationId",
                table: "follow_up_tasks");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "follow_up_tasks");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "follow_up_tasks");

            migrationBuilder.DropColumn(
                name: "AllowedOrigins",
                table: "business_settings");

            migrationBuilder.AlterColumn<Guid>(
                name: "LeadId",
                table: "follow_up_tasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}

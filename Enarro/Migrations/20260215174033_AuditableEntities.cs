using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enarro.Migrations
{
    /// <inheritdoc />
    public partial class AuditableEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "refresh_tokens",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "refresh_tokens",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "document_tags",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "document_tags",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "document_tags",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "document_tags");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "document_tags");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "document_tags");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Otzivi.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityQuestionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSecurityQuestionEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityAnswerHash",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityQuestion",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecurityQuestionSetAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSecurityQuestionEnabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SecurityAnswerHash",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SecurityQuestion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SecurityQuestionSetAt",
                table: "AspNetUsers");
        }
    }
}

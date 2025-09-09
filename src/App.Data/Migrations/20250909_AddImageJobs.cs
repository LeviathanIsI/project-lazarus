using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lazarus.Data.Migrations
{
    public partial class AddImageJobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Prompt = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    NegativePrompt = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Mode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ControlNetPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    StylePresetPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    UpscalerPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    VaePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Seed = table.Column<int>(type: "INTEGER", nullable: true),
                    Steps = table.Column<int>(type: "INTEGER", nullable: false),
                    CfgScale = table.Column<double>(type: "REAL", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceImagePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    MaskImagePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Strength = table.Column<double>(type: "REAL", nullable: true),
                    OutputPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageJobs_CreatedAt",
                table: "ImageJobs",
                column: "CreatedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ImageJobs");
        }
    }
}


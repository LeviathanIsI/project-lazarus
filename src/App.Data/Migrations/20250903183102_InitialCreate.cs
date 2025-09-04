using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lazarus.App.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LlmAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AssetType = table.Column<string>(type: "TEXT", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    QuantizationFormat = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    ParameterCount = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    VramEstimateGb = table.Column<decimal>(type: "TEXT", precision: 8, scale: 2, nullable: true),
                    Architecture = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CompatibleRunners = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    LastLoadedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ActiveRunnerId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ValidationResult = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsValidated = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Progress = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LlmAssets_Architecture",
                table: "LlmAssets",
                column: "Architecture");

            migrationBuilder.CreateIndex(
                name: "IX_LlmAssets_AssetType",
                table: "LlmAssets",
                column: "AssetType");

            migrationBuilder.CreateIndex(
                name: "IX_LlmAssets_CreatedAt",
                table: "LlmAssets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LlmAssets_FileHash",
                table: "LlmAssets",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_LlmAssets_FilePath",
                table: "LlmAssets",
                column: "FilePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LlmAssets_IsDeleted",
                table: "LlmAssets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LlmAssets_Name",
                table: "LlmAssets",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_LlmAssets_Status",
                table: "LlmAssets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_CreatedAt",
                table: "TrainingSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_IsDeleted",
                table: "TrainingSessions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_Name",
                table: "TrainingSessions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_Status",
                table: "TrainingSessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LlmAssets");

            migrationBuilder.DropTable(
                name: "TrainingSessions");
        }
    }
}

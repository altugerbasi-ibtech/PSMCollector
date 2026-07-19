using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectorOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectorCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectorType = table.Column<byte>(type: "tinyint", nullable: false),
                    CommandType = table.Column<byte>(type: "tinyint", nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectorCommands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollectorHeartbeats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollectorType = table.Column<byte>(type: "tinyint", nullable: false),
                    InstanceName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastHeartbeatUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StatusMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectorHeartbeats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectorCommands_CollectorType_Status_RequestedAtUtc",
                table: "CollectorCommands",
                columns: new[] { "CollectorType", "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectorHeartbeats_CollectorType_InstanceName",
                table: "CollectorHeartbeats",
                columns: new[] { "CollectorType", "InstanceName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectorCommands");

            migrationBuilder.DropTable(
                name: "CollectorHeartbeats");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollectionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IisServerCount = table.Column<int>(type: "int", nullable: false),
                    SuccessfulIisServerCount = table.Column<int>(type: "int", nullable: false),
                    FailedIisServerCount = table.Column<int>(type: "int", nullable: false),
                    StagedConnectionCount = table.Column<int>(type: "int", nullable: false),
                    MatchedConnectionCount = table.Column<int>(type: "int", nullable: false),
                    UnmatchedConnectionCount = table.Column<int>(type: "int", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IisServers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Fqdn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CollectionIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    ConnectionTimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastCollectionAttemptUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSuccessfulCollectionUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCollectionStatus = table.Column<byte>(type: "tinyint", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IisServers", x => x.Id);
                    table.CheckConstraint("CK_IisServers_CollectionInterval", "[CollectionIntervalSeconds] BETWEEN 10 AND 86400");
                    table.CheckConstraint("CK_IisServers_ConnectionTimeout", "[ConnectionTimeoutSeconds] BETWEEN 5 AND 300");
                });

            migrationBuilder.CreateTable(
                name: "SqlServers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Fqdn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(48)", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TrustServerCertificate = table.Column<bool>(type: "bit", nullable: false),
                    ConnectionTimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    LastConnectionStatus = table.Column<byte>(type: "tinyint", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SqlServers", x => x.Id);
                    table.CheckConstraint("CK_SqlServers_ConnectionTimeout", "[ConnectionTimeoutSeconds] BETWEEN 5 AND 300");
                    table.CheckConstraint("CK_SqlServers_Port", "[Port] = 1433");
                });

            migrationBuilder.CreateTable(
                name: "UnknownSqlEndpoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IpAddress = table.Column<string>(type: "varchar(48)", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    FirstSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ObservationCount = table.Column<long>(type: "bigint", nullable: false),
                    LastIisServerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnknownSqlEndpoints", x => x.Id);
                    table.CheckConstraint("CK_UnknownSqlEndpoints_Port", "[Port] = 1433");
                });

            migrationBuilder.CreateTable(
                name: "IisConnectionStaging",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    IisServerId = table.Column<int>(type: "int", nullable: false),
                    IisServerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AppPoolName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WorkerProcessId = table.Column<int>(type: "int", nullable: false),
                    ClientIp = table.Column<string>(type: "varchar(48)", nullable: false),
                    ClientPort = table.Column<int>(type: "int", nullable: false),
                    SqlServerIp = table.Column<string>(type: "varchar(48)", nullable: false),
                    SqlServerPort = table.Column<int>(type: "int", nullable: false),
                    ProcessingStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    NextRetryAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IisConnectionStaging", x => x.Id);
                    table.CheckConstraint("CK_IisConnectionStaging_SqlServerPort", "[SqlServerPort] = 1433");
                    table.ForeignKey(
                        name: "FK_IisConnectionStaging_CollectionRuns_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "CollectionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IisConnectionStaging_IisServers_IisServerId",
                        column: x => x.IisServerId,
                        principalTable: "IisServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionInventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryDateUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    IisServerId = table.Column<int>(type: "int", nullable: false),
                    IisServerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AppPoolName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SqlServerId = table.Column<int>(type: "int", nullable: false),
                    SqlServerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SqlInstance = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DatabaseName = table.Column<string>(type: "sysname", nullable: false),
                    SqlEndpoint = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TotalConnections = table.Column<int>(type: "int", nullable: false),
                    ActiveConnections = table.Column<int>(type: "int", nullable: false),
                    IdlePooledConnections = table.Column<int>(type: "int", nullable: false),
                    WorkerProcessIds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionInventory", x => x.Id);
                    table.CheckConstraint("CK_ConnectionInventory_Totals", "[TotalConnections] = [ActiveConnections] + [IdlePooledConnections]");
                    table.ForeignKey(
                        name: "FK_ConnectionInventory_CollectionRuns_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "CollectionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectionInventory_IisServers_IisServerId",
                        column: x => x.IisServerId,
                        principalTable: "IisServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectionInventory_SqlServers_SqlServerId",
                        column: x => x.SqlServerId,
                        principalTable: "SqlServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAtUtc",
                table: "AuditLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionRuns_Status_StartedAtUtc",
                table: "CollectionRuns",
                columns: new[] { "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionInventory_CollectionId_IisServerId_AppPoolName_SqlServerId_DatabaseName",
                table: "ConnectionInventory",
                columns: new[] { "CollectionId", "IisServerId", "AppPoolName", "SqlServerId", "DatabaseName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionInventory_IisServerId",
                table: "ConnectionInventory",
                column: "IisServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionInventory_InventoryDateUtc",
                table: "ConnectionInventory",
                column: "InventoryDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionInventory_SqlServerId",
                table: "ConnectionInventory",
                column: "SqlServerId");

            migrationBuilder.CreateIndex(
                name: "IX_IisConnectionStaging_CollectionId_IisServerId_AppPoolName_WorkerProcessId_ClientIp_ClientPort_SqlServerIp_SqlServerPort",
                table: "IisConnectionStaging",
                columns: new[] { "CollectionId", "IisServerId", "AppPoolName", "WorkerProcessId", "ClientIp", "ClientPort", "SqlServerIp", "SqlServerPort" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IisConnectionStaging_IisServerId",
                table: "IisConnectionStaging",
                column: "IisServerId");

            migrationBuilder.CreateIndex(
                name: "IX_IisConnectionStaging_ProcessingStatus_CollectionId_SqlServerIp",
                table: "IisConnectionStaging",
                columns: new[] { "ProcessingStatus", "CollectionId", "SqlServerIp" });

            migrationBuilder.CreateIndex(
                name: "IX_IisServers_ServerName",
                table: "IisServers",
                column: "ServerName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SqlServers_IpAddress",
                table: "SqlServers",
                column: "IpAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnknownSqlEndpoints_IpAddress_Port",
                table: "UnknownSqlEndpoints",
                columns: new[] { "IpAddress", "Port" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ConnectionInventory");

            migrationBuilder.DropTable(
                name: "IisConnectionStaging");

            migrationBuilder.DropTable(
                name: "UnknownSqlEndpoints");

            migrationBuilder.DropTable(
                name: "SqlServers");

            migrationBuilder.DropTable(
                name: "CollectionRuns");

            migrationBuilder.DropTable(
                name: "IisServers");
        }
    }
}

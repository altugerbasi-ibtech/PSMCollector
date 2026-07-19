IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [OccurredAtUtc] datetime2 NOT NULL,
        [UserName] nvarchar(256) NOT NULL,
        [Action] nvarchar(64) NOT NULL,
        [EntityType] nvarchar(128) NOT NULL,
        [EntityId] nvarchar(128) NOT NULL,
        [OldValuesJson] nvarchar(max) NULL,
        [NewValuesJson] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE TABLE [CollectionRuns] (
        [Id] uniqueidentifier NOT NULL,
        [StartedAtUtc] datetime2 NOT NULL,
        [CompletedAtUtc] datetime2 NULL,
        [Status] tinyint NOT NULL,
        [IisServerCount] int NOT NULL,
        [SuccessfulIisServerCount] int NOT NULL,
        [FailedIisServerCount] int NOT NULL,
        [StagedConnectionCount] int NOT NULL,
        [MatchedConnectionCount] int NOT NULL,
        [UnmatchedConnectionCount] int NOT NULL,
        [ErrorSummary] nvarchar(2000) NULL,
        CONSTRAINT [PK_CollectionRuns] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE TABLE [IisServers] (
        [Id] int NOT NULL IDENTITY,
        [ServerName] nvarchar(128) NOT NULL,
        [Fqdn] nvarchar(255) NULL,
        [IsEnabled] bit NOT NULL,
        [CollectionIntervalSeconds] int NOT NULL,
        [ConnectionTimeoutSeconds] int NOT NULL,
        [Description] nvarchar(500) NULL,
        [LastCollectionAttemptUtc] datetime2 NULL,
        [LastSuccessfulCollectionUtc] datetime2 NULL,
        [LastCollectionStatus] tinyint NULL,
        [LastErrorMessage] nvarchar(2000) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(256) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_IisServers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_IisServers_CollectionInterval] CHECK ([CollectionIntervalSeconds] BETWEEN 10 AND 86400),
        CONSTRAINT [CK_IisServers_ConnectionTimeout] CHECK ([ConnectionTimeoutSeconds] BETWEEN 5 AND 300)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE TABLE [SqlServers] (
        [Id] int NOT NULL IDENTITY,
        [ServerName] nvarchar(128) NOT NULL,
        [Fqdn] nvarchar(255) NOT NULL,
        [IpAddress] varchar(48) NOT NULL,
        [Port] int NOT NULL,
        [IsEnabled] bit NOT NULL,
        [TrustServerCertificate] bit NOT NULL,
        [ConnectionTimeoutSeconds] int NOT NULL,
        [LastConnectionStatus] tinyint NULL,
        [LastErrorMessage] nvarchar(2000) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(256) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SqlServers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SqlServers_ConnectionTimeout] CHECK ([ConnectionTimeoutSeconds] BETWEEN 5 AND 300),
        CONSTRAINT [CK_SqlServers_Port] CHECK ([Port] = 1433)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE TABLE [UnknownSqlEndpoints] (
        [Id] int NOT NULL IDENTITY,
        [IpAddress] varchar(48) NOT NULL,
        [Port] int NOT NULL,
        [FirstSeenUtc] datetime2 NOT NULL,
        [LastSeenUtc] datetime2 NOT NULL,
        [ObservationCount] bigint NOT NULL,
        [LastIisServerName] nvarchar(128) NOT NULL,
        CONSTRAINT [PK_UnknownSqlEndpoints] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UnknownSqlEndpoints_Port] CHECK ([Port] = 1433)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE TABLE [IisConnectionStaging] (
        [Id] bigint NOT NULL IDENTITY,
        [CollectionId] uniqueidentifier NOT NULL,
        [CollectedAtUtc] datetime2(3) NOT NULL,
        [IisServerId] int NOT NULL,
        [IisServerName] nvarchar(128) NOT NULL,
        [AppPoolName] nvarchar(256) NOT NULL,
        [WorkerProcessId] int NOT NULL,
        [ClientIp] varchar(48) NOT NULL,
        [ClientPort] int NOT NULL,
        [SqlServerIp] varchar(48) NOT NULL,
        [SqlServerPort] int NOT NULL,
        [ProcessingStatus] tinyint NOT NULL,
        [ProcessingStartedAtUtc] datetime2 NULL,
        [RetryCount] int NOT NULL,
        [NextRetryAtUtc] datetime2 NULL,
        [LastErrorMessage] nvarchar(2000) NULL,
        CONSTRAINT [PK_IisConnectionStaging] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_IisConnectionStaging_SqlServerPort] CHECK ([SqlServerPort] = 1433),
        CONSTRAINT [FK_IisConnectionStaging_CollectionRuns_CollectionId] FOREIGN KEY ([CollectionId]) REFERENCES [CollectionRuns] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_IisConnectionStaging_IisServers_IisServerId] FOREIGN KEY ([IisServerId]) REFERENCES [IisServers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE TABLE [ConnectionInventory] (
        [Id] bigint NOT NULL IDENTITY,
        [CollectionId] uniqueidentifier NOT NULL,
        [InventoryDateUtc] datetime2(0) NOT NULL,
        [IisServerId] int NOT NULL,
        [IisServerName] nvarchar(128) NOT NULL,
        [AppPoolName] nvarchar(256) NOT NULL,
        [SqlServerId] int NOT NULL,
        [SqlServerName] nvarchar(128) NOT NULL,
        [SqlInstance] nvarchar(128) NULL,
        [DatabaseName] sysname NOT NULL,
        [SqlEndpoint] nvarchar(256) NOT NULL,
        [TotalConnections] int NOT NULL,
        [ActiveConnections] int NOT NULL,
        [IdlePooledConnections] int NOT NULL,
        [WorkerProcessIds] nvarchar(1000) NULL,
        CONSTRAINT [PK_ConnectionInventory] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ConnectionInventory_Totals] CHECK ([TotalConnections] = [ActiveConnections] + [IdlePooledConnections]),
        CONSTRAINT [FK_ConnectionInventory_CollectionRuns_CollectionId] FOREIGN KEY ([CollectionId]) REFERENCES [CollectionRuns] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ConnectionInventory_IisServers_IisServerId] FOREIGN KEY ([IisServerId]) REFERENCES [IisServers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ConnectionInventory_SqlServers_SqlServerId] FOREIGN KEY ([SqlServerId]) REFERENCES [SqlServers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_OccurredAtUtc] ON [AuditLogs] ([OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE INDEX [IX_CollectionRuns_Status_StartedAtUtc] ON [CollectionRuns] ([Status], [StartedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ConnectionInventory_CollectionId_IisServerId_AppPoolName_SqlServerId_DatabaseName] ON [ConnectionInventory] ([CollectionId], [IisServerId], [AppPoolName], [SqlServerId], [DatabaseName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE INDEX [IX_ConnectionInventory_IisServerId] ON [ConnectionInventory] ([IisServerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE INDEX [IX_ConnectionInventory_InventoryDateUtc] ON [ConnectionInventory] ([InventoryDateUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE INDEX [IX_ConnectionInventory_SqlServerId] ON [ConnectionInventory] ([SqlServerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IisConnectionStaging_CollectionId_IisServerId_AppPoolName_WorkerProcessId_ClientIp_ClientPort_SqlServerIp_SqlServerPort] ON [IisConnectionStaging] ([CollectionId], [IisServerId], [AppPoolName], [WorkerProcessId], [ClientIp], [ClientPort], [SqlServerIp], [SqlServerPort]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE INDEX [IX_IisConnectionStaging_IisServerId] ON [IisConnectionStaging] ([IisServerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE INDEX [IX_IisConnectionStaging_ProcessingStatus_CollectionId_SqlServerIp] ON [IisConnectionStaging] ([ProcessingStatus], [CollectionId], [SqlServerIp]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IisServers_ServerName] ON [IisServers] ([ServerName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SqlServers_IpAddress] ON [SqlServers] ([IpAddress]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UnknownSqlEndpoints_IpAddress_Port] ON [UnknownSqlEndpoints] ([IpAddress], [Port]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719065931_InitialInventorySchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719065931_InitialInventorySchema', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719071212_AddCollectorOperations'
)
BEGIN
    CREATE TABLE [CollectorCommands] (
        [Id] uniqueidentifier NOT NULL,
        [CollectorType] tinyint NOT NULL,
        [CommandType] tinyint NOT NULL,
        [TargetId] int NOT NULL,
        [Status] tinyint NOT NULL,
        [RequestedAtUtc] datetime2 NOT NULL,
        [RequestedBy] nvarchar(256) NOT NULL,
        [ProcessingStartedAtUtc] datetime2 NULL,
        [CompletedAtUtc] datetime2 NULL,
        [ResultMessage] nvarchar(2000) NULL,
        CONSTRAINT [PK_CollectorCommands] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719071212_AddCollectorOperations'
)
BEGIN
    CREATE TABLE [CollectorHeartbeats] (
        [Id] int NOT NULL IDENTITY,
        [CollectorType] tinyint NOT NULL,
        [InstanceName] nvarchar(256) NOT NULL,
        [LastHeartbeatUtc] datetime2 NOT NULL,
        [Version] nvarchar(64) NOT NULL,
        [StatusMessage] nvarchar(1000) NULL,
        CONSTRAINT [PK_CollectorHeartbeats] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719071212_AddCollectorOperations'
)
BEGIN
    CREATE INDEX [IX_CollectorCommands_CollectorType_Status_RequestedAtUtc] ON [CollectorCommands] ([CollectorType], [Status], [RequestedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719071212_AddCollectorOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CollectorHeartbeats_CollectorType_InstanceName] ON [CollectorHeartbeats] ([CollectorType], [InstanceName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719071212_AddCollectorOperations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719071212_AddCollectorOperations', N'10.0.9');
END;

COMMIT;
GO


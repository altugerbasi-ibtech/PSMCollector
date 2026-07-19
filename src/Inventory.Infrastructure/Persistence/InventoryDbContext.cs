using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<IisServer> IisServers => Set<IisServer>();
    public DbSet<SqlServer> SqlServers => Set<SqlServer>();
    public DbSet<CollectionRun> CollectionRuns => Set<CollectionRun>();
    public DbSet<IisConnectionStaging> IisConnectionStaging => Set<IisConnectionStaging>();
    public DbSet<ConnectionInventory> ConnectionInventory => Set<ConnectionInventory>();
    public DbSet<UnknownSqlEndpoint> UnknownSqlEndpoints => Set<UnknownSqlEndpoint>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CollectorCommand> CollectorCommands => Set<CollectorCommand>();
    public DbSet<CollectorHeartbeat> CollectorHeartbeats => Set<CollectorHeartbeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureIisServer(modelBuilder.Entity<IisServer>());
        ConfigureSqlServer(modelBuilder.Entity<SqlServer>());
        ConfigureCollectionRun(modelBuilder.Entity<CollectionRun>());
        ConfigureStaging(modelBuilder.Entity<IisConnectionStaging>());
        ConfigureInventory(modelBuilder.Entity<ConnectionInventory>());
        ConfigureUnknownEndpoint(modelBuilder.Entity<UnknownSqlEndpoint>());
        ConfigureAuditLog(modelBuilder.Entity<AuditLog>());
        ConfigureCollectorCommand(modelBuilder.Entity<CollectorCommand>());
        ConfigureCollectorHeartbeat(modelBuilder.Entity<CollectorHeartbeat>());
    }

    private static void ConfigureIisServer(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<IisServer> entity)
    {
        entity.ToTable("IisServers", table =>
        {
            table.HasCheckConstraint("CK_IisServers_CollectionInterval", "[CollectionIntervalSeconds] BETWEEN 10 AND 86400");
            table.HasCheckConstraint("CK_IisServers_ConnectionTimeout", "[ConnectionTimeoutSeconds] BETWEEN 5 AND 300");
        });
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.ServerName).IsUnique();
        entity.Property(x => x.ServerName).HasMaxLength(128);
        entity.Property(x => x.Fqdn).HasMaxLength(255);
        entity.Property(x => x.Description).HasMaxLength(500);
        entity.Property(x => x.LastErrorMessage).HasMaxLength(2000);
        ConfigureAudit(entity);
    }

    private static void ConfigureSqlServer(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<SqlServer> entity)
    {
        entity.ToTable("SqlServers", table =>
        {
            table.HasCheckConstraint("CK_SqlServers_Port", "[Port] = 1433");
            table.HasCheckConstraint("CK_SqlServers_ConnectionTimeout", "[ConnectionTimeoutSeconds] BETWEEN 5 AND 300");
        });
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.IpAddress).IsUnique();
        entity.Property(x => x.ServerName).HasMaxLength(128);
        entity.Property(x => x.Fqdn).HasMaxLength(255);
        entity.Property(x => x.IpAddress).HasColumnType("varchar(48)");
        entity.Property(x => x.LastErrorMessage).HasMaxLength(2000);
        ConfigureAudit(entity);
    }

    private static void ConfigureAudit<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> entity)
        where T : AuditableEntity
    {
        entity.Property(x => x.CreatedBy).HasMaxLength(256);
        entity.Property(x => x.UpdatedBy).HasMaxLength(256);
        entity.Property(x => x.RowVersion).IsRowVersion();
    }

    private static void ConfigureCollectionRun(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CollectionRun> entity)
    {
        entity.ToTable("CollectionRuns");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.ErrorSummary).HasMaxLength(2000);
        entity.HasIndex(x => new { x.Status, x.StartedAtUtc });
    }

    private static void ConfigureStaging(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<IisConnectionStaging> entity)
    {
        entity.ToTable("IisConnectionStaging", table =>
            table.HasCheckConstraint("CK_IisConnectionStaging_SqlServerPort", "[SqlServerPort] = 1433"));
        entity.HasKey(x => x.Id);
        entity.Property(x => x.CollectedAtUtc).HasPrecision(3);
        entity.Property(x => x.IisServerName).HasMaxLength(128);
        entity.Property(x => x.AppPoolName).HasMaxLength(256);
        entity.Property(x => x.ClientIp).HasColumnType("varchar(48)");
        entity.Property(x => x.SqlServerIp).HasColumnType("varchar(48)");
        entity.Property(x => x.LastErrorMessage).HasMaxLength(2000);
        entity.HasIndex(x => new { x.ProcessingStatus, x.CollectionId, x.SqlServerIp });
        entity.HasIndex(x => new
        {
            x.CollectionId,
            x.IisServerId,
            x.AppPoolName,
            x.WorkerProcessId,
            x.ClientIp,
            x.ClientPort,
            x.SqlServerIp,
            x.SqlServerPort
        }).IsUnique();
        entity.HasOne(x => x.CollectionRun).WithMany().HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.IisServer).WithMany().HasForeignKey(x => x.IisServerId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureInventory(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ConnectionInventory> entity)
    {
        entity.ToTable("ConnectionInventory", table =>
            table.HasCheckConstraint("CK_ConnectionInventory_Totals", "[TotalConnections] = [ActiveConnections] + [IdlePooledConnections]"));
        entity.HasKey(x => x.Id);
        entity.Property(x => x.InventoryDateUtc).HasPrecision(0);
        entity.Property(x => x.IisServerName).HasMaxLength(128);
        entity.Property(x => x.AppPoolName).HasMaxLength(256);
        entity.Property(x => x.SqlServerName).HasMaxLength(128);
        entity.Property(x => x.SqlInstance).HasMaxLength(128);
        entity.Property(x => x.DatabaseName).HasColumnType("sysname");
        entity.Property(x => x.SqlEndpoint).HasMaxLength(256);
        entity.Property(x => x.WorkerProcessIds).HasMaxLength(1000);
        entity.HasIndex(x => new { x.CollectionId, x.IisServerId, x.AppPoolName, x.SqlServerId, x.DatabaseName }).IsUnique();
        entity.HasIndex(x => x.InventoryDateUtc);
        entity.HasOne(x => x.CollectionRun).WithMany().HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.IisServer).WithMany().HasForeignKey(x => x.IisServerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.SqlServer).WithMany().HasForeignKey(x => x.SqlServerId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUnknownEndpoint(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<UnknownSqlEndpoint> entity)
    {
        entity.ToTable("UnknownSqlEndpoints", table => table.HasCheckConstraint("CK_UnknownSqlEndpoints_Port", "[Port] = 1433"));
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.IpAddress, x.Port }).IsUnique();
        entity.Property(x => x.IpAddress).HasColumnType("varchar(48)");
        entity.Property(x => x.LastIisServerName).HasMaxLength(128);
    }

    private static void ConfigureAuditLog(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<AuditLog> entity)
    {
        entity.ToTable("AuditLogs");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.OccurredAtUtc);
        entity.Property(x => x.UserName).HasMaxLength(256);
        entity.Property(x => x.Action).HasMaxLength(64);
        entity.Property(x => x.EntityType).HasMaxLength(128);
        entity.Property(x => x.EntityId).HasMaxLength(128);
    }

    private static void ConfigureCollectorCommand(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CollectorCommand> entity)
    {
        entity.ToTable("CollectorCommands");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.RequestedBy).HasMaxLength(256);
        entity.Property(x => x.ResultMessage).HasMaxLength(2000);
        entity.HasIndex(x => new { x.CollectorType, x.Status, x.RequestedAtUtc });
    }

    private static void ConfigureCollectorHeartbeat(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CollectorHeartbeat> entity)
    {
        entity.ToTable("CollectorHeartbeats");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.InstanceName).HasMaxLength(256);
        entity.Property(x => x.Version).HasMaxLength(64);
        entity.Property(x => x.StatusMessage).HasMaxLength(1000);
        entity.HasIndex(x => new { x.CollectorType, x.InstanceName }).IsUnique();
    }
}

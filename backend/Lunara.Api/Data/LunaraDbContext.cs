using Lunara.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lunara.Api.Data;

// Punto de acceso de EF Core a las entidades y lugar donde se declaran índices, tipos y relaciones.
public class LunaraDbContext(
    DbContextOptions<LunaraDbContext> options,
    CsvDataStore csvDataStore) : DbContext(options)
{
    private bool _csvPersistenceEnabled = true;

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<SparePartPurchase> SparePartPurchases => Set<SparePartPurchase>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<RepairOrder> RepairOrders => Set<RepairOrder>();
    public DbSet<RepairStatusHistory> RepairStatusHistory => Set<RepairStatusHistory>();
    public DbSet<AuthUser> AuthUsers => Set<AuthUser>();

    public void DisableCsvPersistence() => _csvPersistenceEnabled = false;

    public void EnableCsvPersistence() => _csvPersistenceEnabled = true;

    public Task SaveCsvSnapshotAsync(CancellationToken cancellationToken = default) =>
        csvDataStore.SaveAsync(this, cancellationToken);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var changes = base.SaveChanges(acceptAllChangesOnSuccess);
        if (_csvPersistenceEnabled)
            csvDataStore.SaveAsync(this).GetAwaiter().GetResult();
        return changes;
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var changes = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (_csvPersistenceEnabled)
            await csvDataStore.SaveAsync(this, cancellationToken);
        return changes;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Los índices evitan códigos repetidos y las relaciones protegen órdenes con historial asociado.
        modelBuilder.Entity<Client>().HasIndex(x => x.Cedula).IsUnique();
        modelBuilder.Entity<Client>().HasIndex(x => x.Email);
        modelBuilder.Entity<SparePart>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<SparePart>().Property(x => x.UnitPrice).HasPrecision(12, 2);
        modelBuilder.Entity<SparePartPurchase>().Property(x => x.UnitPrice).HasPrecision(12, 2);
        modelBuilder.Entity<RepairOrder>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<RepairOrder>().Property(x => x.Budget).HasPrecision(12, 2);
        modelBuilder.Entity<RepairOrder>()
            .HasOne(x => x.Client).WithMany(x => x.Repairs)
            .HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RepairOrder>()
            .HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RepairOrder>()
            .HasOne(x => x.Technician).WithMany().HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<RepairStatusHistory>()
            .HasOne(x => x.RepairOrder).WithMany(x => x.History)
            .HasForeignKey(x => x.RepairOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

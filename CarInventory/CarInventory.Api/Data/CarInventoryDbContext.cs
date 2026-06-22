using CarInventory.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInventory.Api.Data;

public class CarInventoryDbContext(DbContextOptions<CarInventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<Car>           Cars           => Set<Car>();
    public DbSet<Owner>         Owners         => Set<Owner>();
    public DbSet<ServiceRecord> ServiceRecords => Set<ServiceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Car>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Make).IsRequired().HasMaxLength(50);
            e.Property(c => c.Model).IsRequired().HasMaxLength(50);
            e.Property(c => c.Vin).IsRequired().HasMaxLength(17);
            e.HasIndex(c => c.Vin).IsUnique();
            e.Property(c => c.Price).HasColumnType("decimal(10,2)");
            e.Property(c => c.Status).HasDefaultValue("Available");
            e.HasOne(c => c.Owner)
             .WithMany(o => o.Cars)
             .HasForeignKey(c => c.OwnerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Owner>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Email).IsRequired().HasMaxLength(100);
            e.HasIndex(o => o.Email).IsUnique();
            e.Ignore(o => o.FullName); // computed — not stored
        });

        modelBuilder.Entity<ServiceRecord>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Description).IsRequired().HasMaxLength(500);
            e.Property(s => s.Cost).HasColumnType("decimal(8,2)");
            e.HasOne(s => s.Car)
             .WithMany(c => c.ServiceRecords)
             .HasForeignKey(s => s.CarId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

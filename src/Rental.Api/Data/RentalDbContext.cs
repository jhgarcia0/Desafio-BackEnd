using Microsoft.EntityFrameworkCore;
using Rental.Api.Models;

namespace Rental.Api.Data;

public class RentalDbContext : DbContext
{
    public RentalDbContext(DbContextOptions<RentalDbContext> options) : base(options) {}

    public DbSet<Moto> Motos => Set<Moto>();
    public DbSet<Courier> Couriers => Set<Courier>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Moto>(e =>
        {
            e.ToTable("motos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Identifier).IsRequired();
            e.Property(x => x.Year).IsRequired();
            e.Property(x => x.Model).IsRequired();
            e.Property(x => x.Plate).IsRequired();

            e.HasIndex(x => x.Plate).IsUnique();
        });
        modelBuilder.Entity<Courier>(e =>
        {
            e.ToTable("couriers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Identifier).IsRequired();
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Cnpj).IsRequired();
            e.Property(x => x.BirthDate).IsRequired();
            e.Property(x => x.CnhNumber).IsRequired();
            e.Property(x => x.CnhType).IsRequired();
            e.HasIndex(x => x.Cnpj).IsUnique();
            e.HasIndex(x => x.CnhNumber).IsUnique();
        });
    }
}

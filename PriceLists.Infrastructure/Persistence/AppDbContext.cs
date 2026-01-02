using Microsoft.EntityFrameworkCore;
using PriceLists.Core.Models;

namespace PriceLists.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<PriceList> PriceLists => Set<PriceList>();

    public DbSet<PriceItem> PriceItems => Set<PriceItem>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.ToTable("PriceLists");
            entity.HasKey(pl => pl.Id);
            entity.Property(pl => pl.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(pl => pl.SourceFileName)
                .HasMaxLength(260);
            entity.Property(pl => pl.ImportedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<PriceItem>(entity =>
        {
            entity.ToTable("PriceItems");
            entity.HasKey(pi => pi.Id);
            entity.Property(pi => pi.Description)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(pi => pi.Code)
                .HasMaxLength(200);
            entity.Property(pi => pi.SectionName)
                .HasMaxLength(200);

            entity.HasIndex(pi => new { pi.PriceListId, pi.Code });
            entity.HasIndex(pi => new { pi.PriceListId, pi.Description });

            entity.HasOne(pi => pi.PriceList)
                .WithMany(pl => pl.Items)
                .HasForeignKey(pi => pi.PriceListId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

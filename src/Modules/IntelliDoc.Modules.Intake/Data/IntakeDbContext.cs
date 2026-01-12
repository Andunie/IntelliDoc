using IntelliDoc.Modules.Intake.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntelliDoc.Modules.Intake.Data;

public class IntakeDbContext : DbContext
{
    public IntakeDbContext(DbContextOptions<IntakeDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tablolar "intake" şeması altında oluşacak (intake.Documents)
        modelBuilder.HasDefaultSchema("intake");

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StoragePath).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>(); // Enum'ı yazı olarak sakla
        });

        base.OnModelCreating(modelBuilder);
    }
}
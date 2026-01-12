using IntelliDoc.Modules.Extraction.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntelliDoc.Modules.Extraction.Data;

public class ExtractionDbContext : DbContext
{
    public ExtractionDbContext(DbContextOptions<ExtractionDbContext> options) : base(options)
    {
    }

    public DbSet<ExtractionResult> ExtractionResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ÖNEMLİ: Bu modülün tabloları "extraction" şemasına kaydedilecek.
        modelBuilder.HasDefaultSchema("extraction");

        modelBuilder.Entity<ExtractionResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JsonData).HasColumnType("jsonb"); // Postgres JSON formatı
        });

        base.OnModelCreating(modelBuilder);
    }
}
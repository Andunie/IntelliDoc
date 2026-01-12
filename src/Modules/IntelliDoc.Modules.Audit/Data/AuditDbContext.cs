using IntelliDoc.Modules.Audit.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace IntelliDoc.Modules.Audit.Data;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    public DbSet<AuditRecord> AuditRecords { get; set; }
    public DbSet<FieldHistory> FieldHistories { get; set; }
    public DbSet<BusinessRuleLog> BusinessRuleLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("audit");

        // İlişkiler
        modelBuilder.Entity<AuditRecord>()
            .HasMany(r => r.FieldHistories)
            .WithOne()
            .HasForeignKey(fh => fh.AuditRecordId);

        base.OnModelCreating(modelBuilder);
    }
}
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AnomalyDetector.Model;

public partial class VariableStoreContext : DbContext
{
    public VariableStoreContext()
    {
    }

    public VariableStoreContext(DbContextOptions<VariableStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<RecordItem> RecordItems { get; set; }

    public virtual DbSet<TrainedModel> TrainedModels { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=tcp:sql-variable-store.database.windows.net,1433;Initial Catalog=variableStore;Persist Security Info=False;User ID=rooty;Password=AZnr6BZBHvUb88sX5yEu;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("Device");

            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RecordItem>(entity =>
        {
            entity.ToTable("RecordItem");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.RecordName)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Device).WithMany()
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecordItem_Computer");
        });

        modelBuilder.Entity<TrainedModel>(entity =>
        {
            entity.ToTable("TrainedModel");

            entity.HasOne(d => d.Device).WithMany()
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrainedModel_Computer");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

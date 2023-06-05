using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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

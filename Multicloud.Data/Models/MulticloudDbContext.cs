using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Multicloud.Data.Models;

public partial class MulticloudDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public MulticloudDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public MulticloudDbContext(DbContextOptions<MulticloudDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Region> Regions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(_configuration.GetConnectionString("MulticloudDB"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Regions__3214EC2709926C7B");

            entity.HasIndex(e => e.Name, "UQ__Regions__737584F63CEB3AA5").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RegionalDisplayName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

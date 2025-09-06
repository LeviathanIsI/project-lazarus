using Lazarus.Data.Entities;
using Lazarus.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lazarus.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the Model entity.
/// </summary>
internal class ModelConfiguration : IEntityTypeConfiguration<Model>
{
    public void Configure(EntityTypeBuilder<Model> builder)
    {
        builder.ToTable("Models");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(255)
            .IsUnicode(true);

        builder.Property(m => m.Path)
            .IsRequired()
            .HasMaxLength(500)
            .IsUnicode(true);

        builder.Property(m => m.RunnerType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.Parameters)
            .IsRequired(false)
            .IsUnicode(true);

        builder.Property(m => m.CreatedAt)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(m => m.LastModified)
            .IsRequired()
            .HasColumnType("TEXT");

        // Indexes for performance
        builder.HasIndex(m => m.IsActive)
            .HasDatabaseName("IX_Models_IsActive")
            .HasFilter("IsActive = 1");

        builder.HasIndex(m => m.RunnerType)
            .HasDatabaseName("IX_Models_RunnerType");

        builder.HasIndex(m => m.Name)
            .HasDatabaseName("IX_Models_Name");
    }
}
using Lazarus.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lazarus.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the Settings entity.
/// </summary>
internal class SettingsConfiguration : IEntityTypeConfiguration<Settings>
{
    public void Configure(EntityTypeBuilder<Settings> builder)
    {
        builder.ToTable("Settings");

        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(255)
            .IsUnicode(true);

        builder.Property(s => s.Value)
            .IsRequired(false)
            .IsUnicode(true);

        builder.Property(s => s.LastModified)
            .IsRequired()
            .HasColumnType("TEXT");

        // Index for performance
        builder.HasIndex(s => s.LastModified)
            .HasDatabaseName("IX_Settings_LastModified");
    }
}
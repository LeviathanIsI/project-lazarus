using Lazarus.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lazarus.Data.Configurations;

public sealed class ImageJobConfiguration : IEntityTypeConfiguration<ImageJob>
{
    public void Configure(EntityTypeBuilder<ImageJob> builder)
    {
        builder.ToTable("ImageJobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedAt).HasColumnType("TEXT");
        builder.HasIndex(x => x.CreatedAt);
        builder.Property(x => x.Mode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.OutputPath).HasMaxLength(1024);
        builder.Property(x => x.Prompt).HasMaxLength(4000);
        builder.Property(x => x.NegativePrompt).HasMaxLength(4000);
    }
}


using Lazarus.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lazarus.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the Conversation entity.
/// </summary>
internal class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(255)
            .IsUnicode(true);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(c => c.LastMessageAt)
            .IsRequired()
            .HasColumnType("TEXT");

        // Configure relationship with Messages
        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_Conversations_CreatedAt");

        builder.HasIndex(c => c.LastMessageAt)
            .HasDatabaseName("IX_Conversations_LastMessageAt");
    }
}
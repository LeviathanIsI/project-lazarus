using Lazarus.Data.Entities;
using Lazarus.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lazarus.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the Message entity.
/// </summary>
internal class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Content)
            .IsRequired()
            .IsUnicode(true);

        builder.Property(m => m.Timestamp)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(m => m.TokenCount)
            .IsRequired(false);

        // Configure relationship with Conversation
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(m => m.ConversationId)
            .HasDatabaseName("IX_Messages_ConversationId");

        builder.HasIndex(m => m.Timestamp)
            .HasDatabaseName("IX_Messages_Timestamp");

        builder.HasIndex(m => m.Role)
            .HasDatabaseName("IX_Messages_Role");
    }
}
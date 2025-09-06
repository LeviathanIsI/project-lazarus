using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lazarus.Data.Enums;

namespace Lazarus.Data.Entities;

/// <summary>
/// Represents a single message within a conversation.
/// </summary>
public class Message
{
    /// <summary>
    /// Gets or sets the unique identifier for this message.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the identifier of the conversation this message belongs to.
    /// </summary>
    [Required]
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the role of the message sender.
    /// </summary>
    [Required]
    public MessageRole Role { get; set; }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when this message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the token count for this message (if available).
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    /// Gets or sets the conversation this message belongs to.
    /// </summary>
    [ForeignKey(nameof(ConversationId))]
    public virtual Conversation? Conversation { get; set; }
}
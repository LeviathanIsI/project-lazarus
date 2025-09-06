using System.ComponentModel.DataAnnotations;

namespace Lazarus.Data.Entities;

/// <summary>
/// Represents a conversation containing multiple messages.
/// </summary>
public class Conversation
{
    /// <summary>
    /// Gets or sets the unique identifier for this conversation.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display title of the conversation.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when this conversation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the last message was added to this conversation.
    /// </summary>
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the collection of messages in this conversation.
    /// </summary>
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
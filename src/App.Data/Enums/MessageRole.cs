namespace Lazarus.Data.Enums;

/// <summary>
/// Represents the role of a message participant in a conversation.
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// System message providing context or instructions.
    /// </summary>
    System = 0,

    /// <summary>
    /// User-provided input message.
    /// </summary>
    User = 1,

    /// <summary>
    /// AI assistant response message.
    /// </summary>
    Assistant = 2
}
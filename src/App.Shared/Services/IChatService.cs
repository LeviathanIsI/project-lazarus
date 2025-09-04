using System.Collections.ObjectModel;

namespace Lazarus.App.Shared.Services;

/// <summary>
/// Service contract for managing chat conversations and LLM interactions
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Gets the collection of available conversations
    /// </summary>
    ObservableCollection<Conversation> Conversations { get; }

    /// <summary>
    /// Gets the currently active conversation
    /// </summary>
    Conversation? ActiveConversation { get; }

    /// <summary>
    /// Event raised when a new message chunk is received during streaming
    /// </summary>
    event EventHandler<MessageChunkReceivedEventArgs>? MessageChunkReceived;

    /// <summary>
    /// Event raised when a message is completed
    /// </summary>
    event EventHandler<MessageCompletedEventArgs>? MessageCompleted;

    /// <summary>
    /// Event raised when an error occurs during chat operations
    /// </summary>
    event EventHandler<ChatErrorEventArgs>? ChatError;

    /// <summary>
    /// Creates a new conversation
    /// </summary>
    /// <param name="title">The conversation title</param>
    /// <returns>The created conversation</returns>
    Task<Conversation> CreateConversationAsync(string? title = null);

    /// <summary>
    /// Selects an active conversation
    /// </summary>
    /// <param name="conversation">The conversation to activate</param>
    /// <returns>Task representing the operation</returns>
    Task SetActiveConversationAsync(Conversation conversation);

    /// <summary>
    /// Sends a message in the active conversation with streaming response
    /// </summary>
    /// <param name="content">The message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task SendMessageAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation
    /// </summary>
    /// <param name="conversation">The conversation to delete</param>
    /// <returns>Task representing the operation</returns>
    Task DeleteConversationAsync(Conversation conversation);

    /// <summary>
    /// Gets the current runner connection status
    /// </summary>
    /// <returns>The runner status</returns>
    RunnerStatus GetRunnerStatus();

    /// <summary>
    /// Searches conversations by title or content
    /// </summary>
    /// <param name="searchText">The search text</param>
    /// <returns>Filtered conversations</returns>
    Task<IEnumerable<Conversation>> SearchConversationsAsync(string searchText);
}

/// <summary>
/// Represents a chat conversation
/// </summary>
public class Conversation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Conversation"/> class
    /// </summary>
    /// <param name="id">The conversation ID</param>
    /// <param name="title">The conversation title</param>
    public Conversation(Guid id, string title)
    {
        Id = id;
        Title = title;
        Messages = new ObservableCollection<ChatMessage>();
        CreatedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the unique identifier for the conversation
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets or sets the conversation title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets the collection of messages in this conversation
    /// </summary>
    public ObservableCollection<ChatMessage> Messages { get; }

    /// <summary>
    /// Gets when the conversation was created
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets or sets when the conversation was last active
    /// </summary>
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// Gets or sets whether there are unread messages
    /// </summary>
    public bool HasUnreadMessages { get; set; }

    /// <summary>
    /// Gets the last message preview
    /// </summary>
    public string? LastMessagePreview => Messages.LastOrDefault()?.Content?.Substring(0, Math.Min(100, Messages.LastOrDefault()?.Content?.Length ?? 0));
}

/// <summary>
/// Represents a chat message
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessage"/> class
    /// </summary>
    /// <param name="id">The message ID</param>
    /// <param name="content">The message content</param>
    /// <param name="role">The message role</param>
    public ChatMessage(Guid id, string content, MessageRole role)
    {
        Id = id;
        Content = content;
        Role = role;
        Timestamp = DateTime.UtcNow;
        IsStreaming = false;
    }

    /// <summary>
    /// Gets the unique identifier for the message
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets or sets the message content
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Gets the message role
    /// </summary>
    public MessageRole Role { get; }

    /// <summary>
    /// Gets when the message was created
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets or sets whether the message is currently being streamed
    /// </summary>
    public bool IsStreaming { get; set; }

    /// <summary>
    /// Gets or sets any error associated with the message
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Represents the role of a message sender
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// Message from the user
    /// </summary>
    User,

    /// <summary>
    /// Message from the assistant/AI
    /// </summary>
    Assistant,

    /// <summary>
    /// System message
    /// </summary>
    System
}

/// <summary>
/// Represents the runner connection status
/// </summary>
public class RunnerStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunnerStatus"/> class
    /// </summary>
    /// <param name="isConnected">Whether the runner is connected</param>
    /// <param name="isHealthy">Whether the runner is healthy</param>
    /// <param name="statusMessage">The status message</param>
    public RunnerStatus(bool isConnected, bool isHealthy, string statusMessage)
    {
        IsConnected = isConnected;
        IsHealthy = isHealthy;
        StatusMessage = statusMessage;
    }

    /// <summary>
    /// Gets whether the runner is connected
    /// </summary>
    public bool IsConnected { get; }

    /// <summary>
    /// Gets whether the runner is healthy
    /// </summary>
    public bool IsHealthy { get; }

    /// <summary>
    /// Gets the status message
    /// </summary>
    public string StatusMessage { get; }
}

/// <summary>
/// Event arguments for message chunk received events
/// </summary>
public class MessageChunkReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageChunkReceivedEventArgs"/> class
    /// </summary>
    /// <param name="messageId">The message ID</param>
    /// <param name="chunk">The message chunk</param>
    public MessageChunkReceivedEventArgs(Guid messageId, string chunk)
    {
        MessageId = messageId;
        Chunk = chunk;
    }

    /// <summary>
    /// Gets the message ID
    /// </summary>
    public Guid MessageId { get; }

    /// <summary>
    /// Gets the message chunk
    /// </summary>
    public string Chunk { get; }
}

/// <summary>
/// Event arguments for message completed events
/// </summary>
public class MessageCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageCompletedEventArgs"/> class
    /// </summary>
    /// <param name="messageId">The message ID</param>
    /// <param name="fullContent">The complete message content</param>
    public MessageCompletedEventArgs(Guid messageId, string fullContent)
    {
        MessageId = messageId;
        FullContent = fullContent;
    }

    /// <summary>
    /// Gets the message ID
    /// </summary>
    public Guid MessageId { get; }

    /// <summary>
    /// Gets the complete message content
    /// </summary>
    public string FullContent { get; }
}

/// <summary>
/// Event arguments for chat error events
/// </summary>
public class ChatErrorEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatErrorEventArgs"/> class
    /// </summary>
    /// <param name="error">The error message</param>
    /// <param name="exception">The exception</param>
    public ChatErrorEventArgs(string error, Exception? exception = null)
    {
        Error = error;
        Exception = exception;
    }

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Gets the exception
    /// </summary>
    public Exception? Exception { get; }
}
using Lazarus.Data.Entities;
using Lazarus.Data.Enums;

namespace Lazarus.Data.Repositories;

/// <summary>
/// Repository interface for message-specific data operations.
/// </summary>
public interface IMessageRepository : IRepository<Message>
{
    /// <summary>
    /// Gets messages for a specific conversation asynchronously.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The messages for the conversation ordered by timestamp.</returns>
    Task<IEnumerable<Message>> GetMessagesByConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages for a specific conversation with pagination asynchronously.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The number of messages to take.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The paginated messages for the conversation.</returns>
    Task<IEnumerable<Message>> GetMessagesByConversationAsync(Guid conversationId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last message in a conversation asynchronously.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The last message if found; otherwise, null.</returns>
    Task<Message?> GetLastMessageAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches messages by content asynchronously.
    /// </summary>
    /// <param name="searchTerm">The search term to match in content.</param>
    /// <param name="conversationId">Optional conversation identifier to limit search scope.</param>
    /// <param name="limit">The maximum number of messages to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching messages.</returns>
    Task<IEnumerable<Message>> SearchByContentAsync(string searchTerm, Guid? conversationId = null, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages by role asynchronously.
    /// </summary>
    /// <param name="role">The message role to filter by.</param>
    /// <param name="conversationId">Optional conversation identifier to limit search scope.</param>
    /// <param name="limit">The maximum number of messages to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The messages with the specified role.</returns>
    Task<IEnumerable<Message>> GetMessagesByRoleAsync(MessageRole role, Guid? conversationId = null, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total token count for a conversation asynchronously.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total token count for the conversation.</returns>
    Task<int> GetTotalTokenCountAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
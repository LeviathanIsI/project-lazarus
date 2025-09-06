using Lazarus.Data.Entities;

namespace Lazarus.Data.Repositories;

/// <summary>
/// Repository interface for conversation-specific data operations.
/// </summary>
public interface IConversationRepository : IRepository<Conversation>
{
    /// <summary>
    /// Gets conversations ordered by last message date asynchronously.
    /// </summary>
    /// <param name="limit">The maximum number of conversations to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The most recent conversations.</returns>
    Task<IEnumerable<Conversation>> GetRecentConversationsAsync(int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a conversation with all its messages asynchronously.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The conversation with messages if found; otherwise, null.</returns>
    Task<Conversation?> GetConversationWithMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches conversations by title asynchronously.
    /// </summary>
    /// <param name="searchTerm">The search term to match in titles.</param>
    /// <param name="limit">The maximum number of conversations to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching conversations.</returns>
    Task<IEnumerable<Conversation>> SearchByTitleAsync(string searchTerm, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last message timestamp for a conversation asynchronously.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="timestamp">The new timestamp.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateLastMessageTimestampAsync(Guid conversationId, DateTime timestamp, CancellationToken cancellationToken = default);
}
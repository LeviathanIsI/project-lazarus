using Lazarus.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lazarus.Data.Repositories;

/// <summary>
/// Repository implementation for conversation-specific data operations.
/// </summary>
public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    private readonly LazarusDbContext _context;
    private readonly ILogger<ConversationRepository>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Optional logger for repository operations.</param>
    public ConversationRepository(LazarusDbContext context, ILogger<ConversationRepository>? logger = null)
        : base(context, logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Conversation>> GetRecentConversationsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Conversations
                .OrderByDescending(c => c.LastMessageAt)
                .Take(limit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get recent conversations with limit {Limit}", limit);
            throw;
        }
    }

    public async Task<Conversation?> GetConversationWithMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get conversation with messages for ID {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<IEnumerable<Conversation>> SearchByTitleAsync(string searchTerm, int limit = 50, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchTerm);

        try
        {
            return await _context.Conversations
                .Where(c => c.Title.Contains(searchTerm))
                .OrderByDescending(c => c.LastMessageAt)
                .Take(limit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to search conversations by title with term '{SearchTerm}'", searchTerm);
            throw;
        }
    }

    public async Task<bool> UpdateLastMessageTimestampAsync(Guid conversationId, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowsAffected = await _context.Database
                .ExecuteSqlRawAsync(
                    "UPDATE Conversations SET LastMessageAt = {0} WHERE Id = {1}",
                    [timestamp.ToString("O"), conversationId.ToString()],
                    cancellationToken)
                .ConfigureAwait(false);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to update last message timestamp for conversation {ConversationId}", conversationId);
            throw;
        }
    }
}
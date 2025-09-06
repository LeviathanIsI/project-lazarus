using Lazarus.Data.Entities;
using Lazarus.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lazarus.Data.Repositories;

/// <summary>
/// Repository implementation for message-specific data operations.
/// </summary>
public class MessageRepository : Repository<Message>, IMessageRepository
{
    private readonly LazarusDbContext _context;
    private readonly ILogger<MessageRepository>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Optional logger for repository operations.</param>
    public MessageRepository(LazarusDbContext context, ILogger<MessageRepository>? logger = null)
        : base(context, logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Message>> GetMessagesByConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get messages for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<IEnumerable<Message>> GetMessagesByConversationAsync(Guid conversationId, int skip, int take, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get paginated messages for conversation {ConversationId} (skip: {Skip}, take: {Take})", conversationId, skip, take);
            throw;
        }
    }

    public async Task<Message?> GetLastMessageAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get last message for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<IEnumerable<Message>> SearchByContentAsync(string searchTerm, Guid? conversationId = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchTerm);

        try
        {
            var query = _context.Messages.Where(m => m.Content.Contains(searchTerm));

            if (conversationId.HasValue)
            {
                query = query.Where(m => m.ConversationId == conversationId.Value);
            }

            return await query
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to search messages by content with term '{SearchTerm}' for conversation {ConversationId}", searchTerm, conversationId);
            throw;
        }
    }

    public async Task<IEnumerable<Message>> GetMessagesByRoleAsync(MessageRole role, Guid? conversationId = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Messages.Where(m => m.Role == role);

            if (conversationId.HasValue)
            {
                query = query.Where(m => m.ConversationId == conversationId.Value);
            }

            return await query
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get messages by role {Role} for conversation {ConversationId}", role, conversationId);
            throw;
        }
    }

    public async Task<int> GetTotalTokenCountAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId && m.TokenCount.HasValue)
                .SumAsync(m => m.TokenCount!.Value, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get total token count for conversation {ConversationId}", conversationId);
            throw;
        }
    }
}
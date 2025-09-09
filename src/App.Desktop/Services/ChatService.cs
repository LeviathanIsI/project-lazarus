using Lazarus.Data.Entities;
using Lazarus.Data.Enums;
using Lazarus.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.Services;

public sealed class ChatService : IChatService
{
    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;
    private readonly ILogger<ChatService>? _logger;

    // In-memory fallback if DB access fails
    private readonly List<Conversation> _memConversations = new();
    private readonly Dictionary<Guid, List<Message>> _memMessages = new();
    private bool _useMemoryFallback;

    public ChatService(IConversationRepository conversations, IMessageRepository messages, ILogger<ChatService>? logger = null)
    {
        _conversations = conversations;
        _messages = messages;
        _logger = logger;
    }

    public async Task<List<Conversation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_useMemoryFallback)
            return _memConversations.OrderByDescending(c => c.LastMessageAt).ToList();

        try
        {
            var items = await _conversations.GetRecentConversationsAsync(100, cancellationToken).ConfigureAwait(false);
            return items.ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ChatService.GetAllAsync failed; switching to in-memory fallback");
            _useMemoryFallback = true;
            return _memConversations.OrderByDescending(c => c.LastMessageAt).ToList();
        }
    }

    public async Task<Conversation> CreateAsync(string? title = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var convo = new Conversation
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(title) ? "New Chat" : title!,
            CreatedAt = now,
            LastMessageAt = now
        };

        if (_useMemoryFallback)
        {
            _memConversations.Add(convo);
            return convo;
        }

        try
        {
            _conversations.Add(convo);
            await _conversations.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return convo;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ChatService.CreateAsync failed; switching to in-memory fallback");
            _useMemoryFallback = true;
            _memConversations.Add(convo);
            return convo;
        }
    }

    public async Task RenameAsync(Guid chatId, string title, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (_useMemoryFallback)
        {
            var c = _memConversations.FirstOrDefault(x => x.Id == chatId);
            if (c != null) c.Title = title;
            return;
        }

        try
        {
            var existing = await _conversations.GetByIdAsync(chatId, cancellationToken).ConfigureAwait(false);
            if (existing != null)
            {
                existing.Title = title;
                _conversations.Update(existing);
                await _conversations.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ChatService.RenameAsync failed; switching to in-memory fallback");
            _useMemoryFallback = true;
            var c = _memConversations.FirstOrDefault(x => x.Id == chatId);
            if (c != null) c.Title = title;
        }
    }

    public async Task DeleteAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        if (_useMemoryFallback)
        {
            _memConversations.RemoveAll(x => x.Id == chatId);
            _memMessages.Remove(chatId);
            return;
        }

        try
        {
            var existing = await _conversations.GetByIdAsync(chatId, cancellationToken).ConfigureAwait(false);
            if (existing != null)
            {
                _conversations.Remove(existing);
                await _conversations.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ChatService.DeleteAsync failed; switching to in-memory fallback");
            _useMemoryFallback = true;
            _memConversations.RemoveAll(x => x.Id == chatId);
            _memMessages.Remove(chatId);
        }
    }

    public async Task<List<Message>> GetMessagesAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        if (_useMemoryFallback)
        {
            if (_memMessages.TryGetValue(chatId, out var list))
                return list.OrderBy(m => m.Timestamp).ToList();
            return new List<Message>();
        }

        try
        {
            var items = await _messages.GetMessagesByConversationAsync(chatId, cancellationToken).ConfigureAwait(false);
            return items.ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ChatService.GetMessagesAsync failed; switching to in-memory fallback");
            _useMemoryFallback = true;
            if (_memMessages.TryGetValue(chatId, out var list))
                return list.OrderBy(m => m.Timestamp).ToList();
            return new List<Message>();
        }
    }

    public async Task<Message> AddMessageAsync(Guid chatId, MessageRole role, string content, DateTime? timestamp = null, CancellationToken cancellationToken = default)
    {
        var msg = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = chatId,
            Role = role,
            Content = content,
            Timestamp = timestamp ?? DateTime.UtcNow
        };

        if (_useMemoryFallback)
        {
            if (!_memMessages.TryGetValue(chatId, out var list))
            {
                list = new List<Message>();
                _memMessages[chatId] = list;
            }
            list.Add(msg);
            var convo = _memConversations.FirstOrDefault(c => c.Id == chatId);
            if (convo != null) convo.LastMessageAt = msg.Timestamp;
            return msg;
        }

        try
        {
            _messages.Add(msg);
            await _messages.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _conversations.UpdateLastMessageTimestampAsync(chatId, msg.Timestamp, cancellationToken).ConfigureAwait(false);
            return msg;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ChatService.AddMessageAsync failed; switching to in-memory fallback");
            _useMemoryFallback = true;
            if (!_memMessages.TryGetValue(chatId, out var list))
            {
                list = new List<Message>();
                _memMessages[chatId] = list;
            }
            list.Add(msg);
            var convo = _memConversations.FirstOrDefault(c => c.Id == chatId);
            if (convo != null) convo.LastMessageAt = msg.Timestamp;
            return msg;
        }
    }
}


using Lazarus.Data.Entities;
using Lazarus.Data.Enums;
using Lazarus.Data.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Lazarus.Desktop.Services;

public sealed class ChatService : IChatService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChatService>? _logger;

    // In-memory fallback if DB access fails
    private readonly List<Conversation> _memConversations = new();
    private readonly Dictionary<Guid, List<Message>> _memMessages = new();
    private bool _useMemoryFallback;
    private readonly JsonSerializerOptions _json = new JsonSerializerOptions { WriteIndented = true };

    public ChatService(IServiceScopeFactory scopeFactory, ILogger<ChatService>? logger = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<List<Conversation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Always attempt DB first; fall back to memory only if this call fails
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var conv = scope.ServiceProvider.GetRequiredService<IConversationRepository>();
            var items = (await conv.GetRecentConversationsAsync(100, cancellationToken).ConfigureAwait(false)).ToList();

            if (items.Count == 0)
            {
                // Try to hydrate DB from JSON exports
                var imported = await TryImportFromFilesAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
                if (imported.Count > 0)
                    return imported;

                // As a last resort, read directly from files (without DB)
                var fileOnly = await LoadConversationsFromFilesAsync(cancellationToken).ConfigureAwait(false);
                if (fileOnly.Count > 0)
                    return fileOnly.OrderByDescending(c => c.LastMessageAt).ToList();
            }

            _useMemoryFallback = false; // DB is healthy
            return items;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ChatService.GetAllAsync failed; switching to in-memory fallback");
            _useMemoryFallback = true;
            var mem = _memConversations.OrderByDescending(c => c.LastMessageAt).ToList();
            if (mem.Count == 0)
            {
                // Even in memory fallback, try to at least surface file-backed conversations
                try
                {
                    var fileOnly = await LoadConversationsFromFilesAsync(cancellationToken).ConfigureAwait(false);
                    if (fileOnly.Count > 0) return fileOnly.OrderByDescending(c => c.LastMessageAt).ToList();
                }
                catch { }
            }
            return mem;
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
            try { await SaveConversationFileAsync(convo, Array.Empty<Message>()).ConfigureAwait(false); } catch { }
            return convo;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var convRepo = scope.ServiceProvider.GetRequiredService<IConversationRepository>();
            convRepo.Add(convo);
            await convRepo.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await SaveConversationFileAsync(convo, Array.Empty<Message>()).ConfigureAwait(false);
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
            if (c != null)
            {
                c.Title = title;
                try { await UpdateConversationTitleAsync(c).ConfigureAwait(false); } catch { }
            }
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var convRepo = scope.ServiceProvider.GetRequiredService<IConversationRepository>();
            var existing = await convRepo.GetByIdAsync(chatId, cancellationToken).ConfigureAwait(false);
            if (existing != null)
            {
                existing.Title = title;
                convRepo.Update(existing);
                await convRepo.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await UpdateConversationTitleAsync(existing).ConfigureAwait(false);
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
            TryDeleteConversationFile(chatId);
            _memMessages.Remove(chatId);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var convRepo = scope.ServiceProvider.GetRequiredService<IConversationRepository>();
            var existing = await convRepo.GetByIdAsync(chatId, cancellationToken).ConfigureAwait(false);
            if (existing != null)
            {
                convRepo.Remove(existing);
                await convRepo.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                TryDeleteConversationFile(chatId);
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
        // Always attempt DB first; fall back to memory only if this call fails
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var msgRepo = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
            var items = await msgRepo.GetMessagesByConversationAsync(chatId, cancellationToken).ConfigureAwait(false);
            _useMemoryFallback = false; // DB is healthy
            var list = items.ToList();
            if (list.Count == 0)
            {
                // Read from file if DB has not been hydrated yet
                var fileMsgs = await LoadMessagesFromFileAsync(chatId, cancellationToken).ConfigureAwait(false);
                if (fileMsgs.Count > 0) return fileMsgs;
            }
            return list;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ChatService.GetMessagesAsync failed; switching to in-memory fallback");
            _useMemoryFallback = true;
            if (_memMessages.TryGetValue(chatId, out var list))
                return list.OrderBy(m => m.Timestamp).ToList();
            // Fallback to file if available
            try
            {
                var fileMsgs = await LoadMessagesFromFileAsync(chatId, cancellationToken).ConfigureAwait(false);
                if (fileMsgs.Count > 0) return fileMsgs;
            }
            catch { }
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
            try { await AppendMessageToFileAsync(chatId, msg).ConfigureAwait(false); } catch { }
            return msg;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var msgRepo = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
            var convRepo = scope.ServiceProvider.GetRequiredService<IConversationRepository>();
            msgRepo.Add(msg);
            await msgRepo.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await convRepo.UpdateLastMessageTimestampAsync(chatId, msg.Timestamp, cancellationToken).ConfigureAwait(false);
            await AppendMessageToFileAsync(chatId, msg).ConfigureAwait(false);
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
    private static string GetConversationsFolder()
    {
        return Path.Combine(Lazarus.Shared.LazarusPaths.Root, "Conversations");
    }

    private string GetConversationFilePath(Guid id)
    {
        var dir = GetConversationsFolder();
        try { Directory.CreateDirectory(dir); } catch { }
        return Path.Combine(dir, $"{id}.json");
    }

    private async Task SaveConversationFileAsync(Conversation convo, IEnumerable<Message> messages)
    {
        var file = GetConversationFilePath(convo.Id);
        var model = new ConversationFile
        {
            Id = convo.Id,
            Title = convo.Title,
            CreatedAt = convo.CreatedAt,
            UpdatedAt = convo.LastMessageAt,
            Messages = messages.Select(m => new ConversationFileMessage
            {
                Id = m.Id,
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content,
                Timestamp = m.Timestamp
            }).ToList()
        };
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(model, _json));
    }

    private async Task AppendMessageToFileAsync(Guid chatId, Message message)
    {
        var file = GetConversationFilePath(chatId);
        ConversationFile model;
        if (File.Exists(file))
        {
            try { model = JsonSerializer.Deserialize<ConversationFile>(await File.ReadAllTextAsync(file), _json) ?? new ConversationFile { Id = chatId, Title = "New Chat", CreatedAt = message.Timestamp }; }
            catch { model = new ConversationFile { Id = chatId, Title = "New Chat", CreatedAt = message.Timestamp }; }
        }
        else
        {
            model = new ConversationFile { Id = chatId, Title = "New Chat", CreatedAt = message.Timestamp };
        }

        model.Messages ??= new List<ConversationFileMessage>();
        model.Messages.Add(new ConversationFileMessage { Id = message.Id, Role = message.Role.ToString().ToLowerInvariant(), Content = message.Content, Timestamp = message.Timestamp });
        model.UpdatedAt = message.Timestamp;
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(model, _json));
    }

    private async Task UpdateConversationTitleAsync(Conversation convo)
    {
        var file = GetConversationFilePath(convo.Id);
        if (!File.Exists(file)) return;
        try
        {
            var model = JsonSerializer.Deserialize<ConversationFile>(await File.ReadAllTextAsync(file), _json);
            if (model != null)
            {
                model.Title = convo.Title;
                await File.WriteAllTextAsync(file, JsonSerializer.Serialize(model, _json));
            }
        }
        catch { }
    }

    private async Task<List<Conversation>> LoadConversationsFromFilesAsync(CancellationToken ct)
    {
        var result = new List<Conversation>();
        var dir = GetConversationsFolder();
        if (!Directory.Exists(dir)) return result;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                var model = JsonSerializer.Deserialize<ConversationFile>(await File.ReadAllTextAsync(file, ct), _json);
                if (model == null) continue;
                result.Add(new Conversation
                {
                    Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id,
                    Title = string.IsNullOrWhiteSpace(model.Title) ? "New Chat" : model.Title!,
                    CreatedAt = model.CreatedAt,
                    LastMessageAt = model.UpdatedAt == default ? model.CreatedAt : model.UpdatedAt
                });
            }
            catch { }
        }
        return result;
    }

    private async Task<List<Message>> LoadMessagesFromFileAsync(Guid chatId, CancellationToken ct)
    {
        var list = new List<Message>();
        var file = GetConversationFilePath(chatId);
        if (!File.Exists(file)) return list;
        try
        {
            var model = JsonSerializer.Deserialize<ConversationFile>(await File.ReadAllTextAsync(file, ct), _json);
            if (model?.Messages == null) return list;
            foreach (var m in model.Messages.OrderBy(m => m.Timestamp))
            {
                var role = (m.Role ?? "user").ToLowerInvariant() switch
                {
                    "assistant" => MessageRole.Assistant,
                    "system" => MessageRole.System,
                    _ => MessageRole.User
                };
                list.Add(new Message
                {
                    Id = m.Id == Guid.Empty ? Guid.NewGuid() : m.Id,
                    ConversationId = chatId,
                    Role = role,
                    Content = m.Content ?? string.Empty,
                    Timestamp = m.Timestamp == default ? DateTime.UtcNow : m.Timestamp
                });
            }
        }
        catch { }
        return list;
    }

    private void TryDeleteConversationFile(Guid chatId)
    {
        try
        {
            var file = GetConversationFilePath(chatId);
            if (File.Exists(file)) File.Delete(file);
        }
        catch { }
    }

    private async Task<List<Conversation>> TryImportFromFilesAsync(IServiceProvider sp, CancellationToken ct)
    {
        var list = new List<Conversation>();
        var dir = GetConversationsFolder();
        if (!Directory.Exists(dir)) return list;
        try
        {
            var convRepo = sp.GetRequiredService<IConversationRepository>();
            var msgRepo = sp.GetRequiredService<IMessageRepository>();
            foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
            {
                ConversationFile? model = null;
                try { model = JsonSerializer.Deserialize<ConversationFile>(await File.ReadAllTextAsync(file, ct), _json); }
                catch { }
                if (model == null) continue;
                var convo = new Conversation { Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id, Title = model.Title ?? "New Chat", CreatedAt = model.CreatedAt, LastMessageAt = model.UpdatedAt == default ? model.CreatedAt : model.UpdatedAt };
                convRepo.Add(convo);
                if (model.Messages != null)
                {
                    foreach (var m in model.Messages)
                    {
                        var role = m.Role?.ToLowerInvariant() switch
                        {
                            "assistant" => MessageRole.Assistant,
                            "system" => MessageRole.System,
                            _ => MessageRole.User
                        };
                        msgRepo.Add(new Message { Id = m.Id == Guid.Empty ? Guid.NewGuid() : m.Id, ConversationId = convo.Id, Role = role, Content = m.Content ?? string.Empty, Timestamp = m.Timestamp == default ? DateTime.UtcNow : m.Timestamp });
                    }
                }
            }
            await convRepo.SaveChangesAsync(ct).ConfigureAwait(false);
            await msgRepo.SaveChangesAsync(ct).ConfigureAwait(false);
            list = (await convRepo.GetRecentConversationsAsync(100, ct).ConfigureAwait(false)).ToList();
        }
        catch { }
        return list;
    }

    private sealed class ConversationFile
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ConversationFileMessage>? Messages { get; set; }
    }

    private sealed class ConversationFileMessage
    {
        public Guid Id { get; set; }
        public string? Role { get; set; }
        public string? Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

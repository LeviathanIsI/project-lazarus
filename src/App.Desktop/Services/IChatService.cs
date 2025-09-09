using Lazarus.Data.Entities;
using Lazarus.Data.Enums;

namespace Lazarus.Desktop.Services;

public interface IChatService
{
    Task<List<Conversation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Conversation> CreateAsync(string? title = null, CancellationToken cancellationToken = default);
    Task RenameAsync(Guid chatId, string title, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task<List<Message>> GetMessagesAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task<Message> AddMessageAsync(Guid chatId, MessageRole role, string content, DateTime? timestamp = null, CancellationToken cancellationToken = default);
}


using Lazarus.App.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using Lazarus.App.Desktop.Collections;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Service for managing chat conversations and LLM interactions
/// </summary>
public class ChatService : IChatService
{
    private readonly ILogger<ChatService> _logger;
    private readonly RunnerProcessService _runnerService;
    private readonly HttpClient _httpClient;

    private Conversation? _activeConversation;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="runnerService">The runner process service</param>
    public ChatService(ILogger<ChatService> logger, RunnerProcessService runnerService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runnerService = runnerService ?? throw new ArgumentNullException(nameof(runnerService));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) }; // Long timeout for streaming

        Conversations = new ObservableCollection<Conversation>();
        
        _logger.LogInformation("ChatService initialized");
    }

    /// <summary>
    /// Gets the collection of available conversations (thread-safe operations enforced)
    /// </summary>
    public ObservableCollection<Conversation> Conversations { get; }

    /// <summary>
    /// Gets the currently active conversation
    /// </summary>
    public Conversation? ActiveConversation
    {
        get => _activeConversation;
        private set
        {
            if (_activeConversation != value)
            {
                _activeConversation = value;
                _logger.LogInformation("Active conversation changed to: {ConversationId}", value?.Id);
            }
        }
    }

    /// <summary>
    /// Event raised when a new message chunk is received during streaming
    /// </summary>
    public event EventHandler<MessageChunkReceivedEventArgs>? MessageChunkReceived;

    /// <summary>
    /// Event raised when a message is completed
    /// </summary>
    public event EventHandler<MessageCompletedEventArgs>? MessageCompleted;

    /// <summary>
    /// Event raised when an error occurs during chat operations
    /// </summary>
    public event EventHandler<ChatErrorEventArgs>? ChatError;

    /// <summary>
    /// Creates a new conversation
    /// </summary>
    /// <param name="title">The conversation title</param>
    /// <returns>The created conversation</returns>
    public Task<Conversation> CreateConversationAsync(string? title = null)
    {
        try
        {
            var id = Guid.NewGuid();
            var conversationTitle = !string.IsNullOrWhiteSpace(title) 
                ? title 
                : $"Conversation {DateTime.Now:HH:mm}";

            var conversation = new Conversation(id, conversationTitle);
            
            // Add system message for context
            var systemMessage = new ChatMessage(
                Guid.NewGuid(),
                "You are a helpful AI assistant. Please provide clear, accurate, and helpful responses.",
                MessageRole.System
            );
            conversation.Messages.Add(systemMessage);

            // Insert conversation on UI thread to ensure thread safety
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Conversations.Insert(0, conversation);
            });
            
            _logger.LogInformation("Created new conversation: {ConversationId} - {Title}", id, conversationTitle);
            
            return Task.FromResult(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation");
            OnChatError("Failed to create conversation", ex);
            throw;
        }
    }

    /// <summary>
    /// Selects an active conversation
    /// </summary>
    /// <param name="conversation">The conversation to activate</param>
    /// <returns>Task representing the operation</returns>
    public Task SetActiveConversationAsync(Conversation conversation)
    {
        try
        {
            if (conversation == null)
                throw new ArgumentNullException(nameof(conversation));

            ActiveConversation = conversation;
            conversation.HasUnreadMessages = false;
            conversation.LastActivity = DateTime.UtcNow;

            _logger.LogInformation("Set active conversation: {ConversationId}", conversation.Id);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active conversation");
            OnChatError("Failed to set active conversation", ex);
            throw;
        }
    }

    /// <summary>
    /// Sends a message in the active conversation with streaming response
    /// </summary>
    /// <param name="content">The message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    public async Task SendMessageAsync(string content, CancellationToken cancellationToken = default)
    {
        if (ActiveConversation == null)
        {
            OnChatError("No active conversation selected");
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            OnChatError("Message content cannot be empty");
            return;
        }

        try
        {
            // Check runner status
            var runnerStatus = GetRunnerStatus();
            if (!runnerStatus.IsConnected || !runnerStatus.IsHealthy)
            {
                OnChatError($"Runner is not ready: {runnerStatus.StatusMessage}");
                return;
            }

            // Add user message on UI thread to ensure thread safety
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var userMessage = new ChatMessage(Guid.NewGuid(), content, MessageRole.User);
                ActiveConversation.Messages.Add(userMessage);
                ActiveConversation.LastActivity = DateTime.UtcNow;
            });

            _logger.LogInformation("Sending message in conversation {ConversationId}: {ContentPreview}",
                ActiveConversation.Id, content.Substring(0, Math.Min(50, content.Length)));

            // Create assistant message for streaming response on UI thread
            ChatMessage assistantMessage = null!;
            Application.Current?.Dispatcher.Invoke(() =>
            {
                assistantMessage = new ChatMessage(Guid.NewGuid(), string.Empty, MessageRole.Assistant)
                {
                    IsStreaming = true
                };
                ActiveConversation.Messages.Add(assistantMessage);
            });

            try
            {
                await StreamResponseAsync(assistantMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                assistantMessage.Error = ex.Message;
                assistantMessage.IsStreaming = false;
                assistantMessage.Content = $"Error: {ex.Message}";
                OnChatError($"Error generating response: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            OnChatError("Failed to send message", ex);
        }
    }

    /// <summary>
    /// Streams the AI response from the runner service
    /// </summary>
    private async Task StreamResponseAsync(ChatMessage assistantMessage, CancellationToken cancellationToken)
    {
        try
        {
            var messages = ActiveConversation!.Messages
                .Where(m => m.Role != MessageRole.System || m == ActiveConversation.Messages.First())
                .Where(m => m != assistantMessage) // Exclude the message we're currently generating
                .Select(m => new
                {
                    role = m.Role.ToString().ToLowerInvariant(),
                    content = m.Content
                })
                .ToArray();

            var requestData = new
            {
                messages,
                stream = true,
                max_tokens = 2048,
                temperature = 0.7,
                top_p = 0.9,
                repeat_penalty = 1.1
            };

            var jsonContent = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var endpoint = $"{_runnerService.ApiEndpoint}/v1/chat/completions";

            _logger.LogInformation("Sending chat request to: {Endpoint}", endpoint);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = httpContent
            };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"API request failed: {response.StatusCode} - {errorContent}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var fullContent = new StringBuilder();
            string? line;

            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                    continue;

                var jsonData = line.Substring(6).Trim(); // Remove "data: " prefix

                if (jsonData == "[DONE]")
                {
                    break;
                }

                try
                {
                    using var jsonDoc = JsonDocument.Parse(jsonData);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0)
                    {
                        var choice = choices[0];
                        if (choice.TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("content", out var contentProp))
                        {
                            var chunk = contentProp.GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(chunk))
                            {
                                fullContent.Append(chunk);
                                assistantMessage.Content = fullContent.ToString();

                                // Notify about new chunk
                                OnMessageChunkReceived(assistantMessage.Id, chunk);
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse JSON chunk: {Json}", jsonData);
                }
            }

            // Complete the message
            assistantMessage.IsStreaming = false;
            var finalContent = fullContent.ToString();
            assistantMessage.Content = finalContent;

            if (string.IsNullOrWhiteSpace(finalContent))
            {
                assistantMessage.Content = "No response generated";
                assistantMessage.Error = "Empty response from AI";
            }

            ActiveConversation!.LastActivity = DateTime.UtcNow;
            OnMessageCompleted(assistantMessage.Id, assistantMessage.Content);

            _logger.LogInformation("Completed streaming response for message {MessageId} in conversation {ConversationId}",
                assistantMessage.Id, ActiveConversation.Id);
        }
        catch (Exception ex)
        {
            assistantMessage.IsStreaming = false;
            assistantMessage.Error = ex.Message;
            _logger.LogError(ex, "Error streaming response");
            throw;
        }
    }

    /// <summary>
    /// Deletes a conversation
    /// </summary>
    /// <param name="conversation">The conversation to delete</param>
    /// <returns>Task representing the operation</returns>
    public async Task DeleteConversationAsync(Conversation conversation)
    {
        try
        {
            if (conversation == null)
                throw new ArgumentNullException(nameof(conversation));

            if (ActiveConversation == conversation)
                ActiveConversation = null;

            // Remove conversation on UI thread to ensure thread safety
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Conversations.Remove(conversation);
            });
            
            await Task.CompletedTask;
            _logger.LogInformation("Deleted conversation: {ConversationId}", conversation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation");
            OnChatError("Failed to delete conversation", ex);
            throw;
        }
    }

    /// <summary>
    /// Gets the current runner connection status
    /// </summary>
    /// <returns>The runner status</returns>
    public Lazarus.App.Shared.Services.RunnerStatus GetRunnerStatus()
    {
        return new Lazarus.App.Shared.Services.RunnerStatus(
            _runnerService.IsRunning,
            _runnerService.IsHealthy,
            _runnerService.Status
        );
    }

    /// <summary>
    /// Searches conversations by title or content
    /// </summary>
    /// <param name="searchText">The search text</param>
    /// <returns>Filtered conversations</returns>
    public async Task<IEnumerable<Conversation>> SearchConversationsAsync(string searchText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Conversations;

            var lowerSearchText = searchText.ToLowerInvariant();
            
            var filteredConversations = Conversations
                .Where(c => c.Title.ToLowerInvariant().Contains(lowerSearchText) ||
                           c.Messages.Any(m => m.Content.ToLowerInvariant().Contains(lowerSearchText)))
                .ToList();

            await Task.CompletedTask;
            _logger.LogInformation("Search for '{SearchText}' returned {Count} conversations", searchText, filteredConversations.Count);
            
            return filteredConversations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching conversations");
            OnChatError("Failed to search conversations", ex);
            return Conversations;
        }
    }

    /// <summary>
    /// Raises the MessageChunkReceived event
    /// </summary>
    private void OnMessageChunkReceived(Guid messageId, string chunk)
    {
        MessageChunkReceived?.Invoke(this, new MessageChunkReceivedEventArgs(messageId, chunk));
    }

    /// <summary>
    /// Raises the MessageCompleted event
    /// </summary>
    private void OnMessageCompleted(Guid messageId, string fullContent)
    {
        MessageCompleted?.Invoke(this, new MessageCompletedEventArgs(messageId, fullContent));
    }

    /// <summary>
    /// Raises the ChatError event
    /// </summary>
    private void OnChatError(string error, Exception? exception = null)
    {
        ChatError?.Invoke(this, new ChatErrorEventArgs(error, exception));
    }
}
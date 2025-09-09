using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Lazarus.Desktop.Services;
using Lazarus.Data.Entities;
using Lazarus.Data.Enums;
using Lazarus.Shared.Settings;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.ViewModels;

public sealed class ChatSessionsViewModel : ViewModelBase
{
    // Message model
    public sealed class MessageVm : INotifyPropertyChanged
    {
        private string _role = "user";
        private string _content = string.Empty;
        private DateTime _timestamp = DateTime.Now;
        private bool _isStreaming;

        public string Role 
        { 
            get => _role; 
            set { if (_role != value) { _role = value; OnPropertyChanged(); } } 
        }
        
        public string Content 
        { 
            get => _content; 
            set { if (_content != value) { _content = value; OnPropertyChanged(); } } 
        }
        
        public DateTime Timestamp 
        { 
            get => _timestamp; 
            set { if (_timestamp != value) { _timestamp = value; OnPropertyChanged(); } } 
        }
        
        public bool IsStreaming 
        { 
            get => _isStreaming; 
            set { if (_isStreaming != value) { _isStreaming = value; OnPropertyChanged(); } } 
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class ChatItemViewModel : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private DateTime _updatedAt;
        private string? _preview;
        private bool _isEditing;

        public Guid Id { get; init; }
        public DateTime CreatedAt { get; init; }
        public string Title { get => _title; set { if (_title != value) { _title = value; OnPropertyChanged(); } } }
        public DateTime UpdatedAt { get => _updatedAt; set { if (_updatedAt != value) { _updatedAt = value; OnPropertyChanged(); } } }
        public string? Preview { get => _preview; set { if (_preview != value) { _preview = value; OnPropertyChanged(); } } }
        public bool IsEditing { get => _isEditing; set { if (_isEditing != value) { _isEditing = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private readonly RunnerStatusProvider _runnerStatus;
    private readonly ISettingsService _settingsService;
    private readonly IChatService _chatService;
    private readonly ILogger<ChatSessionsViewModel>? _logger;
    private readonly IAppState _appState;
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    // UI Properties
    private string _inputText = string.Empty;
    private bool _isStreaming;
    private string _errorMessage = string.Empty;
    private string _modelName = "No model loaded";
    private bool _isHealthy;
    private bool _hasConversations;

    // Conversations
    public ObservableCollection<ChatItemViewModel> Conversations { get; } = new();
    private ChatItemViewModel? _selectedConversation;

    // Inference parameters
    private double _temperature = 0.7;
    private double _topP = 0.9;
    private int _topK = 40;
    private int _maxTokens = 2048;
    private double _presencePenalty = 0.0;
    private double _frequencyPenalty = 0.0;

    public ChatSessionsViewModel(
        RunnerStatusProvider runnerStatus,
        ISettingsService settingsService,
        IChatService chatService,
        IAppState appState,
        ILogger<ChatSessionsViewModel> logger)
    {
        _runnerStatus = runnerStatus ?? throw new ArgumentNullException(nameof(runnerStatus));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _logger = logger;
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:11711/"),
            Timeout = TimeSpan.FromMinutes(5)
        };
        // Prefer SSE, but server may still return application/json for non-streaming
        _httpClient.DefaultRequestHeaders.Remove("Accept");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/event-stream");

        Messages = new ObservableCollection<MessageVm>();

        // React to settings changes to update display names
        _settingsService.SettingsChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(UserDisplayName));
            OnPropertyChanged(nameof(AssistantDisplayName));
        };

        // Commands
        SendMessageCommand = new RelayCommand(
            async () => await SendMessageAsync(),
            () => !IsStreaming && !string.IsNullOrWhiteSpace(InputText) && IsHealthy);

        NewChatCommand = new RelayCommand(async () => await NewChatAsync());
        DeleteChatCommand = new RelayCommand<ChatItemViewModel>(async vm => await DeleteChatAsync(vm), vm => vm != null || SelectedConversation != null);
        BeginRenameChatCommand = new RelayCommand<ChatItemViewModel>(vm => { if (vm != null) vm.IsEditing = true; });
        CommitRenameChatCommand = new RelayCommand<ChatItemViewModel>(async vm => { if (vm != null) await CommitRenameAsync(vm); });

        // Subscribe to runner state changes
        _runnerStatus.RunnerStateChanged += OnRunnerStateChanged;
        UpdateFromRunnerState(_runnerStatus.Current);

        Conversations.CollectionChanged += (_, __) =>
        {
            HasConversations = Conversations.Count > 0;
        };

        // Kick off loading conversations
        _ = InitializeAsync();

        // Mark field as used until settings are integrated in chat parameters
        _ = _settingsService;
    }

    public ObservableCollection<MessageVm> Messages { get; }
    
    // Allow navigation to trigger a fresh load after startup settles
    public async Task RefreshConversationsAsync()
    {
        await InitializeAsync().ConfigureAwait(true);
    }

    public ICommand SendMessageCommand { get; }
    public ICommand NewChatCommand { get; }
    public ICommand DeleteChatCommand { get; }
    public ICommand BeginRenameChatCommand { get; }
    public ICommand CommitRenameChatCommand { get; }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsStreaming
    {
        get => _isStreaming;
        private set
        {
            if (SetProperty(ref _isStreaming, value))
            {
                (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string ModelName
    {
        get => _modelName;
        private set => SetProperty(ref _modelName, value);
    }

    public bool IsHealthy
    {
        get => _isHealthy;
        private set
        {
            if (SetProperty(ref _isHealthy, value))
            {
                (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                if (DeleteChatCommand is RelayCommand<ChatItemViewModel> del)
                    del.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasConversations
    {
        get => _hasConversations;
        private set => SetProperty(ref _hasConversations, value);
    }

    public string UserDisplayName => string.IsNullOrWhiteSpace(_settingsService.Current.UserName) ? "You" : _settingsService.Current.UserName;
    public string AssistantDisplayName => string.IsNullOrWhiteSpace(_settingsService.Current.AssistantName) ? "Assistant" : _settingsService.Current.AssistantName;

    public ChatItemViewModel? SelectedConversation
    {
        get => _selectedConversation;
        set
        {
            if (SetProperty(ref _selectedConversation, value))
            {
                (DeleteChatCommand as RelayCommand)?.RaiseCanExecuteChanged();
                _ = LoadMessagesForSelectionAsync();
            }
        }
    }

    // Inference parameters
    public double Temperature
    {
        get => _temperature;
        set => SetProperty(ref _temperature, Math.Clamp(value, 0.0, 2.0));
    }

    public double TopP
    {
        get => _topP;
        set => SetProperty(ref _topP, Math.Clamp(value, 0.0, 1.0));
    }

    public int TopK
    {
        get => _topK;
        set => SetProperty(ref _topK, Math.Max(1, value));
    }

    public int MaxTokens
    {
        get => _maxTokens;
        set => SetProperty(ref _maxTokens, Math.Clamp(value, 1, 32768));
    }

    public double PresencePenalty
    {
        get => _presencePenalty;
        set => SetProperty(ref _presencePenalty, Math.Clamp(value, -2.0, 2.0));
    }

    public double FrequencyPenalty
    {
        get => _frequencyPenalty;
        set => SetProperty(ref _frequencyPenalty, Math.Clamp(value, -2.0, 2.0));
    }

    private void OnRunnerStateChanged(object? sender, RunnerStatusProvider.RunnerState state)
    {
        Application.Current?.Dispatcher?.Invoke(() => UpdateFromRunnerState(state));
    }

    private void UpdateFromRunnerState(RunnerStatusProvider.RunnerState state)
    {
        ModelName = state.ModelName ?? "No model loaded";
        IsHealthy = state.IsHealthy;
        
        if (!IsHealthy)
        {
            ErrorMessage = "Model not loaded. Please load a model first.";
        }
        else
        {
            ErrorMessage = string.Empty;
        }
    }

    private async Task SendMessageAsync()
    {
        try 
        {
            if (!IsHealthy || string.IsNullOrWhiteSpace(InputText) || IsStreaming)
                return;

            var userText = InputText.Trim();
            InputText = string.Empty;
            ErrorMessage = string.Empty;

            // Ensure we have a conversation
            if (SelectedConversation == null)
            {
                var created = await _chatService.CreateAsync().ConfigureAwait(true);
                var cvm = ToVm(created);
                Conversations.Insert(0, cvm);
                SelectedConversation = cvm;
            }

            // Add user message to UI and persist
            var userMessage = new MessageVm
            {
                Role = "user",
                Content = userText,
                Timestamp = DateTime.Now
            };
            Messages.Add(userMessage);
            try
            {
                await _chatService.AddMessageAsync(SelectedConversation!.Id, MessageRole.User, userText).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist user message");
            }

            // Update preview to reflect latest user text until assistant responds
            SelectedConversation!.Preview = MakePreview(userText);
            SelectedConversation!.UpdatedAt = DateTime.UtcNow;
            var curIdx = Conversations.IndexOf(SelectedConversation);
            if (curIdx > 0)
            {
                Conversations.Move(curIdx, 0);
            }

            // Auto-title new chats on first user message
            if (SelectedConversation!.Title == "New Chat")
            {
                var title = new string(userText.Take(40).ToArray());
                if (!string.IsNullOrWhiteSpace(title))
                {
                    SelectedConversation.Title = title;
                    _ = _chatService.RenameAsync(SelectedConversation.Id, title);
                }
            }

            // Start streaming assistant response
            await StreamAssistantAsync(userText);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in SendMessageAsync");
            ErrorMessage = $"Failed to send message: {ex.Message}";
        }
    }

    private async Task StreamAssistantAsync(string userText)
    {
        IsStreaming = true;
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        // Add assistant message placeholder
        var assistantMessage = new MessageVm
        {
            Role = "assistant",
            Content = "",
            Timestamp = DateTime.Now,
            IsStreaming = true
        };
        
        Application.Current?.Dispatcher?.Invoke(() => Messages.Add(assistantMessage));

        try
        {
            var requestBody = BuildRequest(userText);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(requestBody, jsonOptions);
            _logger?.LogDebug("Sending request: {Json}", json);

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            // Ensure Accept on the request (in addition to default)
            request.Headers.Remove("Accept");
            request.Headers.Add("Accept", "text/event-stream");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"API returned {response.StatusCode}: {error}");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            _logger?.LogInformation("Chat response Content-Type: {ContentType}", contentType);

            // Read as stream first and sniff the first line to decide SSE vs JSON.
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? firstLine = await reader.ReadLineAsync();
            if (firstLine is null)
            {
                _logger?.LogWarning("Empty response from chat endpoint");
                return;
            }

            string firstTrim = firstLine.TrimStart();
            bool looksLikeJson = firstTrim.StartsWith("{") || firstTrim.StartsWith("[");
            bool looksLikeSse = firstTrim.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || firstTrim.StartsWith(":");

            if (looksLikeJson && string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                // Collect full JSON text including the first line
                var sb = new StringBuilder();
                sb.AppendLine(firstLine);
                string rest = await reader.ReadToEndAsync();
                sb.Append(rest);
                var jsonText = sb.ToString();
                _logger?.LogDebug("First 200 json chars: {Snippet}", jsonText.Length > 200 ? jsonText.Substring(0, 200) : jsonText);

                try
                {
                    var resp = JsonSerializer.Deserialize<ChatCompletionResponse>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var contentText = resp?.Choices?.FirstOrDefault()?.Message?.Content;
                    if (!string.IsNullOrEmpty(contentText))
                    {
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            assistantMessage.Content += contentText;
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to parse JSON chat response");
                    throw;
                }
            }
            else
            {
                // Treat as SSE (also covers servers that mislabel Content-Type)
                bool firstLogged = false;
                var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // Helper to process accumulated event data (possibly multi-line)
                bool ProcessEvent(string eventData)
                {
                    var data = eventData.Trim();
                    if (!firstLogged)
                    {
                        _logger?.LogDebug("First SSE data: {Snippet}", data.Length > 200 ? data.Substring(0, 200) : data);
                        firstLogged = true;
                    }

                    if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                        return true; // signal break

                    try
                    {
                        var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data, deserializeOptions);
                        var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                        if (!string.IsNullOrEmpty(delta))
                        {
                            Application.Current?.Dispatcher?.Invoke(() => assistantMessage.Content += delta);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger?.LogWarning(ex, "Failed to parse streaming chunk: {Data}", data);
                    }
                    return false;
                }

                // Initialize accumulator with first line already read
                var eventBuilder = new StringBuilder();

                bool flushEvent()
                {
                    var evt = eventBuilder.ToString();
                    eventBuilder.Clear();
                    // Remove leading repeated 'data:' prefixes and join lines
                    var lines = evt.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(l => l.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ? l.Substring(5).TrimStart() : l.Trim());
                    var normalized = string.Join("\n", lines);
                    return ProcessEvent(normalized);
                }

                // Seed with first line
                eventBuilder.AppendLine(firstLine);

                while (!reader.EndOfStream && !ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    if (line.Length == 0)
                    {
                        // Blank line separates events
                        if (eventBuilder.Length > 0)
                        {
                            if (flushEvent()) break;
                        }
                        continue;
                    }

                    eventBuilder.AppendLine(line);
                }

                // Flush any trailing data
                if (eventBuilder.Length > 0)
                {
                    flushEvent();
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during streaming");
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (string.IsNullOrEmpty(assistantMessage.Content))
                {
                    assistantMessage.Content = "[Error: " + ex.Message + "]";
                }
                else
                {
                    assistantMessage.Content += " [error]";
                }
                ErrorMessage = "Failed to get response: " + ex.Message;
            });
        }
        finally
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                assistantMessage.IsStreaming = false;
            });
            IsStreaming = false;

            // Persist assistant message if any
            if (SelectedConversation != null && !string.IsNullOrWhiteSpace(assistantMessage.Content))
            {
                try
                {
                    await _chatService.AddMessageAsync(SelectedConversation.Id, MessageRole.Assistant, assistantMessage.Content).ConfigureAwait(false);

                    // Update preview and timestamps for sidebar
                    SelectedConversation.Preview = MakePreview(assistantMessage.Content);
                    SelectedConversation.UpdatedAt = DateTime.UtcNow;

                    // Reorder: move selected conversation to top
                    var currentIndex = Conversations.IndexOf(SelectedConversation);
                    if (currentIndex > 0)
                    {
                        Conversations.Move(currentIndex, 0);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to persist assistant message");
                }
            }
        }
    }

    private object BuildRequest(string userText)
    {
        var messages = BuildRequestMessages(userText);
        
        return new
        {
            model = ModelName,
            stream = true,
            messages = messages,
            temperature = Temperature,
            top_p = TopP,
            top_k = TopK,
            max_tokens = MaxTokens,
            presence_penalty = PresencePenalty,
            frequency_penalty = FrequencyPenalty,
            adapters = BuildAdaptersObject()
        };
    }

    private object[] BuildRequestMessages(string userText)
    {
        var messages = new System.Collections.Generic.List<object>();

        // Prepend synthesized system message from settings
        var sys = BuildSystemPrompt();
        messages.Add(new { role = "system", content = sys });

        // Add conversation history (excluding the current streaming message)
        foreach (var msg in Messages.Where(m => !m.IsStreaming))
        {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        // Add current user message if not already added
        if (Messages.LastOrDefault()?.Content != userText)
        {
            messages.Add(new { role = "user", content = userText });
        }

        // Attachments (adapters) hint for runners that support them
        var adapters = new System.Collections.Generic.Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(_appState.LoadedLora)) adapters["lora"] = _appState.LoadedLora!;
        if (_appState.LoraScale.HasValue) adapters["lora_scale"] = _appState.LoraScale.Value;
        if (!string.IsNullOrWhiteSpace(_appState.LoadedTokenizer)) adapters["tokenizer"] = _appState.LoadedTokenizer!;
        if (!string.IsNullOrWhiteSpace(_appState.LoadedEmbedding)) adapters["embedding"] = _appState.LoadedEmbedding!;

        // Build final payload messages list
        return messages.ToArray();
    }

    private string BuildSystemPrompt()
    {
        var s = _settingsService.Current;
        var user = string.IsNullOrWhiteSpace(s.UserName) ? "You" : s.UserName.Trim();
        var asst = string.IsNullOrWhiteSpace(s.AssistantName) ? "Assistant" : s.AssistantName.Trim();
        var extra = s.SystemPrompt ?? string.Empty;
        if (string.IsNullOrWhiteSpace(extra))
        {
            return $"Your name is \"{asst}\". The user's name is \"{user}\". Answer helpfully and concisely.";
        }
        return $"Your name is \"{asst}\". The user's name is \"{user}\".\n\n{extra}";
    }

    private object? BuildAdaptersObject()
    {
        var hasAny = !string.IsNullOrWhiteSpace(_appState.LoadedLora) || !string.IsNullOrWhiteSpace(_appState.LoadedTokenizer) || !string.IsNullOrWhiteSpace(_appState.LoadedEmbedding) || _appState.LoraScale.HasValue;
        if (!hasAny) return null;
        return new
        {
            lora = string.IsNullOrWhiteSpace(_appState.LoadedLora) ? null : _appState.LoadedLora,
            lora_scale = _appState.LoraScale,
            tokenizer = string.IsNullOrWhiteSpace(_appState.LoadedTokenizer) ? null : _appState.LoadedTokenizer,
            embedding = string.IsNullOrWhiteSpace(_appState.LoadedEmbedding) ? null : _appState.LoadedEmbedding
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            _cts?.Cancel();
            _runnerStatus.RunnerStateChanged -= OnRunnerStateChanged;
            _httpClient?.Dispose();
        }
        
        _disposed = true;
        base.Dispose(disposing);
    }

    private async Task InitializeAsync()
    {
        try
        {
            var list = await _chatService.GetAllAsync().ConfigureAwait(true);
            Conversations.Clear();
            foreach (var c in list.OrderByDescending(c => c.LastMessageAt))
            {
                Conversations.Add(ToVm(c));
            }
            HasConversations = Conversations.Count > 0;
            if (SelectedConversation == null && Conversations.Count > 0)
            {
                SelectedConversation = Conversations[0];
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load conversations");
        }
    }

    private async Task LoadMessagesForSelectionAsync()
    {
        try
        {
            Messages.Clear();
            if (SelectedConversation == null) return;
            var msgs = await _chatService.GetMessagesAsync(SelectedConversation.Id).ConfigureAwait(true);
            foreach (var m in msgs)
            {
                Messages.Add(new MessageVm
                {
                    Role = m.Role == MessageRole.User ? "user" : m.Role == MessageRole.Assistant ? "assistant" : "system",
                    Content = m.Content,
                    Timestamp = m.Timestamp.ToLocalTime()
                });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load messages for selection");
        }
    }

    private async Task NewChatAsync()
    {
        try
        {
            var convo = await _chatService.CreateAsync().ConfigureAwait(true);
            var vm = ToVm(convo);
            Conversations.Insert(0, vm);
            SelectedConversation = vm;
            Messages.Clear();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create new chat");
            ErrorMessage = "Failed to create conversation.";
        }
    }

    private async Task DeleteChatAsync(ChatItemViewModel? vm)
    {
        var target = vm ?? SelectedConversation;
        if (target == null) return;
        try
        {
            var id = target.Id;
            await _chatService.DeleteAsync(id).ConfigureAwait(true);
            var idx = Conversations.IndexOf(target);
            Conversations.Remove(target);
            if (ReferenceEquals(target, SelectedConversation))
            {
                SelectedConversation = Conversations.Count > 0 ? Conversations[Math.Clamp(idx, 0, Conversations.Count - 1)] : null;
                Messages.Clear();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to delete chat");
            ErrorMessage = "Failed to delete conversation.";
        }
    }

    private async Task CommitRenameAsync(ChatItemViewModel vm)
    {
        try
        {
            var title = string.IsNullOrWhiteSpace(vm.Title) ? "New Chat" : vm.Title.Trim();
            vm.Title = title;
            await _chatService.RenameAsync(vm.Id, title).ConfigureAwait(true);
            if (SelectedConversation != null && vm.Id == SelectedConversation.Id)
                OnPropertyChanged(nameof(SelectedConversation));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to rename chat");
            ErrorMessage = "Failed to rename conversation.";
        }
        finally { vm.IsEditing = false; }
    }

    private static ChatItemViewModel ToVm(Conversation c)
    {
        return new ChatItemViewModel
        {
            Id = c.Id,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.LastMessageAt,
            Preview = null
        };
    }

    private static string MakePreview(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;
        var s = content.Replace("\r", " ").Replace("\n", " ");
        return s.Length <= 80 ? s : s.Substring(0, 80);
    }

    // JSON response models
    private class ChatCompletionChunk
    {
        [JsonPropertyName("choices")] public Choice[]? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("delta")] public Delta? Delta { get; set; }
        [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
    }

    private class Delta
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }

    private class ChatCompletionResponse
    {
        [JsonPropertyName("choices")] public ChatChoice[]? Choices { get; set; }
    }

    private class ChatChoice
    {
        [JsonPropertyName("message")] public ChatMessage? Message { get; set; }
    }

    private class ChatMessage
    {
        [JsonPropertyName("role")] public string? Role { get; set; }
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}

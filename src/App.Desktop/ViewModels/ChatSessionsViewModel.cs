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

    private readonly RunnerStatusProvider _runnerStatus;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ChatSessionsViewModel>? _logger;
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    // UI Properties
    private string _inputText = string.Empty;
    private bool _isStreaming;
    private string _errorMessage = string.Empty;
    private string _modelName = "No model loaded";
    private bool _isHealthy;

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
        ILogger<ChatSessionsViewModel> logger)
    {
        _runnerStatus = runnerStatus ?? throw new ArgumentNullException(nameof(runnerStatus));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
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
        
        // Commands
        SendMessageCommand = new RelayCommand(
            async () => await SendMessageAsync(),
            () => !IsStreaming && !string.IsNullOrWhiteSpace(InputText) && IsHealthy);

        // Subscribe to runner state changes
        _runnerStatus.RunnerStateChanged += OnRunnerStateChanged;
        UpdateFromRunnerState(_runnerStatus.Current);

        // Mark field as used until settings are integrated in chat parameters
        _ = _settingsService;
    }

    public ObservableCollection<MessageVm> Messages { get; }

    public ICommand SendMessageCommand { get; }

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

            // Add user message to UI
            var userMessage = new MessageVm
            {
                Role = "user",
                Content = userText,
                Timestamp = DateTime.Now
            };
            Messages.Add(userMessage);

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

            if (string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                // Fallback non-streaming JSON response
                var jsonText = await response.Content.ReadAsStringAsync(ct);
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
                // Stream SSE
                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(stream);
                bool firstLogged = false;
                var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                while (!reader.EndOfStream && !ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;
                    if (line.Length == 0) continue; // ignore keep-alives
                    if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;

                    var data = line.Substring(5).Trim();
                    if (!firstLogged)
                    {
                        _logger?.LogDebug("First SSE data line: {Snippet}", data.Length > 200 ? data.Substring(0, 200) : data);
                        firstLogged = true;
                    }

                    if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                        break;

                    try
                    {
                        var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data, deserializeOptions);
                        var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;

                        if (!string.IsNullOrEmpty(delta))
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                assistantMessage.Content += delta;
                            });
                        }

                        // Optional finish condition
                        if (!string.IsNullOrEmpty(chunk?.Choices?.FirstOrDefault()?.FinishReason))
                        {
                            break;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger?.LogWarning(ex, "Failed to parse streaming chunk: {Data}", data);
                    }
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
            frequency_penalty = FrequencyPenalty
        };
    }

    private object[] BuildRequestMessages(string userText)
    {
        var messages = new System.Collections.Generic.List<object>();
        
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

        return messages.ToArray();
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

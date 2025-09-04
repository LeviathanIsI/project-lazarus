using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazarus.App.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the Conversations section with full chat functionality
/// </summary>
public partial class ConversationsViewModel : BaseViewModel
{
    private readonly ILogger<ConversationsViewModel> _logger;
    private readonly IChatService _chatService;
    private readonly Dispatcher _dispatcher;
    
    private string _searchText = string.Empty;
    private string _messageText = string.Empty;
    private Conversation? _selectedConversation;
    private RunnerStatus? _runnerStatus;
    
    // Navigation state preservation tracking
    private bool _isInitialized = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationsViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="chatService">The chat service</param>
    public ConversationsViewModel(ILogger<ConversationsViewModel> logger, IChatService chatService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _dispatcher = Dispatcher.CurrentDispatcher;
        
        Title = "Conversations";
        StatusMessage = "Ready to chat with AI";
        
        // Initialize commands
        NewConversationCommand = new AsyncRelayCommand(ExecuteNewConversationAsync);
        SearchConversationsCommand = new AsyncRelayCommand(SearchConversationsAsync);
        SendMessageCommand = new AsyncRelayCommand(ExecuteSendMessageAsync, CanSendMessage);
        DeleteConversationCommand = new AsyncRelayCommand<Conversation>(ExecuteDeleteConversationAsync);
        
        // NAVIGATION-SAFE: Only initialize once for singleton instance
        if (!_isInitialized)
        {
            // Subscribe to chat service events
            _chatService.MessageChunkReceived += OnMessageChunkReceived;
            _chatService.MessageCompleted += OnMessageCompleted;
            _chatService.ChatError += OnChatError;
            
            // Initialize with chat service data
            LoadInitialDataAsync();
            
            _isInitialized = true;
            _logger.LogInformation("Conversations view model initialized with chat service (Navigation-Safe Mode)");
        }
        else
        {
            _logger.LogDebug("Conversations view model reused - preserving existing state and connections");
        }
    }

    /// <summary>
    /// Gets the title of the view
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets or sets the search text
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    /// <summary>
    /// Gets or sets the message text being composed
    /// </summary>
    public string MessageText
    {
        get => _messageText;
        set
        {
            if (SetProperty(ref _messageText, value))
            {
                SendMessageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected conversation
    /// </summary>
    public Conversation? SelectedConversation
    {
        get => _selectedConversation;
        set => SetSelectedConversationAsync(value);
    }

    /// <summary>
    /// Gets the collection of conversations from the chat service
    /// </summary>
    public ObservableCollection<Conversation> Conversations => _chatService.Conversations;

    /// <summary>
    /// Gets the current runner status
    /// </summary>
    public RunnerStatus? RunnerStatus
    {
        get => _runnerStatus;
        private set => SetProperty(ref _runnerStatus, value);
    }

    /// <summary>
    /// Gets the new conversation command
    /// </summary>
    public IAsyncRelayCommand NewConversationCommand { get; }

    /// <summary>
    /// Gets the search conversations command
    /// </summary>
    public IAsyncRelayCommand SearchConversationsCommand { get; }

    /// <summary>
    /// Gets the send message command
    /// </summary>
    public IAsyncRelayCommand SendMessageCommand { get; }

    /// <summary>
    /// Gets the delete conversation command
    /// </summary>
    public IAsyncRelayCommand<Conversation> DeleteConversationCommand { get; }

    /// <summary>
    /// Executes the new conversation command
    /// </summary>
    private async Task ExecuteNewConversationAsync()
    {
        try
        {
            SetBusyState(true, "Creating new conversation...");
            _logger.LogInformation("Creating new conversation");
            
            var conversation = await _chatService.CreateConversationAsync();
            await _chatService.SetActiveConversationAsync(conversation);
            
            SelectedConversation = conversation;
            UpdateRunnerStatus();
            
            StatusMessage = "New conversation created";
            SetBusyState(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new conversation");
            StatusMessage = "Failed to create conversation";
            SetBusyState(false);
        }
    }

    /// <summary>
    /// Searches conversations based on the search text
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task SearchConversationsAsync()
    {
        try
        {
            SetBusyState(true, "Searching conversations...");
            _logger.LogInformation("Searching conversations with text: {SearchText}", SearchText);

            var results = await _chatService.SearchConversationsAsync(SearchText);
            
            SetBusyState(false, string.IsNullOrWhiteSpace(SearchText) 
                ? "All conversations displayed" 
                : $"Found {results.Count()} matching conversations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching conversations");
            SetBusyState(false, "Search failed");
        }
    }

    /// <summary>
    /// Loads initial data and creates a default conversation if none exist
    /// </summary>
    private async void LoadInitialDataAsync()
    {
        try
        {
            UpdateRunnerStatus();
            
            // Create a default conversation if none exist
            if (!Conversations.Any())
            {
                _logger.LogInformation("No existing conversations, creating initial conversation");
                var conversation = await _chatService.CreateConversationAsync("Welcome to Lazarus AI");
                await _chatService.SetActiveConversationAsync(conversation);
                SelectedConversation = conversation;
            }
            else if (SelectedConversation == null && Conversations.Any())
            {
                // Select the first conversation if none is selected
                var firstConversation = Conversations.First();
                await _chatService.SetActiveConversationAsync(firstConversation);
                SelectedConversation = firstConversation;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading initial data");
            StatusMessage = "Error loading conversations";
        }
    }

    /// <summary>
    /// Executes the send message command
    /// </summary>
    private async Task ExecuteSendMessageAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(MessageText) || SelectedConversation == null)
                return;

            var messageToSend = MessageText.Trim();
            MessageText = string.Empty; // Clear the input immediately
            
            SetBusyState(true, "Sending message...");
            _logger.LogInformation("Sending message: {MessagePreview}", messageToSend.Substring(0, Math.Min(50, messageToSend.Length)));
            
            await _chatService.SendMessageAsync(messageToSend);
            
            StatusMessage = "Message sent";
            SetBusyState(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            StatusMessage = "Failed to send message";
            SetBusyState(false);
        }
    }

    /// <summary>
    /// Determines whether a message can be sent
    /// </summary>
    private bool CanSendMessage()
    {
        return !string.IsNullOrWhiteSpace(MessageText) && 
               SelectedConversation != null && 
               !IsBusy &&
               (RunnerStatus?.IsConnected == true);
    }

    /// <summary>
    /// Executes the delete conversation command
    /// </summary>
    private async Task ExecuteDeleteConversationAsync(Conversation? conversation)
    {
        if (conversation == null)
            return;

        try
        {
            SetBusyState(true, "Deleting conversation...");
            _logger.LogInformation("Deleting conversation: {ConversationId}", conversation.Id);
            
            await _chatService.DeleteConversationAsync(conversation);
            
            // Select another conversation if we deleted the current one
            if (SelectedConversation == conversation)
            {
                SelectedConversation = Conversations.FirstOrDefault();
            }
            
            StatusMessage = "Conversation deleted";
            SetBusyState(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation");
            StatusMessage = "Failed to delete conversation";
            SetBusyState(false);
        }
    }

    /// <summary>
    /// Sets the selected conversation asynchronously
    /// </summary>
    private async void SetSelectedConversationAsync(Conversation? conversation)
    {
        if (SetProperty(ref _selectedConversation, conversation) && conversation != null)
        {
            try
            {
                await _chatService.SetActiveConversationAsync(conversation);
                SendMessageCommand.NotifyCanExecuteChanged();
                _logger.LogInformation("Selected conversation: {ConversationId}", conversation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting active conversation");
                StatusMessage = "Error selecting conversation";
            }
        }
    }

    /// <summary>
    /// Updates the runner status
    /// </summary>
    private void UpdateRunnerStatus()
    {
        RunnerStatus = _chatService.GetRunnerStatus();
        SendMessageCommand.NotifyCanExecuteChanged();
        
        if (RunnerStatus != null)
        {
            var statusText = RunnerStatus.IsConnected && RunnerStatus.IsHealthy 
                ? "AI Ready" 
                : $"AI Status: {RunnerStatus.StatusMessage}";
            
            if (string.IsNullOrWhiteSpace(StatusMessage) || StatusMessage.StartsWith("AI"))
            {
                StatusMessage = statusText;
            }
        }
    }

    /// <summary>
    /// Handles message chunk received events
    /// </summary>
    private void OnMessageChunkReceived(object? sender, MessageChunkReceivedEventArgs e)
    {
        // UI updates happen automatically through data binding since the message content is updated directly
        ExecuteOnUIThread(() =>
        {
            // Force UI refresh if needed
            if (SelectedConversation?.Messages.Any(m => m.Id == e.MessageId) == true)
            {
                // The message content is already updated in the service, just notify UI
                OnPropertyChanged(nameof(SelectedConversation));
            }
        });
    }

    /// <summary>
    /// Handles message completed events
    /// </summary>
    private void OnMessageCompleted(object? sender, MessageCompletedEventArgs e)
    {
        ExecuteOnUIThread(() =>
        {
            SetBusyState(false, "Response completed");
            UpdateRunnerStatus();
        });
    }

    /// <summary>
    /// Handles chat error events
    /// </summary>
    private void OnChatError(object? sender, ChatErrorEventArgs e)
    {
        ExecuteOnUIThread(() =>
        {
            _logger.LogWarning("Chat error: {Error}", e.Error);
            StatusMessage = $"Error: {e.Error}";
            SetBusyState(false);
            UpdateRunnerStatus();
        });
    }

    /// <summary>
    /// Disposes of resources used by the ConversationsViewModel
    /// SINGLETON-SAFE: Only dispose if actually initialized to prevent issues with singleton lifecycle
    /// </summary>
    protected override void DisposeResources()
    {
        if (_isInitialized)
        {
            // Unsubscribe from chat service events to prevent memory leaks
            _chatService.MessageChunkReceived -= OnMessageChunkReceived;
            _chatService.MessageCompleted -= OnMessageCompleted;
            _chatService.ChatError -= OnChatError;
            
            _logger.LogDebug("ConversationsViewModel event subscriptions cleaned up (Singleton-Safe Mode)");
        }
        
        base.DisposeResources();
    }
}
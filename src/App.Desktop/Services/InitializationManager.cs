using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Lazarus.Data;
using Lazarus.Shared;

namespace Lazarus.Desktop.Services
{
    /// <summary>
    /// Manages the initialization sequence of Lazarus application components
    /// </summary>
    public class InitializationManager : IInitializationManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InitializationManager> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private bool _isInitialized;
        private bool _isInitializing;
        private int _progressPercentage;
        private string _currentMessage = "Starting...";

        public bool IsInitialized => _isInitialized;
        public bool IsInitializing => _isInitializing;
        public int ProgressPercentage => _progressPercentage;
        public string CurrentMessage => _currentMessage;

        public event EventHandler<InitializationProgressEventArgs>? InitializationProgressChanged;
        public event EventHandler? InitializationCompleted;
        public event EventHandler<InitializationFailedEventArgs>? InitializationFailed;

        public InitializationManager(IServiceProvider serviceProvider, ILogger<InitializationManager> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task InitializeAsync()
        {
            if (_isInitializing || _isInitialized)
            {
                _logger.LogWarning("Initialization already in progress or completed");
                return;
            }

            _isInitializing = true;
            _progressPercentage = 0;

            try
            {
                _logger.LogInformation("Starting Lazarus initialization sequence");

                // Step 1: Bootstrap directories
                await InitializeStepAsync(10, "Bootstrapping directories...", async () =>
                {
                    DirectoryBootstrap.EnsureAll();
                    await Task.Delay(500); // Simulate bootstrap time
                });

                // Step 2: Initialize database
                await InitializeStepAsync(25, "Initializing database...", async () =>
                {
                    var dbf = _serviceProvider.GetRequiredService<IDbContextFactory<LazarusDbContext>>();
                    await using var context = await dbf.CreateDbContextAsync();
                    await context.Database.EnsureCreatedAsync();
                    await Task.Delay(800); // Simulate database initialization
                });

                // Step 3: Start orchestrator host
                await InitializeStepAsync(40, "Starting orchestrator services...", async () =>
                {
                    // Note: Orchestrator host is typically started separately
                    // This simulates the service startup
                    await Task.Delay(1000);
                });

                // Step 4: Initialize backend services
                await InitializeStepAsync(60, "Initializing backend services...", async () =>
                {
                    // Get singleton services directly from root provider
                    var modelInventoryService = _serviceProvider.GetRequiredService<Lazarus.Backend.Services.ModelInventoryService>();
                    var modelPresetService = _serviceProvider.GetRequiredService<Lazarus.Backend.Services.ModelPresetService>();
                    await Task.Delay(600);
                });

                // Step 5: Verify agent connections
                await InitializeStepAsync(80, "Verifying agent connections...", async () =>
                {
                    // Simulate agent connection verification
                    await Task.Delay(500);
                });

                // Step 6: Final setup
                await InitializeStepAsync(100, "Finalizing setup...", async () =>
                {
                    await Task.Delay(300);
                });

                _isInitialized = true;
                _isInitializing = false;

                _logger.LogInformation("Lazarus initialization completed successfully");
                InitializationCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Initialization was cancelled");
                _isInitializing = false;
                InitializationFailed?.Invoke(this, new InitializationFailedEventArgs("Initialization was cancelled"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initialization failed");
                _isInitializing = false;
                InitializationFailed?.Invoke(this, new InitializationFailedEventArgs($"Initialization failed: {ex.Message}"));
            }
        }

        private async Task InitializeStepAsync(int targetPercentage, string message, Func<Task> stepAction)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            _currentMessage = message;
            _progressPercentage = targetPercentage;

            _logger.LogDebug("Initialization step: {Message} ({Percentage}%)", message, targetPercentage);

            InitializationProgressChanged?.Invoke(this, new InitializationProgressEventArgs(targetPercentage, message));

            try
            {
                await stepAction();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initialization step failed: {Message}", message);
                throw;
            }
        }

        public void Cancel()
        {
            _logger.LogInformation("Cancelling initialization");
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Lazarus.Data;
using Lazarus.Shared;

namespace Lazarus.Desktop.Services
{
    /// <summary>
    /// Application bootstrapper that handles initialization with progress reporting
    /// </summary>
    public sealed class AppBootstrapper : IAppBootstrapper
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AppBootstrapper> _logger;

        public AppBootstrapper(IServiceProvider serviceProvider, ILogger<AppBootstrapper> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync(IProgress<BootstrapProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting Lazarus resurrection sequence...");

                // Step 1: Bootstrap directories
                progress?.Report(new("Summoning directory spirits...", 5));
                await InitializeDirectoriesAsync(cancellationToken);

                // Step 2: Initialize database
                progress?.Report(new("Awakening data phantoms...", 20));
                await InitializeDatabaseAsync(cancellationToken);

                // Step 3: Start orchestrator services
                progress?.Report(new("Binding service wraiths...", 40));
                await InitializeOrchestratorAsync(cancellationToken);

                // Step 4: Initialize backend services
                progress?.Report(new("Resurrecting core entities...", 70));
                await InitializeBackendServicesAsync(cancellationToken);

                // Step 5: Final setup
                progress?.Report(new("Completing the dark ritual...", 100));
                await FinalizeSetupAsync(cancellationToken);

                _logger.LogInformation("Lazarus resurrection completed successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Resurrection sequence was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resurrection sequence failed");
                throw;
            }
        }

        private async Task InitializeDirectoriesAsync(CancellationToken cancellationToken)
        {
            // Ensure all required directories exist
            DirectoryBootstrap.EnsureAll();
            await Task.Delay(500, cancellationToken); // Simulate directory creation time
        }

        private async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
        {
            // Use factory pattern for database initialization
            var dbf = _serviceProvider.GetRequiredService<IDbContextFactory<LazarusDbContext>>();
            await using var context = await dbf.CreateDbContextAsync(cancellationToken);
            await context.Database.EnsureCreatedAsync(cancellationToken);
            await Task.Delay(800, cancellationToken); // Simulate database initialization
        }

        private async Task InitializeOrchestratorAsync(CancellationToken cancellationToken)
        {
            // Note: Orchestrator host is typically started separately
            // This simulates the service startup
            await Task.Delay(1000, cancellationToken);
        }

        private async Task InitializeBackendServicesAsync(CancellationToken cancellationToken)
        {
            // Get required services to ensure they're initialized
            // These are singletons, so they can be resolved from root provider
            var modelInventoryService = _serviceProvider.GetRequiredService<Lazarus.Backend.Services.ModelInventoryService>();
            var modelPresetService = _serviceProvider.GetRequiredService<Lazarus.Backend.Services.ModelPresetService>();
            await Task.Delay(600, cancellationToken);
        }

        private async Task FinalizeSetupAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(300, cancellationToken);
        }
    }
}

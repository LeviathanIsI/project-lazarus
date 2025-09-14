using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Extensions.Logging;
using Lazarus.Desktop.Services;

namespace Lazarus.Desktop.Views
{
    /// <summary>
    /// Dark ceremony loading window for Lazarus resurrection
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private readonly ILogger<LoadingWindow>? _logger;
        private readonly IInitializationManager _initializationManager;
        private readonly Storyboard _flickerAnimation;
        private readonly Storyboard _orbSpinAnimation;
        private readonly Storyboard _glitchAnimation;
        private bool _isInitialized = false;

        public event EventHandler? InitializationCompleted;
        public event EventHandler? InitializationFailed;

        public LoadingWindow(IInitializationManager initializationManager, ILogger<LoadingWindow>? logger = null)
        {
            InitializeComponent();

            _logger = logger;
            _initializationManager = initializationManager ?? throw new ArgumentNullException(nameof(initializationManager));

            // Get animations from resources
            _flickerAnimation = (Storyboard)FindResource("FlickerAnimation");
            _orbSpinAnimation = (Storyboard)FindResource("OrbSpinAnimation");
            _glitchAnimation = (Storyboard)FindResource("GlitchAnimation");

            // Start the dark ceremony
            StartDarkCeremony();

            // Subscribe to initialization events
            _initializationManager.InitializationProgressChanged += OnInitializationProgressChanged;
            _initializationManager.InitializationCompleted += OnInitializationCompleted;
            _initializationManager.InitializationFailed += OnInitializationFailed;

            // Begin the resurrection sequence
            _ = StartInitializationAsync();
        }

        private void StartDarkCeremony()
        {
            try
            {
                _flickerAnimation?.Begin();
                _orbSpinAnimation?.Begin();
                _logger?.LogInformation("Dark ceremony initiated - Lazarus resurrection in progress");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to start dark ceremony animations");
            }
        }

        private void StopDarkCeremony()
        {
            try
            {
                _flickerAnimation?.Stop();
                _orbSpinAnimation?.Stop();
                _glitchAnimation?.Stop();
                _logger?.LogInformation("Dark ceremony concluded");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to stop dark ceremony animations");
            }
        }

        private async Task StartInitializationAsync()
        {
            try
            {
                _logger?.LogInformation("Initiating Lazarus resurrection sequence...");

                // Set resurrection timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                var initTask = _initializationManager.InitializeAsync();

                var completedTask = await Task.WhenAny(initTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger?.LogWarning("Resurrection sequence timed out after 30 seconds");
                    ShowCorruption("Resurrection timeout. Neural pathways may be compromised.");
                }
                else
                {
                    await initTask; // Ensure we get any exceptions
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Resurrection sequence failed with exception");
                ShowCorruption($"Resurrection failed: {ex.Message}");
            }
        }

        private void OnInitializationProgressChanged(object? sender, InitializationProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Convert corporate messages to dark ceremony language
                var darkMessage = ConvertToDarkCeremony(e.Message);
                ProgressText.Text = darkMessage;
                _logger?.LogDebug("Resurrection progress: {Progress}", darkMessage);
            });
        }

        private string ConvertToDarkCeremony(string message)
        {
            return message.ToLower() switch
            {
                var msg when msg.Contains("bootstrapping") => "Summoning directory spirits...",
                var msg when msg.Contains("database") => "Awakening data phantoms...",
                var msg when msg.Contains("orchestrator") => "Binding service wraiths...",
                var msg when msg.Contains("backend") => "Resurrecting core entities...",
                var msg when msg.Contains("agent") => "Establishing neural communion...",
                var msg when msg.Contains("finalizing") => "Completing the dark ritual...",
                _ => "Channeling arcane energies..."
            };
        }

        private void OnInitializationCompleted(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_isInitialized) return;
                _isInitialized = true;

                _logger?.LogInformation("Lazarus resurrection completed successfully");
                StatusText.Text = "LAZARUS RISEN";
                ProgressText.Text = "The dark ceremony is complete...";

                // Stop the dark ceremony
                StopDarkCeremony();

                // Elegant fade out with purple glow
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(800)
                };

                fadeOut.Completed += (s, args) =>
                {
                    InitializationCompleted?.Invoke(this, EventArgs.Empty);
                    Close();
                };

                BeginAnimation(OpacityProperty, fadeOut);
            });
        }

        private void OnInitializationFailed(object? sender, InitializationFailedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _logger?.LogError("Lazarus resurrection failed: {Error}", e.Error);
                ShowCorruption(e.Error);
            });
        }

        private void ShowCorruption(string errorMessage)
        {
            StopDarkCeremony();

            // Switch to error state with glitch effects
            LoadingContainer.Visibility = Visibility.Collapsed;
            ErrorContainer.Visibility = Visibility.Visible;

            // Start glitch animation
            _glitchAnimation?.Begin();

            _logger?.LogError("Dark ceremony corrupted: {Error}", errorMessage);
            InitializationFailed?.Invoke(this, EventArgs.Empty);
        }

        private async void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger?.LogInformation("User requested resurrection retry");

                // Reset to loading state
                ErrorContainer.Visibility = Visibility.Collapsed;
                LoadingContainer.Visibility = Visibility.Visible;

                StatusText.Text = "RESURRECTING LAZARUS";
                ProgressText.Text = "Reinitiating dark ceremony...";

                // Restart the dark ceremony
                StartDarkCeremony();

                // Retry the resurrection sequence
                await StartInitializationAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Resurrection retry failed");
                ShowCorruption($"Resurrection retry failed: {ex.Message}");
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("User terminated Lazarus resurrection ceremony");
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            _initializationManager.InitializationProgressChanged -= OnInitializationProgressChanged;
            _initializationManager.InitializationCompleted -= OnInitializationCompleted;
            _initializationManager.InitializationFailed -= OnInitializationFailed;

            // Stop the dark ceremony
            StopDarkCeremony();

            base.OnClosed(e);
        }
    }
}
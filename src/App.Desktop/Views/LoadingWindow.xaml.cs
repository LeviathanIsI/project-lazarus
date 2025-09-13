using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Extensions.Logging;
using Lazarus.Desktop.Services;

namespace Lazarus.Desktop.Views
{
    /// <summary>
    /// Loading window that displays during application initialization
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private readonly ILogger<LoadingWindow>? _logger;
        private readonly IInitializationManager _initializationManager;
        private readonly Storyboard _pulseAnimation;
        private readonly Storyboard _rotateAnimation;
        private readonly Storyboard _innerRotateAnimation;
        private readonly Storyboard _typewriterAnimation;
        private bool _isInitialized = false;

        public event EventHandler? InitializationCompleted;
        public event EventHandler? InitializationFailed;

        public LoadingWindow(IInitializationManager initializationManager, ILogger<LoadingWindow>? logger = null)
        {
            InitializeComponent();
            
            _logger = logger;
            _initializationManager = initializationManager ?? throw new ArgumentNullException(nameof(initializationManager));
            
            // Get animations from resources
            _pulseAnimation = (Storyboard)FindResource("PulseAnimation");
            _rotateAnimation = (Storyboard)FindResource("RotateAnimation");
            _typewriterAnimation = (Storyboard)FindResource("TypewriterAnimation");
            
            // Create inner rotation animation
            _innerRotateAnimation = new Storyboard();
            var innerRotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = -360,
                Duration = TimeSpan.FromSeconds(1.5),
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(innerRotateAnimation, InnerRotateTransform);
            Storyboard.SetTargetProperty(innerRotateAnimation, new PropertyPath("Angle"));
            _innerRotateAnimation.Children.Add(innerRotateAnimation);

            // Start animations
            StartAnimations();
            
            // Subscribe to initialization events
            _initializationManager.InitializationProgressChanged += OnInitializationProgressChanged;
            _initializationManager.InitializationCompleted += OnInitializationCompleted;
            _initializationManager.InitializationFailed += OnInitializationFailed;
            
            // Start initialization process
            _ = StartInitializationAsync();
        }

        private void StartAnimations()
        {
            try
            {
                _pulseAnimation?.Begin();
                _rotateAnimation?.Begin();
                _innerRotateAnimation?.Begin();
                _typewriterAnimation?.Begin();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to start loading animations");
            }
        }

        private void StopAnimations()
        {
            try
            {
                _pulseAnimation?.Stop();
                _rotateAnimation?.Stop();
                _innerRotateAnimation?.Stop();
                _typewriterAnimation?.Stop();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to stop loading animations");
            }
        }

        private async Task StartInitializationAsync()
        {
            try
            {
                _logger?.LogInformation("Starting Lazarus initialization...");
                
                // Set timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                var initTask = _initializationManager.InitializeAsync();
                
                var completedTask = await Task.WhenAny(initTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _logger?.LogWarning("Initialization timed out after 30 seconds");
                    ShowError("Initialization timed out. The application may be taking longer than expected to start.");
                }
                else
                {
                    await initTask; // Ensure we get any exceptions
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Initialization failed with exception");
                ShowError($"Initialization failed: {ex.Message}");
            }
        }

        private void OnInitializationProgressChanged(object? sender, InitializationProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressText.Text = e.Message;
                _logger?.LogDebug("Initialization progress: {Progress}", e.Message);
            });
        }

        private void OnInitializationCompleted(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_isInitialized) return;
                _isInitialized = true;
                
                _logger?.LogInformation("Initialization completed successfully");
                ProgressText.Text = "Ready!";
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = 100;
                
                // Stop animations
                StopAnimations();
                
                // Fade out effect
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(500)
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
                _logger?.LogError("Initialization failed: {Error}", e.Error);
                ShowError(e.Error);
            });
        }

        private void ShowError(string errorMessage)
        {
            StopAnimations();
            
            LoadingText.Text = "INITIALIZATION FAILED";
            LoadingText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFF6B6B"));
            
            ProgressText.Visibility = Visibility.Collapsed;
            ProgressBar.Visibility = Visibility.Collapsed;
            
            ErrorMessage.Text = errorMessage;
            ErrorPanel.Visibility = Visibility.Visible;
            
            InitializationFailed?.Invoke(this, EventArgs.Empty);
        }

        private async void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Reset UI
                ErrorPanel.Visibility = Visibility.Collapsed;
                ProgressText.Visibility = Visibility.Visible;
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.IsIndeterminate = true;
                
                LoadingText.Text = "INITIALIZING LAZARUS...";
                LoadingText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFFFF"));
                
                ProgressText.Text = "Retrying...";
                
                // Restart animations
                StartAnimations();
                
                // Retry initialization
                await StartInitializationAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Retry failed");
                ShowError($"Retry failed: {ex.Message}");
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("User requested exit during initialization");
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            _initializationManager.InitializationProgressChanged -= OnInitializationProgressChanged;
            _initializationManager.InitializationCompleted -= OnInitializationCompleted;
            _initializationManager.InitializationFailed -= OnInitializationFailed;
            
            // Stop animations
            StopAnimations();
            
            base.OnClosed(e);
        }
    }
}
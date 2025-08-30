using System;
using System.Collections.Concurrent;
using App.Shared.Enums;
using Lazarus.Desktop.ViewModels;
using Lazarus.Desktop.ViewModels.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Services.Dashboard
{
    /// <summary>
    /// Thread-safe factory for creating and caching ViewMode-specific Dashboard ViewModels
    /// Ensures proper UI thread creation and prevents threading violations
    /// </summary>
    public class DashboardViewModelFactory : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<ViewMode, object> _dashboardCache = new();
        private readonly object _lockObject = new object();

        public DashboardViewModelFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Get or create a Dashboard ViewModel for the specified ViewMode
        /// Thread-safe and caches instances to prevent recreation
        /// </summary>
        public object GetDashboardForViewMode(ViewMode viewMode)
        {
            // Check cache first
            if (_dashboardCache.TryGetValue(viewMode, out var cachedDashboard))
            {
                return cachedDashboard;
            }

            // Create new Dashboard ViewModel on UI thread
            lock (_lockObject)
            {
                // Double-check pattern - another thread might have created it
                if (_dashboardCache.TryGetValue(viewMode, out var doubleCheckedDashboard))
                {
                    return doubleCheckedDashboard;
                }

                try
                {
                    var dashboard = CreateDashboardForViewMode(viewMode);
                    _dashboardCache[viewMode] = dashboard;
                    
                    Console.WriteLine($"[DashboardFactory] Created {viewMode} Dashboard ViewModel");
                    return dashboard;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DashboardFactory] Failed to create {viewMode} Dashboard: {ex.Message}");
                    
                    // Return fallback Dashboard
                    var fallback = _serviceProvider.GetService<DashboardViewModel>();
                    if (fallback != null)
                    {
                        _dashboardCache[viewMode] = fallback;
                        return fallback;
                    }
                    
                    throw; // Re-throw if we can't even get a fallback
                }
            }
        }

        /// <summary>
        /// Clear the cache for a specific ViewMode (useful when switching themes or reloading)
        /// </summary>
        public void ClearCache(ViewMode viewMode)
        {
            lock (_lockObject)
            {
                if (_dashboardCache.TryRemove(viewMode, out var dashboard))
                {
                    // Dispose the old Dashboard if it implements IDisposable
                    if (dashboard is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    
                    Console.WriteLine($"[DashboardFactory] Cleared cache for {viewMode} Dashboard");
                }
            }
        }

        /// <summary>
        /// Clear all cached Dashboard ViewModels
        /// </summary>
        public void ClearAllCache()
        {
            lock (_lockObject)
            {
                foreach (var kvp in _dashboardCache)
                {
                    if (kvp.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                
                _dashboardCache.Clear();
                Console.WriteLine("[DashboardFactory] Cleared all Dashboard cache");
            }
        }

        /// <summary>
        /// Create a new Dashboard ViewModel for the specified ViewMode
        /// </summary>
        private object CreateDashboardForViewMode(ViewMode viewMode)
        {
            return viewMode switch
            {
                ViewMode.Novice => CreateNoviceDashboard(),
                ViewMode.Enthusiast => CreateEnthusiastDashboard(),
                ViewMode.Developer => CreateDeveloperDashboard(),
                _ => CreateNoviceDashboard() // Fallback to Novice
            };
        }

        private object CreateNoviceDashboard()
        {
            // Ensure we're on the UI thread for Dashboard creation
            if (!App.Current.Dispatcher.CheckAccess())
            {
                return App.Current.Dispatcher.Invoke(() => CreateNoviceDashboard());
            }

            var dashboard = _serviceProvider.GetService<NoviceDashboardViewModel>();
            if (dashboard == null)
            {
                Console.WriteLine("[DashboardFactory] NoviceDashboardViewModel service not found - using fallback");
                return _serviceProvider.GetRequiredService<DashboardViewModel>();
            }

            return dashboard;
        }

        private object CreateEnthusiastDashboard()
        {
            // Ensure we're on the UI thread for Dashboard creation
            if (!App.Current.Dispatcher.CheckAccess())
            {
                return App.Current.Dispatcher.Invoke(() => CreateEnthusiastDashboard());
            }

            var dashboard = _serviceProvider.GetService<EnthusiastDashboardViewModel>();
            if (dashboard == null)
            {
                Console.WriteLine("[DashboardFactory] EnthusiastDashboardViewModel service not found - using fallback");
                return _serviceProvider.GetRequiredService<DashboardViewModel>();
            }

            return dashboard;
        }

        private object CreateDeveloperDashboard()
        {
            // Ensure we're on the UI thread for Dashboard creation
            if (!App.Current.Dispatcher.CheckAccess())
            {
                return App.Current.Dispatcher.Invoke(() => CreateDeveloperDashboard());
            }

            var dashboard = _serviceProvider.GetService<DeveloperDashboardViewModel>();
            if (dashboard == null)
            {
                Console.WriteLine("[DashboardFactory] DeveloperDashboardViewModel service not found - using fallback");
                return _serviceProvider.GetRequiredService<DashboardViewModel>();
            }

            return dashboard;
        }

        public void Dispose()
        {
            ClearAllCache();
        }
    }
}

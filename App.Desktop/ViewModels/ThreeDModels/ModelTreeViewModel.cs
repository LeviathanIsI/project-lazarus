using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;

namespace Lazarus.Desktop.ViewModels.ThreeDModels
{
    public class ModelNode : INotifyPropertyChanged
    {
        // [ModelNode class remains exactly the same - no changes needed]
        private bool _isExpanded;
        private bool _isSelected;
        private string _name = string.Empty;
        private string _fullPath = string.Empty;
        private bool _isDirectory;
        private ObservableCollection<ModelNode> _children = new();

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string FullPath
        {
            get => _fullPath;
            set => SetProperty(ref _fullPath, value);
        }

        public bool IsDirectory
        {
            get => _isDirectory;
            set => SetProperty(ref _isDirectory, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public ObservableCollection<ModelNode> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }

        public string Icon => IsDirectory ? "📁" : GetFileIcon();
        public string FileSize => IsDirectory ? "" : GetFileSizeFormatted();
        public string FileType => IsDirectory ? "Folder" : Path.GetExtension(FullPath).ToUpperInvariant().TrimStart('.');

        private string GetFileIcon()
        {
            var ext = Path.GetExtension(FullPath).ToLowerInvariant();
            return ext switch
            {
                ".obj" => "🎭",
                ".fbx" => "🎪",
                ".blend" => "🔥",
                ".dae" => "⚗️",
                ".3ds" => "🎲",
                ".gltf" => "✨",
                ".glb" => "💎",
                _ => "📄"
            };
        }

        private string GetFileSizeFormatted()
        {
            if (!File.Exists(FullPath)) return "";

            var fileInfo = new FileInfo(FullPath);
            var size = fileInfo.Length;

            return size switch
            {
                < 1024 => $"{size} B",
                < 1024 * 1024 => $"{size / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{size / (1024.0 * 1024.0):F1} MB",
                _ => $"{size / (1024.0 * 1024.0 * 1024.0):F1} GB"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class ModelTreeViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ModelNode> _rootNodes = new();
        private string _searchFilter = string.Empty;
        private bool _isLoading;
        private string _statusText = "Ready";
        private ModelNode? _selectedNode;
        private string _basePath = string.Empty;

        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".obj", ".fbx", ".dae", ".3ds", ".blend", ".gltf", ".glb", ".ply", ".stl"
        };

        public ObservableCollection<ModelNode> RootNodes
        {
            get => _rootNodes;
            set => SetProperty(ref _rootNodes, value);
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                SetProperty(ref _searchFilter, value);
                FilterNodes();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public ModelNode? SelectedNode
        {
            get => _selectedNode;
            set => SetProperty(ref _selectedNode, value);
        }

        public string BasePath
        {
            get => _basePath;
            set => SetProperty(ref _basePath, value);
        }

        // Commands - removed ImportModelCommand, renamed SetBasePathCommand
        public ICommand RefreshCommand { get; }
        public ICommand BrowsePathCommand { get; }
        public ICommand NodeSelectedCommand { get; }

        public ModelTreeViewModel()
        {
            RefreshCommand = new Lazarus.Desktop.Helpers.RelayCommand(_ => _ = Task.Run(RefreshModelTree));
            BrowsePathCommand = new Lazarus.Desktop.Helpers.RelayCommand(_ => BrowseForPath());
            NodeSelectedCommand = new Lazarus.Desktop.Helpers.RelayCommand(param => SelectNode(param as ModelNode));

            BasePath = LoadSavedBasePath();
            Directory.CreateDirectory(BasePath);

            _ = Task.Run(RefreshModelTree);
        }

        private string LoadSavedBasePath()
        {
            try
            {
                var savedPath = GetSavedPathFromRegistry();

                if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                {
                    return savedPath;
                }
            }
            catch
            {
                // Fall back to default if anything goes wrong
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "3D Models");
        }

        private string GetSavedPathFromRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\ProjectLazarus");
                return key?.GetValue("ModelsBasePath") as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void SaveBasePathToRegistry(string path)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\ProjectLazarus");
                key?.SetValue("ModelsBasePath", path);
            }
            catch
            {
                // Silent fail - not critical if we can't save preferences
            }
        }

        private void BrowseForPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Your 3D Models Directory",
                InitialDirectory = BasePath
            };

            if (dialog.ShowDialog() == true)
            {
                var newPath = dialog.FolderName;
                if (Directory.Exists(newPath))
                {
                    BasePath = newPath;
                    SaveBasePathToRegistry(BasePath);
                    StatusText = $"Directory: {Path.GetFileName(BasePath)}";

                    _ = Task.Run(RefreshModelTree);
                }
            }
        }

        // [Rest of the methods remain the same - RefreshModelTree, LoadDirectoryNodes, etc.]
        private async Task RefreshModelTree()
        {
            IsLoading = true;
            StatusText = "Scanning for 3D models...";

            try
            {
                await Task.Run(() =>
                {
                    var rootNodes = new ObservableCollection<ModelNode>();

                    if (Directory.Exists(BasePath))
                    {
                        LoadDirectoryNodes(BasePath, rootNodes);
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        RootNodes.Clear();
                        foreach (var node in rootNodes)
                        {
                            RootNodes.Add(node);
                        }
                    });
                });

                var totalModels = CountModels(RootNodes);
                StatusText = $"Found {totalModels} 3D models in {Path.GetFileName(BasePath)}";
            }
            catch (Exception ex)
            {
                StatusText = $"Error scanning directory: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadDirectoryNodes(string directoryPath, ObservableCollection<ModelNode> parentNodes)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(directoryPath);

                foreach (var subDir in directoryInfo.GetDirectories().OrderBy(d => d.Name))
                {
                    var dirNode = new ModelNode
                    {
                        Name = subDir.Name,
                        FullPath = subDir.FullName,
                        IsDirectory = true
                    };

                    LoadDirectoryNodes(subDir.FullName, dirNode.Children);

                    if (ContainsModels(dirNode))
                    {
                        parentNodes.Add(dirNode);
                    }
                }

                foreach (var file in directoryInfo.GetFiles().OrderBy(f => f.Name))
                {
                    if (SupportedExtensions.Contains(file.Extension))
                    {
                        var fileNode = new ModelNode
                        {
                            Name = file.Name,
                            FullPath = file.FullName,
                            IsDirectory = false
                        };

                        parentNodes.Add(fileNode);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading directory {directoryPath}: {ex.Message}");
            }
        }

        private bool ContainsModels(ModelNode node)
        {
            if (!node.IsDirectory) return SupportedExtensions.Contains(Path.GetExtension(node.FullPath));

            return node.Children.Any(child =>
                !child.IsDirectory || ContainsModels(child));
        }

        private int CountModels(ObservableCollection<ModelNode> nodes)
        {
            return nodes.Sum(node =>
                node.IsDirectory ? CountModels(node.Children) : 1);
        }

        private void FilterNodes()
        {
            if (string.IsNullOrWhiteSpace(SearchFilter))
            {
                _ = Task.Run(RefreshModelTree);
            }
        }

        private void SelectNode(ModelNode? node)
        {
            if (_selectedNode != null)
            {
                _selectedNode.IsSelected = false;
            }

            SelectedNode = node;
            if (node != null)
            {
                node.IsSelected = true;
                if (!node.IsDirectory)
                {
                    StatusText = $"Selected: {node.Name} ({node.FileSize})";
                    // TODO: Signal viewport to load this model
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
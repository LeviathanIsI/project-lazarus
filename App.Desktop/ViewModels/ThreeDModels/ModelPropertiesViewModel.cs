using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.ViewModels.ThreeDModels
{
    /// <summary>
    /// ViewModel for the Model Properties panel
    /// </summary>
    public class ModelPropertiesViewModel : INotifyPropertyChanged
    {
        private string? _selectedModelPath;
        private bool _hasModel;
        private int _vertexCount;
        private int _materialCount;
        private string _fileSize = "";

        public string? SelectedModelPath
        {
            get => _selectedModelPath;
            set
            {
                if (SetProperty(ref _selectedModelPath, value))
                {
                    LoadModelProperties();
                }
            }
        }

        public bool HasModel
        {
            get => _hasModel;
            set => SetProperty(ref _hasModel, value);
        }

        public int VertexCount
        {
            get => _vertexCount;
            set => SetProperty(ref _vertexCount, value);
        }

        public int MaterialCount
        {
            get => _materialCount;
            set => SetProperty(ref _materialCount, value);
        }

        public string FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        private void LoadModelProperties()
        {
            if (string.IsNullOrEmpty(SelectedModelPath) || !System.IO.File.Exists(SelectedModelPath))
            {
                HasModel = false;
                VertexCount = 0;
                MaterialCount = 0;
                FileSize = "";
                return;
            }

            HasModel = true;
            
            // TODO: Implement actual model parsing to get vertex/material counts
            // For now, just show placeholder values and file size
            VertexCount = 1234; // Placeholder
            MaterialCount = 2; // Placeholder
            
            var fileInfo = new System.IO.FileInfo(SelectedModelPath);
            FileSize = FormatFileSize(fileInfo.Length);
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            
            while (System.Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) 
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
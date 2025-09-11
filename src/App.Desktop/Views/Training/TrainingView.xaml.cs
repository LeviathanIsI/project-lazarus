using System;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels.Training;

namespace Lazarus.Desktop.Views.Training
{
    public partial class TrainingView : UserControl, IDisposable
    {
        private readonly TrainingViewModel _vm;
        public TrainingView()
        {
            InitializeComponent();
            // TODO(training): consider resolving via DI; for now create a mock training service
            var mockTrainingService = new MockTrainingService();
            _vm = new TrainingViewModel(mockTrainingService);
            DataContext = _vm;
        }

        public void Dispose()
        {
            _vm.Dispose();
        }
    }
}


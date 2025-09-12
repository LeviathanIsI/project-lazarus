using System;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels.Training;
using Lazarus.Desktop.Services;
using Lazarus.Backend.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views.Training
{
    public partial class TrainingView : UserControl, IDisposable
    {
        private readonly TrainingViewModel _vm;
        public TrainingView()
        {
            InitializeComponent();
            // Resolve via DI to ensure correct lifetimes
            var sp = Lazarus.Desktop.App.ServiceProvider;
            var trainingService = sp.GetRequiredService<Lazarus.Shared.Contracts.ITrainingService>();
            var conversationService = sp.GetRequiredService<IConversationTrainingService>();
            _vm = new TrainingViewModel(trainingService, conversationService);
            DataContext = _vm;
        }

        public void Dispose()
        {
            _vm.Dispose();
        }
    }
}


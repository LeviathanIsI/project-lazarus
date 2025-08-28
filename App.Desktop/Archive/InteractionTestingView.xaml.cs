using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Lazarus.Desktop.ViewModels.Entities;

namespace Lazarus.Desktop.Views.Entities
{
    public partial class InteractionTestingView : UserControl
    {
        public InteractionTestingView()
        {
            InitializeComponent();
        }

        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                if (DataContext is InteractionTestingViewModel viewModel && viewModel.SendMessageCommand.CanExecute(null))
                {
                    viewModel.SendMessageCommand.Execute(null);
                }
            }
        }
    }
}
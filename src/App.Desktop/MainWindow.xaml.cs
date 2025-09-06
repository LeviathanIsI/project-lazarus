using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Lazarus.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateMaximizeRestoreButton();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            UpdateMaximizeRestoreButton();
            base.OnStateChanged(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                RequestClose();
            }
            else if (e.Key == Key.F4 && Keyboard.Modifiers == ModifierKeys.Alt)
            {
                RequestClose();
            }
            base.OnKeyDown(e);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            RequestClose();
        }

        private void RequestClose()
        {
            Close();
        }

        private void UpdateMaximizeRestoreButton()
        {
            if (MaximizeRestoreIcon != null)
            {
                if (WindowState == WindowState.Maximized)
                {
                    MaximizeRestoreIcon.Data = Geometry.Parse("M5 16H8V14H5V11H3V16H5ZM8 8V5H5V3H10V8H8ZM14 19H19V14H21V19H14ZM16 8V3H21V5H19V8H16Z");
                    MaximizeRestoreButton.ToolTip = "Restore Down";
                }
                else
                {
                    MaximizeRestoreIcon.Data = Geometry.Parse("M3 3H21V21H3V3ZM5 5V19H19V5H5Z");
                    MaximizeRestoreButton.ToolTip = "Maximize";
                }
            }
        }
    }
}
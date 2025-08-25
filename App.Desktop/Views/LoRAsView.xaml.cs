using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Views
{
    public partial class LorAsView : UserControl
    {
        private static readonly Regex FloatRegex = new(@"^[0-9]*\.?[0-9]*$");
        
        public LorAsView()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[EMERGENCY] LorAsView constructor START");
                Console.WriteLine("[EMERGENCY] LorAsView constructor START");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("[EMERGENCY] LorAsView constructor COMPLETED");
                Console.WriteLine("[EMERGENCY] LorAsView constructor COMPLETED");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EMERGENCY] LorAsView constructor CRASHED: {ex.Message}");
                Console.WriteLine($"[EMERGENCY] LorAsView constructor exception: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[EMERGENCY] Stack trace: {ex.StackTrace}");
                throw; // Re-throw to prevent silent failures
            }
        }
        
        /// <summary>
        /// Handle Apply button click - forwards to ViewModel command
        /// </summary>
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[EMERGENCY] ApplyButton_Click START");
                Console.WriteLine($"[EMERGENCY] ApplyButton_Click triggered");
                
                if (sender is Button button && button.DataContext is LoRADto lora && DataContext is LorAsViewModel viewModel)
                {
                    System.Diagnostics.Debug.WriteLine($"[EMERGENCY] Executing ApplyLoRACommand for: {lora.Name}");
                    Console.WriteLine($"[EMERGENCY] Applying LoRA: {lora.Name ?? "null"}");
                    
                    if (viewModel.ApplyLoRACommand.CanExecute(lora))
                    {
                        viewModel.ApplyLoRACommand.Execute(lora);
                        System.Diagnostics.Debug.WriteLine("[EMERGENCY] ApplyLoRACommand executed successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[EMERGENCY] ApplyLoRACommand.CanExecute returned false");
                        Console.WriteLine("[EMERGENCY] Cannot execute ApplyLoRACommand - command not available");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[EMERGENCY] ApplyButton_Click - invalid context");
                    Console.WriteLine($"[EMERGENCY] Invalid context - sender: {sender?.GetType().Name}, DataContext: {DataContext?.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EMERGENCY] ApplyButton_Click CRASHED: {ex.Message}");
                Console.WriteLine($"[EMERGENCY] ApplyButton_Click exception: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[EMERGENCY] Stack trace: {ex.StackTrace}");
                
                // Show user-friendly error instead of crashing
                MessageBox.Show($"Failed to apply LoRA: {ex.Message}", "LoRA Application Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Validate text input for weight values (0.00-2.00)
        /// </summary>
        private void WeightTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            
            var newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            
            // Allow valid float format and restrict range
            if (!FloatRegex.IsMatch(newText) || 
                (float.TryParse(newText, out var value) && (value < 0.0f || value > 2.0f)))
            {
                e.Handled = true;
            }
        }
        
        /// <summary>
        /// Handle Enter key to apply weight changes immediately
        /// </summary>
        private void WeightTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                // Force binding update and move focus to apply change
                textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
            }
        }
    }
}
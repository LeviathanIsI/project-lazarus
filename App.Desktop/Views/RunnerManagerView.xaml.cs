using System.Windows.Controls;

namespace Lazarus.Desktop.Views
{
    /// <summary>
    /// Runner Manager - Control and manage LLM backends
    /// Provides interface for switching between llama.cpp, vLLM, ExLlamaV2, etc.
    /// </summary>
    public partial class RunnerManagerView : UserControl
    {
        public RunnerManagerView()
        {
            InitializeComponent();
        }
    }
}
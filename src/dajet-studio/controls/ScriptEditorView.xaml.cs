using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DaJet.Studio
{
    public partial class ScriptEditorView : UserControl
    {
        public ScriptEditorView()
        {
            InitializeComponent();
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            BindingExpression binding = ScriptCodeTextBox.GetBindingExpression(TextBox.TextProperty);
            if (binding != null)
            {
                binding.UpdateSource();
            }
        }
    }
}
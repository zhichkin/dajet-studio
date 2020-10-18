using System.Windows;
using System.Windows.Controls;

namespace DaJet.Studio.MVVM
{
    public partial class TreeNodeView : UserControl
    {
        public TreeNodeView()
        {
            InitializeComponent();
        }
        private void NodeNameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (!(sender is TextBox textBox)) return;
            if (textBox.IsVisible)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        }
    }
}
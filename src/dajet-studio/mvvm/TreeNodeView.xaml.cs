using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            TreeView control = sender as TreeView;
            if (control == null) return;

            Point point = e.GetPosition(this);
            HitTestResult result = VisualTreeHelper.HitTest(this, point);
            if (result == null) return;

            DependencyObject obj = result.VisualHit;
            while (VisualTreeHelper.GetParent(obj) != null && !(obj is TextBlock))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            TextBlock item = obj as TextBlock;
            if (item == null) return;
            if (item.DataContext == null) return;

            DragDrop.DoDragDrop(control, item.DataContext.ToString(), DragDropEffects.Copy);
        }
        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1) // check if it is double click
            {
                e.Handled = true;
                TreeNodeViewModel viewModel = this.DataContext as TreeNodeViewModel;
                if (viewModel == null) return;
                TreeView item = sender as TreeView;
                if (item == null) return;
                if (item.SelectedItem == null) return;
                //viewModel.TreeViewDoubleClickCommand.Execute(item.SelectedItem);
            }
        }
    }
}
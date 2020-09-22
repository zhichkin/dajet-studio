using System.Windows.Controls;

namespace DaJet.UI
{
    public partial class SelectServerDialogView : UserControl
    {
        public SelectServerDialogView(SelectServerDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

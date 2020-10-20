using System.Windows.Controls;

namespace DaJet.UI
{
    public partial class ConnectSQLServerDialogView : UserControl
    {
        public ConnectSQLServerDialogView(ConnectSQLServerDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

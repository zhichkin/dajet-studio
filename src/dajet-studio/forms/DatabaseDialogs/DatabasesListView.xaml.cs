using System.Windows.Controls;

namespace DaJet.Studio
{
    public partial class DatabasesListView : UserControl
    {
        public DatabasesListView(DatabasesListViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
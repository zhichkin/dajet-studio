using System.Windows.Controls;

namespace DaJet.Studio
{
    public partial class WebServerListView : UserControl
    {
        public WebServerListView(WebServerListViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
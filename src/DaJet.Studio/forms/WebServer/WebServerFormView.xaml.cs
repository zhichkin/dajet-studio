using System.Windows.Controls;

namespace DaJet.Studio
{
    public partial class WebServerFormView : UserControl
    {
        public WebServerFormView(WebServerFormViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
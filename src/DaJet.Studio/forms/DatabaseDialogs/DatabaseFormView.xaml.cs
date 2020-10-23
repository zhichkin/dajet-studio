using System.Windows.Controls;

namespace DaJet.Studio
{
    public partial class DatabaseFormView : UserControl
    {
        public DatabaseFormView(DatabaseFormViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
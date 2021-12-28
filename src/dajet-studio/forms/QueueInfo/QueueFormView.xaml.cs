using System.Windows.Controls;

namespace DaJet.Studio
{
    public partial class QueueFormView : UserControl
    {
        public QueueFormView(QueueFormViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
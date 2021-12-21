using DaJet.UI.Model;
using System.Windows;

namespace DaJet.Studio.UI
{
    public partial class RabbitMQServerForm : Window
    {
        private RabbitMQServer Model { get; }
        private RabbitMQServerViewModel ViewModel { get; }
        public RabbitMQServerForm(RabbitMQServer model)
        {
            InitializeComponent();

            Model = model;
            RabbitMQServer copy = Model.Copy();
            ViewModel = new RabbitMQServerViewModel(copy);
            DataContext = ViewModel;
        }
        private void Confirm_Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Model.CopyTo(Model);
            DialogResult = true;
            Close();
        }
        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
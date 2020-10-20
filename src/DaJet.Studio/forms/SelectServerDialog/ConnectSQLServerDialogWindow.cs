using DaJet.Metadata;
using System.Windows;

namespace DaJet.UI
{
    public sealed class ConnectSQLServerDialogWindow : Window
    {
        private readonly ConnectSQLServerDialogViewModel viewModel;
        private void ConfigureWindowProperties()
        {
            this.SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Content = new ConnectSQLServerDialogView(viewModel);
        }
        public ConnectSQLServerDialogWindow()
        {
            this.Title = "Add SQL Server";
            viewModel = new ConnectSQLServerDialogViewModel()
            {
                OnCancel = OnCancel,
                OnConfirm = OnConfirm
            };
            ConfigureWindowProperties();
        }
        public ConnectSQLServerDialogWindow(DatabaseServer server)
        {
            this.Title = "Edit SQL Server";
            viewModel = new ConnectSQLServerDialogViewModel(server)
            {
                OnCancel = OnCancel,
                OnConfirm = OnConfirm
            };
            ConfigureWindowProperties();
        }
        public DatabaseServer Result { get; private set; }
        private void OnConfirm(DatabaseServer result)
        {
            Result = result;
            this.Close();
        }
        private void OnCancel()
        {
            Result = null;
            this.Close();
        }
    }
}
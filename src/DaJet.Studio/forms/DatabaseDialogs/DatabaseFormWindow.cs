using DaJet.Metadata;
using System.Windows;

namespace DaJet.Studio
{
    public sealed class DatabaseFormWindow : Window
    {
        private readonly DatabaseFormViewModel viewModel;
        private void ConfigureWindowProperties()
        {
            this.SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Content = new DatabaseFormView(viewModel);
        }
        public DatabaseFormWindow()
        {
            this.Title = "Add database";
            viewModel = new DatabaseFormViewModel()
            {
                OnCancel = OnCancel,
                OnConfirm = OnConfirm
            };
            ConfigureWindowProperties();
        }
        public DatabaseFormWindow(DatabaseInfo database)
        {
            this.Title = "Edit database";
            viewModel = new DatabaseFormViewModel(database)
            {
                OnCancel = OnCancel,
                OnConfirm = OnConfirm
            };
            ConfigureWindowProperties();
        }
        public DatabaseInfo Result { get; private set; }
        private void OnConfirm(DatabaseInfo result)
        {
            Result = result;
            this.DialogResult = true;
            this.Close();
        }
        private void OnCancel()
        {
            Result = null;
            this.DialogResult = false;
            this.Close();
        }
    }
}
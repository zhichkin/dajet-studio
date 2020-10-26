using System.Windows;

namespace DaJet.Studio
{
    public sealed class WebServerFormWindow : Window
    {
        private readonly WebServerFormViewModel viewModel;
        private void ConfigureWindowProperties()
        {
            this.SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Content = new WebServerFormView(viewModel);
        }
        public WebServerFormWindow()
        {
            this.Title = "Add web server";
            viewModel = new WebServerFormViewModel()
            {
                OnCancel = OnCancel,
                OnConfirm = OnConfirm
            };
            ConfigureWindowProperties();
        }
        public WebServerFormWindow(WebServer model)
        {
            this.Title = "Edit web server";
            viewModel = new WebServerFormViewModel(model)
            {
                OnCancel = OnCancel,
                OnConfirm = OnConfirm
            };
            ConfigureWindowProperties();
        }
        public WebServer Result { get; private set; }
        private void OnConfirm(WebServer result)
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
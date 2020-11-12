using System.Collections.Generic;
using System.Windows;

namespace DaJet.Studio
{
    public sealed class SelectWebServerWindow : Window
    {
        private readonly WebServerListViewModel viewModel;
        public SelectWebServerWindow(List<WebServer> servers)
        {
            this.Title = "Select web server...";
            this.SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            viewModel = new WebServerListViewModel(servers);
            viewModel.OnCancel = OnCancel;
            viewModel.OnConfirm = OnConfirm;
            this.Content = new WebServerListView(viewModel);
        }
        public WebServer Result { get; private set; }
        private void OnConfirm(WebServer result)
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
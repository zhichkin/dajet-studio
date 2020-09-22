using System.Windows;

namespace DaJet.UI
{
    public sealed class SelectServerDialogWindow : Window
    {
        private readonly SelectServerDialogViewModel viewModel;
        public SelectServerDialogWindow()
        {
            this.Title = "Input server address ...";
            this.SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            viewModel = new SelectServerDialogViewModel();
            viewModel.OnCancel = OnCancel;
            viewModel.OnConfirm = OnConfirm;
            this.Content = new SelectServerDialogView(viewModel);
        }
        public string Result { get; private set; }
        private void OnConfirm(string result)
        {
            Result = result;
            this.Close();
        }
        private void OnCancel()
        {
            this.Close();
        }
    }
}
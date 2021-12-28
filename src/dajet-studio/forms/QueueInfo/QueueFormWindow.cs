using DaJet.Messaging;
using System.Windows;

namespace DaJet.Studio
{
    public sealed class QueueFormWindow : Window
    {
        private readonly QueueFormViewModel viewModel;
        private void ConfigureWindowProperties()
        {
            this.SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Content = new QueueFormView(viewModel);
        }
        public QueueFormWindow()
        {
            this.Title = "Create new queue";
            viewModel = new QueueFormViewModel()
            {
                OnCancel = OnCancel,
                OnConfirm = OnConfirm
            };
            ConfigureWindowProperties();
        }
        public QueueFormWindow(QueueInfo model)
        {
            this.Title = "Edit queue settings";
            viewModel = new QueueFormViewModel(model)
            {
                OnCancel = OnCancel,
                OnConfirm = OnConfirm
            };
            ConfigureWindowProperties();
        }
        public QueueInfo Result { get; private set; }
        private void OnConfirm(QueueInfo result)
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
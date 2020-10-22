using DaJet.Metadata;
using System.Collections.Generic;
using System.Windows;

namespace DaJet.Studio
{
    public sealed class SelectDatabaseWindow : Window
    {
        private readonly DatabasesListViewModel viewModel;
        public SelectDatabaseWindow(List<DatabaseInfo> databases)
        {
            this.Title = "Select database...";
            this.SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            viewModel = new DatabasesListViewModel(databases);
            viewModel.OnCancel = OnCancel;
            viewModel.OnConfirm = OnConfirm;
            this.Content = new DatabasesListView(viewModel);
        }
        public DatabaseInfo Result { get; private set; }
        private void OnConfirm(DatabaseInfo result)
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
using DaJet.Studio.MVVM;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DaJet.Studio.UI
{
    public sealed class DataExportTaskListViewModel : IListViewModelController
    {
        private IListViewModelController Parent { get; set; }
        public DataExportTaskListViewModel(IListViewModelController parent)
        {
            Parent = parent;
        }
        public ObservableCollection<DataExportTaskViewModel> TaskLog { get; private set; } = new ObservableCollection<DataExportTaskViewModel>();
        public DataExportTaskViewModel CreateNewEntry()
        {
            return new DataExportTaskViewModel(this);
        }
        public void AddNewItem() { throw new System.NotImplementedException(); }
        public void EditItem(object item) { throw new System.NotImplementedException(); }
        public void CopyItem(object item)
        {
            if (!(item is DataExportTaskViewModel model)) return;

            Parent.EditItem(item);
        }
        public void RemoveItem(object item)
        {
            if (!(item is DataExportTaskViewModel model)) return;

            TaskLog.Remove(model);
        }
    }
    public sealed class DataExportTaskViewModel
    {
        private IListViewModelController Parent { get; set; }
        public DataExportTaskViewModel(IListViewModelController parent)
        {
            Parent = parent;
            RepeatTaskCommand = new RelayCommand(RepeatTaskCommandHandler);
            RemoveTaskCommand = new RelayCommand(RemoveTaskCommandHandler);
        }
        public string Description { get; set; }
        public int PageSize { get; set; }
        public string PageNumber { get; set; }
        public int ExportResult { get; set; }
        public string RoutingKey { get; set; }
        public ICommand RepeatTaskCommand { get; private set; }
        public ICommand RemoveTaskCommand { get; private set; }
        private void RemoveTaskCommandHandler(object parameter)
        {
            Parent.RemoveItem(parameter);
        }
        private void RepeatTaskCommandHandler(object parameter)
        {
            Parent.CopyItem(parameter);
        }
    }
}
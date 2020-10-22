using DaJet.Metadata;
using DaJet.Studio.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DaJet.Studio
{
    public sealed class DatabasesListViewModel : ViewModelBase
    {
        public DatabasesListViewModel(List<DatabaseInfo> databases)
        {
            Databases = new ObservableCollection<DatabaseInfo>();
            foreach (DatabaseInfo database in databases)
            {
                Databases.Add(database);
            }
            CancelCommand = new RelayCommand(Cancel);
            ConfirmCommand = new RelayCommand(Confirm);
        }
        public ObservableCollection<DatabaseInfo> Databases { get; private set; }
        public Action OnCancel { get; set; }
        public Action<DatabaseInfo> OnConfirm { get; set; }
        public DatabaseInfo SelectedItem { get; set; }
        public ICommand ConfirmCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        private void Confirm(object parameter)
        {
            OnConfirm?.Invoke(SelectedItem);
        }
        private void Cancel(object parameter)
        {
            OnCancel?.Invoke();
        }
    }
}
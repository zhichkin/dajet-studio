using DaJet.Metadata;
using DaJet.Studio.MVVM;
using System;
using System.Windows.Input;

namespace DaJet.Studio
{
    public sealed class DatabaseFormViewModel : ViewModelBase
    {
        private DatabaseInfo MyDatabase { get; } = new DatabaseInfo();
        public DatabaseFormViewModel()
        {
            ConfirmCommand = new RelayCommand(Confirm);
            CancelCommand = new RelayCommand(Cancel);
        }
        public DatabaseFormViewModel(DatabaseInfo database) : this()
        {
            MyDatabase = database;
        }
        public ICommand ConfirmCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public Action OnCancel { get; set; }
        public Action<DatabaseInfo> OnConfirm { get; set; }
        public string Name
        {
            get { return MyDatabase.Name; }
            set { MyDatabase.Name = value; OnPropertyChanged(); }
        }
        public string Alias
        {
            get { return MyDatabase.Alias; }
            set { MyDatabase.Alias = value; OnPropertyChanged(); }
        }
        public string UserName
        {
            get { return MyDatabase.UserName; }
            set { MyDatabase.UserName = value; OnPropertyChanged(); }
        }
        public string Password
        {
            get { return MyDatabase.Password; }
            set { MyDatabase.Password = value; OnPropertyChanged(); }
        }
        private void Confirm(object parameter)
        {
            OnConfirm?.Invoke(MyDatabase);
        }
        private void Cancel(object parameter)
        {
            OnCancel?.Invoke();
        }
    }
}
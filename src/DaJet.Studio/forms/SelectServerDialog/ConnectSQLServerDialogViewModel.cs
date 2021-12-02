using DaJet.Studio.MVVM;
using DaJet.UI.Model;
using System;
using System.Windows.Input;

namespace DaJet.UI
{
    public sealed class ConnectSQLServerDialogViewModel : ViewModelBase
    {
        private DatabaseServer MyServer { get; } = new DatabaseServer();
        public ConnectSQLServerDialogViewModel()
        {
            ConfirmCommand = new RelayCommand(Confirm);
            CancelCommand = new RelayCommand(Cancel);
        }
        public ConnectSQLServerDialogViewModel(DatabaseServer server) : this()
        {
            MyServer = server;
        }
        public ICommand ConfirmCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public Action OnCancel { get; set; }
        public Action<DatabaseServer> OnConfirm { get; set; }
        public string ServerName
        {
            get { return MyServer.Name; }
            set { MyServer.Name = value; OnPropertyChanged(); }
        }
        //public string ServerAddress
        //{
        //    get { return MyServer.Address; }
        //    set { MyServer.Address = value; OnPropertyChanged(); }
        //}
        //public string NetworkAddress
        //{
        //    get { return MyServer.NetworkAddress; }
        //    set { MyServer.NetworkAddress = value; OnPropertyChanged(); }
        //}
        //public int ServiceBrokerPortNumber
        //{
        //    get { return MyServer.ServiceBrokerPortNumber; }
        //    set { MyServer.ServiceBrokerPortNumber = value; OnPropertyChanged(); }
        //}
        public string UserName
        {
            get { return MyServer.UserName; }
            set { MyServer.UserName = value; OnPropertyChanged(); }
        }
        public string Password
        {
            get { return MyServer.Password; }
            set { MyServer.Password = value; OnPropertyChanged(); }
        }
        private void Confirm(object parameter)
        {
            OnConfirm?.Invoke(MyServer);
        }
        private void Cancel(object parameter)
        {
            OnCancel?.Invoke();
        }
    }
}
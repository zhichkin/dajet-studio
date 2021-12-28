using DaJet.Studio.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DaJet.Studio
{
    public sealed class WebServerListViewModel : ViewModelBase
    {
        public WebServerListViewModel(List<WebServer> servers)
        {
            WebServers = new ObservableCollection<WebServer>();
            foreach (WebServer server in servers)
            {
                WebServers.Add(server);
            }
            CancelCommand = new RelayCommand(Cancel);
            ConfirmCommand = new RelayCommand(Confirm);
        }
        public ObservableCollection<WebServer> WebServers { get; private set; }
        public Action OnCancel { get; set; }
        public Action<WebServer> OnConfirm { get; set; }
        public WebServer SelectedItem { get; set; }
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
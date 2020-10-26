using DaJet.Studio.MVVM;
using System;
using System.Windows.Input;

namespace DaJet.Studio
{
    public sealed class WebServerFormViewModel : ViewModelBase
    {
        private WebServer Model { get; } = new WebServer();
        public WebServerFormViewModel()
        {
            ConfirmCommand = new RelayCommand(Confirm);
            CancelCommand = new RelayCommand(Cancel);
        }
        public WebServerFormViewModel(WebServer model) : this()
        {
            Model = model;
        }
        public ICommand ConfirmCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public Action OnCancel { get; set; }
        public Action<WebServer> OnConfirm { get; set; }
        public string Name
        {
            get { return Model.Name; }
            set { Model.Name = value; OnPropertyChanged(); }
        }
        public string Address
        {
            get { return Model.Address; }
            set { Model.Address = value; OnPropertyChanged(); }
        }
        private void Confirm(object parameter)
        {
            OnConfirm?.Invoke(Model);
        }
        private void Cancel(object parameter)
        {
            OnCancel?.Invoke();
        }
    }
}
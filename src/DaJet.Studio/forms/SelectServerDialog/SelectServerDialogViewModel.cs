using DaJet.Studio.MVVM;
using System;
using System.Windows.Input;

namespace DaJet.UI
{
    public sealed class SelectServerDialogViewModel : ViewModelBase
    {
        public SelectServerDialogViewModel()
        {
            ConfirmCommand = new RelayCommand(Confirm);
            CancelCommand = new RelayCommand(Cancel);
        }
        public ICommand ConfirmCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public Action OnCancel { get; set; }
        public Action<string> OnConfirm { get; set; }
        public string ServerAddress { get; set; }
        private void Confirm(object parameter)
        {
            OnConfirm?.Invoke(ServerAddress);
        }
        private void Cancel(object parameter)
        {
            OnCancel?.Invoke();
        }
    }
}
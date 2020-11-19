using DaJet.Messaging;
using DaJet.Studio.MVVM;
using System;
using System.Windows.Input;

namespace DaJet.Studio
{
    public sealed class QueueFormViewModel : ViewModelBase
    {
        private QueueInfo Model { get; } = new QueueInfo();
        public QueueFormViewModel()
        {
            ConfirmCommand = new RelayCommand(Confirm);
            CancelCommand = new RelayCommand(Cancel);
        }
        public QueueFormViewModel(QueueInfo model) : this()
        {
            Model = model;
        }
        public ICommand ConfirmCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public Action OnCancel { get; set; }
        public Action<QueueInfo> OnConfirm { get; set; }
        public string Name
        {
            get { return Model.Name; }
            set { Model.Name = value; OnPropertyChanged(); }
        }
        public bool Status
        {
            get { return Model.Status; }
            set { Model.Status = value; OnPropertyChanged(); }
        }
        public bool Activation
        {
            get { return Model.Activation; }
            set { Model.Activation = value; OnPropertyChanged(); }
        }
        public string ProcedureName
        {
            get { return Model.ProcedureName; }
            set { Model.ProcedureName = value; OnPropertyChanged(); }
        }
        public short MaxQueueReaders
        {
            get { return Model.MaxQueueReaders; }
            set { Model.MaxQueueReaders = value; OnPropertyChanged(); }
        }
        public bool PoisonMessageHandling
        {
            get { return Model.PoisonMessageHandling; }
            set { Model.PoisonMessageHandling = value; OnPropertyChanged(); }
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
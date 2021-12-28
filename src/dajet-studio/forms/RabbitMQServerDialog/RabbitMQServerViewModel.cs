using DaJet.Studio.MVVM;
using DaJet.UI.Model;

namespace DaJet.Studio.UI
{
    public sealed class RabbitMQServerViewModel : ViewModelBase
    {
        public RabbitMQServerViewModel(RabbitMQServer model)
        {
            Model = model;
        }
        public RabbitMQServer Model { get; }

        public string Host
        {
            get { return Model.Host; }
            set { Model.Host = value; OnPropertyChanged(); }
        }
        public int Port
        {
            get { return Model.Port; }
            set { Model.Port = value; OnPropertyChanged(); }
        }
        public string Description
        {
            get { return Model.Description; }
            set { Model.Description = value; OnPropertyChanged(); }
        }
        public string UserName
        {
            get { return Model.UserName; }
            set { Model.UserName = value; OnPropertyChanged(); }
        }
        public string Password
        {
            get { return Model.Password; }
            set { Model.Password = value; OnPropertyChanged(); }
        }
    }
}
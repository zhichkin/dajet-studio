using DaJet.RabbitMQ.HttpApi;
using DaJet.Studio.MVVM;
using DaJet.UI.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DaJet.Studio.UI
{
    public sealed class ExchangeViewModel : ViewModelBase
    {
        private ExchangeInfo Model { get; }
        public ExchangeViewModel(ExchangeInfo model)
        {
            Model = model;
        }
        private bool _IsMarked;
        public bool IsMarked
        {
            get { return _IsMarked; }
            set { _IsMarked = value; OnPropertyChanged(nameof(IsMarked)); }
        }
        public string Name
        {
            get { return Model.Name; }
        }
    }
    public sealed class RabbitMQExchangeListViewModel : ViewModelBase, IErrorHandler
    {
        private RabbitMQServer Server { get; set; }
        private VirtualHostInfo VHost { get; set; }
        private IRabbitMQHttpManager Manager { get; }
        public RabbitMQExchangeListViewModel(IRabbitMQHttpManager manager)
        {
            Manager = manager;
            ClearListFilterCommand = new AsyncRelayCommand(ClearListFilterCommandHandler, this);
            OpenExchangeListCommand = new AsyncRelayCommand(OpenExchangeListCommandHandler, this);
        }

        private string _ErrorMessage = string.Empty;
        public string ErrorMessage
        {
            get { return _ErrorMessage; }
            set { _ErrorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }
        public void HandleError(Exception error)
        {
            ErrorMessage = ExceptionHelper.GetErrorTextAndStackTrace(error);
        }
        
        public void Initialize(RabbitMQServer server, VirtualHostInfo vhost)
        {
            VHost = vhost;
            Server = server;
            Manager.UseHostName(Server.Host)
                .UsePortNumber(Server.Port)
                .UseUserName(Server.UserName)
                .UsePassword(Server.Password)
                .UseVirtualHost(VHost.Name);

            HostName = Server.ToString() + " (" + VHost.Name + ")";
            HostDescription = VHost.Description;

            OpenExchangeListCommand.Execute(null);
        }
        public string HostName { get; private set; }
        public string HostDescription { get; private set; }
        private string _ListFilter = string.Empty;
        public string ListFilter
        {
            get { return _ListFilter; }
            set { _ListFilter = value; OnPropertyChanged(nameof(ListFilter)); }
        }
        public IAsyncCommand ClearListFilterCommand { get; private set; }
        public IAsyncCommand OpenExchangeListCommand { get; private set; }
        public ObservableCollection<ExchangeViewModel> ExchangeList { get; private set; } = new ObservableCollection<ExchangeViewModel>();
        private HashSet<string> SystemExchanges = new HashSet<string>()
        {
            "amq.topic",
            "amq.match",
            "amq.direct",
            "amq.fanout",
            "amq.headers",
            "amq.rabbitmq.trace"
        };
        private async Task ClearListFilterCommandHandler()
        {
            ListFilter = string.Empty;
            await OpenExchangeListCommandHandler();
        }
        private async Task OpenExchangeListCommandHandler()
        {
            ExchangeList.Clear();

            List<ExchangeInfo> list;

            if (string.IsNullOrWhiteSpace(ListFilter))
            {
                list = await Manager.GetExchanges();
            }
            else
            {
                ExchangeResponse response = await Manager.GetExchanges(1, 1000, ListFilter);
                list = response.Items;
            }

            foreach (ExchangeInfo exchange in list)
            {
                if (string.IsNullOrEmpty(exchange.Name) ||
                    SystemExchanges.Contains(exchange.Name))
                {
                    continue;
                }

                ExchangeList.Add(new ExchangeViewModel(exchange));
            }
        }
    }
}
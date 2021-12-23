using DaJet.RabbitMQ.HttpApi;
using DaJet.Studio.MVVM;
using DaJet.UI.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

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
            DeleteExchangeListCommand = new AsyncRelayCommand(DeleteExchangeListCommandHandler, this);
        }

        private string _DisplayMessage = string.Empty;
        public string DisplayMessage
        {
            get { return _DisplayMessage; }
            set { _DisplayMessage = value; OnPropertyChanged(nameof(DisplayMessage)); }
        }
        public void HandleError(Exception error)
        {
            DisplayMessage = ExceptionHelper.GetErrorTextAndStackTrace(error);
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
        private string _FilterResult = string.Empty;
        public string ListFilter
        {
            get { return _ListFilter; }
            set { _ListFilter = value; OnPropertyChanged(nameof(ListFilter)); }
        }
        public string FilterResult
        {
            get { return _FilterResult; }
            set { _FilterResult = value; OnPropertyChanged(nameof(FilterResult)); }
        }
        public IAsyncCommand ClearListFilterCommand { get; private set; }
        public IAsyncCommand OpenExchangeListCommand { get; private set; }
        public IAsyncCommand DeleteExchangeListCommand { get; private set; }
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
                ExchangeResponse response = await Manager.GetExchanges(1, 5000, ListFilter);
                list = response.Items;
            }

            int count = 0;
            foreach (ExchangeInfo exchange in list)
            {
                if (string.IsNullOrEmpty(exchange.Name) ||
                    SystemExchanges.Contains(exchange.Name))
                {
                    continue;
                }

                count++;
                ExchangeList.Add(new ExchangeViewModel(exchange));
            }

            if (count == 0)
            {
                FilterResult = "Found 0 exchanges";
            }
            else
            {
                FilterResult = $"Found {count} exchanges";
            }
        }
        private async Task DeleteExchangeListCommandHandler()
        {
            // ^РИБ.[0-9]+.ЦБ$
            // ^РИБ.ЦБ.[0-9]+$

            MessageBoxResult answer = MessageBox.Show(
                "Удалить выбранные точки доступа на сервере RabbitMQ ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (answer != MessageBoxResult.OK)
            {
                return;
            }

            int count = 0;
            foreach (ExchangeViewModel item in ExchangeList)
            {
                count++;
                await Manager.DeleteExchange(item.Name);
                DisplayMessage = "Deleting selected exchanges:" + Environment.NewLine + item.Name;
            }
            DisplayMessage = "Deleting selected exchanges:" + Environment.NewLine + $"{count} exchanges have been deleted.";

            await OpenExchangeListCommandHandler();
        }
    }
}
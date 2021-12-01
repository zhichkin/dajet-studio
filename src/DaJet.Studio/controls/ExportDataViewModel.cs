using DaJet.Data.Mapping;
using DaJet.Json;
using DaJet.Metadata.Model;
using DaJet.RabbitMQ;
using DaJet.Studio.MVVM;
using System;
using System.Text;
using System.Windows.Input;

namespace DaJet.Studio.UI
{
    public sealed class ExportDataViewModel : ViewModelBase
    {
        private IServiceProvider Services { get; }
        private string _TargetConnectionString = "amqp://guest:guest@localhost:5672/%2F/{ExchangeName}";
        private string _Sender = string.Empty;
        private string _RoutingKey = string.Empty;
        private string _MessageType = string.Empty;
        private string _TotalRowCount = string.Empty;
        private string _PageSize = string.Empty;
        private string _PageNumber = string.Empty;
        private string _ResultText = string.Empty;

        private EntityDataMapper DataMapper { get; set; }
        private EntityJsonSerializer Serializer { get; set; }

        public ExportDataViewModel(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
            ExportDataCommand = new RelayCommand(ExportDataCommandHandler);
            ShowTotalRowCountCommand = new RelayCommand(ShowTotalRowCountCommandCommandHandler);
        }
        public InfoBase InfoBase { get; set; }
        public ApplicationObject MetaObject { get; set; }
        public string ConnectionString { get; set; }
        public ICommand ExportDataCommand { get; private set; }
        public ICommand ShowTotalRowCountCommand { get; private set; }
        public string TargetConnectionString
        {
            get { return _TargetConnectionString; }
            set { _TargetConnectionString = value; OnPropertyChanged(); }
        }
        public string Sender
        {
            get { return _Sender; }
            set { _Sender = value; OnPropertyChanged(); }
        }
        public string RoutingKey
        {
            get { return _RoutingKey; }
            set { _RoutingKey = value; OnPropertyChanged(); }
        }
        public string MessageType
        {
            get { return _MessageType; }
            set { _MessageType = value; OnPropertyChanged(); }
        }
        public string TotalRowCount
        {
            get { return _TotalRowCount; }
            set { _TotalRowCount = value; OnPropertyChanged(); }
        }
        public string PageSize
        {
            get { return _PageSize; }
            set { _PageSize = value; OnPropertyChanged(); }
        }
        public string PageNumber
        {
            get { return _PageNumber; }
            set { _PageNumber = value; OnPropertyChanged(); }
        }
        public string ResultText
        {
            get { return _ResultText; }
            set { _ResultText = value; OnPropertyChanged(); }
        }

        private void InitializeServices()
        {
            if (DataMapper != null)
            {
                return;
            }

            DataMapper = new EntityDataMapper()
                .Configure(new DataMapperOptions()
                {
                    InfoBase = InfoBase,
                    MetaObject = MetaObject,
                    ConnectionString = ConnectionString
                });
            Serializer = new EntityJsonSerializer(DataMapper);
        }

        private void ShowTotalRowCountCommandCommandHandler(object parameter)
        {
            InitializeServices();

            TotalRowCount = DataMapper.GetTotalRowCount().ToString();
        }
        private void ExportDataCommandHandler(object parameter)
        {
            InitializeServices();

            using (RabbitMQProducer producer = new RabbitMQProducer(TargetConnectionString, RoutingKey))
            {
                producer.Initialize();
                producer.AppId = Sender;
                producer.MessageType = MessageType;

                ResultText = new StringBuilder()
                    .AppendLine($"AppId: {producer.AppId}")
                    .AppendLine($"Host: {producer.HostName}")
                    .AppendLine($"Port: {producer.HostPort}")
                    .AppendLine($"VHost: {producer.VirtualHost}")
                    .AppendLine($"User: {producer.UserName}")
                    .AppendLine($"Pass: {producer.Password}")
                    .AppendLine($"Exchange: {producer.ExchangeName}")
                    .AppendLine($"RoutingKey: {producer.RoutingKey}")
                    .AppendLine($"MessageType: {producer.MessageType}")
                    .ToString();

                int pageSize = int.Parse(PageSize);
                int pageNumber = int.Parse(PageNumber);

                int messagesSent = producer.Publish(Serializer, pageSize, pageNumber);

                ResultText += Environment.NewLine + $"Page [{pageNumber}] = {messagesSent} sent";
                ResultText += Environment.NewLine + $"Totally {messagesSent} messages sent";
            }
        }
    }
}
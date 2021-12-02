using DaJet.Data.Mapping;
using DaJet.Json;
using DaJet.Metadata.Model;
using DaJet.RabbitMQ;
using DaJet.Studio.MVVM;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DaJet.Studio.UI
{
    public sealed class ExportDataRabbitMQViewModel : ViewModelBase, IErrorHandler
    {
        private IServiceProvider Services { get; }

        private InfoBase _InfoBase;
        private ApplicationObject _MetaObject;
        private string _InfoBasePresentation = string.Empty;
        private string _MetaObjectPresentation = string.Empty;
        private string _TargetConnectionString = "amqp://guest:guest@localhost:5672/%2F/{ExchangeName}";
        private string _Sender = string.Empty;
        private string _RoutingKey = string.Empty;
        private string _MessageType = string.Empty;
        private string _TotalRowCount = string.Empty;
        private string _PageSize = string.Empty;
        private string _PageNumber = string.Empty;
        private string _ResultText = string.Empty;
        private bool _CanExecuteExportCommand = true;

        private EntityDataMapper DataMapper { get; set; }
        private EntityJsonSerializer Serializer { get; set; }

        public ExportDataRabbitMQViewModel(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
            ExportDataDelegate = new Action(ExportData);
            ExportDataCommand = new AsyncRelayCommand(ExportDataCommandHandler, this);
            ShowTotalRowCountCommand = new RelayCommand(ShowTotalRowCountCommandHandler);
        }
        public void HandleError(Exception error)
        {
            ResultText = ExceptionHelper.GetErrorText(error);
        }
        public InfoBase InfoBase
        {
            get { return _InfoBase; }
            set
            {
                _InfoBase = value;
                _InfoBasePresentation = $"{_InfoBase.Name} ({_InfoBase.ConfigInfo.ConfigVersion})";
            }
        }
        public ApplicationObject MetaObject
        {
            get { return _MetaObject; }
            set
            {
                _MetaObject = value;
                _RoutingKey = GetMetaObjectFullName(_MetaObject);
                _MessageType = _RoutingKey;
                _MetaObjectPresentation = _RoutingKey;
            }
        }
        public string SourceConnectionString { get; set; }
        private Action ExportDataDelegate { get; set; }
        public ICommand ExportDataCommand { get; private set; }
        public ICommand ShowTotalRowCountCommand { get; private set; }
        public string InfoBasePresentation
        {
            get { return _InfoBasePresentation; }
        }
        public string MetaObjectPresentation
        {
            get { return _MetaObjectPresentation; }
        }
        public string TargetConnectionString
        {
            get { return _TargetConnectionString; }
            set { _TargetConnectionString = value; OnPropertyChanged(nameof(TargetConnectionString)); }
        }
        public string Sender
        {
            get { return _Sender; }
            set { _Sender = value; OnPropertyChanged(nameof(Sender)); }
        }
        public string RoutingKey
        {
            get { return _RoutingKey; }
            set { _RoutingKey = value; OnPropertyChanged(nameof(RoutingKey)); }
        }
        public string MessageType
        {
            get { return _MessageType; }
            set { _MessageType = value; OnPropertyChanged(nameof(MessageType)); }
        }
        public string TotalRowCount
        {
            get { return _TotalRowCount; }
            set { _TotalRowCount = value; OnPropertyChanged(nameof(TotalRowCount)); }
        }
        public string PageSize
        {
            get { return _PageSize; }
            set { _PageSize = value; OnPropertyChanged(nameof(PageSize)); }
        }
        public string PageNumber
        {
            get { return _PageNumber; }
            set { _PageNumber = value; OnPropertyChanged(nameof(PageNumber)); }
        }
        public string ResultText
        {
            get { return _ResultText; }
            set { _ResultText = value; OnPropertyChanged(nameof(ResultText)); }
        }
        public bool IsBusy
        {
            get { return !_CanExecuteExportCommand; }
        }
        public bool CanExecuteExportCommand
        {
            get { return _CanExecuteExportCommand; }
            private set
            {
                _CanExecuteExportCommand = value;
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(CanExecuteExportCommand));
            }
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
                    ConnectionString = SourceConnectionString
                });
            Serializer = new EntityJsonSerializer(DataMapper);
        }
        private string GetMetaObjectFullName(ApplicationObject metaObject)
        {
            string type = string.Empty;

            if (metaObject == null) { return string.Empty; }
            else if (metaObject is Catalog) { type = "Справочник"; }
            else if (metaObject is Document) { type = "Документ"; }
            else if (metaObject is Enumeration) { type = "Перечисление"; }
            else if (metaObject is Characteristic) { type = "ПланВидовХарактеристик"; }
            else if (metaObject is InformationRegister) { type = "РегистрСведений"; }
            else if (metaObject is AccumulationRegister) { type = "РегистрНакопления"; }
            else return string.Empty;

            return $"{type}.{metaObject.Name}";
        }
        private void ShowTotalRowCountCommandHandler(object parameter)
        {
            try
            {
                InitializeServices();
                TotalRowCount = DataMapper.GetTotalRowCount().ToString();
            }
            catch (Exception error)
            {
                HandleError(error);
            }
        }
        private static void ParsePageNumber(string pageNumber, out int firstPage, out int lastPage)
        {
            if (pageNumber.Contains('-'))
            {
                string[] pages = pageNumber.Split('-', StringSplitOptions.RemoveEmptyEntries);
                lastPage = int.Parse(pages[1]);
                firstPage = int.Parse(pages[0]);
            }
            else
            {
                lastPage = 0;
                firstPage = int.Parse(pageNumber);
            }
        }
        private async Task ExportDataCommandHandler()
        {
            MessageBoxResult result = MessageBox.Show("Export data \"" + MetaObject.Name + "\" to RabbitMQ ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            try
            {
                CanExecuteExportCommand = false;
                InitializeServices();
                await Task.Run(ExportDataDelegate);
            }
            finally
            {
                CanExecuteExportCommand = true;
            }
        }
        private void ExportData()
        {
            int pageSize = int.Parse(PageSize);

            ParsePageNumber(PageNumber, out int firstPage, out int lastPage);
            
            int pageNumber = firstPage;
            if (lastPage == 0)
            {
                lastPage = pageNumber;
            }

            int totalCount = 0;

            using (RabbitMQProducer producer = new RabbitMQProducer(TargetConnectionString, RoutingKey))
            {
                producer.Initialize();
                producer.AppId = Sender;
                producer.MessageType = MessageType;

                for (; pageNumber <= lastPage; pageNumber++)
                {
                    int messagesSent = producer.Publish(Serializer, pageSize, pageNumber);

                    totalCount += messagesSent;

                    ResultText = $"Page [{pageNumber}] = {totalCount} messages sent";
                }
            }

            ResultText = $"Totally {totalCount} messages sent";
        }
    }
}
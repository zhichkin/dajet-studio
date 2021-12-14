using DaJet.Data;
using DaJet.Data.Mapping;
using DaJet.Json;
using DaJet.Metadata.Model;
using DaJet.RabbitMQ;
using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DaJet.Studio.UI
{
    public sealed class ExportDataRabbitMQViewModel : ViewModelBase, IErrorHandler, IListViewModelController
    {
        #region "PRIVATE FIELDS"

        private InfoBase _InfoBase;
        private ApplicationObject _MetaObject;
        private string _InfoBasePresentation = string.Empty;
        private string _MetaObjectPresentation = string.Empty;
        private string _TargetConnectionString = "amqp://guest:guest@localhost:5672/%2F/{ExchangeName}";
        private string _Sender = string.Empty;
        private string _RoutingKey = string.Empty;
        private string _MessageType = string.Empty;
        private string _TotalRowCount = string.Empty;
        private int _PageSize = 1000;
        private string _PageNumber = "1-100";
        private string _ResultText = string.Empty;
        private bool _CanExecuteExportCommand = true;
        private string _exportJsonFilePath = string.Empty;

        #endregion

        private IServiceProvider Services { get; }

        public IDaJetDataMapper DataMapper { get; set; }
        public IDaJetJsonSerializer JsonSerializer { get; set; }
        public ExportDataRabbitMQViewModel(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;

            DataExportTaskLog = new DataExportTaskListViewModel(this);

            SelectIndexCommand = new RelayCommand(SelectIndexCommandHandler);
            ClearIndexCommand = new RelayCommand(ClearIndexCommandHandler);
            ExportDataDelegate = new Func<int>(ExportData);
            ExportDataCommand = new AsyncRelayCommand(ExportDataCommandHandler, this);
            ExportDataToFileDelegate = new Action(ExportDataToFile);
            ExportDataToFileCommand = new AsyncRelayCommand(ExportDataToFileCommandHandler, this);
            CancelExportDataCommand = new RelayCommand(CancelExportDataCommandHandler);
            ShowTotalRowCountCommand = new RelayCommand(ShowTotalRowCountCommandHandler);
        }
        
        #region "PROPERTIES AND COMMANDS"

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
        private Func<int> ExportDataDelegate { get; set; }
        public ICommand ExportDataCommand { get; private set; }
        private Action ExportDataToFileDelegate { get; set; }
        public ICommand ExportDataToFileCommand { get; private set; }
        public ICommand CancelExportDataCommand { get; private set; }
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
        public int PageSize
        {
            get { return _PageSize; }
            set
            {
                if (value == 0)
                {
                    _PageSize = 1000;
                }
                else
                {
                    _PageSize = value > 10000 ? 10000 : value;
                }
                OnPropertyChanged(nameof(PageSize));
            }
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

        #endregion

        #region "TABLE INDEX"

        public IndexInfo TableIndex { get; set; }
        public string TableIndexName { get; set; }
        public ICommand SelectIndexCommand { get; private set; }
        public ICommand ClearIndexCommand { get; private set; }
        private void SelectIndexCommandHandler(object parameter)
        {
            try
            {
                List<IndexInfo> indexes = SQLHelper.GetIndexes(SourceConnectionString, MetaObject.TableName);
                SelectIndexDialog dialog = new SelectIndexDialog(MetaObject, indexes);
                _ = dialog.ShowDialog();
                if (dialog.Result != null)
                {
                    TableIndex = dialog.Result;
                    TableIndexName = TableIndex.Name;
                    OnPropertyChanged(nameof(TableIndexName));
                    ConfigureFilterTable();
                    OnPropertyChanged(nameof(IsFilterTableVisible));
                }
            }
            catch (Exception error)
            {
                HandleError(error);
            }
        }
        private void ClearIndexCommandHandler(object parameter)
        {
            TableIndex = null;
            TableIndexName = null;
            OnPropertyChanged(nameof(TableIndexName));
            ConfigureFilterTable();
            OnPropertyChanged(nameof(IsFilterTableVisible));
        }
        public bool IsFilterTableVisible
        {
            get { return !string.IsNullOrEmpty(TableIndexName); }
        }

        #endregion

        private void InitializeServices()
        {
            if (DataMapper != null)
            {
                return;
            }

            if (MetaObject is Catalog || MetaObject is Document)
            {
                DataMapper = new EntityDataMapper();
            }
            else if (MetaObject is InformationRegister || MetaObject is AccumulationRegister)
            {
                DataMapper = new RegisterDataMapper();
            }
            else
            {
                throw new InvalidOperationException($"Metadata type \"{_MessageType}\" is not supported.");
            }

            DataMapper.Configure(new DataMapperOptions()
            {
                InfoBase = InfoBase,
                MetaObject = MetaObject,
                ConnectionString = SourceConnectionString
            });

            if (MetaObject is Catalog || MetaObject is Document)
            {
                JsonSerializer = new EntityJsonSerializer((EntityDataMapper)DataMapper);
            }
            else
            {
                JsonSerializer = new RegisterJsonSerializer((RegisterDataMapper)DataMapper);
            }
        }
        private void ConfigureDataMapper()
        {
            DataMapper.Options.Index = TableIndex;
            DataMapper.Options.Filter = null;
            if (TableIndex != null)
            {
                List<FilterParameter> filter = new List<FilterParameter>();
                foreach (FilterParameterViewModel parameter in FilterParameters)
                {
                    if (parameter.UseMe && parameter.Value != null)
                    {
                        filter.Add(new FilterParameter()
                        {
                            Path = parameter.Name,
                            Operator = parameter.FilterOperator,
                            Value = parameter.Value
                        });
                    }
                }
                if (filter.Count > 0)
                {
                    DataMapper.Options.Filter = filter;
                }
            }
            DataMapper.Reconfigure();
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
                ConfigureDataMapper();

                int pageSize = PageSize;

                int totalRowCount = DataMapper.GetTotalRowCount();

                int numberOfPages = totalRowCount / pageSize;
                if (totalRowCount % pageSize > 0)
                {
                    numberOfPages++;
                }

                int firstPage = 1;
                int lastPage = numberOfPages;

                int firstPageSize = pageSize < totalRowCount ? pageSize : totalRowCount;
                int lastPageSize = pageSize > totalRowCount ? totalRowCount : pageSize - (numberOfPages * pageSize - totalRowCount);

                long firstPageTiming = 0L;
                long lastPageTiming = 0L;
                if (totalRowCount > 0)
                {
                    firstPageTiming = DataMapper.TestGetPageDataRows(pageSize, firstPage);
                    lastPageTiming = DataMapper.TestGetPageDataRows(pageSize, lastPage);
                }

                NumberFormatInfo format = new NumberFormatInfo();
                format.NumberDecimalDigits = 0;
                format.NumberGroupSeparator = " ";

                TotalRowCount = $"{totalRowCount.ToString("N", format)} rows on [{numberOfPages.ToString("N", format)}] pages"
                    + Environment.NewLine
                    + $"Size: [1] = {firstPageSize.ToString("N", format)} rows, [{lastPage}] = {lastPageSize.ToString("N", format)} rows"
                    + Environment.NewLine
                    + $"Time: [1] = {firstPageTiming.ToString("N", format)} ms, [{lastPage}] = {lastPageTiming.ToString("N", format)} ms";
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

        #region "EXPORT DATA TO RabbitMQ"

        private CancellationTokenSource ExportDataCancellation;
        private async Task ExportDataCommandHandler()
        {
            MessageBoxResult result = MessageBox.Show("Export data \"" + MetaObject.Name + "\" to RabbitMQ ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            try
            {
                int messagesSent = 0;
                bool exportCanceled = false;
                CanExecuteExportCommand = false;

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                InitializeServices();
                ConfigureDataMapper();
                using (ExportDataCancellation = new CancellationTokenSource())
                {
                    messagesSent = await Task.Run(ExportDataDelegate, ExportDataCancellation.Token);
                    exportCanceled = ExportDataCancellation.IsCancellationRequested;
                }

                stopwatch.Stop();

                LogDataExportResult(messagesSent, stopwatch.ElapsedMilliseconds, exportCanceled);
            }
            finally
            {
                CanExecuteExportCommand = true;
            }
        }
        private async Task ExportDataToFileCommandHandler()
        {
            MessageBoxResult result = MessageBox.Show("Export data \"" + MetaObject.Name + "\" to file ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (result != MessageBoxResult.OK)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_exportJsonFilePath))
            {
                IFileProvider fileProvider = Services.GetService<IFileProvider>();
                if (fileProvider is PhysicalFileProvider provider)
                {
                    _exportJsonFilePath = provider.Root;
                }
            }

            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "json files (*.json)|*.json",
                FileName = MetaObject.Name,
                InitialDirectory = Path.GetDirectoryName(_exportJsonFilePath)
            };

            if (!dialog.ShowDialog().Value)
            {
                return;
            }

            _exportJsonFilePath = dialog.FileName;

            try
            {
                CanExecuteExportCommand = false;
                InitializeServices();
                ConfigureDataMapper();
                using (ExportDataCancellation = new CancellationTokenSource())
                {
                    await Task.Run(ExportDataToFileDelegate, ExportDataCancellation.Token);
                }
            }
            finally
            {
                CanExecuteExportCommand = true;
            }
        }
        private void CancelExportDataCommandHandler(object parameter)
        {
            MessageBoxResult result = MessageBox.Show("Cancel export data \"" + MetaObject.Name + "\" to RabbitMQ ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            try
            {
                ExportDataCancellation?.Cancel();
            }
            catch { /* The CancellationTokenSource has been disposed */ }
        }
        private int ExportData()
        {
            int pageSize = PageSize;

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
                    if (ExportDataCancellation.IsCancellationRequested)
                    {
                        break;
                    }

                    int messagesSent = producer.Publish(JsonSerializer, pageSize, pageNumber);

                    totalCount += messagesSent;

                    ResultText = $"Page [{pageNumber}] = {totalCount} messages sent";
                }
            }

            return totalCount;
        }
        private void ExportDataToFile()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int pageSize = PageSize;

            ParsePageNumber(PageNumber, out int firstPage, out int lastPage);

            int pageNumber = firstPage;
            if (lastPage == 0)
            {
                lastPage = pageNumber;
            }

            int totalCount = 0;
            using (StreamWriter writer = new StreamWriter(_exportJsonFilePath, false, Encoding.UTF8))
            {
                writer.Write("[");

                for (; pageNumber <= lastPage; pageNumber++)
                {
                    if (ExportDataCancellation.IsCancellationRequested)
                    {
                        break;
                    }

                    int messagesSent = 0;
                    foreach (ReadOnlyMemory<byte> messageBody in JsonSerializer.Serialize(pageSize, pageNumber))
                    {
                        if (messagesSent > 0 || totalCount > 0)
                        {
                            writer.Write(",");
                        }
                        writer.Write(Encoding.UTF8.GetString(messageBody.Span));

                        messagesSent++;
                    }
                    totalCount += messagesSent;

                    ResultText = _exportJsonFilePath + Environment.NewLine + $"Page [{pageNumber}] = {totalCount} messages saved to file";
                }

                writer.Write("]");
            }

            stopwatch.Stop();

            NumberFormatInfo format = new NumberFormatInfo();
            format.NumberDecimalDigits = 0;
            format.NumberGroupSeparator = " ";

            ResultText = _exportJsonFilePath + Environment.NewLine;

            if (ExportDataCancellation.IsCancellationRequested)
            {
                ResultText += $"Operation canceled: totally {totalCount} messages saved to file";
            }
            else
            {
                ResultText += $"Operation completed: totally {totalCount} messages saved to file";
            }

            ResultText += Environment.NewLine + $"Time elapsed: {stopwatch.ElapsedMilliseconds.ToString("N", format)} ms";
        }

        #endregion

        public void HandleError(Exception error)
        {
            ResultText = ExceptionHelper.GetErrorText(error);
        }

        #region "FILTER PARAMETERS TABLE"

        public ObservableCollection<FilterParameterViewModel> FilterParameters { get; set; } = new ObservableCollection<FilterParameterViewModel>();
        public void ConfigureFilterTable()
        {
            FilterParameters.Clear();

            if (TableIndex == null)
            {
                return;
            }

            foreach (IndexColumnInfo column in TableIndex.Columns)
            {
                MetadataProperty property = GetPropertyByColumn(column);

                FilterParameterViewModel parameter = new FilterParameterViewModel(this)
                {
                    UseMe = false,
                    Name = property == null ? "Свойство не найдено" : property.Name,
                    FilterOperator = ComparisonOperator.Equal,
                    Value = property == null ? null : GetDefaultPropertyValue(property)
                };

                FilterParameters.Add(parameter);
            }
        }
        private MetadataProperty GetPropertyByColumn(IndexColumnInfo column)
        {
            foreach (MetadataProperty property in MetaObject.Properties)
            {
                foreach (DatabaseField field in property.Fields)
                {
                    if (field.Name == column.Name)
                    {
                        return property;
                    }
                }
            }
            return null;
        }
        private object GetDefaultPropertyValue(MetadataProperty property)
        {
            if (property.PropertyType.IsMultipleType) return null;
            else if (property.PropertyType.CanBeString) return string.Empty;
            else if (property.PropertyType.CanBeDateTime) return new DateTime(DateTime.Now.Year, 1, 1);
            return null;
        }

        #endregion

        #region "DATA EXPORT TASK LIST"

        public DataExportTaskListViewModel DataExportTaskLog { get; private set; }
        private void LogDataExportResult(int messagesSent, long timeElapsed, bool exportCanceled)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NumberDecimalDigits = 0;
            format.NumberGroupSeparator = " ";

            if (exportCanceled)
            {
                ResultText = $"Operation canceled: totally {messagesSent} messages sent";
            }
            else
            {
                ResultText = $"Operation completed: totally {messagesSent} messages sent";
            }
            ResultText += Environment.NewLine + $"Time elapsed: {timeElapsed.ToString("N", format)} ms";

            DataExportTaskViewModel entry = DataExportTaskLog.CreateNewEntry();
            entry.PageSize = PageSize;
            entry.PageNumber = PageNumber;
            entry.RoutingKey = RoutingKey;
            entry.Description = ResultText;
            entry.ExportResult = messagesSent;
            DataExportTaskLog.TaskLog.Add(entry);
        }

        #endregion

        #region "Interface IListViewModelController implementation"

        public void AddNewItem() { throw new NotImplementedException(); }
        public void EditItem(object item)
        {
            if (item is DataExportTaskViewModel task)
            {
                PageSize = task.PageSize;
                PageNumber = task.PageNumber;
                RoutingKey = task.RoutingKey;
                ResultText = string.Empty;
            }
        }
        public void CopyItem(object item)
        {
            if (!(item is FilterParameterViewModel model)) return;

            int index = FilterParameters.IndexOf(model);
            if (index == -1) return;

            FilterParameterViewModel parameter = new FilterParameterViewModel(this)
            {
                UseMe = model.UseMe,
                Name = model.Name,
                FilterOperator = model.FilterOperator,
                Value = model.Value
            };

            FilterParameters.Insert(index, parameter);
        }
        public void RemoveItem(object item)
        {
            if (!(item is FilterParameterViewModel model)) return;

            FilterParameters.Remove(model);
        }

        #endregion
    }
}
using DaJet.Metadata;
using DaJet.Scripting;
using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF = System.Windows.Controls;

namespace DaJet.Studio
{
    public sealed class ScriptEditorViewModel : ViewModelBase
    {
        private const string ROOT_CATALOG_NAME = "scripts";
        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        private IFileProvider FileProvider { get; }
        public ScriptEditorViewModel(IServiceProvider serviceProvider, IFileProvider fileProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
            FileProvider = fileProvider;
            InitializeViewModel();
        }
        private void InitializeViewModel()
        {
            SaveCommand = new RelayCommand(SaveCommandHandler);
            ExecuteCommand = new RelayCommand(ExecuteCommandHandler);
            TranslateCommand = new RelayCommand(TranslateCommandHandler);
        }
        private DatabaseServer MyServer { get; set; }
        private DatabaseInfo MyDatabase { get; set; }
        private void ConfigureServerAndDatabase()
        {
            if (MyServer != null) return;

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            if (mainWindow == null) return;

            object[] result = new object[] { null, null };
            bool found = mainWindow.GetServerAndDatabase(mainWindow.MainTreeRegion.TreeNodes, this, result);

            MyServer = result[0] as DatabaseServer;
            MyDatabase = result[1] as DatabaseInfo;
        }
        private string _name = string.Empty;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }
        private bool _isScriptChanged = false;
        public bool IsScriptChanged
        {
            get { return _isScriptChanged; }
            set { _isScriptChanged = value; OnPropertyChanged(); }
        }
        private string _scriptCode = string.Empty;
        public string ScriptCode
        {
            get { return _scriptCode; }
            set { _scriptCode = value; OnPropertyChanged(); }
        }
        
        public ICommand TranslateCommand { get; private set; }
        private void TranslateCommandHandler(object parameter)
        {
            ConfigureServerAndDatabase();

            IMetadataService metadata = Services.GetService<IMetadataService>();
            metadata.AttachDatabase(string.IsNullOrWhiteSpace(MyServer.Address) ? MyServer.Name : MyServer.Address, MyDatabase);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            string sql = scripting.PrepareScript(ScriptCode, out IList<ParseError> errors);
            string errorMessage = string.Empty;
            foreach (ParseError error in errors)
            {
                errorMessage += error.Message + Environment.NewLine;
            }

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();

            if (errors.Count > 0)
            {
                scriptEditor.Name = "Errors";
                scriptEditor.ScriptCode = errorMessage;
            }
            else
            {
                scriptEditor.Name = $"{Name} (SQL)";
                scriptEditor.ScriptCode = sql;
            }
            ScriptEditorView scriptView = new ScriptEditorView()
            {
                DataContext = scriptEditor
            };
            mainWindow.AddNewTab(scriptEditor.Name, scriptView);
        }

        public ICommand ExecuteCommand { get; private set; }
        private void ExecuteCommandHandler(object parameter)
        {
            ConfigureServerAndDatabase();

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();

            IMetadataService metadata = Services.GetService<IMetadataService>();
            metadata.AttachDatabase(string.IsNullOrWhiteSpace(MyServer.Address) ? MyServer.Name : MyServer.Address, MyDatabase);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            string sql = scripting.PrepareScript(ScriptCode, out IList<ParseError> errors);
            string errorMessage = string.Empty;
            foreach (ParseError error in errors)
            {
                errorMessage += error.Message + Environment.NewLine;
            }
            if (errors.Count > 0)
            {
                ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
                scriptEditor.Name = "Errors";
                scriptEditor.ScriptCode = errorMessage;
                ScriptEditorView scriptView = new ScriptEditorView()
                {
                    DataContext = scriptEditor
                };
                mainWindow.AddNewTab(scriptEditor.Name, scriptView);
                return;
            }

            string json = "[]";
            try
            {
                json = scripting.ExecuteScript(sql, out IList<ParseError> executeErrors);
                foreach (ParseError error in executeErrors)
                {
                    errorMessage += error.Message + Environment.NewLine;
                }
                if (executeErrors.Count > 0)
                {
                    ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
                    scriptEditor.Name = "Errors";
                    scriptEditor.ScriptCode = errorMessage;
                    ScriptEditorView scriptView = new ScriptEditorView()
                    {
                        DataContext = scriptEditor
                    };
                    mainWindow.AddNewTab(scriptEditor.Name, scriptView);
                    return;
                }
            }
            catch (Exception ex)
            {
                ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
                scriptEditor.Name = "Errors";
                scriptEditor.ScriptCode = ex.Message;
                ScriptEditorView scriptView = new ScriptEditorView()
                {
                    DataContext = scriptEditor
                };
                mainWindow.AddNewTab(scriptEditor.Name, scriptView);
                return;
            }
            JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new DynamicJsonConverter());
            dynamic data = JsonSerializer.Deserialize<dynamic>(json, serializerOptions);

            DataGrid dataView = CreateDynamicDataGrid(data);
            mainWindow.AddNewTab($"{Name} (result)", dataView);
        }

        public ICommand SaveCommand { get; private set; }
        private void SaveCommandHandler(object parameter)
        {
            ConfigureServerAndDatabase();

            // TODO
            //DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            //DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            IFileInfo serverCatalog = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{MyServer.Identity.ToString().ToLower()}");
            if (!serverCatalog.Exists) { Directory.CreateDirectory(serverCatalog.PhysicalPath); }
            
            IFileInfo databaseCatalog = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{MyServer.Identity.ToString().ToLower()}/{MyDatabase.Identity.ToString().ToLower()}");
            if (!databaseCatalog.Exists) { Directory.CreateDirectory(databaseCatalog.PhysicalPath); }
            
            IFileInfo file = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{MyServer.Identity.ToString().ToLower()}/{MyDatabase.Identity.ToString().ToLower()}/{Name}");
            
            using (StreamWriter writer = new StreamWriter(file.PhysicalPath))
            {
                writer.Write(ScriptCode);
            }

            IsScriptChanged = false;
        }



        private Grid CreateDynamicGrid(dynamic data)
        {
            Grid grid = new Grid()
            {
                ShowGridLines = true,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            if (data is IList list)
            {
                RowDefinition rowDef;
                rowDef = new RowDefinition()
                {
                    Height = new GridLength()
                };
                grid.RowDefinitions.Add(rowDef); // headers row

                for (int i = 0; i < list.Count; i++)
                {
                    rowDef = new RowDefinition()
                    {
                        Height = new GridLength()
                    };
                    grid.RowDefinitions.Add(rowDef);
                }

                if (list.Count > 0)
                {
                    ExpandoObject item = list[0] as ExpandoObject;
                    int ii = 0;
                    foreach (var column in item)
                    {
                        WPF.ColumnDefinition colDef = new WPF.ColumnDefinition()
                        {
                            Width = new GridLength(1, GridUnitType.Auto)
                        };
                        grid.ColumnDefinitions.Add(colDef);

                        TextBlock block = new TextBlock()
                        {
                            Text = column.Key,
                            FontSize = 14,
                            FontWeight = FontWeights.Bold,
                            VerticalAlignment = VerticalAlignment.Top
                        };
                        Grid.SetRow(block, 0);
                        Grid.SetColumn(block, ii);
                        grid.Children.Add(block);
                        ii++;
                    }
                }
                int r = 0;
                int c = 0;
                foreach (ExpandoObject obj in list)
                {
                    r++;
                    foreach (var item in obj)
                    {
                        TextBlock block = new TextBlock()
                        {
                            Text = item.Value == null ? string.Empty : item.Value.ToString(),
                            FontSize = 14,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetRow(block, r);
                        Grid.SetColumn(block, c);
                        grid.Children.Add(block);
                        c++;
                    }
                }
            }

            return grid;
        }
        private DataGrid CreateDynamicDataGrid(dynamic data)
        {
            List<Dictionary<string, object>> source = new List<Dictionary<string, object>>();
            if (data is IEnumerable list)
            {
                foreach (ExpandoObject item in list)
                {
                    Dictionary<string, object> row = new Dictionary<string, object>();
                    foreach (var value in item)
                    {
                        row.Add(value.Key.Replace('-', '_'), value.Value);
                    }
                    source.Add(row);
                }
            }
            DataGrid grid = new DataGrid()
            {
                ItemsSource = source.ToDataSource(),
                AutoGenerateColumns = true,
                CanUserResizeColumns = true
            };
            return grid;
        }
    }
}
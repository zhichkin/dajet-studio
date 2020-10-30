using DaJet.Metadata;
using DaJet.Scripting;
using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Input;

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
        public DatabaseServer MyServer { get; set; }
        public DatabaseInfo MyDatabase { get; set; }
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

            DataGrid dataView = DynamicGridCreator.CreateDynamicDataGrid(data);
            mainWindow.AddNewTab($"{Name} (result)", dataView);
        }
        public ICommand SaveCommand { get; private set; }
        private void SaveCommandHandler(object parameter)
        {
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
    }
}
using DaJet.Metadata;
using DaJet.Scripting;
using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace DaJet.Studio
{
    public sealed class ScriptEditorViewModel : ViewModelBase
    {
        public const string NAME_PROPERTY_DEFAULT_VALUE = "New script";
        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        public ScriptEditorViewModel(IServiceProvider serviceProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
            InitializeViewModel();
        }
        private void InitializeViewModel()
        {

            TranslateCommand = new RelayCommand(TranslateCommandHandler);
        }
        private string _name = NAME_PROPERTY_DEFAULT_VALUE;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
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

            // TODO: get server and database names from main tree parent nodes
            DatabaseServer server = Settings.DatabaseServers.Where(s => s.Name == "zhichkin").FirstOrDefault();
            if (server == null) return;
            DatabaseInfo database = server.Databases.Where(d => d.Name == "mdlp_demo").FirstOrDefault();
            if (database == null) return;

            metadata.AttachDatabase(server.Name, database);

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
                scriptEditor.Name = "SQL";
                scriptEditor.ScriptCode = sql;
            }
            ScriptEditorView scriptView = new ScriptEditorView()
            {
                DataContext = scriptEditor
            };
            mainWindow.AddNewTab(scriptEditor.Name, scriptView);
        }
    }
}
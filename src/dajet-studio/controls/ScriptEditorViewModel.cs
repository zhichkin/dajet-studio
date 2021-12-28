using DaJet.Data.Scripting;
using DaJet.Studio.MVVM;
using DaJet.UI.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace DaJet.Studio
{
    public sealed class ScriptEditorViewModel : ViewModelBase
    {
        private IServiceProvider Services { get; }
        public ScriptEditorViewModel(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
            InitializeViewModel();
        }
        private void InitializeViewModel()
        {
            SaveCommand = new RelayCommand(SaveCommandHandler);
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
        public ScriptType ScriptType { get; set; } = ScriptType.Script;

        public ICommand SaveCommand { get; private set; }
        private void SaveCommandHandler(object parameter)
        {
            string errorMessage = string.Empty;

            try
            {
                SaveScriptSourceCode();
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                IsScriptChanged = false;
            }
            else
            {
                ShowException(errorMessage);
            }
        }
        private void SaveScriptSourceCode()
        {
            IScriptingService scripting = Services.GetService<IScriptingService>();
            _ = scripting.ParseScript(ScriptCode, out IList<ParseError> errors);

            if (errors.Count > 0)
            {
                ShowParseErrors(errors);
                throw new InvalidOperationException("Saving script failed: incorrect syntax.");
            }

            ScriptingController controller = Services.GetService<ScriptingController>();
            string catalogName = controller.GetScriptsCatalogName(MyServer, MyDatabase, ScriptType);
            if (controller.ScriptFileExists(catalogName, Name))
            {
                controller.SaveScriptFile(catalogName, Name, ScriptCode);
            }
            else
            {
                if (controller.GetScriptTreeNodeByName(MyServer, MyDatabase, ScriptType, Name) != null)
                {
                    throw new InvalidOperationException($"Script node \"{Name}\" already exists!");
                }
                controller.SaveScriptFile(catalogName, Name, ScriptCode);
            }
            
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            TreeNodeViewModel treeNode = mainWindow.GetTreeNodeByPayload(mainWindow.MainTreeRegion.TreeNodes, this);
            if (treeNode == null)
            {
                controller.CreateScriptTreeNode(this);
            }
        }

        private void ShowException(string errorMessage)
        {
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorViewModel errorsViewModel = Services.GetService<ScriptEditorViewModel>();
            errorsViewModel.Name = "Error";
            errorsViewModel.ScriptCode = errorMessage;
            ScriptEditorView errorsView = new ScriptEditorView() { DataContext = errorsViewModel };
            mainWindow.AddNewTab(errorsViewModel.Name, errorsView);
        }
        private void ShowException(Exception ex)
        {
            ShowException(ex.Message);
        }
        private void ShowParseErrors(IList<ParseError> errors)
        {
            string errorMessage = string.Empty;
            foreach (ParseError error in errors)
            {
                errorMessage += error.Message + Environment.NewLine;
            }

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorViewModel errorsViewModel = Services.GetService<ScriptEditorViewModel>();
            errorsViewModel.Name = "Errors";
            errorsViewModel.ScriptCode = errorMessage;
            ScriptEditorView errorsView = new ScriptEditorView() { DataContext = errorsViewModel };
            mainWindow.AddNewTab(errorsViewModel.Name, errorsView);
        }
    }
}
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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class ScriptingController : ITreeNodeController
    {
        #region "Icons and constants"

        private const string ROOT_NODE_NAME = "Scripts";
        private const string ROOT_CATALOG_NAME = "scripts";
        private const string SCRIPT_DEFAULT_NAME = "new_script.qry";
        private const string FUNCTIONS_NODE_NAME = "Functions";
        private const string TABLE_FUNCTION_NODE_NAME = "Table functions";
        private const string SCALAR_FUNCTION_NODE_NAME = "Scalar functions";
        private const string STORED_PROCEDURES_NODE_NAME = "Procedures";

        private const string TREE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/tree.png";
        private const string SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database-script.png";
        private const string NEW_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/new-script.png";
        private const string EDIT_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/edit-script.png";
        private const string DELETE_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/delete-script.png";
        private const string UPLOAD_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/upload-script.png";
        private const string EXECUTE_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/run.png";
        private const string SQL_CODE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/sql-query.png";
        private const string ADD_QUERY_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/add-query.png";
        private const string FUNCTION_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/function.png";
        private const string TABLE_FUNCTION_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/table-function.png";
        private const string SCALAR_FUNCTION_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/scalar-function.png";
        private const string STORED_PROCEDURE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/stored-procedure.png";

        private readonly BitmapImage TREE_ICON = new BitmapImage(new Uri(TREE_ICON_PATH));
        private readonly BitmapImage SCRIPT_ICON = new BitmapImage(new Uri(SCRIPT_ICON_PATH));
        private readonly BitmapImage NEW_SCRIPT_ICON = new BitmapImage(new Uri(NEW_SCRIPT_ICON_PATH));
        private readonly BitmapImage EDIT_SCRIPT_ICON = new BitmapImage(new Uri(EDIT_SCRIPT_ICON_PATH));
        private readonly BitmapImage DELETE_SCRIPT_ICON = new BitmapImage(new Uri(DELETE_SCRIPT_ICON_PATH));
        private readonly BitmapImage UPLOAD_SCRIPT_ICON = new BitmapImage(new Uri(UPLOAD_SCRIPT_ICON_PATH));
        private readonly BitmapImage EXECUTE_SCRIPT_ICON = new BitmapImage(new Uri(EXECUTE_SCRIPT_ICON_PATH));
        private readonly BitmapImage SQL_CODE_ICON = new BitmapImage(new Uri(SQL_CODE_ICON_PATH));
        private readonly BitmapImage ADD_QUERY_ICON = new BitmapImage(new Uri(ADD_QUERY_ICON_PATH));
        private readonly BitmapImage FUNCTION_ICON = new BitmapImage(new Uri(FUNCTION_ICON_PATH));
        private readonly BitmapImage TABLE_FUNCTION_ICON = new BitmapImage(new Uri(TABLE_FUNCTION_ICON_PATH));
        private readonly BitmapImage SCALAR_FUNCTION_ICON = new BitmapImage(new Uri(SCALAR_FUNCTION_ICON_PATH));
        private readonly BitmapImage STORED_PROCEDURE_ICON = new BitmapImage(new Uri(STORED_PROCEDURE_ICON_PATH));

        #endregion

        private MetadataSettings Settings { get; }
        private IServiceProvider Services { get; }
        private IFileProvider FileProvider { get; }
        public ScriptingController(IServiceProvider serviceProvider, IFileProvider fileProvider, IOptions<MetadataSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
            FileProvider = fileProvider;
        }



        private void CreateCatalogIfNotExists(string catalogName)
        {
            IFileInfo catalog = FileProvider.GetFileInfo(catalogName);
            if (!catalog.Exists) { Directory.CreateDirectory(catalog.PhysicalPath); }
        }
        public string GetDatabaseCatalog(DatabaseServer server, DatabaseInfo database)
        {
            string catalogName = $"{ROOT_CATALOG_NAME}";

            CreateCatalogIfNotExists(catalogName);

            catalogName += $"/{server.Identity.ToString().ToLower()}";
            
            CreateCatalogIfNotExists(catalogName);
            
            catalogName += $"/{database.Identity.ToString().ToLower()}";
            
            CreateCatalogIfNotExists(catalogName);

            return catalogName;
        }
        public string GetTableFunctionsCatalog(DatabaseServer server, DatabaseInfo database)
        {
            string catalogName = GetDatabaseCatalog(server, database) + "/table-functions";
            CreateCatalogIfNotExists(catalogName);
            return catalogName;
        }
        public string GetScalarFunctionsCatalog(DatabaseServer server, DatabaseInfo database)
        {
            string catalogName = GetDatabaseCatalog(server, database) + "/scalar-functions";
            CreateCatalogIfNotExists(catalogName);
            return catalogName;
        }
        public string GetStoredProceduresCatalog(DatabaseServer server, DatabaseInfo database)
        {
            string catalogName = GetDatabaseCatalog(server, database) + "/stored-procedures";
            CreateCatalogIfNotExists(catalogName);
            return catalogName;
        }
        public void SaveScriptFile(string catalogName, string fileName, string sourceCode)
        {
            IFileInfo file = FileProvider.GetFileInfo($"{catalogName}/{fileName}");
            using (StreamWriter writer = new StreamWriter(file.PhysicalPath, false, Encoding.UTF8))
            {
                writer.Write(sourceCode);
            }
        }



        public TreeNodeViewModel CreateTreeNode() { throw new NotImplementedException(); }
        public TreeNodeViewModel CreateTreeNode(TreeNodeViewModel parent)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parent,
                IsExpanded = false,
                NodeIcon = SCRIPT_ICON,
                NodeText = ROOT_NODE_NAME,
                NodeToolTip = "SQL scripts",
                NodePayload = null
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add new script",
                MenuItemIcon = NEW_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(AddScriptCommand),
                MenuItemPayload = node
            });
            
            CreateFunctionsNode(node);
            CreateStoredProceduresNode(node);
            CreateScriptNodesFromSettings(node);

            return node;
        }
        private void CreateFunctionsNode(TreeNodeViewModel parentNode)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                NodeIcon = FUNCTION_ICON,
                NodeText = FUNCTIONS_NODE_NAME,
                NodeToolTip = "SQL user functions",
                NodePayload = null
            };
            parentNode.TreeNodes.Add(node);
            CreateTableFunctionsNode(node);
            CreateScalarFunctionsNode(node);
        }
        private void CreateTableFunctionsNode(TreeNodeViewModel parentNode)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                NodeIcon = TABLE_FUNCTION_ICON,
                NodeText = TABLE_FUNCTION_NODE_NAME,
                NodeToolTip = "SQL table-valued functions",
                NodePayload = null
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add new table function",
                MenuItemIcon = ADD_QUERY_ICON,
                MenuItemCommand = new RelayCommand(AddTableFunctionCommand),
                MenuItemPayload = node
            });
            parentNode.TreeNodes.Add(node);
        }
        private void CreateScalarFunctionsNode(TreeNodeViewModel parentNode)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                NodeIcon = SCALAR_FUNCTION_ICON,
                NodeText = SCALAR_FUNCTION_NODE_NAME,
                NodeToolTip = "SQL scalar-valued functions",
                NodePayload = null
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add new scalar function",
                MenuItemIcon = ADD_QUERY_ICON,
                MenuItemCommand = new RelayCommand(AddScalarFunctionCommand),
                MenuItemPayload = node
            });
            parentNode.TreeNodes.Add(node);
        }
        private void CreateStoredProceduresNode(TreeNodeViewModel parentNode)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                NodeIcon = STORED_PROCEDURE_ICON,
                NodeText = STORED_PROCEDURES_NODE_NAME,
                NodeToolTip = "SQL stored procedures",
                NodePayload = null
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add new stored procedure",
                MenuItemIcon = ADD_QUERY_ICON,
                MenuItemCommand = new RelayCommand(AddStoredProcedureCommand),
                MenuItemPayload = node
            });
            parentNode.TreeNodes.Add(node);
        }
        private TreeNodeViewModel CreateScriptTreeNode(TreeNodeViewModel parentNode, ScriptEditorViewModel scriptEditor)
        {
            scriptEditor.MyServer = parentNode.GetAncestorPayload<DatabaseServer>();
            scriptEditor.MyDatabase = parentNode.GetAncestorPayload<DatabaseInfo>();

            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                IsExpanded = true,
                NodeIcon = SCRIPT_ICON,
                //NodeText = null, // indicates first time initialization
                IsEditable = true,
                NodeToolTip = "SQL script",
                NodeTextPropertyBinding = "Name",
                NodePayload = scriptEditor
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Execute script",
                MenuItemIcon = EXECUTE_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(ExecuteScriptCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Edit script",
                MenuItemIcon = EDIT_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(EditScriptCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel() { IsSeparator = true });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Show SQL code",
                MenuItemIcon = SQL_CODE_ICON,
                MenuItemCommand = new RelayCommand(ShowSqlCodeCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Show syntax tree",
                MenuItemIcon = TREE_ICON,
                MenuItemCommand = new RelayCommand(ShowSyntaxTreeCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel() { IsSeparator = true });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Delete script",
                MenuItemIcon = DELETE_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(DeleteScriptCommand),
                MenuItemPayload = node
            });

            // TODO: отложено до реализации основного функционала
            //node.ContextMenuItems.Add(new MenuItemViewModel()
            //{
            //    MenuItemHeader = "Deploy script to web server",
            //    MenuItemIcon = UPLOAD_SCRIPT_ICON,
            //    MenuItemCommand = new RelayCommand(DeployScriptToWebServerCommand),
            //    MenuItemPayload = node
            //});

            node.NodeTextPropertyChanged += NodeTextPropertyChangedHandler;

            return node;
        }
        private void CreateScriptNodesFromSettings(TreeNodeViewModel rootNode)
        {
            DatabaseInfo database = rootNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = rootNode.GetAncestorPayload<DatabaseServer>();

            IFileInfo serverCatalog = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{server.Identity.ToString().ToLower()}");
            if (!serverCatalog.Exists) { Directory.CreateDirectory(serverCatalog.PhysicalPath); }

            IFileInfo databaseCatalog = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}");
            if (!databaseCatalog.Exists) { Directory.CreateDirectory(databaseCatalog.PhysicalPath); }

            IDirectoryContents rootCatalog = FileProvider.GetDirectoryContents($"{ROOT_CATALOG_NAME}/{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}");
            foreach (IFileInfo fileInfo in rootCatalog)
            {
                if (fileInfo.IsDirectory) continue;

                ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
                scriptEditor.Name = fileInfo.Name;
                
                TreeNodeViewModel scriptNode = CreateScriptTreeNode(rootNode, scriptEditor);
                scriptNode.NodeText = fileInfo.Name;
                rootNode.TreeNodes.Add(scriptNode);
            }
        }

        private void AddScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (treeNode.NodeText != ROOT_NODE_NAME) return;

            foreach (TreeNodeViewModel scriptNode in treeNode.TreeNodes)
            {
                if (scriptNode.NodeText == SCRIPT_DEFAULT_NAME)
                {
                    _ = MessageBox.Show("New script already exists! Rename it first.",
                        "DaJet", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    treeNode.IsExpanded = true;
                    scriptNode.IsSelected = true;
                    return;
                }
            }

            ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
            scriptEditor.Name = SCRIPT_DEFAULT_NAME;
            scriptEditor.IsScriptChanged = true;

            TreeNodeViewModel child = CreateScriptTreeNode(treeNode, scriptEditor);
            child.NodeText = SCRIPT_DEFAULT_NAME;
            treeNode.IsExpanded = true;
            treeNode.TreeNodes.Add(child);
            child.IsSelected = true;

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorView editorView = new ScriptEditorView() { DataContext = scriptEditor };
            mainWindow.AddNewTab(scriptEditor.Name, editorView);
        }
        private void ExecuteScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            if (string.IsNullOrWhiteSpace(scriptEditor.ScriptCode))
            {
                IFileInfo file = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/{scriptEditor.Name}");
                if (file != null && file.Exists)
                {
                    using (StreamReader reader = new StreamReader(file.PhysicalPath, Encoding.UTF8))
                    {
                        scriptEditor.ScriptCode = reader.ReadToEnd();
                    }
                }
            }

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();

            IMetadataService metadata = Services.GetService<IMetadataService>();
            metadata.AttachDatabase(string.IsNullOrWhiteSpace(server.Address) ? server.Name : server.Address, database);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            string sql = scripting.PrepareScript(scriptEditor.ScriptCode, out IList<ParseError> errors);
            string errorMessage = string.Empty;
            foreach (ParseError error in errors)
            {
                errorMessage += error.Message + Environment.NewLine;
            }
            if (errors.Count > 0)
            {
                ScriptEditorViewModel editor = Services.GetService<ScriptEditorViewModel>();
                editor.Name = "Errors";
                editor.ScriptCode = errorMessage;
                ScriptEditorView scriptView = new ScriptEditorView()
                {
                    DataContext = editor
                };
                mainWindow.AddNewTab(editor.Name, scriptView);
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
                    ScriptEditorViewModel editor = Services.GetService<ScriptEditorViewModel>();
                    editor.Name = "Errors";
                    editor.ScriptCode = errorMessage;
                    ScriptEditorView scriptView = new ScriptEditorView()
                    {
                        DataContext = editor
                    };
                    mainWindow.AddNewTab(editor.Name, scriptView);
                    return;
                }
            }
            catch (Exception ex)
            {
                ScriptEditorViewModel editor = Services.GetService<ScriptEditorViewModel>();
                editor.Name = "Errors";
                editor.ScriptCode = ex.Message;
                ScriptEditorView scriptView = new ScriptEditorView()
                {
                    DataContext = editor
                };
                mainWindow.AddNewTab(editor.Name, scriptView);
                return;
            }
            JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new DynamicJsonConverter());
            dynamic data = JsonSerializer.Deserialize<dynamic>(json, serializerOptions);

            DataGrid dataView = DynamicGridCreator.CreateDynamicDataGrid(data);
            mainWindow.AddNewTab($"{scriptEditor.Name} (data)", dataView);
        }
        private void ShowSqlCodeCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            if (string.IsNullOrWhiteSpace(scriptEditor.ScriptCode))
            {
                IFileInfo file = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/{scriptEditor.Name}");
                if (file != null && file.Exists)
                {
                    using (StreamReader reader = new StreamReader(file.PhysicalPath, Encoding.UTF8))
                    {
                        scriptEditor.ScriptCode = reader.ReadToEnd();
                    }
                }
            }

            IMetadataService metadata = Services.GetService<IMetadataService>();
            metadata.AttachDatabase(string.IsNullOrWhiteSpace(server.Address) ? server.Name : server.Address, database);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            string sql = scripting.PrepareScript(scriptEditor.ScriptCode, out IList<ParseError> errors);
            string errorMessage = string.Empty;
            foreach (ParseError error in errors)
            {
                errorMessage += error.Message + Environment.NewLine;
            }

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorViewModel editor = Services.GetService<ScriptEditorViewModel>();

            if (errors.Count > 0)
            {
                editor.Name = "Errors";
                editor.ScriptCode = errorMessage;
            }
            else
            {
                editor.Name = $"{scriptEditor.Name} (SQL)";
                editor.ScriptCode = sql;
            }
            ScriptEditorView scriptView = new ScriptEditorView()
            {
                DataContext = editor
            };
            mainWindow.AddNewTab(editor.Name, scriptView);
        }
        private void EditScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            if (string.IsNullOrWhiteSpace(scriptEditor.ScriptCode))
            {
                IFileInfo file = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/{scriptEditor.Name}");
                if (file != null && file.Exists)
                {
                    using (StreamReader reader = new StreamReader(file.PhysicalPath, Encoding.UTF8))
                    {
                        scriptEditor.ScriptCode = reader.ReadToEnd();
                    }
                }
            }

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorView editorView = new ScriptEditorView() { DataContext = scriptEditor };
            mainWindow.AddNewTab(scriptEditor.Name, editorView);
        }
        private void DeleteScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            MessageBoxResult result = MessageBox.Show("Delete script \"" + scriptEditor.Name + "\" ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            if (database == null)
            {
                _ = MessageBox.Show("Parent database is not found!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();
            if (server == null)
            {
                _ = MessageBox.Show("Parent server is not found!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            IFileInfo file = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/{scriptEditor.Name}");
            if (file.Exists)
            {
                File.Delete(file.PhysicalPath);
            }

            treeNode.NodeTextPropertyChanged -= NodeTextPropertyChangedHandler;
            treeNode.Parent.TreeNodes.Remove(treeNode);

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            for (int i = 0; i < mainWindow.Tabs.Count; i++)
            {
                TabViewModel tab = mainWindow.Tabs[i];
                if (tab.Content is ScriptEditorView view)
                {
                    if (view.DataContext is ScriptEditorViewModel editor)
                    {
                        if (editor == scriptEditor)
                        {
                            mainWindow.RemoveTab(tab);
                            break;
                        }
                    }
                }
            }
        }
        private void NodeTextPropertyChangedHandler(TreeNodeViewModel node, NodeTextPropertyChangedEventArgs args)
        {
            if (!(node.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            DatabaseInfo database = node.GetAncestorPayload<DatabaseInfo>();
            if (database == null)
            {
                _ = MessageBox.Show("Parent database is not found!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            DatabaseServer server = node.GetAncestorPayload<DatabaseServer>();
            if (server == null)
            {
                _ = MessageBox.Show("Parent server is not found!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            IFileInfo currentFile = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/{args.OldValue}");
            IFileInfo newFile = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/{args.NewValue}");
            if (!currentFile.Exists)
            {
                if (args.OldValue == SCRIPT_DEFAULT_NAME)
                {
                    try
                    {
                        scriptEditor.SaveCommand.Execute(null);
                    }
                    catch (Exception ex)
                    {
                        args.Cancel = true;
                        _ = MessageBox.Show(ex.Message, "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    _ = MessageBox.Show($"File \"{args.OldValue}\" is not found!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    args.Cancel = true;
                    return;
                }
            }

            try
            {
                File.Move(currentFile.PhysicalPath, newFile.PhysicalPath);

                scriptEditor.Name = args.NewValue;

                MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
                mainWindow.RefreshTabHeader(scriptEditor, scriptEditor.Name);
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                _ = MessageBox.Show(ex.Message, "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ShowSyntaxTreeCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            if (string.IsNullOrWhiteSpace(scriptEditor.ScriptCode))
            {
                DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
                DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

                IFileInfo file = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/{scriptEditor.Name}");
                if (file != null && file.Exists)
                {
                    using (StreamReader reader = new StreamReader(file.PhysicalPath, Encoding.UTF8))
                    {
                        scriptEditor.ScriptCode = reader.ReadToEnd();
                    }
                }
            }

            IScriptingService scripting = Services.GetService<IScriptingService>();
            TSqlFragment syntaxTree = scripting.ParseScript(scriptEditor.ScriptCode, out IList<ParseError> errors);
            string errorMessage = string.Empty;
            foreach (ParseError error in errors)
            {
                errorMessage += error.Message + Environment.NewLine;
            }

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorViewModel viewModel = Services.GetService<ScriptEditorViewModel>();

            if (errors.Count > 0)
            {
                viewModel.Name = "Errors";
                viewModel.ScriptCode = errorMessage;
                ScriptEditorView scriptView = new ScriptEditorView() { DataContext = viewModel };
                mainWindow.AddNewTab(viewModel.Name, scriptView);
                return;
            }

            TreeNodeViewModel treeModel;
            TSqlFragmentTreeBuilder builder = new TSqlFragmentTreeBuilder();
            builder.Build(syntaxTree, out treeModel);

            TreeNodeView treeView = new TreeNodeView() { DataContext = treeModel };
            mainWindow.AddNewTab(scriptEditor.Name + " (syntax tree)", treeView);
        }



        private void AddTableFunctionCommand(object node)
        {
            if (!(node is TreeNodeViewModel parentNode)) return;
            if (parentNode.NodeText != TABLE_FUNCTION_NODE_NAME) return;

            // TODO
            MessageBox.Show("Under construction...", "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void AddScalarFunctionCommand(object node)
        {
            if (!(node is TreeNodeViewModel parentNode)) return;
            if (parentNode.NodeText != SCALAR_FUNCTION_NODE_NAME) return;

            DatabaseInfo database = parentNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = parentNode.GetAncestorPayload<DatabaseServer>();

            ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
            scriptEditor.Name = "new func";
            scriptEditor.MyServer = server;
            scriptEditor.MyDatabase = database;
            scriptEditor.ScriptType = MetaScriptType.ScalarFunction;
            scriptEditor.ScriptCode = "CREATE FUNCTION [fn_NewFunction]\n(\n\t@param nvarchar(36)\n)\nRETURNS int\nAS\nBEGIN\n\n\tRETURN 1;\n\nEND;";
            scriptEditor.IsScriptChanged = true;

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorView editorView = new ScriptEditorView() { DataContext = scriptEditor };
            mainWindow.AddNewTab(scriptEditor.Name, editorView);
        }
        private void AddStoredProcedureCommand(object node)
        {
            if (!(node is TreeNodeViewModel parentNode)) return;
            if (parentNode.NodeText != STORED_PROCEDURES_NODE_NAME) return;

            DatabaseInfo database = parentNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = parentNode.GetAncestorPayload<DatabaseServer>();

            ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
            scriptEditor.Name = "new proc";
            scriptEditor.MyServer = server;
            scriptEditor.MyDatabase = database;
            scriptEditor.ScriptType = MetaScriptType.StoredProcedure;
            scriptEditor.ScriptCode = "CREATE PROCEDURE [sp_NewProcedure]\n\t@param nvarchar(36)\nAS\nBEGIN\n\nEND;";
            scriptEditor.IsScriptChanged = true;

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorView editorView = new ScriptEditorView() { DataContext = scriptEditor };
            mainWindow.AddNewTab(scriptEditor.Name, editorView);
        }



        private void DeployScriptToWebServerCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            MessageBoxResult result = MessageBox.Show("Deploy script \"" + scriptEditor.Name + "\" ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            if (database == null)
            {
                _ = MessageBox.Show("Parent database is not found!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();
            if (server == null)
            {
                _ = MessageBox.Show("Parent server is not found!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            IFileInfo file = FileProvider.GetFileInfo($"{ROOT_CATALOG_NAME}/{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/{scriptEditor.Name}");
            if (!file.Exists)
            {
                _ = MessageBox.Show("Script file is not found!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            // TODO: select web server to deploy script to

            IHttpClientFactory http = Services.GetService<IHttpClientFactory>();
            var client = http.CreateClient("test-server");
            if (client.BaseAddress == null) { client.BaseAddress = new Uri("http://localhost:5000"); }

            string url = $"{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/{scriptEditor.Name}";

            byte[] bytes = File.ReadAllBytes(file.PhysicalPath);
            string content = Convert.ToBase64String(bytes);
            string requestJson = $"{{ \"script\" : \"{content}\" }}";
            StringContent body = new StringContent(requestJson, Encoding.UTF8, "application/json");

            try
            {
                var response = client.PutAsync(url, body).Result;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    HttpServicesController controller = Services.GetService<HttpServicesController>();
                    if (controller != null)
                    {
                        // TODO: add tree node to the "Http services" node
                        controller.CreateScriptNode(controller.WebSettings.WebServers[0], server, database, scriptEditor.Name);
                    }
                    _ = MessageBox.Show("Script has been deployed successfully.", scriptEditor.Name, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _ = MessageBox.Show(((int)response.StatusCode).ToString() + " (" + response.StatusCode.ToString() + "): " + response.ReasonPhrase, scriptEditor.Name);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, scriptEditor.Name);
            }
        }
    }
}
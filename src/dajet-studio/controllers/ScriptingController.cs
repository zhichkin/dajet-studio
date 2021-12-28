using DaJet.Data.Scripting;
using DaJet.Studio.MVVM;
using DaJet.UI.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class ScriptingController : ITreeNodeController
    {
        #region "Constants"

        private const string ROOT_NODE_NAME = "Scripts";
        private const string SCRIPTS_NODE_NAME = "Scripts";
        private const string ROOT_CATALOG_NAME = "scripts";
        private const string SCRIPT_FILE_EXTENSION = ".qry";
        private const string SCRIPT_DEFAULT_NAME = "new_script";
        private const string TABLE_FUNCTION_DEFAULT_NAME = "new_table_function";
        private const string SCALAR_FUNCTION_DEFAULT_NAME = "new_scalar_function";
        private const string STORED_PROCEDURE_DEFAULT_NAME = "new_stored_procedure";
        private const string FUNCTIONS_NODE_NAME = "Functions";
        private const string TABLE_FUNCTION_NODE_NAME = "Table functions";
        private const string SCALAR_FUNCTION_NODE_NAME = "Scalar functions";
        private const string STORED_PROCEDURES_NODE_NAME = "Procedures";

        #endregion

        #region "Icons and constants"

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
        private const string DELETE_SCRIPT_FROM_DATABASE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/delete-database.png";

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
        private readonly BitmapImage DELETE_SCRIPT_FROM_DATABASE_ICON = new BitmapImage(new Uri(DELETE_SCRIPT_FROM_DATABASE_ICON_PATH));

        #endregion

        private IServiceProvider Services { get; }
        private IFileProvider FileProvider { get; }
        public ScriptingController(IServiceProvider serviceProvider, IFileProvider fileProvider)
        {
            Services = serviceProvider;
            FileProvider = fileProvider;
        }

        private void CreateCatalogIfNotExists(string catalogName)
        {
            IFileInfo catalog = FileProvider.GetFileInfo(catalogName);
            if (!catalog.Exists)
            {
                _ = Directory.CreateDirectory(catalog.PhysicalPath);
            }
        }
        public string GetDatabaseCatalog(DatabaseServer server, DatabaseInfo database)
        {
            string catalogName = $"{ROOT_CATALOG_NAME}";

            CreateCatalogIfNotExists(catalogName);

            catalogName += $"/{server.Name.ToLower()}";
            
            CreateCatalogIfNotExists(catalogName);
            
            catalogName += $"/{database.Name.ToLower()}";
            
            CreateCatalogIfNotExists(catalogName);

            return catalogName;
        }
        public string GetScriptsCatalog(DatabaseServer server, DatabaseInfo database)
        {
            string catalogName = GetDatabaseCatalog(server, database) + "/scripts";
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
        public string GetScriptsCatalogName(DatabaseServer server, DatabaseInfo database, ScriptType scriptType)
        {
            string catalogName;
            if (scriptType == ScriptType.Script)
            {
                catalogName = GetScriptsCatalog(server, database);
            }
            else if (scriptType == ScriptType.TableFunction)
            {
                catalogName = GetTableFunctionsCatalog(server, database);
            }
            else if (scriptType == ScriptType.ScalarFunction)
            {
                catalogName = GetScalarFunctionsCatalog(server, database);
            }
            else if (scriptType == ScriptType.StoredProcedure)
            {
                catalogName = GetStoredProceduresCatalog(server, database);
            }
            else
            {
                throw new InvalidOperationException(scriptType.ToString() + ": unknown script type!");
            }
            return catalogName;
        }
        public bool ScriptFileExists(string catalogName, string scriptName)
        {
            return FileProvider.GetFileInfo($"{catalogName}/{scriptName}{SCRIPT_FILE_EXTENSION}").Exists;
        }
        public void SaveScriptFile(string catalogName, string scriptName, string sourceCode)
        {
            IFileInfo file = FileProvider.GetFileInfo($"{catalogName}/{scriptName}{SCRIPT_FILE_EXTENSION}");
            using (StreamWriter writer = new StreamWriter(file.PhysicalPath, false, Encoding.UTF8))
            {
                writer.Write(sourceCode);
            }
        }
        public void RenameScriptFile(string catalogName, string oldName, string newName)
        {
            IFileInfo oldFile = FileProvider.GetFileInfo($"{catalogName}/{oldName}{SCRIPT_FILE_EXTENSION}");
            IFileInfo newFile = FileProvider.GetFileInfo($"{catalogName}/{newName}{SCRIPT_FILE_EXTENSION}");
            if (newFile.Exists)
            {
                throw new InvalidOperationException($"Renaming file failed: \"{newFile}\" already exists!");
            }
            File.Move(oldFile.PhysicalPath, newFile.PhysicalPath);
        }
        public void DeleteScriptFile(string catalogName, string scriptName)
        {
            IFileInfo file = FileProvider.GetFileInfo($"{catalogName}/{scriptName}{SCRIPT_FILE_EXTENSION}");
            if (file.Exists)
            {
                File.Delete(file.PhysicalPath);
            }
        }
        public string ReadScriptSourceCode(DatabaseServer server, DatabaseInfo database, ScriptType scriptType, string scriptName)
        {
            string sourceCode;
            
            string catalogName;
            if (scriptType == ScriptType.Script)
            {
                catalogName = GetScriptsCatalog(server, database);
            }
            else if (scriptType == ScriptType.TableFunction)
            {
                catalogName = GetTableFunctionsCatalog(server, database);
            }
            else if (scriptType == ScriptType.ScalarFunction)
            {
                catalogName = GetScalarFunctionsCatalog(server, database);
            }
            else if (scriptType == ScriptType.StoredProcedure)
            {
                catalogName = GetStoredProceduresCatalog(server, database);
            }
            else
            {
                throw new InvalidOperationException(scriptType.ToString() + ": unknown script type!");
            }

            IFileInfo file = FileProvider.GetFileInfo($"{catalogName}/{scriptName}{SCRIPT_FILE_EXTENSION}");
            if (!file.Exists)
            {
                throw new FileNotFoundException(scriptName);
            }

            using (StreamReader reader = new StreamReader(file.PhysicalPath, Encoding.UTF8))
            {
                sourceCode = reader.ReadToEnd();
            }

            return sourceCode;
        }
        public byte[] ReadScriptAsBytes(DatabaseServer server, DatabaseInfo database, ScriptType scriptType, string scriptName)
        {
            string catalogName;
            if (scriptType == ScriptType.Script)
            {
                catalogName = GetScriptsCatalog(server, database);
            }
            else if (scriptType == ScriptType.TableFunction)
            {
                catalogName = GetTableFunctionsCatalog(server, database);
            }
            else if (scriptType == ScriptType.ScalarFunction)
            {
                catalogName = GetScalarFunctionsCatalog(server, database);
            }
            else if (scriptType == ScriptType.StoredProcedure)
            {
                catalogName = GetStoredProceduresCatalog(server, database);
            }
            else
            {
                throw new InvalidOperationException(scriptType.ToString() + ": unknown script type!");
            }

            IFileInfo file = FileProvider.GetFileInfo($"{catalogName}/{scriptName}{SCRIPT_FILE_EXTENSION}");
            if (!file.Exists)
            {
                throw new FileNotFoundException(scriptName);
            }

            return File.ReadAllBytes(file.PhysicalPath);
        }
        public TreeNodeViewModel GetScriptTreeNode(ScriptEditorViewModel scriptEditor)
        {
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            TreeNodeViewModel databaseNode = mainWindow.GetTreeNodeByPayload(mainWindow.MainTreeRegion.TreeNodes, scriptEditor.MyDatabase);
            if (databaseNode == null) { return null; }

            TreeNodeViewModel parentNode = GetParentTreeNode(databaseNode, scriptEditor.ScriptType);
            if (parentNode == null) { return null; }

            return parentNode.TreeNodes.Where(n => n.NodePayload == scriptEditor).FirstOrDefault();
        }
        public TreeNodeViewModel GetScriptTreeNodeByName(DatabaseServer server, DatabaseInfo database, ScriptType scriptType, string scriptName)
        {
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();

            TreeNodeViewModel serverNode = mainWindow.GetTreeNodeByPayload(mainWindow.MainTreeRegion.TreeNodes, server);
            if (serverNode == null)
            {
                return null;
            }

            TreeNodeViewModel databaseNode = mainWindow.GetTreeNodeByPayload(serverNode.TreeNodes, database);
            if (databaseNode == null)
            {
                return null;
            }

            TreeNodeViewModel parentNode = GetParentTreeNode(databaseNode, scriptType);
            if (parentNode == null)
            {
                return null;
            }

            return parentNode.TreeNodes.Where(n => n.NodeText == scriptName).FirstOrDefault();
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
                NodeToolTip = "SQL scripts, functions and procedures",
                NodePayload = ROOT_NODE_NAME // Аналогично пространствам имён 1С: Справочники, документы и т.д.
            };
            
            CreateScriptsNode(node);
            CreateFunctionsNode(node);
            CreateStoredProceduresNode(node);

            return node;
        }
        private void CreateScriptsNode(TreeNodeViewModel parentNode)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                NodeIcon = SCRIPT_ICON,
                NodeText = SCRIPTS_NODE_NAME,
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
            parentNode.TreeNodes.Add(node);

            CreateScriptNodesFromFileSystem(node, ScriptType.Script);
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

            CreateScriptNodesFromFileSystem(node, ScriptType.TableFunction);
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

            CreateScriptNodesFromFileSystem(node, ScriptType.ScalarFunction);
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

            CreateScriptNodesFromFileSystem(node, ScriptType.StoredProcedure);
        }
        private BitmapImage GetScriptNodeIcon(ScriptType scriptType)
        {
            if (scriptType == ScriptType.Script) { return SCRIPT_ICON; }
            else if (scriptType == ScriptType.TableFunction) { return TABLE_FUNCTION_ICON; }
            else if (scriptType == ScriptType.ScalarFunction) { return SCALAR_FUNCTION_ICON; }
            else if (scriptType == ScriptType.StoredProcedure) { return STORED_PROCEDURE_ICON; }
            else { return SCRIPT_ICON; }
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
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "New Avalon editor",
                MenuItemIcon = EDIT_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(AvalonEditScriptCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel() { IsSeparator = true });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Delete script from file system",
                MenuItemIcon = DELETE_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(DeleteScriptCommand),
                MenuItemPayload = node
            });

            if (scriptEditor.ScriptType == ScriptType.TableFunction
                || scriptEditor.ScriptType == ScriptType.ScalarFunction
                || scriptEditor.ScriptType == ScriptType.StoredProcedure)
            {
                node.ContextMenuItems.Add(new MenuItemViewModel() { IsSeparator = true });
                node.ContextMenuItems.Add(new MenuItemViewModel()
                {
                    MenuItemHeader = "Create script in database",
                    MenuItemIcon = UPLOAD_SCRIPT_ICON,
                    MenuItemCommand = new RelayCommand(DeployScriptToDatabaseCommand),
                    MenuItemPayload = node
                });
                node.ContextMenuItems.Add(new MenuItemViewModel()
                {
                    MenuItemHeader = "Delete script in database",
                    MenuItemIcon = DELETE_SCRIPT_FROM_DATABASE_ICON,
                    MenuItemCommand = new RelayCommand(DeleteScriptInDatabaseCommand),
                    MenuItemPayload = node
                });
            }

            node.NodeTextPropertyChanged += NodeTextPropertyChangedHandler;

            return node;
        }
        public void CreateScriptTreeNode(ScriptEditorViewModel scriptEditor)
        {
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            TreeNodeViewModel databaseNode = mainWindow.GetTreeNodeByPayload(mainWindow.MainTreeRegion.TreeNodes, scriptEditor.MyDatabase);
            if (databaseNode == null) { return; }
            
            TreeNodeViewModel parentNode = GetParentTreeNode(databaseNode, scriptEditor.ScriptType);
            if (parentNode == null) { return; }

            TreeNodeViewModel scriptNode = parentNode.TreeNodes
                .Where(n => n.NodeText == scriptEditor.Name)
                .FirstOrDefault();
            if (scriptNode != null)
            {
                parentNode.IsExpanded = true;
                scriptNode.IsSelected = true;
                throw new InvalidOperationException($"Script \"{scriptEditor.Name}\" already exists!");
            }

            scriptNode = CreateScriptTreeNode(parentNode, scriptEditor);
            scriptNode.NodeText = scriptEditor.Name;
            scriptNode.NodeIcon = GetScriptNodeIcon(scriptEditor.ScriptType);
            parentNode.TreeNodes.Add(scriptNode);
            parentNode.IsExpanded = true;
            scriptNode.IsSelected = true;
        }

        private void CreateScriptNodesFromFileSystem(TreeNodeViewModel rootNode, ScriptType scriptType)
        {
            DatabaseInfo database = rootNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = rootNode.GetAncestorPayload<DatabaseServer>();

            string catalogName = GetScriptsCatalogName(server, database, scriptType);
            IDirectoryContents rootCatalog = FileProvider.GetDirectoryContents(catalogName);
            foreach (IFileInfo fileInfo in rootCatalog)
            {
                if (fileInfo.IsDirectory) continue;

                ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
                scriptEditor.ScriptType = scriptType;
                scriptEditor.Name = fileInfo.Name.TrimEnd(new char[] { '.', 'q', 'r', 'y' });
                
                TreeNodeViewModel scriptNode = CreateScriptTreeNode(rootNode, scriptEditor);
                scriptNode.NodeText = scriptEditor.Name;
                scriptNode.NodeIcon = GetScriptNodeIcon(scriptType);
                rootNode.TreeNodes.Add(scriptNode);
            }
        }

        private TreeNodeViewModel GetParentTreeNode(TreeNodeViewModel databaseNode, ScriptType scriptType)
        {
            TreeNodeViewModel parentNode = databaseNode.TreeNodes.Where(n => n.NodeText == ROOT_NODE_NAME).FirstOrDefault();
            if (parentNode == null) { return null; }

            if (scriptType == ScriptType.Script)
            {
                return parentNode.TreeNodes.Where(n => n.NodeText == SCRIPTS_NODE_NAME).FirstOrDefault();
            }
            else if (scriptType == ScriptType.TableFunction)
            {
                parentNode = parentNode.TreeNodes.Where(n => n.NodeText == FUNCTIONS_NODE_NAME).FirstOrDefault();
            }
            else if (scriptType == ScriptType.ScalarFunction)
            {
                parentNode = parentNode.TreeNodes.Where(n => n.NodeText == FUNCTIONS_NODE_NAME).FirstOrDefault();
            }
            else if (scriptType == ScriptType.StoredProcedure)
            {
                return parentNode.TreeNodes.Where(n => n.NodeText == STORED_PROCEDURES_NODE_NAME).FirstOrDefault();
            }

            if (parentNode == null) { return null; }

            if (scriptType == ScriptType.TableFunction)
            {
                parentNode = parentNode.TreeNodes.Where(n => n.NodeText == TABLE_FUNCTION_NODE_NAME).FirstOrDefault();
            }
            else if (scriptType == ScriptType.ScalarFunction)
            {
                parentNode = parentNode.TreeNodes.Where(n => n.NodeText == SCALAR_FUNCTION_NODE_NAME).FirstOrDefault();
            }

            return parentNode;
        }

        private string GetConnectionString(DatabaseServer server, DatabaseInfo database)
        {
            SqlConnectionStringBuilder helper = new SqlConnectionStringBuilder()
            {
                DataSource = server.Name,
                InitialCatalog = database.Name
            };
            if (string.IsNullOrEmpty(database.UserName))
            {
                helper.IntegratedSecurity = true;
            }
            else
            {
                helper.UserID = database.UserName;
                helper.Password = database.Password;
            }
            return helper.ToString();
        }

        private void ExecuteScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            if (string.IsNullOrWhiteSpace(scriptEditor.ScriptCode))
            {
                scriptEditor.ScriptCode = ReadScriptSourceCode(server, database, scriptEditor.ScriptType, scriptEditor.Name);
            }

            if (scriptEditor.ScriptType == ScriptType.Script)
            {
                ExecuteScript(server, database, scriptEditor);
            }
            else if (scriptEditor.ScriptType == ScriptType.TableFunction || scriptEditor.ScriptType == ScriptType.ScalarFunction)
            {
                ExecuteFunction(server, database, scriptEditor.ScriptCode);
            }
            else if (scriptEditor.ScriptType == ScriptType.StoredProcedure)
            {
                ExecuteStoredProcedure(server, database, scriptEditor.ScriptCode);
            }
        }
        private void ExecuteScript(DatabaseServer server, DatabaseInfo database, ScriptEditorViewModel scriptEditor)
        {
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();

            string connectionString = GetConnectionString(server, database);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            scripting.UseConnectionString(connectionString);
            scripting.MainInfoBase = database.InfoBase;
            scripting.Databases.Clear();

            string sql = scripting.PrepareScript(scriptEditor.ScriptCode, out IList<ParseError> errors);
            string errorMessage = string.Empty;
            foreach (ParseError error in errors)
            {
                errorMessage += error.Message + Environment.NewLine;
            }
            if (errors.Count > 0)
            {
                ShowException(errorMessage);
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
                    ShowException(errorMessage);
                    return;
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
                return;
            }
            JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new DynamicJsonConverter());
            dynamic data = JsonSerializer.Deserialize<dynamic>(json, serializerOptions);

            try
            {
                DataGrid dataView = DynamicGridCreator.CreateDynamicDataGrid(data);
                mainWindow.AddNewTab($"{scriptEditor.Name} (data)", dataView);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }
        private void ExecuteFunction(DatabaseServer server, DatabaseInfo database, string sourceCode)
        {
            string connectionString = GetConnectionString(server, database);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            scripting.UseConnectionString(connectionString);
            scripting.MainInfoBase = database.InfoBase;
            scripting.Databases.Clear();

            TSqlFragment syntaxTree = scripting.ParseScript(sourceCode, out IList<ParseError> errors);
            if (errors.Count > 0) { ShowParseErrors(errors); return; }

            // Generate SQL script to execute stored procedure
            CreateFunctionStatementVisitor visitor = new CreateFunctionStatementVisitor();
            syntaxTree.Accept(visitor);
            string scriptCode = visitor.GenerateExecuteFunctionCode();

            // Show script code
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorViewModel viewModel = Services.GetService<ScriptEditorViewModel>();
            viewModel.Name = visitor.FunctionName;
            viewModel.MyServer = server;
            viewModel.MyDatabase = database;
            viewModel.ScriptType = ScriptType.Script;
            viewModel.ScriptCode = scriptCode;
            viewModel.IsScriptChanged = true;
            ScriptEditorView view = new ScriptEditorView() { DataContext = viewModel };
            mainWindow.AddNewTab(viewModel.Name, view);
        }
        private void ExecuteStoredProcedure(DatabaseServer server, DatabaseInfo database, string sourceCode)
        {
            string connectionString = GetConnectionString(server, database);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            scripting.UseConnectionString(connectionString);
            scripting.MainInfoBase = database.InfoBase;
            scripting.Databases.Clear();

            TSqlFragment syntaxTree = scripting.ParseScript(sourceCode, out IList<ParseError> errors);
            if (errors.Count > 0) { ShowParseErrors(errors); return; }

            // Generate SQL script to execute stored procedure
            CreateProcedureStatementVisitor visitor = new CreateProcedureStatementVisitor();
            syntaxTree.Accept(visitor);
            string scriptCode = visitor.GenerateExecuteProcedureCode();

            // Show script code
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorViewModel viewModel = Services.GetService<ScriptEditorViewModel>();
            viewModel.Name = visitor.ProcedureName;
            viewModel.MyServer = server;
            viewModel.MyDatabase = database;
            viewModel.ScriptType = ScriptType.Script;
            viewModel.ScriptCode = scriptCode;
            viewModel.IsScriptChanged = true;
            ScriptEditorView view = new ScriptEditorView() { DataContext = viewModel };
            mainWindow.AddNewTab(viewModel.Name, view);
        }

        private void ShowSqlCodeCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            if (string.IsNullOrWhiteSpace(scriptEditor.ScriptCode))
            {
                scriptEditor.ScriptCode = ReadScriptSourceCode(server, database, scriptEditor.ScriptType, scriptEditor.Name);
            }

            string connectionString = GetConnectionString(server, database);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            scripting.UseConnectionString(connectionString);
            scripting.MainInfoBase = database.InfoBase;
            scripting.Databases.Clear();

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
                scriptEditor.ScriptCode = ReadScriptSourceCode(server, database, scriptEditor.ScriptType, scriptEditor.Name);
            }

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorView editorView = new ScriptEditorView() { DataContext = scriptEditor };
            mainWindow.AddNewTab(scriptEditor.Name, editorView);
        }
        private void AvalonEditScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            string script;
            if (string.IsNullOrWhiteSpace(scriptEditor.ScriptCode))
            {
                script = ReadScriptSourceCode(server, database, scriptEditor.ScriptType, scriptEditor.Name);
            }
            else
            {
                script = scriptEditor.ScriptCode;
            }

            string connectionString = GetConnectionString(server, database);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            scripting.UseConnectionString(connectionString);
            scripting.MainInfoBase = database.InfoBase;
            scripting.Databases.Clear();

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            AvalonScriptEditor editorView = new AvalonScriptEditor(script, scripting);
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
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            string catalogName = GetScriptsCatalogName(server, database, scriptEditor.ScriptType);
            IFileInfo file = FileProvider.GetFileInfo($"{catalogName}/{scriptEditor.Name}{SCRIPT_FILE_EXTENSION}");
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
            DatabaseServer server = node.GetAncestorPayload<DatabaseServer>();

            string catalogName = GetScriptsCatalogName(server, database, scriptEditor.ScriptType);

            IFileInfo oldFile = FileProvider.GetFileInfo($"{catalogName}/{args.OldValue}{SCRIPT_FILE_EXTENSION}");
            IFileInfo newFile = FileProvider.GetFileInfo($"{catalogName}/{args.NewValue}{SCRIPT_FILE_EXTENSION}");
            if (!oldFile.Exists)
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
                File.Move(oldFile.PhysicalPath, newFile.PhysicalPath);

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
                scriptEditor.ScriptCode = ReadScriptSourceCode(server, database, scriptEditor.ScriptType, scriptEditor.Name);
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



        private void AddScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel parentNode)) return;
            if (parentNode.NodeText != ROOT_NODE_NAME) return;

            DatabaseInfo database = parentNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = parentNode.GetAncestorPayload<DatabaseServer>();

            ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
            scriptEditor.Name = SCRIPT_DEFAULT_NAME;
            scriptEditor.MyServer = server;
            scriptEditor.MyDatabase = database;
            scriptEditor.ScriptType = ScriptType.Script;
            scriptEditor.ScriptCode = string.Empty;
            scriptEditor.IsScriptChanged = true;

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorView editorView = new ScriptEditorView() { DataContext = scriptEditor };
            mainWindow.AddNewTab(scriptEditor.Name, editorView);
        }
        private void AddTableFunctionCommand(object node)
        {
            if (!(node is TreeNodeViewModel parentNode)) return;
            if (parentNode.NodeText != TABLE_FUNCTION_NODE_NAME) return;

            DatabaseInfo database = parentNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = parentNode.GetAncestorPayload<DatabaseServer>();

            ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
            scriptEditor.Name = TABLE_FUNCTION_DEFAULT_NAME;
            scriptEditor.MyServer = server;
            scriptEditor.MyDatabase = database;
            scriptEditor.ScriptType = ScriptType.TableFunction;
            scriptEditor.ScriptCode = "CREATE OR ALTER FUNCTION [fn_NewFunction]\n(\n\t@param nvarchar(36)\n)\nRETURNS TABLE\nAS\nRETURN\n\tSELECT * FROM [table];";
            scriptEditor.IsScriptChanged = true;

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorView editorView = new ScriptEditorView() { DataContext = scriptEditor };
            mainWindow.AddNewTab(scriptEditor.Name, editorView);
        }
        private void AddScalarFunctionCommand(object node)
        {
            if (!(node is TreeNodeViewModel parentNode)) return;
            if (parentNode.NodeText != SCALAR_FUNCTION_NODE_NAME) return;

            DatabaseInfo database = parentNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = parentNode.GetAncestorPayload<DatabaseServer>();

            ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
            scriptEditor.Name = SCALAR_FUNCTION_DEFAULT_NAME;
            scriptEditor.MyServer = server;
            scriptEditor.MyDatabase = database;
            scriptEditor.ScriptType = ScriptType.ScalarFunction;
            scriptEditor.ScriptCode = "CREATE OR ALTER FUNCTION [fn_NewFunction]\n(\n\t@param nvarchar(36)\n)\nRETURNS int\nAS\nBEGIN\n\n\tRETURN 1;\n\nEND;";
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
            scriptEditor.Name = STORED_PROCEDURE_DEFAULT_NAME;
            scriptEditor.MyServer = server;
            scriptEditor.MyDatabase = database;
            scriptEditor.ScriptType = ScriptType.StoredProcedure;
            scriptEditor.ScriptCode = "CREATE OR ALTER PROCEDURE [sp_NewProcedure]\n\t@param nvarchar(36)\nAS\nBEGIN\n\nEND;";
            scriptEditor.IsScriptChanged = true;

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorView editorView = new ScriptEditorView() { DataContext = scriptEditor };
            mainWindow.AddNewTab(scriptEditor.Name, editorView);
        }

        private void DeployScriptToDatabaseCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            MessageBoxResult result = MessageBox.Show("Deploy script \"" + scriptEditor.Name + "\" ?"
                + Environment.NewLine + "Server: " + server.Name + "."
                + Environment.NewLine + "Database: " + database.Name + ".",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            if (scriptEditor.IsScriptChanged)
            {
                MessageBox.Show("The script has unsaved changes. Save them first.",
                    "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                return;
            }

            string sourceCode = ReadScriptSourceCode(server, database, scriptEditor.ScriptType, scriptEditor.Name);

            string connectionString = GetConnectionString(server, database);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            scripting.UseConnectionString(connectionString);
            scripting.MainInfoBase = database.InfoBase;
            scripting.Databases.Clear();

            string sql = scripting.PrepareScript(sourceCode, out IList<ParseError> errors);
            if (errors.Count > 0) { ShowParseErrors(errors); return; }

            string json = "[]";
            try
            {
                json = scripting.ExecuteScript(sql, out IList<ParseError> executeErrors);
                if (executeErrors.Count > 0) { ShowParseErrors(executeErrors); return; }
            }
            catch (Exception ex) { ShowException(ex); return; }

            MessageBox.Show($"Script \"{scriptEditor.Name}\" has been created successfully.",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Information);
        }
        private void DeleteScriptInDatabaseCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ScriptEditorViewModel scriptEditor)) return;

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            MessageBoxResult result = MessageBox.Show("Delete script \"" + scriptEditor.Name + "\" ?"
                + Environment.NewLine + "Server: " + server.Name + "."
                + Environment.NewLine + "Database: " + database.Name + ".",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            if (scriptEditor.IsScriptChanged)
            {
                MessageBox.Show("The script has unsaved changes. Save them first.",
                    "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                return;
            }

            string sourceCode = ReadScriptSourceCode(server, database, scriptEditor.ScriptType, scriptEditor.Name);

            string connectionString = GetConnectionString(server, database);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            scripting.UseConnectionString(connectionString);
            scripting.MainInfoBase = database.InfoBase;
            scripting.Databases.Clear();

            TSqlFragment syntaxTree = scripting.ParseScript(sourceCode, out IList<ParseError> errors);
            if (errors.Count > 0) { ShowParseErrors(errors); return; }

            //IF OBJECT_ID('dbo.fn_is_name_valid', 'FN') IS NOT NULL
            //BEGIN
            //    DROP FUNCTION [dbo].[fn_is_name_valid];
            //END;

            //IF OBJECT_ID('dbo.sp_create_queue', 'P') IS NOT NULL
            //BEGIN
            //    DROP PROCEDURE[dbo].[sp_create_queue];
            //END;

            string sql = string.Empty;
            string scriptName = scriptEditor.Name;
            if (scriptEditor.ScriptType == ScriptType.TableFunction || scriptEditor.ScriptType == ScriptType.ScalarFunction)
            {
                CreateFunctionStatementVisitor visitor = new CreateFunctionStatementVisitor();
                syntaxTree.Accept(visitor);
                scriptName = visitor.FunctionName;
                sql = $"DROP FUNCTION [{scriptName}];";
            }
            else if (scriptEditor.ScriptType == ScriptType.StoredProcedure)
            {
                CreateProcedureStatementVisitor visitor = new CreateProcedureStatementVisitor();
                syntaxTree.Accept(visitor);
                scriptName = visitor.ProcedureName;
                sql = $"DROP PROCEDURE [{scriptName}];";
            }

            string json = "[]";
            try
            {
                json = scripting.ExecuteScript(sql, out IList<ParseError> executeErrors);
                if (executeErrors.Count > 0) { ShowParseErrors(executeErrors); return; }
            }
            catch (Exception ex) { ShowException(ex); return; }

            MessageBox.Show($"Script \"{scriptEditor.Name}\" has been deleted successfully.",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Information);
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

        public void Search(string text)
        {
            throw new NotImplementedException();
        }
        public TreeNodeViewModel CreateTreeNode(MainWindowViewModel parent)
        {
            throw new NotImplementedException();
        }
    }
}
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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class HttpServicesController : ITreeNodeController
    {
        #region "Icons and constants"

        private const string HTTP_SERVICES_NODE_NAME = "HTTP services";
        private const string HTTP_SERVICES_NODE_TOOLTIP = "HTTP web services";
        private const string WEB_SETTINGS_FILE_NAME = "web-settings.json";
        private const string WEB_SETTINGS_CATALOG_NAME = "web";

        private const string WEB_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/web-server.png";
        private const string ADD_WEB_SERVICE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/add-web-service.png";
        private const string SERVER_SETTINGS_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server-settings.png";
        private const string HTTP_CONNECTION_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/http-connection.png";
        private const string DISCONNECT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/disconnect.png";
        private const string CONNECTION_OFFLINE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/connection-offline.png";
        private const string CONNECTION_WARNING_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/connection-warning.png";
        private const string DATA_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/data-server.png";
        private const string DATABASE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database.png";
        private const string DELETE_DATABASE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/delete-database.png";
        private const string SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database-script.png";
        private const string UPLOAD_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/upload-script.png";
        private const string DELETE_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/delete-script.png";
        private const string EXECUTE_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/run.png";

        private readonly BitmapImage WEB_SERVER_ICON = new BitmapImage(new Uri(WEB_SERVER_ICON_PATH));
        private readonly BitmapImage ADD_WEB_SERVICE_ICON = new BitmapImage(new Uri(ADD_WEB_SERVICE_ICON_PATH));
        private readonly BitmapImage SERVER_SETTINGS_ICON = new BitmapImage(new Uri(SERVER_SETTINGS_ICON_PATH));
        private readonly BitmapImage HTTP_CONNECTION_ICON = new BitmapImage(new Uri(HTTP_CONNECTION_ICON_PATH));
        private readonly BitmapImage DISCONNECT_ICON = new BitmapImage(new Uri(DISCONNECT_ICON_PATH));
        private readonly BitmapImage CONNECTION_OFFLINE_ICON = new BitmapImage(new Uri(CONNECTION_OFFLINE_ICON_PATH));
        private readonly BitmapImage CONNECTION_WARNING_ICON = new BitmapImage(new Uri(CONNECTION_WARNING_ICON_PATH));
        private readonly BitmapImage DATA_SERVER_ICON = new BitmapImage(new Uri(DATA_SERVER_ICON_PATH));
        private readonly BitmapImage DATABASE_ICON = new BitmapImage(new Uri(DATABASE_ICON_PATH));
        private readonly BitmapImage DELETE_DATABASE_ICON = new BitmapImage(new Uri(DELETE_DATABASE_ICON_PATH));
        private readonly BitmapImage SCRIPT_ICON = new BitmapImage(new Uri(SCRIPT_ICON_PATH));
        private readonly BitmapImage UPLOAD_SCRIPT_ICON = new BitmapImage(new Uri(UPLOAD_SCRIPT_ICON_PATH));
        private readonly BitmapImage DELETE_SCRIPT_ICON = new BitmapImage(new Uri(DELETE_SCRIPT_ICON_PATH));
        private readonly BitmapImage EXECUTE_SCRIPT_ICON = new BitmapImage(new Uri(EXECUTE_SCRIPT_ICON_PATH));

        #endregion

        private IServiceProvider Services { get; }
        private IFileProvider FileProvider { get; }
        public WebSettings WebSettings { get; }
        public TreeNodeViewModel RootNode { get; private set; }
        public HttpServicesController(IServiceProvider serviceProvider, IFileProvider fileProvider, IOptions<WebSettings> options)
        {
            Services = serviceProvider;
            FileProvider = fileProvider;
            WebSettings = options.Value;
        }
        public void SaveWebSettings()
        {
            IFileInfo fileInfo = FileProvider.GetFileInfo(WEB_SETTINGS_CATALOG_NAME);
            if (!fileInfo.Exists) { Directory.CreateDirectory(fileInfo.PhysicalPath); }

            fileInfo = FileProvider.GetFileInfo($"{WEB_SETTINGS_CATALOG_NAME}/{WEB_SETTINGS_FILE_NAME}");
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(WebSettings, options);
            using (StreamWriter writer = new StreamWriter(fileInfo.PhysicalPath, false, Encoding.UTF8))
            {
                writer.Write(json);
            }
        }
        public void CreateScriptNode(WebServer webServer, DatabaseServer server, DatabaseInfo database, MetaScript script)
        {
            if (RootNode == null) return;
            foreach (TreeNodeViewModel webServerNode in RootNode.TreeNodes)
            {
                if (webServerNode.NodePayload != webServer) continue;

                DatabaseServer dataServer = webServer.DatabaseServers.Where(s => s.Identity == server.Identity).FirstOrDefault();
                if (dataServer == null)
                {
                    dataServer = new DatabaseServer()
                    {
                        Name = server.Name,
                        Identity = server.Identity
                    };
                    DatabaseInfo newDatabase = new DatabaseInfo()
                    {
                        Name = database.Name,
                        Identity = database.Identity,
                        Scripts = new List<MetaScript>() { script }
                    };
                    dataServer.Databases.Add(newDatabase);
                    webServer.DatabaseServers.Add(dataServer);

                    TreeNodeViewModel serverNode = CreateDatabaseServerNode(webServerNode, dataServer);
                    webServerNode.TreeNodes.Add(serverNode);
                    TreeNodeViewModel databaseNode = CreateDatabaseNode(serverNode, newDatabase);
                    serverNode.TreeNodes.Add(databaseNode);
                    TreeNodeViewModel scriptNode = CreateScriptNode(databaseNode, script);
                    databaseNode.TreeNodes.Add(scriptNode);
                }
                else
                {
                    DatabaseInfo dbref = dataServer.Databases.Where(db => db.Identity == database.Identity).FirstOrDefault();
                    if (dbref == null)
                    {
                        DatabaseInfo newDatabase = new DatabaseInfo()
                        {
                            Name = database.Name,
                            Identity = database.Identity,
                            Scripts = new List<MetaScript>() { script }
                        };
                        dataServer.Databases.Add(newDatabase);

                        TreeNodeViewModel serverNode = webServerNode.TreeNodes.Where(n => n.NodePayload == dataServer).FirstOrDefault();
                        if (serverNode == null)
                        {
                            serverNode = CreateDatabaseServerNode(webServerNode, dataServer);
                            webServerNode.TreeNodes.Add(serverNode);
                        }
                        TreeNodeViewModel databaseNode = CreateDatabaseNode(serverNode, newDatabase);
                        serverNode.TreeNodes.Add(databaseNode);
                        TreeNodeViewModel scriptNode = CreateScriptNode(databaseNode, script);
                        databaseNode.TreeNodes.Add(scriptNode);
                    }
                    else
                    {
                        if (dbref.Scripts.Where(s => s == script).FirstOrDefault() == null)
                        {
                            dbref.Scripts.Add(script);
                        }

                        TreeNodeViewModel serverNode = webServerNode.TreeNodes.Where(n => n.NodePayload == dataServer).FirstOrDefault();
                        if (serverNode == null)
                        {
                            serverNode = CreateDatabaseServerNode(webServerNode, dataServer);
                            webServerNode.TreeNodes.Add(serverNode);
                        }
                        TreeNodeViewModel databaseNode = serverNode.TreeNodes.Where(n => n.NodePayload == dbref).FirstOrDefault();
                        if (databaseNode == null)
                        {
                            databaseNode = CreateDatabaseNode(serverNode, dbref);
                            serverNode.TreeNodes.Add(databaseNode);
                        }
                        TreeNodeViewModel scriptNode = databaseNode.TreeNodes.Where(n => n.NodePayload == script).FirstOrDefault();
                        if (scriptNode == null)
                        {
                            scriptNode = CreateScriptNode(databaseNode, script);
                            databaseNode.TreeNodes.Add(scriptNode);
                        }
                    }
                }
                SaveWebSettings();
                break;
            }
        }
        public WebServer SelectWebServer()
        {
            SelectWebServerWindow dialog = new SelectWebServerWindow(WebSettings.WebServers);
            _ = dialog.ShowDialog();
            return dialog.Result;
        }
        public string GetExecuteScriptUrl(WebServer webServer, DatabaseServer server, DatabaseInfo database, MetaScript script)
        {
            if (webServer == null)
            {
                return $"{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/{script.Identity.ToString().ToLower()}";
            }
            else
            {
                return $"{webServer.Address}/{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/{script.Identity.ToString().ToLower()}";
            }
        }
        public string GetActionScriptUrl(DatabaseServer server, DatabaseInfo database, MetaScript script)
        {
            return $"{server.Identity.ToString().ToLower()}/{database.Identity.ToString().ToLower()}/script/{script.Identity.ToString().ToLower()}";
        }
        public string GetActionDatabaseServerUrl(DatabaseServer server)
        {
            return $"server/{server.Identity.ToString().ToLower()}";
        }
        public string GetActionDatabaseUrl(DatabaseServer server, DatabaseInfo database)
        {
            return $"{server.Identity.ToString().ToLower()}/database/{database.Identity.ToString().ToLower()}";
        }

        public TreeNodeViewModel CreateTreeNode(TreeNodeViewModel parent) { throw new NotImplementedException(); }
        public TreeNodeViewModel CreateTreeNode()
        {
            RootNode = new TreeNodeViewModel()
            {
                Parent = null,
                IsExpanded = false,
                NodeIcon = WEB_SERVER_ICON,
                NodeText = HTTP_SERVICES_NODE_NAME,
                NodeToolTip = HTTP_SERVICES_NODE_TOOLTIP,
                NodePayload = null
            };

            RootNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add web server",
                MenuItemIcon = ADD_WEB_SERVICE_ICON,
                MenuItemCommand = new RelayCommand(AddWebServerCommand),
                MenuItemPayload = RootNode
            });

            CreateWebServerNodesFromSettings(RootNode);

            return RootNode;
        }
        private void CreateWebServerNodesFromSettings(TreeNodeViewModel rootNode)
        {
            TreeNodeViewModel serverNode;
            foreach (WebServer server in WebSettings.WebServers)
            {
                serverNode = CreateWebServerNode(rootNode, server);
                rootNode.TreeNodes.Add(serverNode);
            }
        }
        private TreeNodeViewModel CreateWebServerNode(TreeNodeViewModel parentNode, WebServer server)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                IsExpanded = false,
                NodeIcon = WEB_SERVER_ICON,
                NodeText = server.Name,
                NodeToolTip = server.Address,
                NodePayload = server
            };

            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Edit web server settings",
                MenuItemIcon = SERVER_SETTINGS_ICON,
                MenuItemCommand = new RelayCommand(EditWebServerCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Check web server",
                MenuItemIcon = HTTP_CONNECTION_ICON,
                MenuItemCommand = new RelayCommand(CheckWebServerCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Delete web server",
                MenuItemIcon = CONNECTION_OFFLINE_ICON,
                MenuItemCommand = new RelayCommand(DeleteWebServerCommand),
                MenuItemPayload = node
            });

            CreateDatabaseServerNodes(node, server);

            return node;
        }
        private void CreateDatabaseServerNodes(TreeNodeViewModel parentNode, WebServer server)
        {
            foreach (DatabaseServer dataServer in server.DatabaseServers)
            {
                TreeNodeViewModel node = CreateDatabaseServerNode(parentNode, dataServer);
                parentNode.TreeNodes.Add(node);

                CreateDatabaseNodes(node, dataServer);
            }
        }
        private TreeNodeViewModel CreateDatabaseServerNode(TreeNodeViewModel parentNode, DatabaseServer server)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                IsExpanded = false,
                NodeIcon = DATA_SERVER_ICON,
                NodeText = server.Name,
                NodeToolTip = string.Empty,
                NodePayload = server
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Delete database server at web server",
                MenuItemIcon = DISCONNECT_ICON,
                MenuItemCommand = new RelayCommand(DeleteDatabaseServerCommand),
                MenuItemPayload = node
            });
            return node;
        }
        private void CreateDatabaseNodes(TreeNodeViewModel parentNode, DatabaseServer server)
        {
            foreach (DatabaseInfo database in server.Databases)
            {
                TreeNodeViewModel node = CreateDatabaseNode(parentNode, database);
                parentNode.TreeNodes.Add(node);

                CreateScriptNodes(node, database);
            }
        }
        private TreeNodeViewModel CreateDatabaseNode(TreeNodeViewModel parentNode, DatabaseInfo database)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                IsExpanded = false,
                NodeIcon = DATABASE_ICON,
                NodeText = database.Name,
                NodeToolTip = string.Empty,
                NodePayload = database
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Delete database at web server",
                MenuItemIcon = DELETE_DATABASE_ICON,
                MenuItemCommand = new RelayCommand(DeleteDatabaseCommand),
                MenuItemPayload = node
            });
            return node;
        }
        private void CreateScriptNodes(TreeNodeViewModel parentNode, DatabaseInfo database)
        {
            foreach (MetaScript script in database.Scripts)
            {
                TreeNodeViewModel node = CreateScriptNode(parentNode, script);
                parentNode.TreeNodes.Add(node);
            }
        }
        private TreeNodeViewModel CreateScriptNode(TreeNodeViewModel parentNode, MetaScript script)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                IsExpanded = false,
                NodeIcon = SCRIPT_ICON,
                NodeText = script.Name,
                NodeToolTip = string.Empty,
                NodePayload = script
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Show script URL",
                MenuItemIcon = SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(ShowScriptUrlCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Execute script at web server",
                MenuItemIcon = EXECUTE_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(ExecuteScriptCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Update script at web server",
                MenuItemIcon = UPLOAD_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(UpdateScriptCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Delete script from web server",
                MenuItemIcon = DELETE_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(DeleteScriptCommand),
                MenuItemPayload = node
            });
            return node;
        }



        private bool WebServerAddressExists(WebServer server)
        {
            foreach (WebServer existing in WebSettings.WebServers)
            {
                if (existing.Address == server.Address)
                {
                    return true;
                }
            }
            return false;
        }
        private void AddWebServerCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (treeNode.NodeText != HTTP_SERVICES_NODE_NAME) return;

            WebServerFormWindow form = new WebServerFormWindow();
            if (!form.ShowDialog().Value) return;
            WebServer server = form.Result;

            if (string.IsNullOrWhiteSpace(server.Name))
            {
                _ = MessageBox.Show("Не указано имя web сервера!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(server.Address))
            {
                _ = MessageBox.Show("Не указан адрес web сервера!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (WebServerAddressExists(server))
            {
                _ = MessageBox.Show($"Web сервер \"{server.Address}\" уже добавлен!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            WebSettings.WebServers.Add(server);
            SaveWebSettings();

            TreeNodeViewModel serverNode = CreateWebServerNode(treeNode, server);
            treeNode.TreeNodes.Add(serverNode);
            treeNode.IsExpanded = true;
            serverNode.IsSelected = true;
        }
        private void EditWebServerCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is WebServer server)) return;

            // make copy of server settings to rollback changes if needed
            WebServer serverCopy = server.Copy();

            // edit server settings
            WebServerFormWindow form = new WebServerFormWindow(serverCopy);
            if (!form.ShowDialog().Value) return;

            if (string.IsNullOrWhiteSpace(serverCopy.Name))
            {
                _ = MessageBox.Show("Не указано имя web сервера!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(serverCopy.Address))
            {
                _ = MessageBox.Show("Не указан адрес web сервера!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // check if new server address already exists
            if (serverCopy.Address != server.Address)
            {
                if (WebServerAddressExists(serverCopy))
                {
                    _ = MessageBox.Show($"Web сервер \"{serverCopy.Address}\" уже добавлен!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // persist server settings changes
            serverCopy.CopyTo(server);
            SaveWebSettings();

            // show server name and address changes in UI
            treeNode.NodeText = server.Name;
            treeNode.NodeToolTip = server.Address;
        }
        private async void CheckWebServerCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is WebServer server)) return;

            IHttpClientFactory http = Services.GetService<IHttpClientFactory>();
            var client = http.CreateClient(server.Name);
            if (client.BaseAddress == null)
            {
                client.BaseAddress = new Uri(server.Address);
            }
            try
            {
                HttpResponseMessage response = await client.GetAsync("ping");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    treeNode.NodeIcon = WEB_SERVER_ICON;
                    _ = MessageBox.Show("Connection is Ok.", server.Name);
                }
                else
                {
                    treeNode.NodeIcon = CONNECTION_WARNING_ICON;
                    _ = MessageBox.Show(((int)response.StatusCode).ToString() + " (" + response.StatusCode.ToString() + "): " + response.ReasonPhrase, server.Name);
                }
            }
            catch (Exception ex)
            {
                treeNode.NodeIcon = CONNECTION_WARNING_ICON;
                _ = MessageBox.Show(ex.Message, server.Name);
            }
        }
        private void DeleteWebServerCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is WebServer server)) return;

            MessageBoxResult result = MessageBox.Show("Удалить web сервер \"" + server.Name + "\" ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            WebSettings.WebServers.Remove(server);
            SaveWebSettings();

            IFileInfo catalog = FileProvider.GetFileInfo($"{WEB_SETTINGS_CATALOG_NAME}/{server.Identity.ToString().ToLower()}");
            if (catalog.Exists)
            {
                Directory.Delete(catalog.PhysicalPath, true);
            }

            treeNode.Parent.TreeNodes.Remove(treeNode);
        }



        private void ShowScriptUrlCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is MetaScript script)) return;

            WebServer webServer = treeNode.GetAncestorPayload<WebServer>();
            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            string url = GetExecuteScriptUrl(webServer, server, database, script);

            ScriptingController controller = Services.GetService<ScriptingController>();
            string sourceCode = controller.ReadScriptSourceCode(server, database, MetaScriptType.Script, script.Name);

            IMetadataService metadata = Services.GetService<IMetadataService>();
            metadata.AttachDatabase(string.IsNullOrWhiteSpace(server.Address) ? server.Name : server.Address, database);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            TSqlFragment syntaxTree = scripting.ParseScript(sourceCode, out IList<ParseError> errors);
            if (errors.Count > 0) { ShowParseErrors(errors); return; }

            DeclareVariableStatementVisitor visitor = new DeclareVariableStatementVisitor();
            syntaxTree.Accept(visitor);
            string jsonDTO = visitor.GenerateJsonParametersObject();

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorViewModel editor = Services.GetService<ScriptEditorViewModel>();
            editor.Name = $"{script.Name} (URL)";
            editor.ScriptCode = url + Environment.NewLine + jsonDTO;
            ScriptEditorView scriptView = new ScriptEditorView() { DataContext = editor };
            mainWindow.AddNewTab(editor.Name, scriptView);
        }
        private void ExecuteScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is MetaScript script)) return;

            WebServer webServer = treeNode.GetAncestorPayload<WebServer>();
            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            string url = GetExecuteScriptUrl(null, server, database, script);

            //ScriptingController controller = Services.GetService<ScriptingController>();
            //string sourceCode = controller.ReadScriptSourceCode(server, database, MetaScriptType.Script, script.Name);

            //IMetadataService metadata = Services.GetService<IMetadataService>();
            //metadata.AttachDatabase(string.IsNullOrWhiteSpace(server.Address) ? server.Name : server.Address, database);

            //IScriptingService scripting = Services.GetService<IScriptingService>();
            //TSqlFragment syntaxTree = scripting.ParseScript(sourceCode, out IList<ParseError> errors);
            //if (errors.Count > 0) { ShowParseErrors(errors); return; }

            //DeclareVariableStatementVisitor visitor = new DeclareVariableStatementVisitor();
            //syntaxTree.Accept(visitor);

            string requestJson = string.Empty;
            StringContent body = new StringContent(requestJson, Encoding.UTF8, "application/json");

            IHttpClientFactory http = Services.GetService<IHttpClientFactory>();
            HttpClient client = http.CreateClient(webServer.Name);
            if (client.BaseAddress == null) { client.BaseAddress = new Uri(webServer.Address); }

            try
            {
                var response = client.PostAsync(url, body).Result;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
                    ScriptEditorViewModel editor = Services.GetService<ScriptEditorViewModel>();
                    editor.Name = $"{script.Name} (WEB response)";
                    string responseJson = response.Content.ReadAsStringAsync().Result;
                    editor.ScriptCode = url + Environment.NewLine + responseJson;
                    ScriptEditorView scriptView = new ScriptEditorView() { DataContext = editor };
                    mainWindow.AddNewTab(editor.Name, scriptView);
                }
                else
                {
                    ShowHttpError(script.Name + " (error)", response);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, script.Name);
            }
        }
        private void UpdateScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is MetaScript script)) return;

            MessageBoxResult result = MessageBox.Show("Обновить скрипт \"" + script.Name + "\" ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            WebServer webServer = treeNode.GetAncestorPayload<WebServer>();
            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            string url = GetActionScriptUrl(server, database, script);

            ScriptingController controller = Services.GetService<ScriptingController>();
            byte[] bytes = controller.ReadScriptAsBytes(server, database, MetaScriptType.Script, script.Name);
            script.SourceCode = Convert.ToBase64String(bytes);

            JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
            string requestJson = JsonSerializer.Serialize(script, options);
            StringContent body = new StringContent(requestJson, Encoding.UTF8, "application/json");
            script.SourceCode = string.Empty; // we do not need source code any more

            IHttpClientFactory http = Services.GetService<IHttpClientFactory>();
            HttpClient client = http.CreateClient(webServer.Name);
            if (client.BaseAddress == null) { client.BaseAddress = new Uri(webServer.Address); }

            try
            {
                var response = client.PutAsync(url, body).Result;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _ = MessageBox.Show("Script has been updated successfully.", script.Name, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ShowHttpError(script.Name + " (error)", response);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, script.Name);
            }
        }
        private void DeleteScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is MetaScript script)) return;

            MessageBoxResult result = MessageBox.Show("Удалить скрипт \"" + script.Name + "\" ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            WebServer webServer = treeNode.GetAncestorPayload<WebServer>();
            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            string url = GetActionScriptUrl(server, database, script);

            IHttpClientFactory http = Services.GetService<IHttpClientFactory>();
            HttpClient client = http.CreateClient(webServer.Name);
            if (client.BaseAddress == null) { client.BaseAddress = new Uri(webServer.Address); }

            try
            {
                var response = client.DeleteAsync(url).Result;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    database.Scripts.Remove(script);
                    SaveWebSettings();
                    treeNode.Parent.TreeNodes.Remove(treeNode);
                }
                else
                {
                    ShowHttpError(script.Name + " (error)", response);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, script.Name);
            }
        }
        private void DeleteDatabaseCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is DatabaseInfo database)) return;

            MessageBoxResult result = MessageBox.Show("Удалить базу данных \"" + database.Name + "\" на web сервере ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            WebServer webServer = treeNode.GetAncestorPayload<WebServer>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            string url = GetActionDatabaseUrl(server, database);

            IHttpClientFactory http = Services.GetService<IHttpClientFactory>();
            HttpClient client = http.CreateClient(webServer.Name);
            if (client.BaseAddress == null) { client.BaseAddress = new Uri(webServer.Address); }

            try
            {
                var response = client.DeleteAsync(url).Result;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _ = MessageBox.Show("Database has been removed successfully.", database.Name, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ShowHttpError(database.Name + " (error)", response);
                }
                server.Databases.Remove(database);
                SaveWebSettings();
                treeNode.Parent.TreeNodes.Remove(treeNode);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, database.Name);
            }
        }
        private void DeleteDatabaseServerCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is DatabaseServer server)) return;

            MessageBoxResult result = MessageBox.Show("Удалить сервер баз данных \"" + server.Name + "\" на web сервере ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            WebServer webServer = treeNode.GetAncestorPayload<WebServer>();

            string url = GetActionDatabaseServerUrl(server);

            IHttpClientFactory http = Services.GetService<IHttpClientFactory>();
            HttpClient client = http.CreateClient(webServer.Name);
            if (client.BaseAddress == null) { client.BaseAddress = new Uri(webServer.Address); }

            try
            {
                var response = client.DeleteAsync(url).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _ = MessageBox.Show("Database server has been removed successfully.", server.Name, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ShowHttpError(server.Name + " (error)", response);
                }

                webServer.DatabaseServers.Remove(server);
                SaveWebSettings();
                treeNode.Parent.TreeNodes.Remove(treeNode);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, server.Name);
            }
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
        private void ShowHttpError(string header, HttpResponseMessage response)
        {
            string errorHeader = ((int)response.StatusCode).ToString() + " (" + response.StatusCode.ToString() + "): " + response.ReasonPhrase;
            string errorDescription = response.Content.ReadAsStringAsync().Result;

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorViewModel errorsViewModel = Services.GetService<ScriptEditorViewModel>();
            errorsViewModel.Name = header;
            errorsViewModel.ScriptCode = errorHeader + Environment.NewLine + errorDescription;
            ScriptEditorView errorsView = new ScriptEditorView() { DataContext = errorsViewModel };
            mainWindow.AddNewTab(errorsViewModel.Name, errorsView);
        }
    }
}
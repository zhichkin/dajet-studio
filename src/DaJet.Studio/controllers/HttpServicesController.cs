using DaJet.Metadata;
using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
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
        private const string CONNECTION_OFFLINE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/connection-offline.png";
        private const string CONNECTION_WARNING_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/connection-warning.png";
        private const string DATA_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/data-server.png";
        private const string DATABASE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database.png";
        private const string SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database-script.png";

        private readonly BitmapImage WEB_SERVER_ICON = new BitmapImage(new Uri(WEB_SERVER_ICON_PATH));
        private readonly BitmapImage ADD_WEB_SERVICE_ICON = new BitmapImage(new Uri(ADD_WEB_SERVICE_ICON_PATH));
        private readonly BitmapImage SERVER_SETTINGS_ICON = new BitmapImage(new Uri(SERVER_SETTINGS_ICON_PATH));
        private readonly BitmapImage HTTP_CONNECTION_ICON = new BitmapImage(new Uri(HTTP_CONNECTION_ICON_PATH));
        private readonly BitmapImage CONNECTION_OFFLINE_ICON = new BitmapImage(new Uri(CONNECTION_OFFLINE_ICON_PATH));
        private readonly BitmapImage CONNECTION_WARNING_ICON = new BitmapImage(new Uri(CONNECTION_WARNING_ICON_PATH));
        private readonly BitmapImage DATA_SERVER_ICON = new BitmapImage(new Uri(DATA_SERVER_ICON_PATH));
        private readonly BitmapImage DATABASE_ICON = new BitmapImage(new Uri(DATABASE_ICON_PATH));
        private readonly BitmapImage SCRIPT_ICON = new BitmapImage(new Uri(SCRIPT_ICON_PATH));

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
        public void CreateScriptNode(WebServer webServer, DatabaseServer server, DatabaseInfo database, string script)
        {
            if (RootNode == null) return;
            foreach (TreeNodeViewModel webServerNode in RootNode.TreeNodes)
            {
                if (webServerNode.NodePayload != webServer) continue;

                DatabaseServerReference dataServer = webServer.DatabaseServers.Where(s => s.Identity == server.Identity).FirstOrDefault();
                if (dataServer == null)
                {
                    dataServer = new DatabaseServerReference()
                    {
                        Name = server.Name,
                        Identity = server.Identity
                    };
                    DatabaseReference newDatabase = new DatabaseReference()
                    {
                        Name = database.Name,
                        Identity = database.Identity,
                        Scripts = new List<string>() { script }
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
                    DatabaseReference dbref = dataServer.Databases.Where(db => db.Identity == database.Identity).FirstOrDefault();
                    if (dbref == null)
                    {
                        DatabaseReference newDatabase = new DatabaseReference()
                        {
                            Name = database.Name,
                            Identity = database.Identity,
                            Scripts = new List<string>() { script }
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
            foreach (DatabaseServerReference dataServer in server.DatabaseServers)
            {
                TreeNodeViewModel node = CreateDatabaseServerNode(parentNode, dataServer);
                parentNode.TreeNodes.Add(node);

                CreateDatabaseNodes(node, dataServer);
            }
        }
        private TreeNodeViewModel CreateDatabaseServerNode(TreeNodeViewModel parentNode, DatabaseServerReference server)
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
            return node;
        }
        private void CreateDatabaseNodes(TreeNodeViewModel parentNode, DatabaseServerReference server)
        {
            foreach (DatabaseReference database in server.Databases)
            {
                TreeNodeViewModel node = CreateDatabaseNode(parentNode, database);
                parentNode.TreeNodes.Add(node);

                CreateScriptNodes(node, database);
            }
        }
        private TreeNodeViewModel CreateDatabaseNode(TreeNodeViewModel parentNode, DatabaseReference database)
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
            return node;
        }
        private void CreateScriptNodes(TreeNodeViewModel parentNode, DatabaseReference database)
        {
            foreach (string script in database.Scripts)
            {
                TreeNodeViewModel node = CreateScriptNode(parentNode, script);
                parentNode.TreeNodes.Add(node);
            }
        }
        private TreeNodeViewModel CreateScriptNode(TreeNodeViewModel parentNode, string script)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                IsExpanded = false,
                NodeIcon = SCRIPT_ICON,
                NodeText = script,
                NodeToolTip = string.Empty,
                NodePayload = script
            };
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
                Directory.Delete(catalog.PhysicalPath);
            }

            treeNode.Parent.TreeNodes.Remove(treeNode);
        }



        private void DeleteScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is WebServer server)) return;

            MessageBoxResult result = MessageBox.Show("Удалить скрипт \"" + server.Name + "\" ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            //WebSettings.WebServers.Remove(server);
            //SaveWebSettings();

            //IFileInfo catalog = FileProvider.GetFileInfo($"{WEB_SETTINGS_CATALOG_NAME}/{server.Identity.ToString().ToLower()}");
            //if (catalog.Exists)
            //{
            //    Directory.Delete(catalog.PhysicalPath);
            //}

            //treeNode.Parent.TreeNodes.Remove(treeNode);
        }
    }
}
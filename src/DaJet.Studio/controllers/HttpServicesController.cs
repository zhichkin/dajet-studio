using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class HttpServicesController : ITreeNodeController
    {
        private const string HTTP_SERVICES_NODE_NAME = "HTTP services";
        private const string HTTP_SERVICES_NODE_TOOLTIP = "HTTP web services";
        private const string WEB_SETTINGS_FILE_NAME = "web-settings.json";
        private const string WEB_SETTINGS_CATALOG_NAME = "web";

        private const string WEB_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/web-server.png";
        private const string ADD_WEB_SERVICE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/add-web-service.png";
        private const string SERVER_SETTINGS_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server-settings.png";

        private readonly BitmapImage WEB_SERVER_ICON = new BitmapImage(new Uri(WEB_SERVER_ICON_PATH));
        private readonly BitmapImage ADD_WEB_SERVICE_ICON = new BitmapImage(new Uri(ADD_WEB_SERVICE_ICON_PATH));
        private readonly BitmapImage SERVER_SETTINGS_ICON = new BitmapImage(new Uri(SERVER_SETTINGS_ICON_PATH));

        private IServiceProvider Services { get; }
        private IFileProvider FileProvider { get; }
        private WebSettings WebSettings { get; set; } = new WebSettings();
        public HttpServicesController(IServiceProvider serviceProvider, IFileProvider fileProvider)
        {
            Services = serviceProvider;
            FileProvider = fileProvider;
            InitializeWebSettings();
        }
        private void SaveWebSettings()
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
        private void InitializeWebSettings()
        {
            IFileInfo fileInfo = FileProvider.GetFileInfo(WEB_SETTINGS_CATALOG_NAME);
            if (!fileInfo.Exists) { Directory.CreateDirectory(fileInfo.PhysicalPath); }

            fileInfo = FileProvider.GetFileInfo($"{WEB_SETTINGS_CATALOG_NAME}/{WEB_SETTINGS_FILE_NAME}");
            if (fileInfo.Exists)
            {
                string json;
                using (StreamReader reader = new StreamReader(fileInfo.PhysicalPath, Encoding.UTF8))
                {
                    json = reader.ReadToEnd();
                }
                WebSettings = JsonSerializer.Deserialize<WebSettings>(json);
            }
            else
            {
                SaveWebSettings();
            }
        }
        public TreeNodeViewModel CreateTreeNode(TreeNodeViewModel parent) { throw new NotImplementedException(); }
        public TreeNodeViewModel CreateTreeNode()
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = null,
                IsExpanded = false,
                NodeIcon = WEB_SERVER_ICON,
                NodeText = HTTP_SERVICES_NODE_NAME,
                NodeToolTip = HTTP_SERVICES_NODE_TOOLTIP,
                NodePayload = null
            };

            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add web server",
                MenuItemIcon = ADD_WEB_SERVICE_ICON,
                MenuItemCommand = new RelayCommand(AddWebServerCommand),
                MenuItemPayload = node
            });

            CreateWebServerNodesFromSettings(node);

            return node;
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
                MenuItemIcon = SERVER_SETTINGS_ICON,
                MenuItemCommand = new RelayCommand(CheckWebServerCommand),
                MenuItemPayload = node
            });

            //foreach (DatabaseServerReference databaseServer in server.DatabaseServers)
            //{
            //    serverNode = CreateDatabaseServerNode(parentNode, databaseServer);
            //    parentNode.TreeNodes.Add(serverNode);
            //}

            return node;
        }
        private TreeNodeViewModel CreateDatabaseServerNode(TreeNodeViewModel parentNode, DatabaseServerReference server)
        {
            // TODO
            return null;
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

            string requestJson = "{ \"Param1\" : \"Hello\" }"; //JsonSerializer.Serialize(requestData);

            var body = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var client = http.CreateClient(server.Name);

            HttpResponseMessage response = await client.PostAsync("zhichkin/mdlp_demo/test_script", body);

            string responseJson = await response.Content.ReadAsStringAsync();

            _ = MessageBox.Show(responseJson);
        }
    }
}
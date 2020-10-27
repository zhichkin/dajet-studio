using DaJet.Metadata;
using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class ScriptingController : ITreeNodeController
    {
        #region "Icons and constants"

        private const string ROOT_NODE_NAME = "Scripts";
        private const string ROOT_CATALOG_NAME = "scripts";
        private const string SCRIPT_DEFAULT_NAME = "new_script.qry";

        private const string SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database-script.png";
        private const string NEW_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/new-script.png";
        private const string EDIT_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/edit-script.png";
        private const string DELETE_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/delete-script.png";
        private const string UPLOAD_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/upload-script.png";

        private readonly BitmapImage SCRIPT_ICON = new BitmapImage(new Uri(SCRIPT_ICON_PATH));
        private readonly BitmapImage NEW_SCRIPT_ICON = new BitmapImage(new Uri(NEW_SCRIPT_ICON_PATH));
        private readonly BitmapImage EDIT_SCRIPT_ICON = new BitmapImage(new Uri(EDIT_SCRIPT_ICON_PATH));
        private readonly BitmapImage DELETE_SCRIPT_ICON = new BitmapImage(new Uri(DELETE_SCRIPT_ICON_PATH));
        private readonly BitmapImage UPLOAD_SCRIPT_ICON = new BitmapImage(new Uri(UPLOAD_SCRIPT_ICON_PATH));

        #endregion

        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        private IFileProvider FileProvider { get; }
        public ScriptingController(IServiceProvider serviceProvider, IFileProvider fileProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
            FileProvider = fileProvider;
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

            CreateScriptNodesFromFileSystem(node);

            return node;
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
                MenuItemHeader = "Edit script",
                MenuItemIcon = EDIT_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(EditScriptCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Delete script",
                MenuItemIcon = DELETE_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(DeleteScriptCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Deploy script",
                MenuItemIcon = UPLOAD_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(DeployScriptCommand),
                MenuItemPayload = node
            });

            node.NodeTextPropertyChanged += NodeTextPropertyChangedHandler;

            return node;
        }
        private void CreateScriptNodesFromFileSystem(TreeNodeViewModel rootNode)
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


        private void DeployScriptCommand(object node)
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
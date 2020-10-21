using DaJet.Metadata;
using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class ScriptingController : ITreeNodeController
    {
        private const string ROOT_NODE_NAME = "Scripts";
        private const string ROOT_CATALOG_NAME = "scripts";
        private const string SCRIPT_DEFAULT_NAME = "new_script.qry";

        private const string SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database-script.png";
        private const string NEW_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/new-script.png";
        private const string EDIT_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/edit-script.png";

        private readonly BitmapImage SCRIPT_ICON = new BitmapImage(new Uri(SCRIPT_ICON_PATH));
        private readonly BitmapImage NEW_SCRIPT_ICON = new BitmapImage(new Uri(NEW_SCRIPT_ICON_PATH));
        private readonly BitmapImage EDIT_SCRIPT_ICON = new BitmapImage(new Uri(EDIT_SCRIPT_ICON_PATH));

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
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                IsExpanded = true,
                NodeIcon = SCRIPT_ICON,
                NodeText = SCRIPT_DEFAULT_NAME,
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
            return node;
        }
        private void CreateScriptNodesFromFileSystem(TreeNodeViewModel rootNode)
        {
            DatabaseInfo database = rootNode.Parent.NodePayload as DatabaseInfo;
            DatabaseServer server = rootNode.Parent.Parent.NodePayload as DatabaseServer;

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

            ScriptEditorViewModel scriptEditor = Services.GetService<ScriptEditorViewModel>();
            scriptEditor.Name = SCRIPT_DEFAULT_NAME;
            scriptEditor.IsScriptChanged = true;

            TreeNodeViewModel child = CreateScriptTreeNode(treeNode, scriptEditor);
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
    }
}
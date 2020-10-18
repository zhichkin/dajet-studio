using DaJet.Messaging;
using DaJet.Metadata;
using DaJet.Studio.MVVM;
using DaJet.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class DataServersNodeController : ITreeNodeController
    {
        private const string SCRIPTS_NODE_NAME = "Scripts";

        #region " Icons "
        private const string ADD_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/add-server.png";
        private const string SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server.png";
        private const string DATA_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/data-server.png";
        private const string SERVER_WARNING_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server-warning.png";
        private const string DATABASE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database.png";
        private const string NAMESPACE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/УстановитьИнтервал.png";
        private const string CATALOG_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/Справочник.png";
        private const string DOCUMENT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/Документ.png";
        private const string NESTED_TABLE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/ВложеннаяТаблица.png";
        private const string PROPERTY_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/Реквизит.png";
        private const string MEASURE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/Ресурс.png";
        private const string DIMENSION_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/Измерение.png";
        private const string ENUM_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/Перечисление.png";
        private const string CONST_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/Константа.png";
        private const string INFO_REGISTER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/РегистрСведений.png";
        private const string ACCUM_REGISTER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/РегистрНакопления.png";
        private const string CHARACTERISTICS_REGISTER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/ПланВидовХарактеристик.png";
        private const string SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database-script.png";
        private const string NEW_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/new-script.png";

        private readonly BitmapImage ADD_SERVER_ICON = new BitmapImage(new Uri(ADD_SERVER_ICON_PATH));
        private readonly BitmapImage SERVER_ICON = new BitmapImage(new Uri(SERVER_ICON_PATH));
        private readonly BitmapImage DATA_SERVER_ICON = new BitmapImage(new Uri(DATA_SERVER_ICON_PATH));
        private readonly BitmapImage SERVER_WARNING_ICON = new BitmapImage(new Uri(SERVER_WARNING_ICON_PATH));
        private readonly BitmapImage DATABASE_ICON = new BitmapImage(new Uri(DATABASE_ICON_PATH));
        private readonly BitmapImage NAMESPACE_ICON = new BitmapImage(new Uri(NAMESPACE_ICON_PATH));
        private readonly BitmapImage CATALOG_ICON = new BitmapImage(new Uri(CATALOG_ICON_PATH));
        private readonly BitmapImage DOCUMENT_ICON = new BitmapImage(new Uri(DOCUMENT_ICON_PATH));
        private readonly BitmapImage NESTED_TABLE_ICON = new BitmapImage(new Uri(NESTED_TABLE_ICON_PATH));
        private readonly BitmapImage PROPERTY_ICON = new BitmapImage(new Uri(PROPERTY_ICON_PATH));
        private readonly BitmapImage MEASURE_ICON = new BitmapImage(new Uri(MEASURE_ICON_PATH));
        private readonly BitmapImage DIMENSION_ICON = new BitmapImage(new Uri(DIMENSION_ICON_PATH));
        private readonly BitmapImage ENUM_ICON = new BitmapImage(new Uri(ENUM_ICON_PATH));
        private readonly BitmapImage CONST_ICON = new BitmapImage(new Uri(CONST_ICON_PATH));
        private readonly BitmapImage INFO_REGISTER_ICON = new BitmapImage(new Uri(INFO_REGISTER_ICON_PATH));
        private readonly BitmapImage ACCUM_REGISTER_ICON = new BitmapImage(new Uri(ACCUM_REGISTER_ICON_PATH));
        private readonly BitmapImage CHARACTERISTICS_REGISTER_ICON = new BitmapImage(new Uri(CHARACTERISTICS_REGISTER_ICON_PATH));
        private readonly BitmapImage SCRIPT_ICON = new BitmapImage(new Uri(SCRIPT_ICON_PATH));
        private readonly BitmapImage NEW_SCRIPT_ICON = new BitmapImage(new Uri(NEW_SCRIPT_ICON_PATH));

        #endregion

        public TreeNodeViewModel RootNode { get; private set; }
        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        public DataServersNodeController(IServiceProvider serviceProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
        }
        public TreeNodeViewModel CreateTreeNode()
        {
            RootNode = new TreeNodeViewModel()
            {
                IsExpanded = true,
                NodeIcon = DATA_SERVER_ICON,
                NodeText = "Data servers",
                NodeToolTip = "SQL Server instances",
                NodePayload = this
            };
            RootNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add server",
                MenuItemIcon = ADD_SERVER_ICON,
                MenuItemCommand = new RelayCommand(AddDataServerCommand),
                MenuItemPayload = RootNode
            });

            CreateDatabaseServersFromSettings();
            InitializeDatabasesMetadata();
            CreateMetadataTreeNodes();

            return RootNode;
        }
        private void CreateDatabaseServersFromSettings()
        {
            if (RootNode == null || Settings.DatabaseServers == null || Settings.DatabaseServers.Count == 0)
            {
                return;
            }

            TreeNodeViewModel serverNode;
            foreach (DatabaseServer server in Settings.DatabaseServers)
            {
                serverNode = CreateServerTreeNode(server.Name, false);
                serverNode.NodePayload = server;

                TreeNodeViewModel databaseNode;
                foreach (DatabaseInfo database in server.Databases)
                {
                    databaseNode = CreateDatabaseTreeNode(database.Name);
                    databaseNode.NodePayload = database;
                    databaseNode.NodeToolTip = database.Alias;
                    serverNode.TreeNodes.Add(databaseNode);
                }
            }
        }
        private void InitializeDatabasesMetadata()
        {
            foreach (DatabaseServer server in Settings.DatabaseServers)
            {
                //_ = Parallel.ForEach(server.Databases, InitializeMetadata);
                foreach (DatabaseInfo database in server.Databases)
                {
                    InitializeMetadata(server, database);
                }
            }
        }
        private async void InitializeMetadata(DatabaseServer server, DatabaseInfo database)
        {
            IMetadataService metadata = Services.GetService<IMetadataService>();
            IMetadataProvider provider = metadata.GetMetadataProvider(database);
            provider.UseServer(server);
            provider.UseDatabase(database);
            provider.InitializeMetadata(database);
            //await Task.Run(() => provider.InitializeMetadata(database));
        }

        private BitmapImage GetMetaObjectIcon(BaseObject baseObject)
        {
            if (baseObject == null) { return null; }
            else if (baseObject.Name == "Reference") { return CATALOG_ICON; }
            else if (baseObject.Name == "Document") { return DOCUMENT_ICON; }
            else if (baseObject.Name == "InfoRg") { return INFO_REGISTER_ICON; }
            else if (baseObject.Name == "Chrc") { return CHARACTERISTICS_REGISTER_ICON; }
            else if (baseObject.Name == "Enum") { return ENUM_ICON; }
            else { return null; }
        }
        private BitmapImage GetMetaPropertyIcon(MetaProperty property)
        {
            if (property == null) { return null; }
            else if (property.Purpose == MetaPropertyPurpose.Property) { return PROPERTY_ICON; }
            else if (property.Purpose == MetaPropertyPurpose.Dimension) { return DIMENSION_ICON; }
            else if (property.Purpose == MetaPropertyPurpose.Measure) { return MEASURE_ICON; }
            else { return PROPERTY_ICON; }
        }

        private TreeNodeViewModel CreateServerTreeNode(string serverName, bool warning)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = (warning) ? SERVER_WARNING_ICON : SERVER_ICON,
                NodeText = serverName,
                NodeToolTip = (warning) ? "connection might be broken" : "connection is ok",
                NodePayload = null
            };
            RootNode.TreeNodes.Add(node);

            return node;
        }
        private TreeNodeViewModel CreateDatabaseTreeNode(string databaseName)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = DATABASE_ICON,
                NodeText = databaseName,
                NodeToolTip = string.Empty,
                NodePayload = null
            };
            node.TreeNodes.Add(CreateScriptsTreeNode());
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add new script",
                MenuItemIcon = NEW_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(AddScriptNodeCommand),
                MenuItemPayload = node
            });
            return node;
        }
        private TreeNodeViewModel CreateScriptsTreeNode()
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = SCRIPT_ICON,
                NodeText = "Scripts",
                NodeToolTip = "SQL scripts",
                NodePayload = null
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add new script",
                MenuItemIcon = NEW_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(AddScriptNodeCommand),
                MenuItemPayload = node
            });
            return node;
        }
        private void CreateMetadataTreeNodes()
        {
            foreach (TreeNodeViewModel serverNode in RootNode.TreeNodes)
            {
                foreach (TreeNodeViewModel databaseNode in serverNode.TreeNodes)
                {
                    InitializeDatabaseTreeNodes(databaseNode);
                }
            }
        }
        
        private void InitializeDatabaseTreeNodes(TreeNodeViewModel databaseNode)
        {
            if (!(databaseNode.NodePayload is DatabaseInfo database)) return;

            foreach (BaseObject baseObject in database.BaseObjects)
            {
                TreeNodeViewModel node = new TreeNodeViewModel()
                {
                    IsExpanded = false,
                    NodeIcon = GetMetaObjectIcon(baseObject),
                    NodeText = baseObject.Name,
                    NodeToolTip = baseObject.Name,
                    NodePayload = baseObject
                };
                databaseNode.TreeNodes.Add(node);

                InitializeMetaObjectsTreeNodes(node);
            }
        }
        private void InitializeMetaObjectsTreeNodes(TreeNodeViewModel parentNode)
        {
            List<MetaObject> metaObjects;
            if (parentNode.NodePayload is BaseObject baseObject)
            {
                metaObjects = baseObject.MetaObjects;
            }
            else if (parentNode.NodePayload is MetaObject metaObject)
            {
                metaObjects = metaObject.MetaObjects;
            }
            else
            {
                return;
            }

            foreach (MetaObject metaObject in metaObjects)
            {
                TreeNodeViewModel node = new TreeNodeViewModel()
                {
                    IsExpanded = false,
                    NodeIcon = (metaObject.Owner == null)
                                ? GetMetaObjectIcon(metaObject.Parent)
                                : NESTED_TABLE_ICON,
                    NodeText = metaObject.Name,
                    NodeToolTip = string.IsNullOrWhiteSpace(metaObject.Alias) ? metaObject.TableName : metaObject.Alias,
                    NodePayload = metaObject
                };
                parentNode.TreeNodes.Add(node);

                InitializeMetaPropertiesTreeNodes(node);
                InitializeMetaObjectsTreeNodes(node);
            }
        }
        private void InitializeMetaPropertiesTreeNodes(TreeNodeViewModel metaObjectNode)
        {
            if (!(metaObjectNode.NodePayload is MetaObject metaObject)) return;

            foreach (MetaProperty property in metaObject.Properties)
            {
                TreeNodeViewModel node = new TreeNodeViewModel()
                {
                    IsExpanded = false,
                    NodeIcon = GetMetaPropertyIcon(property),
                    NodeText = property.Name,
                    NodeToolTip = GetPropertyToolTip(property),
                    NodePayload = property
                };
                metaObjectNode.TreeNodes.Add(node);
            }
        }
        private string GetPropertyToolTip(MetaProperty property)
        {
            string toolTip = string.Empty;
            foreach (MetaField field in property.Fields)
            {
                toolTip += (string.IsNullOrEmpty(toolTip) ? string.Empty : Environment.NewLine)
                    + SqlUtility.CreateTableFieldScript(field).Replace("[", string.Empty).Replace("]", string.Empty);
            }
            return toolTip;
        }



        private void AddDataServerCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (treeNode.NodePayload != this) return;

            // get sql server address
            SelectServerDialogWindow dialog = new SelectServerDialogWindow();
            _ = dialog.ShowDialog();
            if (string.IsNullOrWhiteSpace(dialog.Result)) return;

            // TODO: check if serverName is allready exists

            // setup server connection
            IMessagingService messaging = Services.GetService<IMessagingService>();
            messaging.UseServer(dialog.Result);

            // check connection
            string errorMessage;
            if (messaging.CheckConnection(out errorMessage))
            {
                MessageBox.Show("Соединение открыто успешно." + Environment.NewLine + messaging.ConnectionString,
                    "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Соединение недоступно!"
                    + Environment.NewLine + messaging.ConnectionString
                    + Environment.NewLine + Environment.NewLine + errorMessage,
                    "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // TODO: add server to tree view
            _ = CreateServerTreeNode(dialog.Result, !string.IsNullOrEmpty(errorMessage));

            // TODO: save server to settings

        }


        private void AddScriptNodeCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;

            ITreeNodeController controller = Services.GetService<ScriptingController>();
            if (controller == null) return;

            if (treeNode.NodeText != SCRIPTS_NODE_NAME)
            {
                treeNode = treeNode.TreeNodes.Where(n => n.NodeText == SCRIPTS_NODE_NAME).FirstOrDefault();
            }
            if (treeNode != null)
            {
                TreeNodeViewModel child = controller.CreateTreeNode();
                treeNode.IsExpanded = true;
                treeNode.TreeNodes.Add(child);
                child.IsSelected = true;
            }
        }
    }
}
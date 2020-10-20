using DaJet.Messaging;
using DaJet.Metadata;
using DaJet.Studio.MVVM;
using DaJet.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class MetadataController : ITreeNodeController
    {
        #region " Icons "

        private const string ADD_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/add-server.png";
        private const string SERVER_SETTINGS_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server-settings.png";
        private const string SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server.png";
        private const string DATA_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/data-server.png";
        private const string SERVER_WARNING_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server-warning.png";
        private const string DATABASE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database.png";
        private const string ADD_DATABASE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/add-database.png";
        private const string DATABASE_SETTINGS_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database-settings.png";
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

        private readonly BitmapImage ADD_SERVER_ICON = new BitmapImage(new Uri(ADD_SERVER_ICON_PATH));
        private readonly BitmapImage SERVER_SETTINGS_ICON = new BitmapImage(new Uri(SERVER_SETTINGS_ICON_PATH));
        private readonly BitmapImage SERVER_ICON = new BitmapImage(new Uri(SERVER_ICON_PATH));
        private readonly BitmapImage DATA_SERVER_ICON = new BitmapImage(new Uri(DATA_SERVER_ICON_PATH));
        private readonly BitmapImage SERVER_WARNING_ICON = new BitmapImage(new Uri(SERVER_WARNING_ICON_PATH));
        private readonly BitmapImage DATABASE_ICON = new BitmapImage(new Uri(DATABASE_ICON_PATH));
        private readonly BitmapImage ADD_DATABASE_ICON = new BitmapImage(new Uri(ADD_DATABASE_ICON_PATH));
        private readonly BitmapImage DATABASE_SETTINGS_ICON = new BitmapImage(new Uri(DATABASE_SETTINGS_ICON_PATH));
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

        #endregion

        private const string METADATA_CATALOG_NAME = "metadata";
        private const string METADATA_SETTINGS_FILE_NAME = "metadata-settings.json";

        public TreeNodeViewModel RootNode { get; private set; }
        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        private IFileProvider FileProvider { get; }
        private MetadataServiceSettings MetadataSettings { get; set; } = new MetadataServiceSettings();
        public MetadataController(IServiceProvider serviceProvider, IFileProvider fileProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
            FileProvider = fileProvider;
            InitializeMetadataServiceSettings();
        }
        private void SaveMetadataServiceSettings()
        {
            IFileInfo fileInfo = FileProvider.GetFileInfo($"{METADATA_CATALOG_NAME}/{METADATA_SETTINGS_FILE_NAME}");

            JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
            string json = JsonSerializer.Serialize(MetadataSettings, options);
            using (StreamWriter writer = new StreamWriter(fileInfo.PhysicalPath, false, Encoding.UTF8))
            {
                writer.Write(json);
            }
        }
        private void InitializeMetadataServiceSettings()
        {
            IFileInfo fileInfo = FileProvider.GetFileInfo(METADATA_CATALOG_NAME);
            if (!fileInfo.Exists) { Directory.CreateDirectory(fileInfo.PhysicalPath); }

            string json = "{}";
            fileInfo = FileProvider.GetFileInfo($"{METADATA_CATALOG_NAME}/{METADATA_SETTINGS_FILE_NAME}");
            if (fileInfo.Exists)
            {
                using (StreamReader reader = new StreamReader(fileInfo.PhysicalPath, Encoding.UTF8))
                {
                    json = reader.ReadToEnd();
                }
                MetadataSettings = JsonSerializer.Deserialize<MetadataServiceSettings>(json);
            }
            else
            {
                SaveMetadataServiceSettings();
            }
        }
        public TreeNodeViewModel CreateTreeNode(TreeNodeViewModel parent) { throw new NotImplementedException(); }
        public TreeNodeViewModel CreateTreeNode()
        {
            RootNode = new TreeNodeViewModel()
            {
                IsExpanded = true,
                NodeIcon = DATA_SERVER_ICON,
                NodeText = "Metadata",
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
            if (RootNode == null || MetadataSettings.Servers == null || MetadataSettings.Servers.Count == 0)
            {
                return;
            }

            TreeNodeViewModel serverNode;
            foreach (DatabaseServer server in MetadataSettings.Servers)
            {
                serverNode = CreateServerTreeNode(server, false);

                TreeNodeViewModel databaseNode;
                foreach (DatabaseInfo database in server.Databases)
                {
                    databaseNode = CreateDatabaseTreeNode(serverNode, database);
                    serverNode.TreeNodes.Add(databaseNode);
                }
            }
        }
        private void InitializeDatabasesMetadata()
        {
            foreach (DatabaseServer server in MetadataSettings.Servers)
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

        private TreeNodeViewModel CreateServerTreeNode(DatabaseServer server, bool warning)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = (warning) ? SERVER_WARNING_ICON : SERVER_ICON,
                NodeText = string.IsNullOrWhiteSpace(server.Address)
                            ? server.Name
                            : $"{server.Name} ({server.Address})",
                NodeToolTip = (warning) ? "connection might be broken" : "connection is ok",
                NodePayload = server
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Edit server settings",
                MenuItemIcon = SERVER_SETTINGS_ICON,
                MenuItemCommand = new RelayCommand(EditDataServerCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add database",
                MenuItemIcon = ADD_DATABASE_ICON,
                MenuItemCommand = new RelayCommand(AddDatabaseCommand),
                MenuItemPayload = node
            });
            RootNode.TreeNodes.Add(node);

            return node;
        }
        private TreeNodeViewModel CreateDatabaseTreeNode(TreeNodeViewModel serverNode, DatabaseInfo database)
        {
            TreeNodeViewModel databaseNode = new TreeNodeViewModel()
            {
                Parent = serverNode,
                IsExpanded = false,
                NodeIcon = DATABASE_ICON,
                NodeText = database.Name,
                NodeToolTip = database.Alias,
                NodePayload = database
            };
            databaseNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Edit database settings",
                MenuItemIcon = DATABASE_SETTINGS_ICON,
                MenuItemCommand = new RelayCommand(EditDatabaseCommand),
                MenuItemPayload = databaseNode
            });

            TreeNodeViewModel scripts = CreateScriptsTreeNode(databaseNode);
            if (scripts != null) { databaseNode.TreeNodes.Add(scripts); }

            return databaseNode;
        }
        private TreeNodeViewModel CreateScriptsTreeNode(TreeNodeViewModel databaseNode)
        {
            ITreeNodeController controller = Services.GetService<ScriptingController>();
            if (controller == null) return null;
            return controller.CreateTreeNode(databaseNode);
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



        private bool DatabaseServerExists(DatabaseServer server)
        {
            foreach (DatabaseServer existing in MetadataSettings.Servers)
            {
                if (string.IsNullOrWhiteSpace(server.Address))
                {
                    if (existing.Name == server.Name)
                    {
                        return true;
                    }
                }
                else if (existing.Address == server.Address)
                {
                    return true;
                }
            }
            return false;
        }
        private void AddDataServerCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (treeNode.NodePayload != this) return;

            // get sql server address
            ConnectSQLServerDialogWindow dialog = new ConnectSQLServerDialogWindow();
            _ = dialog.ShowDialog();
            if (dialog.Result == null) return;

            // check if server name or address is allready exists
            if (DatabaseServerExists(dialog.Result))
            {
                MessageBox.Show("SQL сервер " + dialog.Result.Name + " уже добавлен.",
                    "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // setup server connection
            IMessagingService messaging = Services.GetService<IMessagingService>();
            messaging.UseServer(
                string.IsNullOrWhiteSpace(dialog.Result.Address)
                ? dialog.Result.Name
                : dialog.Result.Address);

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

            MetadataSettings.Servers.Add(dialog.Result);
            SaveMetadataServiceSettings();

            TreeNodeViewModel serverNode = CreateServerTreeNode(dialog.Result, !string.IsNullOrEmpty(errorMessage));
            serverNode.IsSelected = true;
        }
        private void EditDataServerCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is DatabaseServer server)) return;
            
            ConnectSQLServerDialogWindow dialog = new ConnectSQLServerDialogWindow(server);
            _ = dialog.ShowDialog();
            if (dialog.Result == null) return;

            SaveMetadataServiceSettings();

            treeNode.NodeText = string.IsNullOrWhiteSpace(server.Address)
                                ? server.Name
                                : $"{server.Name} ({server.Address})";
        }
        private void AddDatabaseCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is DatabaseServer server)) return;

            // TODO: open new database form
            MessageBox.Show("New database form...");
        }
        private void EditDatabaseCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is DatabaseInfo database)) return;

            // TODO: open database edit form
            MessageBox.Show("Database edit form...");

            //SaveMetadataServiceSettings();

            //treeNode.NodeText = database.Name;
        }
    }
}
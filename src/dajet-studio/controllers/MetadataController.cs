﻿using DaJet.Data;
using DaJet.Data.Mapping;
using DaJet.Json;
using DaJet.Metadata;
using DaJet.Metadata.Model;
using DaJet.Studio.MVVM;
using DaJet.Studio.UI;
using DaJet.UI;
using DaJet.UI.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class MetadataController : ITreeNodeController
    {
        #region " Icons "

        private readonly BitmapImage INDEX_ICON = new BitmapImage(new Uri(INDEX_ICON_PATH));
        private const string INDEX_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/clustered-index.png";

        private const string RABBITMQ_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/rabbitmq.png";
        private const string ADD_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/add-server.png";
        private const string SERVER_SETTINGS_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server-settings.png";
        private const string SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server.png";
        private const string DATA_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/data-server.png";
        private const string SERVER_WARNING_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server-warning.png";
        private const string DATABASE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database.png";
        private const string DELETE_DATABASE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/delete-database.png";
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
        private const string METADATA_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/metadata.png";
        private const string SAVE_FILE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/save-file.png";
        private const string KEY_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/key.png";
        private const string SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database-script.png";

        private readonly BitmapImage SCRIPT_ICON = new BitmapImage(new Uri(SCRIPT_ICON_PATH));
        private readonly BitmapImage RABBITMQ_ICON = new BitmapImage(new Uri(RABBITMQ_ICON_PATH));
        private readonly BitmapImage ADD_SERVER_ICON = new BitmapImage(new Uri(ADD_SERVER_ICON_PATH));
        private readonly BitmapImage SERVER_SETTINGS_ICON = new BitmapImage(new Uri(SERVER_SETTINGS_ICON_PATH));
        private readonly BitmapImage SERVER_ICON = new BitmapImage(new Uri(SERVER_ICON_PATH));
        private readonly BitmapImage DATA_SERVER_ICON = new BitmapImage(new Uri(DATA_SERVER_ICON_PATH));
        private readonly BitmapImage SERVER_WARNING_ICON = new BitmapImage(new Uri(SERVER_WARNING_ICON_PATH));
        private readonly BitmapImage DATABASE_ICON = new BitmapImage(new Uri(DATABASE_ICON_PATH));
        private readonly BitmapImage DELETE_DATABASE_ICON = new BitmapImage(new Uri(DELETE_DATABASE_ICON_PATH));
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
        private readonly BitmapImage METADATA_ICON = new BitmapImage(new Uri(METADATA_ICON_PATH));
        private readonly BitmapImage SAVE_FILE_ICON = new BitmapImage(new Uri(SAVE_FILE_ICON_PATH));
        private readonly BitmapImage KEY_ICON = new BitmapImage(new Uri(KEY_ICON_PATH));

        #endregion
                
        public TreeNodeViewModel RootNode { get; private set; }
        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        public MetadataController(IServiceProvider serviceProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
        }
        
        public TreeNodeViewModel CreateTreeNode(TreeNodeViewModel parent) { throw new NotImplementedException(); }
        private BitmapImage GetNamespaceIcon(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }
            else if (name == "Константы") { return CONST_ICON; }
            else if (name == "Справочники") { return CATALOG_ICON; }
            else if (name == "Документы") { return DOCUMENT_ICON; }
            else if (name == "Перечисления") { return ENUM_ICON; }
            else if (name == "Планы видов характеристик") { return CHARACTERISTICS_REGISTER_ICON; }
            else if (name == "Регистры сведений") { return INFO_REGISTER_ICON; }
            else if (name == "Регистры накопления") { return ACCUM_REGISTER_ICON; }
            return null;
        }
        private BitmapImage GetMetaObjectIcon(ApplicationObject metaObject)
        {
            if (metaObject == null) { return null; }
            else if (metaObject is Catalog) { return CATALOG_ICON; }
            else if (metaObject is Document) { return DOCUMENT_ICON; }
            else if (metaObject is Enumeration) { return ENUM_ICON; }
            else if (metaObject is Characteristic) { return CHARACTERISTICS_REGISTER_ICON; }
            else if (metaObject is InformationRegister) { return INFO_REGISTER_ICON; }
            else if (metaObject is AccumulationRegister) { return ACCUM_REGISTER_ICON; }
            return null;
        }
        private BitmapImage GetMetaPropertyIcon(MetadataProperty property)
        {
            if (property == null) { return null; }
            else if (property.Purpose == PropertyPurpose.System && property.IsPrimaryKey()) { return KEY_ICON; }
            else if (property.Purpose == PropertyPurpose.Property && property.IsPrimaryKey()) { return KEY_ICON; }
            else if (property.Purpose == PropertyPurpose.Property) { return PROPERTY_ICON; }
            else if (property.Purpose == PropertyPurpose.Dimension) { return DIMENSION_ICON; }
            else if (property.Purpose == PropertyPurpose.Measure) { return MEASURE_ICON; }
            return PROPERTY_ICON;
        }
        private string GetMetaPropertyToolTip(MetadataProperty property)
        {
            string toolTip = string.Empty;
            foreach (DatabaseField field in property.Fields)
            {
                toolTip += (string.IsNullOrEmpty(toolTip) ? string.Empty : Environment.NewLine) + GetDbFieldDescription(field);
            }
            return toolTip;
        }
        private string GetDbFieldDescription(DatabaseField field)
        {
            if (field.TypeName == "numeric")
            {
                return $"{field.Name} numeric({field.Scale}, {field.Precision}) {(field.IsNullable ? "NULL" : "NOT NULL")}";
            }
            else if (field.TypeName == "binary")
            {
                return $"{field.Name} binary({field.Length}) {(field.IsNullable ? "NULL" : "NOT NULL")}";
            }
            else if (field.TypeName == "char"
                || field.TypeName == "nchar"
                || field.TypeName == "varchar"
                || field.TypeName == "nvarchar"
                || field.TypeName == "text")
            {
                return $"{field.Name} {field.TypeName}({(field.Length > 0 ? field.Length.ToString() : "max")}) {(field.IsNullable ? "NULL" : "NOT NULL")}";
            }
            
            return $"{field.Name} {field.TypeName} {(field.IsNullable ? "NULL" : "NOT NULL")}";
        }

        #region "Filter Tree View"

        public void Search(string filter)
        {
            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo("ru-RU");
            }
            catch (CultureNotFoundException)
            {
                culture = CultureInfo.CurrentUICulture;
            }

            if (string.IsNullOrWhiteSpace(filter))
            {
                ClearFilter(RootNode.TreeNodes);
            }
            else
            {
                FilterNodes(RootNode.TreeNodes, filter, culture);
            }
        }
        private void FilterNodes(IEnumerable<TreeNodeViewModel> nodes, string filter, CultureInfo culture)
        {
            foreach (TreeNodeViewModel node in nodes)
            {
                FilterNodes(node.TreeNodes, filter, culture);

                if (node.NodePayload is ApplicationObject item && !(node.NodePayload is TablePart))
                {
                    node.IsVisible = culture.CompareInfo.IndexOf(item.Name, filter, CompareOptions.IgnoreCase) >= 0;
                }
                else if (node.NodePayload is ScriptEditorViewModel script)
                {
                    node.IsVisible = culture.CompareInfo.IndexOf(script.Name, filter, CompareOptions.IgnoreCase) >= 0;
                }

                if (node.NodePayload == this ||
                    node.NodePayload is DatabaseServer ||
                    node.NodePayload is DatabaseInfo ||
                    node.NodePayload is string) // Пространства имён 1С: Справочники, Документы и т.д.
                {
                    node.IsExpanded = true;
                }
                else
                {
                    node.IsExpanded = false;
                }
            }
        }
        private void ClearFilter(IEnumerable<TreeNodeViewModel> nodes)
        {
            foreach (TreeNodeViewModel node in nodes)
            {
                ClearFilter(node.TreeNodes);

                node.IsVisible = true;

                if (node.NodePayload == this ||
                    node.NodePayload is DatabaseServer ||
                    node.NodePayload is DatabaseInfo)
                {
                    node.IsExpanded = true;
                }
                else
                {
                    node.IsExpanded = false;
                }
            }
        }

        #endregion

        #region "Metadata Explorer Root Tree Node"

        public TreeNodeViewModel CreateTreeNode(MainWindowViewModel parent)
        {
            RootNode = new TreeNodeViewModel()
            {
                IsExpanded = true,
                NodeIcon = DATA_SERVER_ICON,
                NodeText = "SQL servers",
                NodeToolTip = "SQL Server instances",
                NodePayload = this
            };
            RootNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add server",
                MenuItemIcon = ADD_SERVER_ICON,
                MenuItemCommand = new RelayCommand(AddServerNodeCommand),
                MenuItemPayload = RootNode
            });

            return RootNode;
        }
        private void AddServerNodeCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (treeNode.NodePayload != this) return;

            ConnectSQLServerDialogWindow dialog = new ConnectSQLServerDialogWindow();
            _ = dialog.ShowDialog();
            if (dialog.Result == null) return;

            TreeNodeViewModel serverNode = CreateServerTreeNode(dialog.Result, false);
            serverNode.IsSelected = true;
            RootNode.TreeNodes.Add(serverNode);
        }

        #endregion

        #region "Server Tree Node"

        private TreeNodeViewModel CreateServerTreeNode(DatabaseServer server, bool warning)
        {
            TreeNodeViewModel serverNode = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = (warning) ? SERVER_WARNING_ICON : SERVER_ICON,
                NodeText = string.IsNullOrWhiteSpace(server.Address)
                            ? server.Name
                            : $"{server.Name} ({server.Address})",
                NodeToolTip = (warning) ? "connection might be broken" : "connection is ok",
                NodePayload = server
            };
            serverNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add database to the list",
                MenuItemIcon = ADD_DATABASE_ICON,
                MenuItemCommand = new RelayCommand(AddDatabaseNodeCommand),
                MenuItemPayload = serverNode
            });
            serverNode.ContextMenuItems.Add(new MenuItemViewModel() { IsSeparator = true });
            serverNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Remove server from the list",
                MenuItemIcon = DELETE_DATABASE_ICON,
                MenuItemCommand = new RelayCommand(DeleteServerNodeCommand),
                MenuItemPayload = serverNode
            });

            return serverNode;
        }
        private void AddDatabaseNodeCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is DatabaseServer server)) return;

            List<DatabaseInfo> databases;
            try
            {
                databases = GetDatabases(server);
            }
            catch (Exception error)
            {
                MessageBox.Show(ExceptionHelper.GetErrorText(error),
                    "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (databases.Count == 0)
            {
                MessageBox.Show("Список выбора баз данных пуст.",
                    "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectDatabaseWindow dialog = new SelectDatabaseWindow(databases);
            _ = dialog.ShowDialog();
            if (dialog.Result == null) return;

            if (DatabaseNameExists(server, dialog.Result))
            {
                MessageBox.Show("База данных " + dialog.Result.Name + " уже добавлена.",
                    "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DatabaseFormWindow form = new DatabaseFormWindow(dialog.Result);
            _ = form.ShowDialog();

            if (DatabaseNameExists(server, dialog.Result))
            {
                MessageBox.Show("База данных " + dialog.Result.Name + " уже добавлена.",
                    "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            server.Databases.Add(dialog.Result);
            
            TreeNodeViewModel databaseNode = CreateDatabaseTreeNode(treeNode, dialog.Result);
            treeNode.TreeNodes.Add(databaseNode);
            treeNode.IsExpanded = true;
            databaseNode.IsSelected = true;

            OpenDatabaseCommand(databaseNode);
        }
        private void DeleteServerNodeCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is DatabaseServer server)) return;
            
            MessageBoxResult result = MessageBox.Show(
                "Delete server \"" + server.Name + "\" from the list ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            
            if (result != MessageBoxResult.OK) return;

            RootNode.TreeNodes.Remove(treeNode);
        }

        #endregion

        #region "Database Tree Node"

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
                MenuItemHeader = "Open database",
                MenuItemIcon = ADD_DATABASE_ICON,
                MenuItemCommand = new RelayCommand(OpenDatabaseCommand),
                MenuItemPayload = databaseNode
            });
            databaseNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Edit database settings",
                MenuItemIcon = DATABASE_SETTINGS_ICON,
                MenuItemCommand = new RelayCommand(EditDatabaseCommand),
                MenuItemPayload = databaseNode
            });
            databaseNode.ContextMenuItems.Add(new MenuItemViewModel() { IsSeparator = true });
            databaseNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Remove database from the list",
                MenuItemIcon = DELETE_DATABASE_ICON,
                MenuItemCommand = new RelayCommand(DeleteDatabaseCommand),
                MenuItemPayload = databaseNode
            });

            return databaseNode;
        }
        private bool DatabaseNameExists(DatabaseServer server, DatabaseInfo database)
        {
            foreach (DatabaseInfo existing in server.Databases)
            {
                if (existing.Name == database.Name)
                {
                    return true;
                }
            }
            return false;
        }
        private void EditDatabaseCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is DatabaseInfo database)) return;
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();
            if (server == null) return;

            // make copy of database settings to rollback changes if needed
            DatabaseInfo databaseCopy = database.Copy();

            // edit server settings
            DatabaseFormWindow dialog = new DatabaseFormWindow(databaseCopy);
            _ = dialog.ShowDialog();
            if (dialog.Result == null) return;

            string databaseCopyName = databaseCopy.Name;
            string databaseCopyAlias = databaseCopy.Alias;
            bool databaseNameChanged = (databaseCopy.Name != database.Name);

            // check if new database name already exists
            if (databaseNameChanged)
            {
                if (DatabaseNameExists(server, databaseCopy))
                {
                    MessageBox.Show("База данных " + databaseCopyName + " уже сущестует.",
                        "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            // persist database settings changes
            databaseCopy.CopyTo(database);
            
            // show new database name and alias in UI
            treeNode.NodeText = databaseCopyName;
            treeNode.NodeToolTip = databaseCopyAlias;
        }
        private void DeleteDatabaseCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is DatabaseInfo database)) return;
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();
            if (server == null) return;

            MessageBoxResult result = MessageBox.Show("Delete database \"" + database.Name + "\" ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            server.Databases.Remove(database);
            treeNode.Parent.TreeNodes.Remove(treeNode);
        }
        private void OpenDatabaseCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is DatabaseInfo database)) return;
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();
            if (server == null) return;

            OpenDatabaseNode(treeNode);
        }
        
        private void OpenDatabaseNode(TreeNodeViewModel databaseNode)
        {
            if (!(databaseNode.NodePayload is DatabaseInfo database)) return;
            DatabaseServer server = databaseNode.GetAncestorPayload<DatabaseServer>();
            if (server == null) return;

            if (database.InfoBase != null)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Database \"" + database.Name + "\" is opend.\nDo you want it to be re-opened ?",
                    "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (result != MessageBoxResult.OK)
                {
                    return;
                }

                database.InfoBase = null;
                databaseNode.TreeNodes.Clear();
                databaseNode.IsExpanded = false;
            }

            IMetadataService metadata = Services.GetService<IMetadataService>();
            if (!metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString(GetConnectionString(server, database))
                .TryOpenInfoBase(out InfoBase infoBase, out string errorMessage))
            {
                ShowErrorMessage(errorMessage);
                return;
            }

            database.InfoBase = infoBase;
            databaseNode.NodeToolTip = $"{infoBase.Name} ({infoBase.ConfigInfo.ConfigVersion})";

            TreeNodeViewModel scripts = CreateScriptsTreeNode(databaseNode);
            if (scripts != null)
            {
                databaseNode.TreeNodes.Add(scripts);
            }

            OpenMetaObjectNode(databaseNode, "Перечисления", infoBase.Enumerations, ENUM_ICON);
            OpenMetaObjectNode(databaseNode, "Справочники", infoBase.Catalogs, CATALOG_ICON);
            OpenMetaObjectNode(databaseNode, "Документы", infoBase.Documents, DOCUMENT_ICON);
            OpenMetaObjectNode(databaseNode, "Планы видов характеристик", infoBase.Characteristics, CHARACTERISTICS_REGISTER_ICON);
            OpenMetaObjectNode(databaseNode, "Регистры сведений", infoBase.InformationRegisters, INFO_REGISTER_ICON);
            OpenMetaObjectNode(databaseNode, "Регистры накопления", infoBase.AccumulationRegisters, ACCUM_REGISTER_ICON);
        }
        private void OpenMetaObjectNode(TreeNodeViewModel databaseNode, string nodeName, Dictionary<Guid, ApplicationObject> collection, BitmapImage icon)
        {
            TreeNodeViewModel parentNode = new TreeNodeViewModel()
            {
                Parent = databaseNode,
                IsExpanded = false,
                NodeIcon = GetNamespaceIcon(nodeName),
                NodeText = nodeName,
                NodeToolTip = nodeName,
                NodePayload = nodeName
            };
            databaseNode.TreeNodes.Add(parentNode);

            foreach (ApplicationObject item in collection.Values.OrderBy(i => i.Name))
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    continue;
                }

                TreeNodeViewModel node = new TreeNodeViewModel()
                {
                    Parent = parentNode,
                    IsExpanded = false,
                    NodeIcon = icon,
                    NodeText = item.Name,
                    NodeToolTip = item.TableName,
                    NodePayload = item
                };
                node.ContextMenuItems.Add(new MenuItemViewModel()
                {
                    MenuItemHeader = "Show indexes",
                    MenuItemIcon = INDEX_ICON,
                    MenuItemCommand = new RelayCommand(ShowIndexesCommand),
                    MenuItemPayload = node
                });
                node.ContextMenuItems.Add(new MenuItemViewModel()
                {
                    MenuItemHeader = "Export data to RabbitMQ",
                    MenuItemIcon = RABBITMQ_ICON,
                    MenuItemCommand = new RelayCommand(ExportDataRabbitMQCommand),
                    MenuItemPayload = node
                });
                parentNode.TreeNodes.Add(node);

                OpenMetaPropertyNode(node, item);

                foreach (TablePart tablePart in item.TableParts)
                {
                    TreeNodeViewModel tableNode = new TreeNodeViewModel()
                    {
                        Parent = node,
                        IsExpanded = false,
                        NodeIcon = NESTED_TABLE_ICON,
                        NodeText = tablePart.Name,
                        NodeToolTip = tablePart.TableName,
                        NodePayload = tablePart
                    };
                    node.TreeNodes.Add(tableNode);

                    OpenMetaPropertyNode(tableNode, tablePart);
                }

                if (item is Enumeration enumeration)
                {
                    OpenEnumerationNode(node, enumeration);
                }
            }
        }
        private void OpenMetaPropertyNode(TreeNodeViewModel parentNode, ApplicationObject metaObject)
        {
            foreach (MetadataProperty property in metaObject.Properties)
            {
                TreeNodeViewModel node = new TreeNodeViewModel()
                {
                    Parent = parentNode,
                    IsExpanded = false,
                    NodeIcon = GetMetaPropertyIcon(property),
                    NodeText = property.Name,
                    NodeToolTip = GetMetaPropertyToolTip(property),
                    NodePayload = property
                };
                parentNode.TreeNodes.Add(node);
            }
        }
        private void OpenEnumerationNode(TreeNodeViewModel parentNode, Enumeration enumeration)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                IsExpanded = false,
                NodeIcon = PROPERTY_ICON,
                NodeText = "Values",
                NodeToolTip = "Значения перечисления",
                NodePayload = enumeration
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Show enumeration values",
                MenuItemIcon = SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(ShowEnumValuesCommand),
                MenuItemPayload = node
            });
            parentNode.TreeNodes.Add(node);

            foreach (EnumValue value in enumeration.Values)
            {
                TreeNodeViewModel valueNode = new TreeNodeViewModel()
                {
                    Parent = node,
                    IsExpanded = false,
                    NodeIcon = PROPERTY_ICON,
                    NodeText = value.Name,
                    NodeToolTip = $"{value.Alias} {{{value.Uuid}}}",
                    NodePayload = value
                };
                node.TreeNodes.Add(valueNode);
            }
        }
        private void ShowEnumValuesCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is Enumeration metaObject)) return;
            if (metaObject.Values == null || metaObject.Values.Count == 0) return;

            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            TextTabViewModel viewModel = Services.GetService<TextTabViewModel>();

            for (int i = 0; i < metaObject.Values.Count; i++)
            {
                EnumValue value = metaObject.Values[i];

                viewModel.Text += $"{(i + 1)}. {value.Name} {{{value.Uuid}}} {(new Guid(SQLHelper.GetSqlUuid(value.Uuid.ToByteArray())))}{Environment.NewLine}";
            }

            TextTabView view = new TextTabView() { DataContext = viewModel };
            mainWindow.AddNewTab(metaObject.Name, view);
        }
        private void ShowIndexesCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ApplicationObject metaObject)) return;
            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            if (database == null) return;
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();
            if (server == null) return;

            string connectionString = GetConnectionString(server, database);

            try
            {
                List<IndexInfo> indexes = SQLHelper.GetIndexes(connectionString, metaObject.TableName);
                SelectIndexDialog dialog = new SelectIndexDialog(metaObject, indexes);
                _ = dialog.ShowDialog();
            }
            catch (Exception error)
            {
                ShowErrorMessage(ExceptionHelper.GetErrorText(error));
            }
        }
        private void ExportDataRabbitMQCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is ApplicationObject metaObject)) return;

            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();
            if (server == null)
            {
                _ = MessageBox.Show($"Ошибка инициализации соединения СУБД.",
                    "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            if (database == null || database.InfoBase == null)
            {
                _ = MessageBox.Show($"Ошибка инициализации \"{metaObject.Name}\" метаданных.",
                    "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!(metaObject is Catalog || metaObject is Document || metaObject is InformationRegister || metaObject is AccumulationRegister))
            {
                _ = MessageBox.Show($"Metadata type \"{metaObject.Name}\" is not supported.",
                    "DaJet", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string connectionString = GetConnectionString(server, database);

            try
            {
                ShowExportDataRabbitMQView(connectionString, database.InfoBase, metaObject);
            }
            catch (Exception error)
            {
                ShowErrorMessage(ExceptionHelper.GetErrorText(error));
            }
        }

        #endregion

        #region "Scripts Tree Node"

        private TreeNodeViewModel CreateScriptsTreeNode(TreeNodeViewModel databaseNode)
        {
            ITreeNodeController controller = Services.GetService<ScriptingController>();
            if (controller == null)
            {
                return null;
            }
            return controller.CreateTreeNode(databaseNode);
        }

        #endregion

        private List<DatabaseInfo> GetDatabases(DatabaseServer server)
        {
            SqlConnectionStringBuilder helper = new SqlConnectionStringBuilder()
            {
                DataSource = server.Name
            };
            if (string.IsNullOrEmpty(server.UserName))
            {
                helper.IntegratedSecurity = true;
            }
            else
            {
                helper.UserID = server.UserName;
                helper.Password = server.Password;
            }

            List<DatabaseInfo> list = new List<DatabaseInfo>();

            using (SqlConnection connection = new SqlConnection(helper.ToString()))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                { 
                    command.CommandType = CommandType.Text;
                    command.CommandText = "SELECT [name] FROM [sys].[databases] WHERE NOT [name] IN ('master', 'tempdb', 'msdb', 'model') ORDER BY [name] ASC;";

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DatabaseInfo database = new DatabaseInfo()
                            {
                                Name = reader.GetString(0)
                            };
                            list.Add(database);
                        }
                        reader.Close();
                    }
                }
            }

            return list;
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

        private void ShowErrorMessage(string message)
        {
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            TextTabViewModel viewModel = Services.GetService<TextTabViewModel>();
            viewModel.Text = message;
            TextTabView view = new TextTabView() { DataContext = viewModel };
            mainWindow.AddNewTab("Error", view);
        }
        private void ShowSuccessMessage(string message)
        {
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            TextTabViewModel viewModel = Services.GetService<TextTabViewModel>();
            viewModel.Text = message;
            TextTabView view = new TextTabView() { DataContext = viewModel };
            mainWindow.AddNewTab("Success", view);
        }
        
        

        private void ShowExportDataRabbitMQView(string connectionString, InfoBase infoBase, ApplicationObject metaObject)
        {
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ExportDataRabbitMQViewModel viewModel = Services.GetService<ExportDataRabbitMQViewModel>();

            viewModel.InfoBase = infoBase;
            viewModel.MetaObject = metaObject;
            viewModel.SourceConnectionString = connectionString;

            if (metaObject is Catalog || metaObject is Document)
            {
                // do nothing - will be configured by view model itself
            }
            else if (metaObject is InformationRegister || metaObject is AccumulationRegister)
            {
                ConfigureExportOfRegister(viewModel);
            }

            ExportDataRabbitMQView view = new ExportDataRabbitMQView() { DataContext = viewModel };
            mainWindow.AddNewTab($"Export data to RabbitMQ", view);
        }
        private void ConfigureExportOfRegister(ExportDataRabbitMQViewModel viewModel)
        {
            bool is_recorder_register =
                viewModel.MetaObject is AccumulationRegister ||
                (viewModel.MetaObject is InformationRegister register && register.UseRecorder);

            if (is_recorder_register)
            {
                viewModel.DataMapper = new RecorderDataMapper();
            }
            else
            {
                viewModel.DataMapper = new RegisterDataMapper();
            }
            
            viewModel.DataMapper.Configure(new DataMapperOptions()
            {
                InfoBase = viewModel.InfoBase,
                MetaObject = viewModel.MetaObject,
                ConnectionString = viewModel.SourceConnectionString
            });

            if (is_recorder_register)
            {
                viewModel.JsonSerializer = new RecorderJsonSerializer((RecorderDataMapper)viewModel.DataMapper);
                viewModel.TableIndex = ((RecorderDataMapper)viewModel.DataMapper).GetPagingIndex();
            }
            else
            {
                viewModel.JsonSerializer = new RegisterJsonSerializer((RegisterDataMapper)viewModel.DataMapper);
                viewModel.TableIndex = ((RegisterDataMapper)viewModel.DataMapper).GetPagingIndex();
            }

            viewModel.TableIndexName = viewModel.TableIndex.Name;
            viewModel.ConfigureFilterTable();
        }
    }
}
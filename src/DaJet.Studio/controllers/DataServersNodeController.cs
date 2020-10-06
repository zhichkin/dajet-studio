﻿using DaJet.Messaging;
using DaJet.Metadata;
using DaJet.Studio.MVVM;
using DaJet.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class DataServersNodeController : ITreeNodeController
    {
        private const string ADD_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/add-server.png";
        private const string SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server.png";
        private const string DATA_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/data-server.png";
        private const string SERVER_WARNING_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server-warning.png";
        private const string DATABASE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database.png";

        private readonly BitmapImage ADD_SERVER_ICON = new BitmapImage(new Uri(ADD_SERVER_ICON_PATH));
        private readonly BitmapImage SERVER_ICON = new BitmapImage(new Uri(SERVER_ICON_PATH));
        private readonly BitmapImage DATA_SERVER_ICON = new BitmapImage(new Uri(DATA_SERVER_ICON_PATH));
        private readonly BitmapImage SERVER_WARNING_ICON = new BitmapImage(new Uri(SERVER_WARNING_ICON_PATH));
        private readonly BitmapImage DATABASE_ICON = new BitmapImage(new Uri(DATABASE_ICON_PATH));

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

            // TODO: add local queue menu item
            // TODO: add remote queue menu item
            // TODO: add separator menu item : IsSeparator = true
            // TODO: remove server menu item
            // TODO: check connection menu item

            //node.ContextMenuItems.Add(new MenuItemViewModel()
            //{
            //    MenuItemHeader = "Add server",
            //    MenuItemIcon = ADD_SERVER_ICON,
            //    MenuItemCommand = new RelayCommand(AddDataServerCommand),
            //    MenuItemPayload = node
            //});

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
                    NodeIcon = null,
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
                    NodeIcon = null,
                    NodeText = metaObject.Name,
                    NodeToolTip = metaObject.Alias,
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
                    NodeIcon = null,
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
                toolTip += (string.IsNullOrEmpty(toolTip) ? string.Empty : Environment.NewLine) + field.Name;
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
    }
}
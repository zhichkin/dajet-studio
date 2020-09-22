using DaJet.Messaging;
using DaJet.Studio.MVVM;
using DaJet.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class DataServersNodeController : ITreeNodeController
    {
        private const string ADD_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/add-server.png";
        private const string SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server.png";
        private const string DATA_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/data-server.png";
        private const string SERVER_WARNING_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server-warning.png";

        private readonly BitmapImage ADD_SERVER_ICON = new BitmapImage(new Uri(ADD_SERVER_ICON_PATH));
        private readonly BitmapImage SERVER_ICON = new BitmapImage(new Uri(SERVER_ICON_PATH));
        private readonly BitmapImage DATA_SERVER_ICON = new BitmapImage(new Uri(DATA_SERVER_ICON_PATH));
        private readonly BitmapImage SERVER_WARNING_ICON = new BitmapImage(new Uri(SERVER_WARNING_ICON_PATH));

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

            return RootNode;
        }
        private TreeNodeViewModel CreateServerTreeNode(string serverName, bool warning)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = (warning) ? SERVER_WARNING_ICON : SERVER_ICON,
                NodeText = serverName,
                NodeToolTip = (warning) ? "connection might be broken" : "connection is ok",
                NodePayload = this
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
using DaJet.RabbitMQ.HttpApi;
using DaJet.Studio.MVVM;
using DaJet.Studio.UI;
using DaJet.UI.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class RabbitMQController : ITreeNodeController, IErrorHandler
    {
        #region "ICONS"

        private readonly BitmapImage REMOVE_ICON = new BitmapImage(new Uri(REMOVE_ICON_PATH));
        private const string REMOVE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/remove.png";

        private readonly BitmapImage RABBITMQ_ICON = new BitmapImage(new Uri(RABBITMQ_ICON_PATH));
        private const string RABBITMQ_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/rabbitmq.png";

        private readonly BitmapImage RABBITMQ_SERVER_ICON = new BitmapImage(new Uri(RABBITMQ_SERVER_ICON_PATH));
        private const string RABBITMQ_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/web-server.png";

        private readonly BitmapImage SERVER_SETTINGS_ICON = new BitmapImage(new Uri(SERVER_SETTINGS_ICON_PATH));
        private const string SERVER_SETTINGS_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/server-settings.png";

        private readonly BitmapImage VIRTUAL_HOST_ICON = new BitmapImage(new Uri(VIRTUAL_HOST_ICON_PATH));
        private const string VIRTUAL_HOST_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/virtual-host.png";

        private readonly BitmapImage CREATE_VIRTUAL_HOST_ICON = new BitmapImage(new Uri(CREATE_VIRTUAL_HOST_ICON_PATH));
        private const string CREATE_VIRTUAL_HOST_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/create-virtual-host.png";

        private readonly BitmapImage RMQ_EXCHANGE_ICON = new BitmapImage(new Uri(RMQ_EXCHANGE_ICON_PATH));
        private const string RMQ_EXCHANGE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/rmq-exchange.png";

        private readonly BitmapImage RMQ_QUEUE_ICON = new BitmapImage(new Uri(RMQ_QUEUE_ICON_PATH));
        private const string RMQ_QUEUE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/message-queue.png";

        private readonly BitmapImage OPEN_CATALOG_ICON = new BitmapImage(new Uri(OPEN_CATALOG_ICON_PATH));
        private const string OPEN_CATALOG_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/open-catalog.png";

        #endregion

        #region "INTERFACE ITreeNodeController"

        public void Search(string text) { throw new NotImplementedException(); }
        public TreeNodeViewModel CreateTreeNode(TreeNodeViewModel parent) { throw new NotImplementedException(); }

        #endregion

        private IServiceProvider Services { get; }
        public RabbitMQController(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
        }
        public TreeNodeViewModel RootNode { get; private set; }

        #region "INTERFACE IErrorHandler"
        public void HandleError(Exception error)
        {
            _ = MessageBox.Show(ExceptionHelper.GetErrorText(error),
                "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region "ROOT NODE FACTORY"

        public TreeNodeViewModel CreateTreeNode(MainWindowViewModel parent)
        {
            RootNode = new TreeNodeViewModel()
            {
                IsExpanded = true,
                NodeIcon = RABBITMQ_ICON,
                NodeText = "RabbitMQ",
                NodeToolTip = "RabbitMQ servers",
                NodePayload = this
            };
            RootNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add local RabbitMQ server",
                MenuItemIcon = RABBITMQ_ICON,
                MenuItemCommand = new RelayCommand(AddLocalServerNodeCommand),
                MenuItemPayload = RootNode
            });
            RootNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add RabbitMQ server ...",
                MenuItemIcon = RABBITMQ_ICON,
                MenuItemCommand = new RelayCommand(AddServerNodeCommand),
                MenuItemPayload = RootNode
            });

            return RootNode;
        }
        private void AddLocalServerNodeCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (treeNode.NodePayload != this) return;

            RabbitMQServer server = new RabbitMQServer()
            {
                Description = "Local RabbitMQ server"
            };

            TreeNodeViewModel serverNode = CreateServerTreeNode(server);
            serverNode.Parent = RootNode;
            serverNode.IsSelected = true;
            RootNode.TreeNodes.Add(serverNode);
        }
        private void AddServerNodeCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (treeNode.NodePayload != this) return;

            RabbitMQServer server = new RabbitMQServer();
            RabbitMQServerForm dialog = new RabbitMQServerForm(server);
            if (!dialog.ShowDialog().Value)
            {
                return;
            }

            TreeNodeViewModel serverNode = CreateServerTreeNode(server);
            serverNode.Parent = RootNode;
            serverNode.IsSelected = true;
            RootNode.TreeNodes.Add(serverNode);
        }

        #endregion

        #region "SERVER NODE FACTORY"

        private TreeNodeViewModel CreateServerTreeNode(RabbitMQServer server)
        {
            TreeNodeViewModel serverNode = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = RABBITMQ_SERVER_ICON,
                NodeText = server.ToString(),
                NodeToolTip = server.Description,
                NodePayload = server
            };
            serverNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Edit server settings ...",
                MenuItemIcon = SERVER_SETTINGS_ICON,
                MenuItemCommand = new RelayCommand(EditServerNodeCommand),
                MenuItemPayload = serverNode
            });
            serverNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Open server node",
                MenuItemIcon = OPEN_CATALOG_ICON,
                MenuItemCommand = new AsyncRelayCommand<TreeNodeViewModel>(OpenServerNodeCommand, this),
                MenuItemPayload = serverNode
            });
            serverNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Create new virtual host ...",
                MenuItemIcon = CREATE_VIRTUAL_HOST_ICON,
                MenuItemCommand = new RelayCommand(CreateVirtualHostCommand),
                MenuItemPayload = serverNode
            });
            serverNode.ContextMenuItems.Add(new MenuItemViewModel() { IsSeparator = true });
            serverNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Remove server from list",
                MenuItemIcon = REMOVE_ICON,
                MenuItemCommand = new RelayCommand(RemoveServerNodeCommand),
                MenuItemPayload = serverNode
            });

            return serverNode;
        }
        private void EditServerNodeCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is RabbitMQServer server)) return;
            if (server == null) return;

            RabbitMQServerForm dialog = new RabbitMQServerForm(server);
            if (!dialog.ShowDialog().Value)
            {
                return;
            }

            // refresh tree node values
            treeNode.NodeText = server.ToString();
            treeNode.NodeToolTip = server.Description;
        }
        private void RemoveServerNodeCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is RabbitMQServer server)) return;
            if (server == null) return;

            MessageBoxResult result = MessageBox.Show("Remove \"" + server.ToString() + "\" from list ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            treeNode.Parent.TreeNodes.Remove(treeNode);
        }
        private void CreateVirtualHostCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is RabbitMQServer server)) return;
            if (server == null) return;

            _ = MessageBox.Show("Sorry, under construction ...",
                "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private async Task OpenServerNodeCommand(TreeNodeViewModel treeNode)
        {
            if (!(treeNode.NodePayload is RabbitMQServer server)) return;

            IRabbitMQHttpManager manager = Services.GetService<IRabbitMQHttpManager>()
                .UseHostName(server.Host)
                .UsePortNumber(server.Port)
                .UseUserName(server.UserName)
                .UsePassword(server.Password);

            treeNode.IsExpanded = false;
            treeNode.TreeNodes.Clear();
            await CreateVirtualHostNodes(treeNode, manager);
            treeNode.IsExpanded = true;
        }

        #endregion

        private TreeNodeViewModel CreateVirtualHostTreeNode(VirtualHostInfo vhost)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = VIRTUAL_HOST_ICON,
                NodeText = vhost.Name,
                NodeToolTip = vhost.Description,
                NodePayload = vhost
            };
            return node;
        }
        private TreeNodeViewModel CreateExchangeTreeNode(ExchangeInfo exchange)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = RMQ_EXCHANGE_ICON,
                NodeText = string.IsNullOrEmpty(exchange.Name) ? "(AMQP default)" : exchange.Name,
                NodeToolTip = null,
                NodePayload = exchange
            };
            return node;
        }
        private TreeNodeViewModel CreateQueueTreeNode(QueueInfo queue)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = RMQ_QUEUE_ICON,
                NodeText = queue.Name,
                NodeToolTip = null,
                NodePayload = queue
            };
            return node;
        }

        private async Task CreateVirtualHostNodes(TreeNodeViewModel root, IRabbitMQHttpManager manager)
        {
            List<VirtualHostInfo> list = await manager.GetVirtualHosts();

            foreach (VirtualHostInfo vhost in list)
            {
                TreeNodeViewModel node = CreateVirtualHostTreeNode(vhost);
                root.TreeNodes.Add(node);

                _ = manager.UseVirtualHost(vhost.Name);

                await CreateExchangeNodes(node, manager);
                await CreateQueueNodes(node, manager);
            }
        }
        private async Task CreateExchangeNodes(TreeNodeViewModel root, IRabbitMQHttpManager manager)
        {
            List<ExchangeInfo> list = await manager.GetExchanges();

            foreach (ExchangeInfo exchange in list)
            {
                TreeNodeViewModel node = CreateExchangeTreeNode(exchange);
                root.TreeNodes.Add(node);
            }
        }
        private async Task CreateQueueNodes(TreeNodeViewModel root, IRabbitMQHttpManager manager)
        {
            List<QueueInfo> list = await manager.GetQueues();

            foreach (QueueInfo queue in list)
            {
                TreeNodeViewModel node = CreateQueueTreeNode(queue);
                root.TreeNodes.Add(node);
            }
        }
    }
}
using DaJet.Messaging;
using DaJet.Metadata;
using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class MessagingController : ITreeNodeController
    {
        #region "Icons and constants"

        private const string QUEUES_NODE_NAME = "Queues";
        private const string QUEUES_NODE_TOOLTIP = "Database queues";

        private const string QUEUE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/message-queue.png";
        private const string ADD_QUEUE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/add-query.png";
        private const string EDIT_QUEUE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/edit-script.png";
        private const string DROP_QUEUE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/message-queue-error.png";
        private const string ALERT_QUEUE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/message-queue-warning.png";
        
        private readonly BitmapImage QUEUE_ICON = new BitmapImage(new Uri(QUEUE_ICON_PATH));
        private readonly BitmapImage ADD_QUEUE_ICON = new BitmapImage(new Uri(ADD_QUEUE_ICON_PATH));
        private readonly BitmapImage EDIT_QUEUE_ICON = new BitmapImage(new Uri(EDIT_QUEUE_ICON_PATH));
        private readonly BitmapImage DROP_QUEUE_ICON = new BitmapImage(new Uri(DROP_QUEUE_ICON_PATH));
        private readonly BitmapImage ALERT_QUEUE_ICON = new BitmapImage(new Uri(ALERT_QUEUE_ICON_PATH));

        #endregion

        private IServiceProvider Services { get; }
        private IFileProvider FileProvider { get; }
        public MessagingController(IServiceProvider serviceProvider, IFileProvider fileProvider)
        {
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
                NodeIcon = QUEUE_ICON,
                NodeText = QUEUES_NODE_NAME,
                NodeToolTip = QUEUES_NODE_TOOLTIP,
                NodePayload = null
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Create new queue",
                MenuItemIcon = ADD_QUEUE_ICON,
                MenuItemCommand = new RelayCommand(CreateQueueCommand),
                MenuItemPayload = node
            });

            CreateQueueNodesFromDatabase(node);

            return node;
        }
        private void CreateQueueNodesFromDatabase(TreeNodeViewModel rootNode)
        {
            DatabaseInfo database = rootNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = rootNode.GetAncestorPayload<DatabaseServer>();
            IMessagingService messaging = Services.GetService<IMessagingService>();
            ConfigureMessagingService(messaging, server, database);

            List<QueueInfo> queues = messaging.SelectQueues(out string errorMessage);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                _ = MessageBox.Show(errorMessage, "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TreeNodeViewModel queueNode;
            foreach (QueueInfo queue in queues)
            {
                queueNode = CreateQueueNode(rootNode, queue);
                rootNode.TreeNodes.Add(queueNode);
            }
        }
        private TreeNodeViewModel CreateQueueNode(TreeNodeViewModel parentNode, QueueInfo queue)
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                Parent = parentNode,
                IsExpanded = false,
                NodeIcon = queue.Status ? QUEUE_ICON : ALERT_QUEUE_ICON,
                NodeText = queue.Name,
                NodeToolTip = queue.ToString(),
                NodePayload = queue
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Edit queue settings",
                MenuItemIcon = EDIT_QUEUE_ICON,
                MenuItemCommand = new RelayCommand(EditQueueCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Drop queue",
                MenuItemIcon = DROP_QUEUE_ICON,
                MenuItemCommand = new RelayCommand(DropQueueCommand),
                MenuItemPayload = node
            });

            return node;
        }

        private void ConfigureMessagingService(IMessagingService messaging, DatabaseServer server, DatabaseInfo database)
        {
            messaging.UseServer(string.IsNullOrWhiteSpace(server.Address) ? server.Name : server.Address);
            messaging.UseDatabase(database.Name);
            messaging.UseCredentials(database.UserName, database.Password);
        }

        private void CreateQueueCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (treeNode.NodeText != QUEUES_NODE_NAME) return;

            QueueFormWindow form = new QueueFormWindow();
            if (!form.ShowDialog().Value) return;
            QueueInfo queue = form.Result;

            if (string.IsNullOrWhiteSpace(queue.Name))
            {
                _ = MessageBox.Show("Не указано имя очереди!", "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();
            IMessagingService messaging = Services.GetService<IMessagingService>();
            ConfigureMessagingService(messaging, server, database);

            if (!messaging.CreateQueue(queue, out string errorMessage))
            {
                _ = MessageBox.Show(errorMessage, "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TreeNodeViewModel queueNode = CreateQueueNode(treeNode, queue);
            treeNode.TreeNodes.Add(queueNode);
            treeNode.IsExpanded = true;
            queueNode.IsSelected = true;
        }
        private void EditQueueCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is QueueInfo queue)) return;

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            // TODO
            QueueFormWindow form = new QueueFormWindow(queue);
            if (!form.ShowDialog().Value) return;
            //QueueInfo queue = form.Result;
        }
        private void DropQueueCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is QueueInfo queue)) return;

            MessageBoxResult result = MessageBox.Show("Drop queue \"" + queue.Name + "\" ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) { return; }

            DatabaseInfo database = treeNode.GetAncestorPayload<DatabaseInfo>();
            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            // TODO
        }
    }
}
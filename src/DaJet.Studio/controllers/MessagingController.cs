using DaJet.Messaging;
using DaJet.Metadata;
using DaJet.Scripting;
using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace DaJet.Studio
{
    public sealed class MessagingController : ITreeNodeController
    {
        #region "Icons and constants"

        private const string QUEUES_NODE_NAME = "Queues";
        private const string DAJET_MQ_DATABASE_NAME = "dajet-mq";
        private const string QUEUES_NODE_TOOLTIP = "Database queues";

        private const string CREATE_DAJET_MQ_DATABASE_SCRIPT = "pack://application:,,,/DaJet.Studio;component/dajet-mq/create-dajet-mq-database.sql";
        private const string CREATE_PUBLIC_ENDPOINT_SCRIPT = "pack://application:,,,/DaJet.Studio;component/dajet-mq/create-public-end-point.sql";
        private const string DROP_DAJET_MQ_DATABASE_SCRIPT = "pack://application:,,,/DaJet.Studio;component/dajet-mq/drop-dajet-mq-database.sql";
        private const string DROP_PUBLIC_ENDPOINT_SCRIPT = "pack://application:,,,/DaJet.Studio;component/dajet-mq/drop-public-end-point.sql";

        private const string QUEUE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/message-queue.png";
        private const string ADD_QUEUE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/add-query.png";
        private const string EDIT_QUEUE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/edit-script.png";
        private const string DROP_QUEUE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/message-queue-error.png";
        private const string ALERT_QUEUE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/message-queue-warning.png";
        private const string SEND_MESSAGE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/message-send.png";
        private const string RECEIVE_MESSAGE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/message-receive.png";

        private readonly BitmapImage QUEUE_ICON = new BitmapImage(new Uri(QUEUE_ICON_PATH));
        private readonly BitmapImage ADD_QUEUE_ICON = new BitmapImage(new Uri(ADD_QUEUE_ICON_PATH));
        private readonly BitmapImage EDIT_QUEUE_ICON = new BitmapImage(new Uri(EDIT_QUEUE_ICON_PATH));
        private readonly BitmapImage DROP_QUEUE_ICON = new BitmapImage(new Uri(DROP_QUEUE_ICON_PATH));
        private readonly BitmapImage ALERT_QUEUE_ICON = new BitmapImage(new Uri(ALERT_QUEUE_ICON_PATH));
        private readonly BitmapImage SEND_MESSAGE_ICON = new BitmapImage(new Uri(SEND_MESSAGE_ICON_PATH));
        private readonly BitmapImage RECEIVE_MESSAGE_ICON = new BitmapImage(new Uri(RECEIVE_MESSAGE_ICON_PATH));

        #endregion

        private TreeNodeViewModel RootNode { get; set; }
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
            RootNode = new TreeNodeViewModel()
            {
                Parent = parent,
                IsExpanded = false,
                NodeIcon = QUEUE_ICON,
                NodeText = QUEUES_NODE_NAME,
                NodeToolTip = QUEUES_NODE_TOOLTIP,
                NodePayload = null
            };
            RootNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Create DaJet MQ",
                MenuItemIcon = ADD_QUEUE_ICON,
                MenuItemCommand = new RelayCommand(CreateDaJetMQCommand),
                MenuItemPayload = RootNode
            });
            RootNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Drop DaJet MQ",
                MenuItemIcon = DROP_QUEUE_ICON,
                MenuItemCommand = new RelayCommand(DropDaJetMQCommand),
                MenuItemPayload = RootNode
            });
            RootNode.ContextMenuItems.Add(new MenuItemViewModel() { IsSeparator = true });
            RootNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Create new queue",
                MenuItemIcon = ADD_QUEUE_ICON,
                MenuItemCommand = new RelayCommand(CreateQueueCommand),
                MenuItemPayload = RootNode
            });

            CreateQueueNodesFromDatabase(RootNode);

            return RootNode;
        }
        private void CreateQueueNodesFromDatabase(TreeNodeViewModel rootNode)
        {
            DatabaseServer server = rootNode.GetAncestorPayload<DatabaseServer>();
            IMessagingService messaging = Services.GetService<IMessagingService>();
            ConfigureMessagingService(messaging, server, null);
            if (!messaging.DaJetMQExists()) { return; }

            DatabaseInfo database = new DatabaseInfo() { Name = DAJET_MQ_DATABASE_NAME };
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
            node.ContextMenuItems.Add(new MenuItemViewModel() { IsSeparator = true });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Send test message to queue",
                MenuItemIcon = SEND_MESSAGE_ICON,
                MenuItemCommand = new RelayCommand(SendTestMessageCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Receive test message from queue",
                MenuItemIcon = RECEIVE_MESSAGE_ICON,
                MenuItemCommand = new RelayCommand(ReceiveTestMessageCommand),
                MenuItemPayload = node
            });
            node.ContextMenuItems.Add(new MenuItemViewModel() { IsSeparator = true });
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
            if (database == null)
            {
                messaging.UseDatabase(string.Empty);
                messaging.UseCredentials(server.UserName, server.Password);
            }
            else
            {
                messaging.UseDatabase(database.Name);
                messaging.UseCredentials(database.UserName, database.Password);
            }
        }
        private void ExecuteAdministrativeScript(DatabaseServer server, string scriptUri)
        {
            Uri uri = new Uri(scriptUri);
            StreamResourceInfo resource = Application.GetResourceStream(uri);

            string sql = string.Empty;
            using (StreamReader reader = new StreamReader(resource.Stream))
            {
                sql = reader.ReadToEnd();
            }

            IMetadataService metadata = Services.GetService<IMetadataService>();
            metadata.Configure(server, null);
            
            IScriptingService scripting = Services.GetService<IScriptingService>();
            scripting.ExecuteBatch(sql, out IList<ParseError> errors);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(ExceptionHelper.GetParseErrorsText(errors));
            }
        }

        private void CreateDaJetMQCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;

            MessageBoxResult result = MessageBox.Show(
                "Create DaJet MQ database ?", "DaJet",
                MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) { return; }

            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            IMessagingService messaging = Services.GetService<IMessagingService>();
            ConfigureMessagingService(messaging, server, null);

            try
            {
                if (messaging.DaJetMQExists())
                {
                    _ = MessageBox.Show("Database DaJet MQ already exists.",
                        "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ExecuteAdministrativeScript(server, CREATE_DAJET_MQ_DATABASE_SCRIPT);
                    _ = MessageBox.Show("DaJet MQ database created successfully.",
                        "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.ShowException(ex);
            }
        }
        private void DropDaJetMQCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;

            MessageBoxResult result = MessageBox.Show("Drop DaJet MQ database ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) { return; }

            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            try
            {
                ExecuteAdministrativeScript(server, DROP_DAJET_MQ_DATABASE_SCRIPT);
                RootNode.TreeNodes.Clear(); // remove all queues from UI
                _ = MessageBox.Show("DaJet MQ database has been droped successfully.",
                    "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExceptionHelper.ShowException(ex);
            }
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

            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();
            IMessagingService messaging = Services.GetService<IMessagingService>();
            ConfigureMessagingService(messaging, server, null);

            if (!messaging.TryCreateQueue(queue, out string errorMessage))
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

            _ = MessageBox.Show("Sorry, under construction.",
                "DaJet", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;

            // TODO
            //QueueFormWindow form = new QueueFormWindow(queue);
            //if (!form.ShowDialog().Value) return;
            //QueueInfo queue = form.Result;
        }
        private void DropQueueCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is QueueInfo queue)) return;

            MessageBoxResult result = MessageBox.Show("Drop queue \"" + queue.Name + "\" ?",
                "DaJet", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) { return; }

            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();
            IMessagingService messaging = Services.GetService<IMessagingService>();
            ConfigureMessagingService(messaging, server, null);

            if (!messaging.TryDeleteQueue(queue, out string errorMessage))
            {
                _ = MessageBox.Show(errorMessage, "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            treeNode.Parent.TreeNodes.Remove(treeNode);

            _ = MessageBox.Show("Queue \"" + queue.Name + "\" has been droped successfully.",
                "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SendTestMessageCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is QueueInfo queue)) return;

            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            string script = GenerateSendTestMessageScript(server, queue);

            ShowMainWindowTab(queue.Name + " (send)", script);
        }
        private void ReceiveTestMessageCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;
            if (!(treeNode.NodePayload is QueueInfo queue)) return;

            DatabaseServer server = treeNode.GetAncestorPayload<DatabaseServer>();

            string script = GenerateReceiveTestMessageScript(server, queue);

            ShowMainWindowTab(queue.Name + " (receive)", script);
        }
        private string GenerateSendTestMessageScript(DatabaseServer server, QueueInfo queue)
        {
            IMessagingService messaging = Services.GetService<IMessagingService>();
            ConfigureMessagingService(messaging, server, null);

            string queueFullName = string.Empty;
            string exceptionText = string.Empty;
            try
            {
                queueFullName = messaging.GetQueueFullName(queue.Name);
            }
            catch (Exception ex)
            {
                exceptionText = ExceptionHelper.GetErrorText(ex);
            }

            StringBuilder script = new StringBuilder();
            script.AppendLine($"USE [{DAJET_MQ_DATABASE_NAME}];");
            script.AppendLine();
            script.AppendLine("DECLARE @queueName nvarchar(80) = N'test';");
            script.AppendLine("DECLARE @messageText varchar(max) = 'This is test message';");
            script.AppendLine();
            script.AppendLine("DECLARE @queueFullName nvarchar(128);");
            script.AppendLine("DECLARE @dialogHandle uniqueidentifier;");
            script.AppendLine("DECLARE @messageBody varbinary(max);");
            script.AppendLine();
            script.AppendLine($"SET @queueFullName = [dbo].[fn_create_queue_name](@queueName);");
            script.AppendLine($"SET @dialogHandle = [dbo].[fn_get_dialog_handle](@queueFullName);");
            script.AppendLine("SET @messageBody = CAST(@messageText AS varbinary(max));");
            script.AppendLine();
            script.AppendLine($"EXEC [dbo].[sp_send_message] @dialogHandle, @messageBody;");
            script.AppendLine();
            script.AppendLine("SELECT");
            script.AppendLine("\tmessage_enqueue_time AS [enqueueTime],");
            script.AppendLine("\tCAST(message_body AS varchar(max)) AS [messageBody],");
            script.AppendLine("\tmessage_type_name AS [messageType]");
            script.AppendLine("FROM");
            script.AppendLine($"\t[{queueFullName}] WITH(NOLOCK);");

            if (!string.IsNullOrEmpty(exceptionText))
            {
                script.AppendLine();
                script.AppendLine("Exception getting full queue name:");
                script.AppendLine();
                script.AppendLine(exceptionText);
            }

            return script.ToString();
        }
        private string GenerateReceiveTestMessageScript(DatabaseServer server, QueueInfo queue)
        {
            IMessagingService messaging = Services.GetService<IMessagingService>();
            ConfigureMessagingService(messaging, server, null);

            string queueFullName = string.Empty;
            string exceptionText = string.Empty;
            try
            {
                queueFullName = messaging.GetQueueFullName(queue.Name);
            }
            catch (Exception ex)
            {
                exceptionText = ExceptionHelper.GetErrorText(ex);
            }

            StringBuilder script = new StringBuilder();
            script.AppendLine($"USE [{DAJET_MQ_DATABASE_NAME}];");
            script.AppendLine();
            script.AppendLine("DECLARE @timeout int = 1000;");
            script.AppendLine("DECLARE @message_body varchar(max);");
            script.AppendLine("DECLARE @message_type nvarchar(256);");
            script.AppendLine();
            script.AppendLine("WAITFOR");
            script.AppendLine("(RECEIVE TOP(1)");
            script.AppendLine("\t@message_type = message_type_name,");
            script.AppendLine("\t@message_body = CAST(message_body AS varchar(max))");
            script.AppendLine("FROM");
            script.AppendLine($"\t[{queueFullName}]");
            script.AppendLine("), TIMEOUT @timeout;");
            script.AppendLine();
            script.AppendLine("SELECT @message_type AS [messageType], @message_body AS [messageBody];");
            
            if (!string.IsNullOrEmpty(exceptionText))
            {
                script.AppendLine();
                script.AppendLine("Exception getting full queue name:");
                script.AppendLine();
                script.AppendLine(exceptionText);
            }

            return script.ToString();
        }
        private void ShowMainWindowTab(string caption, string script)
        {
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorViewModel viewModel = Services.GetService<ScriptEditorViewModel>();
            viewModel.Name = caption;
            viewModel.ScriptCode = script;
            ScriptEditorView view = new ScriptEditorView() { DataContext = viewModel };
            mainWindow.AddNewTab(viewModel.Name, view);
        }
    }
}
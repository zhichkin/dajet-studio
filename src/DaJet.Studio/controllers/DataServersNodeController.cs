using DaJet.Studio.MVVM;
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
        private const string DATA_SERVER_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/data-server.png";

        private readonly BitmapImage ADD_SERVER_ICON = new BitmapImage(new Uri(ADD_SERVER_ICON_PATH));
        private readonly BitmapImage DATA_SERVER_ICON = new BitmapImage(new Uri(DATA_SERVER_ICON_PATH));

        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        public DataServersNodeController(IServiceProvider serviceProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
        }
        public TreeNodeViewModel CreateTreeNode()
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = DATA_SERVER_ICON,
                NodeText = "Data servers",
                NodeToolTip = "SQL Server instances",
                NodePayload = this
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add server",
                MenuItemIcon = ADD_SERVER_ICON,
                MenuItemCommand = new RelayCommand(AddDataServerCommand),
                MenuItemPayload = node
            });

            return node;
        }
        private void AddDataServerCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (treeNode.NodePayload != this) return;

            // TODO: get sql server connection service - check connection and add server + write to settings file
            var service = Services.GetService<MainWindowViewModel>();

            MessageBox.Show("Under construction.", "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
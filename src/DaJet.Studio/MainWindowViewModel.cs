using DaJet.Studio.MVVM;
using Microsoft.Extensions.Options;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private const string CATALOG_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/catalog.png";
        private readonly BitmapImage CATALOG_ICON = new BitmapImage(new Uri(CATALOG_ICON_PATH));

        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        public MainWindowViewModel(IServiceProvider serviceProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
            InitializeViewModel();
        }
        private string _StatusBarRegion = string.Empty;
        public string StatusBarRegion
        {
            get { return _StatusBarRegion; }
            set { _StatusBarRegion = value; OnPropertyChanged(); }
        }
        public TreeNodeViewModel MainTreeRegion { get; } = new TreeNodeViewModel();
        public ObservableCollection<MenuItemViewModel> MainMenuRegion { get; } = new ObservableCollection<MenuItemViewModel>();
        private void InitializeViewModel()
        {
            MainMenuRegion.Add(new MenuItemViewModel()
            {
                MenuItemIcon = CATALOG_ICON,
                MenuItemHeader = "About",
                MenuItemCommand = new RelayCommand(ConnectDataServerCommand),
                MenuItemPayload = this
            });

            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                IsExpanded = false,
                NodeIcon = CATALOG_ICON,
                NodeText = "SQL Servers",
                NodeToolTip = "Data servers",
                NodePayload = null
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Connect server",
                MenuItemIcon = CATALOG_ICON,
                MenuItemCommand = new RelayCommand(ConnectDataServerCommand),
                MenuItemPayload = node
            });
            MainTreeRegion.TreeNodes.Add(node);
        }
        private void ConnectDataServerCommand(object parameter)
        {
            if (!(parameter is TreeNodeViewModel treeNode)) return;
            if (treeNode.NodeText != "SQL Servers") return;
            MessageBox.Show("Under construction.", "DaJet", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
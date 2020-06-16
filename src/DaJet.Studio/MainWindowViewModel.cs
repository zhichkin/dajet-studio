using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.ObjectModel;
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
            //MainMenuRegion.Add(new MenuItemViewModel()
            //{
            //    MenuItemIcon = CATALOG_ICON,
            //    MenuItemHeader = "About",
            //    MenuItemCommand = new RelayCommand(ConnectDataServerCommand),
            //    MenuItemPayload = this
            //});

            ITreeNodeController controller = Services.GetService<DataServersNodeController>();
            if (controller != null)
            {
                MainTreeRegion.TreeNodes.Add(controller.CreateTreeNode());
            }
        }
    }
}
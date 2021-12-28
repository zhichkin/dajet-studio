using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DaJet.Studio
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        public MainWindowViewModel(IServiceProvider serviceProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
            InitializeViewModel();
        }
        private void InitializeViewModel()
        {
            SearchCommand = new RelayCommand(SearchCommandHandler);
            ClearSearchCommand = new RelayCommand(ClearSearchCommandHandler);
            SearchBoxKeyDownCommand = new RelayCommand(SearchBoxKeyDownCommandHandler);

            ITreeNodeController controller = Services.GetService<RabbitMQController>();
            if (controller != null)
            {
                try
                {
                    MainTreeRegion.TreeNodes.Add(controller.CreateTreeNode(this));
                }
                catch (Exception error)
                {
                    _ = MessageBox.Show(ExceptionHelper.GetErrorText(error),
                        "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            controller = Services.GetService<MetadataController>();
            if (controller != null)
            {
                try
                {
                    MainTreeRegion.TreeNodes.Add(controller.CreateTreeNode(this));
                }
                catch (Exception error)
                {
                    _ = MessageBox.Show(ExceptionHelper.GetErrorText(error),
                        "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private TabViewModel _selectedTab;
        public TabViewModel SelectedTab
        {
            get { return _selectedTab; }
            set { _selectedTab = value; OnPropertyChanged(nameof(SelectedTab)); }
        }
        private string _StatusBarRegion = string.Empty;
        public string StatusBarRegion
        {
            get { return _StatusBarRegion; }
            set { _StatusBarRegion = value; OnPropertyChanged(); }
        }
        
        public TreeNodeViewModel MainTreeRegion { get; } = new TreeNodeViewModel();
        public ObservableCollection<TabViewModel> Tabs { get; } = new ObservableCollection<TabViewModel>();
        public ObservableCollection<MenuItemViewModel> MainMenuRegion { get; } = new ObservableCollection<MenuItemViewModel>();

        #region "Search box functionality"

        private string _SearchText = string.Empty;
        public ICommand SearchCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }
        public ICommand SearchBoxKeyDownCommand { get; private set; }
        public string SearchText
        {
            get { return _SearchText; }
            set { _SearchText = value; OnPropertyChanged(nameof(SearchText)); }
        }
        private void SearchCommandHandler(object parameter)
        {
            ITreeNodeController controller = Services.GetService<MetadataController>();
            if (controller != null)
            {
                controller.Search(SearchText);
            }
        }
        private void ClearSearchCommandHandler(object parameter)
        {
            SearchText = string.Empty;

            ITreeNodeController controller = Services.GetService<MetadataController>();
            if (controller != null)
            {
                controller.Search(SearchText);
            }
        }
        private void SearchBoxKeyDownCommandHandler(object parameter)
        {
            if (!(parameter is KeyEventArgs args)) return;
            
            if (args.Key == Key.Enter)
            {
                SearchCommandHandler(null);
            }
        }

        #endregion

        public object SelectedTabViewModel
        {
            get
            {
                if (SelectedTab == null) return null;
                if (!(SelectedTab.Content is UserControl content)) return null;
                return content.DataContext;
            }
        }
        public void AddNewTab(string header, object content)
        {
            TabViewModel tab = Services.GetService<TabViewModel>();
            tab.Header = header;
            tab.Content = content;
            Tabs.Add(tab);
            SelectedTab = tab;
        }
        public void RemoveTab(TabViewModel tab)
        {
            if (tab != null)
            {
                Tabs.Remove(tab);
                if (Tabs.Count > 0)
                {
                    SelectedTab = Tabs[0];
                }
            }
        }
        public void RefreshTabHeader(object dataContext, string header)
        {
            for (int i = 0; i < Tabs.Count; i++)
            {
                UserControl content = Tabs[i].Content as UserControl;
                if (content == null)
                {
                    continue;
                }
                if (content.DataContext == dataContext)
                {
                    Tabs[i].Header = header;
                    break;
                }
            }
        }

        public TreeNodeViewModel GetTreeNodeByPayload(ObservableCollection<TreeNodeViewModel> treeNodes, object payload)
        {
            foreach (var treeNode in treeNodes)
            {
                if (treeNode.NodePayload == payload) { return treeNode; }
                TreeNodeViewModel node = GetTreeNodeByPayload(treeNode.TreeNodes, payload);
                if (node != null) { return node; }
            }
            return null;
        }
    }
}
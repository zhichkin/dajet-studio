using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

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
        private void InitializeViewModel()
        {
            ITreeNodeController controller = Services.GetService<MetadataController>();
            if (controller != null)
            {
                try
                {
                    MainTreeRegion.TreeNodes.Add(controller.CreateTreeNode());
                }
                catch (Exception error)
                {
                    _ = MessageBox.Show(ExceptionHelper.GetErrorText(error),
                        "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            controller = Services.GetService<HttpServicesController>();
            if (controller != null)
            {
                try
                {
                    MainTreeRegion.TreeNodes.Add(controller.CreateTreeNode());
                }
                catch (Exception error)
                {
                    _ = MessageBox.Show(ExceptionHelper.GetErrorText(error),
                        "DaJet", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
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
using DaJet.Studio.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Windows.Media.Imaging;

namespace DaJet.Studio
{
    public sealed class ScriptingController : ITreeNodeController
    {
        private const string SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/database-script.png";
        private const string EDIT_SCRIPT_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/edit-script.png";

        private readonly BitmapImage SCRIPT_ICON = new BitmapImage(new Uri(SCRIPT_ICON_PATH));
        private readonly BitmapImage EDIT_SCRIPT_ICON = new BitmapImage(new Uri(EDIT_SCRIPT_ICON_PATH));

        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        public ScriptingController(IServiceProvider serviceProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
        }
        public TreeNodeViewModel CreateTreeNode()
        {
            TreeNodeViewModel node = new TreeNodeViewModel()
            {
                IsExpanded = true,
                NodeIcon = SCRIPT_ICON,
                NodeText = "New script",
                NodeToolTip = "SQL script",
                NodePayload = this
            };
            node.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Edit script",
                MenuItemIcon = EDIT_SCRIPT_ICON,
                MenuItemCommand = new RelayCommand(EditScriptCommand),
                MenuItemPayload = node
            });
            return node;
        }
        private void EditScriptCommand(object node)
        {
            if (!(node is TreeNodeViewModel treeNode)) return;

            TabViewModel tab = Services.GetService<TabViewModel>();
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            ScriptEditorView editor = new ScriptEditorView() { DataContext = tab };
            mainWindow.AddNewTab(treeNode.NodeText, editor);
        }
    }
}
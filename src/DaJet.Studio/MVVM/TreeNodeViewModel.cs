using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DaJet.Studio.MVVM
{
    public sealed class TreeNodeViewModel : ViewModelBase
    {
        private string _nodeText;
        private string _nodeToolTip;
        private BitmapImage _nodeIcon;
        private object _nodePayload;
        private bool _isExpanded;
        public TreeNodeViewModel()
        {
            SelectedItemChanged = new RelayCommand(SelectedItemChangedHandler);
        }
        public string NodeText
        {
            get { return _nodeText; }
            set { _nodeText = value; OnPropertyChanged(); }
        }
        public string NodeToolTip
        {
            get { return _nodeToolTip; }
            set { _nodeToolTip = value; OnPropertyChanged(); }
        }
        public BitmapImage NodeIcon
        {
            get { return _nodeIcon; }
            set { _nodeIcon = value; OnPropertyChanged(); }
        }
        public object NodePayload
        {
            get { return _nodePayload; }
            set { _nodePayload = value; OnPropertyChanged(); }
        }
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                OnPropertyChanged();
            }
        }
        public ICommand SelectedItemChanged { get; set; }
        private void SelectedItemChangedHandler(object parameter)
        {
            if (!(parameter is RoutedPropertyChangedEventArgs<object> args)) return;
            args.Handled = true;
            SelectedItem = args.NewValue as TreeNodeViewModel;
        }
        public TreeNodeViewModel SelectedItem { get; private set; }
        public ObservableCollection<TreeNodeViewModel> TreeNodes { get; } = new ObservableCollection<TreeNodeViewModel>();
        public ObservableCollection<MenuItemViewModel> ContextMenuItems { get; } = new ObservableCollection<MenuItemViewModel>();
    }
}
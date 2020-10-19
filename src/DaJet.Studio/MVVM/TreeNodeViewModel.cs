using System;
using System.Collections.ObjectModel;
using System.Reflection;
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
        private bool _isSelected;
        public TreeNodeViewModel()
        {
            SelectedItemChanged = new RelayCommand(SelectedItemChangedHandler);

            TreeViewKeyDown = new RelayCommand(TreeViewKeyDownHandler);
            TextBoxLostFocus = new RelayCommand(TextBoxLostFocusHandler);
            EnableEditingCommand = new RelayCommand(EnableEditingCommandHandler);
        }
        public TreeNodeViewModel Parent { get; set; }
        public string NodeText
        {
            get { return _nodeText; }
            set { _nodeText = value; NodeTextPropertyChanged(); }
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
        public string NodeTextPropertyBinding { get; set; }
        private void NodeTextPropertyChanged()
        {
            if (string.IsNullOrWhiteSpace(NodeTextPropertyBinding)) return;
            if (NodePayload == null) return;

            PropertyInfo property = NodePayload.GetType().GetProperty(NodeTextPropertyBinding);
            if (property == null) return;
            if (!property.CanRead) return;
            if (!property.CanWrite) return;
            if (property.PropertyType != typeof(string)) return;

            if (EditMode == NodeTextEditMode.Canceled)
            {
                _nodeText = (string)property.GetValue(NodePayload);
            }
            else
            {
                property.SetValue(NodePayload, _nodeText);
            }
            OnPropertyChanged(nameof(NodeText));
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
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
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



        private enum NodeTextEditMode { None, Editing, Confirmed, Canceled }



        private bool _isEditable = false;
        public bool IsEditable
        {
            get { return _isEditable; }
            set
            {
                _isEditable = value;
                OnPropertyChanged();
            }
        }
        public ICommand EnableEditingCommand { get; set; }
        private void EnableEditingCommandHandler(object parameter)
        {
            if (IsSelected)
            {
                EnableEditing();
                if (parameter is MouseButtonEventArgs args)
                {
                    args.Handled = true; // so as TextBox won't lose focus
                }
            }
        }
        private NodeTextEditMode EditMode { get; set; } = NodeTextEditMode.None;
        internal void EnableEditing()
        {
            if (!IsEditable) return;
            IsEditModeEnabled = true;
            IsViewModeEnabled = false;
            EditMode = NodeTextEditMode.Editing;
        }
        internal void ConfirmEditing()
        {
            if (!IsEditable) return;
            IsViewModeEnabled = true;
            IsEditModeEnabled = false;
            EditMode = NodeTextEditMode.Confirmed;
        }
        internal void CancelEditing()
        {
            if (!IsEditable) return;
            IsViewModeEnabled = true;
            IsEditModeEnabled = false;
            EditMode = NodeTextEditMode.Canceled;
        }

        private bool _isViewModeEnabled = true;
        public bool IsViewModeEnabled
        {
            get { return _isViewModeEnabled; }
            set
            {
                if (_isViewModeEnabled == value) return;
                _isViewModeEnabled = value;
                OnPropertyChanged();
            }
        }
        private bool _isEditModeEnabled = false;
        public bool IsEditModeEnabled
        {
            get { return _isEditModeEnabled; }
            set
            {
                if (_isEditModeEnabled == value) return;
                _isEditModeEnabled = value;
                OnPropertyChanged();
            }
        }
        public ICommand TreeViewKeyDown { get; set; }
        private void TreeViewKeyDownHandler(object parameter)
        {
            if (!(parameter is KeyEventArgs args)) return;
            if (SelectedItem == null) return;
            if (args.Key == Key.F2)
            {
                SelectedItem.EnableEditing();
            }
            else if (args.Key == Key.Enter)
            {
                SelectedItem.ConfirmEditing();
            }
            else if (args.Key == Key.Escape)
            {
                SelectedItem.CancelEditing();
            }
        }
        public ICommand TextBoxLostFocus { get; set; }
        private void TextBoxLostFocusHandler(object parameter)
        {
            if (!(parameter is RoutedEventArgs args)) return;
            if (EditMode == NodeTextEditMode.Editing)
            {
                ConfirmEditing();
            }
        }



        public TPayload GetAncestorPayload<TPayload>()
        {
            TreeNodeViewModel ancestor = this.Parent;
            while (ancestor != null)
            {
                if (ancestor.NodePayload is TPayload payload)
                {
                    return payload;
                }
                ancestor = ancestor.Parent;
            }
            return default;
        }
    }
}
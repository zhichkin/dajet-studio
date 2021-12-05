using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DaJet.Studio.MVVM
{
    public sealed class NodeTextPropertyChangedEventArgs : CancelEventArgs
    {
        public NodeTextPropertyChangedEventArgs(string oldValue, string newValue)
        {
            Cancel = false;
            OldValue = oldValue;
            NewValue = newValue;
        }
        public string OldValue { get; }
        public string NewValue { get; }
    }
    public delegate void NodeTextPropertyChangedEventHandler(TreeNodeViewModel sender, NodeTextPropertyChangedEventArgs args);
    public sealed class TreeNodeViewModel : ViewModelBase
    {
        private string _nodeText;
        private string _nodeToolTip;
        private BitmapImage _nodeIcon;
        private object _nodePayload;
        private bool _isExpanded;
        private bool _isSelected;
        private bool _isVisible = true;
        public TreeNodeViewModel()
        {
            SelectedItemChanged = new RelayCommand(SelectedItemChangedHandler);

            TreeViewKeyDown = new RelayCommand(TreeViewKeyDownHandler);
            TextBoxLostFocus = new RelayCommand(TextBoxLostFocusHandler);
            EnableEditingCommand = new RelayCommand(EnableEditingCommandHandler);
        }
        public override string ToString() { return this.NodeText; }
        public TreeNodeViewModel Parent { get; set; }
        public string NodeText
        {
            get { return _nodeText; }
            set
            {
                // store old value
                string oldValue = _nodeText;
                // set new value
                _nodeText = value;
                // notify property changed
                OnNodeTextPropertyChanged(oldValue);
            }
        }
        public event NodeTextPropertyChangedEventHandler NodeTextPropertyChanged;
        public void UpdateNodeText(string nodeText)
        {
            _nodeText = nodeText;
            OnPropertyChanged(nameof(NodeText));
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
        private void OnNodeTextPropertyChanged(string oldValue)
        {
            if (oldValue == null)
            {
                // this is first initialization
                return;
            }
            if (!IsEditable)
            {
                // notify WPF standard binding mechanism
                OnPropertyChanged(nameof(NodeText));
                return;
            }
            if (EditMode == NodeTextEditMode.Canceled)
            {
                // restore old value
                _nodeText = oldValue;
            }
            else
            {
                // check if property value has been changed
                if (oldValue != _nodeText)
                {
                    // notify custom ui controller
                    NodeTextPropertyChangedEventArgs args = new NodeTextPropertyChangedEventArgs(oldValue, _nodeText);
                    NodeTextPropertyChanged?.Invoke(this, args);
                    if (args.Cancel)
                    {
                        // restore old value
                        _nodeText = oldValue;
                    }
                }
            }
            // notify WPF standard binding mechanism
            OnPropertyChanged(nameof(NodeText));
        }
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible == value)
                {
                    return;
                }
                _isVisible = value; OnPropertyChanged();
            }
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

        public TreeNodeViewModel GetDescendant(object payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (this.NodePayload == payload)
            {
                return this;
            }

            Queue<TreeNodeViewModel> queue = new Queue<TreeNodeViewModel>();

            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                TreeNodeViewModel ancestor = queue.Dequeue();

                foreach (TreeNodeViewModel descendant in ancestor.TreeNodes)
                {
                    if (descendant.NodePayload == payload)
                    {
                        return descendant;
                    }
                    else if (descendant.TreeNodes.Count > 0)
                    {
                        queue.Enqueue(descendant);
                    }
                }
            }

            return null;
        }
    }
}
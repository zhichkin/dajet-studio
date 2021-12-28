using DaJet.Data.Mapping;
using DaJet.Studio.MVVM;
using System.Collections.Generic;
using System.Windows.Input;

namespace DaJet.Studio.UI
{
    public sealed class FilterParameterViewModel : ViewModelBase
    {
        private static readonly List<string> _operators = new List<string>()
        {
            "Равно",
            "Не равно",
            "Больше",
            "Больше или равно",
            "Меньше",
            "Меньше или равно"
            //"Между"
            //"Содержит"
        };

        private IListViewModelController Parent { get; set; }

        public FilterParameterViewModel(IListViewModelController parent)
        {
            Parent = parent;
            CopyParameterCommand = new RelayCommand(CopyParameterCommandHandler);
            RemoveParameterCommand = new RelayCommand(RemoveParameterCommandHandler);
        }

        public bool UseMe { get; set; }
        public string Name { set; get; }
        public ComparisonOperator FilterOperator { set; get; }
        public List<string> FilterOperators { get { return _operators; } }
        private string _selectedFilterOperator = "Равно";
        public string SelectedFilterOperator
        {
            get { return _selectedFilterOperator; }
            set
            {
                _selectedFilterOperator = value;
                if (_selectedFilterOperator == "Равно") this.FilterOperator = ComparisonOperator.Equal;
                else if (_selectedFilterOperator == "Не равно") this.FilterOperator = ComparisonOperator.NotEqual;
                else if (_selectedFilterOperator == "Больше") this.FilterOperator = ComparisonOperator.Greater;
                else if (_selectedFilterOperator == "Больше или равно") this.FilterOperator = ComparisonOperator.GreaterOrEqual;
                else if (_selectedFilterOperator == "Меньше") this.FilterOperator = ComparisonOperator.Less;
                else if (_selectedFilterOperator == "Меньше или равно") this.FilterOperator = ComparisonOperator.LessOrEqual;
                else if (_selectedFilterOperator == "Между") this.FilterOperator = ComparisonOperator.Between;
                else if (_selectedFilterOperator == "Содержит") this.FilterOperator = ComparisonOperator.Contains;
            }
        }

        private object _value;
        public object Value
        {
            get { return _value; }
            set { _value = value; OnPropertyChanged(nameof(Value)); }
        }

        public ICommand CopyParameterCommand { get; private set; }
        private void CopyParameterCommandHandler(object parameter)
        {
            Parent.CopyItem(parameter);
        }
        public ICommand RemoveParameterCommand { get; private set; }
        private void RemoveParameterCommandHandler(object parameter)
        {
            Parent.RemoveItem(parameter);
        }
    }
}
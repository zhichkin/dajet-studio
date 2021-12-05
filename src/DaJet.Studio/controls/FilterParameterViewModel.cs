using DaJet.Studio.MVVM;
using System;
using System.Collections.Generic;

namespace DaJet.Studio.UI
{
    public enum FilterOperator
    {
        Equal,
        NotEqual,
        Contains,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Between
    }
    public sealed class FilterParameterViewModel : ViewModelBase
    {
        private static readonly List<string> _operators = new List<string>()
        {
            "Равно",
            "Не равно",
            "Содержит",
            "Больше",
            "Больше или равно",
            "Меньше",
            "Меньше или равно",
            "Между"
        };

        public bool UseMe { get; set; }
        public string Name { set; get; }
        public FilterOperator FilterOperator { set; get; }
        public List<string> FilterOperators { get { return _operators; } }
        private string _selectedFilterOperator = "Равно";
        public string SelectedFilterOperator
        {
            get { return _selectedFilterOperator; }
            set
            {
                _selectedFilterOperator = value;
                if (_selectedFilterOperator == "Равно") this.FilterOperator = FilterOperator.Equal;
                else if (_selectedFilterOperator == "Не равно") this.FilterOperator = FilterOperator.NotEqual;
                else if (_selectedFilterOperator == "Содержит") this.FilterOperator = FilterOperator.Contains;
                else if (_selectedFilterOperator == "Больше") this.FilterOperator = FilterOperator.Greater;
                else if (_selectedFilterOperator == "Больше или равно") this.FilterOperator = FilterOperator.GreaterOrEqual;
                else if (_selectedFilterOperator == "Меньше") this.FilterOperator = FilterOperator.Less;
                else if (_selectedFilterOperator == "Меньше или равно") this.FilterOperator = FilterOperator.LessOrEqual;
                else if (_selectedFilterOperator == "Между") this.FilterOperator = FilterOperator.Between;
            }
        }

        private object _value;
        public object Value
        {
            get { return _value; }
            set { _value = value; OnPropertyChanged(nameof(Value)); }
        }
    }
}

using DaJet.Studio.MVVM;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DaJet.Studio.UI
{
    public sealed class FilterTableDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NullValueTemplate { get; set; }

        public DataTemplate StringViewTemplate { get; set; }
        public DataTemplate DateTimeViewTemplate { get; set; }
        
        public DataTemplate StringEditTemplate { get; set; }
        public DataTemplate DateTimeEditTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;
            if (element == null) return null;

            DataGridCell cell = element.GetParent<DataGridCell>();
            bool isEditing = (cell == null || cell.IsEditing);

            Type type = typeof(string); // default Value column type

            if (cell.DataContext is FilterParameterViewModel parameter)
            {
                if (parameter.Value != null)
                {
                    type = parameter.Value.GetType();
                }
                else
                {
                    return NullValueTemplate;
                }
            }

            if (isEditing)
            {
                if (type == typeof(DateTime)) { return DateTimeEditTemplate; }
                else if (type == typeof(string)) { return StringEditTemplate; }

                return StringEditTemplate; // will throw exception if is null
            }
            else
            {
                if (type == typeof(DateTime)) { return DateTimeViewTemplate; }
                else if (type == typeof(string)) { return StringViewTemplate; }

                return StringViewTemplate; // will throw exception if is null
            }

            //string templateName = isEditing ?
            //    GetEditableTemplateName(type) :
            //    GetReadOnlyTemplateName(type);

            //return element.FindResource(templateName) as DataTemplate;
        }
        private string GetReadOnlyTemplateName(Type type)
        {
            if (type == typeof(bool)) { return "BooleanReadOnlyTemplate"; }
            else if (type == typeof(int)) { return "Int32ReadOnlyTemplate"; }
            else if (type == typeof(decimal)) { return "DecimalReadOnlyTemplate"; }
            else if (type == typeof(DateTime)) { return "DateTimeReadOnlyTemplate"; }
            else if (type == typeof(string)) { return "StringReadOnlyTemplate"; }
            else if (type == typeof(object)) { return "ReferenceObjectReadOnlyTemplate"; }
            return "EmptyReadOnlyTemplate";
        }
        private string GetEditableTemplateName(Type type)
        {
            if (type == typeof(bool)) { return "BooleanEditableTemplate"; }
            else if (type == typeof(int)) { return "Int32EditableTemplate"; }
            else if (type == typeof(decimal)) { return "DecimalEditableTemplate"; }
            else if (type == typeof(DateTime)) { return "DateTimeEditableTemplate"; }
            else if (type == typeof(string)) { return "StringEditableTemplate"; }
            else if (type == typeof(object)) { return "ReferenceObjectEditableTemplate"; }
            return "EmptyEditableTemplate";
        }
    }
}
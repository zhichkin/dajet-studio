using DaJet.Studio.MVVM;
using System.Windows;
using System.Windows.Controls;

namespace DaJet.Studio
{
    internal sealed class ContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LeftRegionTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null) return null;
            if (item is TreeNodeViewModel) return LeftRegionTemplate;
            return null;
            //return (container as FrameworkElement).FindResource("ComparisonOperatorTemplate") as DataTemplate;
        }
    }
}
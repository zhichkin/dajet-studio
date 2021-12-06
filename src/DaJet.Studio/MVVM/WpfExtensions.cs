using System.Windows;
using System.Windows.Media;

namespace DaJet.Studio.MVVM
{
    public static class WpfExtensions
    {
        public static TParent GetParent<TParent>(this DependencyObject child) where TParent : DependencyObject
        {
            if (child == null) return null;
            if (child.GetType() == typeof(TParent)) return (TParent)child;
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && parent.GetType() != typeof(TParent))
            {
                parent = GetParent<TParent>(parent);
            }
            return parent == null ? null : (TParent)parent;
        }
        public static TChild GetChild<TChild>(this DependencyObject parent) where TChild : DependencyObject
        {
            if (parent == null) return null;
            TChild result = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child.GetType() == typeof(TChild)) return (TChild)child;
                result = GetChild<TChild>(child);
                if (result != null) return result;
            }
            return result;
        }
    }
}
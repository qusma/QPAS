using System.Windows;
using System.Windows.Media;

namespace QPAS
{
    public static class DependencyObjectExtensions
    {
        public static T FindAncestor<T>(this DependencyObject dependencyObject)
            where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);

            if (parent == null) return null;

            var parentT = parent as T;
            return parentT ?? FindAncestor<T>(parent);
        }
    }
}
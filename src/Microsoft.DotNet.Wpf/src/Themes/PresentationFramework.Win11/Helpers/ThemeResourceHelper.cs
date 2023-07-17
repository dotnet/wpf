using System.Windows;
using System.Windows.Media;

namespace PresentationFramework.Win11
{
    internal static class ThemeResourceHelper
    {
        private static readonly DependencyProperty ColorKeyProperty =
            DependencyProperty.RegisterAttached(
                "ColorKey",
                typeof(object),
                typeof(ThemeResourceHelper));

        internal static object GetColorKey(SolidColorBrush element)
        {
            return element.GetValue(ColorKeyProperty);
        }

        internal static void SetColorKey(SolidColorBrush element, object value)
        {
            element.SetValue(ColorKeyProperty, value);
        }
    }
}

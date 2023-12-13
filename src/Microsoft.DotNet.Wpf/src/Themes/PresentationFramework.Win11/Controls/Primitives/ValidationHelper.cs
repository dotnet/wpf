using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace PresentationFramework.Win11.Controls.Primitives
{
    public static class ValidationHelper
    {
        #region IsTemplateValidationAdornerSite

        public static readonly DependencyProperty IsTemplateValidationAdornerSiteProperty =
            DependencyProperty.RegisterAttached(
                "IsTemplateValidationAdornerSite",
                typeof(bool),
                typeof(ValidationHelper),
                new PropertyMetadata(OnIsTemplateValidationAdornerSiteChanged));

        public static bool GetIsTemplateValidationAdornerSite(FrameworkElement element)
        {
            return (bool)element.GetValue(IsTemplateValidationAdornerSiteProperty);
        }

        public static void SetIsTemplateValidationAdornerSite(FrameworkElement element, bool value)
        {
            element.SetValue(IsTemplateValidationAdornerSiteProperty, value);
        }

        private static void OnIsTemplateValidationAdornerSiteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = (FrameworkElement)d;
            if ((bool)e.NewValue)
            {
                Debug.Assert(element.TemplatedParent != null);
                Validation.SetErrorTemplate(element, null);
                Validation.SetValidationAdornerSiteFor(element, element.TemplatedParent);
            }
            else
            {
                element.ClearValue(Validation.ErrorTemplateProperty);
                element.ClearValue(Validation.ValidationAdornerSiteForProperty);
            }
        }

        #endregion
    }
}

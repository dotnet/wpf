using System;
using System.Windows.Markup;
using System.Windows.Media;

namespace PresentationFramework.Win11.Markup
{
    public class StaticColorExtension : System.Windows.StaticResourceExtension
    {
        public StaticColorExtension()
        {
        }

        public StaticColorExtension(object resourceKey) : base(resourceKey)
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            object value = base.ProvideValue(serviceProvider);

            if (serviceProvider?.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget provideValueTarget)
            {
                if (provideValueTarget.TargetObject is SolidColorBrush solidColorBrush)
                {
                    ThemeResourceHelper.SetColorKey(solidColorBrush, ResourceKey);
                }
            }

            return value;
        }
    }
}

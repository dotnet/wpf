using System;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace ModernWpf.Markup
{
    [MarkupExtensionReturnType(typeof(KeyTime))]
    public class KeyTimeExtension : MarkupExtension
    {
        public KeyTimeExtension()
        {
        }

        public TimeSpan TimeSpan { get; set; } = TimeSpan.Zero;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return KeyTime.FromTimeSpan(TimeSpan);
        }
    }
}

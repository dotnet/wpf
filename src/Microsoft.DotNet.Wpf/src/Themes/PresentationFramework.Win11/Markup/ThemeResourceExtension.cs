using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ModernWpf.Markup
{
    [TypeConverter(typeof(ThemeResouceExtensionConverter))]
    public class ThemeResourceExtension : DynamicResourceExtension
    {
        public ThemeResourceExtension()
        {
        }

        public ThemeResourceExtension(object resourceKey) : base(resourceKey)
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (ResourceKey is string key && key.StartsWith("SystemColor", StringComparison.Ordinal))
            {
                var binding = new Binding(key) { Source = SystemColorsSource.Current };
                return binding.ProvideValue(serviceProvider);
            }

            return base.ProvideValue(serviceProvider);
        }

        private class SystemColorsSource : INotifyPropertyChanged
        {
            private SystemColorsSource()
            {
                SystemParameters.StaticPropertyChanged += OnSystemParametersPropertyChanged;
            }

            public static SystemColorsSource Current { get; } = new SystemColorsSource();

            public Color SystemColorButtonFaceColor => SystemColors.ControlColor;
            public Color SystemColorButtonTextColor => SystemColors.ControlTextColor;
            public Color SystemColorGrayTextColor => SystemColors.GrayTextColor;
            public Color SystemColorHighlightColor => SystemColors.HighlightColor;
            public Color SystemColorHighlightTextColor => SystemColors.HighlightTextColor;
            public Color SystemColorHotlightColor => SystemColors.HotTrackColor;
            public Color SystemColorWindowColor => SystemColors.WindowColor;
            public Color SystemColorWindowTextColor => SystemColors.WindowTextColor;
            public Color SystemColorActiveCaptionColor => SystemColors.ActiveCaptionColor;
            public Color SystemColorInactiveCaptionTextColor => SystemColors.InactiveCaptionTextColor;

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnSystemParametersPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(SystemParameters.HighContrast) && SystemParameters.HighContrast)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }
        }
    }

    public class ThemeResouceExtensionConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                ThemeResourceExtension dynamicResource = value as ThemeResourceExtension;

                if (dynamicResource == null)

                    throw new ArgumentException($"{value} must be of type {nameof(ThemeResourceExtension)}", nameof(value));

                return new InstanceDescriptor(typeof(ThemeResourceExtension).GetConstructor(new Type[] { typeof(object) }),
                    new object[] { dynamicResource.ResourceKey });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}

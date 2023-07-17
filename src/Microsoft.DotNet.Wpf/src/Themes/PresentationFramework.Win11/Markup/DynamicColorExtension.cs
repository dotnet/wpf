using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
namespace PresentationFramework.Win11.Markup
{
    [TypeConverter(typeof(DynamicColorExtensionConverter))]
    public class DynamicColorExtension : DynamicResourceExtension
    {
        public DynamicColorExtension()
        {
        }

        public DynamicColorExtension(object resourceKey) : base(resourceKey)
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

    public class DynamicColorExtensionConverter : TypeConverter
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

                DynamicColorExtension dynamicResource = value as DynamicColorExtension;

                if (dynamicResource == null)

                    throw new ArgumentException($"{value} must be of type {nameof(DynamicColorExtension)}", nameof(value));

                return new InstanceDescriptor(typeof(DynamicColorExtension).GetConstructor(new Type[] { typeof(object) }),
                    new object[] { dynamicResource.ResourceKey });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}

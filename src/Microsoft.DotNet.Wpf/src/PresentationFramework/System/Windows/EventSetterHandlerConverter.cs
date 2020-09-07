// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.ComponentModel;
using System.Globalization;
using System.Reflection;

using System.Windows;
using System.Collections.Generic;
using System.Xaml;

namespace System.Windows.Markup
{
    /// <summary>
    ///     Type converter for RoutedEvent type
    /// </summary>
    public sealed class EventSetterHandlerConverter : TypeConverter
    {
        static Type s_ServiceProviderContextType;

        /// <summary>
        ///     Whether we can convert from a given type - this class only converts from string
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
            // We can only convert from a string and that too only if we have all the contextual information
            // Note: Sometimes even the serializer calls CanConvertFrom in order
            // to determine if it is a valid converter to use for serialization.
            if (sourceType == typeof(string))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Whether we can convert to a given type - this class only converts to string
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            return false;
        }

        /// <summary>
        ///     Convert a string like "Button.Click" into the corresponding RoutedEvent
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext,
                                           CultureInfo cultureInfo,
                                           object source)
        {
            if (typeDescriptorContext == null)
            {
                throw new ArgumentNullException("typeDescriptorContext");
            }
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (s_ServiceProviderContextType == null)
            {
                // get typeof(MS.Internal.Xaml.ServiceProviderContext) via reflection
                Assembly a = typeof(IRootObjectProvider).Assembly;
                s_ServiceProviderContextType = a.GetType("MS.Internal.Xaml.ServiceProviderContext");
            }
            if (typeDescriptorContext.GetType() != s_ServiceProviderContextType)
            {
                // if the caller is not the XAML parser, don't answer.   This avoids
                // returning an arbitrary delegate to a (possibly malicious) caller.
                throw new ArgumentException(SR.Get(SRID.TextRange_InvalidParameterValue), "typeDescriptorContext");
            }
            IRootObjectProvider rootProvider = typeDescriptorContext.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
            if (rootProvider != null && source is String)
            {
                IProvideValueTarget ipvt = typeDescriptorContext.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
                if (ipvt != null)
                {
                    EventSetter setter = ipvt.TargetObject as EventSetter;
                    string handlerName;
                    if(setter != null && (handlerName = source as string) != null)
                    {
                        handlerName = handlerName.Trim();
                        return Delegate.CreateDelegate(setter.Event.HandlerType, rootProvider.RootObject, handlerName);
                    }
                }
            }

            throw GetConvertFromException(source);
        }

        /// <summary>
        ///     Convert a RoutedEventID into a XAML string like "Button.Click"
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext,
                                         CultureInfo cultureInfo,
                                         object value,
                                         Type destinationType)
        {
            throw GetConvertToException(value, destinationType);
        }
    }
}



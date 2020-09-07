// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.ComponentModel;
using System.Globalization;

using System.Windows;
using System.Collections.Generic;
using System.Xaml;

namespace System.Windows.Markup
{
    /// <summary>
    ///     Type converter for RoutedEvent type
    /// </summary>
    public sealed class RoutedEventConverter : TypeConverter
    {
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
            string routedEventName = source as string;
            RoutedEvent routedEvent = null;

            if (routedEventName != null)
            {
                routedEventName = routedEventName.Trim();
                IServiceProvider serviceProvider = typeDescriptorContext as IServiceProvider;

                if (serviceProvider != null)
                {
                    IXamlTypeResolver resolver = serviceProvider.GetService(typeof(IXamlTypeResolver)) as IXamlTypeResolver;
                    Type type = null;

                    if (resolver != null)
                    {
                        // Verify that there's at least one period.  (A simple
                        //  but not foolproof check for "[class].[event]")
                        int lastIndex = routedEventName.IndexOf('.');
                        if (lastIndex != -1)
                        {
                            string typeName = routedEventName.Substring(0, lastIndex);
                            routedEventName = routedEventName.Substring(lastIndex + 1);

                            type = resolver.Resolve(typeName);  
                        }
                    }

                    if (type == null)
                    {              
                        IXamlSchemaContextProvider schemaContextProvider = (typeDescriptorContext.
                            GetService(typeof(IXamlSchemaContextProvider))
                                as IXamlSchemaContextProvider);

                        IAmbientProvider iapp = serviceProvider.GetService(typeof(IAmbientProvider)) as IAmbientProvider;

                        if (schemaContextProvider != null && iapp != null)
                        {
                            XamlSchemaContext schemaContext = schemaContextProvider.SchemaContext;

                            XamlType styleXType = schemaContext.GetXamlType(typeof(Style));

                            List<XamlType> ceilingTypes = new List<XamlType>();
                            ceilingTypes.Add(styleXType);

                            XamlMember styleTargetType = styleXType.GetMember("TargetType");

                            AmbientPropertyValue firstAmbientValue = iapp.GetFirstAmbientValue(ceilingTypes, styleTargetType);

                            if (firstAmbientValue != null)
                            {
                                type = firstAmbientValue.Value as Type;
                            }
                            if (type == null)
                            {
                                type = typeof(FrameworkElement);
                            }
                        }
                    }

                    if (type != null)
                    {
                        Type currentType = type;

                        // Force load the Statics by walking up the hierarchy and running class constructors
                        while (null != currentType)
                        {
                            MS.Internal.WindowsBase.SecurityHelper.RunClassConstructor(currentType);
                            currentType = currentType.BaseType;
                        }

                        routedEvent = EventManager.GetRoutedEventFromName(routedEventName, type);
                    }
                }
            }

            if (routedEvent == null)
            {
                // Falling through here means we are unable to perform the conversion.
                throw GetConvertFromException(source);
            }

            return routedEvent;
        }

        /// <summary>
        ///     Convert a RoutedEventID into a XAML string like "Button.Click"
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext,
                                         CultureInfo cultureInfo,
                                         object value,
                                         Type destinationType)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            else if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }
            else
            {
                throw GetConvertToException(value, destinationType);
            }
        }

        // This routine is copied from TemplateBamlRecordReader.  This functionality
        //  is planned to be part of the utilities exposed by the parser, eliminating
        //  the need to duplicate code.  See task #18279
        private string ExtractNamespaceString(ref string nameString, ParserContext parserContext)
        {
            // The colon is what we look for to determine if there's a namespace prefix specifier.
            int nsIndex = nameString.IndexOf(':');
            string nsPrefix = string.Empty;
            if (nsIndex != -1)
            {
                // Found a namespace prefix separator, so create replacement propertyName.
                // String processing - split "foons" from "BarClass.BazProp"
                nsPrefix = nameString.Substring(0, nsIndex);
                nameString = nameString.Substring(nsIndex + 1);
            }

            // Find the namespace, even if its the default one
            string namespaceURI = parserContext.XmlnsDictionary[nsPrefix];
            if (namespaceURI == null)
            {
                throw new ArgumentException(SR.Get(SRID.ParserPrefixNSProperty, nsPrefix, nameString));
            }

            return namespaceURI;
        }
    }
}
    


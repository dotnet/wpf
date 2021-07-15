// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*  Class for Xaml markup extension for TemplateBinds that
*  can be set on the nodes of the Template VisualTree.
*
*
\***************************************************************************/
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace System.Windows
{
    /// <summary>
    ///  Class for Xaml markup extension for TemplateBindings that
    ///  can be set on the nodes of the Template VisualTree.
    /// </summary>
    [TypeConverter(typeof(TemplateBindingExtensionConverter))]
    [MarkupExtensionReturnType(typeof(Object))]
    public class TemplateBindingExtension : MarkupExtension
    {
        /// <summary>
        ///  Constructor that takes no parameters
        /// </summary>
        public TemplateBindingExtension()
        {
        }

        /// <summary>
        ///  Constructor that takes the resource key that this is a static reference to.
        /// </summary>
        public TemplateBindingExtension(
            DependencyProperty property)
        {
            if (property != null)
            {
                _property = property;
            }
            else
            {
                throw new ArgumentNullException("property");
            }
        }

        /// <summary>
        ///  Return an object that should be set on the targetObject's targetProperty
        ///  for this markup extension.  For TemplateBindingExtension, this is the object found in
        ///  a resource dictionary in the current parent chain that is keyed by ResourceKey
        /// </summary>
        /// <param name="serviceProvider">ServiceProvider that can be queried for services.</param>
        /// <returns>
        ///  The object to set on this property.
        /// </returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Property == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.MarkupExtensionProperty));
            }

            return new TemplateBindingExpression(this);
        }

        /// <summary>
        ///     Property we are binding to
        /// </summary>
        [ConstructorArgument("property")]
        public DependencyProperty Property
        {
            get { return _property; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _property = value;
            }
        }

        /// <summary>
        ///     ValueConverter to interpose between the source and target properties
        /// </summary>
        [DefaultValue(null)]
        public IValueConverter Converter
        {
            get { return _converter; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _converter = value;
            }
        }

        /// <summary>
        ///     ConverterParameter we are binding to
        /// </summary>
        [DefaultValue(null)]
        public object ConverterParameter
        {
            get { return _parameter; }
            set { _parameter = value; }
        }

        private DependencyProperty _property;
        private IValueConverter _converter;
        private object _parameter;
    }
}


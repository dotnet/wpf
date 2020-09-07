// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace System.Windows.Baml2006
{
    internal class TypeConverterMarkupExtension : System.Windows.Markup.MarkupExtension
    {
        private TypeConverter _converter;
        private object _value;

        public TypeConverterMarkupExtension(TypeConverter converter, object value)
        {
            _converter = converter;
            _value = value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _converter.ConvertFrom(new TypeConverterContext(serviceProvider), System.Globalization.CultureInfo.InvariantCulture, _value);
        }

        private class TypeConverterContext : ITypeDescriptorContext
        {
            private IServiceProvider _serviceProvider;
            public TypeConverterContext(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            object IServiceProvider.GetService(Type serviceType)
            {
                return _serviceProvider.GetService(serviceType);
            }

            #region ITypeDescriptorContext Methods
            // ITypeDescriptorContext derives from IServiceProvider.
            void ITypeDescriptorContext.OnComponentChanged()
            {
            }

            bool ITypeDescriptorContext.OnComponentChanging()
            {
                return false;
            }

            IContainer ITypeDescriptorContext.Container
            {
                get { return null; }
            }

            object ITypeDescriptorContext.Instance
            {
                get { return null; }
            }

            PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor
            {
                get { return null; }
            }
            #endregion
        }
    }
}

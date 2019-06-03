// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: wrapper around default converter to dynamcially pick
//      and change value converters depending on changing source and target types
//

using System;
using System.Globalization;
using System.Collections;
using System.ComponentModel;

using System.Reflection;
using System.Windows;
using System.Windows.Data;

using MS.Internal;          // Invariant.Assert
using System.Diagnostics;

namespace MS.Internal.Data
{
    // dynamically pick and switch a default value converter to convert between source and target type
    internal class DynamicValueConverter : IValueConverter
    {
        internal DynamicValueConverter(bool targetToSourceNeeded)
        {
            _targetToSourceNeeded = targetToSourceNeeded;
        }

        internal DynamicValueConverter(bool targetToSourceNeeded, Type sourceType, Type targetType)
        {
            _targetToSourceNeeded = targetToSourceNeeded;
            EnsureConverter(sourceType, targetType);
        }

        internal object Convert(object value, Type targetType)
        {
            return Convert(value, targetType, null, CultureInfo.InvariantCulture);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object result = DependencyProperty.UnsetValue;  // meaning: failure to convert

            if (value != null)
            {
                Type sourceType = value.GetType();
                EnsureConverter(sourceType, targetType);

                if (_converter != null)
                {
                    result = _converter.Convert(value, targetType, parameter, culture);
                }
            }
            else
            {
                if (!targetType.IsValueType)
                {
                    result = null;
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type sourceType, object parameter, CultureInfo culture)
        {
            object result = DependencyProperty.UnsetValue;  // meaning: failure to convert

            if (value != null)
            {
                Type targetType = value.GetType();
                EnsureConverter(sourceType, targetType);

                if (_converter != null)
                {
                    result = _converter.ConvertBack(value, sourceType, parameter, culture);
                }
            }
            else
            {
                if (!sourceType.IsValueType)
                {
                    result = null;
                }
            }

            return result;
        }


        private void EnsureConverter(Type sourceType, Type targetType)
        {
            if ((_sourceType != sourceType) || (_targetType != targetType))
            {
                // types have changed - get a new converter

                if (sourceType != null && targetType != null)
                {
                    // DefaultValueConverter.Create() is more sophisticated to find correct type converters,
                    // e.g. if source/targetType is object or well-known system types.
                    // if there is any change in types, give that code to come up with the correct converter
                    if (_engine == null)
                    {
                        _engine = DataBindEngine.CurrentDataBindEngine;
                    }
                    Invariant.Assert(_engine != null);
                    _converter = _engine.GetDefaultValueConverter(sourceType, targetType, _targetToSourceNeeded);
                }
                else
                {
                    // if either type is null, no conversion is possible.
                    // Don't ask GetDefaultValueConverter - it will use null as a
                    // hashtable key, and crash (bug 110859).
                    _converter = null;
                }

                _sourceType = sourceType;
                _targetType = targetType;
            }
        }

        private Type _sourceType;
        private Type _targetType;
        private IValueConverter _converter;
        private bool _targetToSourceNeeded;
        private DataBindEngine _engine;
    }
}

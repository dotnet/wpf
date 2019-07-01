// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.CustomTypes
{
    public class PropertyForBinding
    {
        public static List<DependencyProperty> GetPropertiesForBinding(UIElement element)
        {
            DependencyPropertyFilter propertyFilter = new DependencyPropertyFilter(DataBindingPropertyFilterForObjects);
            Type elementType = element.GetType();
            List<DependencyProperty> propertyList = new List<DependencyProperty>();

            foreach (FieldInfo fieldInfo in elementType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (fieldInfo.FieldType == typeof(DependencyProperty))
                {
                    DependencyProperty dp = fieldInfo.GetValue(null) as DependencyProperty;

                    if (propertyFilter(dp))
                    {
                        if (!propertyList.Contains(dp))
                        {
                            propertyList.Add(dp);
                        }
                    }
                }
            }

            return propertyList;
        }

        private static bool DataBindingPropertyFilterForObjects(DependencyProperty dp)
        {
            // Can not bind to DataContext in this engine, by design
            if (dp == FrameworkElement.DataContextProperty)
            {
                return false;
            }

            PropertyMetadata meta = dp.GetMetadata(typeof(FrameworkElement));

            if ((!(dp.ReadOnly)) && (meta is FrameworkPropertyMetadata) && (!((FrameworkPropertyMetadata)meta).IsNotDataBindable))
            {
                if (CLRDataItem.IsSupported(dp.PropertyType))
                {
                    return true;
                }
            }

            return false;
        }

        internal delegate bool DependencyPropertyFilter(DependencyProperty dp);
    }
}

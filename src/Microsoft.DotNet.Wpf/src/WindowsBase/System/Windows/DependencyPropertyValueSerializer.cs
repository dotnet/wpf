// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  ValueSerializer for DependencyProperty
//
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;

namespace System.Windows
{
    internal class DependencyPropertyValueSerializer : ValueSerializer 
    {
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            return ValueSerializer.GetSerializerFor(typeof(Type), context) != null;
        }

        public override bool CanConvertFromString(string value, IValueSerializerContext context)
        {
            return ValueSerializer.GetSerializerFor(typeof(Type), context) != null;
        }

        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            DependencyProperty property = value as DependencyProperty;
            if (property != null)
            {
                ValueSerializer typeSerializer = ValueSerializer.GetSerializerFor(typeof(Type), context);
                if (typeSerializer != null)
                {
                    return typeSerializer.ConvertToString(property.OwnerType, context) + "." + property.Name;
                }
            }

            throw GetConvertToException(value, typeof(string));
        }

        public override IEnumerable<Type> TypeReferences(object value, IValueSerializerContext context)
        {
            DependencyProperty property = value as DependencyProperty;
            if (property != null)
            {
                return new Type[] { property.OwnerType };
            }
            else
            {
                return base.TypeReferences(value, context);
            }
        }

        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            ValueSerializer typeSerializer = ValueSerializer.GetSerializerFor(typeof(Type), context);
            if (typeSerializer != null)
            {
                int dotIndex = value.IndexOf('.');
                if (dotIndex >= 0)
                {
                    string typeName = value.Substring(0, dotIndex - 1);
                    Type ownerType = typeSerializer.ConvertFromString(typeName, context) as Type;
                    if (ownerType != null)
                    {
                        return DependencyProperty.FromName(typeName, ownerType);
                    }
                }
            }
            throw GetConvertFromException(value);
        }
    }
}

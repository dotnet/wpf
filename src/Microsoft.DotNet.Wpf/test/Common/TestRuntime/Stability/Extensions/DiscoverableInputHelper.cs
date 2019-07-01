// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.Test.Stability.Extensions
{
    internal static class DiscoverableInputHelper
    {
        public static List<PropertyDescriptor> GetInputProperties(Type t)
        {
            List<PropertyDescriptor> results = new List<PropertyDescriptor>();
            PropertyDescriptorCollection inputProperties = TypeDescriptor.GetProperties(t);
            foreach (PropertyDescriptor property in inputProperties)
            {
                results.Add(property);
            }
            return results;
        }

        public static List<Type> GetFactoryInputTypes(Type t)
        {
            List<Type> types = new List<Type>();
            foreach (PropertyDescriptor prop in GetInputProperties(t))
            {
                Type inputType = prop.PropertyType;
                //TODO: Find a better way of filtering to exlude non-default/non-factory requests
                if (!types.Contains(inputType) && IsFactoryProperty(prop))
                {
                    types.Add(inputType);
                }
            }
            return types;
        }

        private static bool IsFactoryProperty(PropertyDescriptor prop)
        {
            return !prop.Attributes.Contains(InputAttribute.CreateFromConstraints) 
                && !(prop.Attributes.Contains(InputAttribute.GetFromVisualTree)) 
                && !(prop.Attributes.Contains(InputAttribute.GetFromLogicalTree)) 
                && !(prop.Attributes.Contains(InputAttribute.GetFromObjectTree));
        }

        internal static List<Type> GetFactoryInputTypes(List<Type> objectTypes)
        {

            List<Type> demandedTypes = new List<Type>();
            foreach (Type t in objectTypes)
            {
                List<Type> dependencies = DiscoverableInputHelper.GetFactoryInputTypes(t);
                foreach (Type demandedType in dependencies)
                {
                    if (!demandedTypes.Contains(demandedType))
                    {
                        demandedTypes.Add(demandedType);
                    }
                }
            }
            return demandedTypes;
        }
    }
}

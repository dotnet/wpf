// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Windows;

using Microsoft.Test.Markup;

namespace Microsoft.Test.Windows
{
    /// <summary>
    /// Helper routines for testing Avalon types and properties.
    ///</summary>
    public static class ActionForTypeHelper
    {
        /// <summary>
        /// Uses same PropertyToIgnore type as TreeComparer.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="type"></param>
        /// <param name="skipProps"></param>
        /// <returns></returns>
        public static bool ShouldIgnoreProperty(string propertyName, Type type, Dictionary<string, PropertyToIgnore> skipProps)
        {
            PropertyToIgnore property = null;
            foreach (string key in skipProps.Keys)
            {
                if (String.Equals(key, propertyName, StringComparison.InvariantCulture)
                    || key.StartsWith(propertyName + "___owner___"))
                {
                    property = skipProps[key];
                    if ((null == property.Owner) || DoesTypeMatch(type, property.Owner))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        
        /// <summary>
        /// Checks that the current effective property value matches the expected value.
        /// </summary>
        /// <param name="styledElement"></param>
        /// <param name="unstyledElement"></param>
        /// <param name="dp"></param>
        /// <param name="skipProps"></param>
        public static void CheckExpectedPropertyValue(DependencyObject styledElement, DependencyObject unstyledElement, DependencyProperty dp, Dictionary<string, PropertyToIgnore> skipProps)
        {
            Type type = styledElement.GetType();
            bool isSameValue = true;

            object styledValue = styledElement.GetValue(dp);
            object localValue = unstyledElement.GetValue(dp);

            TreeCompareResult result = TreeComparer.CompareLogical(styledValue, localValue, skipProps);

            isSameValue = (CompareResult.Equivalent == result.Result);

            if (!isSameValue)
            {
                throw new TestValidationException("Unexpected '" + dp.Name + "' value on element '" + type.Name + "'.");
            }
        }

        /// <summary>
        /// Checks whether or not a type is of a certain type or derives from it.
        /// Checks the non-qualified name, i.e. no namespace check.
        /// </summary>
        private static bool DoesTypeMatch(Type ownerType, string typeName)
        {
            Type type = ownerType;
            bool isMatch = false;

            while (type != null && !isMatch)
            {
                if (0 == String.Compare(type.Name, typeName))
                {
                    isMatch = true;
                }

                type = type.BaseType;
            }

            return isMatch;
        }
    }
}

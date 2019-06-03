// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Reflection;

namespace System.Windows
{
    /// <summary>
    ///     Specifies where theme dictionaries are stored for types in an assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ThemeInfoAttribute : Attribute
    {
        /// <summary>
        ///     Creates an attribute that defines theme dictionary locations for types in an assembly.
        /// </summary>
        /// <param name="themeDictionaryLocation">The location of theme specific resources.</param>
        /// <param name="genericDictionaryLocation">The location of generic, not theme specific, resources.</param>
        public ThemeInfoAttribute(ResourceDictionaryLocation themeDictionaryLocation, ResourceDictionaryLocation genericDictionaryLocation)
        {
            _themeDictionaryLocation = themeDictionaryLocation;
            _genericDictionaryLocation = genericDictionaryLocation;
        }

        /// <summary>
        ///     The location of theme specific resources.
        /// </summary>
        public ResourceDictionaryLocation ThemeDictionaryLocation
        {
            get
            {
                return _themeDictionaryLocation;
            }
        }

        /// <summary>
        ///     The location of generic, not theme specific, resources.
        /// </summary>
        public ResourceDictionaryLocation GenericDictionaryLocation
        {
            get
            {
                return _genericDictionaryLocation;
            }
        }

        internal static ThemeInfoAttribute FromAssembly(Assembly assembly)
        {
            return Attribute.GetCustomAttribute(assembly, typeof(ThemeInfoAttribute)) as ThemeInfoAttribute;
        }

        private ResourceDictionaryLocation _themeDictionaryLocation;
        private ResourceDictionaryLocation _genericDictionaryLocation;
    }
}

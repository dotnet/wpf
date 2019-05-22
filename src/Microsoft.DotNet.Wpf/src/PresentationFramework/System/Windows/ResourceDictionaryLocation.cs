// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;

namespace System.Windows
{
    /// <summary>
    ///     Specifies the locations where theme resource dictionaries are located.
    /// </summary>
    public enum ResourceDictionaryLocation
    {
        /// <summary>
        ///     No theme dictionaries exist.
        /// </summary>
        None,

        /// <summary>
        ///     Theme dictionaries exist in the assembly that defines the types being themed.
        /// </summary>
        SourceAssembly,

        /// <summary>
        ///     Theme dictionaries exist in assemblies external to the one defining the types being themed.
        ///     These dictionaries are named based on the original assembly with the theme name appended to it.
        ///         Example: PresentationFramework.Luna.dll.
        ///     These dictionaries share the same version and key as the original assembly.
        /// </summary>
        ExternalAssembly,
    }
}

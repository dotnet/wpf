// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Markup
{
    using System.Runtime.CompilerServices;
    /// <summary>
    /// This is an interface that should be implemented by classes that wish to provide 
    /// fallback values for one or more of their properties.
    /// </summary>

    [TypeForwardedFrom("PresentationFramework, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    internal interface IProvidePropertyFallback
    {
        /// <summary>
        /// Says if the type can provide fallback value for the given property
        /// </summary>
        bool CanProvidePropertyFallback(string property);

        /// <summary>
        /// Returns the fallback value for the given property.
        /// </summary>
        object ProvidePropertyFallback(string property, Exception cause);
    }
}


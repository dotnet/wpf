// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{
    /// <summary>
    /// Provides services to help resolve nsPrefix:LocalName into the appropriate Type.
    /// </summary>
    [TypeForwardedFrom("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public interface IXamlTypeResolver
    {
        /// <summary>
        /// Resolves nsPrefix:LocalName into the appropriate Type.
        /// </summary>
        /// <param name="qualifiedTypeName">TypeName that appears in Xaml - nsPrefix:LocalName or LocalName.</param>
        /// <returns>
        /// The type that the qualifiedTypeName represents.
        /// </returns>
        Type Resolve(string qualifiedTypeName);
    }
}

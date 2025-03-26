// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Navigation
{
    /// <summary>
    /// Holds slices of pack/application URIs and exposes each slice through a property.
    /// </summary>
    /// <remarks>This should be dissected from relative URIs in format /AssemblyShortName{;Version]{;PublicKey];component/PackagePartName</remarks>
    internal ref struct AssemblyPackageInfo
    {
        /// <summary>
        /// Specifies the name of the package/resource.
        /// </summary>
        internal ReadOnlySpan<char> PackagePartName { get; set; }

        /// <summary>
        /// Holds Assembly.Name-like slice if specified, may be empty.
        /// </summary>
        /// <remarks>The slice is without leading forward slash ('/').</remarks>
        internal ReadOnlySpan<char> AssemblyName { get; set; }
        /// <summary>
        /// Holds Assembly.Version if specified, may be empty.
        /// </summary>
        /// <remarks>The slice is without 'v' prefix.</remarks>
        internal ReadOnlySpan<char> AssemblyVersion { get; set; }
        /// <summary>
        /// Holds Assembly.PublicKeyToken if specified, often is empty.
        /// </summary>
        internal ReadOnlySpan<char> AssemblyToken { get; set; }

    }
}

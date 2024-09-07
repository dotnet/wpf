// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace MS.Internal
{
    /// <summary>
    /// Provides utilities for working with reflection efficiently.
    /// </summary>
    internal static class ReflectionUtils
    {
        /// <summary>
        ///  Given an <paramref name="assembly"/>, returns the partial/simple name of the assembly.
        /// </summary>
        internal static string GetAssemblyPartialName(Assembly assembly)
        {
            AssemblyName name = new(assembly.FullName);
            return name.Name ?? string.Empty;
        }

        /// <summary>
        ///  Retrieves the full assembly name by combining the <paramref name="partialName"/> passed in
        ///  with everything else from <paramref name="assembly"/>.
        /// </summary>
        internal static string GetFullAssemblyNameFromPartialName(Assembly assembly, string partialName)
        {
            AssemblyName name = new(assembly.FullName) { Name = partialName };
            return name.FullName;
        }
    }
}

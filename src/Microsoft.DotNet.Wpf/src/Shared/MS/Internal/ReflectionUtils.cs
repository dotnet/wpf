// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System;

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
#if !NETFX
        internal static ReadOnlySpan<char> GetAssemblyPartialName(Assembly assembly)
#else
        internal static string GetAssemblyPartialName(Assembly assembly)
#endif
        {
#if !NETFX
            // We know that the input is trusted (it will be properly escaped, with ", " between tokens etc.)
            // So we can allow ourselves to do a little trick, where we just find the first separator
            // You cannot load an assembly (or define) where name is empty, it needs to be at least 1 character
            // But we will keep this for consistency of the previous function, maybe I've missed a class
            ReadOnlySpan<char> fullName = assembly.FullName;
            if (fullName.IsEmpty)
                return string.Empty;

            ReadOnlySpan<char> nameSlice = fullName;
            // Skip any escaped commas in the name
            int escapedComma = fullName.LastIndexOf("\\,");
            if (escapedComma != -1)
                nameSlice = nameSlice.Slice(escapedComma + 2);

            // Find the start of the next token (usually "Version")
            int commaIndex = nameSlice.IndexOf(',');
            if (commaIndex != -1)
                fullName = fullName.Slice(0, fullName.Length - nameSlice.Length + commaIndex);

            return fullName;
#else
            AssemblyName name = new(assembly.FullName);
            return name.Name ?? string.Empty;
#endif
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

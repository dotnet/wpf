// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Runtime.CompilerServices;
using System.Reflection.Metadata;
using System.Reflection;
using System.Text;
using System;

namespace MS.Internal
{
    /// <summary>
    /// Provides utilities for working with reflection efficiently.
    /// </summary>
    internal static class ReflectionUtils
    {
#if !NETFX
        private const string Version = ", Version=";
        private const string PublicKeyToken = ", PublicKeyToken=";

        /// <summary>
        /// Retrieves the full assembly name by combining the <paramref name="partialName"/> passed in
        /// with everything else from <paramref name="assembly"/>.
        /// </summary>
        internal static string GetFullAssemblyNameFromPartialName(Assembly assembly, string partialName)
        {
            ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));
            string? fullName = assembly.FullName;
            ArgumentNullException.ThrowIfNull(fullName, nameof(Assembly.FullName));

            AssemblyName name = new(fullName) { Name = partialName };
            return name.FullName;
        }
#endif

        /// <summary>
        /// Given an <paramref name="assembly"/>, returns the partial/simple name of the assembly.
        /// </summary>
#if !NETFX
        internal static ReadOnlySpan<char> GetAssemblyPartialName(Assembly assembly)
#else
        internal static string GetAssemblyPartialName(Assembly assembly)
#endif
        {
#if !NETFX
            ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));
            // We know that the input is trusted (it will be properly escaped, with ", " between tokens etc.)
            // So we can allow ourselves to do a little trick, where we just find the first separator
            // You cannot load an assembly (or define) where name is empty, it needs to be at least 1 character
            // But we will keep this for consistency of the previous function, maybe I've missed a class
            ReadOnlySpan<char> fullName = assembly.FullName;
            if (fullName.IsEmpty)
                return ReadOnlySpan<char>.Empty;

            ReadOnlySpan<char> nameSlice = fullName;
            // Skip any escaped commas in the name if present
            int escapedComma = fullName.LastIndexOf("\\,", StringComparison.Ordinal);
            if (escapedComma != -1)
                nameSlice = nameSlice.Slice(escapedComma + 2);

            // Find the real ending of the name section
            int commaIndex = nameSlice.IndexOf(',');
            if (commaIndex != -1)
                fullName = fullName.Slice(0, fullName.Length - nameSlice.Length + commaIndex);

            // Check if we need to unescape, this is very rare case so we can just do it the dirty way
            if (escapedComma != -1 || fullName.Contains('\\'))
                UnescapeDirty(ref fullName);

            // Since having "," or "=" in the assembly name is very rare, we don't want to inline
            // and we will fallback to the runtime implementation to handle such case for us
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void UnescapeDirty(ref ReadOnlySpan<char> dirtyName)
            {
                dirtyName = !AssemblyNameInfo.TryParse(dirtyName, out AssemblyNameInfo? result) ? ReadOnlySpan<char>.Empty : result.Name;
            }

            return fullName;
#else
            AssemblyName name = new(assembly.FullName);
            return name.Name ?? string.Empty;
#endif
        }

#if !NETFX
        /// <summary>
        /// Parses <see cref="Assembly.FullName"/> and retrieves "Version" and "PublicKeyToken" values from the original string.
        /// This should only be passed a RuntimeAssembly to ensure proper functionality.
        /// </summary>
        /// <param name="assembly">The RuntimeAssembly which will provide properly formatted full name.</param>
        /// <param name="version">If present, returns the value of Version portion, otherwise Empty result.</param>
        /// <param name="token">If present, returns the value of PublicKeyToken portion. Empty result is returned when the value is "null" or not present.</param>
        internal static void GetAssemblyVersionPlusToken(Assembly assembly, out ReadOnlySpan<char> assemblyVersion, out ReadOnlySpan<char> assemblyToken)
        {
            ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));
            ReadOnlySpan<char> assemblyName = assembly.FullName;

            assemblyVersion = ReadOnlySpan<char>.Empty;
            assemblyToken = ReadOnlySpan<char>.Empty;

            // Parse Version section
            int versionIndex = assemblyName.IndexOf(Version);
            if (versionIndex != -1)
            {
                int tokenEnding = assemblyName.Slice(versionIndex + 1).IndexOf(',') + 1;
                int tokenLength = tokenEnding == 0 ? assemblyName.Slice(versionIndex).Length : tokenEnding;

                assemblyVersion = assemblyName.Slice(versionIndex + Version.Length, tokenLength - Version.Length);
            }

            // Parse PublicKeyToken section
            int tokenIndex = assemblyName.IndexOf(PublicKeyToken);
            if (tokenIndex != -1)
            {
                int tokenEnding = assemblyName.Slice(tokenIndex + 1).IndexOf(',') + 1;
                int tokenLength = tokenEnding == 0 ? assemblyName.Slice(tokenIndex).Length : tokenEnding;

                // PublicKeyToken is always 8 bytes (16 chars in HEX), in other cases it is gonna be "null",
                // however it is simply faster to match it via Length as original parser does it than anything else
                assemblyToken = assemblyName.Slice(tokenIndex + PublicKeyToken.Length, tokenLength - PublicKeyToken.Length);
                if (assemblyToken.Length != 16)
                    assemblyToken = ReadOnlySpan<char>.Empty;
            }
        }
#endif
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xaml.MS.Impl;

namespace System.Xaml.Schema
{
    internal static class ClrNamespaceUriParser
    {
        public static string GetUri(ReadOnlySpan<char> clrNs, ReadOnlySpan<char> assemblyName)
        {
            return $"{KnownStrings.UriClrNamespace}:{clrNs};{KnownStrings.UriAssembly}={assemblyName}";
        }

        public static bool TryParseUri(string uriInput, out ReadOnlySpan<char> clrNs, out ReadOnlySpan<char> assemblyName)
        {
            clrNs = ReadOnlySpan<char>.Empty;
            assemblyName = ReadOnlySpan<char>.Empty;

            // xmlns:foo="clr-namespace:System.Windows;assembly=myassemblyname"
            // xmlns:bar="clr-namespace:MyAppsNs"
            // xmlns:spam="clr-namespace:MyAppsNs;assembly="

            int colonIdx = uriInput.IndexOf(':', StringComparison.Ordinal);
            if (colonIdx == -1)
            {
                return false;
            }

            ReadOnlySpan<char> uriInputSpan = uriInput;

            ReadOnlySpan<char> keyword = uriInputSpan.Slice(0, colonIdx);
            if (!keyword.Equals(KnownStrings.UriClrNamespace, StringComparison.Ordinal))
            {
                return false;
            }

            int clrNsStartIdx = colonIdx + 1;
            int semicolonIdx = uriInput.IndexOf(';', StringComparison.Ordinal);
            if (semicolonIdx == -1)
            {
                clrNs = uriInputSpan.Slice(clrNsStartIdx);
                return true;
            }
            else
            {
                int clrNsLength = semicolonIdx - clrNsStartIdx;
                clrNs = uriInputSpan.Slice(clrNsStartIdx, clrNsLength);
            }

            int assemblyKeywordStartIdx = semicolonIdx + 1;
            int equalIdx = uriInput.IndexOf('=', StringComparison.Ordinal);
            if (equalIdx == -1)
            {
                return false;
            }

            keyword = uriInputSpan.Slice(assemblyKeywordStartIdx, equalIdx - assemblyKeywordStartIdx);
            if (!keyword.Equals(KnownStrings.UriAssembly, StringComparison.Ordinal))
            {
                return false;
            }

            assemblyName = uriInputSpan.Slice(equalIdx + 1);
            return true;
        }
    }
}

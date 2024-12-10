// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Windows.Primitives.Resources;

internal static partial class SR
{
    // The rest of this class is auto-generated. Normally, the following should also be generated, but it
    // currently isn't. This may be related to building with the .NET Framework msbuild.

    [AllowNull]
    internal static CultureInfo Culture { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string? GetResourceString(string resourceKey, string? defaultValue = null) =>
        ResourceManager.GetString(resourceKey, Culture);
}

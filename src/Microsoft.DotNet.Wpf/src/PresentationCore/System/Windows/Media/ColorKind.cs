// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Media;

/// <summary>
/// Encapsulates color kinds as categorized via <see cref="KnownColors.MatchColor(ReadOnlySpan{char})"/>
/// </summary>
internal enum ColorKind
{
    /// <summary>
    /// Unused but it is the default value.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Just a standard #HEXCOLOR format.
    /// </summary>
    NumericColor = 1,
    /// <summary>
    /// Color prefixed with "ContextColor ".
    /// </summary>
    ContextColor = 2,
    /// <summary>
    /// Color prefixed with "sc#".
    /// </summary>
    ScRgbColor = 3,
    /// <summary>
    /// Fallback if none of the previous ones matched.
    /// </summary>
    KnownColor = 4
}

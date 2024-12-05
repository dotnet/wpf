// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace MS.Internal.Hashing.PresentationFramework;

internal static class HashHelper
{
    private static readonly Type[] s_unreliableTypes =
    [
        // The first four are from PresentationCore
        typeof(System.Windows.Media.CharacterMetrics),                      // bug 1612093
        typeof(System.Windows.Ink.ExtendedProperty),                        // bug 1612101
        typeof(System.Windows.Media.FamilyTypeface),                        // bug 1612103
        typeof(System.Windows.Media.NumberSubstitution),                    // bug 1612105

        // Next two are PresentationFramework
        typeof(System.Windows.Markup.Localizer.BamlLocalizableResource),    // bug 1612118
        typeof(System.Windows.ComponentResourceKey)                         // bug 1612119
    ];

    /// <summary>
    ///  Certain objects don't have reliable hashcodes, and cannot be used in a Hashtable, Dictionary, etc.
    /// </summary>
    internal static bool HasReliableHashCode(object item) => item is not null && !s_unreliableTypes.Contains(item.GetType());
}

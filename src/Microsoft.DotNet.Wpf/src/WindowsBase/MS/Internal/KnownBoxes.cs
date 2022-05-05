// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.KnownBoxes
{
    internal static class BooleanBoxes
    {
        internal static readonly object TrueBox = true;
        internal static readonly object FalseBox = false;

        internal static object Box(bool value) =>
            value ? TrueBox : FalseBox;

        internal static object Box(bool? value) =>
            value switch
            {
                true => TrueBox,
                false => FalseBox,
                null => null,
            };
    }
}

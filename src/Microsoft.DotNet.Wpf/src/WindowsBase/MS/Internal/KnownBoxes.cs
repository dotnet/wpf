// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MS.Internal.WindowsBase;

namespace MS.Internal.KnownBoxes
{
    [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
    internal static class BooleanBoxes
    {
        internal static object TrueBox = true;
        internal static object FalseBox = false;

        internal static object Box(bool value)
        {
            if (value)
            {
                return TrueBox;
            }
            else
            {
                return FalseBox;
            }
        }
    }

    [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
    internal static class NullableBooleanBoxes
    {
        internal static object TrueBox = (bool?)true;
        internal static object FalseBox = (bool?)false;
        internal static object NullBox = (bool?)null;

        internal static object Box(bool? value)
        {
            if (value.HasValue)
            {
                if (value == true)
                {
                    return TrueBox;
                }
                else
                {
                    return FalseBox;
                }
            }
            else
            {
                return NullBox;
            }
        }
    }
}

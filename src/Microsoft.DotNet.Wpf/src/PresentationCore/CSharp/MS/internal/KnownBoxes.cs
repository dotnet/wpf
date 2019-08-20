// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;

using MS.Internal.PresentationCore;

namespace MS.Internal.KnownBoxes
{
    [FriendAccessAllowed] // Built into Core, also used by Framework.
    internal static class FillRuleBoxes
    {
        internal static object EvenOddBox = FillRule.EvenOdd;
        internal static object NonzeroBox = FillRule.Nonzero;

        internal static object Box(FillRule value)
        {
            if (value == FillRule.Nonzero)
            {
                return NonzeroBox;
            }
            else
            {
                return EvenOddBox;
            }
        }
    }

    [FriendAccessAllowed] // Built into Core, also used by Framework.
    internal static class VisibilityBoxes
    {
        internal static object VisibleBox = Visibility.Visible;
        internal static object HiddenBox = Visibility.Hidden;
        internal static object CollapsedBox = Visibility.Collapsed;

        internal static object Box(Visibility value)
        {
            if (value == Visibility.Visible)
            {
                return VisibleBox;
            }
            else if (value == Visibility.Hidden)
            {
                return HiddenBox;
            }
            else
            {
                return CollapsedBox;
            }
        }
    }
}

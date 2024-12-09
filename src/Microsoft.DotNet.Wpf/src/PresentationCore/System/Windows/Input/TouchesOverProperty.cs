// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Input
{
    internal class TouchesOverProperty : ReverseInheritProperty
    {
        internal TouchesOverProperty() :
            base(UIElement.AreAnyTouchesOverPropertyKey,
            CoreFlags.TouchesOverCache,
            CoreFlags.TouchesOverChanged,
            CoreFlags.TouchLeaveCache,
            CoreFlags.TouchEnterCache)
        {
        }

        internal override void FireNotifications(UIElement uie, ContentElement ce, UIElement3D uie3D, bool oldValue)
        {
        }
    }
}


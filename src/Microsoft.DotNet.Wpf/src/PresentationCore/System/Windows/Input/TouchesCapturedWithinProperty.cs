// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Input
{
    internal class TouchesCapturedWithinProperty : ReverseInheritProperty
    {
        internal TouchesCapturedWithinProperty() : 
            base(UIElement.AreAnyTouchesCapturedWithinPropertyKey,
            CoreFlags.TouchesCapturedWithinCache,
            CoreFlags.TouchesCapturedWithinChanged)
        {
        }

        internal override void FireNotifications(UIElement uie, ContentElement ce, UIElement3D uie3D, bool oldValue)
        {
        }
    }
}


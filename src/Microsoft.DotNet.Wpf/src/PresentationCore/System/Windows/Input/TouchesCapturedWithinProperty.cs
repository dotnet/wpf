// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Input;
using MS.Internal.KnownBoxes;

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


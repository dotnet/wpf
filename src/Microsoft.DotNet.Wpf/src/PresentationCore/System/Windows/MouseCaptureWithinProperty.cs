// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Input;
using MS.Internal.KnownBoxes;

namespace System.Windows
{
    /////////////////////////////////////////////////////////////////////////

    internal class MouseCaptureWithinProperty : ReverseInheritProperty
    {
        /////////////////////////////////////////////////////////////////////

        internal MouseCaptureWithinProperty() : base(
            UIElement.IsMouseCaptureWithinPropertyKey,
            CoreFlags.IsMouseCaptureWithinCache,
            CoreFlags.IsMouseCaptureWithinChanged)
        {
        }

        /////////////////////////////////////////////////////////////////////

        internal override void FireNotifications(UIElement uie, ContentElement ce, UIElement3D uie3D, bool oldValue)
        {
            DependencyPropertyChangedEventArgs args = 
                    new DependencyPropertyChangedEventArgs(
                        UIElement.IsMouseCaptureWithinProperty, 
                        BooleanBoxes.Box(oldValue), 
                        BooleanBoxes.Box(!oldValue));
            
            if (uie != null)
            {
                uie.RaiseIsMouseCaptureWithinChanged(args);
            }
            else if (ce != null)
            {
                ce.RaiseIsMouseCaptureWithinChanged(args);
            }
            else if (uie3D != null)
            {
                uie3D.RaiseIsMouseCaptureWithinChanged(args);
            }
        }
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Input;
using MS.Internal.KnownBoxes;

namespace System.Windows
{
    /////////////////////////////////////////////////////////////////////////

    internal class StylusOverProperty : ReverseInheritProperty
    {
        /////////////////////////////////////////////////////////////////////

        internal StylusOverProperty() : base(
            UIElement.IsStylusOverPropertyKey,
            CoreFlags.IsStylusOverCache,
            CoreFlags.IsStylusOverChanged)
        {
        }

        /////////////////////////////////////////////////////////////////////

        internal override void FireNotifications(UIElement uie, ContentElement ce, UIElement3D uie3D, bool oldValue)
        {
            // This is all very sketchy...
            //
            // Tablet can support multiple stylus devices concurrently.  They can each
            // be over a different element.  They all update the IsStylusOver property,
            // which calls into here, but ends up using the "current" stylus device,
            // instead of each using their own device.  Worse, all of these will end up
            // writing to the same bits in the UIElement.  They are going to step all over
            // each other.
            if(Stylus.CurrentStylusDevice == null)
            {
                return;
            }
            
            StylusEventArgs stylusEventArgs = new StylusEventArgs(Stylus.CurrentStylusDevice, Environment.TickCount);
            stylusEventArgs.RoutedEvent = oldValue ? Stylus.StylusLeaveEvent : Stylus.StylusEnterEvent;

            if (uie != null)
            {
                uie.RaiseEvent(stylusEventArgs);
            }
            else if (ce != null)
            {
                ce.RaiseEvent(stylusEventArgs);
            }
            else if (uie3D != null)
            {
                uie3D.RaiseEvent(stylusEventArgs);
            }
        }
    }
}

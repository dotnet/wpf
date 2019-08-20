// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Media;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///Event argument used to subscribe to StylusDown events. 
    /// </summary>
    public class StylusDownEventArgs : StylusEventArgs
    {
        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Initializes a new instance of the StylusDownEventArgs class.
        /// </summary>
        /// <param name="stylusDevice">
        ///     The logical Stylus device associated with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        public StylusDownEventArgs(
            StylusDevice stylusDevice, int timestamp)
            :
            base(stylusDevice, timestamp)
        {
        }

        /// <summary>
        ///     Read access to the stylus tap count.
        /// </summary>
        public int TapCount
        {
            get {return StylusDeviceImpl.TapCount;}
        }
        
        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The mechanism used to call the type-specific handler on the
        ///     target.
        /// </summary>
        /// <param name="genericHandler">
        ///     The generic handler to call in a type-specific way.
        /// </param>
        /// <param name="genericTarget">
        ///     The target to call the handler on.
        /// </param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            StylusDownEventHandler handler = (StylusDownEventHandler)genericHandler;
            handler(genericTarget, this);
        }
}
}

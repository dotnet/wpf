// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows.Media;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The StylusEventArgs class provides access to the logical
    ///     Stylus device for all derived event args.
    /// </summary>
    public class StylusEventArgs : InputEventArgs
    {
        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Initializes a new instance of the StylusEventArgs class.
        /// </summary>
        /// <param name="stylus">
        ///     The logical Stylus device associated with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        public StylusEventArgs(StylusDevice stylus, int timestamp) : base(stylus, timestamp)
        {
            if( stylus == null )
            {
                throw new System.ArgumentNullException("stylus");
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Read-only access to the stylus device associated with this
        ///     event.
        /// </summary>
        public StylusDevice StylusDevice
        {
            get
            {
                return (StylusDevice)this.Device;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Read-only access to the stylus device associated with this
        ///     event.
        /// </summary>
        internal StylusDeviceBase StylusDeviceImpl
        {
            get
            {
                return ((StylusDevice)this.Device).StylusDeviceImpl;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Calculates the position of the stylus relative to a particular element.
        /// </summary>
        public Point GetPosition(IInputElement relativeTo) 
        {
            return StylusDevice.GetPosition(relativeTo);
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Indicates the stylus is not touching the surface. 
        /// </summary>
        public bool InAir 
        { 
            get
            {
                return StylusDevice.InAir;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Indicates stylusDevice is in the inverted state.
        /// </summary>
        public bool Inverted 
        { 
            get
            {
                return StylusDevice.Inverted;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Returns a StylusPointCollection for processing the data from input.
        ///		This method creates a new StylusPointCollection and copies the data.
        /// </summary>
        public StylusPointCollection GetStylusPoints(IInputElement relativeTo)
        {
            return StylusDevice.GetStylusPoints(relativeTo);
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Returns a StylusPointCollection for processing the data from input.
        ///		This method creates a new StylusPointCollection and copies the data.
        /// </summary>
        public StylusPointCollection GetStylusPoints(IInputElement relativeTo, StylusPointDescription subsetToReformatTo)
        {
            return StylusDevice.GetStylusPoints(relativeTo, subsetToReformatTo);
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
            StylusEventHandler handler = (StylusEventHandler) genericHandler;
            handler(genericTarget, this);
        }

        /////////////////////////////////////////////////////////////////////
        
        internal RawStylusInputReport InputReport
        {
            get { return _inputReport;  }
            set { _inputReport = value; }
        }

        /////////////////////////////////////////////////////////////////////

        RawStylusInputReport    _inputReport;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// StylusButton class
    /// </summary>
    public class StylusButton
    {
        /////////////////////////////////////////////////////////////////////

        internal StylusButton(string name, Guid id)
        {
            _name = name;
            _guid = id;
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Returns the hardware Guid of the StylusDevice button.
        /// </summary>
        public Guid Guid
        {
            get
            {
                return _guid;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Returns the current state of the button.
        /// </summary>
        public StylusButtonState StylusButtonState
        {
            // Ideally, we should be locally caching the button state and not read from stylusdevice.
            get
            {
                StylusPointCollection stylusPoints = _stylusDevice.GetStylusPoints(null);
                if (stylusPoints == null || stylusPoints.Count == 0)
                    return CachedButtonState;

                return (StylusButtonState)stylusPoints[stylusPoints.Count - 1].GetPropertyValue(new StylusPointProperty(Guid, true));
            }
        }

        internal StylusButtonState CachedButtonState
        {
            get
            {
                return _cachedButtonState;
            }
            set
            {
                _cachedButtonState = value;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Returns the name of the button.
        /// </summary>
        public string Name
        { 
            get
            {	
                return _name;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Returns StylusDevice object that owns this button.
        /// </summary>
        public StylusDevice StylusDevice
        { 
            get
            {	
                return _stylusDevice.StylusDevice;
            }
        }

        /////////////////////////////////////////////////////////////////////
        ///
        internal void SetOwner(StylusDeviceBase stylusDevice)
        {
            _stylusDevice = stylusDevice;
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Returns the friendly representation of the button object
        /// </summary>
        /// <returns><see cref="System.String"/> name of the tablet</returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}({1})", base.ToString(), this.Name);
        }

        /////////////////////////////////////////////////////////////////////

        StylusDeviceBase    _stylusDevice;
        string          _name;
        Guid            _guid;
        StylusButtonState _cachedButtonState = StylusButtonState.Up;
    }
}

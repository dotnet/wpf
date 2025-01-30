// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///		Class containing only static methods to access tablet info.
    /// </summary>
    public static class Tablet
    {
        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Read-only access to the Tablet device associated with the current event
        ///     for the current input manager.
        /// </summary>
        public static TabletDevice CurrentTabletDevice
        { 
            get 
            {
                StylusDevice stylus = Stylus.CurrentStylusDevice;
                if (stylus == null)
                    return null;
                return stylus.TabletDevice;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Returns the collection of Tablet Devices defined on this tablet. 
        /// </summary>
        public static TabletDeviceCollection TabletDevices
        { 
            get 
            {
                // If there is no stylus logic (the stacks are disabled) return an empty collection.
                return StylusLogic.CurrentStylusLogic?.TabletDevices ?? TabletDeviceCollection.EmptyTabletDeviceCollection;
            } 
        }
}
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Utility
{
    /// <summary>
    /// An enumeration that maps to the DPI_AWARENESS_CONTEXT pseudo handles
    /// </summary>
    /// <remarks>
    /// This is an internal enumeration. There is no analogue
    /// for this in the Windows headers
    ///
    /// This a very important enum and these values should not
    /// be changed lightly.
    ///
    /// HwndTarget keeps track of its own DPI_AWARENESS_CONTEXT using this
    /// enum, and passes along this value directly to the renderer.
    ///
    /// Eventually, this is interpreted within DpiProvider::SetDpiAwarenessContext
    /// as a DPI_AWARENESS_CONTEXT (pseudo) handle. For this internal protocol
    /// to work correctly, the values used here need to remain in sync with
    /// (a) the values using in DpiProvider::SetDpiAwarenessContext and
    /// (b) the values used to initialize the DPI_AWARENESS_CONTEXT
    /// (pseudo) handles in the Widnows headers.
    /// </remarks>
    internal enum DpiAwarenessContextValue : int
    {
        /// <summary>
        /// Invalid value
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// DPI_AWARENESS_CONTEXT_UNAWARE
        /// </summary>
        Unaware = -1,

        /// <summary>
        /// DPI_AWARENESS_CONTEXT_SYSTEM_AWARE
        /// </summary>
        SystemAware = -2,

        /// <summary>
        /// DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE
        /// </summary>
        PerMonitorAware = -3,

        /// <summary>
        /// DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
        /// </summary>
        PerMonitorAwareVersion2 = -4
    }
}
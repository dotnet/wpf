// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Multiple View pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    ///  Exposes an element's ability to switch between multiple representations of the 
    ///  same set of information, data, or children
    ///
    ///  This pattern should be implemented on the container which controls the current view of content.
    /// </summary>
    [ComVisible(true)]
    [Guid("6278cab1-b556-4a1a-b4e0-418acc523201")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IMultipleViewProvider
#else
    public interface IMultipleViewProvider
#endif
    {
        /// <summary>
        /// The string view name string must be suitable for use by TTS, Braille, etc.
        /// </summary>
        /// <param name="viewId">
        /// The view ID corresponding to the control's current state. This ID is control-specific and can should
        /// be the same across instances.
        /// </param>
        /// <returns>Return a localized, human readable string in the application's current UI language.</returns>
        string GetViewName( int viewId );

        /// <summary>
        /// Change the current view using an ID returned from SupportedViews property
        /// </summary>
        void SetCurrentView( int viewId );    

        /// <summary>The view ID corresponding to the control's current state. This ID is control-specific</summary>
        int CurrentView
        {
            get;
        }

        /// <summary>Returns an array of ints representing the full set of views available in this control.</summary>
        int [] GetSupportedViews();
    }
}

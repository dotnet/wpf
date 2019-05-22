// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Root provider interface for n-level UI

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;


namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// The root element in a fragment of UI must support this interface. Other
    /// elements in the same fragment need to support the IRawElementProviderFragment
    /// interface.
    /// </summary>
    [ComVisible(true)]
    [Guid("620ce2a5-ab8f-40a9-86cb-de3c75599b58")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IRawElementProviderFragmentRoot : IRawElementProviderFragment
#else
    public interface IRawElementProviderFragmentRoot : IRawElementProviderFragment
#endif
    {
        // Should we use Point now that it's available? Physical vs logical pixels?
        /// <summary>
        /// Return the child element at the specified point, if one exists,
        /// otherwise return this element if the point is on this element,
        /// otherwise return null.
        /// </summary>
        /// <param name="x">x coordinate of point to check</param>
        /// <param name="y">y coordinate of point to check</param>
        /// <returns>Return the child element at the specified point, if one exists,
        /// otherwise return this element if the point is on this element,
        /// otherwise return null.
        /// </returns>
        IRawElementProviderFragment ElementProviderFromPoint( double x, double y );

        /// <summary>
        /// Return the element in this fragment which has the keyboard focus,
        /// </summary>
        /// <returns>Return the element in this fragment which has the keyboard focus,
        /// if any; otherwise return null.</returns>
        IRawElementProviderFragment GetFocus();
    }
}

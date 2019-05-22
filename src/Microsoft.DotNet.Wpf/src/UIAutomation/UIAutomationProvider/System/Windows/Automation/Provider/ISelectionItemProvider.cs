// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Selection Item pattern provider interface

using System;
using System.Collections;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{

    /// <summary>
    /// Define a Selectable Item (only supported on logical elements that are a 
    /// child of an Element that supports SelectionPattern and is itself selectable).  
    /// This allows for manipulation of Selection from the element itself.
    /// </summary>
    [ComVisible(true)]
    [Guid("2acad808-b2d4-452d-a407-91ff1ad167b2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface ISelectionItemProvider
#else
    public interface ISelectionItemProvider
#endif
    {
        /// <summary>
        /// Sets the current element as the selection
        /// This clears the selection from other elements in the container 
        /// </summary>
        void Select();

        /// <summary>
        /// Adds current element to selection
        /// </summary>
        void AddToSelection();
        
        /// <summary>
        /// Removes current element from selection
        /// </summary>
        void RemoveFromSelection();

        /// <summary>
        /// Check whether an element is selected
        /// </summary>
        /// <returns>returns true if the element is selected</returns>
        bool IsSelected
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }

        /// <summary>
        /// The logical element that supports the SelectionPattern for this Item
        /// </summary>
        /// <returns>returns a IRawElementProviderSimple</returns>
        IRawElementProviderSimple SelectionContainer
        {
            get;
        }
    }
}

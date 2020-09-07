// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Selection pattern provider interface

using System;
using System.Collections;
using System.Windows.Automation;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{

    /// <summary>
    /// public interface representing containers that manage selection.
    /// </summary>
    /// <remarks>
    /// Client code uses this public interface; server implementers implent the
    /// ISelectionProvider public interface instead.
    /// </remarks>
    [ComVisible(true)]
    [Guid("fb8b03af-3bdf-48d4-bd36-1a65793be168")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface ISelectionProvider
#else
    public interface ISelectionProvider
#endif
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Get the currently selected elements
        /// </summary>
        /// <returns>An AutomationElement array containing the currently selected elements</returns>
        IRawElementProviderSimple [] GetSelection();

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Indicates whether the control allows more than one element to be selected
        /// </summary>
        /// <returns>Boolean indicating whether the control allows more than one element to be selected</returns>
        /// <remarks>If this is false, then the control is a single-select ccntrol</remarks>
        bool CanSelectMultiple
        {
            [return: MarshalAs(UnmanagedType.Bool)] //  Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }

        /// <summary>
        /// Indicates whether the control requires at least one element to be selected
        /// </summary>
        /// <returns>Boolean indicating whether the control requires at least one element to be selected</returns>
        /// <remarks>If this is false, then the control allows all elements to be unselected</remarks>
        bool IsSelectionRequired 
        {
            [return: MarshalAs(UnmanagedType.Bool)] //  Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }

        #endregion Public Properties
    }
}

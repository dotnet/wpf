// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implements Avalon WindowInteropHelper classes, which helps
//              interop b/w legacy and Avalon Window.
//


using System;
using System.Windows;
using System.Windows.Interop;
using System.Security;
using System.Diagnostics;
using MS.Internal;
using MS.Win32;

namespace System.Windows.Interop
{
    #region class WindowInteropHelper
    /// <summary>
    /// Implements Avalon WindowInteropHelper classes, which helps 
    /// interop b/w legacy and Avalon Window.
    /// </summary>
    public sealed class WindowInteropHelper
    {
        //---------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------
        #region Constructors
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="window"></param>
        public WindowInteropHelper(Window window)
        {
            if (window == null)
                throw new ArgumentNullException("window");
            _window = window;
        }

        #endregion Constructors


        //---------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------
        #region Public Properties

        /// <summary>
        /// Get the Handle of the window
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public IntPtr Handle
        {
            get
            {
                return CriticalHandle;
            }
        }

        internal IntPtr CriticalHandle
        {
            get
            {
                Invariant.Assert(_window != null, "Cannot be null since we verify in the constructor");
                return _window.CriticalHandle;
            }
        }

        /// <summary>
        /// Get/Set the Owner handle of the window
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public IntPtr Owner
        {
            get
            {
                Debug.Assert(_window != null, "Cannot be null since we verify in the constructor");
                return _window.OwnerHandle;
            }
            set
            {
                Debug.Assert(_window != null, "Cannot be null since we verify in the constructor");
                // error checking done in Window
                _window.OwnerHandle = value;
            }
        }

        #endregion Public Properties

        //---------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------
        #region Public Methods

        /// <summary>
        /// Create the hwnd of the Window if the hwnd is not created yet.
        /// </summary>
        public IntPtr EnsureHandle()
        {

            if (CriticalHandle == IntPtr.Zero)
            {
                _window.CreateSourceWindow(false /*create hwnd during show*/);
            }

            return CriticalHandle;
        }

        #endregion Public Methods

        //----------------------------------------------
        //
        // Private Fields
        //
        //----------------------------------------------
        #region Private Fields
                
        private Window      _window;

        #endregion Private Members
    }
    #endregion class WindowInteropHelper
}


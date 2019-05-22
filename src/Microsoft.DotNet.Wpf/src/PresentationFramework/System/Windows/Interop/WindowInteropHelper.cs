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
using System.Security.Permissions;
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
        /// <SecurityNote>
        ///   Critical: Exposes a handle
        ///   PublicOK: There is a demand , this API not available in internet zone
        /// </SecurityNote>
        public IntPtr Handle
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUIWindowPermission();
                return CriticalHandle;
            }
        }

        /// <SecurityNote>
        ///   Critical: Exposes a handle
        /// </SecurityNote>
        internal IntPtr CriticalHandle
        {
            [SecurityCritical]
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
        /// <SecurityNote>
        ///   Critical: Exposes a handle
        ///   PublicOK: There is a demand , this API not available in internet zone
        /// </SecurityNote>
        public IntPtr Owner
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUIWindowPermission();
                Debug.Assert(_window != null, "Cannot be null since we verify in the constructor");
                return _window.OwnerHandle;
            }
            [SecurityCritical]
            set
            {
                SecurityHelper.DemandUIWindowPermission();
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
        /// <SecurityNote>
        ///   Critical: Create and exposes the window handle.
        ///   PublicOK: We demand UIPermission.
        /// </SecurityNote>
        [SecurityCritical]
        public IntPtr EnsureHandle()
        {
            SecurityHelper.DemandUIWindowPermission();

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


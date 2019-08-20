// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Implements Avalon CursorInteropHelper class, which helps
//              interop b/w Cursor handles and Avalon Cursor objects.
//
//
// 06/30/05     jdmack      Created

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Input;
using SecurityHelper=MS.Internal.SecurityHelper; 

namespace System.Windows.Interop
{
    #region class CursorInteropHelper
    /// <summary>
    ///     Implements Avalon CursorInteropHelper classes, which helps
    ///     interop b/w legacy Cursor handles and Avalon Cursor objects.
    /// </summary>
    public static class CursorInteropHelper
    {
        //---------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------
        #region Public Methods

        /// <summary>
        ///     Creates a Cursor from a SafeHandle to a native Win32 Cursor
        /// </summary>
        /// <param name="cursorHandle">
        ///     SafeHandle to a native Win32 cursor
        /// </param>
        ///<remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        ///</remarks>
        public static Cursor Create(SafeHandle cursorHandle)
        {

            return CriticalCreate(cursorHandle);
        }

        #endregion Public Methods

        //---------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------
        #region Internal Methods

        /// <summary>
        ///     Creates a Cursor from a SafeHandle to a native Win32 Cursor
        /// </summary>
        /// <param name="cursorHandle">
        ///     SafeHandle to a native Win32 cursor
        /// </param>
        internal static Cursor CriticalCreate(SafeHandle cursorHandle)
        {
            return new Cursor(cursorHandle);
        }

        #endregion Internal Methods
    }
    #endregion class CursorInteropHelper
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              CommonDialog is a base class representing common dialogs.
//              At this time, we intend it only to be used as a parent class
//              for the FileDialog class, although it could be used to implement
//              other commdlg.dll dialogs in the future.  It is not a
//              general-purpose dialog class - it's specific to Win32 common
//              dialogs.
//
// 


namespace Microsoft.Win32
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Threading;
    using System.Windows;
    using System.Windows.Interop;

    using MS.Internal.Interop;
    using MS.Internal.PresentationFramework;
    using MS.Win32;

    /// <summary>
    ///  An abstract base class for displaying common dialogs.
    /// </summary>
    /// <Remarks>
    ///     InheritanceDemand for UIPermission (UIPermissionWindow.AllWindows)
    /// </Remarks>
    public abstract class CommonDialog
    {
        //---------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------
        //#region Constructors
        //#endregion Constructors

        //---------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------
        #region Public Methods

        /// <summary>
        ///  When overridden in a derived class, resets the properties 
        ///  of a common dialog to their default values.
        /// </summary>
        public abstract void Reset();

        /// <summary>
        ///  This is the public method that will be called to actually show
        ///  a common dialog.  Since CommonDialog is abstract, this function
        ///  performs initialization tasks for all common dialogs and then
        ///  calls RunDialog.
        /// </summary>
        /// <Remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </Remarks>
        public virtual Nullable<bool> ShowDialog()
        {
            CheckPermissionsToShowDialog();

            // Don't allow file dialogs to be shown if not in interactive mode
            // (for example, if we're running as a service)
            if (!Environment.UserInteractive)
            {
                throw new InvalidOperationException(SR.Get(SRID.CantShowModalOnNonInteractive));
            }

            // Call GetActiveWindow to retrieve the window handle to the active window
            // attached to the calling thread's message queue.  We'll set the owner of
            // the common dialog to this handle.
            IntPtr hwndOwner = UnsafeNativeMethods.GetActiveWindow();

            if (hwndOwner == IntPtr.Zero)
            {
                // No active window, so we'll use the parking window as the owner, 
                // if its available.
                if (Application.Current != null)
                {
                    hwndOwner = Application.Current.ParkingHwnd;
                }
            }

            HwndWrapper tempParentHwnd = null;
            try
            {
                // No active window and application wasn't available or didn't have 
                // a ParkingHwnd, we create a hidden parent window for the dialog to 
                // prevent breaking UIAutomation.
                if (hwndOwner == IntPtr.Zero)
                {
                    tempParentHwnd = new HwndWrapper(0, 0, 0, 0, 0, 0, 0, "", IntPtr.Zero, null);
                    hwndOwner = tempParentHwnd.Handle;
                }

                // Store the handle of the owner window inside our class so we can use it
                // to center the dialog later.
                _hwndOwnerWindow = hwndOwner;

                // Signal that this thread is going to go modal.
                try
                {
                    ComponentDispatcher.CriticalPushModal();

                    return RunDialog(hwndOwner);
                }
                finally
                {
                    ComponentDispatcher.CriticalPopModal();
                }
            }
            finally
            {
                if (tempParentHwnd != null)
                {
                    tempParentHwnd.Dispose();
                }
            }
        }

        /// <summary>
        ///  Runs a common dialog box, with the owner as the given Window
        /// </summary>
        /// <Remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </Remarks>
        public Nullable<bool> ShowDialog(Window owner)
        {
            CheckPermissionsToShowDialog();

            // If a valid window wasn't passed into this function, we'll
            // call ShowDialog() to use the active window instead of 
            // throwing an exception
            if (owner == null)
            {
                return ShowDialog();
            }

            // Don't allow file dialogs to be shown if not in interactive mode
            // (for example, if we're running as a service)
            if (!Environment.UserInteractive)
            {
                throw new InvalidOperationException(SR.Get(SRID.CantShowModalOnNonInteractive));
            }

            // Get the handle of the owner window using WindowInteropHelper.
            IntPtr hwndOwner = (new WindowInteropHelper(owner)).CriticalHandle;

            // Just in case, check if the window's handle is zero.
            if (hwndOwner == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            // Store the handle of the owner window inside our class so we can use it
            // to center the dialog later.
            _hwndOwnerWindow = hwndOwner;

            // Signal that this thread is going to go modal.
            try
            {
                ComponentDispatcher.CriticalPushModal();

                return RunDialog(hwndOwner);
            }
            finally
            {
                ComponentDispatcher.CriticalPopModal();
            }
        }

        #endregion Public Methods

        //---------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------
        #region Public Properties

        /// <summary>
        ///  Provides the ability to attach an arbitrary object to the dialog.  
        /// </summary>
        public object Tag
        {
            get
            {
                return _userData;
            }
            set
            {
                _userData = value;
            }
        }

        #endregion Public Properties

        //---------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------
        //#region Public Events
        //#endregion Public Events

        //---------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------
        #region Protected Methods


        /// <summary>
        ///  Defines the common dialog box hook procedure that is overridden to 
        ///  add specific functionality to a common dialog box.
        /// </summary>
        protected virtual IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            // WM_INITDIALOG
            // The WM_INITDIALOG message is sent to the dialog box procedure immediately 
            // before a dialog box is displayed. Dialog box procedures typically use 
            // this message to initialize controls and carry out any other initialization 
            // tasks that affect the appearance of the dialog box. 
            //
            // We handle WM_INITDIALOG to move the dialog to the center of the screen
            if ((WindowMessage)msg == WindowMessage.WM_INITDIALOG)
            {
                // call MoveToScreenCenter to reposition the dialog based on the location
                // of the owner window.
                MoveToScreenCenter(new HandleRef(this, hwnd));

                // WM_INITDIALOG expects TRUE to be returned to properly set focus.
                return new IntPtr(1);
            }
            return IntPtr.Zero;
        }

        /// <summary>
        ///  When overridden in a derived class, displays a particular type of common dialog box.
        /// </summary>
        protected abstract bool RunDialog(IntPtr hwndOwner);

        /// <summary>
        ///  Demands permissions appropriate to the dialog to be shown.
        /// </summary>
        protected virtual void CheckPermissionsToShowDialog()
        {
            // Verify we're on the right thread.  
            // This mitigates multi-threaded attacks without having to make the file dialogs thread-safe.
            if (_thread != Thread.CurrentThread)
            {
                throw new InvalidOperationException(SR.Get(SRID.CantShowOnDifferentThread));
            }

        }

        #endregion Protected Methods

        //---------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------
        #region Internal Methods

        /// <summary>
        ///  Centers the given window on the screen. This method is used by HookProc
        ///  to center the dialog on the screen before it is shown.  We can't mark it
        ///  private because we need to call it from our derived classes like
        ///  FileDialog.
        /// </summary>
        internal void MoveToScreenCenter(HandleRef hWnd)
        {
            // Create an IntPtr to store a handle to the monitor.
            IntPtr hMonitor = IntPtr.Zero;

            // Get the monitor to use based on the location of the parent window
            if (_hwndOwnerWindow != IntPtr.Zero)
            {
                // we have a owner hwnd; center on the screen on 
                // which our owner hwnd is.
                // We use MONITOR_DEFAULTTONEAREST to get the monitor
                // nearest to the window if the window doesn't intersect
                // any display monitor.
                hMonitor = SafeNativeMethods.MonitorFromWindow(
                                    new HandleRef(this, _hwndOwnerWindow),                        // window to find monitor location for
                                    NativeMethods.MONITOR_DEFAULTTONEAREST); // get the monitor nearest to the window


                // Only move the window if we got a valid monitor... otherwise let Windows
                // position the dialog.
                if (hMonitor != IntPtr.Zero)
                {
                    // Now, create another RECT and fill it with the bounds of the parent window.
                    NativeMethods.RECT dialogRect = new NativeMethods.RECT();
                    SafeNativeMethods.GetWindowRect(hWnd, ref dialogRect);

                    Size dialogSize = new Size((dialogRect.right - dialogRect.left),  /*width*/
                                               (dialogRect.bottom - dialogRect.top)); /*height*/

                    // create variables that will receive the new position of the dialog
                    double x = 0;
                    double y = 0;

                    // Call into a static function in System.Windows.Window to calculate
                    // the actual new position
                    Window.CalculateCenterScreenPosition(hMonitor,
                                                         dialogSize,
                                                         ref x,
                                                         ref y);

                    // Call SetWindowPos to actually move the window.
                    UnsafeNativeMethods.SetWindowPos(hWnd,                          // handle to the window to move
                                                     NativeMethods.NullHandleRef,   // window to precede this one in zorder
                                                     (int)Math.Round(x),
                                                     (int)Math.Round(y),            // new X and Y positions
                                                     0, 0,                          // new width and height, if applicable
                                                                                    // Flags:  
                                                                                    //    SWP_NOSIZE: Retains current size
                                                                                    //    SWP_NOZORDER:  retains current zorder
                                                                                    //    SWP_NOACTIVATE:  does not activate the window
                                                     NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
                }
            }
        }

        #endregion Internal Methods

        //---------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------
        #region Internal Properties

        #endregion Internal Properties

        //---------------------------------------------------
        //
        // Internal Events
        //
        //---------------------------------------------------
        //#region Internal Events
        //#endregion Internal Events

        //---------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------
        //#region Private Methods
        //#endregion Private Methods

        //---------------------------------------------------
        //
        // Protected Properties
        //
        //---------------------------------------------------
        //#region Protected Properties
        //#endregion Protected Properties

        //---------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------
        #region Private Fields

        // Private variable used to store data for the Tag property
        private object _userData;

        private Thread _thread = Thread.CurrentThread;

        /// <summary>
        ///  The owner hwnd passed into the dialog is stored as a private
        ///  member so that the dialog can be properly centered onscreen.
        ///  It is exposed through the OwnerWindowHandle property.
        /// </summary>
        private IntPtr _hwndOwnerWindow;

        #endregion Private Fields
    }
}

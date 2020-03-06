// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Interop;
using MS.Win32;

using SWF = System.Windows.Forms;
using SW = System.Windows;
using SWM = System.Windows.Media;
using SWI = System.Windows.Input;

namespace System.Windows.Forms.Integration
{
    internal static class ApplicationInterop
    {
        [ThreadStatic]
        private static WindowsFormsHostList _threadWindowsFormsHostList;
        /// <summary>
        /// Gets a list of all of the WindowsFormsHosts which were created on the current thread.
        /// </summary>
        internal static WindowsFormsHostList ThreadWindowsFormsHostList
        {
            get
            {
                //No synchronization required (the field is ThreadStatic)
                if (null == _threadWindowsFormsHostList)
                {
                    _threadWindowsFormsHostList = new WindowsFormsHostList();
                }
                return _threadWindowsFormsHostList;
            }
        }
        [ThreadStatic]
        private static bool _messageFilterInstalledOnThread;

        /// <summary>
        ///     Enables a System.Windows.Window to receive necessary keyboard
        ///     messages when it is opened modelessly from a Windows.Forms.Application.
        /// </summary>
        /// <param name="window">The System.Windows.Window which will be opened modelessly.</param>
        public static void EnableModelessKeyboardInterop(SW.Window window)
        {
            //Create and add IMessageFilter
            ModelessWindowFilter filter = new ModelessWindowFilter(window);
            WindowFilterList.FilterList.Add(filter);
            SWF.Application.AddMessageFilter(filter);

            //Hook window Close event to remove IMessageFilter
            window.Closed += new EventHandler(WindowFilterList.ModelessWindowClosed);
        }

        public static void EnableWindowsFormsInterop()
        {
            if (!_messageFilterInstalledOnThread)
            {
                SW.Interop.ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(ThreadMessageFilter);
                _messageFilterInstalledOnThread = true;
            }
        }

        // CSS added for keyboard interop
        //
        // TODO: We should allow overriding (for advanced keyboarding changes).
        /// <summary>
        ///     
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="outHandled"></param>
        internal static void ThreadMessageFilter(ref MSG msg, ref bool outHandled)
        {
            // Don't do anything if already handled
            if (outHandled)
            {
                return;
            }

            // If WPF is in "menu mode" then WinForms will generally not
            // receive input because WPF will move keyboard focus into the
            // menu window.  However, in 4.0 WPF introduced "exclusive" menu
            // mode to enable certain menu scenarios in VS.  This mode does
            // not move focus, so by hooking the thread pre-process here, we
            // would accidentally route messages to the control even when in
            // menu mode.  So we just bail out here, but for compat reasons
            // only if the app targets 4.5+ (DevDiv #650335).
            if (CoreCompatibilityPreferences.TargetsAtLeast_Desktop_V4_5 &&
                System.Windows.Input.InputManager.Current.IsInMenuMode)
            {
                return;
            }

            Message m = Convert.ToSystemWindowsFormsMessage(msg);

            // Process Winforms MessageFilters
            if (Application.FilterMessage(ref m))
            {
                // Set the return value correctly.
                outHandled = true;
                return;
            }

            bool handled = false;

            SWF.Control control = SWF.Control.FromChildHandle(m.HWnd);
            if (control != null)
            {
                //CSS The WM_SYSCHAR special case is a workaround for a bug VSWhidbey 575729, which
                //makes IsInputChar not get called with WM_SYSCHAR messages.
                if (m.Msg == NativeMethods.WM_SYSCHAR)
                {
                    handled = control.PreProcessMessage(ref m);
                }
                else
                {
                    SWF.PreProcessControlState processedState = control.PreProcessControlMessage(ref m);

                    if (processedState == SWF.PreProcessControlState.MessageNeeded)
                    {
                        if (m.Msg == NativeMethods.WM_CHAR)
                        {
                            // Since we are going to eat the WM_CHAR, WPF won't
                            // have the chance to process this message.  One
                            // potential problem is that this could leave WPF
                            // stuck in a dead-char composition, so we force
                            // any such composition to complete (but not raise
                            // any TextInput events).
                            SWI.InputManager.Current.PrimaryKeyboardDevice.TextCompositionManager.CompleteDeadCharComposition();
                        }

                        // Control didn't process message but does want the message.
                        UnsafeNativeMethods.TranslateMessage(ref msg);
                        UnsafeNativeMethods.DispatchMessage(ref msg);
                        handled = true;
                    }
                    else if (processedState == SWF.PreProcessControlState.MessageProcessed)
                    {
                        // Control processed the mesage
                        handled = true;
                    }
                    else
                    {
                        // Control doesn't need message
                        Debug.Assert(processedState == SWF.PreProcessControlState.MessageNotNeeded, "invalid state");
                        handled = false;
                    }
                }
            }
            else if (msg.message != 0xc2a3) /* ControlFromHWnd == null */
            {
                // We are only letting the hosted control do preprocess message when it
                // isn't active. All other WF controls will get PreProcessMessage at 
                // normal time (when focused).
                foreach (WindowsFormsHost wfh in ThreadWindowsFormsHostList.ActiveWindowList())
                {
                    if (wfh.HostContainerInternal.PreProcessMessage(ref m, false))
                    {
                        handled = true;
                        break;
                    }
                }
            }

            // Set the return value correctly.
            outHandled = handled;
            return;
        }

    }


    /// <summary>
    ///     This singleton is used to enable Avalon Modeless Windows when using
    ///     the WinForms application to handle events. It keeps track of all the Avalon windows.
    ///     See ElementHost.EnableModelessKeyboardInterop for more info.
    ///
    ///     Since the filter information cannot be stored in the Avalon window
    ///     class itself, keep a list of all the Avalon windows and their filters.
    ///     When an avalon window is closed, remove it from the list
    /// </summary>
    internal class WindowFilterList : WeakReferenceList<ModelessWindowFilter>
    {
        //Singleton instance of the list
        private static WindowFilterList _filterList = new WindowFilterList();
        public static WindowFilterList FilterList
        {
            get
            {
                return _filterList;
            }
        }

        /// <summary>
        ///     Seaches the filter list for an entry pointing to the current
        ///     windows.
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        static ModelessWindowFilter FindFilter(SW.Window window)
        {
            ModelessWindowFilter windowFilter = null;

            if (window == null)
            {
                return null;
            }

            foreach (ModelessWindowFilter filter in _filterList.SnapshotListOfTargets)
            {
                if (filter.Window == window)
                {
                    windowFilter = filter;
                    break;
                }
            }
            Debug.Assert(windowFilter != null);
            return windowFilter;
        }

        /// <summary>
        ///     This callback is added to the avalon window so that its filter is removed
        ///     when the window is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void ModelessWindowClosed(object sender, EventArgs e)
        {
            ModelessWindowFilter windowFilter = WindowFilterList.FindFilter(sender as SW.Window);
            if (windowFilter != null)
            {
                SWF.Application.RemoveMessageFilter(windowFilter);
                WindowFilterList.FilterList.Remove(windowFilter);
            }
        }
    }

    /// <summary>
    ///     This message filter forwards messages to registered Avalon windows.
    ///     Use ElementHost.EnableModelessKeyboardInterop to setup
    /// </summary>
    internal class ModelessWindowFilter : SWF.IMessageFilter
    {
        private System.Windows.Window _window;
        public SW.Window Window
        {
            get
            {
                return _window;
            }
        }

        public ModelessWindowFilter(System.Windows.Window window)
        {
            _window = window;
        }

        //Need a recursion guard for PreFilterMessage: the same message can come back to us via the 
        //ComponentDispatcher.
        bool _inPreFilterMessage;
        public bool PreFilterMessage(ref SWF.Message msg)
        {
            if (_window == null || !_window.IsActive)
            {
                return false;
            }

            switch (msg.Msg)
            {
                case NativeMethods.WM_KEYDOWN:          //0x100
                case NativeMethods.WM_KEYUP:            //0x101
                case NativeMethods.WM_CHAR:             //0x102
                case NativeMethods.WM_DEADCHAR:         //0x103
                case NativeMethods.WM_SYSKEYDOWN:       //0x104
                case NativeMethods.WM_SYSKEYUP:         //0x105
                case NativeMethods.WM_SYSCHAR:          //0x106
                case NativeMethods.WM_SYSDEADCHAR:      //0x107
                    if (!_inPreFilterMessage)
                    {
                        _inPreFilterMessage = true;
                        try
                        {
                            SW.Interop.MSG msg2 = Convert.ToSystemWindowsInteropMSG(msg);
                            bool fReturn = SW.Interop.ComponentDispatcher.RaiseThreadMessage(ref msg2);
                            return fReturn;
                        }
                        finally
                        {
                            _inPreFilterMessage = false;
                        }
                    }
                    return false;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    ///     This class make a strongly typed weak reference collection. Its enumerator
    ///     only returns references to live objects.
    ///     By not keeping a reference count on the objects in this list, they can be 
    ///     garbage collected normally.
    /// </summary>
    internal class WeakReferenceList<T> where T : class
    {
        List<WeakReference> _internalList;
        object _syncRoot = new object();

        public WeakReferenceList()
            : base()
        {
            _internalList = new List<WeakReference>();
        }

        /// <summary>
        ///     This prunes object reference that no longer point to valid objects
        ///     from the list. This is called often in the current implementation,
        ///     but can be phased back if there are perf concerns.
        /// </summary>
        protected void RemoveDeadReferencesFromList()
        {
            for (int i = _internalList.Count - 1; i >= 0; i--)
            {
                if (!_internalList[i].IsAlive)
                {
                    _internalList.RemoveAt(i);
                }
            }
        }

        public List<T> SnapshotListOfTargets
        {
            get
            {
                List<T> targets = new List<T>();
                lock (_syncRoot)
                {
                    RemoveDeadReferencesFromList();
                    foreach (WeakReference obj in _internalList)
                    {
                        //tempValue will be null if it's not alive
                        T tempValue = obj.Target as T;
                        if (tempValue != null)
                        {
                            targets.Add(tempValue);
                        }
                    }
                }
                return targets;
            }
        }

        public void Add(T obj)
        {
            lock (_syncRoot)
            {
                RemoveDeadReferencesFromList();
                WeakReference newItem = new WeakReference(obj, false);
                _internalList.Add(newItem);
            }
        }

        internal int IndexOf(T obj)
        {
            {
                RemoveDeadReferencesFromList();
                for (int i = 0; i < _internalList.Count; i++)
                {
                    if (_internalList[i].IsAlive)
                    {
                        if (_internalList[i].Target as T == obj)
                        {
                            return i;
                        }
                    }
                }
                return -1;
            }
        }

        public bool Remove(T obj)
        {
            lock (_syncRoot)
            {
                int index = IndexOf(obj);
                if (index >= 0)
                {
                    _internalList.RemoveAt(index);
                    return true;
                }
                return false;
            }
        }
    }

    internal class WindowsFormsHostList : WeakReferenceList<WindowsFormsHost>
    {
        public IEnumerable<WindowsFormsHost> ActiveWindowList()
        {
            SW.Window rootWindow = null;
            foreach (WindowsFormsHost wfh in this.SnapshotListOfTargets)
            {
                rootWindow = FindRootVisual(wfh) as SW.Window;
                if (rootWindow != null)
                {
                    if (rootWindow.IsActive)
                    {
                        yield return wfh;
                    }
                }
            }
        }

        private static SWM.Visual FindRootVisual(SWM.Visual x)
        {
            return (PresentationSource.FromVisual(x) != null) ? (PresentationSource.FromVisual(x)).RootVisual : null;
        }
    }
}

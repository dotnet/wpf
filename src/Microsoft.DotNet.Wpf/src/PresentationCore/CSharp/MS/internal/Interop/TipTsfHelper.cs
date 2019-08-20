// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Security;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Internal.WindowsRuntime.Windows.UI.ViewManagement;

namespace MS.Internal.Interop
{
    /// <summary>
    /// DevDiv:1193138
    /// 
    /// This class provides functionality to show/hide the touch keyboard.
    /// This is necessary in Win8+ as touch keyboard invocation was changed 
    /// to use WM_POINTER message tracking.  Since Wisp does not allow these
    /// to propogate to WPF, we have to forcibly show the keyboard.
    /// 
    /// As of creation of this class, we never call Hide, only Show.  This is
    /// because the touch keyboard is smart about when to show or hide.  As long
    /// as we attempt to show it on each focus, it will determine whether this is
    /// a valid scenario or whether it should close.
    /// 
    /// If we are run under a platform that does not support the proper calls into
    /// the InputPane WinRT API we will catch an exception on the first call to
    /// TryShow/Hide and then cache the fact that we no longer need to make these calls.
    /// </summary>
    internal static class TipTsfHelper
    {
        /// <summary>
        /// Cache if the API call is supported on the current platform.
        /// </summary>
        private static bool s_PlatformSupported = true;

        /// <summary>
        /// Cache any in progress operation in case we get multiple calls.
        /// </summary>
        [ThreadStatic]
        private static DispatcherOperation s_KbOperation = null;

        /// <summary>
        /// If DispatcherProcessing is disabled, this will BeginInvoke the appropriate KB operation
        /// for later processing.  It also will cancel any pending operations so only one op can be in
        /// flight at a time.
        /// </summary>
        /// <param name="kbCall">The kb function to call.</param>
        /// <param name="focusedObject">The object being focused</param>
        /// <returns>True if an operation has been scheduled, false otherwise</returns>
        private static bool CheckAndDispatchKbOperation(Action<DependencyObject> kbCall, DependencyObject focusedObject)
        {
            // Abort the current operation if we reach another call and it is
            // still pending.
            if (s_KbOperation?.Status == DispatcherOperationStatus.Pending)
            {
                s_KbOperation.Abort();
            }

            s_KbOperation = null;
            
            // Don't call any KB operations under disabled processing as a COM wait could
            // cause re-entrancy and an InvalidOperationException.
            if (Dispatcher.CurrentDispatcher._disableProcessingCount > 0)
            {
                // Retry when processing resumes
                s_KbOperation =
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input,
                    new Action(() =>
                    {
                        // Since this action is happening sometime later, the focus 
                        // may have already changed.
                        if (Keyboard.FocusedElement == focusedObject)
                        {
                            kbCall(focusedObject);
                        }
                    }));

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to show the touch keyboard.
        /// We rely on the Windows API to determine if showing the keyboard aligns with the 
        /// current state (no physical KB, touch enabled, focused edit control).
        /// </summary>
        internal static void Show(DependencyObject focusedObject)
        {
            // We need to only show if applicable to this focused object
            // so guard the calls to TryShow here.           
            // If the touch stack is disabled or the WM_POINTER touch stack 
            // is enabled, we get touch KB support for free.  So don't 
            // attempt any calls into InputPane for these scenarios. 
            // Don't show if implicit invocation is turned off.
            if (s_PlatformSupported
                && !CoreAppContextSwitches.DisableImplicitTouchKeyboardInvocation
                && StylusLogic.IsStylusAndTouchSupportEnabled
                && !StylusLogic.IsPointerStackEnabled
                && !CheckAndDispatchKbOperation(Show, focusedObject)
                && ShouldShow(focusedObject))
            {
                InputPane ip;

                try
                {
                    using (ip = InputPane.GetForWindow(GetHwndSource(focusedObject)))
                    {
                        ip?.TryShow();
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    s_PlatformSupported = false;
                }
            }
        }

        /// <summary>
        /// Attempts to hide the touch keyboard.
        /// </summary>
        internal static void Hide(DependencyObject focusedObject)
        {
            // If the touch stack is disabled or the WM_POINTER touch stack 
            // is enabled, we get touch KB support for free.  So don't 
            // attempt any calls into InputPane for these scenarios. 
            if (s_PlatformSupported
                && StylusLogic.IsStylusAndTouchSupportEnabled
                && !StylusLogic.IsPointerStackEnabled
                && !CheckAndDispatchKbOperation(Hide, focusedObject))
            {
                InputPane ip;

                try
                {
                    using (ip = InputPane.GetForWindow(GetHwndSource(focusedObject)))
                    {
                        ip?.TryHide();
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    s_PlatformSupported = false;
                }
            }
        }

        /// <summary>
        /// The KB should only show when we have an object that implements the
        /// UIAutomation Text pattern.  Therefore, we should test for this 
        /// pattern on any focused object that we get.
        /// </summary>
        /// <param name="focusedObject">The object being focused</param>
        /// <returns>True if the touch KB should show, false otherwise.</returns>
        private static bool ShouldShow(DependencyObject focusedObject)
        {
            UIElement uiElement;
            UIElement3D uiElement3D;
            ContentElement contentElement;
            AutomationPeer peer = null;

            if ((uiElement = focusedObject as UIElement) != null)
            {
                peer = uiElement.GetAutomationPeer();
            }
            else if ((uiElement3D = focusedObject as UIElement3D) != null)
            {
                peer = uiElement3D.GetAutomationPeer();
            }
            else if ((contentElement = focusedObject as ContentElement) != null)
            {
                peer = contentElement.GetAutomationPeer();
            }

            return peer?.GetPattern(PatternInterface.Text) != null;
        }

        /// <summary>
        /// Returns an HwndSource from the Visual
        /// </summary>
        /// <param name="focusedVisual">The visual to get the HwndSource for</param>
        /// <returns>The HwndSource associated with the Visual</returns>
        private static HwndSource GetHwndSource(DependencyObject focusedObject)
        {
            return PresentationSource.CriticalFromVisual(focusedObject) as HwndSource;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:  
//      WebBrowserSite is a sub-class of ActiveXSite. 
//      Used to implement IDocHostUIHandler. 
//
//      Copied from WebBrowser.cs in winforms
//

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using MS.Win32;
using System.Security;
using MS.Internal.Interop;
using MS.Internal.PresentationFramework;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading;

using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace MS.Internal.Controls
{
    //
    // WebBrowserSite class:
    //
    /// 
    /// <summary>
    /// Provides a default WebBrowserSite implementation for use in the CreateWebBrowserSite
    /// method in the WebBrowser class. 
    /// </summary> 
    /// <remarks>
    /// THREADING ISSUE: When WebBrowser.IsWebOCHostedInBrowserProcess, calls on the interfaces implemented here
    ///   (and on ActiveXSite) arrive on RPC worker threads. This is because CLR objects don't like to stick to 
    ///   STA threads. Fortunately, most of the current implementation methods are okay to be called on any thread.
    ///   And if not, switching to the WebBrowser object's thread via the Dispatcher is usually possible & safe.
    ///   In a few scenarios, when we need to call a WebOC method from one of these callback interfaces, we get
    ///   RPC_E_CANTCALLOUT_ININPUTSYNCCALL, which happens because the CLR actually tries to switch to the right
    ///   thread to make the COM call, but that thread is already blocked on an outgoing call (to the WebOC). 
    ///   One example is IOleInPlaceSite.OnInPlaceActivate().
    ///   These failures are silent and safely ignorable for now. If this threading issue becomes more troubling,
    ///   a solution like ActiveXHelper.CreateIDispatchSTAForwarder() is possible.
    /// </remarks>
    internal class WebBrowserSite : ActiveXSite,
        UnsafeNativeMethods.IDocHostUIHandler,
        UnsafeNativeMethods.IOleControlSite // partial override
    {
        /// 
        /// <summary>
        ///     WebBrowser implementation of ActiveXSite. Used to override GetHostInfo. 
        ///     and "turn on" our redirect notifications. 
        /// </summary> 
        internal WebBrowserSite(WebBrowser host) : base(host)
        {
        }


        #region IDocHostUIHandler Implementation

        int UnsafeNativeMethods.IDocHostUIHandler.ShowContextMenu(int dwID, NativeMethods.POINT pt, object pcmdtReserved, object pdispReserved)
        {
            //
            // Returning S_FALSE will allow the native control to do default processing,
            // i.e., execute the shortcut key. Returning S_OK will cancel the context menu
            //

            return NativeMethods.S_FALSE;
        }

        int UnsafeNativeMethods.IDocHostUIHandler.GetHostInfo(NativeMethods.DOCHOSTUIINFO info)
        {
            WebBrowser wb = (WebBrowser)Host;

            info.dwDoubleClick = (int)NativeMethods.DOCHOSTUIDBLCLICK.DEFAULT;

            //
            // These are the current flags shdocvw uses. Assumed we want the same. 
            // 

            info.dwFlags = (int)(NativeMethods.DOCHOSTUIFLAG.DISABLE_HELP_MENU |
                           NativeMethods.DOCHOSTUIFLAG.DISABLE_SCRIPT_INACTIVE |
                           NativeMethods.DOCHOSTUIFLAG.ENABLE_INPLACE_NAVIGATION |
                           NativeMethods.DOCHOSTUIFLAG.IME_ENABLE_RECONVERSION |
                           NativeMethods.DOCHOSTUIFLAG.THEME |
                           NativeMethods.DOCHOSTUIFLAG.ENABLE_FORMS_AUTOCOMPLETE |
                           NativeMethods.DOCHOSTUIFLAG.DISABLE_UNTRUSTEDPROTOCOL |
                           NativeMethods.DOCHOSTUIFLAG.LOCAL_MACHINE_ACCESS_CHECK |
                           NativeMethods.DOCHOSTUIFLAG.ENABLE_REDIRECT_NOTIFICATION |
                           NativeMethods.DOCHOSTUIFLAG.NO3DOUTERBORDER);

            return NativeMethods.S_OK;
        }


        int UnsafeNativeMethods.IDocHostUIHandler.EnableModeless(bool fEnable)
        {
            return NativeMethods.E_NOTIMPL;
        }


        int UnsafeNativeMethods.IDocHostUIHandler.ShowUI(int dwID, UnsafeNativeMethods.IOleInPlaceActiveObject activeObject,
                NativeMethods.IOleCommandTarget commandTarget, UnsafeNativeMethods.IOleInPlaceFrame frame,
                UnsafeNativeMethods.IOleInPlaceUIWindow doc)
        {
            return NativeMethods.E_NOTIMPL;
        }


        int UnsafeNativeMethods.IDocHostUIHandler.HideUI()
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IDocHostUIHandler.UpdateUI()
        {
            return NativeMethods.E_NOTIMPL;
        }
        int UnsafeNativeMethods.IDocHostUIHandler.OnDocWindowActivate(bool fActivate)
        {
            return NativeMethods.E_NOTIMPL;
        }
        int UnsafeNativeMethods.IDocHostUIHandler.OnFrameWindowActivate(bool fActivate)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IDocHostUIHandler.ResizeBorder(NativeMethods.COMRECT rect, UnsafeNativeMethods.IOleInPlaceUIWindow doc, bool fFrameWindow)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IDocHostUIHandler.GetOptionKeyPath(string[] pbstrKey, int dw)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IDocHostUIHandler.GetDropTarget(UnsafeNativeMethods.IOleDropTarget pDropTarget, out UnsafeNativeMethods.IOleDropTarget ppDropTarget)
        {
            //
            // Set to null no matter what we return, to prevent the marshaller
            // from going crazy if the pointer points to random stuff.
            ppDropTarget = null;
            return NativeMethods.E_NOTIMPL;
        }

        /// <summary>
        ///    Critical: This code access critical member Host.
        ///    TreatAsSafe: The object returned is sandboxed in the managed environment.
        /// </summary>
        int UnsafeNativeMethods.IDocHostUIHandler.GetExternal(out object ppDispatch)
        {
            WebBrowser wb = (WebBrowser)Host;
            ppDispatch = wb.HostingAdaptor.ObjectForScripting;
            return NativeMethods.S_OK;
        }

        /// <summary>
        /// Called by the WebOC whenever its IOleInPlaceActiveObject::TranslateAccelerator() is called.
        /// See also the IOleControlSite.TranslateAccelerator() implementation here.
        /// </summary>
        int UnsafeNativeMethods.IDocHostUIHandler.TranslateAccelerator(ref System.Windows.Interop.MSG msg, ref Guid group, int nCmdID)
        {
            //
            // Returning S_FALSE will allow the native control to do default processing,
            // i.e., execute the shortcut key. Returning S_OK will cancel the shortcut key.

            /*              WebBrowser wb = (WebBrowser)this.Host;
            
            if (!wb.WebBrowserShortcutsEnabled)
            {
                int keyCode = (int)msg.wParam | (int)Control.ModifierKeys;

                if (msg.message != WindowMessage.WM_CHAR
                        && Enum.IsDefined(typeof(Shortcut), (Shortcut)keyCode)) {
                    return NativeMethods.S_OK;
                }
                return NativeMethods.S_FALSE;
            }
            */

            return NativeMethods.S_FALSE;
        }

        int UnsafeNativeMethods.IDocHostUIHandler.TranslateUrl(int dwTranslate, string strUrlIn, out string pstrUrlOut)
        {
            //
            // Set to null no matter what we return, to prevent the marshaller
            // from going crazy if the pointer points to random stuff.
            pstrUrlOut = null;
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IDocHostUIHandler.FilterDataObject(IComDataObject pDO, out IComDataObject ppDORet)
        {
            //
            // Set to null no matter what we return, to prevent the marshaller
            // from going crazy if the pointer points to random stuff.
            ppDORet = null;
            return NativeMethods.E_NOTIMPL;
        }

        #endregion

        /// <remarks> See overview of keyboard input handling in WebBrowser.cs. </remarks>
        int UnsafeNativeMethods.IOleControlSite.TranslateAccelerator(ref MSG msg, int grfModifiers)
        {
            // Handle tabbing out of the WebOC
            if ((WindowMessage)msg.message == WindowMessage.WM_KEYDOWN && (int)msg.wParam == NativeMethods.VK_TAB)
            {
                FocusNavigationDirection direction =
                    (grfModifiers & 1/*KEYMOD_SHIFT*/) != 0 ?
                    FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
                // For the WebOCHostedInBrowserProcess case, we need to switch to the right thread.
                Host.Dispatcher.Invoke(
                    DispatcherPriority.Send, new SendOrPostCallback(MoveFocusCallback), direction);
                return NativeMethods.S_OK;
            }
            return NativeMethods.S_FALSE;
        }

        private void MoveFocusCallback(object direction)
        {
            Host.MoveFocus(new TraversalRequest((FocusNavigationDirection)direction));
        }
    };
}

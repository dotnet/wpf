// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Interop;
using MS.Internal.Controls;
using MS.Internal;
using MS.Internal.PresentationFramework;
using MS.Win32;
using System.Security;
using System.Security.Permissions;
using System.Windows.Controls;

namespace MS.Internal.Controls
{
    ///
    /// This class implements the necessary interfaces required for an ActiveX site.
    ///
    /// <remarks>
    /// THREADING ISSUE: See comment on WebBrowserSite.
    /// </remarks>
    /// <SecurityNote>
    /// WebOCHostedInBrowserProcess - defense in depth: 
    ///   These interface implementations are exposed across a security boundary. We must not allow a 
    ///   compromised low-integrity-level browser process to gain elevation of privilege via our process or
    ///   tamper with its state. (Attacking the WebOC via this interface is not interesting, because the WebOC
    ///   is directly accessible in the browser process.) Each interface implementation method must be 
    ///   carefully reviewed to ensure that it cannot be abused by disclosing protected resources or by passing
    ///   malicious data to it.
    /// </SecurityNote>
    internal class ActiveXSite :
        UnsafeNativeMethods.IOleControlSite,
        UnsafeNativeMethods.IOleClientSite,
        UnsafeNativeMethods.IOleInPlaceSite,
        UnsafeNativeMethods.IPropertyNotifySink
    {
        #region Constructor

        //
        // The constructor takes an ActiveXHost as a parameter, so unfortunately,
        // this cannot be used as a standalone site. It has to be used in conjunction
        // with ActiveXHost. Perhaps we can change it in future.
        //

        ///<SecurityNote> 
        ///     Critical - stores ActiveXHost - critical data. 
        ///</SecurityNote> 
        [SecurityCritical]
        internal ActiveXSite(ActiveXHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException("host");
            }
            _host = host;
        }

        #endregion Constructor

        //
        // IOleControlSite methods:
        //
        #region IOleControlSite
        int UnsafeNativeMethods.IOleControlSite.OnControlInfoChanged()
        {
            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IOleControlSite.LockInPlaceActive(int fLock)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleControlSite.GetExtendedControl(out object ppDisp)
        {
            ppDisp = null;
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleControlSite.TransformCoords(NativeMethods.POINT pPtlHimetric, NativeMethods.POINTF pPtfContainer, int dwFlags)
        {
            if ((dwFlags & NativeMethods.XFORMCOORDS_HIMETRICTOCONTAINER) != 0)
            {
                if ((dwFlags & NativeMethods.XFORMCOORDS_SIZE) != 0)
                {
                    pPtfContainer.x = (float)ActiveXHelper.HM2Pix(pPtlHimetric.x, ActiveXHelper.LogPixelsX);
                    pPtfContainer.y = (float)ActiveXHelper.HM2Pix(pPtlHimetric.y, ActiveXHelper.LogPixelsY);
                }
                else if ((dwFlags & NativeMethods.XFORMCOORDS_POSITION) != 0)
                {
                    pPtfContainer.x = (float)ActiveXHelper.HM2Pix(pPtlHimetric.x, ActiveXHelper.LogPixelsX);
                    pPtfContainer.y = (float)ActiveXHelper.HM2Pix(pPtlHimetric.y, ActiveXHelper.LogPixelsY);
                }
                else
                {
                    return NativeMethods.E_INVALIDARG;
                }
            }
            else if ((dwFlags & NativeMethods.XFORMCOORDS_CONTAINERTOHIMETRIC) != 0)
            {
                if ((dwFlags & NativeMethods.XFORMCOORDS_SIZE) != 0)
                {
                    pPtlHimetric.x = ActiveXHelper.Pix2HM((int)pPtfContainer.x, ActiveXHelper.LogPixelsX);
                    pPtlHimetric.y = ActiveXHelper.Pix2HM((int)pPtfContainer.y, ActiveXHelper.LogPixelsY);
                }
                else if ((dwFlags & NativeMethods.XFORMCOORDS_POSITION) != 0)
                {
                    pPtlHimetric.x = ActiveXHelper.Pix2HM((int)pPtfContainer.x, ActiveXHelper.LogPixelsX);
                    pPtlHimetric.y = ActiveXHelper.Pix2HM((int)pPtfContainer.y, ActiveXHelper.LogPixelsY);
                }
                else
                {
                    return NativeMethods.E_INVALIDARG;
                }
            }
            else
            {
                return NativeMethods.E_INVALIDARG;
            }

            return NativeMethods.S_OK;
        }

        /// <internalonly/>
        int UnsafeNativeMethods.IOleControlSite.TranslateAccelerator(ref MSG pMsg, int grfModifiers)
        {
            /*
            Debug.Assert(!this.Host.GetAxHostState(AxHostHelper.siteProcessedInputKey), "Re-entering UnsafeNativeMethods.IOleControlSite.TranslateAccelerator!!!");
            this.Host.SetAxHostState(AxHostHelper.siteProcessedInputKey, true);

            Message msg = new Message();
            msg.Msg = pMsg.message;
            msg.WParam = pMsg.wParam;
            msg.LParam = pMsg.lParam;
            msg.HWnd = pMsg.hwnd;
            
            try {
                bool f = ((Control)this.Host).PreProcessMessage(ref msg);
                return f ? NativeMethods.S_OK : NativeMethods.S_FALSE;
            }
            finally {
                this.Host.SetAxHostState(AxHostHelper.siteProcessedInputKey, false);
            }
            */

            // This is called by IOleInPlaceActiveObject::TranslateAccelerator. 
            // returning S_FALSE means we don't process the messages. Let the webbrowser control handle it.
            return NativeMethods.S_FALSE;
        }

        int UnsafeNativeMethods.IOleControlSite.OnFocus(int fGotFocus)
        {
            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IOleControlSite.ShowPropertyFrame()
        {
            return NativeMethods.E_NOTIMPL;
        }

        #endregion IOleControlSite

        //
        // IOleClientSite methods:
        //
        #region IOleClientSite

        int UnsafeNativeMethods.IOleClientSite.SaveObject()
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleClientSite.GetMoniker(int dwAssign, int dwWhichMoniker, out Object moniker)
        {
            moniker = null;
            return NativeMethods.E_NOTIMPL;
        }

        ///<SecurityNote> 
        ///     Critical - calls critical Host property
        ///</SecurityNote> 
        [SecurityCritical]
        int UnsafeNativeMethods.IOleClientSite.GetContainer(out UnsafeNativeMethods.IOleContainer container)
        {
            container = this.Host.Container;
            return NativeMethods.S_OK;
        }
        ///<SecurityNote> 
        ///     Critical - calls critical Host property and critical methods AttachWindow
        ///</SecurityNote> 
        [SecurityCritical]
        int UnsafeNativeMethods.IOleClientSite.ShowObject()
        {
            if (HostState >= ActiveXHelper.ActiveXState.InPlaceActive)
            {
                IntPtr hwnd;
                if (NativeMethods.Succeeded(this.Host.ActiveXInPlaceObject.GetWindow(out hwnd)))
                {
                    if (this.Host.ControlHandle.Handle != hwnd)
                    {
                        if (hwnd != IntPtr.Zero)
                        {
                            this.Host.AttachWindow(hwnd);
                            this.OnActiveXRectChange(this.Host.Bounds);
                        }
                    }
                }
                else if (this.Host.ActiveXInPlaceObject is UnsafeNativeMethods.IOleInPlaceObjectWindowless)
                {
                    throw new InvalidOperationException(SR.Get(SRID.AxWindowlessControl));
                }
            }
            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IOleClientSite.OnShowWindow(int fShow)
        {
            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IOleClientSite.RequestNewObjectLayout()
        {
            return NativeMethods.E_NOTIMPL;
        }

        #endregion IOleClientSite

        //
        // IOleInPlaceSite methods:
        //
        #region IOleInPlaceSite

        ///<SecurityNote> 
        ///     Critical - Calls critical code. 
        ///</SecurityNote> 
        [SecurityCritical]
        IntPtr UnsafeNativeMethods.IOleInPlaceSite.GetWindow()
        {
            try
            {
                return this.Host.ParentHandle.Handle;
            }
            catch (Exception t)
            {
                Debug.Fail(t.ToString());
                throw t;
            }
        }

        int UnsafeNativeMethods.IOleInPlaceSite.ContextSensitiveHelp(int fEnterMode)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.CanInPlaceActivate()
        {
            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.OnInPlaceActivate()
        {
            HostState = ActiveXHelper.ActiveXState.InPlaceActive;
            if (!HostBounds.IsEmpty)
            {
                this.OnActiveXRectChange(HostBounds);
            }
            return NativeMethods.S_OK;
        }

        ///<SecurityNote> 
        ///     Critical - calls critical Host property
        ///</SecurityNote> 
        [SecurityCritical]
        int UnsafeNativeMethods.IOleInPlaceSite.OnUIActivate()
        {
            HostState = ActiveXHelper.ActiveXState.UIActive;
            this.Host.Container.OnUIActivate(this.Host);
            return NativeMethods.S_OK;
        }

        ///<SecurityNote> 
        ///     Critical - accesses ParentHandle - critical data. 
        ///</SecurityNote> 
        [SecurityCritical]
        int UnsafeNativeMethods.IOleInPlaceSite.GetWindowContext(out UnsafeNativeMethods.IOleInPlaceFrame ppFrame, out UnsafeNativeMethods.IOleInPlaceUIWindow ppDoc,
                                             NativeMethods.COMRECT lprcPosRect, NativeMethods.COMRECT lprcClipRect, NativeMethods.OLEINPLACEFRAMEINFO lpFrameInfo)
        {
            ppDoc = null;
            ppFrame = this.Host.Container;

            lprcPosRect.left = (int)this.Host.Bounds.left;
            lprcPosRect.top = (int)this.Host.Bounds.top;
            lprcPosRect.right = (int)this.Host.Bounds.right;
            lprcPosRect.bottom = (int)this.Host.Bounds.bottom;

            lprcClipRect = this.Host.Bounds;
            if (lpFrameInfo != null)
            {
                lpFrameInfo.cb = (uint)Marshal.SizeOf(typeof(NativeMethods.OLEINPLACEFRAMEINFO));
                lpFrameInfo.fMDIApp = false;
                lpFrameInfo.hAccel = IntPtr.Zero;
                lpFrameInfo.cAccelEntries = 0;
                lpFrameInfo.hwndFrame = this.Host.ParentHandle.Handle;
            }

            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.Scroll(NativeMethods.SIZE scrollExtant)
        {
            return NativeMethods.S_FALSE;
        }

        ///<SecurityNote> 
        ///     Critical - calls critical Host property
        ///</SecurityNote> 
        [SecurityCritical]
        int UnsafeNativeMethods.IOleInPlaceSite.OnUIDeactivate(int fUndoable)
        {
            this.Host.Container.OnUIDeactivate(this.Host);
            if (HostState > ActiveXHelper.ActiveXState.InPlaceActive)
            {
                HostState = ActiveXHelper.ActiveXState.InPlaceActive;
            }
            return NativeMethods.S_OK;
        }

        ///<SecurityNote> 
        ///     Critical - calls critical Host property
        ///</SecurityNote> 
        [SecurityCritical]
        int UnsafeNativeMethods.IOleInPlaceSite.OnInPlaceDeactivate()
        {
            if (HostState == ActiveXHelper.ActiveXState.UIActive)
            {
                ((UnsafeNativeMethods.IOleInPlaceSite)this).OnUIDeactivate(0);
            }

            this.Host.Container.OnInPlaceDeactivate(this.Host);
            HostState = ActiveXHelper.ActiveXState.Running;
            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.DiscardUndoState()
        {
            return NativeMethods.S_OK;
        }

        ///<SecurityNote> 
        ///     Critical - calls critical Host property
        ///</SecurityNote> 
        [SecurityCritical]
        int UnsafeNativeMethods.IOleInPlaceSite.DeactivateAndUndo()
        {
            return this.Host.ActiveXInPlaceObject.UIDeactivate();
        }

        int UnsafeNativeMethods.IOleInPlaceSite.OnPosRectChange(NativeMethods.COMRECT lprcPosRect)
        {
            return this.OnActiveXRectChange(lprcPosRect);
        }

        #endregion IOleInPlaceSite


        ///<SecurityNote> 
        ///     Critical - calls Host property. 
        ///     TreatAsSafe - for get - returning current activeXstate is considered safe. 
        ///                   for set - although you are affecting what code runs. 
        ///                             instantiating the control is the critical thing. Once the control is started. 
        ///                             transitioning between states no more risky than instantiation. 
        ///</SecurityNote> 
        ActiveXHelper.ActiveXState HostState
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                return this.Host.ActiveXState;
            }
            [SecurityCritical, SecurityTreatAsSafe]
            set
            {
                this.Host.ActiveXState = value;
            }
        }

        ///<SecurityNote> 
        ///     Critical - calls Host property. 
        ///     TreatAsSafe - for get - returning host bounds is considered safe. 
        ///</SecurityNote> 
        internal NativeMethods.COMRECT HostBounds
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                return this.Host.Bounds;
            }
        }

        //
        // IPropertyNotifySink methods:
        //
        #region IPropertyNotifySink

        /// <SecurityNote>
        /// WebOCHostedInBrowserProcess: We could get spurious property change notifications. 
        /// But the WebBrowser control doesn't rely on any currently.
        /// </SecurityNote>
        [SecurityCritical]
        void UnsafeNativeMethods.IPropertyNotifySink.OnChanged(int dispid)
        {
            // Some controls fire OnChanged() notifications when getting values of some properties. ASURT 20190.
            // To prevent this kind of recursion, we check to see if we are already inside a OnChanged() call.
            //
            // Consider adding this in the future: 
            //if (this.Host.NoComponentChangeEvents != 0)
            //    return;

            // this.Host.NoComponentChangeEvents++;

            try
            {
                OnPropertyChanged(dispid);
            }
            catch (Exception t)
            {
                Debug.Fail(t.ToString());
                throw t;
            }
            finally
            {
                //this.Host.NoComponentChangeEvents--;
            }
        }

        int UnsafeNativeMethods.IPropertyNotifySink.OnRequestEdit(int dispid)
        {
            return NativeMethods.S_OK;
        }

        #endregion IPropertyNotifySink

        #region Protected Methods
        //
        // Virtual overrides:
        //
        /// <SecurityNote>
        /// WebOCHostedInBrowserProcess: We could get spurious property change notifications.
        /// But the WebBrowser control doesn't rely on any currently.
        /// </SecurityNote>
        [SecurityCritical]
        internal virtual void OnPropertyChanged(int dispid)
        {
            /*
            try {
                ISite site = this.Host.Site;
                if (site != null) {
                    IComponentChangeService changeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));

                    if (changeService != null) {
                        try {
                            changeService.OnComponentChanging(this.Host, null);
                        }
                        catch (CheckoutException coEx) {
                            if (coEx == CheckoutException.Canceled) {
                                return;
                            }
                            throw coEx;
                        }

                        // Now notify the change service that the change was successful.
                        //
                        changeService.OnComponentChanged(this.Host, null, null, null);
                    }
                }
            }
            catch (Exception t) {
                Debug.Fail(t.ToString());
                throw t;
            }
             */
        }

        #endregion Protected Methods

        #region Internal Properties
        /// Retrieves the ActiveXHost object set in the constructor.
        ///<SecurityNote> 
        ///     Critical - returns critical data. 
        ///</SecurityNote>         
        internal ActiveXHost Host
        {
            [SecurityCritical]
            get { return _host; }
        }
        #endregion Internal Properties

        #region Internal Methods
        //
        // Internal helper methods:
        //

        // Commented out until it is needed to comply with FXCOP

        /////<SecurityNote> 
        /////     Critical - returns critical data. 
        /////</SecurityNote> 
        //[SecurityCritical ]              
        //internal ActiveXHost GetAxHost() 
        //{
        //    return this.Host;
        //}

        ///<SecurityNote> 
        ///     Critical - instantiates ConnectionPointCoookie - a critical class. 
        ///</SecurityNote> 
        [SecurityCritical]
        internal void StartEvents()
        {
            if (_connectionPoint != null)
                return;

            Object nativeObject = this.Host.ActiveXInstance;
            if (nativeObject != null)
            {
                try
                {
                    _connectionPoint = new ConnectionPointCookie(nativeObject, this, typeof(UnsafeNativeMethods.IPropertyNotifySink));
                }
                catch (Exception ex)
                {
                    if (CriticalExceptions.IsCriticalException(ex))
                    {
                        throw;
                    }
                }
            }
        }

        ///<SecurityNote> 
        ///     Critical - calls critical _connectionPoint property
        ///     NOT TAS - stopping listening to events - could turn off some mitigations. 
        ///</SecurityNote> 
        [SecurityCritical]
        internal void StopEvents()
        {
            if (_connectionPoint != null)
            {
                _connectionPoint.Disconnect();
                _connectionPoint = null;
            }
        }

        ///<SecurityNote> 
        ///     Critical - calls critical Host property
        ///     TreatAsSafe - changing the size of the control is considered safe. 
        ///</SecurityNote> 
        [SecurityCritical, SecurityTreatAsSafe]
        internal int OnActiveXRectChange(NativeMethods.COMRECT lprcPosRect)
        {
            if (this.Host.ActiveXInPlaceObject != null)
            {
                this.Host.ActiveXInPlaceObject.SetObjectRects(
                        lprcPosRect,
                        lprcPosRect); //Same clip rect

                this.Host.Bounds = lprcPosRect;
            }

            return NativeMethods.S_OK;
        }

        #endregion Internal Methods

        #region Private Fields

        ///<SecurityNote> 
        ///     Critical - contains critical data. 
        ///</SecurityNote> 
        [SecurityCritical]
        private ActiveXHost _host;

        ///<SecurityNote> 
        ///     Critical - contains critical data. 
        ///</SecurityNote> 
        [SecurityCritical]
        private ConnectionPointCookie _connectionPoint;

        #endregion Private Fields
    }
}


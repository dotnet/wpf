// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MS.Win32;
using Windows.Win32.Foundation;

namespace MS.Internal.Controls
{
    ///
    /// This class implements the necessary interfaces required for an ActiveX site.
    ///
    /// <remarks>
    /// THREADING ISSUE: See comment on WebBrowserSite.
    /// </remarks>
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

        internal ActiveXSite(ActiveXHost host)
        {
            ArgumentNullException.ThrowIfNull(host);
            _host = host;
        }

        #endregion Constructor

        //
        // IOleControlSite methods:
        //
        #region IOleControlSite
        int UnsafeNativeMethods.IOleControlSite.OnControlInfoChanged()
        {
            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IOleControlSite.LockInPlaceActive(int fLock)
        {
            return HRESULT.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleControlSite.GetExtendedControl(out object ppDisp)
        {
            ppDisp = null;
            return HRESULT.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleControlSite.TransformCoords(ref NativeMethods.POINT pPtlHimetric, ref NativeMethods.POINTF pPtfContainer, int dwFlags)
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
                    return HRESULT.E_INVALIDARG;
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
                    return HRESULT.E_INVALIDARG;
                }
            }
            else
            {
                return HRESULT.E_INVALIDARG;
            }

            return HRESULT.S_OK;
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
                return f ? HRESULT.S_OK : HRESULT.S_FALSE;
            }
            finally {
                this.Host.SetAxHostState(AxHostHelper.siteProcessedInputKey, false);
            }
            */

            // This is called by IOleInPlaceActiveObject::TranslateAccelerator. 
            // returning S_FALSE means we don't process the messages. Let the webbrowser control handle it.
            return HRESULT.S_FALSE;
        }

        int UnsafeNativeMethods.IOleControlSite.OnFocus(int fGotFocus)
        {
            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IOleControlSite.ShowPropertyFrame()
        {
            return HRESULT.E_NOTIMPL;
        }

        #endregion IOleControlSite

        //
        // IOleClientSite methods:
        //
        #region IOleClientSite

        int UnsafeNativeMethods.IOleClientSite.SaveObject()
        {
            return HRESULT.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleClientSite.GetMoniker(int dwAssign, int dwWhichMoniker, out Object moniker)
        {
            moniker = null;
            return HRESULT.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleClientSite.GetContainer(out UnsafeNativeMethods.IOleContainer container)
        {
            container = Host.Container;
            return HRESULT.S_OK;
        }
        int UnsafeNativeMethods.IOleClientSite.ShowObject()
        {
            if (HostState >= ActiveXHelper.ActiveXState.InPlaceActive)
            {
                IntPtr hwnd;
                if (Host.ActiveXInPlaceObject.GetWindow(out hwnd).Succeeded)
                {
                    if (Host.ControlHandle.Handle != hwnd)
                    {
                        if (hwnd != IntPtr.Zero)
                        {
                            Host.AttachWindow(hwnd);
                            OnActiveXRectChange(Host.Bounds);
                        }
                    }
                }
                else if (Host.ActiveXInPlaceObject is UnsafeNativeMethods.IOleInPlaceObjectWindowless)
                {
                    throw new InvalidOperationException(SR.AxWindowlessControl);
                }
            }
            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IOleClientSite.OnShowWindow(int fShow)
        {
            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IOleClientSite.RequestNewObjectLayout()
        {
            return HRESULT.E_NOTIMPL;
        }

        #endregion IOleClientSite

        //
        // IOleInPlaceSite methods:
        //
        #region IOleInPlaceSite

        IntPtr UnsafeNativeMethods.IOleInPlaceSite.GetWindow()
        {
            try
            {
                return Host.ParentHandle.Handle;
            }
            catch (Exception t)
            {
                Debug.Fail(t.ToString());
                throw;
            }
        }

        int UnsafeNativeMethods.IOleInPlaceSite.ContextSensitiveHelp(int fEnterMode)
        {
            return HRESULT.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.CanInPlaceActivate()
        {
            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.OnInPlaceActivate()
        {
            HostState = ActiveXHelper.ActiveXState.InPlaceActive;
            if (!HostBounds.IsEmpty)
            {
                OnActiveXRectChange(HostBounds);
            }
            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.OnUIActivate()
        {
            HostState = ActiveXHelper.ActiveXState.UIActive;
            Host.Container.OnUIActivate(Host);
            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.GetWindowContext(out UnsafeNativeMethods.IOleInPlaceFrame ppFrame, out UnsafeNativeMethods.IOleInPlaceUIWindow ppDoc,
                                             NativeMethods.COMRECT lprcPosRect, NativeMethods.COMRECT lprcClipRect, NativeMethods.OLEINPLACEFRAMEINFO lpFrameInfo)
        {
            ppDoc = null;
            ppFrame = Host.Container;

            lprcPosRect.left = (int)Host.Bounds.left;
            lprcPosRect.top = (int)Host.Bounds.top;
            lprcPosRect.right = (int)Host.Bounds.right;
            lprcPosRect.bottom = (int)Host.Bounds.bottom;

            lprcClipRect = Host.Bounds;
            if (lpFrameInfo != null)
            {
                lpFrameInfo.cb = (uint)Marshal.SizeOf(typeof(NativeMethods.OLEINPLACEFRAMEINFO));
                lpFrameInfo.fMDIApp = false;
                lpFrameInfo.hAccel = IntPtr.Zero;
                lpFrameInfo.cAccelEntries = 0;
                lpFrameInfo.hwndFrame = Host.ParentHandle.Handle;
            }

            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.Scroll(NativeMethods.SIZE scrollExtant)
        {
            return HRESULT.S_FALSE;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.OnUIDeactivate(int fUndoable)
        {
            Host.Container.OnUIDeactivate(Host);
            if (HostState > ActiveXHelper.ActiveXState.InPlaceActive)
            {
                HostState = ActiveXHelper.ActiveXState.InPlaceActive;
            }
            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.OnInPlaceDeactivate()
        {
            if (HostState == ActiveXHelper.ActiveXState.UIActive)
            {
                ((UnsafeNativeMethods.IOleInPlaceSite)this).OnUIDeactivate(0);
            }

            Host.Container.OnInPlaceDeactivate(Host);
            HostState = ActiveXHelper.ActiveXState.Running;
            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.DiscardUndoState()
        {
            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceSite.DeactivateAndUndo()
        {
            return Host.ActiveXInPlaceObject.UIDeactivate();
        }

        int UnsafeNativeMethods.IOleInPlaceSite.OnPosRectChange(NativeMethods.COMRECT lprcPosRect)
        {
            return OnActiveXRectChange(lprcPosRect);
        }

        #endregion IOleInPlaceSite


        ActiveXHelper.ActiveXState HostState
        {
            get
            {
                return Host.ActiveXState;
            }
            set
            {
                Host.ActiveXState = value;
            }
        }

        internal NativeMethods.COMRECT HostBounds
        {
            get
            {
                return Host.Bounds;
            }
        }

        //
        // IPropertyNotifySink methods:
        //
        #region IPropertyNotifySink

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
                throw;
            }
            finally
            {
                //this.Host.NoComponentChangeEvents--;
            }
        }

        int UnsafeNativeMethods.IPropertyNotifySink.OnRequestEdit(int dispid)
        {
            return HRESULT.S_OK;
        }

        #endregion IPropertyNotifySink

        #region Protected Methods
        //
        // Virtual overrides:
        //
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
        internal ActiveXHost Host
        {
            get { return _host; }
        }
        #endregion Internal Properties

        #region Internal Methods
        //
        // Internal helper methods:
        //

        // Commented out until it is needed to comply with FXCOP

        //internal ActiveXHost GetAxHost() 
        //{
        //    return this.Host;
        //}

        internal void StartEvents()
        {
            if (_connectionPoint != null)
                return;

            Object nativeObject = Host.ActiveXInstance;
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

        internal void StopEvents()
        {
            if (_connectionPoint != null)
            {
                _connectionPoint.Disconnect();
                _connectionPoint = null;
            }
        }

        internal int OnActiveXRectChange(NativeMethods.COMRECT lprcPosRect)
        {
            if (Host.ActiveXInPlaceObject != null)
            {
                Host.ActiveXInPlaceObject.SetObjectRects(
                        lprcPosRect,
                        lprcPosRect); //Same clip rect

                Host.Bounds = lprcPosRect;
            }

            return HRESULT.S_OK;
        }

        #endregion Internal Methods

        #region Private Fields

        private ActiveXHost _host;

        private ConnectionPointCookie _connectionPoint;

        #endregion Private Fields
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implements ActiveXContainer interfaces to host
//              ActiveX controls
//
//              Source copied from AxContainer.cs
//

using System;
using System.Collections;
using System.Diagnostics;
using Microsoft.Win32;

using System.Windows.Interop;
using MS.Win32;
using System.Security;
using System.Windows.Controls;

namespace MS.Internal.Controls
{
    #region class ActiveXContainer

    //This implements the basic container interfaces. Other siting related interfaces are
    //implemented on the ActiveXSite object e.g. IOleClientSite, IOleInPlaceSite, IOleControlSite etc.

    internal class ActiveXContainer : UnsafeNativeMethods.IOleContainer, UnsafeNativeMethods.IOleInPlaceFrame
    {
        #region Constructor

        internal ActiveXContainer(ActiveXHost host)
        {
            this._host = host;

            Invariant.Assert(_host != null);
        }

        #endregion Constructor

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        #region IOleContainer

        //
        // IOleContainer methods:
        //
        int UnsafeNativeMethods.IOleContainer.ParseDisplayName(Object pbc, string pszDisplayName, int[] pchEaten, Object[] ppmkOut)
        {
            if (ppmkOut != null)
                ppmkOut[0] = null;

            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleContainer.EnumObjects(int grfFlags, out UnsafeNativeMethods.IEnumUnknown ppenum)
        {
            ppenum = null;

            Debug.Assert(_host != null, "gotta have the avalon activex host");

            object ax = _host.ActiveXInstance;

            //We support only one control, return that here
            //How does one add multiple controls to a container?
            if (ax != null
                &&
                (((grfFlags & NativeMethods.OLECONTF_EMBEDDINGS) != 0)
                  ||
                  ((grfFlags & NativeMethods.OLECONTF_ONLYIFRUNNING) != 0 &&
                    _host.ActiveXState == ActiveXHelper.ActiveXState.Running)))
            {
                Object[] temp = new Object[1];
                temp[0] = ax;
                ppenum = new EnumUnknown(temp);
                return NativeMethods.S_OK;
            }

            ppenum = new EnumUnknown(null);
            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IOleContainer.LockContainer(bool fLock)
        {
            return NativeMethods.E_NOTIMPL;
        }

        #endregion IOleContainer

        #region IOleInPlaceFrame

        //
        // IOleInPlaceFrame methods:
        //

        IntPtr UnsafeNativeMethods.IOleInPlaceFrame.GetWindow()
        {
            return _host.ParentHandle.Handle;
        }

        int UnsafeNativeMethods.IOleInPlaceFrame.ContextSensitiveHelp(int fEnterMode)
        {
            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceFrame.GetBorder(NativeMethods.COMRECT lprectBorder)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleInPlaceFrame.RequestBorderSpace(NativeMethods.COMRECT pborderwidths)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleInPlaceFrame.SetBorderSpace(NativeMethods.COMRECT pborderwidths)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleInPlaceFrame.SetActiveObject(UnsafeNativeMethods.IOleInPlaceActiveObject pActiveObject, string pszObjName)
        {
            //Winforms has code to remove selection handler around the active object
            //and add it around the new one
            //
            //Since we don't have anything like that in Avalon, we do nothing
            //
            //For future reference, added skeletal code on how to get to the internal hosting
            //objects incase they are needed here.


            /*
            ActiveXHost host = null;

            if (pActiveObject is UnsafeNativeMethods.IOleObject)
            {
                UnsafeNativeMethods.IOleObject oleObject = (UnsafeNativeMethods.IOleObject)pActiveObject;
                UnsafeNativeMethods.IOleClientSite clientSite = null;

                try
                {
                    clientSite = oleObject.GetClientSite();
                    if ((clientSite as ActiveXSite) != null)
                    {
                        ctl = ((ActiveXSite)(clientSite)).GetActiveXHost();
                    }
                }
                catch (COMException t)
                {
                    Debug.Fail(t.ToString());
                }
            }
            */

            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceFrame.InsertMenus(IntPtr hmenuShared, NativeMethods.tagOleMenuGroupWidths lpMenuWidths)
        {
            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IOleInPlaceFrame.SetMenu(IntPtr hmenuShared, IntPtr holemenu, IntPtr hwndActiveObject)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleInPlaceFrame.RemoveMenus(IntPtr hmenuShared)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleInPlaceFrame.SetStatusText(string pszStatusText)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleInPlaceFrame.EnableModeless(bool fEnable)
        {
            return NativeMethods.E_NOTIMPL;
        }

        int UnsafeNativeMethods.IOleInPlaceFrame.TranslateAccelerator(ref MSG lpmsg, short wID)
        {
            return NativeMethods.S_FALSE;
        }

        #endregion IOleInPlaceFrame

        internal void OnUIActivate(ActiveXHost site)
        {
            // The ShDocVw control repeatedly calls OnUIActivate() with the same
            // site. This causes the assert below to fire.
            //
            if (_siteUIActive == site)
                return;

            if (_siteUIActive != null)
            {
                //Winforms WebOC also uses ActiveXHost instead of ActiveXSite.
                //Ideally it should have been the site but since its a 1-1 relationship
                //for hosting the webOC, it will work
                ActiveXHost tempSite = _siteUIActive;
                tempSite.ActiveXInPlaceObject.UIDeactivate();
            }
            Debug.Assert(_siteUIActive == null, "Object did not call OnUIDeactivate");
            _siteUIActive = site;

            // Should we set focus to the WebOC here?
        }

        internal void OnUIDeactivate(ActiveXHost site)
        {
#if DEBUG
            if (_siteUIActive != null) {
                //TODO: Debug.Assert(_siteUIActive == site, "deactivating when not active...");
                Debug.Assert(this.ActiveXHost == site, "deactivating when not active...");
            }
#endif // DEBUG

            //TODO: Clear focus from the WebOC of set in OnUIActivate
            //Winforms WebOC code does this only in OnInPlaceDeactivate
            //but this seems to be the right place to do it
            _siteUIActive = null;
        }

        internal void OnInPlaceDeactivate(ActiveXHost site)
        {
            //TODO: Clear the focus here too?
            if (this.ActiveXHost == site)
            {
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        #region Internal Properties

        internal ActiveXHost ActiveXHost
        {
            get { return _host; }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields

        private ActiveXHost _host;


        private ActiveXHost _siteUIActive;

        #endregion Private Fields
    }

    #endregion class ActiveXContainer
}

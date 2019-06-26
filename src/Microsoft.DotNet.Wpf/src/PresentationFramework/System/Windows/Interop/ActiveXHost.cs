// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      An ActiveXHost is a System.Windows.FrameworkElement which can
//      host a windowed ActiveX control.  This class provides a technology
//      bridge between ActiveX controls and Avalon by wrapping ActiveX controls
//      and exposing them as fully featured avalon elements. It implements both the
//      container interfaces (via aggregation) required to host the ActiveXControl
//      and also derives from HwndHost to support hosting an HWND in the avalon tree.
//
//      Currently the activex hosting support is limited to windowed controls.
//
//      Inheritors of this class simply need to concentrate on defining and implementing the
//      properties/methods/events of the specific ActiveX control they are wrapping, the
//      default properties etc and the code to implement the activation etc. are
//      encapsulated in the class below.
//
//      The classid of the ActiveX control is specified in the constructor.
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.Utility;
using MS.Win32;
using System.Security;

// Since we disable PreSharp warnings in this file, PreSharp warning is unknown to C# compiler.
// We first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace System.Windows.Interop
{
    #region ActiveXHost

    /// <summary>
    ///     An ActiveXHost is a Systew.Windows.FrameworkElement which can
    ///     host an ActiveX control. Currently the support is limited to
    ///     windowed controls. This class provides a technology bridge
    ///     between unmanaged ActiveXControls and Avalon framework.
    ///     
    ///     The only reason we expose this class public in ArrowHead without public OM
    ///     is for the WebBrowser control. The WebBrowser class derives from ActiveXHost, and
    ///     we are exposing the WebBrowser class in ArrowHead. This class does not have public
    ///     constructor and OM. It may be enabled in future releases.
    /// </summary>
    public class ActiveXHost : HwndHost
    {
        //------------------------------------------------------
        //
        //  Constructors and Finalizers
        //
        //------------------------------------------------------

        #region Constructors and Finalizers

        static ActiveXHost()
        {
            // We use this map to lookup which invalidator method to call
            // when the Avalon parent's properties change.
            invalidatorMap[UIElement.VisibilityProperty] = new PropertyInvalidator(OnVisibilityInvalidated);
            invalidatorMap[FrameworkElement.IsEnabledProperty] = new PropertyInvalidator(OnIsEnabledInvalidated);

            // register for access keys
            EventManager.RegisterClassHandler(typeof(ActiveXHost), AccessKeyManager.AccessKeyPressedEvent, new AccessKeyPressedEventHandler(OnAccessKeyPressed));

            Control.IsTabStopProperty.OverrideMetadata(typeof(ActiveXHost), new FrameworkPropertyMetadata(true));

            FocusableProperty.OverrideMetadata(typeof(ActiveXHost), new FrameworkPropertyMetadata(true));

            EventManager.RegisterClassHandler(typeof(ActiveXHost), Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGotFocus));
            EventManager.RegisterClassHandler(typeof(ActiveXHost), Keyboard.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnLostFocus));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(ActiveXHost), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
        }


        /// constructor for ActiveXHost
        internal ActiveXHost(Guid clsid, bool fTrusted ) : base( fTrusted )
        {
            // Thread.ApartmentState is [Obsolete]
            #pragma warning disable 0618
            // What if the control is marked as free-threaded?
            if (Thread.CurrentThread.ApartmentState != ApartmentState.STA)
            {
                throw new ThreadStateException(SR.Get(SRID.AxRequiresApartmentThread, clsid.ToString()));
            }
            #pragma warning restore 0618

            _clsid.Value = clsid;

            // hookup so we are notified when loading is finished.
            Initialized += new EventHandler(OnInitialized);
        }


        #endregion Constructors and Finalizers

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        #region Framework Related

        /// <internalonly>
        ///     Overriden to push the values of invalidated properties down to our
        ///     ActiveX control.
        /// </internalonly>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.IsAValueChange || e.IsASubPropertyChange)
            {
                DependencyProperty dp = e.Property;

                // We lookup the property in our invalidatorMap
                // and call the appropriate method to push
                // down the changed value to the hosted ActiveX control.
                if (dp != null && invalidatorMap.ContainsKey(dp))
                {
                    PropertyInvalidator invalidator = (PropertyInvalidator)invalidatorMap[dp];
                    invalidator(this);
                }
            }
        }

        /// <internalonly>
        ///     Overriden to create our window and parent it.
        /// </internalonly>
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            this.ParentHandle = hwndParent;

            //BuildWindowCore should only be called if visible. Bug 1236445 tracks this.
            TransitionUpTo(ActiveXHelper.ActiveXState.InPlaceActive);

            //The above call should have set this interface
            Invariant.Assert(_axOleInPlaceActiveObject != null, "InPlace activation of ActiveX control failed");
            
            if (ControlHandle.Handle == IntPtr.Zero)
            {
                IntPtr inplaceWindow = IntPtr.Zero;
                _axOleInPlaceActiveObject.GetWindow(out inplaceWindow);
                AttachWindow(inplaceWindow);
            }                        

            return _axWindow;
        }

        /// <internalonly>
        ///     Overridden to plug the ActiveX control into Avalon's layout manager.
        /// </internalonly>
        /// <SecurityNote >
        ///     Critical - accesses ActiveXSite critical property
        ///     Not making TAS - you may be able to spoof content of web-pages if you could position any arbitrary
        ///                      control over a WebOC.
        protected override void OnWindowPositionChanged(Rect bounds)
        {
            //Its okay to process this if we the control is not yet created

            _boundRect = bounds;

            //These are already transformed to client co-ordinate/device units for high dpi also
            _bounds.left    = (int) bounds.X;
            _bounds.top     = (int) bounds.Y;
            _bounds.right   = (int) (bounds.Width + bounds.X);
            _bounds.bottom  = (int) (bounds.Height + bounds.Y);

            //SetExtent only sets height and width, can call it for perf if X, Y haven't changed
            //We need to call SetObjectRects instead, which updates X, Y, width and height
            //OnActiveXRectChange calls SetObjectRects
            this.ActiveXSite.OnActiveXRectChange(_bounds);
        }

        /// <summary>
        ///     Derived classes override this method to destroy the
        ///     window being hosted.
        /// </summary>
        protected override void DestroyWindowCore(HandleRef hwnd)
        {
        }

        /// <internalonly>
        ///     Overridden to plug the ActiveX control into Avalon's layout manager.
        /// </internalonly>
        protected override Size MeasureOverride(Size swConstraint)
        {
            base.MeasureOverride(swConstraint);

            double newWidth, newHeight;

            if (Double.IsPositiveInfinity(swConstraint.Width))
                newWidth = 150;
            else
                newWidth = swConstraint.Width;

            if (Double.IsPositiveInfinity(swConstraint.Height))
                newHeight = 150;
            else
                newHeight = swConstraint.Height;

            return new Size(newWidth, newHeight);
        }


        /// <internalOnly>
        ///     Forward the access key to our hosted ActiveX control
        ///</internalOnly>
        protected override void OnAccessKey(AccessKeyEventArgs args)
        {
            Debug.Assert(args.Key.Length > 0, "got an empty access key");
            //Consider adding: HostedControl.ProcessKey(args.Key[0]);  //used in winformshost
            // How do we pass this to the activex control - IOleControl.OnMnemonic?
        }

        #endregion Framework Related

        #region ActiveX Related

        protected override void Dispose(bool disposing)
        {
            try
            {
                if ((disposing) && (!_isDisposed))
                {
                    TransitionDownTo(ActiveXHelper.ActiveXState.Passive);
                    _isDisposed = true;
                }
            }
            finally
            {
                //This destroys the parent window, so call base after we have done our processing.
                base.Dispose(disposing);
            }
        }

        // The native ActiveX control QI's for interfaces on it's site to see if
        // it needs to change it's behaviour. Since the AxSite class is generic,
        // it only implements site interfaces that are generic to all sites. QI's
        // for any more specific interfaces will fail. This is a problem if anyone
        // wants to support any other interfaces on the site. In order to overcome
        // this, one needs to extend AxSite and implement any additional interfaces
        // needed.
        //
        // ActiveX wrapper controls that derive from this class should override the
        // below method and return their own AxSite derived object.
        //
        // This method is protected by an InheritanceDemand (from HwndSource) and
        // a LinkDemand because extending a site is strictly an advanced feature for
        // which one needs UnmanagedCode permissions.
        //

        /// Returns an object that will be set as the site for the native ActiveX control.
        /// Implementors of the site can derive from ActiveXSite class.
        internal virtual ActiveXSite CreateActiveXSite()
        {
            return new ActiveXSite(this);
        }

        internal virtual object CreateActiveXObject(Guid clsid)
        {
            return Activator.CreateInstance(Type.GetTypeFromCLSID(clsid));
        }

        /// This will be called when the native ActiveX control has just been created.
        /// Inheritors of this class can override this method to cast the nativeActiveXObject
        /// parameter to the appropriate interface. They can then cache this interface
        /// value in a member variable. However, they must release this value when
        /// DetachInterfaces is called (by setting the cached interface variable to null).
        internal virtual void AttachInterfaces(object nativeActiveXObject)
        {
        }

        /// See AttachInterfaces for a description of when to override DetachInterfaces.
        internal virtual void DetachInterfaces()
        {
        }

        /// This will be called when we are ready to start listening to events.
        /// Inheritors can override this method to hook their own connection points.
        internal virtual void CreateSink()
        {
        }

        /// <summary>
        /// This will be called when it is time to stop listening to events.
        /// This is where inheritors have to disconnect their connection points.
        /// </summary>
        internal virtual void DetachSink()
        {
        }

        /// <summary>
        /// Called whenever the ActiveX state changes. Subclasses can do additional hookup/cleanup depending
        /// on the state transitions
        /// </summary>
        /// <param name="oldState"></param>
        /// <param name="newState"></param>
        internal virtual void OnActiveXStateChange(int oldState, int newState)
        {
        }

        #endregion ActiveX Related

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        #region Protected Properties

        protected bool IsDisposed
        {
            get { return _isDisposed; }
        }

        #endregion Protected Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        #region ActiveX Related

        /// This method needs to be called by the user of ActiveXHost for each
        /// hosted ActiveX control that has a mnemonic bound to it
        internal void RegisterAccessKey(char key)
        {
            AccessKeyManager.Register(key.ToString(), this);
        }

        internal ActiveXSite ActiveXSite
        {
            get
            {
                if (_axSite == null)
                {
                    _axSite = CreateActiveXSite();
                }
                return _axSite;
            }
        }

        internal ActiveXContainer Container
        {
            get
            {
                if (_axContainer == null)
                {
                    _axContainer = new ActiveXContainer(this);
                }
                return _axContainer;
            }
        }

        internal ActiveXHelper.ActiveXState ActiveXState
        {
            get
            {
                return _axState;
            }
            set
            {
                _axState = value;
            }
        }

        internal bool GetAxHostState(int mask)
        {
            return _axHostState[mask];
        }

        internal void SetAxHostState(int mask, bool value)
        {
            _axHostState[mask] = value;
        }

        internal void TransitionUpTo(ActiveXHelper.ActiveXState state)
        {
            if (!this.GetAxHostState(ActiveXHelper.inTransition))
            {
                this.SetAxHostState(ActiveXHelper.inTransition, true);

                try
                {
                    ActiveXHelper.ActiveXState oldState;

                    while (state > this.ActiveXState)
                    {
                        oldState = this.ActiveXState;

                        switch (this.ActiveXState)
                        {
                            case ActiveXHelper.ActiveXState.Passive:
                                TransitionFromPassiveToLoaded();
                                Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.Loaded, "Failed transition");
                                this.ActiveXState = ActiveXHelper.ActiveXState.Loaded;
                                break;
                            case ActiveXHelper.ActiveXState.Loaded:
                                TransitionFromLoadedToRunning();
                                Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.Running, "Failed transition");
                                this.ActiveXState = ActiveXHelper.ActiveXState.Running;
                                break;
                            case ActiveXHelper.ActiveXState.Running:
                                TransitionFromRunningToInPlaceActive();
                                Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.InPlaceActive, "Failed transition");
                                this.ActiveXState = ActiveXHelper.ActiveXState.InPlaceActive;
                                break;
                            case ActiveXHelper.ActiveXState.InPlaceActive:
                                TransitionFromInPlaceActiveToUIActive();
                                Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.UIActive, "Failed transition");
                                this.ActiveXState = ActiveXHelper.ActiveXState.UIActive;
                                break;
                            default:
                                Debug.Fail("bad state");
                                this.ActiveXState = this.ActiveXState + 1;  // To exit the loop
                                break;
                        }

                        OnActiveXStateChange((int)oldState, (int)this.ActiveXState);
                    }
                }
                finally
                {
                    this.SetAxHostState(ActiveXHelper.inTransition, false);
                }
            }
        }

        internal void TransitionDownTo(ActiveXHelper.ActiveXState state)
        {
            if (!this.GetAxHostState(ActiveXHelper.inTransition))
            {
                this.SetAxHostState(ActiveXHelper.inTransition, true);

                try
                {
                    ActiveXHelper.ActiveXState oldState;

                    while (state < this.ActiveXState)
                    {
                        oldState = this.ActiveXState;

                        switch (this.ActiveXState)
                        {
                            case ActiveXHelper.ActiveXState.Open:
                                Debug.Fail("how did we ever get into the open state?");
                                this.ActiveXState = ActiveXHelper.ActiveXState.UIActive;
                                break;
                            case ActiveXHelper.ActiveXState.UIActive:
                                TransitionFromUIActiveToInPlaceActive();
                                Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.InPlaceActive, "Failed transition");
                                this.ActiveXState = ActiveXHelper.ActiveXState.InPlaceActive;
                                break;
                            case ActiveXHelper.ActiveXState.InPlaceActive:
                                TransitionFromInPlaceActiveToRunning();
                                Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.Running, "Failed transition");
                                this.ActiveXState = ActiveXHelper.ActiveXState.Running;
                                break;
                            case ActiveXHelper.ActiveXState.Running:
                                TransitionFromRunningToLoaded();
                                Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.Loaded, "Failed transition");
                                this.ActiveXState = ActiveXHelper.ActiveXState.Loaded;
                                break;
                            case ActiveXHelper.ActiveXState.Loaded:
                                TransitionFromLoadedToPassive();
                                Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.Passive, "Failed transition");
                                this.ActiveXState = ActiveXHelper.ActiveXState.Passive;
                                break;
                            default:
                                Debug.Fail("bad state");
                                this.ActiveXState = this.ActiveXState - 1;  // To exit the loop
                                break;
                        }

                        OnActiveXStateChange((int)oldState, (int)this.ActiveXState);
                    }
                }
                finally
                {
                    this.SetAxHostState(ActiveXHelper.inTransition, false);
                }
            }
        }

        internal bool DoVerb(int verb)
        {
            int hr = _axOleObject.DoVerb(verb,
                                         IntPtr.Zero,
                                         this.ActiveXSite,
                                         0,
                                         this.ParentHandle.Handle,
                                         _bounds);

            Debug.Assert(hr == NativeMethods.S_OK, String.Format(CultureInfo.CurrentCulture, "DoVerb call failed for verb 0x{0:X}", verb));
            return hr == NativeMethods.S_OK;
        }

        internal void AttachWindow(IntPtr hwnd)
        {
            if (_axWindow.Handle == hwnd)
                return;

            //Ideally it shouldn't happen, but what if we already have a _axWindow?
            //should we call ReleaseHandle even though we don't own it?
            //should we also call UnsafeNativeMethods.SetParent(_axWindow, null); first?

            _axWindow = new HandleRef(this, hwnd);

            if (this.ParentHandle.Handle != IntPtr.Zero)
            {
                UnsafeNativeMethods.SetParent(_axWindow, this.ParentHandle);
            }
        }

        private void StartEvents()
        {
            if (!this.GetAxHostState(ActiveXHelper.sinkAttached))
            {
                this.SetAxHostState(ActiveXHelper.sinkAttached, true);
                CreateSink();
            }
            this.ActiveXSite.StartEvents();
        }

        private void StopEvents()
        {
            if (this.GetAxHostState(ActiveXHelper.sinkAttached))
            {
                this.SetAxHostState(ActiveXHelper.sinkAttached, false);
                DetachSink();
            }
            this.ActiveXSite.StopEvents();
        }

        private void TransitionFromPassiveToLoaded()
        {
            Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.Passive, "Wrong start state to transition from");
            if (this.ActiveXState == ActiveXHelper.ActiveXState.Passive)
            {
                //
                // First, create the ActiveX control
                Debug.Assert(_axInstance == null, "_axInstance must be null");

                _axInstance = CreateActiveXObject(_clsid.Value);
                Debug.Assert(_axInstance != null, "w/o an exception being thrown we must have an object...");

                //
                // We are now Loaded!
                this.ActiveXState = ActiveXHelper.ActiveXState.Loaded;

                //
                // Lets give them a chance to cast the ActiveX object
                // to the appropriate interfaces.
                this.AttachInterfacesInternal();
            }
        }

        private void TransitionFromLoadedToPassive()
        {
            Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.Loaded, "Wrong start state to transition from");
            if (this.ActiveXState == ActiveXHelper.ActiveXState.Loaded)
            {
                //
                // Need to make sure that we don't handle any PropertyChanged
                // notifications at this point.
                //this.NoComponentChangeEvents++;
                try
                {
                    //
                    // Release the _axInstance
                    if (_axInstance != null)
                    {
                        //
                        // Lets first get the cached interface pointers of _axInstance released.
                        this.DetachInterfacesInternal();

                        Marshal.FinalReleaseComObject(_axInstance);
                        _axInstance = null;
                    }
                }
                finally
                {
                    //Consider: this.NoComponentChangeEvents--;
                }

                //
                // We are now Passive!
                this.ActiveXState = ActiveXHelper.ActiveXState.Passive;
            }
        }

        private void TransitionFromLoadedToRunning()
        {
            Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.Loaded, "Wrong start state to transition from");
            if (this.ActiveXState == ActiveXHelper.ActiveXState.Loaded)
            {
                //
                // See if the ActiveX control returns OLEMISC_SETCLIENTSITEFIRST
                int bits = 0;
                int hr = _axOleObject.GetMiscStatus(NativeMethods.DVASPECT_CONTENT, out bits);
                if (NativeMethods.Succeeded(hr) && ((bits & NativeMethods.OLEMISC_SETCLIENTSITEFIRST) != 0))
                {
                    //
                    // Simply setting the site to the ActiveX control should activate it.
                    // And this will take us to the Running state.
                    _axOleObject.SetClientSite(this.ActiveXSite);
                }

                StartEvents();

                //
                // We are now Running!
                this.ActiveXState = ActiveXHelper.ActiveXState.Running;
            }
        }

        private void TransitionFromRunningToLoaded()
        {
            Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.Running, "Wrong start state to transition from");
            if (this.ActiveXState == ActiveXHelper.ActiveXState.Running)
            {
                StopEvents();

                //
                // Inform the ActiveX control that it's been un-sited.
                _axOleObject.SetClientSite(null);

                //
                // We are now Loaded!
                this.ActiveXState = ActiveXHelper.ActiveXState.Loaded;
            }
        }

        private void TransitionFromRunningToInPlaceActive()
        {
            Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.Running, "Wrong start state to transition from");
            if (this.ActiveXState == ActiveXHelper.ActiveXState.Running)
            {
                try
                {
                    DoVerb(NativeMethods.OLEIVERB_INPLACEACTIVATE);
                }
                catch (Exception e)
                {
                    if(CriticalExceptions.IsCriticalException(e))
                    {
                        throw;
                    }
                    else
                    {
                        throw new TargetInvocationException(SR.Get(SRID.AXNohWnd, GetType().Name), e);
                    }
                }

                //
                // We are now InPlaceActive!
                this.ActiveXState = ActiveXHelper.ActiveXState.InPlaceActive;
            }
        }

        private void TransitionFromInPlaceActiveToRunning()
        {
            Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.InPlaceActive, "Wrong start state to transition from");
            if (this.ActiveXState == ActiveXHelper.ActiveXState.InPlaceActive)
            {
                //
                // InPlaceDeactivate.
                _axOleInPlaceObject.InPlaceDeactivate();

                //
                // We are now Running!
                this.ActiveXState = ActiveXHelper.ActiveXState.Running;
            }
        }

        private void TransitionFromInPlaceActiveToUIActive()
        {
            Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.InPlaceActive, "Wrong start state to transition from");
            if (this.ActiveXState == ActiveXHelper.ActiveXState.InPlaceActive)
            {
                DoVerb(NativeMethods.OLEIVERB_UIACTIVATE);

                //
                // We are now UIActive!
                this.ActiveXState = ActiveXHelper.ActiveXState.UIActive;
            }
        }

        private void TransitionFromUIActiveToInPlaceActive()
        {
            Debug.Assert(this.ActiveXState == ActiveXHelper.ActiveXState.UIActive, "Wrong start state to transition from");
            if (this.ActiveXState == ActiveXHelper.ActiveXState.UIActive)
            {
                int hr = _axOleInPlaceObject.UIDeactivate();
                Debug.Assert(NativeMethods.Succeeded(hr), "Failed in IOleInPlaceObject.UiDeactivate");

                //
                // We are now InPlaceActive!
                this.ActiveXState = ActiveXHelper.ActiveXState.InPlaceActive;
            }
        }

        #endregion ActiveX Related

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        #region Framework Related

        /// <summary>
        ///     The DependencyProperty for the TabIndex property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      1
        /// </summary>
        internal static readonly DependencyProperty TabIndexProperty
            = Control.TabIndexProperty.AddOwner(typeof(ActiveXHost));

        /// <summary>
        ///     TabIndex property change the order of Tab navigation between Controls.
        ///     Control with lower TabIndex will get focus before the Control with higher index
        /// </summary>
        internal int TabIndex
        {
            get { return (int) GetValue(TabIndexProperty); }
            set { SetValue(TabIndexProperty, value); }
        }

        internal HandleRef ParentHandle
        {
            get { return _hwndParent; }

            set { _hwndParent = value; }
        }

        // This returns a COMRECT.
        internal NativeMethods.COMRECT Bounds
        {
            get { return _bounds; }
            //How do we notify Avalon tree/layout that the control needs a new size?
            set { _bounds = value; }
        }

        // This returns a Rect.
        internal Rect BoundRect
        {
            get { return _boundRect; }
        }

        #endregion Framework Related

        #region ActiveX Related

        /// <summary>
        /// Returns the hosted ActiveX control's handle
        /// </summary>
        internal HandleRef ControlHandle
        {
            get { return _axWindow; }
        }

        ///<summary>
        ///Returns the native webbrowser object that this control wraps. Needs FullTrust to access.
        /// Internally we will access the private field directly for perf reasons, no security checks
        ///</summary>
        internal object ActiveXInstance
        {
            get
            {
                return _axInstance;
            }
        }


        internal UnsafeNativeMethods.IOleInPlaceObject ActiveXInPlaceObject
        {
            get
            {
                return _axOleInPlaceObject;
            }
        }

        internal UnsafeNativeMethods.IOleInPlaceActiveObject ActiveXInPlaceActiveObject
        {
            get
            {
                return _axOleInPlaceActiveObject;
            }
        }

        #endregion ActiveXRelated

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        #region Framework Methods

        private void OnInitialized(object sender, EventArgs e)
        {
            // Do the needful for getting this info from the ActiveX control
            //ArrayList keys = new ArrayList();
            //GetMnemonicList(HostedControl, keys);
            //foreach(char k in keys)
            //{
            //    AccessKeyManager.Register(k.ToString(), this);
            //}

            this.Initialized -= new EventHandler(this.OnInitialized);

            //Cannot inplace activate yet, since BuildWindowCore is not called yet
            //and we need that for getting the parent handle to pass on to the control.
        }

        private static void OnIsEnabledInvalidated(ActiveXHost axHost)
        {
            //Consider: ActiveX equivalent?
            //if (axHost != null)
            //{
            //    axHost.HostedControl.Enabled = axHost.IsEnabled;
            //}
        }

        private static void OnVisibilityInvalidated(ActiveXHost axHost)
        {
            if (axHost != null)
            {
                switch (axHost.Visibility)
                {
                    case Visibility.Visible:
                        //Consider: axHost.HostedControl.Visible = true;
                        break;
                    case Visibility.Collapsed:
                        //Consider: axHost.HostedControl.Visible = false;
                        break;
                    case Visibility.Hidden:
                        //Consider:
                        //axHost._cachedSize = axHost.HostedControl.PreferredSize;
                        //axHost._hidden = true;
                        //axHost.HostedControl.Visible = false;
                        break;
                }
            }
        }

        ///     This event handler forwards focus events to the hosted ActiveX control
        private static void OnGotFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ActiveXHost axhost = sender as ActiveXHost;

            if (axhost != null)
            {
                Invariant.Assert(axhost.ActiveXState >= ActiveXHelper.ActiveXState.InPlaceActive, "Should at least be InPlaceActive when getting focus");

                if (axhost.ActiveXState < ActiveXHelper.ActiveXState.UIActive)
                {
                    axhost.TransitionUpTo(ActiveXHelper.ActiveXState.UIActive);
                }
            }
        }

        ///     This event handler forwards focus events to the hosted WF controls
        private static void OnLostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ActiveXHost axhost = sender as ActiveXHost;

            if (axhost != null)
            {
                // If the focus goes from our control window to one of the child windows,
                // we should not deactivate.
                //

                Invariant.Assert(axhost.ActiveXState >= ActiveXHelper.ActiveXState.UIActive, "Should at least be UIActive when losing focus");

                bool uiDeactivate = !axhost.IsKeyboardFocusWithin;

                if (uiDeactivate)
                {
                    axhost.TransitionDownTo(ActiveXHelper.ActiveXState.InPlaceActive);
                }
            }
        }

        private static void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs args)
        {
            if (!args.Handled && args.Scope == null && args.Target == null)
            {
                args.Target = (UIElement)sender;
            }
        }

        /*
                private void GetMnemonicList(SWF.Control control, ArrayList mnemonicList) {

                    // Get the mnemonic for our control
                    //
                    char mnemonic = HostUtils.GetMnemonic(control.Text, true);
                    if (mnemonic != 0)
                    {
                        mnemonicList.Add(mnemonic);
                    }

                    // And recurse for our children, currently we only support one activex control
                    //
                    if (HostedControl != null) GetMnemonicList(HostedControl, mnemonicList);
                }
        */

        #endregion Framework Methods

        #region ActiveX Related

        private void AttachInterfacesInternal()
        {
            Debug.Assert(_axInstance != null, "The native control is null");
            _axOleObject = (UnsafeNativeMethods.IOleObject)_axInstance;
            _axOleInPlaceObject = (UnsafeNativeMethods.IOleInPlaceObject)_axInstance;
            _axOleInPlaceActiveObject = (UnsafeNativeMethods.IOleInPlaceActiveObject)_axInstance;
            //
            // Lets give the inheriting classes a chance to cast
            // the ActiveX object to the appropriate interfaces.
            AttachInterfaces(_axInstance);
        }

        private void DetachInterfacesInternal()
        {
            _axOleObject = null;
            _axOleInPlaceObject = null;
            _axOleInPlaceActiveObject = null;
            //
            // Lets give the inheriting classes a chance to release
            // their cached interfaces of the ActiveX object.
            DetachInterfaces();
        }

        private NativeMethods.SIZE SetExtent(int width, int height)
        {
            NativeMethods.SIZE sz = new NativeMethods.SIZE();
            sz.cx = width;
            sz.cy = height;

            bool resetExtents = false;
            try
            {
                _axOleObject.SetExtent(NativeMethods.DVASPECT_CONTENT, sz);
            }
            catch (COMException)
            {
                resetExtents = true;
            }
            if (resetExtents)
            {
                _axOleObject.GetExtent(NativeMethods.DVASPECT_CONTENT, sz);
                try
                {
                    _axOleObject.SetExtent(NativeMethods.DVASPECT_CONTENT, sz);
                }
                catch (COMException e)
                {
                    Debug.Fail(e.ToString());
                }
            }
            return GetExtent();
        }

        private NativeMethods.SIZE GetExtent()
        {
            NativeMethods.SIZE sz = new NativeMethods.SIZE();
            _axOleObject.GetExtent(NativeMethods.DVASPECT_CONTENT, sz);
            return sz;
        }

        #endregion ActiveX Related

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        #region Framework Related

        private static Hashtable invalidatorMap = new Hashtable();

        private delegate void PropertyInvalidator(ActiveXHost axhost);

        private NativeMethods.COMRECT _bounds    = new NativeMethods.COMRECT(0, 0, 0, 0);
        private Rect             _boundRect      = new Rect(0, 0, 0, 0);

        private Size             _cachedSize     = Size.Empty;
        private HandleRef        _hwndParent;
        private bool             _isDisposed;

        #endregion Framework Related

        #region ActiveX Related

        private SecurityCriticalDataForSet<Guid>    _clsid;

        private HandleRef                   _axWindow;
        private BitVector32                 _axHostState    = new BitVector32();
        private ActiveXHelper.ActiveXState  _axState        = ActiveXHelper.ActiveXState.Passive;

        private ActiveXSite                 _axSite;

        private ActiveXContainer            _axContainer;

        private object                      _axInstance;

        // Pointers to the ActiveX object: Interface pointers are cached for perf.
        private UnsafeNativeMethods.IOleObject              _axOleObject;

        private UnsafeNativeMethods.IOleInPlaceObject       _axOleInPlaceObject;

        private UnsafeNativeMethods.IOleInPlaceActiveObject _axOleInPlaceActiveObject;

        #endregion ActiveX Related


        #endregion Private Fields
    }
    #endregion ActiveXHost
}

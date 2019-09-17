// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Manages Text Services Framework state.
//
//

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using System.Security;
using System.Diagnostics;
using System.Collections;
using MS.Utility;
using MS.Win32;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper

namespace System.Windows.Input
{
    //------------------------------------------------------
    //
    //  TextServicesContext class
    //
    //------------------------------------------------------

    /// <summary>
    /// This class manages the ITfThreadMgr, EmptyDim and the reference to
    /// the default TextStore.
    /// The instance of TextServicesContext class is created per Dispatcher.
    /// </summary>
    /// <remarks>
    /// </remarks>
    internal class TextServicesContext
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Instantiates a TextServicesContext.
        /// </summary>
        private TextServicesContext()
        {
            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "SetDispatcherThreaad on MTA thread");

            // We will clean up Cicero's resource when Dispatcher is terminated or
            // when AppDomain is unloaded.
            TextServicesContextShutDownListener listener = new TextServicesContextShutDownListener(this, ShutDownEvents.DispatcherShutdown | ShutDownEvents.DomainUnload);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Releases all unmanaged resources allocated by the
        /// TextServicesContext.
        /// </summary>
        /// <remarks>
        /// if appDomainShutdown == false, this method must be called on the
        /// Dispatcher thread.  Otherwise, the caller is an AppDomain.Shutdown
        /// listener, and is calling from a worker thread.
        /// </remarks>
        internal void Uninitialize(bool appDomainShutdown)
        {
            // Unregister DefaultTextStore.
            if (_defaultTextStore != null)
            {
                StopTransitoryExtension();
                if (_defaultTextStore.DocumentManager != null)
                {
                    _defaultTextStore.DocumentManager.Pop(UnsafeNativeMethods.PopFlags.TF_POPF_ALL);
                    Marshal.ReleaseComObject(_defaultTextStore.DocumentManager);
                    _defaultTextStore.DocumentManager = null;
                }

                // We can't use tls during AppDomainShutdown -- we're called on
                // a worker thread.  But we don't need to cleanup in that case
                // either.
                if (!appDomainShutdown)
                {
                    InputMethod.Current.DefaultTextStore = null;
                }

                _defaultTextStore = null;
            }

            // Free up any remaining textstores.
            if (_istimactivated == true)
            {
                // Shut down the thread manager when the last TextStore goes away.
                // On XP, if we're called on a worker thread (during AppDomain shutdown)
                // we can't call call any methods on _threadManager.  The problem is
                // that there's no proxy registered for ITfThreadMgr on OS versions
                // previous to Vista.  Not calling Deactivate will leak the IMEs, but
                // in practice (1) they're singletons, so it's not unbounded; and (2)
                // most applications will share the thread with other AppDomains that
                // have a UI, in which case the IME won't be released until the process
                // shuts down in any case.  In theory we could also work around this
                // problem by creating our own XP proxy/stub implementation, which would
                // be added to WPF setup....
                if (!appDomainShutdown || System.Environment.OSVersion.Version.Major >= 6)
                {
                    _threadManager.Value.Deactivate();
                }
                _istimactivated = false;
            }

            // Release the empty dim.
            if (_dimEmpty != null)
            {
                if (_dimEmpty.Value != null)
                {
                    Marshal.ReleaseComObject(_dimEmpty.Value);
                }
                _dimEmpty = null;
            }

            // Release the ThreadManager.
            // We don't do this in UnregisterTextStore because someone may have
            // called get_ThreadManager after the last TextStore was unregistered.
            if (_threadManager != null)
            {
                if (_threadManager.Value != null)
                {
                    Marshal.ReleaseComObject(_threadManager.Value);
                }
                _threadManager = null;
            }
        }

        /// <summary>
        /// Feeds a keystroke to the Text Services Framework, wrapper for
        /// ITfKeystrokeMgr::TestKeyUp/TestKeyDown/KeyUp/KeyDown.
        /// </summary>
        /// <remarks>
        /// Must be called on the main dispatcher thread.
        /// </remarks>
        /// <returns>
        /// true if the keystroke will be eaten by the Text Services Framework,
        /// false otherwise.
        /// Callers should stop further processing of the keystroke on true,
        /// continue otherwise.
        /// </returns>
        internal bool Keystroke(int wParam, int lParam, KeyOp op)
        {
            bool fConsume;
            UnsafeNativeMethods.ITfKeystrokeMgr keystrokeMgr;

            // We delay load cicero until someone creates an ITextStore.
            // Or this thread may not have a ThreadMgr.
            if ((_threadManager == null) || (_threadManager.Value == null))
                return false;

            keystrokeMgr = _threadManager.Value as UnsafeNativeMethods.ITfKeystrokeMgr;

            switch (op)
            {
                case KeyOp.TestUp:
                    keystrokeMgr.TestKeyUp(wParam, lParam, out fConsume);
                    break;
                case KeyOp.TestDown:
                    keystrokeMgr.TestKeyDown(wParam, lParam, out fConsume);
                    break;
                case KeyOp.Up:
                    keystrokeMgr.KeyUp(wParam, lParam, out fConsume);
                    break;
                case KeyOp.Down:
                    keystrokeMgr.KeyDown(wParam, lParam, out fConsume);
                    break;
                default:
                    fConsume = false;
                    break;
            }

            return fConsume;
        }

        // Called by framework's TextStore class.  This method registers a
        // document with TSF.  The TextServicesContext must maintain this list
        // to ensure all native resources are released after gc or uninitialization.
        internal void RegisterTextStore(DefaultTextStore defaultTextStore)
        {
            // We must cache the DefaultTextStore because we'll need it from
            // a worker thread if the AppDomain is torn down before the Dispatcher
            // is shutdown.
            _defaultTextStore = defaultTextStore;

            UnsafeNativeMethods.ITfThreadMgr threadManager = ThreadManager;

            if (threadManager != null)
            {
                UnsafeNativeMethods.ITfDocumentMgr doc;
                UnsafeNativeMethods.ITfContext context;
                int editCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;

                // Activate TSF on this thread if this is the first TextStore.
                if (_istimactivated == false)
                {
                    //temp variable created to retrieve the value
                    // which is then stored in the critical data.
                    int clientIdTemp;
                    threadManager.Activate(out clientIdTemp);
                    _clientId = new SecurityCriticalData<int>(clientIdTemp);
                    _istimactivated = true;
                }

                // Create a TSF document.
                threadManager.CreateDocumentMgr(out doc);
                doc.CreateContext(_clientId.Value, 0 /* flags */, _defaultTextStore, out context, out editCookie);
                doc.Push(context);

                // Release any native resources we're done with.
                Marshal.ReleaseComObject(context);

                // Same DocumentManager and EditCookie in _defaultTextStore.
                _defaultTextStore.DocumentManager = doc;
                _defaultTextStore.EditCookie = editCookie;

                // Start the transitory extenstion so we can have Level 1 composition window from Cicero.
                StartTransitoryExtension();
            }
        }


        // Cal ITfThreadMgr.SetFocus() with the dim for the default text store
        internal void SetFocusOnDefaultTextStore()
        {
            SetFocusOnDim(DefaultTextStore.Current.DocumentManager);
        }

        // Cal ITfThreadMgr.SetFocus() with the empty dim.
        internal void SetFocusOnEmptyDim()
        {
            SetFocusOnDim(EmptyDocumentManager);
        }


        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------


        // Get TextServicesContext that is linked to the current Dispatcher.
        // return NULL if the default text store is not registered yet.
        internal static TextServicesContext DispatcherCurrent
        {
            get
            {
                // Create TextServicesContext on demand.
                if (InputMethod.Current.TextServicesContext == null)
                {
                    InputMethod.Current.TextServicesContext = new TextServicesContext();
                }

                return InputMethod.Current.TextServicesContext;
            }
        }

        /// <summary>
        /// This is an internal, link demand protected method.
        /// </summary>
        internal UnsafeNativeMethods.ITfThreadMgr ThreadManager
        {
            // The ITfThreadMgr for this thread.
            get
            {
                if (_threadManager == null)
                {
                    _threadManager = new SecurityCriticalDataClass<UnsafeNativeMethods.ITfThreadMgr>(TextServicesLoader.Load());
                }

                return _threadManager.Value;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Enums
        //
        //------------------------------------------------------

        #region Internal Enums

        /// <summary>
        /// Specifies the type of keystroke operation to perform in the
        /// TextServicesContext.Keystroke method.
        /// </summary>
        internal enum KeyOp
        {
            /// <summary>
            /// ITfKeystrokeMgr::TestKeyUp
            /// </summary>
            TestUp,

            /// <summary>
            /// ITfKeystrokeMgr::TestKeyDown
            /// </summary>
            TestDown,

            /// <summary>
            /// ITfKeystrokeMgr::KeyUp
            /// </summary>
            Up,

            /// <summary>
            /// ITfKeystrokeMgr::KeyDown
            /// </summary>
            Down
        };

        #endregion Internal Enums

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        // Cal ITfThreadMgr.SetFocus() with dim
        private void SetFocusOnDim(UnsafeNativeMethods.ITfDocumentMgr dim)
        {
            UnsafeNativeMethods.ITfThreadMgr threadmgr = ThreadManager;

            if (threadmgr != null)
            {
                threadmgr.SetFocus(dim);
            }
        }

        // Start the transitory extestion for Cicero Level1/Level2 composition window support.
        private void StartTransitoryExtension()
        {
            Guid guid;
            Object var;
            UnsafeNativeMethods.ITfCompartmentMgr compmgr;
            UnsafeNativeMethods.ITfCompartment comp;
            UnsafeNativeMethods.ITfSource source;
            int transitoryExtensionSinkCookie;

            // Start TransitryExtension
            compmgr = _defaultTextStore.DocumentManager as UnsafeNativeMethods.ITfCompartmentMgr;

            // Set GUID_COMPARTMENT_TRANSITORYEXTENSION
            guid = UnsafeNativeMethods.GUID_COMPARTMENT_TRANSITORYEXTENSION;
            compmgr.GetCompartment(ref guid, out comp);
            var = (int)1;
            comp.SetValue(0, ref var);

            // Advise TransitoryExtension Sink and store the cookie.
            guid = UnsafeNativeMethods.IID_ITfTransitoryExtensionSink;
            source = _defaultTextStore.DocumentManager as UnsafeNativeMethods.ITfSource;
            if (source != null)
            {
                // DocumentManager only supports ITfSource on Longhorn, XP does not support it
                source.AdviseSink(ref guid, _defaultTextStore, out transitoryExtensionSinkCookie);
                _defaultTextStore.TransitoryExtensionSinkCookie = transitoryExtensionSinkCookie;
            }

            Marshal.ReleaseComObject(comp);
        }

        // Stop TransitoryExtesion
        private void StopTransitoryExtension()
        {
            // Unadvice the transitory extension sink.
            if (_defaultTextStore.TransitoryExtensionSinkCookie != UnsafeNativeMethods.TF_INVALID_COOKIE)
            {
                UnsafeNativeMethods.ITfSource source;
                source = _defaultTextStore.DocumentManager as UnsafeNativeMethods.ITfSource;
                if (source != null)
                {
                    // DocumentManager only supports ITfSource on Longhorn, XP does not support it
                    source.UnadviseSink(_defaultTextStore.TransitoryExtensionSinkCookie);
                }
                _defaultTextStore.TransitoryExtensionSinkCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
            }

            // Reset GUID_COMPARTMENT_TRANSITORYEXTENSION
            UnsafeNativeMethods.ITfCompartmentMgr compmgr;
            compmgr = _defaultTextStore.DocumentManager as UnsafeNativeMethods.ITfCompartmentMgr;

            if (compmgr != null)
            {
                Guid guid;
                Object var;
                UnsafeNativeMethods.ITfCompartment comp;
                guid = UnsafeNativeMethods.GUID_COMPARTMENT_TRANSITORYEXTENSION;
                compmgr.GetCompartment(ref guid, out comp);

                if (comp != null)
                {
                    var = (int)0;
                    comp.SetValue(0, ref var);
                    Marshal.ReleaseComObject(comp);
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------


        // Create an empty dim on demand.
        private UnsafeNativeMethods.ITfDocumentMgr EmptyDocumentManager
        {
            get
            {
                if (_dimEmpty == null)
                {
                    UnsafeNativeMethods.ITfThreadMgr threadManager = ThreadManager;
                    if (threadManager == null)
                    {
                        return null;
                    }
                    //creating temp variable to retrieve from call and store in security critical data
                    UnsafeNativeMethods.ITfDocumentMgr dimEmptyTemp;
                    // Create a TSF document.
                    threadManager.CreateDocumentMgr(out dimEmptyTemp);
                    _dimEmpty = new SecurityCriticalDataClass<UnsafeNativeMethods.ITfDocumentMgr>(dimEmptyTemp);
                }
                return _dimEmpty.Value;
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Cached Dispatcher default text store.
        // We must cache the DefaultTextStore because we sometimes need it from
        // a worker thread if the AppDomain is torn down before the Dispatcher
        // is shutdown.
        private DefaultTextStore _defaultTextStore;

        // This is true if thread manager is activated.
        private bool _istimactivated;

        // The root TSF object, created on demand.
        private SecurityCriticalDataClass<UnsafeNativeMethods.ITfThreadMgr> _threadManager;

        // TSF ClientId from Activate call.
        private SecurityCriticalData<int> _clientId;

        // The empty dim for this thread. Created on demand.
        private SecurityCriticalDataClass<UnsafeNativeMethods.ITfDocumentMgr> _dimEmpty;

        #endregion Private Fields

        #region WeakEventTableShutDownListener

        private sealed class TextServicesContextShutDownListener : ShutDownListener
        {
            public TextServicesContextShutDownListener(TextServicesContext target, ShutDownEvents events) : base(target, events)
            {
            }

            internal override void OnShutDown(object target, object sender, EventArgs e)
            {
                TextServicesContext textServicesContext = (TextServicesContext)target;
                textServicesContext.Uninitialize(!(sender is Dispatcher) /*appDomainShutdown*/);
            }
        }

        #endregion TextServicesContextShutDownListener
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: TextServicesHost implementation.
//

using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Documents;
using MS.Win32;
using MS.Internal;
using MS.Internal.PresentationFramework;                   // SecurityHelper
using System.Security;

namespace System.Windows.Documents
{
    //------------------------------------------------------
    //
    //  TextServicesHost class
    //
    //------------------------------------------------------

    //
    // This class manages registration of TextStore.
    // The instance of this class is per Dispatcher and can be shared by
    // all TextStore in same Dispatcher.
    // The registration of TextStore does:
    //    - create DocumentManger for each TextStore.
    //    - advise ThreadFocusSink and EditSink.
    //
    // This activate ITfThreadMgr and keep the referrence of it.
    // When Dispatcher is finished, ITfThreadMgr is deactivated and released.
    //
    internal class TextServicesHost : DispatcherObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new TextServicesHost instance.
        internal TextServicesHost()
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Method
        //
        //------------------------------------------------------

        // TextStore calls this to register it and advise sink.
        internal void RegisterTextStore(TextStore textstore)
        {
            // VerifyAccess();

            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "OnRegisterTextStore must be called on STA thread");

            // Register textstore and advise sinks.
            _RegisterTextStore((TextStore)textstore);

            _thread = Thread.CurrentThread;
        }

        // Free all resources associated with a TextStore.
        internal void UnregisterTextStore(TextStore textstore, bool finalizer)
        {
            if (!finalizer)
            {
                OnUnregisterTextStore(textstore);
            }
            else
            {
                // GC Finalizer is detaching TextStore and the dispatcher thread could be already
                // terminated or Dispatcher is already finished.
                if (!_isDispatcherShutdownFinished)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(OnUnregisterTextStore), textstore);
                }
            }
        }

        internal void RegisterWinEventSink(TextStore textstore)
        {
            // Start WinEvent hook to listen windows move/size event.
            // We need to call ITextStoreACPSink::OnLayoutChange() whenever the window is moved.
            if (_winEvent == null)
            {
                _winEvent = new MoveSizeWinEventHandler();
                _winEvent.Start();
            }

            _winEvent.RegisterTextStore(textstore);
        }

        internal void UnregisterWinEventSink(TextStore textstore)
        {
            _winEvent.UnregisterTextStore(textstore);

            // If the registerd text store count is 0, we don't need WinEvent hook any more.
            if (_winEvent.TextStoreCount == 0)
            {
                _winEvent.Stop();
                _winEvent.Clear();
                _winEvent = null;
            }
        }

        // Start the transitory extestion for Cicero Level1/Level2 composition window support.
        internal static void StartTransitoryExtension(TextStore textstore)
        {
            Guid guid;
            Object var;
            UnsafeNativeMethods.ITfCompartmentMgr compmgr;
            UnsafeNativeMethods.ITfCompartment comp;
            UnsafeNativeMethods.ITfSource source;
            int transitoryExtensionSinkCookie;

            // Start TransitryExtension
            compmgr = textstore.DocumentManager as UnsafeNativeMethods.ITfCompartmentMgr;

            // Set GUID_COMPARTMENT_TRANSITORYEXTENSION
            guid = UnsafeNativeMethods.GUID_COMPARTMENT_TRANSITORYEXTENSION;
            compmgr.GetCompartment(ref guid, out comp);
            var = (int)2; // Use level 2
            comp.SetValue(0, ref var);

            // Advise TransitoryExtension Sink and store the cookie.
            guid = UnsafeNativeMethods.IID_ITfTransitoryExtensionSink;
            source = textstore.DocumentManager as UnsafeNativeMethods.ITfSource;
            if (source != null)
            {
                // DocumentManager only supports ITfSource on Longhorn, XP does not support it
                source.AdviseSink(ref guid, textstore, out transitoryExtensionSinkCookie);
                textstore.TransitoryExtensionSinkCookie = transitoryExtensionSinkCookie;
            }

            Marshal.ReleaseComObject(comp);
        }

        // Stop TransitoryExtesion
        internal static void StopTransitoryExtension(TextStore textstore)
        {
            Guid guid;
            Object var;
            UnsafeNativeMethods.ITfCompartmentMgr compmgr;
            UnsafeNativeMethods.ITfCompartment comp;

            compmgr = textstore.DocumentManager as UnsafeNativeMethods.ITfCompartmentMgr;

            // Unadvice the transitory extension sink.
            if (textstore.TransitoryExtensionSinkCookie != UnsafeNativeMethods.TF_INVALID_COOKIE)
            {
                UnsafeNativeMethods.ITfSource source;
                source = textstore.DocumentManager as UnsafeNativeMethods.ITfSource;
                if (source != null)
                {
                    // DocumentManager only supports ITfSource on Longhorn, XP does not support it
                    source.UnadviseSink(textstore.TransitoryExtensionSinkCookie);
                }
                textstore.TransitoryExtensionSinkCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
            }

            // Reset GUID_COMPARTMENT_TRANSITORYEXTENSION
            guid = UnsafeNativeMethods.GUID_COMPARTMENT_TRANSITORYEXTENSION;
            compmgr.GetCompartment(ref guid, out comp);
            var = (int)0;
            comp.SetValue(0, ref var);

            Marshal.ReleaseComObject(comp);
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        // Return the text services host associated with the current Dispatcher.
        internal static TextServicesHost Current
        {
            get
            {
                TextEditorThreadLocalStore threadLocalStore = TextEditor._ThreadLocalStore;

                if (threadLocalStore.TextServicesHost == null)
                {
                    threadLocalStore.TextServicesHost = new TextServicesHost();
                }

                return threadLocalStore.TextServicesHost;
            }
        }


        // Return ITfThreadMgr
        internal UnsafeNativeMethods.ITfThreadMgr ThreadManager
        {
            get
            {
                if (_threadManager == null)
                {
                    return null;
                }

                return _threadManager.Value;
            }
        }

        //------------------------------------------------------
        //
        //  Private Method
        //
        //------------------------------------------------------

        #region Private Method

        // This is a callback in the dispacher thread to unregister TextStore.
        private object OnUnregisterTextStore(object arg)
        {
            UnsafeNativeMethods.ITfContext context;
            UnsafeNativeMethods.ITfSource source;

            if ((_threadManager == null) || (_threadManager.Value == null))
            {
                return null;
            }

            TextStore textstore = (TextStore)arg;

            // We don't have to release Dispatcher.
            // These Cicero COM calls could be marshalled when UnregisterTextStore is called from
            // TextEditor's Finalizer through TextStore.OnDetach. GC Thread does not take Dispatcher.
            if (textstore.ThreadFocusCookie != UnsafeNativeMethods.TF_INVALID_COOKIE)
            {
                source = _threadManager.Value as UnsafeNativeMethods.ITfSource;
                source.UnadviseSink(textstore.ThreadFocusCookie);
                textstore.ThreadFocusCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
            }

            textstore.DocumentManager.GetBase(out context);
            if (context != null)
            {
                if (textstore.EditSinkCookie != UnsafeNativeMethods.TF_INVALID_COOKIE)
                {
                    source = context as UnsafeNativeMethods.ITfSource;
                    source.UnadviseSink(textstore.EditSinkCookie);
                    textstore.EditSinkCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
                }
                Marshal.ReleaseComObject(context);
            }

            textstore.DocumentManager.Pop(UnsafeNativeMethods.PopFlags.TF_POPF_ALL);
            Marshal.ReleaseComObject(textstore.DocumentManager);
            textstore.DocumentManager = null;

            Debug.Assert(_registeredtextstorecount > 0, "Overrelease TextStore!");
            _registeredtextstorecount--;

            // If Dispatcher is finished and the last textstore
            // is unregistered, we don't need ThreadManager any more. Deactivate and release it.
            // We keep ThreadManager active as long as Dispatcher is alive even if
            // _registeredtextstorecount is 0.
            if (_isDispatcherShutdownFinished && (_registeredtextstorecount == 0))
            {
                DeactivateThreadManager();
            }

            return null;
        }

        // This is a callback when Dispatcher is finished.
        private void OnDispatcherShutdownFinished(object sender, EventArgs args)
        {
            Debug.Assert(CheckAccess(), "OnDispatcherShutdownFinished called on bad thread!");
            Debug.Assert(_isDispatcherShutdownFinished == false, "Was this dispather finished???");

            // Remove the callback.
            Dispatcher.ShutdownFinished -= new EventHandler(OnDispatcherShutdownFinished);

            // Deactivate and release the ThreadManager if no more TextStore is registered.
            if (_registeredtextstorecount == 0)
            {
                DeactivateThreadManager();
            }

            // We keep _dispatcherThread even Dispatcher is being finished. Because
            // this TextServicesHost won't be reused and we want to check if the thread is alive.
            _isDispatcherShutdownFinished = true;
        }

        // Activate TIM if it is not activated yet by this TextServicesHost.
        // And advise sinks for the given textstore.
        private void _RegisterTextStore(TextStore textstore)
        {
            UnsafeNativeMethods.ITfDocumentMgr doc;
            UnsafeNativeMethods.ITfContext context;
            UnsafeNativeMethods.ITfSource source;
            int editCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
            int threadFocusCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
            int editSinkCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
            Guid guid;

            Debug.Assert(CheckAccess(), "RegisterTextStore called on bad thread!");

            // Get ITfThreadMgr
            if (_threadManager == null)
            {
                Debug.Assert(_isDispatcherShutdownFinished == false, "Was this dispather finished?");
                Debug.Assert(_registeredtextstorecount == 0, "TextStore was registered without ThreadMgr?");

                // TextServicesLoader.Load() might return null if no text services are installed or enabled.
                _threadManager = new SecurityCriticalDataClass<UnsafeNativeMethods.ITfThreadMgr>(TextServicesLoader.Load());

                if (_threadManager.Value == null)
                {
                    _threadManager = null;
                    return;
                }

                // Activate TSF on this thread if this is the first TextStore.
                int clientIdTemp;
                _threadManager.Value.Activate(out clientIdTemp);
                _clientId = new SecurityCriticalData<int>(clientIdTemp);

                // We want to get the notification when Dispatcher is finished.
                Dispatcher.ShutdownFinished += new EventHandler(OnDispatcherShutdownFinished);
            }

            // Create a TSF document.
            _threadManager.Value.CreateDocumentMgr(out doc);
            doc.CreateContext(_clientId.Value, 0 /* flags */, textstore, out context, out editCookie);
            doc.Push(context);

            // Attach a thread focus sink.
            if (textstore is UnsafeNativeMethods.ITfThreadFocusSink)
            {
                guid = UnsafeNativeMethods.IID_ITfThreadFocusSink;
                source = _threadManager.Value as UnsafeNativeMethods.ITfSource;
                source.AdviseSink(ref guid, textstore, out threadFocusCookie);
            }

            // Attach an edit sink.
            if (textstore is UnsafeNativeMethods.ITfTextEditSink)
            {
                guid = UnsafeNativeMethods.IID_ITfTextEditSink;
                source = context as UnsafeNativeMethods.ITfSource;
                source.AdviseSink(ref guid, textstore, out editSinkCookie);
            }

            // Release any native resources we're done with.
            Marshal.ReleaseComObject(context);

            textstore.DocumentManager = doc;
            textstore.ThreadFocusCookie = threadFocusCookie;
            textstore.EditSinkCookie = editSinkCookie;
            textstore.EditCookie = editCookie;

            // If Scope of this textstore already has a focus, we need to call SetFocus()
            // in order to put this DIM on Cicero's focus. TextStore.OnGotFocus() calls
            // ITfThreadMgr::SetFocus();
            if (textstore.UiScope.IsKeyboardFocused)
            {
                textstore.OnGotFocus();
            }

            _registeredtextstorecount++;
        }

        // Deactivate and release ThreadManager.
        private void DeactivateThreadManager()
        {
            if (_threadManager != null) 
            {
                if (_threadManager.Value != null)
                {
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
                    if (_thread == Thread.CurrentThread || System.Environment.OSVersion.Version.Major >= 6)
                    {
                        _threadManager.Value.Deactivate();
                    }
                    Marshal.ReleaseComObject(_threadManager.Value);
                }
                _threadManager = null;
            }

            // ThreadManager was deactivated. It is time to release this TextServicesHost.
            TextEditorThreadLocalStore threadLocalStore = TextEditor._ThreadLocalStore;
            threadLocalStore.TextServicesHost = null;
        }

        #endregion Private Method

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Number of the registered TextStore.
        private int _registeredtextstorecount;

        // TSF ClientId from Activate call.
        private SecurityCriticalData<int> _clientId;

        // The root TSF object, created on demand.
        private SecurityCriticalDataClass<UnsafeNativeMethods.ITfThreadMgr> _threadManager;

        // This is true if Dispatcher is finished.
        private bool _isDispatcherShutdownFinished;

        // WinEvent handler for windows move/size.
        private MoveSizeWinEventHandler _winEvent;

        // Thread this host has affinity for.
        private Thread _thread;

        #endregion Private Fields
    }
}

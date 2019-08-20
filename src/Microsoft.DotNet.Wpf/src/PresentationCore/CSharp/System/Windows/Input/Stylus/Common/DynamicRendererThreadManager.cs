// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      Dynamic Renderering Dispatcher - Provides shared Dispatcher for off application
//      Dispatcher inking support.
//
//

using System;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Windows.Threading;
using MS.Utility;
using System.Security;
using MS.Internal;

namespace System.Windows.Input.StylusPlugIns
{
    /// <summary>
    /// Manager for the Dispatcher.ShutdownStarted event.
    /// </summary>
    // Note: this class should have the same visibility (public / internal /
    // private) as the event it manages.  If the event is not public, change
    // the visibility of this class accordingly.
    internal sealed class DispatcherShutdownStartedEventManager : WeakEventManager
    {
        #region Constructors

        //
        //  Constructors
        //

        private DispatcherShutdownStartedEventManager()
        {
        }

        #endregion Constructors

        #region Public Methods

        //
        //  Public Methods
        //

        /// <summary>
        /// Add a listener to the given source's event.
        /// </summary>
        public static void AddListener(Dispatcher source, IWeakEventListener listener)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (listener == null)
                throw new ArgumentNullException("listener");

            CurrentManager.ProtectedAddListener(source, listener);
        }


        #endregion Public Methods

        #region Protected Methods

        //
        //  Protected Methods
        //

        /// <summary>
        /// Listen to the given source for the event.
        /// </summary>
        protected override void StartListening(object source)
        {
            Dispatcher typedSource = (Dispatcher)source;
            typedSource.ShutdownStarted += new EventHandler(OnShutdownStarted);
        }

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected override void StopListening(object source)
        {
            Dispatcher typedSource = (Dispatcher)source;
            typedSource.ShutdownStarted -= new EventHandler(OnShutdownStarted);
        }

        #endregion Protected Methods

        #region Private Properties

        //
        //  Private Properties
        //

        /// <summary>
        /// get the event manager for the current thread
        /// </summary>
        private static DispatcherShutdownStartedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(DispatcherShutdownStartedEventManager);
                DispatcherShutdownStartedEventManager manager = (DispatcherShutdownStartedEventManager)GetCurrentManager(managerType);

                // at first use, create and register a new manager
                if (manager == null)
                {
                    manager = new DispatcherShutdownStartedEventManager();
                    SetCurrentManager(managerType, manager);
                }

                return manager;
            }
        }

        #endregion Private Properties

        #region Private Methods

        //
        //  Private Methods
        //

        // event handler for ShutdownStarted event
        private void OnShutdownStarted(object sender, EventArgs args)
        {
            DeliverEvent(sender, args);
        }

        #endregion Private Methods
    }

    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// This class provides a Dispatcher on a shared thread for use in real time inking.  Each
    /// instance of this class must call Dispose() in order to have this thread shut
    /// down properly.
    /// </summary>
    internal class DynamicRendererThreadManager : IWeakEventListener, IDisposable
    {
        [ThreadStatic]
        private static WeakReference _tsDRTMWeakRef;

        internal static DynamicRendererThreadManager GetCurrentThreadInstance()
        {
            // Create the threadstatic DynamicRendererThreadManager as needed for calling thread.
            // It only creates one
            if (_tsDRTMWeakRef == null || _tsDRTMWeakRef.Target == null)
            {
                _tsDRTMWeakRef = new WeakReference(new DynamicRendererThreadManager());
            }
            return _tsDRTMWeakRef.Target as DynamicRendererThreadManager;
        }

        private volatile Dispatcher __inkingDispatcher; // Can be accessed from multiple threads.
        private bool _disposed;

        /// <summary>
        /// Private contructor called by static method so that we can only ever create one of these per thread!
        /// </summary>
        private DynamicRendererThreadManager()
        {
            // Create the thread
            DynamicRendererThreadManagerWorker worker = new DynamicRendererThreadManagerWorker();
            __inkingDispatcher = worker.StartUpAndReturnDispatcher();

            // 
            // Add a weak listener to the application dispatcher's ShutdownStarted event. So we can
            // shut down our dynamic rendering thread gracefully when the app dispatcher is being shut down.
            DispatcherShutdownStartedEventManager.AddListener(Dispatcher.CurrentDispatcher, this);

            Debug.Assert(__inkingDispatcher != null); // We should have a valid ref here
        }

        // Finalizer - clean up thread
        ~DynamicRendererThreadManager()
        {
            Dispose(false);
        }

        /// <summary>
        /// IDisposable.Dispose implementation.
        /// </summary>
        public void Dispose()
        {
            // NOTE: This object is internal to the DynamicRenderer and you really shouldn't call this.
            //   It's was added to be consistent with .NET design guidelines.
            //   Just let the finalizer do any required clean up work!
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs args)
        {
            //NOTE: if DispatcherShutdownStartedEventManager is updated to listen
            //to anything besides ShutdownStarted, you should disambiguate the event here
            if (managerType == typeof(DispatcherShutdownStartedEventManager))
            {
                OnAppDispatcherShutdown(sender, args);
            }
            else
            {
                return false;       // unrecognized event
            }

            return true;
        }


        /// <summary>
        /// The app dispatcher Shutdown Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAppDispatcherShutdown(object sender, EventArgs e)
        {
            Dispatcher inkingDispatcher = __inkingDispatcher;
            // Return from here if the inking dispathcer is gone already.
            if (inkingDispatcher == null)
                return;

            // Mashal the Dispose call, which will shut down our Dynamic rendering thread, to the inking dispatcher.
            inkingDispatcher.Invoke(DispatcherPriority.Send,
                (DispatcherOperationCallback)delegate(object unused)
                {
                    Dispose();
                    return null;
                },
                null);
        }

        /// <summary>
        /// Handles disposing of internal object data.
        /// </summary>
        /// <param name="disposing">true when freeing managed and unmanaged resources; false if freeing just unmanaged resources.</param>
        void Dispose(bool disposing)
        {
            if(!_disposed)
            {
                _disposed = true;

                // Free up the thread
                if (__inkingDispatcher != null && !Environment.HasShutdownStarted)
                {
                    try
                    {
                        __inkingDispatcher.CriticalInvokeShutdown();
                    }
                    catch(System.ComponentModel.Win32Exception e)
                    {
                        if (e.NativeErrorCode != 1400) // ERROR_INVALID_WINDOW_HANDLE
                        {
                            // This is an unlocalized string but it only prints on the Debug Console
                            Debug.WriteLine(String.Format("Dispatcher.CriticalInvokeShutdown() Failed.  Error={0}", e.NativeErrorCode));
                        }
                    }
                    finally
                    {
                        __inkingDispatcher = null;
                    }
                }
            }
            GC.KeepAlive(this);
        }


        /// <summary>
        /// Gets the inking thread dispatcher.
        /// </summary>
        internal Dispatcher ThreadDispatcher
        {
            get
            {
                return __inkingDispatcher;
            }
        }


        // Helper class to manager the thread startup and thread proc.  Needed in order to allow
        // the DynamicRendererThreadManager to get garbage collected.
        private class DynamicRendererThreadManagerWorker
        {
            private Dispatcher _dispatcher;
            private AutoResetEvent _startupCompleted;

            /// <summary>
            /// Constructor.
            /// </summary>
            internal DynamicRendererThreadManagerWorker()
            {
            }

            internal Dispatcher StartUpAndReturnDispatcher()
            {
                _startupCompleted = new AutoResetEvent(false);
                Thread inkingThread = new Thread(new ThreadStart(InkingThreadProc));
                inkingThread.SetApartmentState(ApartmentState.STA);
                inkingThread.IsBackground = true; // Don't keep process alive if this thread still running.
                inkingThread.Start();

                _startupCompleted.WaitOne();
                _startupCompleted.Close();
                _startupCompleted = null;

                return _dispatcher;
            }

            public void InkingThreadProc()
            {
                Thread.CurrentThread.Name = "DynamicRenderer";
                // Now make sure we create the dispatcher.
                _dispatcher = Dispatcher.CurrentDispatcher;
                // Now we can signal that everything is set up.
                _startupCompleted.Set();
                // Now start the dispatcher message loop for this thread.
                Dispatcher.Run();
            }
        }
}
}

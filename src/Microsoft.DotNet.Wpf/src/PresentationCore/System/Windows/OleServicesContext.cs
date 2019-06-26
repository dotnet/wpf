// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: Ole Services for DragDrop and Clipboard.
//
//

using MS.Win32;
using MS.Internal;
using System.Security;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Input;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace System.Windows
{
    //------------------------------------------------------
    //
    //  OleServicesContext class
    //
    //------------------------------------------------------

    /// <summary>
    /// This class manages Ole services for DragDrop and Clipboard.
    /// The instance of OleServicesContext class is created per Thread...Dispatcher.
    /// </summary>
    /// <remarks>
    /// </remarks>
    // threading issues
    //  - This class needs to be modified to marshal calls over to the Dispatcher/STA thread.
    //  - Once we have an event we can listen to when a Dispatcher
    //    shuts down, we should use that.  We currently listen to Dispatcher shutdown, which has no thread
    //    affinity -- it could happen on any thread, which breaks us.
    internal class OleServicesContext
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Instantiates a OleServicesContext.
        /// </summary>
        private OleServicesContext()
        {
            // We need to get the Dispatcher Thread in order to get OLE DragDrop and Clipboard services that
            // require STA.
            SetDispatcherThread();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Get the ole services context associated with the current Thread.
        /// </summary>
        internal static OleServicesContext CurrentOleServicesContext
        {
            get
            {
                OleServicesContext oleServicesContext;

                // Get the ole services context from the Thread data slot.
                oleServicesContext = (OleServicesContext)Thread.GetData(OleServicesContext._threadDataSlot);

                if (oleServicesContext == null)
                {
                    // Create OleSErvicesContext instance.
                    oleServicesContext = new OleServicesContext();

                    // Save the ole services context into the UIContext data slot.
                    Thread.SetData(OleServicesContext._threadDataSlot, oleServicesContext);
                }

                return oleServicesContext;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// OleSetClipboard - Call OLE Interopo OleSetClipboard()
        /// </summary>
        internal int OleSetClipboard(IComDataObject dataObject)
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException(SR.Get(SRID.OleServicesContext_ThreadMustBeSTA));
            }

            return UnsafeNativeMethods.OleSetClipboard(dataObject);
        }

        /// <summary>
        /// OleGetClipboard - Call OLE Interop OleGetClipboard()
        /// </summary>
        internal int OleGetClipboard(ref IComDataObject dataObject)
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException(SR.Get(SRID.OleServicesContext_ThreadMustBeSTA));
            }

            return UnsafeNativeMethods.OleGetClipboard(ref dataObject);
        }

        /// <summary>
        /// OleFlushClipboard - Call OLE Interop OleFlushClipboard()
        /// </summary>
        internal int OleFlushClipboard()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException(SR.Get(SRID.OleServicesContext_ThreadMustBeSTA));
            }

            return UnsafeNativeMethods.OleFlushClipboard();
        }

        /// <summary>
        /// OleIsCurrentClipboard - OleIsCurrentClipboard only works for the data object 
        /// used in the OleSetClipboard. This means that it can’t be called by the consumer 
        /// of the data object to determine if the object that was on the clipboard at the 
        /// previous OleGetClipboard call is still on the Clipboard.
        /// </summary>
        internal int OleIsCurrentClipboard(IComDataObject dataObject)
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException(SR.Get(SRID.OleServicesContext_ThreadMustBeSTA));
            }

            return UnsafeNativeMethods.OleIsCurrentClipboard(dataObject);
        }

        /// <summary>
        /// OleDoDragDrop - Call OLE Interop DoDragDrop()
        /// Initiate OLE DragDrop
        /// </summary>
        internal void OleDoDragDrop(IComDataObject dataObject, UnsafeNativeMethods.IOleDropSource dropSource, int allowedEffects, int[] finalEffect)
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException(SR.Get(SRID.OleServicesContext_ThreadMustBeSTA));
            }

            InputManager inputManager = (InputManager)Dispatcher.CurrentDispatcher.InputManager;
            if (inputManager != null)
            {
                inputManager.InDragDrop = true;
            }
            try
            {
                UnsafeNativeMethods.DoDragDrop(dataObject, dropSource, allowedEffects, finalEffect);
            }
            finally
            {
                if (inputManager != null)
                {
                    inputManager.InDragDrop = false;
                }
            }
        }

        /// <summary>
        /// OleRegisterDragDrop - Call OLE Interop RegisterDragDrop()
        /// </summary>
        internal int OleRegisterDragDrop(HandleRef windowHandle, UnsafeNativeMethods.IOleDropTarget dropTarget)
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException(SR.Get(SRID.OleServicesContext_ThreadMustBeSTA));
            }

            return UnsafeNativeMethods.RegisterDragDrop(windowHandle, dropTarget);
        }

        /// <summary>
        /// OleRevokeDragDrop - Call OLE Interop RevokeDragDrop()
        /// </summary>
        internal int OleRevokeDragDrop(HandleRef windowHandle)
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException(SR.Get(SRID.OleServicesContext_ThreadMustBeSTA));
            }

            return UnsafeNativeMethods.RevokeDragDrop(windowHandle);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// SetDispatcherThread - Initialize OleServicesContext that will call Ole initialize for ole services(DragDrop and Clipboard)
        /// and add the disposed event handler of Dispatcher to clean up resources and uninitalize Ole.
        /// </summary>
        private void SetDispatcherThread()
        {
            int hr;

            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException(SR.Get(SRID.OleServicesContext_ThreadMustBeSTA));
            }

            // Initialize Ole services.
            // Balanced with OleUninitialize call in OnDispatcherShutdown.
            hr = OleInitialize();

            if (!NativeMethods.Succeeded(hr))
            {
                throw new SystemException(SR.Get(SRID.OleServicesContext_oleInitializeFailure, hr));
            }

            // Add Dispatcher.Shutdown event handler. 
            // We will call ole Uninitialize and clean up the resource when UIContext is terminated.
            Dispatcher.CurrentDispatcher.ShutdownFinished += new EventHandler(OnDispatcherShutdown);
        }

        /// <summary>
        /// This is a callback when Dispatcher is shut down.
        /// </summary>
        /// <remarks>
        /// This method must be called before shutting down the application
        /// on the dispatcher thread.  It must be called by the same
        /// thread running the dispatcher and the thread must have its
        /// ApartmentState property set to ApartmentState.STA.
        /// </remarks>
        private void OnDispatcherShutdown(object sender, EventArgs args)
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException(SR.Get(SRID.OleServicesContext_ThreadMustBeSTA));
            }

            // Uninitialize Ole services.
            // Balanced with OleInitialize call in SetDispatcherThread.
            OleUninitialize();
        }

        // Wrapper for UnsafeNativeMethods.OleInitialize, useful for debugging.
        private int OleInitialize()
        {
#if DEBUG
            _debugOleInitializeRefCount++;
#endif // DEBUG
            return UnsafeNativeMethods.OleInitialize();
        }

        // Wrapper for UnsafeNativeMethods.OleUninitialize, useful for debugging.
        private int OleUninitialize()
        {
            int hr;

            hr = UnsafeNativeMethods.OleUninitialize();
#if DEBUG
            _debugOleInitializeRefCount--;
            Invariant.Assert(_debugOleInitializeRefCount >= 0, "Unbalanced call to OleUnitialize!");
#endif // DEBUG

            return hr;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // This is a slot to store OleServicesContext class per thread.
        private static readonly LocalDataStoreSlot _threadDataSlot = Thread.AllocateDataSlot();

#if DEBUG
        // Ref count of calls to OleInitialize/OleUnitialize.
        private int _debugOleInitializeRefCount;
#endif // DEBUG

        #endregion Private Fields
    }
}


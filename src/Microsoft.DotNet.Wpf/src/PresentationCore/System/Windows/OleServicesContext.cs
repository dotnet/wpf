// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Ole Services for DragDrop and Clipboard.

using MS.Win32;
using MS.Internal;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Input;

using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace System.Windows;

/// <summary>
///  This class manages Ole services for DragDrop and Clipboard.
///  The instance of OleServicesContext class is created per Thread...Dispatcher.
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
    // This is a slot to store OleServicesContext class per thread.
    private static readonly LocalDataStoreSlot s_threadDataSlot = Thread.AllocateDataSlot();

#if DEBUG
    // Ref count of calls to OleInitialize/OleUnitialize.
    private int _debugOleInitializeRefCount;
#endif // DEBUG

    /// <summary>
    /// Instantiates a OleServicesContext.
    /// </summary>
    private OleServicesContext()
    {
        // We need to get the Dispatcher Thread in order to get OLE DragDrop and Clipboard services that
        // require STA.
        SetDispatcherThread();
    }

    /// <summary>
    ///  Get the ole services context associated with the current Thread.
    /// </summary>
    internal static OleServicesContext CurrentOleServicesContext
    {
        get
        {
            // Get the ole services context from the Thread data slot.
            OleServicesContext oleServicesContext = (OleServicesContext)Thread.GetData(s_threadDataSlot);

            if (oleServicesContext is null)
            {
                // Create OleSErvicesContext instance.
                oleServicesContext = new OleServicesContext();

                // Save the ole services context into the UIContext data slot.
                Thread.SetData(s_threadDataSlot, oleServicesContext);
            }

            return oleServicesContext;
        }
    }

    internal static void EnsureThreadState()
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadStateException(SR.OleServicesContext_ThreadMustBeSTA);
        }
    }

    /// <summary>
    ///  Call OLE Interopo OleSetClipboard()
    /// </summary>
    internal int OleSetClipboard(IComDataObject dataObject)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadStateException(SR.OleServicesContext_ThreadMustBeSTA);
        }

        return UnsafeNativeMethods.OleSetClipboard(dataObject);
    }

    /// <summary>
    ///  Call OLE Interop OleGetClipboard()
    /// </summary>
    internal int OleGetClipboard(ref IComDataObject dataObject)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadStateException(SR.OleServicesContext_ThreadMustBeSTA);
        }

        return UnsafeNativeMethods.OleGetClipboard(ref dataObject);
    }

    /// <summary>
    ///  Call OLE Interop OleFlushClipboard()
    /// </summary>
    internal int OleFlushClipboard()
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadStateException(SR.OleServicesContext_ThreadMustBeSTA);
        }

        return UnsafeNativeMethods.OleFlushClipboard();
    }

    /// <summary>
    ///  OleIsCurrentClipboard only works for the data object used in the OleSetClipboard. This means that it can’t be
    ///  called by the consumer of the data object to determine if the object that was on the clipboard at the previous
    ///  OleGetClipboard call is still on the Clipboard.
    /// </summary>
    internal int OleIsCurrentClipboard(IComDataObject dataObject)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadStateException(SR.OleServicesContext_ThreadMustBeSTA);
        }

        return UnsafeNativeMethods.OleIsCurrentClipboard(dataObject);
    }

    /// <summary>
    ///  Call OLE Interop DoDragDrop().  Initiate OLE DragDrop.
    /// </summary>
    internal void OleDoDragDrop(
        IComDataObject dataObject,
        UnsafeNativeMethods.IOleDropSource dropSource,
        int allowedEffects,
        int[] finalEffect)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadStateException(SR.OleServicesContext_ThreadMustBeSTA);
        }

        InputManager inputManager = (InputManager)Dispatcher.CurrentDispatcher.InputManager;
        if (inputManager is not null)
        {
            inputManager.InDragDrop = true;
        }
        try
        {
            UnsafeNativeMethods.DoDragDrop(dataObject, dropSource, allowedEffects, finalEffect);
        }
        finally
        {
            if (inputManager is not null)
            {
                inputManager.InDragDrop = false;
            }
        }
    }

    /// <summary>
    ///  Call OLE Interop RegisterDragDrop()
    /// </summary>
    internal int OleRegisterDragDrop(HandleRef windowHandle, UnsafeNativeMethods.IOleDropTarget dropTarget)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadStateException(SR.OleServicesContext_ThreadMustBeSTA);
        }

        return UnsafeNativeMethods.RegisterDragDrop(windowHandle, dropTarget);
    }

    /// <summary>
    ///  Call OLE Interop RevokeDragDrop()
    /// </summary>
    internal int OleRevokeDragDrop(HandleRef windowHandle)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadStateException(SR.OleServicesContext_ThreadMustBeSTA);
        }

        return UnsafeNativeMethods.RevokeDragDrop(windowHandle);
    }

    /// <summary>
    ///  Initialize OleServicesContext that will call Ole initialize for ole services(DragDrop and Clipboard)
    ///  and add the disposed event handler of Dispatcher to clean up resources and uninitalize Ole.
    /// </summary>
    private void SetDispatcherThread()
    {
        int hr;

        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadStateException(SR.OleServicesContext_ThreadMustBeSTA);
        }

        // Initialize Ole services.
        // Balanced with OleUninitialize call in OnDispatcherShutdown.
        hr = OleInitialize();

        if (!NativeMethods.Succeeded(hr))
        {
            throw new SystemException(SR.Format(SR.OleServicesContext_oleInitializeFailure, hr));
        }

        // Add Dispatcher.Shutdown event handler. 
        // We will call ole Uninitialize and clean up the resource when UIContext is terminated.
        Dispatcher.CurrentDispatcher.ShutdownFinished += new EventHandler(OnDispatcherShutdown);
    }

    /// <summary>
    ///  This is a callback when the <see cref="Dispatcher"/> is shut down.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   This method must be called before shutting down the application on the dispatcher thread. It must be called
    ///   by the same thread running the dispatcher and the thread must have its ApartmentState property set to
    ///   <see cref="ApartmentState.STA"/>.
    ///  </para>
    /// </remarks>
    private void OnDispatcherShutdown(object sender, EventArgs args)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadStateException(SR.OleServicesContext_ThreadMustBeSTA);
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
        int hr = UnsafeNativeMethods.OleUninitialize();
#if DEBUG
        _debugOleInitializeRefCount--;
        Invariant.Assert(_debugOleInitializeRefCount >= 0, "Unbalanced call to OleUnitialize!");
#endif // DEBUG

        return hr;
    }
}

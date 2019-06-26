// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define TRACEPTW

using System;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using System.Threading;
using System.Security;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MS.Win32.Penimc;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    internal sealed class PenThreadWorker
    {
         /// <summary>List of constants for PenImc</summary>
        const int PenEventNone           = 0;
        const int PenEventTimeout       = 1;
        const int PenEventPenInRange    = 707;
        const int PenEventPenOutOfRange = 708;
        const int PenEventPenDown       = 709;
        const int PenEventPenUp         = 710;
        const int PenEventPackets       = 711;
        const int PenEventSystem        = 714;
        
        const int MaxContextPerThread  = 31;  // (64 - 1) / 2 = 31.  Max handle limit for MsgWaitForMultipleMessageEx()
        const int EventsFrequency       = 8;

        IntPtr []             _handles = new IntPtr[0];

        WeakReference []      _penContexts = new WeakReference[0];

        IPimcContext3 []       _pimcContexts = new IPimcContext3[0];

        /// <summary>
        /// A list of all WISP context COM object GIT keys that are locked via this thread.
        /// </summary>
        UInt32[] _wispContextKeys = new UInt32[0];

        private SecurityCriticalData<IntPtr>   _pimcResetHandle;
        private volatile bool                  __disposed;
        private List <WorkerOperation>         _workerOperation = new List<WorkerOperation>();
        private object                         _workerOperationLock = new Object();

        // For caching move events.
        
        private PenContext                      _cachedMovePenContext;
        
        private int                             _cachedMoveStylusPointerId;
        private int                             _cachedMoveStartTimestamp;
        private int []                          _cachedMoveData;


        /////////////////////////////////////////////////////////////////////
        //
        // Here's a bunch of helper classes to manage marshalling the calls
        // over to the worker thread to be executed synchronously.
        //
        /////////////////////////////////////////////////////////////////////

        // Base class for all worker operations
        private abstract class WorkerOperation
        {
            AutoResetEvent  _doneEvent;

            internal WorkerOperation()
            {
                _doneEvent = new AutoResetEvent(false);
            }

            /// <summary>
            /// Critical - Calls SecurityCritical code OnDoWork which is differred based on the various derived class.
            ///             Called by PenThreadWorker.ThreadProc().
            /// </summary>
            internal void DoWork()
            {
                try
                {
                    OnDoWork();
                }
                finally
                {
                    _doneEvent.Set();
                }
}

            /// <summary>
            /// Critical - Calls SecurityCritical code OnDoWork which is differred based on the various derived class.
            ///             Called by WorkerOperation.DoWork().
            /// </summary>
            protected abstract void OnDoWork();

            internal AutoResetEvent DoneEvent
            {
                get { return _doneEvent;}
            }
        }


        // Class that handles getting the current rect for a tablet device.
        private class WorkerOperationThreadStart : WorkerOperation
        {
            /////////////////////////////////////////////////////////////////////////
            /// <summary>
            ///     Used to signal when the thread has started up.
            /// </summary>
            protected override void OnDoWork()
            {
                // We don't need to do anything.  Just have event signal we've executed.
            }
        }


        // Class that handles getting the tablet device info for all tablets on the system.
        private class WorkerOperationGetTabletsInfo : WorkerOperation
        {
            internal TabletDeviceInfo[] TabletDevicesInfo
            {
                get { return _tabletDevicesInfo;}
            }

            /////////////////////////////////////////////////////////////////////////
            /// <summary>
            ///     Returns the list of TabletDeviceInfo structs that contain information
            ///     about all of the TabletDevices on the system.
            /// </summary>
            protected override void OnDoWork()
            {
                try
                {
                    // create new collection of tablets
                    MS.Win32.Penimc.IPimcManager3 pimcManager = MS.Win32.Penimc.UnsafeNativeMethods.PimcManager;
                    uint cTablets;
                    pimcManager.GetTabletCount(out cTablets);

                    TabletDeviceInfo[] tablets = new TabletDeviceInfo[cTablets];

                    for ( uint iTablet = 0; iTablet < cTablets; iTablet++ )
                    {
                        MS.Win32.Penimc.IPimcTablet3 pimcTablet;
                        pimcManager.GetTablet(iTablet, out pimcTablet);

                        tablets[iTablet] = PenThreadWorker.GetTabletInfoHelper(pimcTablet);
                    }

                    // Set result data and signal we are done.
                    _tabletDevicesInfo = tablets;
                }
                catch (Exception e) when (PenThreadWorker.IsKnownException(e))
                {
                    Debug.WriteLine("WorkerOperationGetTabletsInfo.OnDoWork failed due to: {0}{1}", Environment.NewLine, e.ToString());
                }
            }

            TabletDeviceInfo[] _tabletDevicesInfo = new TabletDeviceInfo[0];
        }

        // Class that handles creating a context for a particular tablet device.        
        private class WorkerOperationCreateContext : WorkerOperation
        {
            internal WorkerOperationCreateContext(IntPtr hwnd, IPimcTablet3 pimcTablet)
            {
                _hwnd = hwnd;
                _pimcTablet = pimcTablet;
            }

            internal PenContextInfo Result
            {
                get { return _result;}
            }

            /////////////////////////////////////////////////////////////////////////
            /// <summary>
            ///     Creates a new context for this a window and given tablet device and
            ///     returns a new PenContext in the workOperation class.
            /// </summary>
            protected override void OnDoWork()
            {
                IPimcContext3 pimcContext;
                int id;
                Int64 commHandle;

                try
                {
                    _pimcTablet.CreateContext(_hwnd, true, 250, out pimcContext, out id, out commHandle);
                    // Set result data and signal we are done.
                    PenContextInfo result;
                    result.ContextId = id;
                    result.PimcContext = new SecurityCriticalDataClass<IPimcContext3>(pimcContext);

                    // commHandle cannot be a IntPtr by itself because its native counterpart cannot be a
                    // INT_PTR. The reason being that INT_PTR (__int3264) always gets marshalled as a
                    // 32 bit value, which means in a 64 bit process we would lose the first half of the pointer.
                    // Instead with this we always get a 64 bit value and then instantiate the IntPtr appropriately
                    // so that nothing gets lost during marshalling. The cast from Int64 to Int32 below
                    // should be lossless cast because both COM server and client are expected
                    // to be of same bitness (they are in the same process).
                    result.CommHandle = new SecurityCriticalDataClass<IntPtr>((IntPtr.Size == 4 ? new IntPtr((int)commHandle) : new IntPtr(commHandle)));

                    result.WispContextKey = MS.Win32.Penimc.UnsafeNativeMethods.QueryWispContextKey(pimcContext);

                    _result = result;
                }
                catch (Exception e) when (PenThreadWorker.IsKnownException(e))
                {
                    // result will not be initialized if we fail due to a COM exception.
                    Debug.WriteLine("WorkerOperationCreateContext.OnDoWork failed due to a {0}{1}", Environment.NewLine, e.ToString());
                }
            }

            IntPtr _hwnd;
            IPimcTablet3 _pimcTablet;
            PenContextInfo _result = new PenContextInfo();
        }

        /// <summary>
        /// Class to handle acquiring WISP/PenIMC tablet locks on the PenThread.
        /// </summary>
        private class WorkerOperationAcquireTabletLocks : WorkerOperation
        {
            internal WorkerOperationAcquireTabletLocks(IPimcTablet3 tablet, UInt32 wispTabletKey)
            {
                _tablet = tablet;
                _wispTabletKey = wispTabletKey;
            }

            internal bool Result { get; private set; }

            /// <summary>
            /// Releases the lock on the PenThread.
            /// </summary>
            protected override void OnDoWork()
            {
                MS.Win32.Penimc.UnsafeNativeMethods.AcquireTabletExternalLock(_tablet);
                MS.Win32.Penimc.UnsafeNativeMethods.CheckedLockWispObjectFromGit(_wispTabletKey);
                Result = true;
            }

            /// <summary>
            /// The PenIMC tablet
            /// </summary>
            IPimcTablet3 _tablet;

            /// <summary>
            /// The GIT key for the WISP COM object.
            /// </summary>
            UInt32 _wispTabletKey;
        }

        /// <summary>
        /// Class to handle releasing WISP/PenIMC tablet locks on the PenThread.
        /// </summary>
        private class WorkerOperationReleaseTabletLocks : WorkerOperation
        {
            internal WorkerOperationReleaseTabletLocks(IPimcTablet3 tablet, UInt32 wispTabletKey)
            {
                _tablet = tablet;
                _wispTabletKey = wispTabletKey;
            }

            internal bool Result { get; private set; }

            /// <summary>
            /// Releases the lock on the PenThread.
            /// </summary>
            protected override void OnDoWork()
            {
                MS.Win32.Penimc.UnsafeNativeMethods.CheckedUnlockWispObjectFromGit(_wispTabletKey);
                MS.Win32.Penimc.UnsafeNativeMethods.ReleaseTabletExternalLock(_tablet);
                Result = true;
            }

            /// <summary>
            /// The PenIMC tablet
            /// </summary>
            IPimcTablet3 _tablet;
            
            /// <summary>
            /// The GIT key for the WISP COM object.
            /// </summary>
            UInt32 _wispTabletKey;
        }

        // Class that handles refreshing the cursor devices for a particular tablet device.        
        private class WorkerOperationRefreshCursorInfo : WorkerOperation
        {
            internal WorkerOperationRefreshCursorInfo(IPimcTablet3 pimcTablet)
            {
                _pimcTablet = pimcTablet;
            }

            internal StylusDeviceInfo[] StylusDevicesInfo
            {
                get
                {
                    return _stylusDevicesInfo;
                }
            }

            /////////////////////////////////////////////////////////////////////////
            /// <summary>
            ///     Causes the stylus devices info (cursors) in penimc to be refreshed 
            ///     for the passed in IPimcTablet3. 
            /// </summary>
            protected override void OnDoWork()
            {
                try
                {
                    _pimcTablet.RefreshCursorInfo();
                    _stylusDevicesInfo = PenThreadWorker.GetStylusDevicesInfo(_pimcTablet);
                }
                catch (Exception e) when (PenThreadWorker.IsKnownException(e))
                {
                    Debug.WriteLine("WorkerOperationRefreshCursorInfo.OnDoWork failed due to a {0}{1}", Environment.NewLine, e.ToString());
                }
            }

            IPimcTablet3 _pimcTablet;

            StylusDeviceInfo[]  _stylusDevicesInfo = new StylusDeviceInfo[0];
        }

        // Class that handles getting info about a specific tablet device.
        private class WorkerOperationGetTabletInfo : WorkerOperation
        {
            internal WorkerOperationGetTabletInfo(uint index)
            {
                _index = index;
            }

            internal TabletDeviceInfo TabletDeviceInfo
            {
                get { return _tabletDeviceInfo;}
            }

            /////////////////////////////////////////////////////////////////////////
            /// <summary>
            ///     Fills in a struct containing the list of TabletDevice properties for
            ///     a given tablet device index.
            /// </summary>
            protected override void OnDoWork()
            {
                try
                {
                    // create new collection of tablets
                    MS.Win32.Penimc.IPimcManager3 pimcManager = MS.Win32.Penimc.UnsafeNativeMethods.PimcManager;
                    MS.Win32.Penimc.IPimcTablet3 pimcTablet;
                    pimcManager.GetTablet(_index, out pimcTablet);

                    // Set result data and signal we are done.
                    _tabletDeviceInfo = PenThreadWorker.GetTabletInfoHelper(pimcTablet);
                }
                catch (Exception e) when (PenThreadWorker.IsKnownException(e))
                {
                    // result will not be initialized if we fail due to a COM exception.
                    Debug.WriteLine("WorkerOperationGetTabletInfo.OnDoWork failed due to {0}{1}", Environment.NewLine, e.ToString());
                }
            }

            uint             _index;
            TabletDeviceInfo _tabletDeviceInfo = new TabletDeviceInfo();
        }
        
        // Class that handles getting the current rect for a tablet device.
        private class WorkerOperationWorkerGetUpdatedSizes : WorkerOperation
        {
            internal WorkerOperationWorkerGetUpdatedSizes(IPimcTablet3 pimcTablet)
            {
                _pimcTablet = pimcTablet;
            }

            internal TabletDeviceSizeInfo TabletDeviceSizeInfo
            {
                get { return _tabletDeviceSizeInfo;}
            }


            /////////////////////////////////////////////////////////////////////////
            /// <summary>
            ///     Gets the current rectangle for a tablet device and returns in workOperation class.
            /// </summary>
            protected override void OnDoWork()
            {
                try
                {
                    int displayWidth, displayHeight, tabletWidth, tabletHeight;
                    _pimcTablet.GetTabletAndDisplaySize(out tabletWidth, out tabletHeight, out displayWidth, out displayHeight);

                    // Set result data and signal we are done.
                    _tabletDeviceSizeInfo = new TabletDeviceSizeInfo(
                                        new Size( tabletWidth, tabletHeight), 
                                        new Size( displayWidth, displayHeight));
                }
                catch (Exception e) when (PenThreadWorker.IsKnownException(e))
                {
                    Debug.WriteLine("WorkerOperationWorkerGetUpdatedSizes.OnDoWork failed due to a {0}{1}", Environment.NewLine, e.ToString());
                }
            }

            IPimcTablet3          _pimcTablet;
            TabletDeviceSizeInfo _tabletDeviceSizeInfo = new TabletDeviceSizeInfo(new Size( 1, 1), new Size( 1, 1));
        }


        // Class that handles getting the current rect for a tablet device.
        private class WorkerOperationAddContext : WorkerOperation
        {
            internal WorkerOperationAddContext(PenContext penContext, PenThreadWorker penThreadWorker)
            {
                _newPenContext = penContext;
                _penThreadWorker = penThreadWorker;
            }

            internal bool Result
            {
                get { return _result;}
            }

            /////////////////////////////////////////////////////////////////////////
            /// <summary>
            ///     Adds a PenContext to the list of contexts that events can be received
            ///     from and returns whether it was successful in workOperation class.
            /// </summary>
            protected override void OnDoWork()
            {
                _result = _penThreadWorker.AddPenContext(_newPenContext);
            }
                    
            PenContext      _newPenContext;
            PenThreadWorker _penThreadWorker;

            bool _result;
        }

        // Class that handles getting the current rect for a tablet device.
        private class WorkerOperationRemoveContext : WorkerOperation
        {
            internal WorkerOperationRemoveContext(PenContext penContext, PenThreadWorker penThreadWorker)
            {
                _penContextToRemove = penContext;
                _penThreadWorker = penThreadWorker;
            }

            internal bool Result
            {
                get { return _result;}
            }

            /////////////////////////////////////////////////////////////////////////
            /// <summary>
            ///     Adds a PenContext to the list of contexts that events can be received
            ///     from and returns whether it was successful in workOperation class.
            /// </summary>
            protected override void OnDoWork()
            {
                _result = _penThreadWorker.RemovePenContext(_penContextToRemove);
            }
                    
            PenContext  _penContextToRemove;
            PenThreadWorker _penThreadWorker;

            bool _result;
        }


        /////////////////////////////////////////////////////////////////////

        internal PenThreadWorker()
        {
            IntPtr resetHandle;
            // Consider: We could use a AutoResetEvent handle instead and avoid the penimc.dll call.
            MS.Win32.Penimc.UnsafeNativeMethods.CreateResetEvent(out resetHandle);
            _pimcResetHandle = new SecurityCriticalData<IntPtr>(resetHandle);

            WorkerOperationThreadStart started = new WorkerOperationThreadStart();
            lock(_workerOperationLock)
            {
                _workerOperation.Add((WorkerOperation)started);
            }

            Thread thread = new Thread(new ThreadStart(ThreadProc));
            thread.IsBackground = true; // don't hold process open due to this thread.
            thread.Start();
            
            // Wait for this work to be completed (ie thread is started up).
            started.DoneEvent.WaitOne();
            started.DoneEvent.Close();
        }

        internal void Dispose()
        {
            if(!__disposed)
            {
                __disposed = true;
                
                // Kick thread to wake up and see we are disposed.
                MS.Win32.Penimc.UnsafeNativeMethods.RaiseResetEvent(_pimcResetHandle.Value);
                // Let it destroy the reset event.
            }
            GC.KeepAlive(this);
        }

        /////////////////////////////////////////////////////////////////////

        internal bool WorkerAddPenContext(PenContext penContext)
        {
            if (__disposed)
            {
                throw new ObjectDisposedException(null, SR.Get(SRID.Penservice_Disposed));
            }

            Debug.Assert(penContext != null);
            
            WorkerOperationAddContext addContextOperation = new WorkerOperationAddContext(penContext, this);

            lock(_workerOperationLock)
            {
                _workerOperation.Add(addContextOperation);
            }

            // Kick thread to do this work.
            MS.Win32.Penimc.UnsafeNativeMethods.RaiseResetEvent(_pimcResetHandle.Value);

            // Wait for this work to be completed.
            addContextOperation.DoneEvent.WaitOne();
            addContextOperation.DoneEvent.Close();

            return addContextOperation.Result;
        }


        internal bool WorkerRemovePenContext(PenContext penContext)
        {
            if (__disposed)
            {
                return true;
            }

            Debug.Assert(penContext != null);
            
            WorkerOperationRemoveContext removeContextOperation = new WorkerOperationRemoveContext(penContext, this);

            lock(_workerOperationLock)
            {
                _workerOperation.Add(removeContextOperation);
            }

            // Kick thread to do this work.
            MS.Win32.Penimc.UnsafeNativeMethods.RaiseResetEvent(_pimcResetHandle.Value);

            // Wait for this work to be completed.
            removeContextOperation.DoneEvent.WaitOne();
            removeContextOperation.DoneEvent.Close();

            return removeContextOperation.Result;
        }

        /////////////////////////////////////////////////////////////////////

        internal TabletDeviceInfo[] WorkerGetTabletsInfo()
        {
            // Set data up for this call
            WorkerOperationGetTabletsInfo getTablets = new WorkerOperationGetTabletsInfo();
            
            lock(_workerOperationLock)
            {
                _workerOperation.Add(getTablets);
            }

            // Kick thread to do this work.
            MS.Win32.Penimc.UnsafeNativeMethods.RaiseResetEvent(_pimcResetHandle.Value);

            // Wait for this work to be completed.
            getTablets.DoneEvent.WaitOne();
            getTablets.DoneEvent.Close();
        
            return getTablets.TabletDevicesInfo;
        }


        internal PenContextInfo WorkerCreateContext(IntPtr hwnd, IPimcTablet3 pimcTablet)
        {
            WorkerOperationCreateContext createContextOperation = new WorkerOperationCreateContext(
                                                                    hwnd,
                                                                    pimcTablet);
            lock(_workerOperationLock)
            {
                _workerOperation.Add(createContextOperation);
            }

            // Kick thread to do this work.
            MS.Win32.Penimc.UnsafeNativeMethods.RaiseResetEvent(_pimcResetHandle.Value);

            // Wait for this work to be completed.
            createContextOperation.DoneEvent.WaitOne();
            createContextOperation.DoneEvent.Close();

            return createContextOperation.Result;
        }

        /// <summary>
        /// Instantiates a worker to acquire a WISP/PenIMC tablet object's lock on the PenThread and waits on the operation.
        /// </summary>
        /// <param name="gitKey">The GIT key for the WISP COM object.</param>
        /// <returns>True if successful, false otherwise.</returns>
        internal bool WorkerAcquireTabletLocks(IPimcTablet3 tablet, UInt32 wispTabletKey)
        {
            WorkerOperationAcquireTabletLocks acquireOperation =
                new WorkerOperationAcquireTabletLocks(tablet, wispTabletKey);

            lock (_workerOperationLock)
            {
                _workerOperation.Add(acquireOperation);
            }

            // Kick thread to do this work.
            MS.Win32.Penimc.UnsafeNativeMethods.RaiseResetEvent(_pimcResetHandle.Value);

            // Wait for this work to be completed.
            acquireOperation.DoneEvent.WaitOne();
            acquireOperation.DoneEvent.Close();

            return acquireOperation.Result;
        }

        /// <summary>
        /// Instantiates a worker to release a WISP/PenIMC tablet object's lock on the PenThread and waits on the operation.
        /// </summary>
        /// <param name="gitKey">The GIT key for the WISP COM object.</param>
        /// <returns>True if successful, false otherwise.</returns>
        internal bool WorkerReleaseTabletLocks(IPimcTablet3 tablet, UInt32 wispTabletKey)
        {
            WorkerOperationReleaseTabletLocks releaseOperation = 
                new WorkerOperationReleaseTabletLocks(tablet, wispTabletKey);

            lock (_workerOperationLock)
            {
                _workerOperation.Add(releaseOperation);
            }

            // Kick thread to do this work.
            MS.Win32.Penimc.UnsafeNativeMethods.RaiseResetEvent(_pimcResetHandle.Value);

            // Wait for this work to be completed.
            releaseOperation.DoneEvent.WaitOne();
            releaseOperation.DoneEvent.Close();

            return releaseOperation.Result;
        }

        internal StylusDeviceInfo[] WorkerRefreshCursorInfo(IPimcTablet3 pimcTablet)
        {
            WorkerOperationRefreshCursorInfo refreshCursorInfo = new WorkerOperationRefreshCursorInfo(
                                                                 pimcTablet);
            lock(_workerOperationLock)
            {
                _workerOperation.Add(refreshCursorInfo);
            }

            // Kick thread to do this work.
            MS.Win32.Penimc.UnsafeNativeMethods.RaiseResetEvent(_pimcResetHandle.Value);

            // Wait for this work to be completed.
            refreshCursorInfo.DoneEvent.WaitOne();
            refreshCursorInfo.DoneEvent.Close();

            return refreshCursorInfo.StylusDevicesInfo;
        }

        internal TabletDeviceInfo WorkerGetTabletInfo(uint index)
        {
            // Set up data for call
            WorkerOperationGetTabletInfo getTabletInfo = new WorkerOperationGetTabletInfo(
                                                             index);
            lock(_workerOperationLock)
            {
                _workerOperation.Add(getTabletInfo);
            }

            // Kick thread to do this work.
            MS.Win32.Penimc.UnsafeNativeMethods.RaiseResetEvent(_pimcResetHandle.Value);

            // Wait for this work to be completed.
            getTabletInfo.DoneEvent.WaitOne();
            getTabletInfo.DoneEvent.Close();

            return getTabletInfo.TabletDeviceInfo;
        }

        internal TabletDeviceSizeInfo WorkerGetUpdatedSizes(IPimcTablet3 pimcTablet)
        {           
            // Set data up for call
            WorkerOperationWorkerGetUpdatedSizes getUpdatedSizes = new WorkerOperationWorkerGetUpdatedSizes(pimcTablet);
            lock(_workerOperationLock)
            {
                _workerOperation.Add(getUpdatedSizes);
            }

            // Kick thread to do this work.
            MS.Win32.Penimc.UnsafeNativeMethods.RaiseResetEvent(_pimcResetHandle.Value);

            // Wait for this work to be completed.
            getUpdatedSizes.DoneEvent.WaitOne();
            getUpdatedSizes.DoneEvent.Close();
            
            return getUpdatedSizes.TabletDeviceSizeInfo;
        }

        /////////////////////////////////////////////////////////////////////
        void FlushCache(bool goingOutOfRange)
        {
            // Force any cached move/inairmove data to be flushed if we have any.
            if (_cachedMoveData != null)
            {
                // If we are going out of range and this stylus id is not currently in range
                // then eat these cached events (keeps from going in and out of range quickly)
                if (!goingOutOfRange || _cachedMovePenContext.IsInRange(_cachedMoveStylusPointerId))
                {
                    _cachedMovePenContext.FirePenInRange(_cachedMoveStylusPointerId, _cachedMoveData, _cachedMoveStartTimestamp);
                    _cachedMovePenContext.FirePackets(_cachedMoveStylusPointerId, _cachedMoveData, _cachedMoveStartTimestamp);
                }

                _cachedMoveData = null;
                _cachedMovePenContext = null;
                _cachedMoveStylusPointerId = 0;
            }
        }

        /////////////////////////////////////////////////////////////////////

        bool DoCacheEvent(int evt, PenContext penContext, int stylusPointerId, int [] data, int timestamp)
        {
            // NOTE: Big assumption is that we always get other events between packets (ie don't get move
            // down position followed by move in up position).  We don't account for that here but it should
            // never happen.
            if (evt == PenEventPackets)
            {
                // If no cache then just cache it.
                if (_cachedMoveData == null)
                {
                    _cachedMovePenContext = penContext;
                    _cachedMoveStylusPointerId = stylusPointerId;
                    _cachedMoveStartTimestamp = timestamp;
                    _cachedMoveData = data;
                    return true;
                }
                else if (_cachedMovePenContext == penContext && stylusPointerId == _cachedMoveStylusPointerId)
                {
                    int sinceBeginning = timestamp - _cachedMoveStartTimestamp;
                    if (timestamp < _cachedMoveStartTimestamp)
                        sinceBeginning = (Int32.MaxValue - _cachedMoveStartTimestamp) + timestamp;

                    if (EventsFrequency > sinceBeginning)
                    {
                        // Add to cache data
                        int[] data0 = _cachedMoveData;
                        _cachedMoveData = new int [data0.Length + data.Length];
                        data0.CopyTo(_cachedMoveData, 0);
                        data.CopyTo(_cachedMoveData, data0.Length);
                        return true;
                    }
                }
            }

            return false;
        }

        /////////////////////////////////////////////////////////////////////

        internal void FireEvent(PenContext penContext, int evt, int stylusPointerId, int cPackets, int cbPacket, IntPtr pPackets)
        {
            // disposed?
            if (__disposed)
            {
                return;  // Don't process this event if we're in the process of shutting down.
            }

            // marshal the data to our cache
            if (cbPacket % 4 != 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.PenService_InvalidPacketData));
            }

            int cItems = cPackets * (cbPacket / 4);
            int[] data = null;
            if (0 < cItems)
            {
                data = new int [cItems]; // GetDataArray(cItems); // see comment on GetDataArray
                Marshal.Copy(pPackets, data, 0, cItems);
                penContext.CheckForRectMappingChanged(data, cPackets);
            }
            else
            {
                data = null;
            }

            int timestamp = Environment.TickCount;
            
            // Deal with caching packet data.
            if (DoCacheEvent(evt, penContext, stylusPointerId, data, timestamp))
            {
                return;
            }
            else
            {
                FlushCache(false);  // make sure we flush cache if not caching.
            }

            //
            // fire it
            //
            switch (evt)
            {
                case PenEventPenDown:
                    penContext.FirePenInRange(stylusPointerId, data, timestamp);
                    penContext.FirePenDown(stylusPointerId, data, timestamp);
                    break;

                case PenEventPenUp:
                    penContext.FirePenInRange(stylusPointerId, data, timestamp);
                    penContext.FirePenUp(stylusPointerId, data, timestamp);
                    break;

                case PenEventPackets:
                    penContext.FirePenInRange(stylusPointerId, data, timestamp);
                    penContext.FirePackets(stylusPointerId, data, timestamp);
                    break;

                case PenEventPenInRange:
                    // We fire this special event just to give the app thread an early peak at
                    // the inrange to filter out mouse moves before we get our first stylus event.
                    penContext.FirePenInRange(stylusPointerId, null, timestamp);
                    break;

                case PenEventPenOutOfRange:
                    penContext.FirePenOutOfRange(stylusPointerId, timestamp);
                    break;

                case PenEventSystem:
                    penContext.FireSystemGesture(stylusPointerId, timestamp);
                    break;
            }
        }

        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Returns a struct containing the list of TabletDevice properties for
        ///     a given tablet device (pimcTablet).
        /// </summary>
        private static TabletDeviceInfo GetTabletInfoHelper(IPimcTablet3 pimcTablet)
        {
            TabletDeviceInfo tabletInfo = new TabletDeviceInfo();

            tabletInfo.PimcTablet = new SecurityCriticalDataClass<IPimcTablet3>(pimcTablet);
            pimcTablet.GetKey(out tabletInfo.Id);
            pimcTablet.GetName(out tabletInfo.Name);
            pimcTablet.GetPlugAndPlayId(out tabletInfo.PlugAndPlayId);
            int iTabletWidth, iTabletHeight, iDisplayWidth, iDisplayHeight;
            pimcTablet.GetTabletAndDisplaySize(out iTabletWidth, out iTabletHeight, out iDisplayWidth, out iDisplayHeight);
            tabletInfo.SizeInfo = new TabletDeviceSizeInfo(new Size(iTabletWidth, iTabletHeight),
                                                           new Size(iDisplayWidth, iDisplayHeight));
            int caps;
            pimcTablet.GetHardwareCaps(out caps);
            tabletInfo.HardwareCapabilities = (TabletHardwareCapabilities)caps;
            int deviceType;
            pimcTablet.GetDeviceType(out deviceType);
            tabletInfo.DeviceType = (TabletDeviceType)(deviceType -1);

            // 
            // REENTRANCY NOTE: Let a PenThread do this work to avoid reentrancy!
            //                  The IPimcTablet3 object is created in the pen thread. If we access it from the UI thread,
            //                  COM will set up message pumping which will cause reentrancy here.
            InitializeSupportedStylusPointProperties(pimcTablet, tabletInfo);
            tabletInfo.StylusDevicesInfo = GetStylusDevicesInfo(pimcTablet);

            
            // Obtain the WispTabletKey for future use in locking the WISP tablet.
            tabletInfo.WispTabletKey = MS.Win32.Penimc.UnsafeNativeMethods.QueryWispTabletKey(pimcTablet);

            
            // If the manager has not already been created and locked, we will lock it here.  This is the first opportunity
            // we will have to lock the manager as it will have been created on the thread to instantiate the first tablet.
            MS.Win32.Penimc.UnsafeNativeMethods.SetWispManagerKey(pimcTablet);

            MS.Win32.Penimc.UnsafeNativeMethods.LockWispManager();

            return tabletInfo;
        }

        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Initializing the supported stylus point properties. and returns in workOperation class.
        /// </summary>
        private static void InitializeSupportedStylusPointProperties(IPimcTablet3 pimcTablet, TabletDeviceInfo tabletInfo)
        {
            int cProps;
            int cButtons;
            int pressureIndex = -1;

            pimcTablet.GetPacketDescriptionInfo(out cProps, out cButtons); // Calls Unmanaged code - SecurityCritical with SUC.
            List<StylusPointProperty> properties = new List<StylusPointProperty>(cProps + cButtons + 3);
            for ( int i = 0; i < cProps; i++ )
            {
                Guid guid;
                int min, max;
                int units;
                float res;
                pimcTablet.GetPacketPropertyInfo(i, out guid, out min, out max, out units, out res); // Calls Unmanaged code - SecurityCritical with SUC.

                if ( pressureIndex == -1 && guid == StylusPointPropertyIds.NormalPressure )
                {
                    pressureIndex = i;
                }

                StylusPointProperty property = new StylusPointProperty(guid, false);
                properties.Add(property);
            }

            for ( int i = 0; i < cButtons; i++ )
            {
                Guid buttonGuid;
                pimcTablet.GetPacketButtonInfo(i, out buttonGuid); // Calls Unmanaged code - SecurityCritical with SUC.

                StylusPointProperty buttonProperty = new StylusPointProperty(buttonGuid, true);
                properties.Add(buttonProperty);
            }

            //validate we can never get X, Y at index != 0, 1
            Debug.Assert(properties[StylusPointDescription.RequiredXIndex /*0*/].Id == StylusPointPropertyIds.X, "X isn't where we expect it! Fix PenImc to ask for X at index 0");
            Debug.Assert(properties[StylusPointDescription.RequiredYIndex /*1*/].Id == StylusPointPropertyIds.Y, "Y isn't where we expect it! Fix PenImc to ask for Y at index 1");
            // NOTE: We can't force pressure since touch digitizers may not provide this info.  The following assert is bogus.
            //Debug.Assert(pressureIndex == -1 || pressureIndex == StylusPointDescription.RequiredPressureIndex /*2*/,
            //    "Fix PenImc to ask for NormalPressure at index 2!");

            if ( pressureIndex == -1 )
            {
                //pressure wasn't found.  Add it
                properties.Insert(StylusPointDescription.RequiredPressureIndex /*2*/, System.Windows.Input.StylusPointProperties.NormalPressure);
            }
            else
            {
                //this device supports pressure
                tabletInfo.HardwareCapabilities |= TabletHardwareCapabilities.SupportsPressure;
            }

            tabletInfo.StylusPointProperties = new ReadOnlyCollection<StylusPointProperty>(properties);
            tabletInfo.PressureIndex = pressureIndex;
        }

        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Getting the cursor info of the stylus devices.
        /// </summary>
        private static StylusDeviceInfo[] GetStylusDevicesInfo(IPimcTablet3 pimcTablet)
        {
            int cCursors;

            pimcTablet.GetCursorCount(out cCursors); // Calls Unmanaged code - SecurityCritical with SUC.

            StylusDeviceInfo[] stylusDevicesInfo = new StylusDeviceInfo[cCursors];

            for ( int iCursor = 0; iCursor < cCursors; iCursor++ )
            {
                string sCursorName;
                int cursorId;
                bool fCursorInverted;
                pimcTablet.GetCursorInfo(iCursor, out sCursorName, out cursorId, out fCursorInverted); // Calls Unmanaged code - SecurityCritical with SUC.

                int cButtons;

                pimcTablet.GetCursorButtonCount(iCursor, out cButtons); // Calls Unmanaged code - SecurityCritical with SUC.
                StylusButton[] buttons = new StylusButton[cButtons];
                for ( int iButton = 0; iButton < cButtons; iButton++ )
                {
                    string sButtonName;
                    Guid buttonGuid;
                    pimcTablet.GetCursorButtonInfo(iCursor, iButton, out sButtonName, out buttonGuid); // Calls Unmanaged code - SecurityCritical with SUC.
                    buttons[iButton] = new StylusButton(sButtonName, buttonGuid);
                }
                StylusButtonCollection buttonCollection = new StylusButtonCollection(buttons);

                stylusDevicesInfo[iCursor].CursorName = sCursorName;
                stylusDevicesInfo[iCursor].CursorId = cursorId;
                stylusDevicesInfo[iCursor].CursorInverted = fCursorInverted;
                stylusDevicesInfo[iCursor].ButtonCollection = buttonCollection;
            }

            return stylusDevicesInfo;
        }


        internal bool AddPenContext(PenContext penContext)
        {
            List <PenContext> penContextRefs = new List<PenContext>(); // keep them alive while processing!
            int i;
            bool result = false;

            // Now go through and figure out the good entries
            // Need to clean up the list for gc'd references.
            for (i=0; i<_penContexts.Length; i++)
            {
                if (_penContexts[i].IsAlive)
                {
                    PenContext pc = _penContexts[i].Target as PenContext;
                    // We only need to ref if we have a penContext.
                    if (pc != null)
                    {
                        penContextRefs.Add(pc);
                    }
                }
            }

            // Now try again to see if we have room.
            if (penContextRefs.Count < MaxContextPerThread)
            {
                penContextRefs.Add(penContext); // add the new one to our list.

                
                // Lock the WISP Context to protect against COM rundown
                MS.Win32.Penimc.UnsafeNativeMethods.CheckedLockWispObjectFromGit(penContext.WispContextKey);

                // Now build up the handle array and PimcContext ref array.
                _pimcContexts = new IPimcContext3[penContextRefs.Count];
                _penContexts = new WeakReference[penContextRefs.Count];
                _handles = new IntPtr[penContextRefs.Count];
                _wispContextKeys = new UInt32[penContextRefs.Count];

                for (i=0; i < penContextRefs.Count; i++)
                {
                    PenContext pc = penContextRefs[i];
                    // We'd have hole in our array if this ever happened.
                    Debug.Assert(pc != null && pc.CommHandle != IntPtr.Zero);
                    _handles[i] = pc.CommHandle; // Add to array.
                    _pimcContexts[i] = pc._pimcContext.Value;
                    _penContexts[i] = new WeakReference(pc);
                    _wispContextKeys[i] = pc.WispContextKey;
                    pc = null;
                }

                result = true;
            }

            // Now clean up old refs and assign new array.
            penContextRefs.Clear(); // Make sure we remove refs!
            penContextRefs = null;

            return result;
        }


        internal bool RemovePenContext(PenContext penContext)
        {
            List <PenContext> penContextRefs = new List<PenContext>(); // keep them alive while processing!
            int i;
            bool removed = false;

            // Now go through and figure out the good entries
            // Need to clean up the list for gc'd references.
            for (i=0; i<_penContexts.Length; i++)
            {
                if (_penContexts[i].IsAlive)
                {
                    PenContext pc = _penContexts[i].Target as PenContext;
                    // See if we should keep this PenContext.  
                    // We keep if not GC'd and not the removing one (except if it is 
                    // in range where we need to wait till it goes out of range).
                    if (pc != null && (pc != penContext || pc.IsInRange(0)))
                    {
                        penContextRefs.Add(pc);
                    }
                }
            }

            removed = !penContextRefs.Contains(penContext);

            // Now build up the handle array and PimcContext ref array.
            _pimcContexts = new IPimcContext3[penContextRefs.Count];
            _penContexts = new WeakReference[penContextRefs.Count];
            _handles = new IntPtr[penContextRefs.Count];
            _wispContextKeys = new UInt32[penContextRefs.Count];

            for (i=0; i < penContextRefs.Count; i++)
            {
                PenContext pc = penContextRefs[i];
                // We'd have hole in our array if this ever happened.
                Debug.Assert(pc != null && pc.CommHandle != IntPtr.Zero);
                _handles[i] = pc.CommHandle; // Add to array.
                _pimcContexts[i] = pc._pimcContext.Value;
                _penContexts[i] = new WeakReference(pc);
                _wispContextKeys[i] = pc.WispContextKey;
                pc = null;
            }

            // Now clean up old refs and assign new array.
            penContextRefs.Clear(); // Make sure we remove refs!
            penContextRefs = null;

            if (removed)
            {
                
                // Unlock the WISP Context balancing the call in AddPenContext
                MS.Win32.Penimc.UnsafeNativeMethods.CheckedUnlockWispObjectFromGit(penContext.WispContextKey);

                
                // Since we are no longer using this PenContext, ensure it's released in the
                // native layer.
                // This is needed due to a COM rundown issue in the OS(OSGVSO:10779198).  If 
                // the COM proxy that the IPimcContext RCW holds is disconnected, the RCW will 
                // release the references to the underlying CPimcContext.  WPF will then use the
                // raw pointer to this object without realizing it has been destructed, leading
                // to AVs.
                penContext._pimcContext.Value.ShutdownComm();

                
                // Release the PenIMC object only when we are assured that the
                // context was removed from the list of waiting handles.
                
                // Restrict COM releases to Win7 as this can cause issues with later versions
                // of PenIMC and WISP due to using a context after it is released.
                if (!OSVersionHelper.IsOsWindows8OrGreater)
                {
                    Marshal.ReleaseComObject(penContext._pimcContext.Value);
                }
            }

            return removed;
        }

        /// <summary>
        /// Filters exceptions that we know could potentially throw in calls down into PenIMC
        /// </summary>
        /// <param name="e">The exception to filter</param>
        /// <returns>True if we should handle the exception, false otherwise.</returns>
        private static bool IsKnownException(Exception e)
        {
            return (e is COMException
                    || e is ArgumentException
                    || e is UnauthorizedAccessException
                    || e is InvalidCastException);
        }

        /////////////////////////////////////////////////////////////////////

        internal void ThreadProc()
        {
            Thread.CurrentThread.Name = "Stylus Input";

            try
            {
                //
                // the rarely iterated loop
                //
                while (!__disposed)
                {
#if TRACEPTW
                    Debug.WriteLine(String.Format("PenThreadWorker::ThreadProc():  Update __penContextWeakRefList loop"));
#endif

                    WorkerOperation [] workerOps = null;

                    lock(_workerOperationLock)
                    {
                        if (_workerOperation.Count > 0)
                        {
                            workerOps = _workerOperation.ToArray();
                            _workerOperation.Clear();
                        }
                    }

                    if (workerOps != null)
                    {
                        for (int j=0; j<workerOps.Length; j++)
                        {
                            workerOps[j].DoWork();
                        }
                        workerOps = null;
                    }

                    //
                    // the intense loop of dispatching events
                    //

                    while (true)
                    {
#if TRACEPTW
                        Debug.WriteLine (String.Format("PenThreadWorker::ThreadProc - handle event loop"));
#endif
                        // get next event
                        int     evt;
                        int     stylusPointerId;
                        int     cPackets, cbPacket;
                        IntPtr  pPackets;
                        int     iHandleEvt;
                        
                        if (_handles.Length == 1)
                        {
                            if (!MS.Win32.Penimc.UnsafeNativeMethods.GetPenEvent(
                                _handles[0], _pimcResetHandle.Value,
                                out evt, out stylusPointerId,
                                out cPackets, out cbPacket, out pPackets))
                            {
                                break;
                            }
                            iHandleEvt = 0;
                        }
                        else
                        {
                            if (!MS.Win32.Penimc.UnsafeNativeMethods.GetPenEventMultiple(
                                _handles.Length, _handles, _pimcResetHandle.Value,
                                out iHandleEvt, out evt, out stylusPointerId,
                                out cPackets, out cbPacket, out pPackets))
                            {
                                break;
                            }
                        }
                        if (evt != PenEventTimeout)
                        {
                            // dispatch the event
#if TRACEPTW
                            Debug.WriteLine (String.Format("PenThreadWorker::ThreadProc - FireEvent [evt={0}, stylusId={1}]", evt, stylusPointerId));
#endif
                            
                            // This comment addresses and IndexOutOfRangeException in PenThreadWorker which is related and likely caused by the above.
                            // This index is safe as long as there are no corruption issues within PenIMC.  There have been
                            // instances of IndexOutOfRangeExceptions from this code but this should not occur in practice.
                            // If this throws, check that the handles list generated in CPimcContext::GetPenEventMultiple
                            // is not corrupted (it has appropriate wait handles and does not point to invalid memory).
                            PenContext penContext = _penContexts[iHandleEvt].Target as PenContext;
                            // If we get an event from a GC'd PenContext then just ignore.
                            if (penContext != null)
                            {
                                FireEvent(penContext, evt, stylusPointerId, cPackets, cbPacket, pPackets);
                                penContext = null;
                            }
                        }
                        else
                        {
#if TRACEPTW
                            Debug.WriteLine (String.Format("PenThreadWorker::ThreadProc - FlushInput"));
#endif
                            FlushCache(true);

                            // we hit the timeout, make sure that all our devices are in the correct out-of-range state
                            // we are doing this to compinsate for drivers that send a move after they send a outofrange
                            for (int i = 0; i < _penContexts.Length; i++)
                            {
                                PenContext penContext = _penContexts[i].Target as PenContext;
                                if (penContext != null)
                                {
                                    // we send 0 as the stulyspointerId to trigger code in PenContext::FirePenOutOfRange
                                    penContext.FirePenOutOfRange(0, Environment.TickCount);
                                    penContext = null;
                                }
                            }
                        }
                    }
                }
            }

            finally
            {
                // Make sure we are marked as disposed now.  This keeps the
                // Dispose() method from doing any work.
                __disposed = true;

                // We've been disposed or hit thread abort.  Release this handle before exiting.
                MS.Win32.Penimc.UnsafeNativeMethods.DestroyResetEvent(_pimcResetHandle.Value);

                
                // Release the manager locks, both PenIMC and WISP here to balance lock calls.
                MS.Win32.Penimc.UnsafeNativeMethods.UnlockWispManager();
                MS.Win32.Penimc.UnsafeNativeMethods.ReleaseManagerExternalLock();

                for (int i = 0; i < _pimcContexts.Length; i++)
                {
                    
                    // Unlock the WISP Context, balancing the call in AddPenContext.
                    MS.Win32.Penimc.UnsafeNativeMethods.CheckedUnlockWispObjectFromGit(_wispContextKeys[i]);

                    // Ensure that all native references are released.
                    _pimcContexts[i].ShutdownComm();
                }

                // Make sure the _pimcResetHandle is still valid after Dispose is called and before
                // our thread exits.
                GC.KeepAlive(this);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define TRACE

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Threading;
using System.Security;
using System.Security.Permissions;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Win32.Penimc;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////

    internal sealed class PenThread
    {
        private PenThreadWorker _penThreadWorker;

        /// <SecurityNote>
        ///    Critical - Calls SecurityCritical code PenThreadWorker constructor.
        ///             Called by PenThreadPool.RegisterPenContextHelper.
        ///             TreatAsSafe boundry is Stylus.EnableCore, Stylus.RegisterHwndForInput
        ///                and HwndWrapperHook class (via HwndSource.InputFilterMessage).
        /// </SecurityNote>
        internal PenThread()
        {
            _penThreadWorker = new PenThreadWorker();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        internal void Dispose()
        {
            DisposeHelper();
        }

        /////////////////////////////////////////////////////////////////////

        ~PenThread()
        {
            DisposeHelper();
        }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        /// Critical - Call security critical method PenThreadWorker.Dispose().
        /// TreatAsSafe - Safe since it only frees internal private handle
        ///               on an object that is going to be also marked as disposed and
        ///               start failing all calls after return.
        /// </SecurityNote>
        void DisposeHelper()
        {
            // NOTE: PenThreadWorker deals with already being disposed logic.
            if (_penThreadWorker != null)
            {
                _penThreadWorker.Dispose();
            }
            GC.KeepAlive(this);
        }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenThreadWorker.WorkerAddPenContext.
        ///             Called by PenThreadPool.RegisterPenContextHelper.
        ///             TreatAsSafe boundry is Stylus.EnableCore, Stylus.RegisterHwndForInput
        ///                and HwndWrapperHook class (via HwndSource.InputFilterMessage).
        /// </SecurityNote>
        internal bool AddPenContext(PenContext penContext)
        {
            return _penThreadWorker.WorkerAddPenContext(penContext);
        }

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenThreadWorker.WorkerRemovePenContext.
        ///             Called by PenContext.Disable.
        ///             TreatAsSafe boundry is PenContext.Dispose, Stylus.ProcessDisplayChange
        ///                and HwndWrapperHook class (via HwndSource.InputFilterMessage).
        /// </SecurityNote>
        internal bool RemovePenContext(PenContext penContext)
        {
            return _penThreadWorker.WorkerRemovePenContext(penContext);
        }


        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenThreadWorker.WorkerGetTabletsInfo.
        ///             Called by PenThreadPool.WorkerGetTabletsInfo.
        /// </SecurityNote>
        internal TabletDeviceInfo[] WorkerGetTabletsInfo()
        {
            return _penThreadWorker.WorkerGetTabletsInfo();
        }


        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenThreadWorker.WorkerCreateContext.
        ///             Called by PenThreadPool.WorkerCreateContext.
        ///             TreatAsSafe boundry is Stylus.EnableCore and HwndWrapperHook class 
        ///             (via HwndSource.InputFilterMessage).
        /// </SecurityNote>
        internal PenContextInfo WorkerCreateContext(IntPtr hwnd, IPimcTablet3 pimcTablet)
        {
            return _penThreadWorker.WorkerCreateContext(hwnd, pimcTablet);
        }

        /// <summary>
        /// Acquires a WISP/PenIMC tablet object's external lock on the PenThread.
        /// </summary>
        /// <param name="gitKey">The GIT key for the object.</param>
        /// <returns>True if successful, false otherwise.</returns>
        /// <SecurityNote>
        ///     Critical - Calls PenThreadWorker.WorkerAcquireTabletLocks.
        /// </SecurityNote>
        internal bool WorkerAcquireTabletLocks(IPimcTablet3 tablet, UInt32 wispTabletKey)
        {
            return _penThreadWorker.WorkerAcquireTabletLocks(tablet, wispTabletKey);
        }

        /// <summary>
        /// Releases a WISP/PenIMC tablet object's external lock on the PenThread.
        /// </summary>
        /// <param name="gitKey">The GIT key for the object.</param>
        /// <returns>True if successful, false otherwise.</returns>
        /// <SecurityNote>
        ///     Critical - Calls PenThreadWorker.WorkerReleaseTabletLocks.
        /// </SecurityNote>
        internal bool WorkerReleaseTabletLocks(IPimcTablet3 tablet, UInt32 wispTabletKey)
        {
            return _penThreadWorker.WorkerReleaseTabletLocks(tablet, wispTabletKey);
        }

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenThreadWorker.WorkerRefreshCursorInfo.
        ///             Called by PenThreadPool.WorkerRefreshCursorInfo.
        /// </SecurityNote>
        internal StylusDeviceInfo[] WorkerRefreshCursorInfo(IPimcTablet3 pimcTablet)
        {
            return _penThreadWorker.WorkerRefreshCursorInfo(pimcTablet);
        }

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenThreadWorker.WorkerGetTabletInfo.
        ///             Called by PenThreadPool.WorkerGetTabletInfo.
        /// </SecurityNote>
        internal TabletDeviceInfo WorkerGetTabletInfo(uint index)
        {
            return _penThreadWorker.WorkerGetTabletInfo(index);
        }

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenThreadWorker.WorkerGetUpdatedSizes.
        ///             Called by PenThreadPool.WorkerGetUpdatedTabletRect.
        /// </SecurityNote>
        internal TabletDeviceSizeInfo WorkerGetUpdatedSizes(IPimcTablet3 pimcTablet)
        {
            return _penThreadWorker.WorkerGetUpdatedSizes(pimcTablet);
        }
    }
}

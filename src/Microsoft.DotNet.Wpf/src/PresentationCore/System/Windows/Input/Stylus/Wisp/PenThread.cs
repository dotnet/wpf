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

        internal bool AddPenContext(PenContext penContext)
        {
            return _penThreadWorker.WorkerAddPenContext(penContext);
        }

        internal bool RemovePenContext(PenContext penContext)
        {
            return _penThreadWorker.WorkerRemovePenContext(penContext);
        }


        /////////////////////////////////////////////////////////////////////

        internal TabletDeviceInfo[] WorkerGetTabletsInfo()
        {
            return _penThreadWorker.WorkerGetTabletsInfo();
        }


        internal PenContextInfo WorkerCreateContext(IntPtr hwnd, IPimcTablet3 pimcTablet)
        {
            return _penThreadWorker.WorkerCreateContext(hwnd, pimcTablet);
        }

        /// <summary>
        /// Acquires a WISP/PenIMC tablet object's external lock on the PenThread.
        /// </summary>
        /// <param name="gitKey">The GIT key for the object.</param>
        /// <returns>True if successful, false otherwise.</returns>
        internal bool WorkerAcquireTabletLocks(IPimcTablet3 tablet, UInt32 wispTabletKey)
        {
            return _penThreadWorker.WorkerAcquireTabletLocks(tablet, wispTabletKey);
        }

        /// <summary>
        /// Releases a WISP/PenIMC tablet object's external lock on the PenThread.
        /// </summary>
        /// <param name="gitKey">The GIT key for the object.</param>
        /// <returns>True if successful, false otherwise.</returns>
        internal bool WorkerReleaseTabletLocks(IPimcTablet3 tablet, UInt32 wispTabletKey)
        {
            return _penThreadWorker.WorkerReleaseTabletLocks(tablet, wispTabletKey);
        }

        internal StylusDeviceInfo[] WorkerRefreshCursorInfo(IPimcTablet3 pimcTablet)
        {
            return _penThreadWorker.WorkerRefreshCursorInfo(pimcTablet);
        }

        internal TabletDeviceInfo WorkerGetTabletInfo(uint index)
        {
            return _penThreadWorker.WorkerGetTabletInfo(index);
        }

        internal TabletDeviceSizeInfo WorkerGetUpdatedSizes(IPimcTablet3 pimcTablet)
        {
            return _penThreadWorker.WorkerGetUpdatedSizes(pimcTablet);
        }
    }
}

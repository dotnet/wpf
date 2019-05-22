// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace System.Windows.Xps.Serialization.RCW
{
    internal class PrintDocumentPackageStatusProvider : IPrintDocumentPackageStatusEvent
    {
        public PrintDocumentPackageStatusProvider(IPrintDocumentPackageTarget docPackageTarget)
        {
            _jobId = 0;
            _jobIdAcquiredEvent = null;

            IConnectionPointContainer connectionPointContainer = docPackageTarget as IConnectionPointContainer;
            if (connectionPointContainer != null)
            {
                IConnectionPoint connectionPoint = null;
                Guid riid = typeof(IPrintDocumentPackageStatusEvent).GUID;
                connectionPointContainer.FindConnectionPoint(ref riid, out connectionPoint);

                if (connectionPoint != null)
                {
                    _connectionPoint = connectionPoint;
                    int cookie = -1;
                    connectionPoint.Advise(this, out cookie);

                    if (cookie != -1)
                    {
                        _cookie = cookie;
                        _jobIdAcquiredEvent = new ManualResetEvent(false);
                    }
                }
            }
        }

        public void PackageStatusUpdated(ref PrintDocumentPackageStatus packageStatus)
        {
            // Same behavior as XpsDeviceSimulatingPrintThunkHandler
            _jobId = unchecked((int)packageStatus.JobId);
            if (_jobId != 0)
            {
                _jobIdAcquiredEvent.Set();
            }
        }

        public 
        void
        UnAdvise(
            )
        {
            if (_connectionPoint != null && _cookie != null)
            {
                _connectionPoint.Unadvise(_cookie.Value);
            }
        }


        public
        ManualResetEvent
        JobIdAcquiredEvent
        {
            get
            {
                return _jobIdAcquiredEvent;
            }
        }

        public
        int
        JobId
        {
            get
            {
                return _jobId;
            }
        }


        private int _jobId;
        private ManualResetEvent _jobIdAcquiredEvent;
        private int? _cookie;
        private IConnectionPoint _connectionPoint;

    }
}

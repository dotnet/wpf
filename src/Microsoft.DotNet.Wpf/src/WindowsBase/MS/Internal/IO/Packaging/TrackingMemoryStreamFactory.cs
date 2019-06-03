// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This is a basic implementation of the ITrackingMemoryStreamFactory interface
//
//
//
//

using System;
using System.IO;
using System.Diagnostics;

namespace MS.Internal.IO.Packaging
{   
    /// <summary>
    /// TrackingMemoryStreamFactory class is used in the Sparse Memory Stream to keep track of the memory Usage 
    /// </summary>
    internal class TrackingMemoryStreamFactory : ITrackingMemoryStreamFactory
    {
        public MemoryStream Create()
        {   
            return new TrackingMemoryStream((ITrackingMemoryStreamFactory)this);
        }

        public MemoryStream Create(int capacity)
        {   
            return new TrackingMemoryStream((ITrackingMemoryStreamFactory)this, capacity);
        }

        public void ReportMemoryUsageDelta(int delta)
        {   
            checked{_bufferedMemoryConsumption += delta;}
            Debug.Assert(_bufferedMemoryConsumption >=0, "we end up having buffers of negative size");
        }

        internal long CurrentMemoryConsumption
        {
            get
            {
                return _bufferedMemoryConsumption;
            }
        }

        private long _bufferedMemoryConsumption;
    }
} 

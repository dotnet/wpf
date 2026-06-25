// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

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

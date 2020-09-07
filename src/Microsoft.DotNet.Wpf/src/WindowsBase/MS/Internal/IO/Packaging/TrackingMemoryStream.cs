// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This is a stream that is capable of reporting data usage up to the registered
//  owner 
//
//
//
//
//

using System;
using System.Diagnostics;
using System.IO;

namespace MS.Internal.IO.Packaging
{
    // making this class sealed as it is taking advantage of some Virtual methods 
    // in MemoryStream(Capacity); therefore, there is a danger of subclass overriding those and unexpected 
    // behavior changes. Consider calls from Constructor->ReportIfNecessary->Capacity 
    // prior to unsealing this class (they would be marked as FxCop violations) 
    internal sealed class TrackingMemoryStream : MemoryStream
    {
        // other constructors can be added later, as we need them, for now we only use the following 2  
        internal TrackingMemoryStream(ITrackingMemoryStreamFactory memoryStreamFactory): base()
        {
            // although we could have implemented this constructor in terms of the other constructor; we shouldn't. 
            // It seems safer to always call the equivalent base class constructor, as we might be ignorant about 
            // some minor differences between various MemoryStream constructors
            Debug.Assert(memoryStreamFactory != null);            
            _memoryStreamFactory = memoryStreamFactory;
            ReportIfNeccessary();
        }
            
        internal TrackingMemoryStream (ITrackingMemoryStreamFactory memoryStreamFactory, Int32 capacity) : base(capacity)
        {
            Debug.Assert(memoryStreamFactory != null);
            _memoryStreamFactory = memoryStreamFactory;
            ReportIfNeccessary();
        }


        // Here are the overrides for members that could possible result in changes in the allocated memory 
        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = base.Read(buffer, offset, count);

            ReportIfNeccessary();
            return result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);

            ReportIfNeccessary();
        }

        public override void SetLength(long value)
        {
            base.SetLength(value);

            ReportIfNeccessary();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_memoryStreamFactory != null)
                    {
                        // release all the memory, and report it to the TrackingMemoryStreamFactory 
                        SetLength(0);
                        Capacity = 0;
                        ReportIfNeccessary();
                        _memoryStreamFactory = null;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void ReportIfNeccessary ()        
        {
            if (this.Capacity !=_lastReportedHighWaterMark)
            {
                // we need to report the new memory being allocated as a part of the constructor  
                _memoryStreamFactory.ReportMemoryUsageDelta(checked(this.Capacity - _lastReportedHighWaterMark));
                _lastReportedHighWaterMark = this.Capacity;
            }
        }
        
        private ITrackingMemoryStreamFactory _memoryStreamFactory;
        private int _lastReportedHighWaterMark;
    }
}

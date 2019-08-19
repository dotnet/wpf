// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  WriteTimeStream - wraps the ArchiveStream in Streaming generation scenarios so that we
//  can determine current archive stream offset even when working on a stream that is non-seekable
//  because the Position property is unusable on such streams.
//
//
//
//

using System;
using System.IO;
using System.Windows;  
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Zip
{
    internal class WriteTimeStream : Stream
    {
        //------------------------------------------------------
        //
        //  Public Properties  
        //
        //------------------------------------------------------
        /// <summary>
        /// CanRead - never
        /// </summary>
        override public bool CanRead { get { return false; } }

        /// <summary>
        /// CanSeek - never
        /// </summary>
        override public bool CanSeek{ get { return false; } }

        /// <summary>
        /// CanWrite - only if we are not disposed
        /// </summary>
        override public bool CanWrite { get { return (_baseStream != null); } }

        /// <summary>
        /// Same as Position
        /// </summary>
        override public long Length 
        { 
            get 
            {
                CheckDisposed();
                return _position;
            } 
        }

        /// <summary>
        /// Get is supported even on Write-only stream
        /// </summary>
        override public long Position
        {
            get
            {
                CheckDisposed();
                return _position;
            }
            set 
            {
                CheckDisposed();
                IllegalAccess();        // throw exception
            }
        }

        //------------------------------------------------------
        //
        //  Public Methods  
        //
        //------------------------------------------------------
        public override void SetLength(long newLength)
        {
            IllegalAccess();        // throw exception
        }

        override public long Seek(long offset, SeekOrigin origin)
        {
            IllegalAccess();        // throw exception
            return -1;              // keep compiler happy 
        }        

        override public int Read(byte[] buffer, int offset, int count)
        {
            IllegalAccess();        // throw exception
            return -1;              // keep compiler happy 
        }

        override public void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            _baseStream.Write(buffer, offset, count);
            checked{_position += count;}
        }
        
        override public void Flush()
        {
            CheckDisposed();
            _baseStream.Flush(); 
        }

        //------------------------------------------------------
        //
        //  Internal Methods  
        //
        //------------------------------------------------------
        internal WriteTimeStream(Stream baseStream)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException("baseStream");
            }

            _baseStream = baseStream;

            // must be based on writable stream
            if (!_baseStream.CanWrite)
                throw new ArgumentException(SR.Get(SRID.WriteNotSupported), "baseStream");
        }

        //------------------------------------------------------
        //
        //  Protected Methods  
        //
        //------------------------------------------------------
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && (_baseStream != null))
                {
                    _baseStream.Close();
                }
            }
            finally
            {
                _baseStream = null;
                base.Dispose(disposing);
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods  
        //
        //------------------------------------------------------
        private static void IllegalAccess()
        {
            throw new NotSupportedException(SR.Get(SRID.WriteOnlyStream));
        }

        private void CheckDisposed()
        {
            if (_baseStream == null)
                throw new ObjectDisposedException("Stream");
        }

        // _baseStream doubles as our disposed indicator - it's null if we are disposed
        private Stream      _baseStream;        // stream we wrap - needs to only support Write
        private long        _position;          // current position
    }
}

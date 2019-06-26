// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Description:    The class UnsafeIndexingFilterStream uses an OLE IStream component
//                  passed on a indexing filter's IPersistStream interface to implement
//                  the System.IO.Stream functions necessary for filtering a document.
//                  In other words, it basically implements a seekable read-only stream.
//
//                  For a more complete example of an IStream adapter, see Listing 20.2
//                  in Adam Nathan's ".Net and COM".
//

using System;
using System.IO;
using System.Runtime.InteropServices;           // For Marshal
using System.Windows;                           // for ExceptionStringTable
using MS.Win32;                                 // For NativeMethods
using MS.Internal.Interop;	                // for IStream
using System.Security;                          // For SecurityCritical


namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// The class UnsafeIndexingFilterStream uses an OLE IStream component           
    /// passed on an indexing filter's IPersistStream interface to implement       
    /// the System.IO.Stream functions necessary for filtering a document.      
    /// In other words, it basically implements a seekable read-only stream.
    /// </summary>
    /// <remarks>
    /// 
    /// This class is used only by the Container filter, since the Xaml filter is not accessible directly
    /// from unmanaged code and so can use System.IO.Stream natively.
    ///     
    /// This class does not own the process of closing the underlying stream. However, 
    /// This class does own a reference to a COM object that should be released as a part of the Dispose pattern,
    /// so that the underlying unmanaged code doesn't keep the stream open indefinitely  (or until GC gets to it)
    ///
    /// The definition of IStream that is used is MS.Internal.Interop.IStream rather than the standard one
    /// so as to allow efficient marshaling of arrays with specified offsets in Read.
    /// </remarks>
    internal class UnsafeIndexingFilterStream : Stream
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        /// <summary>
        /// Build a System.IO.Stream implementation around an IStream component.
        /// </summary>
        /// <remarks>
        /// The client code is entirely responsible for the lifespan of the stream,
        /// and there is no way it can tip us off for when to release it. Therefore,
        /// its reference count is not incremented. The risk of the client 
        /// releasing the IStream component before we're done with it is no worse than
        /// that of the client passing a pointer to garbage in the first place, and we
        /// cannot protect against that either. After all, the client is unmanaged and
        /// has endless possibilities of trashing the machine if she wishes to.
        /// </remarks>
        internal UnsafeIndexingFilterStream(IStream oleStream)
        {
            if (oleStream == null)
                throw new ArgumentNullException("oleStream");

            _oleStream = oleStream;
            _disposed = false;
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Return the bytes requested.
        /// </summary>
        /// <param name="buffer">Destination buffer.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">How many bytes requested.</param>
        /// <returns>How many bytes were written into buffer.</returns>
        public unsafe override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfStreamDisposed();

            // Check arguments.
            PackagingUtilities.VerifyStreamReadArgs(this, buffer, offset, count);

            // Reading 0 bytes is a no-op.
            if (count == 0)
                return 0;

            // Prepare location of return value and call the COM object.
            int    bytesRead;
            IntPtr pBytesRead = new IntPtr(&bytesRead);

            // Prepare to restore position in case the read fails.
            long positionBeforeReadAttempt = this.Position;

            try 
            {
                // Pin the array wrt GC while using an address in it.
                fixed (byte *bufferPointer = &buffer[offset])
                {
                    _oleStream.Read(new IntPtr(bufferPointer), count, pBytesRead);
                }
            }
            catch (COMException comException)
            {
                this.Position = positionBeforeReadAttempt;
                throw new IOException("Read", comException);
            }
            catch (IOException ioException)
            {
                this.Position = positionBeforeReadAttempt;
                throw new IOException("Read", ioException);
            }
            return bytesRead;
        }

        /// <summary>
        /// Seek -unmanaged streams do not allow seeking beyond the end of the stream
        /// and since we rely on the underlying stream to validate and return the seek
        /// results, unlike managed streams where seeking beyond the end of the stream
        /// is allowed we will get an exception.
        /// </summary>
        /// <param name="offset">Offset in byte.</param>
        /// <param name="origin">Offset origin (start, current, or end).</param>
        public unsafe override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfStreamDisposed();

            long position = 0;
            // The address of 'position' can be used without pinning the object, because it
            // is a value and is therefore allocated on the stack rather than the heap.
            IntPtr positionAddress = new IntPtr(&position);
            
            // The enum values of SeekOrigin match the STREAM_SEEK_* values. This
            // convention is as good as carved in stone, so there's no need for a switch here.
            _oleStream.Seek(offset, (int)origin, positionAddress);

            return position;
        }

        /// <summary>
        /// SetLength
        /// </summary>
        /// <exception cref="NotSupportedException">not supported</exception>
        /// <remarks>
        /// Not supported. No indexing filter should require it.
        /// </remarks>
        public override void SetLength(long newLength)
        {
            ThrowIfStreamDisposed();
            throw new NotSupportedException(SR.Get(SRID.StreamDoesNotSupportWrite));
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <exception cref="NotSupportedException">not supported</exception>
        /// <remarks>
        /// Not supported. No indexing filter should require it.
        /// </remarks>
        public override void Write(byte[] buf, int offset, int count)
        {
            ThrowIfStreamDisposed();
            throw new NotSupportedException(SR.Get(SRID.StreamDoesNotSupportWrite));
        }

        /// <summary>
        /// Flush 
        /// </summary>
        public override void Flush()
        {
            ThrowIfStreamDisposed();
            //This stream is always readonly, and calling this method is a no-op
            //No IndexingFilter should require this method.
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Is stream readable?
        /// </summary>
        /// <remarks>
        /// We always return true, because there's no way of checking whether the caller
        /// has closed the stream.
        /// </remarks>
        public override bool CanRead
        {
            get
            {
                return !_disposed;
            }
        }

        /// <summary>
        /// Is stream seekable?
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                // This information is not available from the underlying stream.
                // So one assumption has to be made. True is the most common for indexable streams.
                return !_disposed;
            }
        }

        /// <summary>
        /// Is stream writable?
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Logical byte position in this stream
        /// </summary>
        public override long Position
        {
            get
            {
                ThrowIfStreamDisposed();
                return Seek(0, SeekOrigin.Current);
            }
            set
            {
                ThrowIfStreamDisposed();

                if (value < 0)
                    throw new ArgumentException(SR.Get(SRID.CannotSetNegativePosition));

                Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Length.
        /// </summary>
        public override long Length
        {
            get
            {
                ThrowIfStreamDisposed();

                // Retrieve stream stats. STATFLAG_NONAME means don't return the stream name.
                System.Runtime.InteropServices.ComTypes.STATSTG statstg;
                _oleStream.Stat(out statstg, NativeMethods.STATFLAG_NONAME);
                return statstg.cbSize;
            }
        }

        //------------------------------------------------------
        //
        //   Protected methods.
        //
        //------------------------------------------------------

        /// <summary>
        /// <para>
        /// Although UnsafeIndexingFilterStream does not close the underlying stream, it is responsible for releasing 
        /// the ComObject it holds, so that unmanaged code can properly close the stream. 
        /// </para> <para>
        /// This method gets invoked as part of the base class's Dispose() or Close() implementation.
        /// </para>
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_oleStream != null)
                    {
                        MS.Win32.UnsafeNativeMethods.SafeReleaseComObject(_oleStream);
                    }
                }
            }
            finally
            {
                // Calls to Dispose(bool) are expected to bubble up through the class hierarchy.
                _oleStream = null;  
                _disposed = true;
                base.Dispose(disposing);
            }
        }
            
        //------------------------------------------------------
        //
        //   Private methods.
        //
        //------------------------------------------------------

        private void ThrowIfStreamDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(null, SR.Get(SRID.StreamObjectDisposed));
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private IStream          _oleStream;   // Underlying COM component.
        private bool             _disposed;
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   Stream interface for manipulating data within a container stream.
//
//
//
//
//
//

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.IO.Packaging;
using MS.Internal.WindowsBase;

using System.Windows;

namespace MS.Internal.IO.Packaging.CompoundFile
{
/// <summary>
/// Class for manipulating data within container streams
/// </summary>
internal class CFStream : Stream
{
    //------------------------------------------------------
    //
    //  Public Properties
    //
    //------------------------------------------------------

    /// <summary>
    /// See .NET Framework SDK under System.IO.Stream
    /// </summary>
    public override bool CanRead
    {
        get
        {
            return (!StreamDisposed && (FileAccess.Read == (access & FileAccess.Read) ||
                    FileAccess.ReadWrite == (access & FileAccess.ReadWrite)));
        }
    }

    /// <summary>
    /// See .NET Framework SDK under System.IO.Stream
    /// </summary>
    public override bool CanSeek
    {
        get
        {
            // OLE32 DocFiles on local disk always seekable
            return (!StreamDisposed);       // unless it's disposed
        }
    }

    /// <summary>
    /// See .NET Framework SDK under System.IO.Stream
    /// </summary>
    public override bool CanWrite
    {
        get
        {
            return (!StreamDisposed && (FileAccess.Write     == (access & FileAccess.Write) ||
                    FileAccess.ReadWrite == (access & FileAccess.ReadWrite)));
        }
    }

    /// <summary>
    /// See .NET Framework SDK under System.IO.Stream
    /// </summary>
    public override long Length
    {
        get
        {
            CheckDisposedStatus();
            
            System.Runtime.InteropServices.ComTypes.STATSTG streamStat;

            // STATFLAG_NONAME required on IStream::Stat
            _safeIStream.Stat( out streamStat, SafeNativeCompoundFileConstants.STATFLAG_NONAME );

            return streamStat.cbSize;
        }
    }

    /// <summary>
    /// See .NET Framework SDK under System.IO.Stream
    /// </summary>
    public override long Position
    {
        get
        {
            CheckDisposedStatus();
            
            long seekPos = 0;

            _safeIStream.Seek(
                0, // Offset of zero
                SafeNativeCompoundFileConstants.STREAM_SEEK_CUR,
                out seekPos );

            return seekPos;
        }
        set
        {
            CheckDisposedStatus();

            if (!CanSeek)
            {
                throw new NotSupportedException(SR.SetPositionNotSupported);
            }
            
            long seekPos = 0;

            _safeIStream.Seek(
                value , // given offset
                SafeNativeCompoundFileConstants.STREAM_SEEK_SET,
                out seekPos );

            if( value != seekPos )
            {
                throw new IOException(
                    SR.SeekFailed);
            }
        }
    }

    //------------------------------------------------------
    //
    //  Public Methods
    //
    //------------------------------------------------------

    /// <summary>
    /// See .NET Framework SDK under System.IO.Stream
    /// </summary>
    public override void Flush()
    {
        CheckDisposedStatus();
        _safeIStream.Commit(0);
    }

    /// <summary>
    /// See .NET Framework SDK under System.IO.Stream
    /// </summary>
    /// <param name="offset">Offset byte count</param>
    /// <param name="origin">Offset origin</param>
    /// <returns></returns>
    public override long Seek( long offset, SeekOrigin origin )
    {
        CheckDisposedStatus();

        if (!CanSeek)
        {
            throw new NotSupportedException(SR.SeekNotSupported);
        }
        
        // 
        long seekPos = 0;
        int translatedSeekOrigin = 0;

        switch(origin)
        {
            case SeekOrigin.Begin:
                translatedSeekOrigin = SafeNativeCompoundFileConstants.STREAM_SEEK_SET;
                if( 0 > offset )
                {
                    throw new ArgumentOutOfRangeException("offset",
                        SR.SeekNegative);
                }
                break;
            case SeekOrigin.Current:
                translatedSeekOrigin = SafeNativeCompoundFileConstants.STREAM_SEEK_CUR;
                // Need to find the current seek pointer to see if we'll end
                //  up with a negative position.
                break;
            case SeekOrigin.End:
                translatedSeekOrigin = SafeNativeCompoundFileConstants.STREAM_SEEK_END;
                // Need to find the current seek pointer to see if we'll end
                //  up with a negative position.
                break;
            default:
                throw new System.ComponentModel.InvalidEnumArgumentException("origin", (int) origin, typeof(SeekOrigin));
        }

        _safeIStream.Seek( offset, translatedSeekOrigin, out seekPos );

        return seekPos;
    }

    /// <summary>
    /// See .NET Framework SDK under System.IO.Stream
    /// </summary>
    /// <param name="newLength">New length</param>
    public override void SetLength( long newLength )
    {
        CheckDisposedStatus();

        if (!CanWrite)
        {
            throw new NotSupportedException(SR.SetLengthNotSupported);
        }

        if( 0 > newLength )
        {
            throw new ArgumentOutOfRangeException("newLength",
                SR.StreamLengthNegative);
        }
        
        _safeIStream.SetSize( newLength );

        // updating the stream pointer if the stream has been truncated.
        if (newLength < this.Position)
            this.Position = newLength;
    }

    /// <summary>
    /// See .NET Framework SDK under System.IO.Stream
    /// </summary>
    /// <param name="buffer">Read data buffer</param>
    /// <param name="offset">Buffer start position</param>
    /// <param name="count">Number of bytes to read</param>
    /// <returns>Number of bytes actually read</returns>
    public override int Read( byte[] buffer, int offset, int count )
    {
        CheckDisposedStatus();

        PackagingUtilities.VerifyStreamReadArgs(this, buffer, offset, count);

        int read = 0;

        if ( 0 == offset ) // Zero offset is typical case
        {
            _safeIStream.Read( buffer, count, out read );
        }
        else // Non-zero offset
        {
            // Read into local array and then copy it into the given buffer at
            //  the specified offset.
            byte[] localBuffer = new byte[count];
            _safeIStream.Read( localBuffer, count, out read );

            if (read > 0)
                Array.Copy(localBuffer, 0, buffer, offset, read);
        }

        return read;
    }

    /// <summary>
    /// See .NET Framework SDK under System.IO.Stream
    /// </summary>
    /// <param name="buffer">Data buffer</param>
    /// <param name="offset">Buffer write start position</param>
    /// <param name="count">Number of bytes to write</param>
    public override void Write( byte[] buffer, int offset, int count )
    {
        CheckDisposedStatus();

        PackagingUtilities.VerifyStreamWriteArgs(this, buffer, offset, count);

        int written = 0; // CLR guys have deemed this uninteresting?

        if ( 0 == offset ) // Zero offset is typical case
        {
            _safeIStream.Write( buffer, count, out written );
        }
        else // Non-zero offset
        {
            // Copy from indicated offset to zero-based temp buffer
            byte[] localBuffer = new byte[count];
            Array.Copy(buffer, offset, localBuffer, 0, count);
            _safeIStream.Write( localBuffer, count, out written );
        }

        if( count != written )
            throw new IOException(
                SR.WriteFailure);
}

    //------------------------------------------------------
    //
    //  Internal Methods
    //
    //------------------------------------------------------

    // Check whether this Stream object is still valid.  If not, thrown an
    //  ObjectDisposedException.
    internal void CheckDisposedStatus()
    {
        if( StreamDisposed )
            throw new ObjectDisposedException(null, SR.StreamObjectDisposed);
    }

    // Check whether this Stream object is still valid.
    internal bool StreamDisposed
    {
        get
        {
            return (backReference.StreamInfoDisposed || ( null == _safeIStream ));
        }
    }

    // Constructors
    internal CFStream(
        IStream underlyingStream,
        FileAccess openAccess,
        StreamInfo creator)
    {
        _safeIStream = underlyingStream;
        access = openAccess;
        backReference = creator;
    }

    //------------------------------------------------------
    //
    //  Protected Methods
    //
    //------------------------------------------------------
    /// <summary>
    /// Dispose(bool)
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                // As per CLR docs for Stream.Close, calls to close on dead stream produces no exceptions.
                if (null != _safeIStream)
                {
                    // No need to call IStream.Commit, commiting at storage root level
                    //  is sufficient.
                    // Can't do Marshal.ReleaseComObject() here because there's only 
                    //  one COM ref for each RCW and there may be other users.
                    _safeIStream = null;
                }
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    //------------------------------------------------------
    //
    //  Private Members
    //
    //------------------------------------------------------
    IStream _safeIStream;
    FileAccess access;

    /// <summary>
    /// If only this stream object is held open, and the rest of the container
    /// has not been explicitly closed, we need this to keep the rest of the
    /// tree open because the CLR GC doesn't realize that our IStream has
    /// a dependency on the rest of the container object tree.
    /// </summary>
    StreamInfo backReference;
}
}

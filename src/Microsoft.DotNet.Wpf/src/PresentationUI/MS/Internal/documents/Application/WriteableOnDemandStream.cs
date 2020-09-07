// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
// This class acts as a type of proxy; it is responsible for forwarding all 
// stream requests to the active stream.  Unlike a proxy it is also controls 
// which stream is active. Initially the active stream is the stream provided
// at construction, at the time of the first write operation the active stream
// is replaced with one provided by a delegate.

using System;
using System.IO;
using System.Windows.TrustUI;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// This class acts as a type of proxy; it is responsible for forwarding all
/// stream requests to the active stream.  Unlike a proxy it is also 
/// controls which stream is active. Initially the active stream is the 
/// stream provided on construction, at the time of the first write 
/// operation the active stream is replaced with one provided by a delegate.
/// </summary>
/// <remarks>
/// As this class only proxies the abstract methods of Stream, if we are
/// proxing a stream that has overriden other virutal methods behavioral
/// inconsistencies may occur.  This could be solved by forwarding all
/// calls.  For internal use it is not currently needed.
/// </remarks>
internal sealed class WriteableOnDemandStream : Stream
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    /// <summary>
    /// Constructs the class with readingStream as the active Stream.
    /// </summary>
    /// <exception cref="System.ArgumentNullException" />
    /// <param name="readingStream">The read only Stream.</param>
    /// <param name="mode">FileMode that will be use to create write 
    /// stream.</param>
    /// <param name="access">FileAccess that will be use to create write
    /// stream.</param>
    /// <param name="writeableStreamFactory">Delegate used to create a 
    /// write stream on first write operation.</param>
    internal WriteableOnDemandStream(
        Stream readingStream,
        FileMode mode,
        FileAccess access,
        GetWriteableInstance writeableStreamFactory)
    {
        if (readingStream == null)
        {
            throw new ArgumentNullException("readingStream");
        }

        if (writeableStreamFactory == null)
        {
            throw new ArgumentNullException("writeableStreamFactory");
        }

        _active = readingStream;
        _mode = mode;
        _access = access;
        _writeableStreamFactory = writeableStreamFactory;
        _wantedWrite = ((_access == FileAccess.ReadWrite)
            || (_access == FileAccess.Write));

    }
    #endregion Constructors

    #region Stream Overrides
    //--------------------------------------------------------------------------
    // Stream Overrides
    //--------------------------------------------------------------------------
    // Due to sub-nesting of pure proxies vs. decorating ones code structure
    // is simplier in this case by grouping Methods & Properies.

    #region Pure Proxies for Stream
    //--------------------------------------------------------------------------
    // This region of code simply follows the proxy patern and only forwards
    // the calls to the active stream.
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="System.IO.Stream.CanRead"/>
    /// </summary>
    public override bool CanRead
    {
        get
        {
            return _active.CanRead;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream.CanSeek"/>
    /// </summary>
    public override bool CanSeek
    {
        get
        {
            return _active.CanSeek;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream.Flush"/>
    /// </summary>
    public override void Flush()
    {
        Trace.SafeWriteIf(
            _isActiveWriteable,
            Trace.Packaging,
            "Flush is being called on read-only stream.");

        if (_isActiveWriteable)
        {
            _active.Flush();
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream.Length"/>
    /// </summary>
    public override long Length
    {
        get
        {
            return _active.Length;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream.Position"/>
    /// </summary>
    public override long Position
    {
        get
        {
            return _active.Position;
        }
        set
        {
            _active.Position = value;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream.Read"/>
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        return _active.Read(buffer, offset, count);
    }

    /// <summary>
    /// <see cref="System.IO.Stream.Seek"/>
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin)
    {
        return _active.Seek(offset, origin);
    }
    #endregion Pure Proxies for Stream

    #region Decorating Proxies for Stream
    //--------------------------------------------------------------------------
    // This region of code follows the decorator patern, the added behavior
    // is the replacement of the active stream with one that is writeable
    // on the first write operation.
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="System.IO.Stream.CanWrite"/>
    /// </summary>
    public override bool CanWrite
    {
        get
        {
            // asking if we can write is not a write operation,
            // however, as the active type had to implement this
            // abstract property thier implementation is the one
            // we should prefer, when we are writable, until then
            // it's more efficent to assume if the user asked for
            // write it will be supported
            if (_isActiveWriteable)
            {
                return _active.CanWrite;
            }
            else
            {
                return _wantedWrite;
            }
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream.SetLength"/>
    /// </summary>
    public override void SetLength(long value)
    {
        EnsureWritable();
        _active.SetLength(value);
    }

    /// <summary>
    /// <see cref="System.IO.Stream.Write"/>
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
        EnsureWritable();
        _active.Write(buffer, offset, count);
    }
    #endregion Decorating Proxies for Stream
    #endregion Stream Overrides

    #region Internal Delegates
    //--------------------------------------------------------------------------
    // Internal Delegates
    //--------------------------------------------------------------------------

    /// <summary>
    /// The delegate is use as a factory to return writable copy of the
    /// stream we were originally given.
    /// </summary>
    /// <param name="mode">The FileMode desired.</param>
    /// <param name="access">The FileAccess desired.</param>
    /// <returns>A writeable Stream.</returns>
    public delegate Stream GetWriteableInstance(
        FileMode mode,
        FileAccess access);
    #endregion Internal Delegates

    #region Protected Methods - Stream Overrides
    //--------------------------------------------------------------------------
    // Protected Methods - Stream Overrides
    //--------------------------------------------------------------------------

    /// <summary>
    /// Ensure we do not aquire new resources.
    /// </summary>
    /// <param name="disposing">Indicates if we are disposing.</param>
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                if (_isActiveWriteable && _active.CanWrite)
                {
                    _active.Flush();
                    // we do not want to dispose of _active
                    // first we are merely a proxy; and being done with
                    // us is not the same as being done with the PackagePart
                    // it supprots, the Package which created _active 
                    // is calls dispose
                }
                _isActiveWriteable = true;
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }
    #endregion Protected Methods - Stream Overrides

    #region Private Methods
    //--------------------------------------------------------------------------
    // Private Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Ensures either the active stream is our writeable stream or uses the
    /// factory delegate to get one and assign it as the active stream.
    /// </summary>
    /// <exception cref="System.IO.IOException" />
    /// <exception cref="System.NotSupportedException" />
    private void EnsureWritable()
    {
        if (!_wantedWrite)
        {
            throw new NotSupportedException(
                SR.Get(SRID.PackagingWriteNotSupported));
        }

        if (!_isActiveWriteable)
        {
            Stream writer = _writeableStreamFactory(_mode, _access);
            if (writer == null)
            {
                throw new IOException(
                    SR.Get(SRID.PackagingWriteableDelegateGaveNullStream));
            }

            if (writer.Equals(this))
            {
                throw new IOException(
                    SR.Get(SRID.PackagingCircularReference));
            }

            writer.Position = _active.Position;

            _active = writer;
            _isActiveWriteable = true;
        }
    }
    #endregion Private Methods

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    /// <summary>
    /// The delegate we were given to have a writeable stream created.
    /// </summary>
    private GetWriteableInstance _writeableStreamFactory;
    /// <summary>
    /// The stream we should be currently using.
    /// </summary>
    private Stream _active;
    /// <summary>
    /// Whether the active stream is writeable.
    /// </summary>
    private bool _isActiveWriteable;
    /// <summary>
    /// The caller's desired FileMode provided on construction.
    /// </summary>
    private FileMode _mode;
    /// <summary>
    /// The caller's desired FileAccess provided on construction.
    /// </summary>
    private FileAccess _access;
    /// <summary>
    /// Calculated using FileAccess, it determines if writing was indicated.
    /// </summary>
    private bool _wantedWrite;
    #endregion Private Methods
}
}

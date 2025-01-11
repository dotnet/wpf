// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//   Implements the Proxy pattern from Design Patterns for Stream.  The intended
//   usage is to control access to the Stream; specifically to allow one to 
//   replace the underlying stream.  The StreamProxy can also ensure, if
//   desired, that the underlying stream is readonly.

using System;
using System.IO;
using System.Windows.TrustUI;

namespace MS.Internal.Documents.Application
{
    /// <summary>
    /// Implements the Proxy pattern from Design Patterns for Stream.  The intended
    /// usage is to control access to the Stream; specifically to allow one to 
    /// replace the underlying stream.  The StreamProxy can also ensure, if
    /// desired, that the underlying stream is readonly.
    /// </summary>
    internal class StreamProxy : Stream
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    /// <summary>
    /// Will construct a StreamProxy backed by the specified stream and leave
    /// the target modifiable.
    /// </summary>
    /// <param name="targetOfProxy">The stream that is backing the proxy.
    /// </param>
    internal StreamProxy(Stream targetOfProxy)
        : this(targetOfProxy, false)
    {
    }

    /// <summary>
    /// Will construct a StreamProxy backed by the specified stream and make
    /// the target read-only if so specified.
    /// </summary>
    /// <param name="targetOfProxy">The stream that is backing the proxy.
    /// </param>
    /// <param name="isTargetReadOnly">Whether or not the target should be set
    /// to read-only.</param>
    internal StreamProxy(Stream targetOfProxy, bool isTargetReadOnly)
    {
        _proxy = targetOfProxy;
        _isTargetReadOnly = isTargetReadOnly;
    }

    #endregion Constructors

    #region Stream Overrides
    //--------------------------------------------------------------------------
    // Stream Overrides
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override bool CanRead
    {
        get { return _proxy.CanRead; }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override bool CanSeek
    {
        get { return _proxy.CanSeek; }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override bool CanTimeout
    {
        get
        {
            return _proxy.CanTimeout;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override bool CanWrite
    {
        get { return _proxy.CanWrite; }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void Close()
    {
        _proxy.Close();
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void Flush()
    {
        _proxy.Flush();
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override long Length
    {
        get { return _proxy.Length; }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override long Position
    {
        get
        {
            return _proxy.Position;
        }
        set
        {
            _proxy.Position = value;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        return _proxy.Read(buffer, offset, count);
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override int ReadTimeout
    {
        get
        {
            return _proxy.ReadTimeout;
        }
        set
        {
            _proxy.ReadTimeout = value;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin)
    {
        return _proxy.Seek(offset, origin);
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void SetLength(long value)
    {
        _proxy.SetLength(value);
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
        _proxy.Write(buffer, offset, count);
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override int WriteTimeout
    {
        get
        {
            return _proxy.WriteTimeout;
        }
        set
        {
            _proxy.WriteTimeout = value;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        try
        {
            // other operations like async methods in
            // our base class call us, we need them to
            // clean up before we release the proxy
            base.Dispose(disposing);
        }
        finally
        {
            if (disposing && _proxy != null)
            {
                _proxy.Dispose();
                _proxy = null;
            }
        }
    }

    #endregion Stream Overrides

    #region Object Overrides
    //--------------------------------------------------------------------------
    // Object Overrides
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="System.Object"/>
    /// </summary>
    public override int GetHashCode()
    {
        return _proxy.GetHashCode();
    }

    /// <summary>
    /// <see cref="System.Object"/>
    /// </summary>
    public override bool Equals(object obj)
    {
        return _proxy.Equals(obj);
    }

    #endregion Object Overrides

    #region Internal Properties
    //--------------------------------------------------------------------------
    // Internal Properties
    //--------------------------------------------------------------------------

    internal Stream Target
    {
        get { return _proxy; }

        set {
            if (!_isTargetReadOnly)
            {
                _proxy = value;
            }
            else
            {
                throw new InvalidOperationException(
                    SR.FileManagementStreamProxyIsReadOnly);
            }
        }
    }
        #endregion Internal Properties

        #region Private Fields
        //--------------------------------------------------------------------------
        // Private Fields
        //--------------------------------------------------------------------------

        private Stream _proxy;
        private bool _isTargetReadOnly;

    #endregion Private Fields
}
}

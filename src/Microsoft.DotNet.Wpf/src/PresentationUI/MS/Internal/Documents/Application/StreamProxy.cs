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
using System.Security;
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
        _proxy.Value = targetOfProxy;
        _isTargetReadOnly.Value = isTargetReadOnly;
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
        get { return _proxy.Value.CanRead; }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override bool CanSeek
    {
        get { return _proxy.Value.CanSeek; }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override bool CanTimeout
    {
        get
        {
            return _proxy.Value.CanTimeout;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override bool CanWrite
    {
        get { return _proxy.Value.CanWrite; }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void Close()
    {
        _proxy.Value.Close();
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void Flush()
    {
        _proxy.Value.Flush();
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override long Length
    {
        get { return _proxy.Value.Length; }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override long Position
    {
        get
        {
            return _proxy.Value.Position;
        }
        set
        {
            _proxy.Value.Position = value;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        return _proxy.Value.Read(buffer, offset, count);
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override int ReadTimeout
    {
        get
        {
            return _proxy.Value.ReadTimeout;
        }
        set
        {
            _proxy.Value.ReadTimeout = value;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin)
    {
        return _proxy.Value.Seek(offset, origin);
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void SetLength(long value)
    {
        _proxy.Value.SetLength(value);
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
        _proxy.Value.Write(buffer, offset, count);
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override int WriteTimeout
    {
        get
        {
            return _proxy.Value.WriteTimeout;
        }
        set
        {
            _proxy.Value.WriteTimeout = value;
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
            if (disposing && _proxy.Value != null)
            {
                _proxy.Value.Dispose();
                _proxy.Value = null;
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
        return _proxy.Value.GetHashCode();
    }

    /// <summary>
    /// <see cref="System.Object"/>
    /// </summary>
    public override bool Equals(object obj)
    {
        return _proxy.Value.Equals(obj);
    }

    #endregion Object Overrides

    #region Internal Properties
    //--------------------------------------------------------------------------
    // Internal Properties
    //--------------------------------------------------------------------------

    internal Stream Target
    {
        get { return _proxy.Value; }

        set {
            if (!_isTargetReadOnly.Value)
            {
                _proxy.Value = value;
            }
            else
            {
                throw new InvalidOperationException(
                    SR.Get(SRID.FileManagementStreamProxyIsReadOnly));
            }
        }
    }
    #endregion Internal Properties

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    SecurityCriticalDataForSet<Stream> _proxy;
    SecurityCriticalDataForSet<bool> _isTargetReadOnly;

    #endregion Private Fields
}
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Implements the Decorator pattern from Design Patterns for Stream

using System;
using System.IO;
using System.Security;
using System.Windows.TrustUI;

using SR=System.Windows.TrustUI.SR;
using SRID=System.Windows.TrustUI.SRID;

namespace MS.Internal.Documents.Application
{

internal sealed class RightsManagementSuppressedStream : StreamProxy
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    /// <summary>
    /// Will construct a RightsManagementSuppressedStream backed by the
    /// specified stream.
    /// </summary>
    /// <param name="targetOfProxy">The stream that is backing the proxy.
    /// </param>
    /// <param name="isWriteAllowed">Whether or not callers are allowed to
    /// write to the stream</param>
    internal RightsManagementSuppressedStream(Stream targetOfProxy, bool isWriteAllowed)
        : base(targetOfProxy, true)
    {
        _allowWrite.Value = isWriteAllowed;
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
        get
        {
            return base.CanRead;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override bool CanWrite
    {
        get
        {
            if (!AllowWrite)
            {
                return false;
            }

            return base.CanWrite;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void Close()
    {
        base.Close();
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void Flush()
    {
        base.Flush();
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override long Length
    {
        get
        {
            return base.Length;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override long Position
    {
        get
        {
            return base.Position;
        }

        set
        {
            base.Position = value;
        }
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        return base.Read(buffer, offset, count);
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin)
    {
        return base.Seek(offset, origin);
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void SetLength(long value)
    {
        ThrowIfReadOnly();

        base.SetLength(value);
    }

    /// <summary>
    /// <see cref="System.IO.Stream"/>
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfReadOnly();

        base.Write(buffer, offset, count);
    }

    #endregion Stream Overrides

    #region Private Methods
    //--------------------------------------------------------------------------
    // Private Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Throws an appropriate exception if writing to the stream is not allowed.
    /// </summary>
    private void ThrowIfReadOnly()
    {
        if (!AllowWrite)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.RightsManagementExceptionNoRightsForOperation));
        }
    }

    #endregion Private Methods

    #region Private Properties
    //--------------------------------------------------------------------------
    // Private Properties
    //--------------------------------------------------------------------------
    
    /// <summary>
    /// Returns whether or not the current RM permission set allows writing to
    /// the stream.
    /// </summary>
    private bool AllowWrite
    {
        get
        {
            return _allowWrite.Value;
        }
    }

    #endregion Private Properties

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    /// <summary>
    /// Whether or not the proxy should enforce that the stream is read-only.
    /// </summary>
    private SecurityCriticalDataForSet<bool> _allowWrite;

    #endregion Private Fields
}
}

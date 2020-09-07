// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Extends Document with a support for StreamProxy versus simply stream.


using System.IO;
using System.Security;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Extends Document with a support for StreamProxy versus simply stream.
/// </summary>
/// <typeparam name="T">The type of stream to back the document with.
/// </typeparam>
internal class StreamDocument<T> : Document where T : StreamProxy
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    internal StreamDocument(Document dependency)
        : base(dependency) { }

    #endregion Constructors

    #region Internal Properties
    //--------------------------------------------------------------------------
    // Internal Properties
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.Document"/>
    /// </summary>
    internal override Stream Destination
    {
        get { return _destination.Value; }
    }
    
    /// <summary>
    /// The T that is backing the Destination stream.
    /// </summary>
    internal T DestinationProxy
    {
        get { return _destination.Value; }
        set { _destination.Value = value; }
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.Document"/>
    /// </summary>
    internal override Stream Source
    {
        get { return _source.Value; }
    }

    /// <summary>
    /// The T that is backing the Source stream.
    /// </summary>
    internal T SourceProxy
    {
        get { return _source.Value; }
        set { _source.Value = value; }
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.Document"/>
    /// </summary>
    internal override Stream Workspace
    {
        get { return _workspace.Value; }
    }

    /// <summary>
    /// The T that is backing the Workspace stream.
    /// </summary>
    internal T WorkspaceProxy
    {
        get { return _workspace.Value; }
        set { _workspace.Value = value; }
    }

    #endregion Internal Properties

    #region Protected Methods
    //--------------------------------------------------------------------------
    // Protected Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Will close streams in the reverse order of intended creation.
    /// </summary>
    protected void ReleaseStreams()
    {
        try
        {
            // closing in revers order of creation
            if (DestinationProxy != null)
            {
                if (DestinationProxy == SourceProxy)
                {
                    SourceProxy = null;
                }
                DestinationProxy.Close();
                DestinationProxy = null;
            }
        }
        finally
        {
            try
            {
                if (WorkspaceProxy != null)
                {
                    WorkspaceProxy.Close();
                    WorkspaceProxy = null;
                }
            }
            finally
            {
                if (SourceProxy != null)
                {
                    SourceProxy.Close();
                    SourceProxy = null;
                }
            }
        }
    }

    #endregion Protected Methods

    #region IDisposable Members
    //--------------------------------------------------------------------------
    // IDisposable Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.Document"/>
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                ReleaseStreams();
            }
        }
        finally
        {
            base.Dispose(true);
        }
    }
    #endregion IDisposable Members

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    private SecurityCriticalDataForSet<T> _destination =
        new SecurityCriticalDataForSet<T>();
    private SecurityCriticalDataForSet<T> _source =
        new SecurityCriticalDataForSet<T>();
    private SecurityCriticalDataForSet<T> _workspace =
        new SecurityCriticalDataForSet<T>();
    #endregion Private Fields
}
}

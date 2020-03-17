// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  Extends StreamDocument with CriticalFileTokens for use by FileController
//  FilePresentation and DocumentStream.

using System;
using System.IO;
using System.Security;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Extends StreamDocument with CriticalFileTokens for use by FileController
/// FilePresentation and DocumentStream.
/// </summary>
internal class FileDocument : StreamDocument<DocumentStream>
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

#if DRT
    /// <summary>
    /// Constructs a FileDocument allowing for a dependency.  Null can be used
    /// if there are none.
    /// </summary>
    /// <param name="dependency">The Document this object depends on.</param>
    internal FileDocument(Document dependency)
        : base(dependency) { }
#endif

    /// <summary>
    /// Will construct a FileDocument with no dependency.
    /// </summary>
    /// <param name="fileToken">The file token to use for this document.</param>
    public FileDocument(CriticalFileToken fileToken)
        : base(null)
    {
        _sourceToken = fileToken;
    }

    /// <summary>
    /// Will construct a FileDocument with no dependency.
    /// </summary>
    /// <param name="existing">The existing stream to use for this document.
    /// </param>
    public FileDocument(Stream existing)
        : base(null)
    {
        SourceProxy = DocumentStream.Open(existing);
    }

    #endregion Constructors

    #region Internal Properties
    //--------------------------------------------------------------------------
    // Internal Properties
    //--------------------------------------------------------------------------

    /// <summary>
    /// Returns the file token to use for saving this document.
    /// </summary>
    internal CriticalFileToken DestinationToken
    {
        get { return _destinationToken; }
        set { _destinationToken = value; }
    }

    /// <summary>
    /// Returns the file token to use for opening this document.
    /// </summary>
    internal CriticalFileToken SourceToken
    {
        get { return _sourceToken; }
    }

    /// <summary>
    /// When true the source and destination should be swapped on SaveCommit.
    /// <seealso cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    internal bool SwapDestination
    {
        get { return _swapFile; }
        set { _swapFile = value; }
    }

    #endregion Internal Properties

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    /// <summary>
    /// The comparee document we will save to.
    /// </summary>
    private CriticalFileToken _destinationToken;

    /// <summary>
    /// The source document we represent.
    /// </summary>
    private CriticalFileToken _sourceToken;

    private bool _swapFile;
    #endregion Private Fields
}
}

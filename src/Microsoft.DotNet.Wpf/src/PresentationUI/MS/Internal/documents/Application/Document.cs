// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  Defines the common contract for all underlying documents in XpsViewer.

using System;
using System.IO;
using System.Security;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Defines the common contract for all underlying documents in XpsViewer.
/// </summary>
/// <remarks>
/// Responsibility:
/// This class imposes a contract which provides for:
/// 
///  - chaining dependencies
///  - expose stream which providers may manipulate at each level
///  - disposing of resources in order of dependency
/// 
/// Design Comments:
/// Packages are dependent on EncryptedPackages who are dependent on FileStreams
/// however all these classes are very different in function.  However they
/// share a need for understanding dependencies and all use streams (some
/// consuming, some publshing and others both).
/// 
/// Providing a chain ensures dependent operations are executed in order.
/// 
/// The design of exsiting components also requires us to define three common
/// types of streams (Source - the original data, Workspace - a type of change
/// log, Destination - the place to save changes).
/// 
/// Examples:
///  - FileController need to substitue streams as as we can not edit in
///    place and may not be able to re-open files on some OSes (hence 
///    IsRebindNeed).
/// 
///  - Protected documents need to decrypt the underlying FileStream before
///    passing them up to the PackageDocument. (hence Source).
/// 
///  - As Package does not allow us to discard changes we need a seperate stream
///    for working on packages (hence Workspace).
/// 
///  - When Protected documents have a key change (PublishLicense) they need
///    to read from the source, and write to the destination at the same time
///    (hence Destination & IsFileCopySafe).
/// </remarks>
internal abstract class Document : IChainOfDependenciesNode<Document>, IDisposable
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------
    
    /// <summary>
    /// Constructor allows the chaining of dependencies.
    /// </summary>
    /// <param name="dependency">A document we are dependent on.</param>
    internal Document(Document dependency)
    {
        _dependency = dependency;
    }
    #endregion Constructors

    #region Finalizers
    //--------------------------------------------------------------------------
    // Finalizers
    //--------------------------------------------------------------------------

    ~Document()
    {
#if DEBUG
        // our code should be explicitly disposing this is to catch bad code
        AssertIfNotDisposed();
#endif
        this.Dispose(true);
    }
    #endregion

    #region IChainOfDependenciesNode<Document> Members
    //--------------------------------------------------------------------------
    // IChainOfDependenciesNode<Document> Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IChainOfDependenciesNode<T>"/>
    /// </summary>
    Document IChainOfDependenciesNode<Document>.Dependency
    {
        get
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            return _dependency;
        }
    }
    #endregion IChainOfDependenciesNode<Document> Members

    #region IDisposable Members
    //--------------------------------------------------------------------------
    // IDisposable Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="System.IDisposable"/>
    /// </summary>
    public void Dispose()
    {
        Trace.SafeWrite(
            Trace.File,
            "{0}.Dispose called.",
            this);
#if DEBUG
        Trace.SafeWriteIf(
            !_isDisposed,
            Trace.File,
            "{0} document is being disposed.",
            this);
#endif
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// <see cref="System.IDisposable"/>
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
#if DEBUG
            ThrowIfDisposed();
            _isDisposed = true;
#endif
            if (_dependency != null)
            {
                _dependency.Dispose();
                _dependency = null;
            }
        }
    }
    #endregion IDisposable Members

    #region Internal Properties
    //--------------------------------------------------------------------------
    // Internal Properties
    //--------------------------------------------------------------------------

    /// <summary>
    /// A stream with the data for the source document.
    /// </summary>
    internal abstract Stream Source
    {
        get;
    }

    /// <summary>
    /// A stream to store working information for edits.
    /// </summary>
    internal abstract Stream Workspace
    {
        get;
    }

    /// <summary>
    /// A stream to publish the changes to when saving.
    /// </summary>
    internal abstract Stream Destination
    {
        get;
    }

    /// <summary>
    /// An underlying document we are dependent on.
    /// </summary>
    internal Document Dependency
    {
        get 
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            return _dependency; 
        }
    }

    /// <summary>
    /// When true copying the data at the file level is valid.
    /// </summary>
    internal bool IsFileCopySafe
    {
        get
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            return ChainOfDependencies<Document>.GetLast(this)._isCopySafe; 
        }
        set
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            ChainOfDependencies<Document>.GetLast(this)._isCopySafe = value;
        }
    }

    /// <summary>
    /// When true, the destination file is exactly identical to the source file
    /// byte-for-byte.
    /// </summary>
    internal bool IsDestinationIdenticalToSource
    {
        get
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            return ChainOfDependencies<Document>.GetLast(this)._isDestinationIdenticalToSource.Value;
        }

        set
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            ChainOfDependencies<Document>.GetLast(this)._isDestinationIdenticalToSource.Value = value;
        }
    }

    /// <summary>
    /// When true the source data has changed and we should rebind.
    /// </summary>
    internal bool IsRebindNeeded
    {
        get
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            return ChainOfDependencies<Document>.GetLast(this)._isRebindNeeded;
        }
        set
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            ChainOfDependencies<Document>.GetLast(this)._isRebindNeeded = value;
        }
    }

    /// <summary>
    /// When true the source location has changed and we should reload.
    /// </summary>
    internal bool IsReloadNeeded
    {
        get
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            return ChainOfDependencies<Document>.GetLast(this)._isReloadNeeded;
        }
        set
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            ChainOfDependencies<Document>.GetLast(this)._isReloadNeeded = value;
        }
    }

    /// <summary>
    /// The location of this document.
    /// </summary>
    internal Uri Uri
    {
        get
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            return ChainOfDependencies<Document>.GetLast(this)._uri.Value;
        }

        set
        {
#if DEBUG
            ThrowIfDisposed();
#endif
            ChainOfDependencies<Document>.GetLast(this)._uri = new SecurityCriticalData<Uri>(value);
        }
    }

    #endregion Internal Properties

    #region Private Methods
    //--------------------------------------------------------------------------
    // Private Methods
    //--------------------------------------------------------------------------
#if DEBUG
    /// <summary>
    /// Will assert if any documents are left undisposed in the application
    /// domain.
    /// </summary>
    private void AssertIfNotDisposed()
    {
        Invariant.Assert(
            _isDisposed,
            string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                "{0} was not disposed.",
                this));
    }

    /// <summary>
    /// Will throw if the object has been disposed.
    /// </summary>
    /// <exception cref="System.ObjectDisposedException"/>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(this.GetType().Name);
        }
    }
#endif
    #endregion Private Methods

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    private SecurityCriticalData<Uri> _uri;

    private bool _isCopySafe = true;

    private SecurityCriticalDataForSet<bool> _isDestinationIdenticalToSource;
    private bool _isRebindNeeded;
    private bool _isReloadNeeded;

    private Document _dependency;

#if DEBUG
    private bool _isDisposed;
#endif
    #endregion Private Fields
}
}

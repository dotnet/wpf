// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  This class acts as a type of proxy; it is responsible for forwarding all 
//  PackagePart requests to the active PackagePart.  Unlike a proxy it controls
//  which part is active.  Initially the active part is the stream provided at
//  construction, at the time of the first write operation the active part is
//  replaced with one provided by a delegate.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Text;
using System.Windows.TrustUI;

using MS.Internal.IO;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// This class acts as a type of proxy; it is responsible for forwarding all
/// PackagePart requests to the active PackagePart.  Unlike a proxy it is 
/// controls which part is active.  Initially the active part is the stream
/// provided on construction, at the time of the first write operation the 
/// active part is replaced with one provided by a delegate.The class will 
/// get a writing PackagePart only when needed.
/// </summary>
/// <remarks>
/// When GetStreamCore is called instead of returning the stream directly we
/// return a WriteableOnDemandStream a proxy.  We provide this stream proxy
/// a read only stream from the active part and a factory method for 
/// creating a writeable stream.  
/// 
/// When the WriteableStreamFactory method is called, if our active part is
/// writable we simply forward a GetStream request to our active part.
/// 
/// Our active part becomes writeable on the first call to our 
/// WriteableStreamFactory method, we use the delegate provide at our 
/// construction to get a writeable part and set it as the active part.
/// 
/// Currently this is the only way our active part becomes writeable.
/// </remarks>
internal sealed class WriteableOnDemandPackagePart : PackagePart
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    /// <summary>
    /// Constructs the class with readingPart as the active PackagePart.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"/>
    /// <param name="container">The Package the PackagePart belongs to.
    /// </param>
    /// <param name="readingPart">The read only PackagePart.</param>
    /// <param name="writeablePartFactory">The delegate used to create a 
    /// writing part when needed.</param>
    internal WriteableOnDemandPackagePart(
        Package container,
        PackagePart readingPart,
        WriteablePackagePartFactoryDelegate writeablePartFactory)
        : base(container, readingPart.Uri)
    {
        if (container == null)
        {
            throw new ArgumentNullException("container");
        }

        if (readingPart == null)
        {
            throw new ArgumentNullException("readingPart");
        }

        if (writeablePartFactory == null)
        {
            throw new ArgumentNullException("writeablePartFactory");
        }

        _activePart = readingPart;
        _getWriteablePartInstance = writeablePartFactory;
    }
    #endregion Constructors

    #region Internal Properties
    //--------------------------------------------------------------------------
    // Internal Properties
    //--------------------------------------------------------------------------

    /// <summary>
    /// Sets the target PackagePart for this proxy.
    /// </summary>
    internal PackagePart Target
    {
        set
        {
            _activePart = value;
            _isActiveWriteable = false;
        }
    }

    #endregion Internal Properties

    #region Internal Delegates
    //--------------------------------------------------------------------------
    // Internal Delegates
    //--------------------------------------------------------------------------

    /// <summary>
    /// This a delegate to get a writeable PackagePart. 
    /// </summary>
    /// <param name="requestor">The active part that is requesting the 
    /// writeable part.</param>
    /// <returns>A writable PackagePart.</returns>
    internal delegate PackagePart WriteablePackagePartFactoryDelegate(
        PackagePart requestor);
    #endregion Internal Delegates

    #region Protected Methods - PackagePart Overrides
    //--------------------------------------------------------------------------
    // Protected Methods - PackagePart Overrides
    //--------------------------------------------------------------------------

    #region Pure Proxies for PackagePart
    //--------------------------------------------------------------------------
    // This region of code simply follows the proxy patern and only forwards
    // the calls to the active part.
    //--------------------------------------------------------------------------

    /// <summary>
    /// Gets the content type of this PackagePart.
    /// </summary>
    /// <returns>The content type of this PackagePart.</returns>
    protected override string GetContentTypeCore()
    {
        return _activePart.ContentType;
    }
    #endregion Pure Proxies for PackagePart

    #region Decorating Proxies for PackagePart
    //--------------------------------------------------------------------------
    // This region of code performs other actions before forwarding
    // to the active part.
    //--------------------------------------------------------------------------

    /// <summary>
    /// Returns a proxy to a stream.
    /// </summary>
    /// <param name="mode">Desired FileMode.</param>
    /// <param name="access">Desired FileAccess.</param>
    /// <returns>A stream.</returns>
    protected override System.IO.Stream GetStreamCore(
        System.IO.FileMode mode,
        System.IO.FileAccess access)
    {
        Trace.SafeWrite(
            Trace.Packaging,
            "Creating a proxy stream for {0}#{1} for {2} access",
            _activePart.Uri,
            _activePart.GetHashCode(),
            access);

        // we do not need to wory about cleaning up this proxy
        // when we are closed our base class handles it
        return new WriteableOnDemandStream(
            _activePart.GetStream(FileMode.Open, FileAccess.Read),
            mode,
            access,
            WriteableStreamFactory);
    }
    #endregion Decorating Proxies for PackagePart

    #endregion Protected Methods - PackagePart Overrides

    #region Private Methods
    //--------------------------------------------------------------------------
    // Private Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// This will return a writable stream with the desired access.
    /// </summary>
    /// <exception cref="System.IO.IOException" />
    /// <remarks>
    /// Regardless of mode & access a writable stream is returned.
    /// </remarks>
    /// <param name="mode">Desired FileMode.</param>
    /// <param name="access">Desired FileAccess.</param>
    /// <returns>A writeable stream.</returns>
    private Stream WriteableStreamFactory(
        FileMode mode,
        FileAccess access)
    {
        if (!_isActiveWriteable)
        {
            Trace.SafeWrite(
                Trace.Packaging,
                "Creating a writeable stream for {0} with {1} access",
                _activePart.Uri,
                access);

            PackagePart writingPart = _getWriteablePartInstance(this);
            if (writingPart == null)
            {
                throw new IOException(
                    SR.Get(SRID.PackagingWriteableDelegateGaveNullPart));
            }

            if (writingPart.Equals(this))
            {
                throw new IOException(
                    SR.Get(SRID.PackagingCircularReference));
            }

            _activePart = writingPart;
            _isActiveWriteable = true;
        }

        return _activePart.GetStream(mode, access);
    }
    #endregion Private Methods

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    /// <summary>
    /// The current comparee of this proxy.
    /// </summary>
    private PackagePart _activePart;
    /// <summary>
    /// Is the active part a writing part?
    /// </summary>
    private bool _isActiveWriteable;
    /// <summary>
    /// Will return a part we can use for writing.
    /// </summary>
    private WriteablePackagePartFactoryDelegate _getWriteablePartInstance;

    #endregion Private Fields
}
}

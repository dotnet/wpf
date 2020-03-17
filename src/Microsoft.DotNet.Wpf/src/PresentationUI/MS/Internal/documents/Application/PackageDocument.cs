// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Extends Document with a single member TrancationalPackage.

using System;
using System.IO;
using System.IO.Packaging;
using System.Security;

using MS.Internal.PresentationUI;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Extends Document with a single member TrancationalPackage.
/// </summary>
[FriendAccessAllowed]
internal class PackageDocument : Document
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    internal PackageDocument(Document dependency)
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
        get
        {
            Invariant.Assert(Dependency != null);
            return Dependency.Destination;
        }
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.Document"/>
    /// </summary>
    internal override Stream Source
    {
        get
        {
            Invariant.Assert(Dependency != null);
            return Dependency.Source;
        }
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.Document"/>
    /// </summary>
    internal override Stream Workspace
    {
        get
        {
            Invariant.Assert(Dependency != null);
            return Dependency.Workspace;
        }
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.Document"/>
    /// </summary>
    internal TransactionalPackage Package
    {
        get { return _package.Value; }

        set { _package.Value = value; }
    }
    #endregion Internal Properties

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
                if (Package != null)
                {
                    Package.Close();
                    Package = null;
                }
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }
    #endregion IDisposable Members

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------
    private SecurityCriticalDataForSet<TransactionalPackage> _package;
    #endregion Private Fields
}
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.IO;
using System.IO.Packaging;
using System.Security;
using System.Security.RightsManagement;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Extends StreamDocument with EncryptedPackageEnvelope for use by RightsController.
/// </summary>
internal class RightsDocument : StreamDocument<StreamProxy>
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    /// <summary>
    /// Constructs a FileDocument allowing for a dependency.
    /// </summary>
    /// <param name="dependency">The Document this object depends on.</param>
    internal RightsDocument(Document dependency)
        : base(dependency) { }
    #endregion Constructors

    #region Internal Methods
    //--------------------------------------------------------------------------
    // Internal Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Returns true when the destination stream is an encrypted package envelope.
    /// </summary>
    internal bool IsDestinationProtected()
    {
        if (DestinationPackage != null)
        {
            return true;
        }

        Stream destinationStream = this.Dependency.Destination;

        return EncryptedPackageEnvelope.IsEncryptedPackageEnvelope(
                destinationStream);
    }

    /// <summary>
    /// Returns true when the source stream is an encrypted package envelope.
    /// </summary>
    internal bool IsSourceProtected()
    {
        if (SourcePackage != null)
        {
            return true;
        }

        Stream sourceStream = this.Dependency.Source;

        return EncryptedPackageEnvelope.IsEncryptedPackageEnvelope(
                sourceStream);
    }
    #endregion Internal Methods

    #region Internal Properties
    //--------------------------------------------------------------------------
    // Internal Properties
    //--------------------------------------------------------------------------

    /// <summary>
    /// The EncryptedPackageEnvelope used for the destination document.
    /// This value may be null when the document is not rights protected.
    /// </summary>
    internal EncryptedPackageEnvelope DestinationPackage
    {
        get { return _destination.Value; }

        set { _destination.Value = value; }
    }

    /// <summary>
    /// The EncryptedPackageEnvelope used for the source document.
    /// This value may be null when the document is not rights protected.
    /// </summary>
    internal EncryptedPackageEnvelope SourcePackage
    {
        get { return _source.Value; }

        set { _source.Value = value; }
    }

    /// <summary>
    /// The EncryptedPackageEnvelope used for the working document.
    /// This value may be null when the document is not rights protected.
    /// </summary>
    internal EncryptedPackageEnvelope WorkspacePackage
    {
        get { return _workspace.Value; }

        set { _workspace.Value = value; }
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
                // our base is StreamDocument, as these packages support
                // the stream we want our base to release them first
                ReleaseStreams();

                // The only code that actually requires this assert are the
                // calls to Close. Regardless I've put the assert around the
                // whole block since the rest of the code under it is almost
                // all just checking packages for null or setting them to null.
                // This is much cleaner than having three separate asserts (and
                // three more try/finally blocks) for each Close call.
                try
                {
                    if (DestinationPackage != null)
                    {
                        if (DestinationPackage == SourcePackage)
                        {
                            SourcePackage = null;
                        }
                        DestinationPackage.Close();
                        DestinationPackage = null;
                    }
                }
                finally
                {
                    try
                    {
                        if (WorkspacePackage != null)
                        {
                            WorkspacePackage.Close();
                            WorkspacePackage = null;
                        }
                    }
                    finally
                    {
                        if (SourcePackage != null)
                        {
                            SourcePackage.Close();
                            SourcePackage = null;
                        }
                    }
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

    private SecurityCriticalDataForSet<EncryptedPackageEnvelope> _source;

    private SecurityCriticalDataForSet<EncryptedPackageEnvelope> _workspace;

    private SecurityCriticalDataForSet<EncryptedPackageEnvelope> _destination;
    #endregion Private Fields
}
}

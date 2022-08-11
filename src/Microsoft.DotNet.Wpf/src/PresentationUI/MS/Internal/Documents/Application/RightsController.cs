// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Responsible for the lifecycle of the RightsDocument and the actions that can
//              be performed on it.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Security;
using System.Security.RightsManagement;
using System.Windows;
using System.Windows.TrustUI; // for SR
using MS.Internal.IO.Packaging.CompoundFile;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Responsible for the lifecycle of the RightsDocument and the actions that can
/// be performed on it.
/// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
/// </summary>
/// <remarks>
/// All IDocumentController methods are expected to throw if provided
/// a document that is not a RightsDocument.  Users of the IDocumentController
/// interface are expected to use the IChainOfResponsibiltyNode method before
/// calling into the IDocumentController methods to avoid runtime errors.
/// </remarks>
class RightsController : IDocumentController, IDisposable
{
    #region IDocumentController Members
    //--------------------------------------------------------------------------
    // IDocumentController Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.EnableEdit(Document document)
    {
        RightsDocument doc = (RightsDocument)document; // see class remarks on why this is ok
        Stream ciphered = doc.Dependency.Workspace;
        Stream clear = ciphered;

        bool canEdit =
            DocumentRightsManagementManager.Current.HasPermissionToEdit;

        EncryptedPackageEnvelope encryptedPackage = null;

        // If editing the document is allowed (i.e. the document is either
        // not RM protected, or the user has permission to edit it), create
        // a temporary editing file
        if (canEdit)
        {
            try
            {
                encryptedPackage =
                    _provider.Value.EncryptPackage(ciphered);

                if (encryptedPackage != null)
                {
                    clear = DecryptEnvelopeAndSuppressStream(
                        encryptedPackage,
                        canEdit);

                    doc.WorkspacePackage = encryptedPackage;
                }
            }
            catch (RightsManagementException exception)
            {
                RightsManagementErrorHandler.HandleOrRethrowException(
                    RightsManagementOperation.Other,
                    exception);

                // Bail out
                return true;
            }

            if (encryptedPackage != null)
            {
                Trace.SafeWrite(
                    Trace.Rights,
                    "Editing package is RM protected.");
            }
            else
            {
                Trace.SafeWrite(
                    Trace.Rights,
                    "Editing package is unprotected.");
            }

            doc.WorkspaceProxy = new StreamProxy(clear);
        }
        else
        {
            Trace.SafeWrite(
                Trace.Rights,
                "Did not create editing package because user does not have permission to edit.");
        }

        return canEdit;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.Open(Document document)
    {
        RightsDocument doc = (RightsDocument)document; // see class remarks on why this is ok
        Stream ciphered = doc.Dependency.Source;
        Stream clear = ciphered;
        bool isSourceProtected = doc.IsSourceProtected();

        if (isSourceProtected)
        {
            // Do not catch exceptions here - there can be no mitigation
            EncryptedPackageEnvelope envelope = OpenEnvelopeOnStream(ciphered);
            PackageProperties properties = new SuppressedProperties(envelope);

            doc.SourcePackage = envelope;
            DocumentProperties.Current.SetRightsManagedProperties(properties);
        }

        RightsManagementProvider provider =
            new RightsManagementProvider(doc.SourcePackage);
        _provider.Value = provider;

        try
        {
            DocumentRightsManagementManager.Initialize(provider);

            DocumentRightsManagementManager.Current.PublishLicenseChange +=
                new EventHandler(delegate(object sender, EventArgs args)
                    {
                        Trace.SafeWrite(
                            Trace.Rights,
                            "Disabling file copy for current document.");
                        doc.IsFileCopySafe = false;

                        DocumentManager.CreateDefault().EnableEdit(null);
                    });

            if (isSourceProtected)
            {
                clear = DocumentRightsManagementManager.Current.DecryptPackage();

                if (clear != null)
                {
                    clear = new RightsManagementSuppressedStream(
                        clear,
                        DocumentRightsManagementManager.Current.HasPermissionToEdit);

                    // Reset the position of the stream since GetPackageStream will
                    // create a package and move the stream pointer somewhere else
                    clear.Position = 0;
                }
                else
                {
                    Trace.SafeWrite(
                        Trace.Rights,
                        "You do not have rights for the current document.");

                    return false;
                }
            }
        }
        catch
        {
            // If anything failed here, we cannot use the provider any longer,
            // so we can dispose it
            provider.Dispose();
            _provider.Value = null;
            throw;
        }

        if (clear != null)
        {
            doc.SourceProxy = new StreamProxy(clear);
        }
        else
        {
            // If decryption failed, we can no longer do anything with the
            // provider instance or the current RM manager
            provider.Dispose();
            _provider.Value = null;
        }

        return true;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.Rebind(Document document)
    {
        RightsDocument doc = (RightsDocument)document; // see class remarks on why this is ok
        Stream ciphered = doc.Dependency.Source;
        Stream clear = ciphered;

        if (doc.IsRebindNeeded)
        {
            if (doc.SourcePackage != null)
            {
                CloseEnvelope(doc.SourcePackage);
                doc.SourcePackage = null;
            }

            EncryptedPackageEnvelope envelope = null;
            PackageProperties properties = null;
            bool isSourceProtected = doc.IsSourceProtected();

            if (isSourceProtected)
            {
                envelope = OpenEnvelopeOnStream(ciphered);
                doc.SourcePackage = envelope;

                properties = new SuppressedProperties(envelope);
            }

            DocumentProperties.Current.SetRightsManagedProperties(properties);
            DocumentRightsManagementManager.Current.SetEncryptedPackage(envelope);

            if (isSourceProtected)
            {
                clear = DocumentRightsManagementManager.Current.DecryptPackage();

                if (clear != null)
                {
                    clear = new RightsManagementSuppressedStream(
                        clear,
                        DocumentRightsManagementManager.Current.HasPermissionToEdit);

                    // Reset the position of the stream since GetPackageStream will
                    // create a package and move the stream pointer somewhere else
                    clear.Position = 0;
                }
                else
                {
                    Trace.SafeWrite(
                        Trace.Rights,
                        "You do not have rights for the current document.");

                    return false;
                }
            }

            doc.SourceProxy.Target = clear;
        }

        return true;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.SaveAsPreperation(Document document)
    {
        if (!DocumentRightsManagementManager.Current.HasPermissionToSave)
        {
            Trace.SafeWrite(Trace.Rights,
                "Can not save a package when we do not have permission.");

            return false;
        }

        return ((IDocumentController)this).SavePreperation(document);
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.SaveCommit(Document document)
    {
        RightsDocument doc = (RightsDocument)document; // see class remarks on why this is ok

        if (doc.DestinationPackage != null)
        {
            CloseEnvelope(doc.DestinationPackage);
            doc.DestinationPackage = null;

            Trace.SafeWrite(Trace.File, "Destination EncryptedPackageEnvelope closed.");
        }

        return true;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.SavePreperation(Document document)
    {
        bool handled = false;

        RightsDocument doc = (RightsDocument)document; // see class remarks on why this is ok
        Stream ciphered = doc.Dependency.Destination;
        Stream clear = ciphered;

        // We protect the actual decrypted content (if necessary) using the
        // RightsManagementSuppressedStream that prevents callers from writing
        // to it. This still allows us to save an acquired use license back to
        // the package even if the user does not have the permissions to modify
        // the document itself. It also means that there is no need to check if
        // the user has permission to save before starting the operation.

        // If we are encrypted and the destination stream is identical to the
        // source stream, there is no need to create a new envelope. Instead we
        // can simply open the envelope on the destination stream, which
        // already contains the complete encrypted envelope that was the source
        // file.
        //
        // It doesn't actually matter to this class why the flag was set, but
        // we know the destination is identical to the source in two situations:
        //  1) The source and the destination are the same file, and since the
        //     publish license didn't change there is no need to write to a
        //     temporary file first
        //  2) The destination file was copied directly from the source file.
        if (doc.IsDestinationProtected() && doc.IsDestinationIdenticalToSource)
        {
            // we can't reuse protections because EncryptedPackage caches the
            // original permissions for the stream (if we re-open the package r/w
            // the Encrypted package will still be r/o)

            doc.DestinationPackage = OpenEnvelopeOnStream(
                doc.Dependency.Destination);

            doc.DestinationPackage.RightsManagementInformation.CryptoProvider = 
                doc.SourcePackage.RightsManagementInformation.CryptoProvider;

            clear = DecryptEnvelopeAndSuppressStream(
                doc.DestinationPackage,
                DocumentRightsManagementManager.Current.HasPermissionToEdit);
            doc.DestinationProxy = new StreamProxy(clear);

            // save the use license in case the user acquired one
            _provider.Value.SaveUseLicense(doc.DestinationPackage);

            handled = true;

            Trace.SafeWrite(
                Trace.Rights,
                "Reused CryptoProvider as underlying stream is the same.");
        }
        else // we are not protected and/or the RM protections have changed
        {
            bool canEdit =
                DocumentRightsManagementManager.Current.HasPermissionToEdit;

            // canEdit should always be true here - either the document is not
            // protected, or the protections have been changed.  In the latter
            // case, the user has to have Owner permissions to change the
            // protections, and that of course includes Edit permissions.
            Invariant.Assert(
                canEdit,
                "Cannot save with changes if Edit permission was not granted.");

            EncryptedPackageEnvelope encryptedPackage =
                _provider.Value.EncryptPackage(ciphered);

            // the destination is intended to be encrypted when a non-null
            // value is returned

            if (encryptedPackage != null)
            {
                clear = DecryptEnvelopeAndSuppressStream(
                    encryptedPackage,
                    canEdit);
                doc.DestinationPackage = encryptedPackage;
            }

            Trace.SafeWriteIf(
                encryptedPackage == null,
                Trace.Rights,
                "Destination package is unprotected.");

            doc.DestinationProxy = new StreamProxy(clear);
            
            // If the destination file is not identical to the source file, we
            // need to copy the (possibly decrypted) source stream to the
            // destination here.
            if (!doc.IsDestinationIdenticalToSource)
            {
                StreamHelper.CopyStream(doc.Source, doc.Destination);

                doc.DestinationProxy.Flush();

                Trace.SafeWrite(
                    Trace.Rights,
                    "Copied Source contents to Destination.");
            }

            handled = true;
        }

        return handled;
    }

    #endregion IDocumentController Members

    #region IChainOfResponsibiltyNode<Document> Members
    //--------------------------------------------------------------------------
    // IChainOfResponsibiltyNode<Document> Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IChainOfResponsibiltyNode<T>"/>
    /// </summary>
    bool IChainOfResponsibiltyNode<Document>.IsResponsible(Document subject)
    {
        return subject is RightsDocument;
    }

    #endregion IChainOfResponsibiltyNode<Document> Members

    #region IDisposable Members
    //--------------------------------------------------------------------------
    // IDisposable Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// Dispose
    /// </summary>
    void IDisposable.Dispose()
    {
        IDisposable provider = _provider.Value as IDisposable;

        if (provider != null)
        {
            provider.Dispose();
        }

        _provider.Value = null;
        
        GC.SuppressFinalize(this);
    }

    #endregion IDisposable Members

    #region Private Methods
    //--------------------------------------------------------------------------
    // Private Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Opens an EncryptedPackageEnvelope on a given stream.
    /// </summary>
    /// <param name="ciphered">The encrypted stream</param>
    /// <returns>An EncryptedPackageEnvelope on the stream</returns>
    /// <remarks>
    /// This function exists to centralize the asserts needed to use encrypted
    /// package envelopes.
    /// </remarks>
    private static EncryptedPackageEnvelope OpenEnvelopeOnStream(Stream ciphered)
    {
        return EncryptedPackageEnvelope.Open(ciphered);
    }

    /// <summary>
    /// Retrieves and creates a rights management suppressed stream around the
    /// decrypted stream from a given encrypted package envelope. The stream
    /// returned should not be handed out to untrusted code.
    /// </summary>
    /// <param name="envelope">The envelope to decrypt and suppress</param>
    /// <param name="allowWrite">True if editing the suppressed stream should
    /// be allowed</param>
    /// <returns>The new demand-suppressed stream</returns>
    /// <remarks>
    /// This function exists to centralize the asserts needed to use encrypted
    /// package envelopes.
    /// </remarks>
    private static Stream DecryptEnvelopeAndSuppressStream(
        EncryptedPackageEnvelope envelope,
        bool allowWrite)
    {
        Stream clear = null;

        clear = envelope.GetPackageStream();

        clear = new RightsManagementSuppressedStream(clear, allowWrite);

        // Reset the position of the stream since GetPackageStream will
        // create a package and move the stream pointer somewhere else
        clear.Position = 0;

        return clear;
    }

    /// <summary>
    /// Close the EncryptedPackageEnvelope passed in as an argument.
    /// </summary>
    /// <param name="envelope">The envelope to close</param>
    /// <remarks>
    /// This function exists to centralize the asserts needed to use encrypted
    /// package envelopes.
    /// </remarks>
    private static void CloseEnvelope(EncryptedPackageEnvelope envelope)
    {
        envelope.Close();
    }

    #endregion Private Methods

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    private static SecurityCriticalDataForSet<IRightsManagementProvider> _provider;
    #endregion Private Fields
}
}

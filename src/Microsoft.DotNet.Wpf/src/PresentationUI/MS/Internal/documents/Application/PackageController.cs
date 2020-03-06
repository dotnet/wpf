// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  Responsible for the lifecycle of the PackageDocument and the actions that can
//  be performed on it.

using System;
using System.IO.Packaging;
using System.Security;
using System.Windows.TrustUI;
using System.Windows.Xps.Packaging;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Responsible for the lifecycle of the PackageDocument and the actions that can
/// be performed on it.
/// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
/// </summary>
internal class PackageController : IDocumentController
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
        PackageDocument doc = (PackageDocument)document;

        if (doc.Workspace != null)
        {
            doc.Package.EnableEditMode(doc.Workspace);
        }

        return true;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.Open(Document document)
    {
        PackageDocument doc = (PackageDocument)document;

        TransactionalPackage package = new RestrictedTransactionalPackage(
            doc.Source);

        doc.Package = package;

        DocumentProperties.Current.SetXpsProperties(package.PackageProperties);

        DocumentSignatureManager.Initialize(new DigitalSignatureProvider(package));

        // when signatures change (are added or removed) we can no longer simply copy bits on disk
        DocumentSignatureManager.Current.SignaturesChanged += 
            new EventHandler(
                delegate (
                    object sender, 
                    EventArgs args)
                    {
                        if (doc.IsFileCopySafe)                
                        {
                            Trace.SafeWrite(
                                Trace.Signatures,
                                "Disabling file copy for current document.");
                            doc.IsFileCopySafe = false;
                        }
                    });

        return true;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.Rebind(Document document)
    {
        PackageDocument doc = (PackageDocument)document;

        if (doc.IsRebindNeeded)
        {
            doc.Package.Rebind(doc.Source);

            // no rebinds are needed above us for documents as package
            doc.IsRebindNeeded = false;
        }

        return true;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.SaveAsPreperation(Document document)
    {
        return ((IDocumentController)this).SavePreperation(document);
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// <exception cref="System.InvalidOperationException"/>
    /// </summary>
    bool IDocumentController.SaveCommit(Document document)
    {
        PackageDocument doc = (PackageDocument)document;

        if (doc.Destination == null)
        {
            return false;
        }

        if (doc.Package.IsDirty || !doc.IsDestinationIdenticalToSource)
        {
            StreamProxy source = doc.Source as StreamProxy;
            StreamProxy destination = doc.Destination as StreamProxy;

            // this will catch the case where our source stream is our destination
            // stream, which would cause corruption of the package
            if (source.GetHashCode() == destination.GetHashCode())
            {
                throw new InvalidOperationException(
                    SR.Get(SRID.PackageControllerStreamCorruption));
            }

            // this will catch the case where no one was able to copy the stream
            // thus the lengths will not match
            if (doc.Source.Length != doc.Destination.Length)
            {
                throw new InvalidOperationException(
                    SR.Get(SRID.PackageControllerStreamCorruption));
            }

            // Flush the package to ensure that the relationship parts are
            // written out before changes are merged
            doc.Package.Flush();

            doc.Package.MergeChanges(doc.Destination);
            Trace.SafeWrite(Trace.File, "Destination Merged.");
        }
        else
        {
            Trace.SafeWrite(Trace.File, "Destination Merge Skipped (nothing to do).");
        }
        return true;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    /// <remarks>
    /// This method severely breaks encapsulating the chain, however it does
    /// so because the requirement breaks encapsulation and the original design
    /// of the class only consider streams not properties.  The requirement is
    /// that as we transition to/from a protected document we copy properties
    /// between the layers.  It was least risk to compromise the design here
    /// then extend it.
    /// </remarks>
    bool IDocumentController.SavePreperation(Document document)
    {
        // we don't have to check packageDoc for because we are only responsible
        // for PackageDocuments
        PackageDocument packageDoc = (PackageDocument)document;
        RightsDocument rightsDoc = document.Dependency as RightsDocument;

        if (rightsDoc != null)
        {
            bool isDestinationProtected = rightsDoc.IsDestinationProtected();
            bool isSourceProtected = rightsDoc.IsSourceProtected();

            // the only time we don't need to copy properties is when
            // neither source nor destination is protected as OPC properties
            // are copied as parts

            // if the source was protected and the destination is not
            // then we need to copy properties to the package
            if (isSourceProtected && !isDestinationProtected)
            {
                DocumentProperties.Copy(
                    new SuppressedProperties(rightsDoc.SourcePackage),
                    packageDoc.Package.PackageProperties);
            }

            // if the source was not protected and the destination is
            // then we need to copy properties from the package to the envelope
            if (!isSourceProtected && isDestinationProtected)
            {
                DocumentProperties.Copy(
                    packageDoc.Package.PackageProperties,
                    new SuppressedProperties(rightsDoc.DestinationPackage));
            }

            // if they were both protected we need to copy properties
            // from the old envelope to the new
            if (isDestinationProtected && isSourceProtected)
            {
                DocumentProperties.Copy(
                    new SuppressedProperties(rightsDoc.SourcePackage),
                    new SuppressedProperties(rightsDoc.DestinationPackage));
            }
        }
        return true;
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
        return subject is PackageDocument;
    }

    #endregion IChainOfResponsibiltyNode<Document> Members
}
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  Responsible for the lifecycle of the FileDocument and the actions that can
//  be performed on it.

using System;
using System.IO;
using System.Security;
using MS.Internal.Security; // AttachmentService

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Responsible for the lifecycle of the FileDocument and the actions that can
/// be performed on it.
/// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
/// </summary>
internal class FileController : IDocumentController
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
        FileDocument doc = (FileDocument)document;

        if (doc.WorkspaceProxy == null)
        {
            // Try to obtain an exclusive lock on the source file. If this
            // fails, we can still create a temporary file and Save As to a
            // different location.
            bool canWriteToSource = doc.SourceProxy.ReOpenWriteable();

            DocumentManager documentManager = DocumentManager.CreateDefault();
            
            // We can save to the source file if we could reopen it for write
            if (documentManager != null)
            { 
                documentManager.CanSave = canWriteToSource;
            }

            doc.WorkspaceProxy = doc.SourceProxy.CreateTemporary(false);

            if (doc.WorkspaceProxy == null)
            {
                FilePresentation.ShowNoTemporaryFileAccess();
            }
        }

        return (doc.WorkspaceProxy != null);
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.Open(Document document)
    {
        FileDocument doc = (FileDocument)document;

        if (doc.Source == null)
        {
            try
            {
                doc.SourceProxy = DocumentStream.Open(doc, false);
            }
            catch (UnauthorizedAccessException uae)
            {
                FilePresentation.ShowNoAccessToSource();
                doc.SourceProxy = null;

                Trace.SafeWrite(
                    Trace.File,
                    "Unable to open specified location.\nException: {0}",
                    uae);

                return false;
            }
            catch (IOException ioe)
            {
                FilePresentation.ShowNoAccessToSource();
                doc.SourceProxy = null;

                Trace.SafeWrite(
                    Trace.File,
                    "Unable to open specified location.\nException: {0}",
                    ioe);

                return false;
            }
        }

        DocumentProperties.InitializeCurrentDocumentProperties(doc.Uri);

        return true;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.Rebind(Document document)
    {
        FileDocument doc = (FileDocument)document;
        if (doc.IsRebindNeeded)
        {
            doc.SourceProxy.Close();
            doc.SourceProxy = null;

            try
            {
                doc.SourceProxy = DocumentStream.Open(doc, false);
            }
            catch (UnauthorizedAccessException uae)
            {
                FilePresentation.ShowNoAccessToSource();
                doc.SourceProxy = null;

                Trace.SafeWrite(
                    Trace.File,
                    "Unable to reopen specified location.\nException: {0}",
                    uae);

                return false;
            }
            catch (IOException ioe)
            {
                FilePresentation.ShowNoAccessToSource();
                doc.SourceProxy = null;

                Trace.SafeWrite(
                    Trace.File,
                    "Unable to reopen specified location.\nException: {0}",
                    ioe);

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.SaveAsPreperation(Document document)
    {
        FileDocument doc = (FileDocument)document;

        CriticalFileToken sourceToken = doc.SourceToken;
        CriticalFileToken saveToken = doc.DestinationToken;

        //----------------------------------------------------------------------
        // Get Save Location and Consent (if needed)

        bool haveConsent = false;
        bool cancelSave = false;

        // Loop until we have consent to save to a file or the user cancels
        while (!haveConsent && !cancelSave)
        {
            // If we have a location, check to see if it is read-only
            if (saveToken != null)
            {
                if (DocumentStream.IsReadOnly(saveToken))
                {
                    // If the target file is read-only, we cannot save over it
                    FilePresentation.ShowDestinationIsReadOnly();
                }
                else
                {
                    // If the file isn't read-only, we now have consent to save
                    haveConsent = true;
                }
            }

            if (!haveConsent)
            {
                Trace.SafeWrite(
                    Trace.File, "We don't have a save location, prompting user.");

                // by default set the same file
                if (saveToken == null)
                {
                    saveToken = sourceToken;
                }

                // A false return value indicates that the user wanted to
                // cancel the save
                cancelSave = !FilePresentation.ShowSaveFileDialog(ref saveToken);
            }
            else
            {
                // Otherwise we do have consent to save to the location stored in
                // the saveToken
                doc.DestinationToken = saveToken;
            }
        }

        // Validate: If still don't have consent return without saving
        if (!haveConsent)
        {
            // ensure token is rescinded
            doc.DestinationToken = null;

            Trace.SafeWrite(
                Trace.File,
                "{0} not handling we do not have user consent.",
                this);

            return false;
        }

        bool isFileCopySafe = doc.IsFileCopySafe;

        //----------------------------------------------------------------------
        // Calculate Save Action
        SaveAction action = SaveAction.Unknown;
        if (doc.Source.CanWrite)
        {
            action |= SaveAction.SourceIsWriteable;
        }
        if (sourceToken == saveToken)
        {
            action |= SaveAction.TargetIsSelf;
        }
        if (isFileCopySafe)
        {
            action |= SaveAction.CanCopyData;
        }

        bool isDestinationIdentical = false;

        // Catch IO Exceptions; will return false and clear token
        try
        {
            //----------------------------------------------------------------------
            // Perform Optimal Save Action (see method remarks)
            switch (action)
            {
                // Example: We were successfully able to open the source package
                // for write and there were no RM changes.
                // I/O Cost: Read & SafeWrite Changes
                case SaveAction.TargetIsSelf
                    | SaveAction.SourceIsWriteable
                    | SaveAction.CanCopyData:

                    Trace.SafeWrite(
                        Trace.File,
                        "SaveAction {0}: Updating comparee to self.",
                        action);

                    doc.DestinationProxy = doc.SourceProxy;
                    isDestinationIdentical = true;
                    break;

                // Example: Another user / process had a lock on the file, however
                // we would like to save now and there were no RM changes.
                // I/O Cost: Varies (Read & SafeWrite Changes - Read & SafeWrite Sum)
                case SaveAction.TargetIsSelf | SaveAction.CanCopyData:

                    Trace.SafeWrite(
                        Trace.File,
                        "SaveAction {0}: reopening editable, updating comparee to self.",
                        action);

                    // re-open writeable if possible otherwise copy the file
                    if (doc.SourceProxy.ReOpenWriteable())
                    {
                        doc.DestinationProxy = doc.SourceProxy;
                        isDestinationIdentical = true;
                    }
                    else
                    {
                        Trace.SafeWrite(
                            Trace.File,
                            "SaveAction {0}: creating a temporary document reopen failed.",
                            action);

                        doc.DestinationProxy = doc.SourceProxy.CreateTemporary(true);
                        doc.SwapDestination = true;
                    }
                    break;

                case SaveAction.TargetIsSelf | SaveAction.SourceIsWriteable:
                case SaveAction.TargetIsSelf:
                    Trace.SafeWrite(
                        Trace.File,
                        "SaveAction {0}: creating a temporary document.",
                        action);
                    doc.DestinationProxy = doc.SourceProxy.CreateTemporary(false);
                    doc.SwapDestination = true;
                    break;

                // Example: All other cases, like source is web based.
                // I/O Cost: Max (Read Sum & SafeWrite Sum)
                default:
                    Trace.SafeWrite(
                        Trace.File,
                        "SaveAction {0}: Performing defaults.",
                        action);
                    if (isFileCopySafe)
                    {
                        Trace.SafeWrite(
                            Trace.File,
                            "SaveAction {0}: copying original document.",
                            action);
                        doc.DestinationProxy = doc.SourceProxy.Copy(saveToken);
                        isDestinationIdentical = true;
                    }
                    else
                    {
                        doc.DestinationProxy = DocumentStream.Open(saveToken, true);
                        // ensure we have enough quota to be as large as the sum
                        doc.DestinationProxy.SetLength(
                            doc.SourceProxy.Length +
                            (doc.WorkspaceProxy == null ? 0 : doc.WorkspaceProxy.Length));
                        // set it back as we don't do the copy
                        doc.DestinationProxy.SetLength(0);
                    }
                    doc.SwapDestination = false;
                    break;
            } // switch (action)

            // Set the IsDestinationIdenticalToSource flag on the document
            // depending on what happened above
            doc.IsDestinationIdenticalToSource = isDestinationIdentical;

            return true;
        }
        catch (UnauthorizedAccessException uae)
        {
            FilePresentation.ShowNoAccessToDestination();
            doc.DestinationProxy = null;
            // ensure token is recinded
            doc.DestinationToken = null;

            Trace.SafeWrite(
                Trace.File,
                "SaveAction {0}: unable to open specified location.\nException: {1}",
                action,
                uae);

            return false;
        }
        catch (IOException ioe)
        {
            FilePresentation.ShowNoAccessToDestination();
            doc.DestinationProxy = null;
            // ensure token is recinded
            doc.DestinationToken = null;

            Trace.SafeWrite(
                Trace.File,
                "SaveAction {0}: unable to set size at specified location.\nException: {1}",
                action,
                ioe);

            return false;
        }
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.SaveCommit(Document document)
    {
        bool success = true;
        FileDocument doc = (FileDocument)document;
        bool haveConsent = (doc.DestinationToken != null);

        // Validate: If we have consent, if not return doing nothing.
        if (!haveConsent)
        {
            Trace.SafeWrite(
                Trace.File,
                "{0}.SaveCommit handling but doing nothing we do not have user consent.",
                this);

            return true;
        }

        doc.DestinationProxy.Flush();

        //----------------------------------------------------------------------
        // Swap Files if Needed

        if (doc.SwapDestination)
        {
            if (doc.DestinationProxy.SwapWithOriginal())
            {
                Trace.SafeWrite(
                    Trace.File,
                    "SaveCommit has swapped Destination and Source file.");
            }
            else
            {
                success = false;
                FilePresentation.ShowNoAccessToDestination(); 
                Trace.SafeWrite(
                    Trace.File, 
                    "SaveCommit did not succeed because a needed file swap failed.");
            }

            // we attempted the operation turn off the request
            doc.SwapDestination = false;
        }

        if (success)
        {
            //------------------------------------------------------------------
            // Rebind / Reload the Document

            if (doc.SourceToken == doc.DestinationToken)
            {
                // we changed the underlying data new keys may be required and
                // document policy may need re-evaluating or lost our reference
                doc.IsRebindNeeded = true;

                Trace.SafeWrite(
                    Trace.File,
                    "SaveCommit declaring a rebind is needed.");
            }
            else
            {
                Uri originalUri = doc.Uri;

                // we are a new document and we should be reloaded
                doc.Uri = doc.DestinationToken.Location;
                doc.IsReloadNeeded = true;
                // we are releasing our lock on the destination
                doc.DestinationProxy.Close();
                doc.DestinationProxy = null;

                Trace.SafeWrite(
                    Trace.File,
                    "SaveCommit declaring a reload to {0} is needed.",
                    doc.Uri);

                //------------------------------------------------------------------
                // Add Mark of Web

                // This only needs to be done when the DestinationToken is in a
                // different location from the SourceToken, and the equality
                // operator on CriticalFileToken does properly compare the URI.

                Trace.SafeWrite(
                        Trace.File,
                        "AttachmentService.SaveWithUI from {0} to {1}.",
                        originalUri,
                        doc.DestinationToken.Location);

                AttachmentService.SaveWithUI(
                    IntPtr.Zero, originalUri, doc.DestinationToken.Location);
            }
        }

        // rescinding user consent as save is completed
        doc.DestinationToken = null;
        return success;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.SavePreperation(Document document)
    {
        FileDocument doc = (FileDocument)document;

        doc.DestinationToken = doc.SourceToken;

        return ((IDocumentController)this).SaveAsPreperation(document);
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
        return subject is FileDocument;
    }

    #endregion IChainOfResponsibiltyNode<Document> Members

    /// <summary>
    /// Represents the type of save action to perform.
    /// </summary>
    [Flags]
    private enum SaveAction
    {
        /// <summary>
        /// Unsure what action to take.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The Destination document has the same location as the Source.
        /// </summary>
        TargetIsSelf = 1,
        /// <summary>
        /// The Source document was opened writeable.
        /// </summary>
        SourceIsWriteable = 2,
        /// <summary>
        /// Copying the data at the file level is a safe operation.
        /// </summary>
        CanCopyData = 4
    }
}
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
// Exposes the basic operations that can be performed on documents. (Open,
// EnableEdit, Save)

using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

using MS.Internal.PresentationUI;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Exposes the basic operations that can be performed on documents. (Open,
/// EnableEdit, Save)
/// </summary>
/// <remarks>
/// Responsibility:
/// The class is responsible for delegating the work to the appropriate
/// controller(s) in the order needed based on document dependencies.
/// 
/// Design Comments:
/// Packages are dependent on EncryptedPackages who are dependent on FileStreams
/// however all these classes are very different in function.
/// 
/// By design once a controller has reported handling a document it will not be
/// given to other controllers.  A controller should not see documents they are
/// not in the dependency chain for.
/// 
/// Example: FileController should never see RightsDocument.
/// </remarks>
[FriendAccessAllowed]
internal sealed class DocumentManager 
    : ChainOfResponsiblity<IDocumentController, Document>
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    /// <summary>
    /// Will construct a DocumentManager using the provided controllers.
    /// </summary>
    /// <param name="controllers">Controllers in order of responsiblity from
    /// top to bottom.</param>
    internal DocumentManager(params IDocumentController[] controllers)
        : base(controllers) { }
    #endregion Constructors

    #region Internal Methods
    //--------------------------------------------------------------------------
    // Internal Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Creates the default chain of controllers.  (PackageController, 
    /// RightsController and FileController)
    /// </summary>
    /// <returns>A DocumentManager.</returns>
    /// <remarks>
    /// This exists because of a a design compromise; see SaveAs below.
    ///
    /// This compromise breaks encapsulation, because we have an understanding of
    /// begin used within a navigation application.
    /// </remarks>
    internal static DocumentManager CreateDefault()
    {
        if (_singleton == null)
        {
            _controllers.Add(new HostedController());
            _controllers.Add(new PackageController());
            _controllers.Add(new RightsController());
            _controllers.Add(new FileController());

            _singleton = new DocumentManager(
                _controllers.ToArray());

            Trace.SafeWrite(Trace.File, "DocumentManager singleton created.");
        }

        return _singleton;
    }

    /// <summary>
    /// Creates the default chain of documents.  (PackageDocument, 
    /// RightsDocument and FileDocument)
    /// </summary>
    /// <returns>A Document.</returns>
    internal static PackageDocument CreateDefaultDocument(
        Uri source, CriticalFileToken fileToken)
    {
        // because we have a fileToken we might be able to save
        _canSave = true;

        PackageDocument doc = new PackageDocument(
            new RightsDocument(
            new FileDocument(fileToken)));

        doc.Uri = source;

        return doc;
    }

    /// <summary>
    /// Creates the default chain of documents.  (PackageDocument, 
    /// RightsDocument and FileDocument)
    /// </summary>
    /// <returns>A Document.</returns>
    internal static PackageDocument CreateDefaultDocument(
        Uri source, Stream stream)
    {
        PackageDocument doc = new PackageDocument(
            new RightsDocument(
            new FileDocument(stream)));

        doc.Uri = source;

        return doc;
    }

    /// <summary>
    /// Allows IDocumentControllers to be properly cleaned up when created
    /// by the factory method of this class.
    /// </summary>
    internal static void CleanUp()
    {
        foreach (IDocumentController controller in _controllers)
        {
            IDisposable disposable = controller as IDisposable;

            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// Will enable editing for the document.
    /// </summary>
    /// <param name="document">A document.</param>
    internal void EnableEdit(Document document)
    {
        if (document == null)
        {
            document = _current;
        }

        if (!_isEditEnabled)
        {
            _isEditEnabled = OrderByLeastDependent(DispatchEnableEdit, document);
        }
    }

    /// <summary>
    /// Will open the specified document.
    /// </summary>
    /// <param name="document">A document.</param>
    /// <returns>True if the operation succeeded</returns>
    internal bool Open(Document document)
    {
        ThrowIfNull(document);

        _current = document;
        // since we just loaded the document, it is now unmodified from the saved version
        _isModified = false; 
        return OrderByLeastDependent(DispatchOpen, document);
    }

    /// <summary>
    /// Will save the current document to a user-specified location.
    /// </summary>
    /// <returns>True if the operation succeeded</returns>
    internal bool SaveAs(Document document)
    {
        if (document == null)
        {
            document = _current;
        }

        bool result = OrderByLeastDependent(DispatchSaveAsPreperation, document);

        if (result)
        {
            result = OrderByMostDependent(DispatchSaveCommit, document);
        }

        if (result)
        {
            // since we just saved the document, it is now unmodified from the saved version
            _isModified = false;
            result = OrderByLeastDependent(DispatchRebind, document); 
        }

        return result;
    }

    /// <summary>
    /// Will save the specified document.
    /// </summary>
    /// <param name="document">A document.</param>
    /// <returns>True if the operation succeeded</returns>
    internal bool Save(Document document)
    {
        if (document == null)
        {
            document = _current;
        }

        bool result = OrderByLeastDependent(DispatchSavePreperation, document);

        if (result)
        {
            result = OrderByMostDependent(DispatchSaveCommit, document);
        }
        
        // We always Rebind even if the Commit failed -- if we fail we still 
        // need to Rebind the original document.
        OrderByLeastDependent(DispatchRebind, document);
        // since we just saved the document, it is now unmodified from the saved version
        _isModified = false;

        return result;
    }

    /// <summary>
    /// This event handler is called to notify us that the document was modified.
    /// </summary>
    /// <param name="sender">sender of the event (not used)</param>
    /// <param name="args">arguments of the event (not used)</param>
    internal static void OnModify(Object sender, EventArgs args)
    {
        _isModified = true;
    }

    /// <summary>
    /// Forces the given document to reload.
    /// </summary>
    /// <param name="document">The document</param>
    /// <returns>True if the operation succeeded</returns>
    internal bool Reload(Document document)
    {
        if (document == null)
        {
            document = _current;
        }

        // Set IsReloadNeeded to force reloading the document
        document.IsReloadNeeded = true;

        // Dispatch a rebind operation, which will cause a reload
        return OrderByLeastDependent(DispatchRebind, document);
    }

    #endregion Internal Methods

    #region Internal Properties
    //--------------------------------------------------------------------------
    // Internal Properties
    //--------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets whether saving to the source of the document may be possible.
    /// </summary>
    /// <remarks>
    /// Currently we only support saving when opened by a file Uri.
    /// </remarks>
    internal bool CanSave
    {
        get
        {
            return _canSave;
        }
        set
        {
            _canSave = value;
        }
    }

    /// <summary>
    /// This property returns true if the document has been modified from the
    /// version on disk (and therefore it makes sense for the user to save it).
    /// This property combines modification information from multiple sources.
    /// </summary>
    /// <remarks>
    /// Some changes are reflected in Package.IsDirty -- all other changes
    /// will notify us by writing to this property.  The document is considered
    /// modified if it is modified either according to the PackageDocument or
    /// our internal flag.
    /// </remarks>
    internal bool IsModified
    {
        get
        {
            PackageDocument doc = _current as PackageDocument;
            // If we don't have a PackageDocument (for example, if we are viewing 
            // a read-only file), then only our internal flag is used.
            if (doc == null || doc.Package == null)
            {
                return _isModified;
            }
            else
            {
                return doc.Package.IsDirty || _isModified;
            }
        }
    }

    #endregion Internal Properties

    #region Private Methods
    //--------------------------------------------------------------------------
    // Private Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Invokes EnableEdit on the controller provided using the document given.
    /// </summary>
    /// <param name="controller">The controller to perform the action.</param>
    /// <param name="document">The document to perform it on.</param>
    /// <returns>True if handled by controller.</returns>
    private static bool DispatchEnableEdit
        (IDocumentController controller, Document document)
    {
        if ((document.Dependency != null) && (document.Dependency.Workspace == null))
        {
            Trace.SafeWrite(
                Trace.File,
                "Skipping EnableEdit for {0} because dependent Stream is null.",
                controller);
            return true;
        }
        return controller.EnableEdit(document);
    }

    /// <summary>
    /// Invokes Open on the controller provided using the document given.
    /// </summary>
    /// <param name="controller">The controller to perform the action.</param>
    /// <param name="document">The document to perform it on.</param>
    /// <returns>True if handled by controller.</returns>
    private static bool DispatchOpen
        (IDocumentController controller, Document document)
    {
        if ((document.Dependency != null) && (document.Dependency.Source == null))
        {
            Trace.SafeWrite(
                Trace.File,
                "Skipping Open for {0} because dependent Stream is null.",
                controller);
            return true;
        }
        return controller.Open(document);
    }

    /// <summary>
    /// Invokes Rebind on the controller provided using the document given.
    /// </summary>
    /// <param name="controller">The controller to perform the action.</param>
    /// <param name="document">The document to perform it on.</param>
    /// <returns>True if handled by controller.</returns>
    private static bool DispatchRebind
    (IDocumentController controller, Document document)
    {
        if ((document.Dependency != null) && (document.Dependency.Source == null))
        {
            Trace.SafeWrite(
                Trace.File,
                "Skipping Rebind for {0} because dependent Stream is null.",
                controller);
            return true;
        }
        return controller.Rebind(document);
    }

    /// <summary>
    /// Invokes SaveAsPreperation on the controller provided using the document
    /// given.
    /// </summary>
    /// <param name="controller">The controller to perform the action.</param>
    /// <param name="document">The document to perform it on.</param>
    /// <returns>True if handled by controller.</returns>
    private static bool DispatchSaveAsPreperation
        (IDocumentController controller, Document document)
    {
        if ((document.Dependency != null) && (document.Dependency.Destination == null))
        {
            Trace.SafeWrite(
                Trace.File,
                "Skipping SaveAsPreperation for {0} because dependent Stream is null.",
                controller);
            return true;
        }
        return controller.SaveAsPreperation(document);
    }

    /// <summary>
    /// Invokes SaveCommit on the controller provided using the document given.
    /// </summary>
    /// <param name="controller">The controller to perform the action.</param>
    /// <param name="document">The document to perform it on.</param>
    /// <returns>True if handled by controller.</returns>
    private static bool DispatchSaveCommit
        (IDocumentController controller, Document document)
    {
        if ((document.Dependency != null) && (document.Dependency.Destination == null))
        {
            Trace.SafeWrite(
                Trace.File,
                "Skipping SaveCommit for {0} because dependent Stream is null.",
                controller);
            return true;
        }
        return controller.SaveCommit(document);
    }

    /// <summary>
    /// Invokes SavePreperation on the controller provided using the document
    /// given.
    /// </summary>
    /// <param name="controller">The controller to perform the action.</param>
    /// <param name="document">The document to perform it on.</param>
    /// <returns>True if handled by controller.</returns>
    private static bool DispatchSavePreperation
        (IDocumentController controller, Document document)
    {
        if ((document.Dependency != null) && (document.Dependency.Destination == null))
        {
            Trace.SafeWrite(
                Trace.File,
                "Skipping SavePreperation for {0} because dependent Stream is null.",
                controller);
            return true;
        }
        return controller.SavePreperation(document);
    }

    /// <summary>
    /// Invokes the action from the top of the document dependency chain to the
    /// bottom, for each provider under it in the chain.
    /// </summary>
    /// <remarks>
    /// OrderByMostDependent is implement in the ChainOfDependency
    /// (not a GoF pattern), it walks the tree of dependencies in this case the
    /// most dependent would be first.  The Chain of Responsiblity pattern is
    /// invoke by the inherited member 'Dispatch' which is called for each
    /// dependency.
    /// </remarks>
    /// <param name="action">The action to perform.</param>
    /// <param name="document">The document to perform it on.</param>
    private bool OrderByMostDependent(DispatchDelegate action, Document document)
    {
        return ChainOfDependencies<Document>.OrderByMostDependent(
            document,
            delegate(Document member)
            {
                return this.Dispatch(delegate(
                    IDocumentController controller,
                    Document subject)
                {
                    return action(controller, subject);
                },
                member);
            });
    }

    /// <summary>
    /// Invokes the action from the bottom of the document dependency chain up
    /// to the top, for each provider under it in the chain.
    /// </summary>
    /// <remarks>
    /// OrderByLeastDependent is implement in the ChainOfDependency
    /// (not a GoF pattern), it walks the tree of dependencies in this case the
    /// least dependent would be first.  The Chain of Responsiblity pattern is
    /// invoke by the inherited member 'Dispatch' which is called for each
    /// dependency.
    /// </remarks>
    /// <param name="action">The action to perform.</param>
    /// <param name="document">The document to perform it on.</param>
    private bool OrderByLeastDependent(DispatchDelegate action, Document document)
    {
        return ChainOfDependencies<Document>.OrderByLeastDependent(
            document,
            delegate(Document member)
            {
                return this.Dispatch(delegate(
                    IDocumentController controller,
                    Document subject)
                {
                    return action(controller, subject);
                },
                member);
            });
    }

    /// <summary>
    /// Will throw if the document is null.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"/>
    /// <param name="document">The document to validate.</param>
    private static void ThrowIfNull(Document document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }
    #endregion Private Methods

    #region Private Delegates
    //--------------------------------------------------------------------------
    // Private Delegates
    //--------------------------------------------------------------------------

    /// <summary>
    /// Defines the delegate for actions invoked by DocumentManager.
    /// </summary>
    /// <param name="controller">The controller to perform the action.</param>
    /// <param name="document">The document to perform it on.</param>
    /// <returns>True if handled by controller.</returns>
    private delegate bool DispatchDelegate(
        IDocumentController controller, Document document);
    #endregion Private Delegates

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    // Fields below simply prevents undesired user experience, they do not
    // impact security constraints nor are they used as protection

    private static Document _current;
    private static DocumentManager _singleton;
    private static bool _canSave;
    private static bool _isEditEnabled;
    private static bool _isModified;
    private static List<IDocumentController> _controllers =
        new List<IDocumentController>();
    #endregion Private Fields
}
}

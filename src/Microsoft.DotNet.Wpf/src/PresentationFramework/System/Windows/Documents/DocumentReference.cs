// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the DocumentReference element
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using MS.Internal.AppModel;
    using MS.Internal.Documents;
    using MS.Internal.Utility;
    using MS.Internal.Navigation;
    using MS.Internal.PresentationFramework; // SecurityHelper
    using System.Reflection;
    using System.Windows;                // DependencyID etc.
    using System.Windows.Navigation;
    using System.Windows.Markup;
    using System.Windows.Threading;               // Dispatcher
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Packaging;
    using System.Net;
    using System.Security;


    //=====================================================================
    /// <summary>
    /// DocumentReference is the class that references a Document. 
    /// Each document 
    /// </summary>
    [UsableDuringInitialization(false)]
    public sealed class DocumentReference : FrameworkElement, IUriContext
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        /// <summary>
        ///     Default DocumentReference constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public DocumentReference() : base()
        {
            _Init();
        }

        #endregion Constructors
        
        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
        
        #region Public Methods
        /// <summary>
        /// Synchonrously download and parse the document based on the Source specification. 
        /// If a document was attached earlier and forceReload == false, the attached 
        /// document will be returned.  forceReload == true results in downloading of the referenced 
        /// document based on Source specification, any previously attached document is ignored
        /// in this case. 
        /// Regardless of forceReload, the document will be loaded based on Source specification if 
        /// it hasn't been loaded earlier.
        /// </summary>
        /// <param name="forceReload">Force reloading the document instead of using cached value</param>
        /// <returns>The document tree</returns>
        public FixedDocument GetDocument(bool forceReload)
        {
            DocumentsTrace.FixedDocumentSequence.IDF.Trace(string.Format("DocumentReference.GetDocument ({0}, {1})", Source == null ? new Uri("", UriKind.RelativeOrAbsolute) : Source, forceReload));
             VerifyAccess();

            FixedDocument idp = null;
            if (_doc != null)
            {
                idp = _doc;
            }
            else 
            {
                if (!forceReload)
                {
                     idp = CurrentlyLoadedDoc;
                }
                
                if (idp == null)
                {
                    FixedDocument idpReloaded = _LoadDocument();
                    if (idpReloaded != null)
                    {
                        DocumentsTrace.FixedDocumentSequence.IDF.Trace(string.Format("DocumentReference.GetDocument Loaded IDP {0}", idpReloaded.GetHashCode()));
                        // save the doc's identity
                        _docIdentity = idpReloaded;
                        idp = idpReloaded;
                    }
                }
            }

            if (idp != null)
            {
                LogicalTreeHelper.AddLogicalChild(this.Parent, idp);
            }
            return idp;
        }

        /// <summary>
        /// Attach a document to this DocumentReference
        /// You can only attach a document if it is not attached before or it is not created from URI before.
        /// </summary>
        /// <param name="doc"></param>
        public void SetDocument(FixedDocument doc)
        {
            VerifyAccess();
            _docIdentity = null;
            _doc = doc;
        }

        #endregion Public Methods


        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------

        #region Public Properties
        /// <summary>
        /// Dynamic Property to reference an external document stream.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
                DependencyProperty.Register(
                        "Source", 
                        typeof(Uri), 
                        typeof(DocumentReference), 
                        new FrameworkPropertyMetadata(
                                (Uri) null,
                                new PropertyChangedCallback(OnSourceChanged)));


        static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentsTrace.FixedDocumentSequence.IDF.Trace(string.Format("DocumentReference.Source_Invaidated"));
            DocumentReference docRef = (DocumentReference)d;
 
            if (!object.Equals(e.OldValue, e.NewValue))
            {
                Uri oldSource = (Uri) e.OldValue;
                Uri newSource = (Uri) e.NewValue;
                DocumentsTrace.FixedDocumentSequence.IDF.Trace(string.Format("====Replace old doc {0} with new {1}", 
                    oldSource == null ? "null" : oldSource.ToString(), 
                    newSource == null? "null" : newSource.ToString()));
                // drop loaded document if source changed
                docRef._doc = null;
                //
                // #966803: Source change won't be a support scenario.
                //
            }
        }


        /// <summary>
        /// Get/Set Source property that references an external page stream. 
        /// </summary>
        public Uri Source
        {
            get { return (Uri) GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }
        #endregion Public Properties

        #region IUriContext
        /// <summary>
        /// <see cref="IUriContext.BaseUri" />
        /// </summary>
        Uri IUriContext.BaseUri
        {
            get { return (Uri)GetValue(BaseUriHelper.BaseUriProperty); }
            set { SetValue(BaseUriHelper.BaseUriProperty, value); }
        }
        #endregion IUriContext

        //--------------------------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------------------------

        #region Public Event
        #endregion Public Event


        //--------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------

        #region Internal Methods
#if DEBUG
        internal void Dump()
        {
            DocumentsTrace.FixedDocumentSequence.Content.Trace(string.Format("     This {0}", this.GetHashCode()));
            DocumentsTrace.FixedDocumentSequence.Content.Trace(string.Format("         Source {0}", this.Source == null ? "null" : this.Source.ToString()));
            DocumentsTrace.FixedDocumentSequence.Content.Trace(string.Format("         _doc   {0}", _doc == null ? 0 : _doc.GetHashCode()));
            DocumentsTrace.FixedDocumentSequence.Content.Trace(string.Format("         _docIdentity {0}", _docIdentity == null ? 0 : _docIdentity.GetHashCode()));
        }
#endif
        #endregion Internal Methods

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties
        // return most recent result of GetDocument, if it is still alive
        internal FixedDocument CurrentlyLoadedDoc
        {
            get
            {
                return _docIdentity;
            }
        }
        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // private Properties
        //
        //---------------------------------------------------------------------

        #region Private Properties
        #endregion Private Properties


        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods
        private void  _Init()
        {
            this.InheritanceBehavior = InheritanceBehavior.SkipToAppNow;
        }


        private Uri _ResolveUri()
        {
            Uri uriToNavigate = this.Source;
            if (uriToNavigate != null)
            {
                uriToNavigate = BindUriHelper.GetUriToNavigate(this, ((IUriContext)this).BaseUri, uriToNavigate);
            }
            return uriToNavigate;
        }


        // sync load a document
        private FixedDocument _LoadDocument()
        {
            FixedDocument idp = null;
            Uri uriToLoad = _ResolveUri();
            if (uriToLoad != null)
            {
                ContentType mimeType = null;
                Stream docStream = null;

                docStream = WpfWebRequestHelper.CreateRequestAndGetResponseStream(uriToLoad, out mimeType);
                if (docStream == null)
                {
                    throw new ApplicationException(SR.Get(SRID.DocumentReferenceNotFound));
                }

                ParserContext pc = new ParserContext();

                pc.BaseUri = uriToLoad;

                if (BindUriHelper.IsXamlMimeType(mimeType))
                {
                    XpsValidatingLoader loader = new XpsValidatingLoader();
                    idp = loader.Load(docStream, ((IUriContext)this).BaseUri, pc, mimeType) as FixedDocument;
                }
                else if (MS.Internal.MimeTypeMapper.BamlMime.AreTypeAndSubTypeEqual(mimeType))
                {
                    idp = XamlReader.LoadBaml(docStream, pc, null, true) as FixedDocument;
                }
                else
                {
                    throw new ApplicationException(SR.Get(SRID.DocumentReferenceUnsupportedMimeType));
                }
                idp.DocumentReference = this;
            }
 
           return idp;
        }
        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private FixedDocument _doc;
        private FixedDocument _docIdentity;     // used to cache the identity of the IDF so the IDF knows where it come from. 
        #endregion Private Fields
    }
}


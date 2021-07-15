// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the FixedDocumentSequence element
//

using MS.Internal.Documents;
using MS.Utility;                   // ExceptionStringTable
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Automation.Peers;    // AutomationPeer
using System.Windows.Threading;     // Dispatcher
using System.Windows;               // DependencyID etc.
using System.Windows.Media;         // Visual
using System.Windows.Markup; // IAddChild, ContentProperty
using System.Windows.Navigation;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Documents
{
    /// <summary>
    /// FixedDocumentSequence is a IDocumentPaginatorSource that composites other IDocumentPaginatorSource
    /// via DocumentReference
    /// </summary>
    [ContentProperty("References")]
    public class FixedDocumentSequence : FrameworkContentElement, IDocumentPaginatorSource, IAddChildInternal, IServiceProvider, IFixedNavigate, IUriContext
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors

        static FixedDocumentSequence()
        {
            FocusableProperty.OverrideMetadata(typeof(FixedDocumentSequence), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        ///     Default FixedDocumentSequence constructor
        /// </summary>
        public FixedDocumentSequence() : base()
        {
            _Init();
        }

        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------

        #region IServiceProvider Members

        /// <summary>
        /// <see cref="IServiceProvider.GetService" />
        /// </summary>
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            if (serviceType == typeof(ITextContainer))
            {
                return this.TextContainer;
            }

            if (serviceType == typeof(RubberbandSelector))
            {
                // create this on demand, but not through the property, only through the
                // service, so it is only created when it's actually used
                if (_rubberbandSelector == null)
                {
                    _rubberbandSelector = new RubberbandSelector();
                }
                return _rubberbandSelector;
            }

            return null;
        }

        #endregion IServiceProvider Members

        #region IAddChild

        ///<summary>
        /// Called to Add the object as a Child.
        ///</summary>
        ///<param name="value">
        /// Object to add as a child
        ///</param>
        void IAddChild.AddChild(Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

//             Dispatcher.VerifyAccess();

            DocumentReference docRef = value as DocumentReference;

            if (docRef == null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(DocumentReference)), "value");
            }

            if (docRef.IsInitialized)
            {
                _references.Add(docRef);
            }
            else
            {
                DocumentsTrace.FixedDocumentSequence.Content.Trace(string.Format("Doc {0} Deferred", _references.Count));
                if (_partialRef == null)
                {
                    _partialRef = docRef;
                    _partialRef.Initialized += new EventHandler(_OnDocumentReferenceInitialized);
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.PrevoiusUninitializedDocumentReferenceOutstanding));
                }
            }
        }

        ///<summary>
        /// Called when text appears under the tag in markup
        ///</summary>
        ///<param name="text">
        /// Text to Add to the Object
        ///</param>
        void IAddChild.AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        #endregion

        #region IFixedNavigate

        void IFixedNavigate.NavigateAsync(string elementID)
        {
            if (IsPageCountValid == true)
            {
                FixedHyperLink.NavigateToElement(this, elementID);
            }
            else
            {
                _navigateAfterPagination = true;
                _navigateFragment = elementID;
            }
        }

        UIElement IFixedNavigate.FindElementByID(string elementID, out FixedPage rootFixedPage)
        {
            UIElement uiElementRet = null;
            rootFixedPage = null;

            DynamicDocumentPaginator childPaginator;
            FixedDocument            childFixedDoc;

            if (Char.IsDigit(elementID[0]))
            {
                //We convert to a page number here.
                int pageNumber = Convert.ToInt32(elementID, CultureInfo.InvariantCulture) - 1;
                int childPageNumber;

                if (TranslatePageNumber(pageNumber, out childPaginator, out childPageNumber))
                {
                    childFixedDoc = childPaginator.Source as FixedDocument;
                    if (childFixedDoc != null)
                    {
                        uiElementRet = childFixedDoc.GetFixedPage(childPageNumber);
                    }
                }
            }
            else
            {
                foreach (DocumentReference docRef in References)
                {
                    childPaginator = GetPaginator(docRef);
                    childFixedDoc = childPaginator.Source as FixedDocument;
                    if (childFixedDoc != null)
                    {
                        uiElementRet = ((IFixedNavigate)childFixedDoc).FindElementByID(elementID, out rootFixedPage);
                        if (uiElementRet != null)
                        {
                            break;
                        }
                    }
                }
            }

            return uiElementRet;
        }
        #endregion

        #region LogicalTree

        /// <summary>
        /// Returns enumerator to logical children
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
//                 Dispatcher.VerifyAccess();

                DocumentReference[] docArray = new DocumentReference[_references.Count];
                this._references.CopyTo(docArray, 0);
                return docArray.GetEnumerator();
            }
        }

        #endregion LogicalTree

        #region IDocumentPaginatorSource Members

        /// <summary>
        /// An object which paginates content.
        /// </summary>
        public DocumentPaginator DocumentPaginator
        {
            get { return _paginator; }
        }

        #endregion IDocumentPaginatorSource Members

        #region DocumentPaginator

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.GetPage"/>
        /// </summary>
        internal DocumentPage GetPage(int pageNumber)
        {
            DocumentsTrace.FixedFormat.IDF.Trace(string.Format("IDP.GetPage({0})", pageNumber));

            // Make sure that the call is in the right context.
//             Dispatcher.VerifyAccess();

            // Page number cannot be negative.
            if (pageNumber < 0)
            {
                throw new ArgumentOutOfRangeException("pageNumber", SR.Get(SRID.IDPNegativePageNumber));
            }

            DocumentPage innerDP = null;
            DynamicDocumentPaginator innerPaginator;
            int innerPageNumber;
            // Find the appropriate inner IDF and BR
            if (TranslatePageNumber(pageNumber, out innerPaginator, out innerPageNumber))
            {
                innerDP = innerPaginator.GetPage(innerPageNumber);
                Debug.Assert(innerDP != null);

                // Now warp inner DP and return it
                return new FixedDocumentSequenceDocumentPage(this, innerPaginator, innerDP);
            }
            return DocumentPage.Missing;
        }

        /// <summary>
        /// Returns a FixedDocumentSequenceDocumentPage for the specified document and page number.
        /// </summary>
        internal DocumentPage GetPage(FixedDocument document, int fixedDocPageNumber)
        {
            if (fixedDocPageNumber < 0)
            {
                throw new ArgumentOutOfRangeException("fixedDocPageNumber", SR.Get(SRID.IDPNegativePageNumber));
            }

            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            DocumentPage innerDP = document.GetPage(fixedDocPageNumber);
            Debug.Assert(innerDP != null);

            // Now wrap inner DP and return it
            return new FixedDocumentSequenceDocumentPage(this, document.DocumentPaginator as DynamicDocumentPaginator, innerDP);
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.GetPageAsync(int,object)"/>
        /// </summary>
        internal void GetPageAsync(int pageNumber, object userState)
        {
            DocumentsTrace.FixedFormat.IDF.Trace(string.Format("IDP.GetPageAsync({0}, {1})", pageNumber, userState));

            // Make sure that the call is in the right context.
//             Dispatcher.VerifyAccess();

            // Page number cannot be negative.
            if (pageNumber < 0)
            {
                throw new ArgumentOutOfRangeException("pageNumber", SR.Get(SRID.IDPNegativePageNumber));
            }

            if (userState == null)
            {
                throw new ArgumentNullException("userState");
            }

            // Add to outstanding AsyncOp list
            GetPageAsyncRequest asyncRequest = new GetPageAsyncRequest(new RequestedPage(pageNumber/*childPaginator, childPageNumber*/), userState);
            _asyncOps[userState] = asyncRequest;
            DispatcherOperationCallback queueTask = new DispatcherOperationCallback(_GetPageAsyncDelegate);
            Dispatcher.BeginInvoke(DispatcherPriority.Background, queueTask, asyncRequest);
        }

        /// <summary>
        /// <see cref="DynamicDocumentPaginator.GetPageNumber"/>
        /// </summary>
        internal int GetPageNumber(ContentPosition contentPosition)
        {
            if (contentPosition == null)
            {
                throw new ArgumentNullException("contentPosition");
            }

            // ContentPosition may be only created by DynamicDocumentPaginator.GetObjectPosition or
            // DynamicDocumentPaginator.GetPagePosition.
            // Because of that we are expecting one of 2 types here.
            DynamicDocumentPaginator childPaginator = null;
            ContentPosition childContentPosition = null;
            if (contentPosition is DocumentSequenceTextPointer)
            {
                DocumentSequenceTextPointer dsTextPointer = (DocumentSequenceTextPointer)contentPosition;

                #pragma warning suppress 6506 // dsTextPointer is obviously not null
                childPaginator = GetPaginator(dsTextPointer.ChildBlock.DocRef);
                childContentPosition = dsTextPointer.ChildPointer as ContentPosition;
            }

            if (childContentPosition == null)
            {
                throw new ArgumentException(SR.Get(SRID.IDPInvalidContentPosition));
            }

            int childPageNumber = childPaginator.GetPageNumber(childContentPosition);
            int pageNumber;
            _SynthesizeGlobalPageNumber(childPaginator, childPageNumber, out pageNumber);
            return pageNumber;
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.CancelAsync"/>
        /// </summary>
        internal void CancelAsync(object userState)
        {
            DocumentsTrace.FixedFormat.IDF.Trace(string.Format("IDP.GetPageAsyncCancel([{0}])", userState));

            if (userState == null)
            {
                throw new ArgumentNullException("userState");
            }

            if (_asyncOps.ContainsKey(userState))
            {
                GetPageAsyncRequest asyncRequest = _asyncOps[userState];
                if (asyncRequest != null)
                {
                    asyncRequest.Cancelled = true;
                    if (asyncRequest.Page.ChildPaginator != null)
                    {
                        asyncRequest.Page.ChildPaginator.CancelAsync(asyncRequest);
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="DynamicDocumentPaginator.GetObjectPosition"/>
        /// </summary>
        internal ContentPosition GetObjectPosition(Object o)
        {
            if (o == null)
            {
                throw new ArgumentNullException("o");
            }

            foreach (DocumentReference docRef in References)
            {
                DynamicDocumentPaginator childPaginator = GetPaginator(docRef);
                if (childPaginator != null)
                {
                    ContentPosition cp = childPaginator.GetObjectPosition(o);
                    if (cp != ContentPosition.Missing && (cp is ITextPointer))
                    {
                        ChildDocumentBlock childBlock = new ChildDocumentBlock(this.TextContainer, docRef);
                        return new DocumentSequenceTextPointer(childBlock, (ITextPointer)cp);
                    }
                }
            }
            return ContentPosition.Missing;
        }

        /// <summary>
        /// <see cref="DynamicDocumentPaginator.GetPagePosition"/>
        /// </summary>
        internal ContentPosition GetPagePosition(DocumentPage page)
        {
            FixedDocumentSequenceDocumentPage docPage = page as FixedDocumentSequenceDocumentPage;
            if (docPage == null)
            {
                return ContentPosition.Missing;
            }
            return docPage.ContentPosition;
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.IsPageCountValid"/>
        /// </summary>
        internal bool IsPageCountValid
        {
            get
            {
                bool documentSequencePageCountFinal = true;
                if (IsInitialized)
                {
                    foreach (DocumentReference docRef in References)
                    {
                        DynamicDocumentPaginator paginator = GetPaginator(docRef);
                        if (paginator == null || !paginator.IsPageCountValid)
                        {
                            documentSequencePageCountFinal = false;
                            break;
                        }
                    }
                }
                else
                {
                    documentSequencePageCountFinal = false;
                }

                return documentSequencePageCountFinal;
            }
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.PageCount"/>
        /// </summary>
        internal int PageCount
        {
            get
            {
                //
                // This can be optimized if the page count for each IDP won't change
                // && the list of IDP won't change. When each doc.IsPageCountValid is
                // true, then page count won't change.
                //
                int count = 0;

                foreach (DocumentReference docRef in References)
                {
                    DynamicDocumentPaginator paginator = GetPaginator(docRef);
                    if (paginator != null)
                    {
                        count += paginator.PageCount;
                        if (!paginator.IsPageCountValid)
                        {
                            break;
                        }
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.PageSize"/>
        /// </summary>
        internal Size PageSize
        {
            get { return _pageSize; }
            set { _pageSize = value; }
        }

        #endregion DocumentPaginator

        #region IUriContext
        /// <summary>
        /// <see cref="IUriContext.BaseUri" />
        /// </summary>
        Uri IUriContext.BaseUri
        {
            get { return (Uri) GetValue(BaseUriHelper.BaseUriProperty); }
            set { SetValue(BaseUriHelper.BaseUriProperty, value); }
        }
        #endregion

        //-------------------------------------------------------------- ------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------

        #region Public Properties
        /// <summary>
        /// Get a collection of DocumentReference that this FixedDocumentSequence contains.
        /// </summary>
        [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content)]
        [CLSCompliant(false)]
        public DocumentReferenceCollection References
        {
            get
            {
//                 Dispatcher.VerifyAccess();
                return _references;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static readonly DependencyProperty PrintTicketProperty
            = DependencyProperty.RegisterAttached("PrintTicket", typeof(object), typeof(FixedDocumentSequence),
                                                  new FrameworkPropertyMetadata((object)null));

        /// <summary>
        /// Get/Set PrintTicket Property
        /// </summary>
        public object PrintTicket
        {
            get
            {
                object printTicket = GetValue(PrintTicketProperty);
                return printTicket;
            }
            set
            {
                SetValue(PrintTicketProperty,value);
            }
        }

        #endregion Public Properties

        //--------------------------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------------------------


        //--------------------------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="ContentElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DocumentAutomationPeer(this);
        }

        #endregion Protected Methods

        //--------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------

        #region Internal Method

        internal DynamicDocumentPaginator GetPaginator(DocumentReference docRef)
        {
            // #966803: Source change won't be a support scenario.
            Debug.Assert(docRef != null);
            DynamicDocumentPaginator paginator = null;
            IDocumentPaginatorSource document = docRef.CurrentlyLoadedDoc;

            if (document != null)
            {
                paginator = document.DocumentPaginator as DynamicDocumentPaginator;
                Debug.Assert(paginator != null);
            }
            else
            {
                document = docRef.GetDocument(false /*forceReload*/);
                if (document != null)
                {
                    paginator = document.DocumentPaginator as DynamicDocumentPaginator;
                    Debug.Assert(paginator != null);
                    // hook up event handlers
                    paginator.PaginationCompleted += new EventHandler(_OnChildPaginationCompleted);
                    paginator.PaginationProgress += new PaginationProgressEventHandler(_OnChildPaginationProgress);
                    paginator.PagesChanged += new PagesChangedEventHandler(_OnChildPagesChanged);
                }
            }

            return paginator;
        }


        //----------------------------------------------------------------------
        // IDP Helper
        //----------------------------------------------------------------------

        // Take a global page number and translate it into a child paginator with a child page number.
        // A document will be look at if the previous document has finished pagination.
        internal bool TranslatePageNumber(int pageNumber, out DynamicDocumentPaginator childPaginator, out int childPageNumber)
        {
            childPaginator = null;
            childPageNumber = 0;

            foreach (DocumentReference docRef in References)
            {
                childPaginator = GetPaginator(docRef);
                if (childPaginator != null)
                {
                    childPageNumber = pageNumber;
                    if (childPaginator.PageCount > childPageNumber)
                    {
                        // The page falls inside this paginator.
                        return true;
                    }
                    else
                    {
                        if (!childPaginator.IsPageCountValid)
                        {
                            // Don't bother look further if this doc has not finished
                            // pagination
                            break;
                        }
                        pageNumber -= childPaginator.PageCount;
                    }
                }
            }
            return false;
        }


        #endregion Internal Method

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties

        internal DocumentSequenceTextContainer TextContainer
        {
            get
            {
                if (_textContainer == null)
                {
                    _textContainer = new DocumentSequenceTextContainer(this);
                }
                return _textContainer;
            }
        }

        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods

        private void _Init()
        {
            _paginator = new FixedDocumentSequencePaginator(this);
            _references = new DocumentReferenceCollection();
            _references.CollectionChanged += new NotifyCollectionChangedEventHandler(_OnCollectionChanged);
            _asyncOps   = new Dictionary<Object,GetPageAsyncRequest>();
            _pendingPages  =  new List<RequestedPage>();
            _pageSize = new Size(8.5d * 96d, 11.0d * 96d);
            this.Initialized += new EventHandler(OnInitialized);
        }

        private void OnInitialized(object sender, EventArgs e)
        {
            bool documentSequencePageCountFinal = true;

            foreach (DocumentReference docRef in References)
            {
                DynamicDocumentPaginator paginator = GetPaginator(docRef);
                if (paginator == null || !paginator.IsPageCountValid)
                {
                    documentSequencePageCountFinal = false;
                    break;
                }
            }

            if (documentSequencePageCountFinal == true)
            {
                _paginator.NotifyPaginationCompleted(EventArgs.Empty);
            }

            if (PageCount > 0)
            {
                DocumentPage docPage = GetPage(0);
                if (docPage != null)
                {
                    FixedPage page = docPage.Visual as FixedPage;
                    if (page != null)
                    {
                        this.Language = page.Language;
                    }
                }
            }
        }

        //----------------------------------------------------------------------
        // Content Model Helper
        //----------------------------------------------------------------------

        private void _OnDocumentReferenceInitialized(object sender, EventArgs e)
        {
            DocumentReference docRef = (DocumentReference)sender;

            if (docRef == _partialRef)
            {
                DocumentsTrace.FixedDocumentSequence.Content.Trace(string.Format("Loaded DocumentReference {0}", _references.Count));
                _partialRef.Initialized -= new EventHandler(_OnDocumentReferenceInitialized);
                _partialRef = null;
                _references.Add(docRef);
            }
        }


        private void _OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                if (args.NewItems.Count != 1)
                {
                    throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                }
                else
                {
                    // get the affected item
                    object item = args.NewItems[0];

                    DocumentsTrace.FixedDocumentSequence.Content.Trace(string.Format("_OnCollectionChange: Add {0}", item.GetHashCode()));
                    AddLogicalChild(item);

                    int pageCount = this.PageCount;
                    DynamicDocumentPaginator paginator = GetPaginator((DocumentReference)item);
                    if (paginator == null)
                    {
                        throw new ApplicationException(SR.Get(SRID.DocumentReferenceHasInvalidDocument));
                    }

                    int addedPages = paginator.PageCount;
                    int firstPage = pageCount - addedPages;

                    if (addedPages > 0)
                    {
                        DocumentsTrace.FixedDocumentSequence.Content.Trace(string.Format("_OnCollectionChange: Add with IDP {0}", paginator.GetHashCode()));
                        _paginator.NotifyPaginationProgress(new PaginationProgressEventArgs(firstPage, addedPages));
                        _paginator.NotifyPagesChanged(new PagesChangedEventArgs(firstPage, addedPages));
                    }
                }
            }
            else
            {
                throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, args.Action));
            }
        }


        // Take a child paginator and a page nubmer, find which global page number it corresponds to
        private bool _SynthesizeGlobalPageNumber(DynamicDocumentPaginator childPaginator, int childPageNumber, out int pageNumber)
        {
            pageNumber = 0;
            foreach (DocumentReference docRef in References)
            {
                DynamicDocumentPaginator innerPaginator = GetPaginator(docRef);
                if (innerPaginator != null)
                {
                    if (innerPaginator == childPaginator)
                    {
                        pageNumber += childPageNumber;
                        return true;
                    }
                    pageNumber += innerPaginator.PageCount;
                }
            }
            return false;
        }

#if PAYLOADCODECOVERAGE
        // Get total page count before a given child document index.
        private int _GetPageCountBefore(int childIndex)
        {
            int count = 0;
            for(int i = 0; i < childIndex && i < References.Count; i++)
            {
                DocumentPaginator childDoc = GetPaginator(References[i]);
                if (childDoc != null)
                {
                    count += childDoc.PageCount;
                    if (!childDoc.IsPageCountValid)
                        break;
                }
            }
            return count;
        }
#endif

        //----------------------------------------------------------------------
        // Child IDP Events
        //----------------------------------------------------------------------
        private void _OnChildPaginationCompleted(object sender, EventArgs args)
        {
            DocumentsTrace.FixedDocumentSequence.IDF.Trace(string.Format("_OnChildPaginationCompleted"));
            if (IsPageCountValid)
            {
                _paginator.NotifyPaginationCompleted(EventArgs.Empty);

                if (_navigateAfterPagination)
                {
                    FixedHyperLink.NavigateToElement(this, _navigateFragment);
                    _navigateAfterPagination = false;
                }
            }
        }

        private void _OnChildPaginationProgress(object sender, PaginationProgressEventArgs args)
        {
            DocumentsTrace.FixedDocumentSequence.IDF.Trace(string.Format("_OnChildPaginationProgress"));
            int pageNumber;
            if (_SynthesizeGlobalPageNumber((DynamicDocumentPaginator)sender, args.Start, out pageNumber))
            {
                _paginator.NotifyPaginationProgress(new PaginationProgressEventArgs(pageNumber, args.Count));
            }
        }

        private void _OnChildPagesChanged(object sender, PagesChangedEventArgs args)
        {
            DocumentsTrace.FixedDocumentSequence.IDF.Trace(string.Format("_OnChildPagesChanged"));
            int pageNumber;
            if (_SynthesizeGlobalPageNumber((DynamicDocumentPaginator)sender, args.Start, out pageNumber))
            {
                _paginator.NotifyPagesChanged(new PagesChangedEventArgs(pageNumber, args.Count));
            }
            else
            {
                _paginator.NotifyPagesChanged(new PagesChangedEventArgs(PageCount, int.MaxValue));
            }
        }

        //----------------------------------------------------------------------
        // IDP Async
        //----------------------------------------------------------------------

        // An async request from the client code
        // It is translated into an async request into child paginator
        private object _GetPageAsyncDelegate(object arg)
        {
            DocumentsTrace.FixedDocumentSequence.IDF.Trace(string.Format("_GetPageAsyncDelegate"));

            GetPageAsyncRequest asyncRequest = (GetPageAsyncRequest)arg;
            int pageNumber = asyncRequest.Page.PageNumber;

            if (asyncRequest.Cancelled
                || !TranslatePageNumber(pageNumber, out asyncRequest.Page.ChildPaginator, out asyncRequest.Page.ChildPageNumber)
                || asyncRequest.Cancelled // Check again for cancellation, as previous line may have loaded FixedDocument and taken a while
                )
            {
                _NotifyGetPageAsyncCompleted(DocumentPage.Missing, pageNumber, null, true, asyncRequest.UserState);
                _asyncOps.Remove(asyncRequest.UserState);
                return null;
            }

            if (!_pendingPages.Contains(asyncRequest.Page))
            {
                // Initiate an async request to child paginator only if this page has not been requested.
                _pendingPages.Add(asyncRequest.Page);
                asyncRequest.Page.ChildPaginator.GetPageCompleted += new GetPageCompletedEventHandler(_OnGetPageCompleted);
                // use this asyncRequest as UserState
                asyncRequest.Page.ChildPaginator.GetPageAsync(asyncRequest.Page.ChildPageNumber, asyncRequest);
            }
            return null;
        }


        // Callback from inner IDP.GetPageAsync
        private void _OnGetPageCompleted(object sender, GetPageCompletedEventArgs args)
        {
            DocumentsTrace.FixedDocumentSequence.IDF.Trace(string.Format("_OnGetPageCompleted"));

            // this job is complete
            GetPageAsyncRequest completedRequest = (GetPageAsyncRequest)args.UserState;
            _pendingPages.Remove(completedRequest.Page);

            // wrap the returned result into FixedDocumentSequenceDocumentPage
            DocumentPage sdp = DocumentPage.Missing;
            int pageNumber = completedRequest.Page.PageNumber;
            if (!args.Cancelled && (args.Error == null) && (args.DocumentPage != DocumentPage.Missing))
            {
                sdp = new FixedDocumentSequenceDocumentPage(this, (DynamicDocumentPaginator)sender, args.DocumentPage);
                _SynthesizeGlobalPageNumber((DynamicDocumentPaginator)sender, args.PageNumber, out pageNumber);
            }

            if (!args.Cancelled)
            {
                // Notify all outstanding request for this particular page
                ArrayList notificationList = new ArrayList();
                IEnumerator<KeyValuePair<Object, GetPageAsyncRequest>> ienum = _asyncOps.GetEnumerator();
                try
                {
                    while (ienum.MoveNext())
                    {
                        GetPageAsyncRequest asyncRequest = ienum.Current.Value;

                        // Process any outstanding request for this PageContent
                        if (completedRequest.Page.Equals(asyncRequest.Page))
                        {
                            notificationList.Add(ienum.Current.Key);
                            // this could throw depending on event handlers that are added
                            _NotifyGetPageAsyncCompleted(sdp, pageNumber, args.Error, asyncRequest.Cancelled, asyncRequest.UserState);
                        }
                    }
                }
                finally
                {
                    // Remove completed requests from current async ops list
                    foreach (Object userState in notificationList)
                    {
                        _asyncOps.Remove(userState);
                    }
                }
            }
        }

        // Notify the caller of IDFAsync.MeasurePageAsync
        private void _NotifyGetPageAsyncCompleted(DocumentPage page, int pageNumber, Exception error, bool cancelled, object userState)
        {
            DocumentsTrace.FixedDocumentSequence.IDF.Trace(string.Format("_NotifyGetPageAsyncCompleted"));
            _paginator.NotifyGetPageCompleted(new GetPageCompletedEventArgs(page, pageNumber, error, cancelled, userState));
        }

        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Properties
        //
        //---------------------------------------------------------------------

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private DocumentReferenceCollection   _references;
        private DocumentReference _partialRef;  // uninitialized doc that is currently parsed.

        // IDP
        private FixedDocumentSequencePaginator _paginator;
        private IDictionary<Object, GetPageAsyncRequest> _asyncOps;  // pending request from client code
        private IList<RequestedPage>         _pendingPages;          // pending request to child page
        private Size _pageSize;
        private bool _navigateAfterPagination = false;
        private string _navigateFragment;

        // Text OM
        private DocumentSequenceTextContainer _textContainer;
        
        // Rubber band selection
        private RubberbandSelector _rubberbandSelector;

        #endregion Private Fields




        //--------------------------------------------------------------------
        //
        // Nested Class
        //
        //---------------------------------------------------------------------
        #region Nested Class


        // Represent a page that is being requested.
        private struct RequestedPage
        {
            internal DynamicDocumentPaginator ChildPaginator;
            internal int ChildPageNumber;
            internal int PageNumber;

            internal RequestedPage(int pageNumber)
            {
                PageNumber = pageNumber;
                ChildPageNumber = 0;
                ChildPaginator = null;
            }

            public override int GetHashCode()
            {
                return PageNumber;
            }

            // override Equals semantic
            public override bool Equals(object obj)
            {
                if (!(obj is RequestedPage))
                {
                    return false;
                }
                return this.Equals((RequestedPage)obj);
            }


            // Strongly typed version of Equals
            public bool Equals(RequestedPage obj)
            {
                return this.PageNumber == obj.PageNumber;
            }

            public static bool operator ==(RequestedPage obj1, RequestedPage obj2)
            {
                return obj1.Equals(obj2);
            }

            public static bool operator !=(RequestedPage obj1, RequestedPage obj2)
            {
                return !(obj1.Equals(obj2));
            }
        }


        // Represents an outstanding async request
        private class GetPageAsyncRequest
        {
            internal GetPageAsyncRequest(RequestedPage page, object userState)
            {
                Page       = page;
                UserState  = userState;
                Cancelled  = false;
            }

            internal RequestedPage Page;
            internal object UserState;
            internal bool Cancelled;
        }

        #endregion Nested Class
    }

    //=====================================================================
    //
    // FixedDocumentSequence's implemenation of DocumentPage
    //
    internal sealed class FixedDocumentSequenceDocumentPage : DocumentPage, IServiceProvider
    {
        //--------------------------------------------------------------------
        //
        // Ctors
        //
        //---------------------------------------------------------------------

        #region Ctors
        internal FixedDocumentSequenceDocumentPage(FixedDocumentSequence documentSequence, DynamicDocumentPaginator documentPaginator, DocumentPage documentPage)
            : base((documentPage is FixedDocumentPage) ? ((FixedDocumentPage)documentPage).FixedPage : documentPage.Visual, documentPage.Size, documentPage.BleedBox, documentPage.ContentBox)
        {
            Debug.Assert(documentSequence != null);
            Debug.Assert(documentPaginator != null);
            Debug.Assert(documentPage != null);
            _fixedDocumentSequence = documentSequence;
            _documentPaginator = documentPaginator;
            _documentPage = documentPage;
        }
        #endregion Ctors;

        //--------------------------------------------------------------------
        //
        // Public  Methods
        //
        //---------------------------------------------------------------------

        #region IServiceProvider
        /// <summary>
        /// Returns service objects associated with this control.
        /// </summary>
        /// <param name="serviceType">
        /// Specifies the type of service object to get.
        /// </param>
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            if (serviceType == typeof(ITextView))
            {
                if (_textView == null)
                {
                    _textView = new DocumentSequenceTextView(this);
                }
                return _textView;
            }
            return null;
        }
        #endregion IServiceProvider

        #region Public Methods
#if DEBUG
        /// <summary>
        /// <see cref="System.Object.ToString" />
        /// </summary>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "SDP:D{0}", _DocumentIndex);
        }
#endif
        #endregion Public Methods

        //--------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------


        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties

        public override Visual Visual
        {
            get
            {
                if (!_layedOut)
                {
                    _layedOut = true;

                    UIElement e;
                    if ((e = ((object)base.Visual) as UIElement) != null)
                    {
                        e.Measure(base.Size);
                        e.Arrange(new Rect(base.Size));
                    }
                }
                return base.Visual;
            }
        }

        internal ContentPosition ContentPosition
        {
            get
            {
                ITextPointer childPosition = _documentPaginator.GetPagePosition(_documentPage) as ITextPointer;
                if (childPosition != null)
                {
                    ChildDocumentBlock childBlock = new ChildDocumentBlock(_fixedDocumentSequence.TextContainer,
                                            ChildDocumentReference);
                    return new DocumentSequenceTextPointer(childBlock, childPosition);
                }
                return null;
            }
        }

        internal DocumentReference ChildDocumentReference
        {
            get
            {
                foreach (DocumentReference docRef in _fixedDocumentSequence.References)
                {
                    if (docRef.CurrentlyLoadedDoc == _documentPaginator.Source)
                    {
                        return docRef;
                    }
                }
                Debug.Assert(false);
                return null;
            }
        }

        internal DocumentPage ChildDocumentPage
        {
            get { return _documentPage; }
        }

        internal FixedDocumentSequence FixedDocumentSequence
        {
            get { return this._fixedDocumentSequence; }
        }

        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Internal Event
        //
        //---------------------------------------------------------------------


        //--------------------------------------------------------------------
        //
        // Private Properties
        //
        //---------------------------------------------------------------------
#if DEBUG
        private int _DocumentIndex
        {
            get
            {
                int docIndex = 0;
                foreach(DocumentReference docRef in _fixedDocumentSequence.References)
                {
                    docIndex++;
                    if (docRef.CurrentlyLoadedDoc == _documentPaginator.Source)
                    {
                        break;
                    }
                }
                return docIndex;
            }
        }
#endif

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        
        private readonly FixedDocumentSequence    _fixedDocumentSequence;
        private readonly DynamicDocumentPaginator _documentPaginator;
        private readonly DocumentPage             _documentPage;
        private bool                              _layedOut;
        // Text OM
        private DocumentSequenceTextView          _textView;

        #endregion Private Fields
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the FixedDocument element
//
// FixedPage changes.mht

namespace System.Windows.Documents
{
    using MS.Internal;                  // DoubleUtil
    using MS.Internal.Documents;
    using MS.Utility;                   // ExceptionStringTable
    using MS.Internal.Utility;
    using System.Windows.Threading;     // Dispatcher
    using System.Windows;               // DependencyID etc.
    using System.Windows.Automation.Peers;    // AutomationPeer
    using System.Windows.Documents;     // DocumentPaginator
    using System.Windows.Documents.DocumentStructures;    
    using System.Windows.Media;         // Visual
    using System.Windows.Markup; // IAddChild, ContentPropertyAttribute
    using System.Windows.Shapes;        // Glyphs
    using System;
    using System.IO;
    using System.IO.Packaging;
    using System.Net;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;        // DesignerSerializationVisibility
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.Serialization.Formatters.Binary;
    using MS.Internal.Annotations.Component;
    using System.Windows.Navigation;
    using System.Windows.Controls;
    using System.Text;
    using MS.Internal.IO.Packaging;
    using System.Security;

    using PackUriHelper = System.IO.Packaging.PackUriHelper;
    //=====================================================================
    /// <summary>
    /// FixedDocument is the spine of a portable, high fidelity fixed-format
    /// document, where the pages are stitched together. The FixedDocument
    /// elements provides and formats pages as requested. It also provides
    /// a Text OM on top of the fixed-format document to allow for read-only
    /// editing (selection, keyboard navigation, find, etc.).
    /// </summary>
    [ContentProperty("Pages")]
    public class FixedDocument : FrameworkContentElement, IDocumentPaginatorSource, IAddChildInternal, IServiceProvider, IFixedNavigate, IUriContext
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------

        #region Constructors

        static FixedDocument()
        {
            FocusableProperty.OverrideMetadata(typeof(FixedDocument), new FrameworkPropertyMetadata(true));
            NavigationService.NavigationServiceProperty.OverrideMetadata(
                        typeof(FixedDocument), 
                        new FrameworkPropertyMetadata(new PropertyChangedCallback(FixedHyperLink.OnNavigationServiceChanged)));
        }

        /// <summary>
        ///     Default FixedDocument constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current UIContext. Use alternative constructor
        ///     that accepts a UIContext for best performance.
        /// </remarks>
        public FixedDocument()
            : base()
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
        /// Returns service objects associated with this control.
        /// </summary>
        /// <remarks>
        /// FixedDocument currently supports TextContainer.
        /// </remarks>
        /// <exception cref="ArgumentNullException">serviceType is NULL.</exception>
        /// <param name="serviceType">
        /// Specifies the type of service object to get.
        /// </param>
        object IServiceProvider.GetService(Type serviceType)
        {
//             Dispatcher.VerifyAccess();
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            if (serviceType == typeof(ITextContainer))
            {
                return this.FixedContainer;
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
        /// <exception cref="ArgumentNullException">value is NULL.</exception>
        /// <exception cref="ArgumentException">value is not of type PageContent.</exception>
        /// <exception cref="InvalidOperationException">A PageContent cannot be added while previous partial page isn't completely Loaded.</exception>
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

            PageContent fp = value as PageContent;

            if (fp == null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(PageContent)), "value");
            }

            if (fp.IsInitialized)
            {
                _pages.Add(fp);
            }
            else
            {
                DocumentsTrace.FixedFormat.FixedDocument.Trace(string.Format("Page {0} Deferred", _pages.Count));
                if (_partialPage == null)
                {
                    _partialPage = fp;
                    _partialPage.ChangeLogicalParent(this);
                    _partialPage.Initialized += new EventHandler(OnPageLoaded);
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.PrevoiusPartialPageContentOutstanding));
                }
            }
        }

        ///<summary>
        /// Called when text appears under the tag in markup
        ///</summary>
        ///<param name="text">
        /// Text to Add to the Object
        ///</param>
        /// <ExternalAPI/>
        void IAddChild.AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }
        #endregion

        #region IUriContext
        /// <summary>
        /// <see cref="IUriContext.BaseUri" />
        /// </summary>
        Uri IUriContext.BaseUri
        {
            get { return (Uri) GetValue(BaseUriHelper.BaseUriProperty); }
            set { SetValue(BaseUriHelper.BaseUriProperty, value); }
        }
        #endregion IUriContext

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

            if (Char.IsDigit(elementID[0]))
            {
                //
                //We convert string to a page number here.
                //
                int pageNumber = Convert.ToInt32(elementID, CultureInfo.InvariantCulture);

                //
                // Metro defines all external page are 1 based. All internals are 0 based.
                //
                pageNumber --;
                
                uiElementRet = GetFixedPage(pageNumber);
                rootFixedPage = GetFixedPage(pageNumber);
            }
            else
            {
                //
                // We need iterate through the PageContentCollect first.
                //
                PageContentCollection pc = this.Pages;
                PageContent pageContent;
                FixedPage fixedPage;

                for (int i = 0, n = pc.Count; i < n; i++)
                {
                    pageContent = pc[i];
                    //
                    // If PageStream is non null, it is not PPS. Otherwise, need to check LinkTargets collection
                    // of PageContent
                    //
                    if (pageContent.PageStream != null)
                    {
                        fixedPage = GetFixedPage(i);
                        if (fixedPage != null)
                        {
                            uiElementRet = ((IFixedNavigate)fixedPage).FindElementByID(elementID, out rootFixedPage);
                            if (uiElementRet != null)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (pageContent.ContainsID(elementID))
                        {
                            fixedPage = GetFixedPage(i);
                            if (fixedPage != null)
                            {
                                uiElementRet = ((IFixedNavigate)fixedPage).FindElementByID(elementID, out rootFixedPage);
                                if (uiElementRet == null)
                                { // return that page if we can't find the named fragment
                                    uiElementRet = fixedPage;
                                }
                                //
                                // we always break here because pageContent include the named fragment
                                //
                                break;
                            }
                        }
                    }
                }
            }

            return uiElementRet;
        }

        internal NavigationService NavigationService
        {
            get { return (NavigationService) GetValue(NavigationService.NavigationServiceProperty); }
            set { SetValue(NavigationService.NavigationServiceProperty, value); }
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
//                 this.Dispatcher.VerifyAccess();
                return this.Pages.GetEnumerator();
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

        #region Document Overrides

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.GetPage(int)"/>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">pageNumber is less than zero.</exception>
        /// <param name="pageNumber">
        /// The page number.
        /// </param>
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

            if (pageNumber < Pages.Count)
            {
                //
                // If we are not out of bound, try next page
                //
                FixedPage page = SyncGetPage(pageNumber, false/*forceReload*/);

                if (page == null)
                {
                    return DocumentPage.Missing;
                }

                Debug.Assert(page != null);
                Size fixedSize = ComputePageSize(page);

                // Always measure with fixed size instead of using constraint
                FixedDocumentPage dp = new FixedDocumentPage(this, page, fixedSize, pageNumber);

                page.Measure(fixedSize);
                page.Arrange(new Rect(new Point(), fixedSize));

                return dp;
            }

            return DocumentPage.Missing;
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.GetPageAsync(int,object)"/>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">pageNumber is less than zero.</exception>
        /// <exception cref="ArgumentNullException">userState is NULL.</exception>
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

            if (pageNumber < Pages.Count)
            {
                PageContent pc = Pages[pageNumber];

                // Add to outstanding AsyncOp list
                GetPageAsyncRequest asyncRequest = new GetPageAsyncRequest(pc, pageNumber, userState);
                _asyncOps[userState] = asyncRequest;

                DispatcherOperationCallback queueTask = new DispatcherOperationCallback(GetPageAsyncDelegate);
                Dispatcher.BeginInvoke(DispatcherPriority.Background, queueTask, asyncRequest);
            }
            else
            {
                _NotifyGetPageAsyncCompleted(DocumentPage.Missing, pageNumber, null, false, userState);
            }
        }
        
        /// <summary>
        /// <see cref="DynamicDocumentPaginator.GetPageNumber"/>
        /// </summary>
        /// <exception cref="ArgumentNullException">contentPosition is NULL.</exception>
        /// <exception cref="ArgumentException">ContentPosition does not exist within this element?s tree.</exception>
        internal int GetPageNumber(ContentPosition contentPosition)
        {
//             Dispatcher.VerifyAccess();

            if (contentPosition == null)
            {
                throw new ArgumentNullException("contentPosition");
            }

            FixedTextPointer fixedTextPointer = contentPosition as FixedTextPointer;
            if (fixedTextPointer == null)
            {
                throw new ArgumentException(SR.Get(SRID.IDPInvalidContentPosition));
            }

            return fixedTextPointer.FixedTextContainer.GetPageNumber(fixedTextPointer);
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.CancelAsync"/>
        /// </summary>
        /// <exception cref="ArgumentNullException">userState is NULL.</exception>
        internal void CancelAsync(object userState)
        {
            DocumentsTrace.FixedFormat.IDF.Trace(string.Format("IDP.GetPageAsyncCancel([{0}])", userState));
//             Dispatcher.VerifyAccess();

            if (userState == null)
            {
                throw new ArgumentNullException("userState");
            }

            GetPageAsyncRequest asyncRequest;
            if (_asyncOps.TryGetValue(userState,out asyncRequest))
            {
                if (asyncRequest != null)
                {
                    asyncRequest.Cancelled = true;
                    asyncRequest.PageContent.GetPageRootAsyncCancel();
                }
            }
        }

        /// <summary>
        /// <see cref="DynamicDocumentPaginator.GetObjectPosition"/>
        /// </summary>
        /// <exception cref="ArgumentNullException">element is NULL.</exception>
        internal ContentPosition GetObjectPosition(Object o)
        {
            if (o == null)
            {
                throw new ArgumentNullException("o");
            }
            
            DependencyObject element = o as DependencyObject;

            if (element == null)
            {
                throw new ArgumentException(SR.Get(SRID.FixedDocumentExpectsDependencyObject));
            }
            DocumentsTrace.FixedFormat.IDF.Trace(string.Format("IDF.GetContentPositionForElement({0})", element));
            // Make sure that the call is in the right context.
//             Dispatcher.VerifyAccess();


            // walk up the logical parent chain to find the containing page
            FixedPage fixedPage = null;
            int pageIndex = -1;
            if (element != this)
            {
                DependencyObject el = element;
                while (el != null)
                {
                    fixedPage = el as FixedPage;

                    if (fixedPage != null)
                    {
                        pageIndex = GetIndexOfPage(fixedPage);
                        if (pageIndex >= 0)
                        {
                            break;
                        }
                        el = fixedPage.Parent;
                    }
                    else
                    {
                        el = LogicalTreeHelper.GetParent(el);
                    }
                }
            }
            else if (this.Pages.Count > 0)
            {
                // if FixedDocument is requested, return ContentPosition for the first page.
                pageIndex = 0;
            }

            // get FixedTextPointer for element or page index
            FixedTextPointer fixedTextPointer = null;
            if (pageIndex >= 0)
            {
                FixedPosition fixedPosition;
                FlowPosition flowPosition=null;
                System.Windows.Shapes.Path p = element as System.Windows.Shapes.Path;
                if (element is Glyphs || element is Image || (p != null &&  p.Fill is ImageBrush))
                {
                    fixedPosition = new FixedPosition(fixedPage.CreateFixedNode(pageIndex, (UIElement)element), 0);
                    flowPosition = FixedContainer.FixedTextBuilder.CreateFlowPosition(fixedPosition);
                }
                if (flowPosition == null)
                {
                    flowPosition = FixedContainer.FixedTextBuilder.GetPageStartFlowPosition(pageIndex);
                }
                fixedTextPointer = new FixedTextPointer(true, LogicalDirection.Forward, flowPosition);
            }

            return (fixedTextPointer != null) ? fixedTextPointer : ContentPosition.Missing;
        }


        /// <summary>
        /// <see cref="DynamicDocumentPaginator.GetPagePosition"/>
        /// </summary>
        internal ContentPosition GetPagePosition(DocumentPage page)
        {
            FixedDocumentPage docPage = page as FixedDocumentPage;
            if (docPage == null)
            {
                return ContentPosition.Missing;
            }
            return docPage.ContentPosition;
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.IsPageCountValid"/>
        /// </summary>
        internal bool IsPageCountValid { get { return this.IsInitialized; } }

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.PageCount"/>
        /// </summary>
        internal int PageCount { get { return Pages.Count; } }

        /// <summary>
        /// <see cref="System.Windows.Documents.DocumentPaginator.PageSize"/>
        /// </summary>
        internal Size PageSize
        {
            get { return new Size(_pageWidth, _pageHeight); }
            set { _pageWidth = value.Width; _pageHeight = value.Height; }
        }

        internal bool HasExplicitStructure
        {
            get
            {
                return _hasExplicitStructure;
            }
        }

        #endregion Document Overrides

        //-------------------------------------------------------------- ------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Get a collection of PageContent that this FixedDocument contains.
        /// </summary>
        [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content)]
        public PageContentCollection Pages
        {
            get
            {
                return _pages;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static readonly DependencyProperty PrintTicketProperty
            = DependencyProperty.RegisterAttached("PrintTicket", typeof(object), typeof(FixedDocument),
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

        #region Internal Methods
        internal int GetIndexOfPage(FixedPage p)
        {
            PageContentCollection pc = this.Pages;
            for (int i = 0, n = pc.Count; i < n; i++)
            {
                if (pc[i].IsOwnerOf(p))
                {
                    return i;
                }
            }
            return -1;
        }


        internal bool IsValidPageIndex(int index)
        {
            return (index >= 0 && index < this.Pages.Count);
        }


        // Check index before trying to load page
        internal FixedPage SyncGetPageWithCheck(int index)
        {
            if (IsValidPageIndex(index))
            {
                return SyncGetPage(index, false /*forceReload*/);
            }
            DocumentsTrace.FixedFormat.FixedDocument.Trace(string.Format("SyncGetPageWithCheck {0} is invalid page", index));
            return null;
        }



        // Assumes index is valid
        internal FixedPage SyncGetPage(int index, bool forceReload)
        {
            DocumentsTrace.FixedFormat.FixedDocument.Trace(string.Format("SyncGetPage {0}", index));
            Debug.Assert(IsValidPageIndex(index));

            PageContentCollection pc = this.Pages;
            FixedPage fp;

            try
            {
                fp = (FixedPage)pc[index].GetPageRoot(forceReload);
            }
            catch (Exception e)
            {
                if (e is InvalidOperationException || e is ApplicationException)
                {
                    ApplicationException ae = new ApplicationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, SR.Get(SRID.ExceptionInGetPage), index), e);
                    throw ae;
                }
                else
                {
                    throw;
                }
            }
            return fp;
        }

        /// <summary>
        /// Callback when a new PageContent is added
        /// </summary>
        internal void OnPageContentAppended(int index)
        {
            FixedContainer.FixedTextBuilder.AddVirtualPage();

            _paginator.NotifyPaginationProgress(new PaginationProgressEventArgs(index, 1));

            if (this.IsInitialized)
            {
                _paginator.NotifyPagesChanged(new PagesChangedEventArgs(index, 1));
            }
        }

        //
        // Make sure page has its width and height.
        // If absoluteOnly is specified, it will overwrite relative size specified
        // on page with default page size.
        //
        internal void EnsurePageSize(FixedPage fp)
        {
            Debug.Assert(fp != null);

            double width = fp.Width;

            if (DoubleUtil.IsNaN(width))
            {
                fp.Width = _pageWidth;
            }

            double height = fp.Height;

            if (DoubleUtil.IsNaN(height))
            {
                fp.Height = _pageHeight;
            }
        }

        // Note: This code is specifically written for PageViewer.
        // Once PageViewer get away from inquring page size before
        // displaying page, we should remove this function.
        internal bool GetPageSize(ref Size pageSize, int pageNumber)
        {
            DocumentsTrace.FixedFormat.FixedDocument.Trace(string.Format("GetPageSize {0}", pageNumber));
            if (pageNumber < Pages.Count)
            {
                // NOTE: it is wrong to call this method when page is outstanding.
                // Unfortunately PageViewer + DocumentPaginator still is dependent on
                // PageSize, so have to avoid throwing exception in this situation.

                FixedPage p = null;
                if (!_pendingPages.Contains(Pages[pageNumber]))
                {
                    p = SyncGetPage(pageNumber, false /*forceReload*/);
                }
#if DEBUG
                else
                {
                    DocumentsTrace.FixedFormat.FixedDocument.Trace(string.Format("====== GetPageSize {0}  Warning sync call made while async outstanding =====", pageNumber));
                }
#endif


                // ComputePageSize will return appropriate value for null page
                pageSize = ComputePageSize(p);

                return true;
            }

            return false;
        }


        //
        // Get absolute page size. If relative page size is specified
        // in the page, it will be overwritten by default page size,
        // which is absolute size.
        //
        internal Size ComputePageSize(FixedPage fp)
        {
            if (fp == null)
            {
                return new Size(_pageWidth, _pageHeight);
            }
            EnsurePageSize(fp);
            return new Size(fp.Width, fp.Height);
        }

        #endregion Internal Methods

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties

        // expose the hosted FixedContainer
        internal FixedTextContainer FixedContainer
        {
            get
            {
                if (_fixedTextContainer == null)
                {
                    _fixedTextContainer = new FixedTextContainer(this);
                    _fixedTextContainer.Highlights.Changed += new HighlightChangedEventHandler(OnHighlightChanged);
                }
                return _fixedTextContainer;
            }
        }

        internal Dictionary<FixedPage, ArrayList> Highlights
        {
            get
            {
                return _highlights;
            }
        }

        internal DocumentReference DocumentReference
        {
            get
            {
                return _documentReference;
            }
            set
            {
                _documentReference = value;
            }
        }

        #endregion Internal Properties


        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods
        //---------------------------------------
        // Initialization
        //---------------------------------------
        private void _Init()
        {
            _paginator = new FixedDocumentPaginator(this);
            _pages = new PageContentCollection(this);
            _highlights = new Dictionary<FixedPage, ArrayList>();
            _asyncOps = new Dictionary<Object, GetPageAsyncRequest>();
            _pendingPages = new List<PageContent>();
            _hasExplicitStructure = false;
            this.Initialized += new EventHandler(OnInitialized);
        }


        private void OnInitialized(object sender, EventArgs e)
        {
            if (_navigateAfterPagination)
            {
                FixedHyperLink.NavigateToElement(this, _navigateFragment);
                _navigateAfterPagination = false;
            }

            //Currently we don't load the DocumentStructure part
            //At least we need to validate if it's there
            ValidateDocStructure();

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

            _paginator.NotifyPaginationCompleted(e);
        }

        internal void ValidateDocStructure()
        {
            Uri baseUri = BaseUriHelper.GetBaseUri(this);

            if (baseUri.Scheme.Equals(PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase))
            {
                // avoid the case of pack://application,,,
                if (baseUri.Host.Equals(BaseUriHelper.PackAppBaseUri.Host) != true &&
                    baseUri.Host.Equals(BaseUriHelper.SiteOfOriginBaseUri.Host) != true)
                {
                    Uri structureUri = GetStructureUriFromRelationship(baseUri, _structureRelationshipName);
                    if (structureUri != null)
                    {
                        ContentType mimeType;
                        ValidateAndLoadPartFromAbsoluteUri(structureUri, true, "DocumentStructure", out mimeType);
                        if (!_documentStructureContentType.AreTypeAndSubTypeEqual(mimeType))
                        {
                            throw new FileFormatException(SR.Get(SRID.InvalidDSContentType));
                        }
                        _hasExplicitStructure = true;
                    }
                }
            }
        }
        
        
        internal static StoryFragments GetStoryFragments(FixedPage fixedPage)
        {
            object o = null;

            Uri baseUri = BaseUriHelper.GetBaseUri(fixedPage);

            if (baseUri.Scheme.Equals(PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase))
            {
                // avoid the case of pack://application,,,
                if (baseUri.Host.Equals(BaseUriHelper.PackAppBaseUri.Host) != true &&
                    baseUri.Host.Equals(BaseUriHelper.SiteOfOriginBaseUri.Host) != true)
                {
                    Uri structureUri = GetStructureUriFromRelationship(baseUri, _storyFragmentsRelationshipName);

                    if (structureUri != null)
                    {
                        ContentType mimeType;
                        o = ValidateAndLoadPartFromAbsoluteUri(structureUri, false, null, out mimeType);
                        if (!_storyFragmentsContentType.AreTypeAndSubTypeEqual(mimeType))
                        {
                            throw new FileFormatException(SR.Get(SRID.InvalidSFContentType));
                        }
                        if (!(o is StoryFragments))
                        {
                            throw new FileFormatException(SR.Get(SRID.InvalidStoryFragmentsMarkup));
                        }
                    }
                }
            }

            return o as StoryFragments;
        }


        private static object ValidateAndLoadPartFromAbsoluteUri(Uri AbsoluteUriDoc, bool validateOnly, string rootElement, out ContentType mimeType)
        {
            mimeType = null;
            Stream pageStream = null;
            object o = null;

            try
            {
                pageStream = WpfWebRequestHelper.CreateRequestAndGetResponseStream(AbsoluteUriDoc, out mimeType);

                ParserContext pc = new ParserContext();
                pc.BaseUri = AbsoluteUriDoc;

                XpsValidatingLoader loader = new XpsValidatingLoader();
                if (validateOnly)
                {
                    loader.Validate(pageStream, null, pc, mimeType, rootElement);
                }
                else
                {
                    o = loader.Load(pageStream, null, pc, mimeType);
                }
            }
            catch (Exception e)
            {
                //System.Net.WebException will be thrown when the document structure does not exist in a non-container
                //and System.InvalidOperation will be thrown when calling Package.GetPart() in a container.
                //We ignore these but all others need to be rethrown
                if (!(e is System.Net.WebException || e is System.InvalidOperationException))
                {
                    throw;
                }
            }
            return o;
        }

         /// <summary>
        /// Retrieves the Uri for the DocumentStructure from the container's relationship
        /// </summary>
        static private Uri GetStructureUriFromRelationship(Uri contentUri, string relationshipName)
        {
            Uri absTargetUri = null;
            if (contentUri != null && relationshipName != null)
            {
                Uri partUri = PackUriHelper.GetPartUri(contentUri);
                if (partUri != null)
                {
                    Uri packageUri = PackUriHelper.GetPackageUri(contentUri);
                    Package package = PreloadedPackages.GetPackage(packageUri);

                    if (package == null)
                    {
                        package = PackageStore.GetPackage(packageUri);
                    }

                    if (package != null)
                    {
                        PackagePart part = package.GetPart(partUri);
                        PackageRelationshipCollection resources = part.GetRelationshipsByType(relationshipName);

                        Uri targetUri = null;
                        foreach (PackageRelationship relationShip in resources)
                        {
                            targetUri = PackUriHelper.ResolvePartUri(partUri, relationShip.TargetUri);
                        }

                        if (targetUri != null)
                        {
                            absTargetUri = PackUriHelper.Create(packageUri, targetUri);
                        }
                    }
                }
            }
            return absTargetUri;
        }

        private void OnPageLoaded(object sender, EventArgs e)
        {
            PageContent pc = (PageContent)sender;
            if (pc == _partialPage)
            {
                DocumentsTrace.FixedFormat.FixedDocument.Trace(string.Format("Loaded Page {0}", _pages.Count));
                _partialPage.Initialized -= new EventHandler(OnPageLoaded);
                _pages.Add(_partialPage);
                _partialPage = null;
            }
        }

        internal FixedPage GetFixedPage(int pageNumber)
        {
            FixedPage fp = null;
            FixedDocumentPage fdp = GetPage(pageNumber) as FixedDocumentPage;
            if (fdp != null && fdp != DocumentPage.Missing)
            {
                fp = fdp.FixedPage;
            }
            return fp;
        }

        //---------------------------------------
        // Text Editing
        //---------------------------------------
        private void OnHighlightChanged(object sender, HighlightChangedEventArgs args)
        {
            Debug.Assert(sender != null);
            Debug.Assert(args != null);
            Debug.Assert(args.Ranges != null);

            DocumentsTrace.FixedTextOM.Highlight.Trace(string.Format("HightlightMoved From {0}-{1} To {2}-{3}",0, 0, 0, 0));
            Debug.Assert(args.Ranges.Count > 0 && ((TextSegment)args.Ranges[0]).Start.CompareTo(((TextSegment)args.Ranges[0]).End) < 0);

            // REVIEW:benwest:7/9/2004: This code is reseting the entire highlight data structure
            // on every highlight delta, which scales horribly.  It should be possible
            // to make incremental changes as new deltas arrive.

            // Add new highlights, if any
            ITextContainer tc = this.FixedContainer;
            Highlights highlights = null;

            // If this document is part of a FixedDocumentSequence, we should use 
            // the highlights that have been set on the sequence.
            FixedDocumentSequence parent = this.Parent as FixedDocumentSequence;
            if (parent != null)
                highlights = parent.TextContainer.Highlights;
            else
                highlights = this.FixedContainer.Highlights;

            StaticTextPointer highlightTransitionPosition;
            StaticTextPointer highlightRangeStart;
            object selected;
            
            //Find out if any highlights have been removed. We need to invalidate those pages
            List<FixedPage> oldHighlightPages = new List<FixedPage>();
            foreach (FixedPage page in _highlights.Keys)
            {
                oldHighlightPages.Add(page);
            }

            _highlights.Clear();

            highlightTransitionPosition = tc.CreateStaticPointerAtOffset(0);

            while (true)
            {
                // Move to the next highlight start.
                if (!highlights.IsContentHighlighted(highlightTransitionPosition, LogicalDirection.Forward))
                {
                    highlightTransitionPosition = highlights.GetNextHighlightChangePosition(highlightTransitionPosition, LogicalDirection.Forward);

                    // No more highlights?
                    if (highlightTransitionPosition.IsNull)
                        break;
                }

                // highlightTransitionPosition is at the start of a new highlight run.
                // Get the highlight data.
                // NB: this code only recognizes typeof(TextSelection) + AnnotationHighlight.
                // We should add code here to handle other properties: foreground/background/etc.
                selected = highlights.GetHighlightValue(highlightTransitionPosition, LogicalDirection.Forward, typeof(TextSelection));

                // Save the start position and find the end.
                highlightRangeStart = highlightTransitionPosition;

                //placeholder for highlight type
                FixedHighlightType fixedHighlightType = FixedHighlightType.None;
                Brush foreground = null;
                Brush background = null;

                // Store the highlight.
                if (selected != DependencyProperty.UnsetValue)
                {
                    //find next TextSelection change.
                    //This is to skip AnnotationHighlight
                    //change positions, since Annotation Highlight is invisible under TextSelection
                    do
                    {
                        highlightTransitionPosition = highlights.GetNextHighlightChangePosition(highlightTransitionPosition, LogicalDirection.Forward);
                        Debug.Assert(!highlightTransitionPosition.IsNull, "Highlight start not followed by highlight end!");
                    }
                    while (highlights.GetHighlightValue(highlightTransitionPosition, LogicalDirection.Forward, typeof(TextSelection)) != DependencyProperty.UnsetValue);
                    fixedHighlightType = FixedHighlightType.TextSelection;
                    foreground = null;
                    background = null;
                }
                else
                {
                    //look for annotation highlight
                    AnnotationHighlightLayer.HighlightSegment highlightSegment = highlights.GetHighlightValue(highlightRangeStart,
                        LogicalDirection.Forward, typeof(HighlightComponent)) as AnnotationHighlightLayer.HighlightSegment;
                    if (highlightSegment != null)
                    {
                        //this is a visible annotation highlight
                        highlightTransitionPosition = highlights.GetNextHighlightChangePosition(highlightTransitionPosition, LogicalDirection.Forward);
                        Debug.Assert(!highlightTransitionPosition.IsNull, "Highlight start not followed by highlight end!");
                        fixedHighlightType = FixedHighlightType.AnnotationHighlight;
                        background = highlightSegment.Fill;
                    }
                }

                //generate fixed highlight if a highlight was has been found
                if (fixedHighlightType != FixedHighlightType.None)
                {
                    this.FixedContainer.GetMultiHighlights((FixedTextPointer)highlightRangeStart.CreateDynamicTextPointer(LogicalDirection.Forward),
                                                            (FixedTextPointer)highlightTransitionPosition.CreateDynamicTextPointer(LogicalDirection.Forward),
                                                           _highlights, fixedHighlightType, foreground, background);
                }
            }

            ArrayList dirtyPages = new ArrayList();
            IList ranges = args.Ranges;

            // Find the dirty page
            for (int i = 0; i < ranges.Count; i++)
            {
                TextSegment textSegment = (TextSegment)ranges[i];
                int startPage = this.FixedContainer.GetPageNumber(textSegment.Start);
                int endPage =  this.FixedContainer.GetPageNumber(textSegment.End);

                for (int count = startPage; count <= endPage; count ++)
                {
                    if (dirtyPages.IndexOf(count) < 0)
                    {
                        dirtyPages.Add(count);
                    }
                }
            }

            ICollection<FixedPage> newHighlightPages = _highlights.Keys as ICollection<FixedPage>;
            
            //Also dirty the pages that had highlights before but not anymore
            foreach (FixedPage page in oldHighlightPages)
            {
                if (!newHighlightPages.Contains(page))
                {
                    int pageNo = GetIndexOfPage(page);
                    Debug.Assert(pageNo >= 0 && pageNo<PageCount);
                    if (pageNo >=0 && pageNo < PageCount && dirtyPages.IndexOf(pageNo) < 0)
                    {
                        dirtyPages.Add(pageNo);
                    }
                }
            }
            dirtyPages.Sort();

            foreach (int i in dirtyPages)
            {
                HighlightVisual hv = HighlightVisual.GetHighlightVisual(SyncGetPage(i, false /*forceReload*/));

                if (hv != null)
                {
                    hv.InvalidateHighlights();
                }
            }
        }


        //---------------------------------------
        // IDP
        //---------------------------------------

        private object GetPageAsyncDelegate(object arg)
        {
            GetPageAsyncRequest asyncRequest = (GetPageAsyncRequest)arg;
            PageContent pc = asyncRequest.PageContent;
            DocumentsTrace.FixedFormat.IDF.Trace(string.Format("IDP.GetPageAsyncDelegate {0}", Pages.IndexOf(pc)));
            // Initiate request for page if necessary
            if (!_pendingPages.Contains(pc))
            {
                _pendingPages.Add(pc);
                // Initiate an async loading of the page
                pc.GetPageRootCompleted += new GetPageRootCompletedEventHandler(OnGetPageRootCompleted);
                pc.GetPageRootAsync(false /*forceReload*/);
                if (asyncRequest.Cancelled)
                {
                    pc.GetPageRootAsyncCancel();
                }
            }
            return null;
        }


        private void OnGetPageRootCompleted(object sender, GetPageRootCompletedEventArgs args)
        {
            DocumentsTrace.FixedFormat.IDF.Trace(string.Format("IDP.OnGetPageRootCompleted {0}", Pages.IndexOf((PageContent)sender)));
            // Mark this page as no longer pending
            PageContent pc = (PageContent)sender;
            pc.GetPageRootCompleted -= new GetPageRootCompletedEventHandler(OnGetPageRootCompleted);
            _pendingPages.Remove(pc);

            // Notify all outstanding request for this particular page
            ArrayList completedRequests = new ArrayList();
            IEnumerator<KeyValuePair<Object, GetPageAsyncRequest>> ienum = _asyncOps.GetEnumerator();
            try
            {
                while (ienum.MoveNext())
                {
                    GetPageAsyncRequest asyncRequest = ienum.Current.Value;
                    // Process any outstanding request for this PageContent
                    if (asyncRequest.PageContent == pc)
                    {
                        completedRequests.Add(ienum.Current.Key);
                        DocumentPage result = DocumentPage.Missing;
                        if (!asyncRequest.Cancelled)
                        {
                            // Do synchronous measure since we have obtained the page
                            if (!args.Cancelled && (args.Error == null))
                            {
                                FixedPage c = (FixedPage)args.Result;
                                Size fixedSize = ComputePageSize(c);

//                              Measure / Arrange in GetVisual override of FixedDocumentPage, not here
                                result = new FixedDocumentPage(this, c, fixedSize, Pages.IndexOf(pc));
                            }
                        }
                        // this could throw
                        _NotifyGetPageAsyncCompleted(result, asyncRequest.PageNumber, args.Error, asyncRequest.Cancelled, asyncRequest.UserState);
                    }
                }
            }
            finally
            {
                // Remove completed requests from current async ops list
                foreach (Object userState in completedRequests)
                {
                    _asyncOps.Remove(userState);
                }
            }
        }


        // Notify the caller of IDFAsync.MeasurePageAsync
        private void _NotifyGetPageAsyncCompleted(DocumentPage page, int pageNumber, Exception error, bool cancelled, object userState)
        {
            DocumentsTrace.FixedFormat.IDF.Trace(string.Format("IDP._NotifyGetPageAsyncCompleted {0} {1} {2} {3} {4}", page, pageNumber, error, cancelled, userState));
            _paginator.NotifyGetPageCompleted(new GetPageCompletedEventArgs(
                                               page,
                                               pageNumber,
                                               error,
                                               cancelled,
                                               userState
                                               ));
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

        private IDictionary<Object, GetPageAsyncRequest> _asyncOps;
        private IList<PageContent> _pendingPages;
        private PageContentCollection _pages;
        private PageContent _partialPage;       // the current partially loaded page.
        private Dictionary<FixedPage, ArrayList> _highlights;
        private double _pageWidth = 8.5 * 96.0d;
        private double _pageHeight = 11 * 96.0d;     // default page size
        private FixedTextContainer _fixedTextContainer;
        private RubberbandSelector _rubberbandSelector;
        private bool _navigateAfterPagination = false ;
        private string _navigateFragment;
        private FixedDocumentPaginator _paginator;
        private DocumentReference _documentReference;
        private bool _hasExplicitStructure;
        
        private const string _structureRelationshipName       = "http://schemas.microsoft.com/xps/2005/06/documentstructure";
        private const string _storyFragmentsRelationshipName  = "http://schemas.microsoft.com/xps/2005/06/storyfragments";
        private static readonly ContentType _storyFragmentsContentType = new ContentType("application/vnd.ms-package.xps-storyfragments+xml");
        private static readonly ContentType _documentStructureContentType = new ContentType("application/vnd.ms-package.xps-documentstructure+xml");

        // Caches the UIElement's DependencyObjectType
        private static DependencyObjectType UIElementType = DependencyObjectType.FromSystemTypeInternal(typeof(UIElement));
        #endregion Private Fields


        //--------------------------------------------------------------------
        //
        // Nested Class
        //
        //---------------------------------------------------------------------

        #region Nested Class
        private class GetPageAsyncRequest
        {
            internal GetPageAsyncRequest(PageContent pageContent, int pageNumber, object userState)
            {
                PageContent = pageContent;
                PageNumber = pageNumber;
                UserState = userState;
                Cancelled = false;
            }

            internal PageContent PageContent;
            internal int PageNumber;
            internal object UserState;
            internal bool Cancelled;
        }
        #endregion Nested Class
    }

    //=====================================================================
    //
    // FixedDocument's implemenation of DocumentPage
    //
    internal sealed class FixedDocumentPage : DocumentPage, IServiceProvider
    {
        //--------------------------------------------------------------------
        //
        // Ctors
        //
        //---------------------------------------------------------------------

        #region Ctors
        internal FixedDocumentPage(FixedDocument panel, FixedPage page, Size fixedSize, int index) : 
            base(page, fixedSize, new Rect(fixedSize), new Rect(fixedSize))
        {
            Debug.Assert(panel != null && page != null);
            _panel = panel;
            _page = page;
            _index = index;
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
        /// <remarks>
        /// FixedDocument currently supports TextView.
        /// </remarks>
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
                return this.TextView;
            }

            return null;
        }
        #endregion IServiceProvider

        //--------------------------------------------------------------------
        //
        // Public  Methods
        //
        //---------------------------------------------------------------------


        //--------------------------------------------------------------------
        //
        // Public  Properties
        //
        //---------------------------------------------------------------------

        public override Visual Visual
        {
            get 
            {
                if (!_layedOut)
                {
                    _layedOut = true;

                    UIElement e;
                    if ((e = ((object)base.Visual) as UIElement)!=null)
                    {
                        e.Measure(base.Size);
                        e.Arrange(new Rect(base.Size));
                    }
                }
                return base.Visual; 
            }
        }

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------
        #region Internal Properties

        internal ContentPosition ContentPosition
        {
            get
            {
                FlowPosition flowPosition = _panel.FixedContainer.FixedTextBuilder.GetPageStartFlowPosition(_index);
                return new FixedTextPointer(true, LogicalDirection.Forward, flowPosition);
            }
        }

        internal FixedPage FixedPage
        {
            get
            {
                return this._page;
            }
        }

        internal int PageIndex
        {
            get
            {
                return this._panel.GetIndexOfPage(this._page);
            }
        }

        internal FixedTextView TextView
        {
            get
            {
                if (_textView == null)
                {
                    _textView = new FixedTextView(this);
                }
                return _textView;
            }
        }

        internal FixedDocument Owner
        {
            get
            {
                return _panel;
            }
        }

        internal FixedTextContainer TextContainer
        {
            get
            {
                return this._panel.FixedContainer;
            }
        }

        #endregion Internal Properties
        //--------------------------------------------------------------------
        //
        // Internal Event
        //
        //---------------------------------------------------------------------
        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private readonly FixedDocument _panel;
        private readonly FixedPage _page;
        private readonly int _index;
        private bool _layedOut;
        // Text OM
        private FixedTextView _textView;
        #endregion Private Fields
    }
}


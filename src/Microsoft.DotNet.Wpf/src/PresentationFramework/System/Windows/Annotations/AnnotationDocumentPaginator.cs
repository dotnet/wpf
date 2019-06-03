// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Annotations.Storage;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using MS.Internal;
using MS.Internal.Annotations;
using MS.Internal.Annotations.Anchoring;
using MS.Internal.Annotations.Component;
using MS.Internal.Documents;

namespace System.Windows.Annotations
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class AnnotationDocumentPaginator : DocumentPaginator
    {
        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Public Constructors
        /// <summary>
        ///     Create an instance of AnnotationDocumentPaginator for a given document and annotation store.
        /// </summary>
        /// <param name="originalPaginator">document to add annotations to</param>
        /// <param name="annotationStore">store to retrieve annotations from</param>
        public AnnotationDocumentPaginator(DocumentPaginator originalPaginator, Stream annotationStore) : this(originalPaginator, new XmlStreamStore(annotationStore), FlowDirection.LeftToRight)
        {
        }

        /// <summary>
        ///     Create an instance of AnnotationDocumentPaginator for a given document and annotation store.
        /// </summary>
        /// <param name="originalPaginator">document to add annotations to</param>
        /// <param name="annotationStore">store to retrieve annotations from</param>
        /// <param name="flowDirection"></param>
        public AnnotationDocumentPaginator(DocumentPaginator originalPaginator, Stream annotationStore, FlowDirection flowDirection) : this(originalPaginator, new XmlStreamStore(annotationStore), flowDirection)
        {
        }

        /// <summary>
        ///     Create an instance of AnnotationDocumentPaginator for a given document and annotation store.
        /// </summary>
        /// <param name="originalPaginator">document to add annotations to</param>
        /// <param name="annotationStore">store to retrieve annotations from</param>
        public AnnotationDocumentPaginator(DocumentPaginator originalPaginator, AnnotationStore annotationStore) : this(originalPaginator, annotationStore, FlowDirection.LeftToRight)
        {
        }

        /// <summary>
        ///     Create an instance of AnnotationDocumentPaginator for a given document and annotation store.
        /// </summary>
        /// <param name="originalPaginator">document to add annotations to</param>
        /// <param name="annotationStore">store to retrieve annotations from</param>
        /// <param name="flowDirection"></param>
        public AnnotationDocumentPaginator(DocumentPaginator originalPaginator, AnnotationStore annotationStore, FlowDirection flowDirection)
        {
            _isFixedContent = originalPaginator is FixedDocumentPaginator || originalPaginator is FixedDocumentSequencePaginator;

            if (!_isFixedContent && !(originalPaginator is FlowDocumentPaginator))
                throw new ArgumentException(SR.Get(SRID.OnlyFlowAndFixedSupported));

            _originalPaginator = originalPaginator;
            _annotationStore = annotationStore;
            _locatorManager = new LocatorManager(_annotationStore);
            _flowDirection = flowDirection;

            // Register for events
            _originalPaginator.GetPageCompleted += new GetPageCompletedEventHandler(HandleGetPageCompleted);
            _originalPaginator.ComputePageCountCompleted += new AsyncCompletedEventHandler(HandleComputePageCountCompleted);
            _originalPaginator.PagesChanged += new PagesChangedEventHandler(HandlePagesChanged);
        }

        #endregion Public Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Whether PageCount is currently valid. If False, then the value of 
        /// PageCount is the number of pages that have currently been formatted.
        /// </summary>
        /// <remarks>
        /// This value may revert to False after being True, in cases where 
        /// PageSize or content changes, forcing a repagination.
        /// </remarks>
        public override bool IsPageCountValid
        {
            get { return _originalPaginator.IsPageCountValid; }
        }

        /// <summary>
        /// If IsPageCountValid is True, this value is the number of pages 
        /// of content. If False, this is the number of pages that have 
        /// currently been formatted.
        /// </summary>
        /// <remarks>
        /// Value may change depending upon changes in PageSize or content changes.
        /// </remarks>
        public override int PageCount
        {
            get { return _originalPaginator.PageCount;  }
        }

        /// <summary>
        /// The suggested size for formatting pages.
        /// </summary>
        /// <remarks>
        /// Note that the paginator may override the specified page size. Users 
        /// should check DocumentPage.Size.
        /// </remarks>
        public override Size PageSize
        {
            get { return _originalPaginator.PageSize; }
            set { _originalPaginator.PageSize = value; }
        }

        /// <summary>
        /// <see cref="DocumentPaginator.Source"/>
        /// </summary>
        public override IDocumentPaginatorSource Source
        {
            get { return _originalPaginator.Source; }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Retrieves the DocumentPage for the given page number. PageNumber 
        /// is zero-based.
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <returns>
        /// Returns DocumentPage.Missing if the given page does not exist.
        /// </returns>
        /// <remarks>
        /// Multiple requests for the same page number may return the same 
        /// object (this is implementation specific).
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if PageNumber is negative.
        /// </exception>
        public override DocumentPage GetPage(int pageNumber)
        {
            DocumentPage documentPage = _originalPaginator.GetPage(pageNumber);

            if (documentPage != DocumentPage.Missing)
            {
                documentPage = ComposePageWithAnnotationVisuals(pageNumber, documentPage);
            }

            return documentPage;
        }

        /// <summary>
        /// Async version of <see cref="DocumentPaginator.GetPage"/>
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if PageNumber is negative.
        /// </exception>
        public override void GetPageAsync(int pageNumber, object userState)
        {
            _originalPaginator.GetPageAsync(pageNumber, userState);
        }

        /// <summary>
        /// Computes the number of pages of content. IsPageCountValid will be 
        /// True immediately after this is called.
        /// </summary>
        /// <remarks>
        /// If content is modified or PageSize is changed (or any other change 
        /// that causes a repagination) after this method is called, 
        /// IsPageCountValid will likely revert to False.
        /// </remarks>
        public override void ComputePageCount()
        {
            _originalPaginator.ComputePageCount();
        }

        /// <summary>
        /// Async version of <see cref="DocumentPaginator.ComputePageCount"/>
        /// </summary>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        public override void ComputePageCountAsync(object userState)
        {
            _originalPaginator.ComputePageCountAsync(userState);
        }

        /// <summary>
        /// Cancels all asynchronous calls made with the given userState. 
        /// If userState is NULL, all asynchronous calls are cancelled.
        /// </summary>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        public override void CancelAsync(object userState)
        {
            _originalPaginator.CancelAsync(userState);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods
        
        /// <summary>
        ///    We are being notified by the wrapped paginator.  If getting the page
        ///    was successful, we use the resulting page to produce a new page that
        ///    includes annotatons.  In either case, we fire an event from this instance.
        /// </summary>
        /// <param name="sender">source of the event</param>
        /// <param name="e">the args for this event</param>
        private void HandleGetPageCompleted(object sender, GetPageCompletedEventArgs e)
        {            
            // If no errors, not cancelled, and page isn't missing, create a new page
            // with annotations and create a new event args for that page.
            if (!e.Cancelled && e.Error == null && e.DocumentPage != DocumentPage.Missing) 
            {      
                // Since we can't change the page the args is holding we create a new
                // args object with the page we produce.
                DocumentPage documentPage = ComposePageWithAnnotationVisuals(e.PageNumber, e.DocumentPage);

                e = new GetPageCompletedEventArgs(documentPage, e.PageNumber, e.Error, e.Cancelled, e.UserState);
            }

            // Fire the event
            OnGetPageCompleted(e);
        }

        /// <summary>
        ///     We are notified by the wrapped paginator.  In response we fire
        ///     an event from this instance.
        /// </summary>
        /// <param name="sender">source of the event</param>
        /// <param name="e">args for the event</param>
        private void HandleComputePageCountCompleted(object sender, AsyncCompletedEventArgs e)
        {
            // Fire the event
            OnComputePageCountCompleted(e);
        }

        /// <summary>
        ///     We are notified by the wrapped paginator.  In response we fire
        ///     an event from this instance.
        /// </summary>
        /// <param name="sender">source of the event</param>
        /// <param name="e">args for the event</param>
        private void HandlePagesChanged(object sender, PagesChangedEventArgs e)
        {
            // Fire the event
            OnPagesChanged(e);
        }

        ///<summary>
        /// For a given page # and a page, returns a page that include the original 
        /// page along with any annotations that are displayed on that page.  
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageNumber"></param>
        private DocumentPage ComposePageWithAnnotationVisuals(int pageNumber, DocumentPage page)
        {
            // Need to store these because our current highlight mechanism
            // causes the page to be disposed
            Size tempSize = page.Size;

            AdornerDecorator decorator = new AdornerDecorator();
            decorator.FlowDirection = _flowDirection;
            DocumentPageView dpv = new DocumentPageView();
            dpv.UseAsynchronousGetPage = false;
            dpv.DocumentPaginator = _originalPaginator;
            dpv.PageNumber = pageNumber;
            decorator.Child = dpv;

            // Arrange the first time to get the DPV setup right
            decorator.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            decorator.Arrange(new Rect(decorator.DesiredSize));
            decorator.UpdateLayout();

            // Create a new one for each page because it keeps a cache of annotation components
            // and we don't want to be holding them in memory once the page is no longer used
            AnnotationComponentManager manager = new MS.Internal.Annotations.Component.AnnotationComponentManager(null); 

            // Setup DPs and processors for annotation handling.  If the service isn't already
            // enabled the processors will be registered by the service when it is enabled.
            if (_isFixedContent)
            {
                // Setup service to look for FixedPages in the content
                AnnotationService.SetSubTreeProcessorId(decorator, FixedPageProcessor.Id);                
                // If the service is already registered, set it up for fixed content
                _locatorManager.RegisterSelectionProcessor(new FixedTextSelectionProcessor(), typeof(TextRange));
            }
            else 
            {
                // Setup up an initial DataId used to identify the document
                AnnotationService.SetDataId(decorator, "FlowDocument");                
                _locatorManager.RegisterSelectionProcessor(new TextViewSelectionProcessor(), typeof(DocumentPageView));
                // Setup the selection processor, pre-targeting it at a specific DocumentPageView
                TextSelectionProcessor textSelectionProcessor = new TextSelectionProcessor();
                textSelectionProcessor.SetTargetDocumentPageView(dpv);
                _locatorManager.RegisterSelectionProcessor(textSelectionProcessor, typeof(TextRange));                
            }

            // Get attached annotations for the page
            IList<IAttachedAnnotation> attachedAnnotations = ProcessAnnotations(dpv);

            // Now make sure they have a visual component added to the DPV via the component manager
            foreach (IAttachedAnnotation attachedAnnotation in attachedAnnotations)
            {
                if (attachedAnnotation.AttachmentLevel != AttachmentLevel.Unresolved && attachedAnnotation.AttachmentLevel != AttachmentLevel.Incomplete)
                {
                    manager.AddAttachedAnnotation(attachedAnnotation, false);
                }
            }

            // Update layout a second time to get the annotations layed out correctly
            decorator.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            decorator.Arrange(new Rect(decorator.DesiredSize));
            decorator.UpdateLayout();

/*          Look into using the VisualBrush in order to get a dead page instead of a live one...
            VisualBrush visualBrush = new VisualBrush(decorator);
            Rectangle rectangle = new Rectangle();
            rectangle.Fill = visualBrush;
            rectangle.Margin = new Thickness(0);
*/

            return new AnnotatedDocumentPage(page, decorator, tempSize, new Rect(tempSize), new Rect(tempSize));
        }

        private IList<IAttachedAnnotation> ProcessAnnotations(DocumentPageView dpv)
        {
            if (dpv == null)
                throw new ArgumentNullException("dpv");

            IList<IAttachedAnnotation> attachedAnnotations = new List<IAttachedAnnotation>();
            IList<ContentLocatorBase> locators = _locatorManager.GenerateLocators(dpv);
            if (locators.Count > 0)
            {
                // LocatorBases for a single node should always be Locators
                ContentLocator[] lists = new ContentLocator[locators.Count];
                locators.CopyTo(lists, 0);

                IList<Annotation> annotations = _annotationStore.GetAnnotations(lists[0]);

                foreach (ContentLocator locator in locators)
                {
                    if (locator.Parts[locator.Parts.Count - 1].NameValuePairs.ContainsKey(TextSelectionProcessor.IncludeOverlaps))
                    {
                        locator.Parts.RemoveAt(locator.Parts.Count - 1);
                    }
                }

                foreach (Annotation annotation in annotations)
                {
                    foreach (AnnotationResource anchor in annotation.Anchors)
                    {
                        foreach (ContentLocatorBase locator in anchor.ContentLocators)
                        {
                            AttachmentLevel attachmentLevel;
                            object attachedAnchor = _locatorManager.FindAttachedAnchor(dpv, lists, locator, out attachmentLevel);

                            if (attachmentLevel != AttachmentLevel.Unresolved)
                            {              
                                Invariant.Assert(VisualTreeHelper.GetChildrenCount(dpv) == 1, "DocumentPageView has no visual children.");
                                DependencyObject firstElement = VisualTreeHelper.GetChild(dpv, 0);

                                attachedAnnotations.Add(new AttachedAnnotation(_locatorManager, annotation, anchor, attachedAnchor, attachmentLevel, firstElement as DocumentPageHost));

                                // Only process one locator per resource
                                break;
                            }
                        }
                    }
                }
            }

            return attachedAnnotations;
        }

        #endregion Private Methods

        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Classes

        /// <summary>
        /// AnnotationDocumentPage implements IContentHost and delegates all calls
        /// to the wrapped DocumentPage.  This allows XPS serialization to make use
        /// of the IContentHost info from the wrapped page in order to provide 
        /// clickable hyperlinks.
        /// </summary>
        private class AnnotatedDocumentPage : DocumentPage, IContentHost
        {
            /// <summary>
            /// Creates an AnnotationDocumentPage for the wrapped page with the specified
            /// visual and sizes.
            /// </summary>
            public AnnotatedDocumentPage(DocumentPage basePage, Visual visual, Size pageSize, Rect bleedBox, Rect contentBox)
                : base(visual, pageSize, bleedBox, contentBox)
            {
                _basePage = basePage as IContentHost;
            }

            /// <summary>
            /// Returns the set of HostedElements in this page.
            /// </summary>
            public IEnumerator<IInputElement> HostedElements
            {
                get
                {
                    if (_basePage != null)
                    {
                        return _basePage.HostedElements;
                    }
                    else
                    {
                        return new HostedElements(new ReadOnlyCollection<TextSegment>(new List<TextSegment>(0)));
                    }
                }
            }

            /// <summary>
            /// Returns the rectangles that are 'hot spots' for the specified element.
            /// </summary>
            public ReadOnlyCollection<Rect> GetRectangles(ContentElement child)
            {
                if (_basePage != null)
                {
                    return _basePage.GetRectangles(child);
                }
                else
                {
                    return new ReadOnlyCollection<Rect>(new List<Rect>(0));
                }
            }

            /// <summary>
            /// Performs a hit-test with the given point on this page.
            /// </summary>
            public IInputElement InputHitTest(Point point)
            {
                if (_basePage != null)
                {
                    return _basePage.InputHitTest(point);
                }
                else
                {
                    return null;
                }
            }

            /// <summary>
            /// Lets the page recalculate the rectangles for this element when its desired
            /// size has changed.
            /// </summary>
            public void OnChildDesiredSizeChanged(UIElement child)
            {
                if (_basePage != null)
                {
                    _basePage.OnChildDesiredSizeChanged(child);
                }
            }


            // DocumentPage being wrapped by this DocumentPage
            private IContentHost _basePage;
        }

        #endregion Private Classes

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Store used to retrieve annotations from
        private AnnotationStore _annotationStore;
        // Document we are loading annotations for
        private DocumentPaginator _originalPaginator;
        // LocatorManager used to generate and resolve locators
        private LocatorManager _locatorManager;
        // Specifies whether the paginator is for fixed content
        private bool _isFixedContent;
        // FlowDirection to use when laying the annotations out
        private FlowDirection _flowDirection;

        #endregion Private Fields
    }
}




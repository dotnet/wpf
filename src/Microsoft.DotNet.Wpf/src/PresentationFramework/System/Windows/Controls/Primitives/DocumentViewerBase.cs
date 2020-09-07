// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: DocumentViewerBase is a minimal base class, providing only
//              the functionality common across document viewing scenarios.
//              The base class provides no user interface, very few properties,
//              and minimal policy. Functionality included in the base class:
//              BringIntoView support & Print API services
//              and Annotation support.
//
#pragma warning disable 1634, 1691  // avoid generating warnings about unknown
                                    // message numbers and unknown pragmas for PRESharp contol

using System.Collections;               // IEnumerator
using System.Collections.Generic;       // List<T>
using System.Collections.ObjectModel;   // ReadOnlyCollection<T>
using System.Windows.Annotations;       // AnnotationService
using System.Windows.Automation;        // AutomationPattern
using System.Windows.Automation.Peers;  // AutomationPeer
using System.Windows.Documents;         // IDocumentPaginatorSource, ...
using System.Windows.Documents.Serialization;  // WritingCompletedEventArgs
using System.Windows.Input;             // UICommand
using System.Windows.Media;             // Visual
using System.Windows.Markup;            // IAddChild, XamlSerializerUtil, ContentPropertyAttribute
using System.Windows.Threading;         // DispatcherPriority
using MS.Internal;                      // Invariant, DoubleUtil
using MS.Internal.KnownBoxes;           // BooleanBoxes
using MS.Internal.Annotations;          // TextAnchor
using MS.Internal.Annotations.Anchoring;// DataIdProcessor, FixedPageProcessor, ...
using MS.Internal.Automation;           // TextAdaptor
using MS.Internal.Documents;            // MultiPageTextView
using MS.Internal.Controls;             // EmptyEnumerator
using MS.Internal.Commands;             // CommandHelpers

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// DocumentViewerBase is a minimal base class, providing only the functionality
    /// common across document viewing scenarios. The base class provides no user
    /// interface, very few properties, and minimal policy. Functionality included in
    /// the base class: BringIntoView support and Print API services
    /// and Annotation support.
    /// </summary>
    [ContentProperty("Document")]
    public abstract class DocumentViewerBase : Control, IAddChild, IServiceProvider
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initializes class-wide settings.
        /// </summary>
        static DocumentViewerBase()
        {
            // Create our CommandBindings
            CreateCommandBindings();

            // Register event handlers
            EventManager.RegisterClassHandler(typeof(DocumentViewerBase), RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(HandleRequestBringIntoView));

            // Default value for AutoWordSelection is false.  We want true.
            TextBoxBase.AutoWordSelectionProperty.OverrideMetadata(typeof(DocumentViewerBase), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Instantiates a new instance of a DocumentViewerBase.
        /// </summary>
        protected DocumentViewerBase()
            : base()
        {
            _pageViews = new ReadOnlyCollection<DocumentPageView>(new List<DocumentPageView>());
            // By default text selection is enabled.
            SetFlags(true, Flags.IsSelectionEnabled);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Called when the Template's tree has been generated
        /// </summary>
        /// <returns>Whether Visuals were added to the tree.</returns>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            // Update collection of DocumentPageViews if the Visual tree has
            // been updated through control template
            UpdatePageViews();
        }

        /// <summary>
        /// Moves the master page to the previous page.
        /// </summary>
        public void PreviousPage()
        {
            OnPreviousPageCommand();
        }

        /// <summary>
        /// Moves the master page to the next page.
        /// </summary>
        public void NextPage()
        {
            OnNextPageCommand();
        }

        /// <summary>
        /// Moves the master page to the first page.
        /// </summary>
        public void FirstPage()
        {
            OnFirstPageCommand();
        }

        /// <summary>
        /// Moves the master page to the last page.
        /// </summary>
        public void LastPage()
        {
            OnLastPageCommand();
        }

        /// <summary>
        /// Moves the master page to the specified page.
        /// </summary>
        /// <param name="pageNumber"></param>
        public void GoToPage(int pageNumber)
        {
            OnGoToPageCommand(pageNumber);
        }

        /// <summary>
        /// Invokes the Print Dialog. This is analogous to the ApplicationCommands.Print.
        /// </summary>
        public void Print()
        {
            OnPrintCommand();
        }

        /// <summary>
        /// Cancels current printing job. This is analogous to the ApplicationCommands.CancelPrint.
        /// </summary>
        public void CancelPrint()
        {
            OnCancelPrintCommand();
        }

        /// <summary>
        /// Whether the master page can be moved to the specified page.
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        public virtual bool CanGoToPage(int pageNumber)
        {
            // Can navigate to a page, if:
            // a) the number is in valid range between 1 and PageCount, or
            // b) Paginator is set and pageNumber is PageCount+1.
            return (pageNumber > 0 && pageNumber <= this.PageCount) || 
                ((_document != null) && (pageNumber - 1 == this.PageCount) && !_document.DocumentPaginator.IsPageCountValid);
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The IDocumentPaginatorSource to be paginated and viewed.
        /// </summary>
        public IDocumentPaginatorSource Document
        {
            get { return _document; }
            set { SetValue(DocumentProperty, value); }
        }

        /// <summary>
        /// The number of pages currently available for viewing. This value
        /// is updated as content is paginated, and will change dramatically
        /// when the content is resized, or edited.
        /// </summary>
        public int PageCount
        {
            get { return (int) GetValue(PageCountProperty); }
        }

        /// <summary>
        /// The one-based page number of the page being displayed on the master
        /// page DocumentPageView. If there is no content, this value will be 0.
        /// </summary>
        public virtual int MasterPageNumber
        {
            get { return (int) GetValue(MasterPageNumberProperty); }
        }

        /// <summary>
        /// Whether the viewer can move the master page to the previous page.
        /// </summary>
        public virtual bool CanGoToPreviousPage
        {
            get { return (bool) GetValue(CanGoToPreviousPageProperty); }
        }

        /// <summary>
        /// Whether the viewer can advance the master page to the next page.
        /// </summary>
        public virtual bool CanGoToNextPage
        {
            get { return (bool) GetValue(CanGoToNextPageProperty); }
        }

        /// <summary>
        /// A read-only collection of the DocumentPageView objects contained
        /// within the viewer. These objects are manipulated by the viewer
        /// in order to display content.
        /// </summary>
        [CLSCompliant(false)]
        public ReadOnlyCollection<DocumentPageView> PageViews
        {
            get { return _pageViews; }
        }

        #region Public Dynamic Properties

        /// <summary>\
        /// <see cref="Document"/>
        /// </summary>
        public static readonly DependencyProperty DocumentProperty =
                DependencyProperty.Register(
                        "Document",
                        typeof(IDocumentPaginatorSource),
                        typeof(DocumentViewerBase),
                        new FrameworkPropertyMetadata(
                                null,
                                new PropertyChangedCallback(DocumentChanged)));

        /// <summary>
        /// <see cref="PageCount"/>
        /// </summary>
        protected static readonly DependencyPropertyKey PageCountPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "PageCount",
                        typeof(int),
                        typeof(DocumentViewerBase),
                        new FrameworkPropertyMetadata(0));

        /// <summary>
        /// <see cref="PageCount"/>
        /// </summary>
        public static readonly DependencyProperty PageCountProperty =
                PageCountPropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="MasterPageNumber"/>
        /// </summary>
        protected static readonly DependencyPropertyKey MasterPageNumberPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "MasterPageNumber",
                        typeof(int),
                        typeof(DocumentViewerBase),
                        new FrameworkPropertyMetadata(0));

        /// <summary>
        /// <see cref="MasterPageNumber"/>
        /// </summary>
        public static readonly DependencyProperty MasterPageNumberProperty =
                MasterPageNumberPropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="CanGoToPreviousPage"/>
        /// </summary>
        protected static readonly DependencyPropertyKey CanGoToPreviousPagePropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanGoToPreviousPage",
                        typeof(bool),
                        typeof(DocumentViewerBase),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// <see cref="CanGoToPreviousPage"/>
        /// </summary>
        public static readonly DependencyProperty CanGoToPreviousPageProperty =
                CanGoToPreviousPagePropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="CanGoToNextPage"/>
        /// </summary>
        protected static readonly DependencyPropertyKey CanGoToNextPagePropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanGoToNextPage",
                        typeof(bool),
                        typeof(DocumentViewerBase),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// <see cref="CanGoToNextPage"/>
        /// </summary>
        public static readonly DependencyProperty CanGoToNextPageProperty =
                CanGoToNextPagePropertyKey.DependencyProperty;

        /// <summary>
        /// Attached property used to signify which of the child DocumentPageView objects
        /// is the master page. If more than one DocumentPageView has this value set,
        /// then the first tagged DocumentPageView in the tree (depth-first) is designated
        /// the master. If none of the children have this property set, then the depth-first
        /// PageView is designated the master.
        /// </summary>
        public static readonly DependencyProperty IsMasterPageProperty =
                DependencyProperty.RegisterAttached(
                        "IsMasterPage",
                        typeof(bool),
                        typeof(DocumentViewerBase),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// DependencyProperty getter for <see cref="IsMasterPageProperty" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static bool GetIsMasterPage(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(IsMasterPageProperty);
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="IsMasterPageProperty" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetIsMasterPage(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(IsMasterPageProperty, value);
        }

        #endregion Public Dynamic Properties

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Public Events
        //
        //-------------------------------------------------------------------

        #region Public Events

        /// <summary>
        /// Fired when collection of DocumentPageViews is changed.
        /// </summary>
        public event EventHandler PageViewsChanged;

        #endregion Public Events

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer() 
        {
            return new DocumentViewerBaseAutomationPeer(this);
        }

        protected override void OnDpiChanged(DpiScale oldDpiScaleInfo, DpiScale newDpiScaleInfo)
        {
            FlowDocument flowDocument = _document as FlowDocument;
            flowDocument?.SetDpi(newDpiScaleInfo);
        }

        /// <summary>
        /// Invalidates the PageViews collection, triggering a call to GetPageViews.
        /// </summary>
        protected void InvalidatePageViews()
        {
            // Update collection of DocumentPageViews if  the collection has
            // been explicitly invalidated.
            UpdatePageViews();
            InvalidateMeasure();
        }

        /// <summary>
        /// Returns the master DocumentPageView.
        /// </summary>
        protected DocumentPageView GetMasterPageView()
        {
            int index;
            DocumentPageView masterPageView = null;

            // Search for the first element with IsMasterPage property set.
            for (index = 0; index < _pageViews.Count; index++)
            {
                if (GetIsMasterPage(_pageViews[index]))
                {
                    masterPageView = _pageViews[index];
                    break;
                }
            }
            // If none of the DocumentPageViews have this property set,
            // then use the first one in the collection.
            if (masterPageView == null)
            {
                masterPageView = _pageViews.Count > 0 ? _pageViews[0] : null;
            }
            return masterPageView;
        }

        /// <summary>
        /// Creates a collection of DocumentPageView objects used to display Document.
        /// </summary>
        /// <param name="changed">True if the collection is different than the public PageViews collection.</param>
        /// <returns>Collection of DocumentPageView objects used to display Document.</returns>
        protected virtual ReadOnlyCollection<DocumentPageView> GetPageViewsCollection(out bool changed)
        {
            List<DocumentPageView> pageViewList;
            AdornerDecorator adornerDecorator;

            // By default retrieve all DocumentPageViews from style.
            pageViewList = new List<DocumentPageView>(1/* simplest case has just one element */);
            FindDocumentPageViews(this, pageViewList);

            // By default use AdornerDecorator.Child as RenderScope for TextEditor. Retieve
            // AdornerDecorator from the style.
            adornerDecorator = FindAdornerDecorator(this);
            this.TextEditorRenderScope = (adornerDecorator != null) ? (adornerDecorator.Child as FrameworkElement) : null;

            // Since existing DocumentPageViews are being replaced, need to disconnect
            // them from the Document.
            for (int index = 0; index < _pageViews.Count; index++)
            {
                _pageViews[index].DocumentPaginator = null;
            }

            changed = true;
            return new ReadOnlyCollection<DocumentPageView>(pageViewList);
        }

        /// <summary>
        /// Called when the PageViews collection is modified; this occurs when GetPageViews
        /// returns True or if the control's template is modified.
        /// </summary>
        protected virtual void OnPageViewsChanged()
        {
            // Raise notification about change to DocumentPageView collection.
            if (this.PageViewsChanged != null)
            {
                this.PageViewsChanged(this, EventArgs.Empty);
            }
            // Change of DocumentPageView collection may cause invalidation of content
            // represented by DocumentPageViews.
            OnMasterPageNumberChanged();
        }

        /// <summary>
        /// Called when the MasterPageNumber property is changed, this occurs when the
        /// developer manually sets the property, or when the FirstPage, NextPage,
        /// etc commands are executed.
        /// </summary>
        protected virtual void OnMasterPageNumberChanged()
        {
            // Invalidation of MasterPage invalidates following properties:
            //      - MasterPageNumber
            //      - CanGoToPreviousPage
            //      - CanGoToNextPage
            UpdateReadOnlyProperties(true, true);
        }

        /// <summary>
        /// Called when a BringIntoView event is bubbled up from the Document.
        /// Base implementation will move the master page to the page
        /// on which the element occurs.
        /// </summary>
        /// <param name="element">The object to make visible.</param>
        /// <param name="rect">The rectangular region in the object's coordinate space which should be made visible.</param>
        /// <param name="pageNumber"></param>
        protected virtual void OnBringIntoView(DependencyObject element, Rect rect, int pageNumber)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            OnGoToPageCommand(pageNumber);
        }

        /// <summary>
        /// Handler for the PreviousPage command.
        /// </summary>
        protected virtual void OnPreviousPageCommand()
        {
            // If can go to the previous page, shift all pages
            // by decrementing page number by 1.
            if (this.CanGoToPreviousPage)
            {
                ShiftPagesByOffset(-1);
            }
        }

        /// <summary>
        /// Handler for the NextPage command.
        /// </summary>
        protected virtual void OnNextPageCommand()
        {
            // If can go to the next page, shift all pages
            // by incrementing page number by 1.
            if (this.CanGoToNextPage)
            {
                ShiftPagesByOffset(1);
            }
        }

        /// <summary>
        /// Handler for the FirstPage command.
        /// </summary>
        protected virtual void OnFirstPageCommand()
        {
            // Navigate the master page to the first one and shift
            // all remaining pages by delta.
            ShiftPagesByOffset(1 - this.MasterPageNumber);
        }

        /// <summary>
        /// Handler for the LastPage command.
        /// </summary>
        protected virtual void OnLastPageCommand()
        {
            // Navigate the master page to the last one and shift
            // all remaining pages by delta.
            ShiftPagesByOffset(this.PageCount - this.MasterPageNumber);
        }

        /// <summary>
        /// Handler for the GoToPage command.
        /// </summary>
        /// <param name="pageNumber"></param>
        protected virtual void OnGoToPageCommand(int pageNumber)
        {
            // Check if can go to the specified page.
            // Navigate the master page to specified page number and shift
            // all remaining pages by delta.
            if (CanGoToPage(pageNumber))
            {
                ShiftPagesByOffset(pageNumber - this.MasterPageNumber);
            }
        }

        /// <summary>
        /// Handler for the Print command.
        /// </summary>
        protected virtual void OnPrintCommand()
        {
#if !DONOTREFPRINTINGASMMETA
            System.Windows.Xps.XpsDocumentWriter docWriter;
            System.Printing.PrintDocumentImageableArea ia = null;

            // Only one printing job is allowed.
            if (_documentWriter != null)
            {
                return;
            }

            if (_document != null)
            {
                // Show print dialog.
                docWriter = System.Printing.PrintQueue.CreateXpsDocumentWriter(ref ia);
                if (docWriter != null && ia != null)
                {
                    // Register for WritingCompleted event.
                    _documentWriter = docWriter;
                    _documentWriter.WritingCompleted += new WritingCompletedEventHandler(HandlePrintCompleted);
                    _documentWriter.WritingCancelled += new WritingCancelledEventHandler(HandlePrintCancelled);

                    // Since _documentWriter value is used to determine CanExecute state, we must invalidate that state.
                    CommandManager.InvalidateRequerySuggested();

                    // Write to the PrintQueue
                    if( _document is FixedDocumentSequence )
                    {
                        docWriter.WriteAsync(_document as FixedDocumentSequence);
                    }
                    else if( _document is FixedDocument )
                    {
                        docWriter.WriteAsync(_document as FixedDocument);
                    }
                    else
                    {
                        docWriter.WriteAsync(_document.DocumentPaginator);
                    }
                }
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        /// <summary>
        /// Handler for the CancelPrint command.
        /// </summary>
        protected virtual void OnCancelPrintCommand()
        {
#if !DONOTREFPRINTINGASMMETA
            if (_documentWriter != null)
            {
                _documentWriter.CancelAsync();
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        /// <summary>
        /// Called when the Document property is changed.
        /// </summary>
        protected virtual void OnDocumentChanged()
        {
            int index;

            // Document has been changed. Update existing DocumentPageViews to point them to the new Document.
            for (index = 0; index < _pageViews.Count; index++)
            {
                _pageViews[index].DocumentPaginator = (_document != null) ? _document.DocumentPaginator : null;
            }

            // Document invalidation invalidates following properties:
            //      - PageCount
            //      - MasterPageNumber
            //      - CanGoToPreviousPage
            //      - CanGoToNextPage
            UpdateReadOnlyProperties(true, true);                        

            // Attach TextEditor, if content supports it. This method will also
            // detach TextEditor from old content.
            AttachTextEditor();
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Protected Properties
        //
        //-------------------------------------------------------------------

        #region Protected Properties

        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                if (this.HasLogicalChildren && _document != null)
                {
                    return new SingleChildEnumerator(_document);
                }
                return EmptyEnumerator.Instance;
            }
        }

        #endregion Protected Properties

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Determines whether DocumentPageView is a master page.
        /// </summary>
        /// <param name="pageView">Instance of DocumentPageView.</param>
        /// <returns>Whether given instance of DocumentPageView is a master page.</returns>
        internal bool IsMasterPageView(DocumentPageView pageView)
        {
            Invariant.Assert(pageView != null);
            return (pageView == GetMasterPageView());
        }

        /// <summary>
        /// Invoked when the "Find" button in the Find Toolbar is clicked.
        /// This method invokes the actual Find process.
        /// </summary>
        internal ITextRange Find(FindToolBar findToolBar)
        {
            ITextView masterPageTextView = null;
            DocumentPageView masterPage = GetMasterPageView();
            if (masterPage != null && masterPage is IServiceProvider)
            {
                masterPageTextView = ((IServiceProvider)masterPage).GetService(typeof(ITextView)) as ITextView;
            }
            return DocumentViewerHelper.Find(findToolBar, _textEditor, _textView, masterPageTextView);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Whether text selection is enabled or disabled.
        /// </summary>
        internal bool IsSelectionEnabled
        {
            get { return CheckFlags(Flags.IsSelectionEnabled); }
            set
            {
                SetFlags(value, Flags.IsSelectionEnabled);
                AttachTextEditor();
            }
        }

        /// <summary>
        /// TextEditor instance.
        /// </summary>
        internal TextEditor TextEditor
        {
            get { return _textEditor; }
        }

        /// <summary>
        /// Allows overriding the RenderScope used by the TextEditor.
        /// </summary>
        internal FrameworkElement TextEditorRenderScope
        {
            get
            {
                return _textEditorRenderScope;
            }
            set
            {
                _textEditorRenderScope = value;
                AttachTextEditor();
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Retrieves an ITextPointer from the MasterPage.
        /// If startOfPage is true, then we will retreive the first
        /// TextPointer in the first TextSegment in the DocumentPageTextView.
        /// Else, we will retreive the last TextPointer in the last TextSegment
        /// of the MasterPage DocumentPageTextView.
        /// </summary>
        /// <param name="startOfPage"></param>
        /// <returns></returns>
        private ITextPointer GetMasterPageTextPointer(bool startOfPage)
        {
            ITextPointer masterPointer = null;
            ITextView textView = null;
            DocumentPageView masterPage = GetMasterPageView();

            if (masterPage != null && masterPage is IServiceProvider)
            {
                textView = ((IServiceProvider)masterPage).GetService(typeof(ITextView)) as ITextView;
                if (textView != null && textView.IsValid)
                {
                    // Find the very first/(last) text pointer in this textView.
                    foreach (TextSegment textSegment in textView.TextSegments)
                    {
                        if (textSegment.IsNull)
                        {
                            continue;
                        }

                        if (masterPointer == null)
                        {
                            // Set initial masterPointer value.
                            masterPointer = startOfPage ? textSegment.Start : textSegment.End;
                        }
                        else
                        {
                            if (startOfPage)
                            {
                                if (textSegment.Start.CompareTo(masterPointer) < 0)
                                {
                                    // Start is before the current masterPointer
                                    masterPointer = textSegment.Start;
                                }
                            }
                            else
                            {
                                // end is after than the current masterPointer
                                if (textSegment.End.CompareTo(masterPointer) > 0)
                                {
                                    masterPointer = textSegment.End;
                                }
                            }
                        }
                    }
                }
            }

            return masterPointer;
        }

        /// <summary>
        /// Update collection of DocumentPageViews in responce to Visual tree changes.
        /// </summary>
        /// <returns>Whether collection of DocumentPageViews has been updated.</returns>
        private void UpdatePageViews()
        {
            int index;
            bool changed;
            ReadOnlyCollection<DocumentPageView> pageViews;

            // Get collection of new DocumentPageViews.
            pageViews = GetPageViewsCollection(out changed);

            // If DocumentPageViews collection has not been changed, there is nothing to update.
            if (changed)
            {
                // Verify collection of DocumentPageViews. It needs to meet following conditions:
                // a) at least one DocumentPageView,
                // b) only one page has IsMasterPage property set to true,
                // c) unique PageNumbers for each active DocumentPageView,
                VerifyDocumentPageViews(pageViews);

                // New collection of DocumentPageViews is replacing the old one. Point new
                // DocumentPageViews to the Document.
                _pageViews = pageViews;
                for (index = 0; index < _pageViews.Count; index++)
                {
                    _pageViews[index].DocumentPaginator = (_document != null) ? _document.DocumentPaginator : null;
                }

                // Collection of DocumentPageView has been changed. Need to update
                // TextView, if one already exists.
                if (_textView != null)
                {
                    _textView.OnPagesUpdated();
                }

                // DocumentPageViews collection has been changed. Notify all listeners
                // and/or derived classes about this fact.
                OnPageViewsChanged();
            }
        }

        /// <summary>
        /// Verify collection of DocumentPageViews. It needs to meet following conditions:
        /// a) collection is not null,
        /// b) only one page has IsMasterPage property set to true,
        /// c) unique PageNumbers for each active DocumentPageView,
        /// </summary>
        /// <param name="pageViews">Collection of DocumentPageViews to validate.</param>
        private void VerifyDocumentPageViews(ReadOnlyCollection<DocumentPageView> pageViews)
        {
            int index;
            bool hasMasterPage = false;

            // At least one DocumentPageView is required.
            if (pageViews == null)
            {
                throw new ArgumentException(SR.Get(SRID.DocumentViewerPageViewsCollectionEmpty));
            }

            // Expecting only one DocumentPageView with IsMasterPage property set to true.
            for (index = 0; index < pageViews.Count; index++)
            {
                if (GetIsMasterPage(pageViews[index]))
                {
                    if (hasMasterPage)
                    {
                        throw new ArgumentException(SR.Get(SRID.DocumentViewerOneMasterPage));
                    }
                    hasMasterPage = true;
                }
            }

            // Unique PageNumbers for each active DocumentPageView.
        }

        /// <summary>
        /// Does deep Visual tree walk to retrieve all DocumentPageViews.
        /// It stops recursing down into visual tree in following situations:
        /// a) Visual is UIElement and it is not part of Contol Template,
        /// b) Visual is DocumentPageView.
        /// </summary>
        /// <param name="root">FrameworkElement that is part of Control Template.</param>
        /// <param name="pageViews">Collection of DocumentPageViews; found elements are appended here.</param>
        /// <returns>Whether collection of DocumentPageViews has been updated.</returns>
        private void FindDocumentPageViews(Visual root, List<DocumentPageView> pageViews)
        {
            Invariant.Assert(root != null);
            Invariant.Assert(pageViews != null);

            FrameworkElement fe;
            // Do deep tree walk to retrieve all DocumentPageViews.
            // It stops recursing down into visual tree in following situations:
            // a) Visual is UIElement and it is not part of Contol Template,
            // b) Visual is DocumentPageView.
            // Add to collection any DocumentPageViews found in the Control Template.
            int count = root.InternalVisualChildrenCount;
            for(int i = 0; i < count; i++)
            {
                Visual child = root.InternalGetVisualChild(i);
                fe = child as FrameworkElement;
                if (fe != null)
                {
                    if (fe.TemplatedParent != null)
                    {
                        if (fe is DocumentPageView)
                        {
                            pageViews.Add(fe as DocumentPageView);
                        }
                        else
                        {
                            FindDocumentPageViews(fe, pageViews);
                        }
                    }
                }
                else
                {
                    FindDocumentPageViews(child, pageViews);
                }
            }
        }

        /// <summary>
        /// Does deep Visual tree walk to retrieve an AdornerDecorator. Because
        /// AdornerDecorator is supposed to cover all DocumentPageViews, it stops
        /// recursing down into visual tree in following situations:
        /// a) Visual is UIElement and it is not part of Contol Template,
        /// b) Visual is DocumentPageView.
        /// c) Visual is AdornerDecorator.
        /// </summary>
        /// <param name="root">FrameworkElement that is part of Control Template.</param>
        /// <returns>AdornerDecorator, if found.</returns>
        private AdornerDecorator FindAdornerDecorator(Visual root)
        {
            Invariant.Assert(root != null);

            FrameworkElement fe;
            AdornerDecorator adornerDecorator = null;

            // Do deep Visual tree walk to retrieve an AdornerDecorator. Because
            // AdornerDecorator is supposed to cover all DocumentPageViews, it stops
            // recursing down into visual tree in following situations:
            // a) Visual is UIElement and it is not part of Contol Template,
            // b) Visual is DocumentPageView.
            // c) Visual is AdornerDecorator.
            int count = root.InternalVisualChildrenCount;
            for(int i = 0; i < count; i++)
            {
                Visual child = root.InternalGetVisualChild(i);
                fe = child as FrameworkElement;
                if (fe != null)
                {
                    if (fe.TemplatedParent != null)
                    {
                        if (fe is AdornerDecorator)
                        {
                            adornerDecorator = (AdornerDecorator)fe;
                        }
                        else if (!(fe is DocumentPageView))
                        {
                            adornerDecorator = FindAdornerDecorator(fe);
                        }
                        // else stop on DocumentPageView
                    }
                }
                else
                {
                    adornerDecorator = FindAdornerDecorator(child);
                }
                if (adornerDecorator != null)
                {
                    break;
                }
            }
            return adornerDecorator;
        }

        /// <summary>
        /// Attach TextEditor to Document, if supports text.
        /// </summary>
        private void AttachTextEditor()
        {
            AnnotationService service = AnnotationService.GetService(this); 
            ITextContainer textContainer;

            // This method is called when Document is changing, so need
            // to clear old TextEditor data.
            if (_textEditor != null)
            {
                _textEditor.OnDetach();
                _textEditor = null;
                if (_textView.TextContainer.TextView == _textView)
                {
                    _textView.TextContainer.TextView = null;
                }
                _textView = null;
            }

            if (service != null)
            {
                // Must be enabled - otherwise it won't be on the tree
                service.Disable();
            }

            // If new Document supports TextEditor, create one.
            // If the Document is already attached to TextEditor (TextSelection != null), 
            // do not create TextEditor for this instance of the viewer. (This situation may happen 
            // when the same instance of Document is attached to more than one viewer).
            textContainer = this.TextContainer;
            if (textContainer != null && this.TextEditorRenderScope != null && textContainer.TextSelection == null)
            {
                _textView = new MultiPageTextView(this, this.TextEditorRenderScope, textContainer);
                _textEditor = new TextEditor(textContainer, this, false);
                _textEditor.IsReadOnly = !IsEditingEnabled;
                _textEditor.TextView = _textView;
                textContainer.TextView = _textView;
            }

            // Re-enable the service in order to register on the new TextView
            if (service != null)
            {
                service.Enable(service.Store);
            }
        }

        /// <summary>
        /// Called when WritingCompleted event raised by a DocumentWriter (during printing).
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void HandlePrintCompleted(object sender, WritingCompletedEventArgs e)
        {
            CleanUpPrintOperation();
        }

        /// <summary>
        /// Called when WritingCancelled event raised by a DocumentWriter (during printing).
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void HandlePrintCancelled(object sender, WritingCancelledEventArgs e)
        {
            CleanUpPrintOperation();
        }

        /// <summary>
        /// Handler for PaginationCompleted event raised by the Document.
        /// </summary>
        private void HandlePaginationCompleted(object sender, EventArgs e)
        {
            // PaginationCompleted may invalidate following properties:
            //      - PageCount
            //      - CanGoToNextPage
            UpdateReadOnlyProperties(true, false);
        }

        /// <summary>
        /// Handler for PaginationProgress event raised by the Document.
        /// </summary>
        private void HandlePaginationProgress(object sender, EventArgs e)
        {
            // PaginationProgress may invalidate following properties:
            //      - PageCount
            //      - CanGoToNextPage
            UpdateReadOnlyProperties(true, false);
        }

        /// <summary>
        /// Handler for GetPageNumberCompleted event raised by the Document.
        /// </summary>
        private void HandleGetPageNumberCompleted(object sender, GetPageNumberCompletedEventArgs e)
        {
            BringIntoViewState bringIntoViewState;

            // At this point the Document's page count might have changed,
            // so update properties accordingly.
            UpdateReadOnlyProperties(true, false);

            if (_document != null && sender == _document.DocumentPaginator && e != null)
            {
                if (!e.Cancelled && (e.Error == null))
                {
                    bringIntoViewState = e.UserState as BringIntoViewState;
                    if (bringIntoViewState != null && bringIntoViewState.Source == this)
                    {
                        OnBringIntoView(bringIntoViewState.TargetObject, bringIntoViewState.TargetRect, e.PageNumber + 1);
                    }
                }
            }
        }

        /// <summary>
        /// Makes sure the target is visible in the client area. May cause navigation
        /// to a different page.
        /// </summary>
        /// <param name="args">RequestBringIntoViewEventArgs indicates the element and region to scroll into view.</param>
        private void HandleRequestBringIntoView(RequestBringIntoViewEventArgs args)
        {
            DependencyObject child;
            DependencyObject parent;
            ContentPosition contentPosition;
            BringIntoViewState bringIntoViewState;
            DynamicDocumentPaginator documentPaginator;
            Rect targetRect = Rect.Empty;

            if (args != null && args.TargetObject != null && _document is DependencyObject)
            {
                // If the passed in object is a logical child of DocumentViewer's Document,
                // attempt to make it visible now.
                // Special case: TargetObject is the document itself. Then scroll to the top (page 1).
                // This supports navigating from baseURI#anchor to just baseURI.
                parent = _document as DependencyObject;
                if (args.TargetObject == _document)
                {
                    OnGoToPageCommand(1);
                    args.Handled = true; // Mark the event as handled.
                }
                else
                {
                    // Verify if TargetObject is in fact a child of Document.
                    child = args.TargetObject;
                    while (child != null && child != parent)
                    {
                        // Skip elements in the control's template (if such exists) and 
                        // walk up logical tree to find if the focused element is within
                        // the document.
                        FrameworkElement fe = child as FrameworkElement;
                        if (fe != null && fe.TemplatedParent != null)
                        {
                            child = fe.TemplatedParent;
                        }
                        else
                        {
                            child = LogicalTreeHelper.GetParent(child);
                        }
                    }

                    if (child != null)
                    {
                        // Special case UIElements already connected to visual tree.
                        if (args.TargetObject is UIElement)
                        {
                            UIElement targetObject = (UIElement)args.TargetObject;
                            if (VisualTreeHelper.IsAncestorOf(this, targetObject))
                            {
                                targetRect = args.TargetRect;
                                if (targetRect.IsEmpty)
                                {
                                    targetRect = new Rect(targetObject.RenderSize);
                                }
                                GeneralTransform transform = targetObject.TransformToAncestor(this);
                                targetRect = transform.TransformBounds(targetRect);
                                targetRect.IntersectsWith(new Rect(this.RenderSize));
                            }
                        }

                        // If target is not already visible, bring appropriate page into view.
                        if (targetRect.IsEmpty)
                        {
                            // Get content position for given target.
                            documentPaginator = _document.DocumentPaginator as DynamicDocumentPaginator;
                            if (documentPaginator != null)
                            {
                                contentPosition = documentPaginator.GetObjectPosition(args.TargetObject);
                                if (contentPosition != null && contentPosition != ContentPosition.Missing)
                                {
                                    // Asynchronously retrieve PageNumber for given ContentPosition.
                                    bringIntoViewState = new BringIntoViewState(this, contentPosition, args.TargetObject, args.TargetRect);
                                    documentPaginator.GetPageNumberAsync(contentPosition, bringIntoViewState);
                                }
                            }
                        }
                        args.Handled = true; // Mark the event as handled.
                    }
                }
                if (args.Handled)
                {
                    // Create new BringIntoView request for this element, so 
                    // if there is an ancestor handling BringIntoView, it can
                    // react appropriately and bring this element into view.
                    if (targetRect.IsEmpty)
                    {
                        BringIntoView();
                    }
                    else
                    {
                        BringIntoView(targetRect);
                    }
                }
            }
        }

        /// <summary>
        /// Update values for readonly properties.
        /// </summary>
        /// <param name="pageCountChanged">Whether PageCount has been changed.</param>
        /// <param name="masterPageChanged">Whether MasterPageNumber has been changed.</param>
        private void UpdateReadOnlyProperties(bool pageCountChanged, bool masterPageChanged)
        {
            if (pageCountChanged)
            {
                SetValue(PageCountPropertyKey, (_document != null) ? _document.DocumentPaginator.PageCount : 0);
            }

            bool invalidateRequery = false;

            if (masterPageChanged)
            {
                int masterPageNumber = 0;
                DocumentPageView masterPageView;
                if (_document != null && _pageViews.Count > 0)
                {
                    masterPageView = GetMasterPageView();
                    if (masterPageView != null)
                    {
                        masterPageNumber = masterPageView.PageNumber + 1;
                    }
                }

                SetValue(MasterPageNumberPropertyKey, masterPageNumber);
                SetValue(CanGoToPreviousPagePropertyKey, MasterPageNumber > 1);

                invalidateRequery = true;
            }

            if (pageCountChanged || masterPageChanged)
            {
                bool canGoToNextPage = false;
                if (_document != null)
                {
                    canGoToNextPage = (MasterPageNumber < _document.DocumentPaginator.PageCount) || !_document.DocumentPaginator.IsPageCountValid;
                }
                SetValue(CanGoToNextPagePropertyKey, canGoToNextPage);

                invalidateRequery = true;
            }

            if(invalidateRequery)
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// Shift all pages by specified offset.
        /// </summary>
        private void ShiftPagesByOffset(int offset)
        {
            int index;
            if (offset != 0)
            {
                for (index = 0; index < _pageViews.Count; index++)
                {
                    _pageViews[index].PageNumber += offset;
                }
                // Change of PageNumber property on DocumentPageViews will cause
                // invalidation of content represented by DocumentPageViews.
                OnMasterPageNumberChanged();
            }
        }

        /// <summary>
        /// Sets or unsets one or multiple flags.
        /// </summary>
        /// <param name="value">Whether flag or flags are set or cleared.</param>
        /// <param name="flags">Combination of flags to change.</param>
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        /// <summary>
        /// Returns true if all of passed flags in the bitmask are set.
        /// </summary>
        /// <param name="flags">Combination of flags to get the value.</param>
        /// <returns>Returns true if all of passed flags in the bitmask are set.</returns>
        private bool CheckFlags(Flags flags)
        {
            return ((_flags & flags) == flags);
        }

        /// <summary>
        /// The Document has changed and needs to be updated.
        /// </summary>
        private void DocumentChanged(IDocumentPaginatorSource oldDocument, IDocumentPaginatorSource newDocument)
        {
            DependencyObject doDocument;
            DynamicDocumentPaginator dynamicDocumentPaginator;
            _document = newDocument;

            // Cleanup state associated with the old document.
            if (oldDocument != null)
            {
                // If Document was added to logical tree of DocumentViewer before, remove it.
                if (CheckFlags(Flags.DocumentAsLogicalChild))
                {
                    RemoveLogicalChild(oldDocument);
                }
                // Unregister from PaginationProgress and PaginationCompleted events.
                dynamicDocumentPaginator = oldDocument.DocumentPaginator as DynamicDocumentPaginator;
                if (dynamicDocumentPaginator != null)
                {
                    dynamicDocumentPaginator.PaginationProgress -= new PaginationProgressEventHandler(HandlePaginationProgress);
                    dynamicDocumentPaginator.PaginationCompleted -= new EventHandler(HandlePaginationCompleted);
                    dynamicDocumentPaginator.GetPageNumberCompleted -= new GetPageNumberCompletedEventHandler(HandleGetPageNumberCompleted);
                }

                DependencyObject depObj = oldDocument as DependencyObject;
                if (depObj != null)
                {
                    depObj.ClearValue(PathNode.HiddenParentProperty);
                }
            }

            // If DocumentViewer was created through style, then do not modify
            // the logical tree. Instead, set "core parent" for the Document.
            doDocument = _document as DependencyObject;
	        if (doDocument != null && LogicalTreeHelper.GetParent(doDocument) != null && doDocument is ContentElement)
            {
                // Set the "core parent" back to us.
                ContentOperations.SetParent((ContentElement)doDocument, this);
                SetFlags(false, Flags.DocumentAsLogicalChild);
            }
            else
            {
                SetFlags(true, Flags.DocumentAsLogicalChild);
            }

            // Initialize state associated with the new document.
            if (_document != null)
            {
                // If Document should be part of DocumentViewer's logical tree, add it.
                if (CheckFlags(Flags.DocumentAsLogicalChild))
                {
                    AddLogicalChild(_document);
                }
                // Register for PaginationProgress and PaginationCompleted events.
                dynamicDocumentPaginator = _document.DocumentPaginator as DynamicDocumentPaginator;
                if (dynamicDocumentPaginator != null)
                {
                    dynamicDocumentPaginator.PaginationProgress += new PaginationProgressEventHandler(HandlePaginationProgress);
                    dynamicDocumentPaginator.PaginationCompleted += new EventHandler(HandlePaginationCompleted);
                    dynamicDocumentPaginator.GetPageNumberCompleted += new GetPageNumberCompletedEventHandler(HandleGetPageNumberCompleted);
                }

                // Setup DPs and processors for annotation handling.  If the service isn't already
                // enabled the processors will be registered by the service when it is enabled.
                FlowDocument flowDocument;
                DependencyObject doc = _document as DependencyObject;
                if (_document is FixedDocument || _document is FixedDocumentSequence)
                {
                    // Clear properties that aren't needed for FixedDocument
                    this.ClearValue(AnnotationService.DataIdProperty);
                    // Setup service to look for FixedPages in the content
                    AnnotationService.SetSubTreeProcessorId(this, FixedPageProcessor.Id);
                    // Tell the content how to get to its parent DocumentViewer
                    doc.SetValue(PathNode.HiddenParentProperty, this);
                    // If the service is already registered, set it up for fixed content
                    AnnotationService service = AnnotationService.GetService(this);
                    if (service != null)
                    {
                        service.LocatorManager.RegisterSelectionProcessor(new FixedTextSelectionProcessor(), typeof(TextRange));
                        service.LocatorManager.RegisterSelectionProcessor(new FixedTextSelectionProcessor(), typeof(TextAnchor));
                    }
                }
                else if ((flowDocument = _document as FlowDocument) != null)
                {
                    flowDocument.SetDpi(this.GetDpi());
                    // Tell the content how to get to its parent DocumentViewer
                    flowDocument.SetValue(PathNode.HiddenParentProperty, this);
                    // If the service is already registered, set it up for fixed content
                    AnnotationService service = AnnotationService.GetService(this);
                    if (service != null)
                    {
                        service.LocatorManager.RegisterSelectionProcessor(new TextSelectionProcessor(), typeof(TextRange));
                        service.LocatorManager.RegisterSelectionProcessor(new TextSelectionProcessor(), typeof(TextAnchor));
                        service.LocatorManager.RegisterSelectionProcessor(new TextViewSelectionProcessor(), typeof(DocumentViewerBase));
                    }
                    // Setup service to use DataID processor
                    AnnotationService.SetDataId(this, "FlowDocument");
                }
                else
                {
                    // Clear values that were set directly on the tree - only valid for Fixed or Flow Documents
                    this.ClearValue(AnnotationService.SubTreeProcessorIdProperty);
                    this.ClearValue(AnnotationService.DataIdProperty);
                }
            }

            // Document is also represented as Automation child. Need to invalidate peer to force update.
            DocumentViewerBaseAutomationPeer peer = UIElementAutomationPeer.FromElement(this) as DocumentViewerBaseAutomationPeer;
            if (peer != null)
            {
                peer.InvalidatePeer();
            }

            // Respond to Document change - update state that is affected by this change.
            OnDocumentChanged();
        }

        /// <summary>
        /// Cleans up after a print operation by unregistering for events and re-enabling buttons.
        /// </summary>
        private void CleanUpPrintOperation()
        {
#if !DONOTREFPRINTINGASMMETA
            if (_documentWriter != null)
            {
                _documentWriter.WritingCompleted -= new WritingCompletedEventHandler(HandlePrintCompleted);
                _documentWriter.WritingCancelled -= new WritingCancelledEventHandler(HandlePrintCancelled);
                _documentWriter = null;

                // Since _documentWriter value is used to determine CanExecute state, we must invalidate that state.
                CommandManager.InvalidateRequerySuggested();
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        #region Commands

        /// <summary>
        /// Set up Command and RoutedCommand bindings.
        /// </summary>
        private static void CreateCommandBindings()
        {
            ExecutedRoutedEventHandler executedHandler;
            CanExecuteRoutedEventHandler canExecuteHandler;

            // Create our generic ExecutedRoutedEventHandler.
            executedHandler = new ExecutedRoutedEventHandler(ExecutedRoutedEventHandler);
            // Create our generic QueryEnabledStatusHandler
            canExecuteHandler = new CanExecuteRoutedEventHandler(CanExecuteRoutedEventHandler);

            // Command: NavigationCommands.PreviousPage
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewerBase), NavigationCommands.PreviousPage,
                executedHandler, canExecuteHandler); // no key gesture

            // Command: NavigationCommands.NextPage
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewerBase), NavigationCommands.NextPage,
                executedHandler, canExecuteHandler); // no key gesture

            // Command: NavigationCommands.FirstPage
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewerBase), NavigationCommands.FirstPage,
                executedHandler, canExecuteHandler); // no key gesture

            // Command: NavigationCommands.LastPage
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewerBase), NavigationCommands.LastPage,
                executedHandler, canExecuteHandler); // no key gesture

            // Command: NavigationCommands.GoToPage
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewerBase), NavigationCommands.GoToPage,
                executedHandler, canExecuteHandler); // no key gesture

            // Command: ApplicationCommands.Print
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewerBase), ApplicationCommands.Print,
                executedHandler, canExecuteHandler, new KeyGesture(Key.P, ModifierKeys.Control));

            // Command: ApplicationCommands.CancelPrint
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewerBase), ApplicationCommands.CancelPrint,
                executedHandler, canExecuteHandler); // no key gesture

            // Register editing command handlers - After our commands to let editor handle them first
            TextEditor.RegisterCommandHandlers(typeof(DocumentViewerBase), /*acceptsRichContent:*/true, /*readOnly:*/!IsEditingEnabled, /*registerEventListeners*/true);
        }

        /// <summary>
        /// Central handler for CanExecuteRouted events fired by Commands directed at DocumentViewerBase.
        /// </summary>
        /// <param name="target">The target of this Command, expected to be DocumentViewerBase</param>
        /// <param name="args">The event arguments for this event.</param>
        private static void CanExecuteRoutedEventHandler(object target, CanExecuteRoutedEventArgs args)
        {
            DocumentViewerBase dv = target as DocumentViewerBase;
            Invariant.Assert(dv != null, "Target of CanExecuteRoutedEventHandler must be DocumentViewerBase.");
            Invariant.Assert(args != null, "args cannot be null.");
  
            // DocumentViewerBase is capable of execution of the majority of its commands.
            // Special rules:
            // a) Print command is enabled when Document is attached and printing is not in progress.
            // b) CancelPrint command is enabled only during printing.
            if (args.Command == ApplicationCommands.Print)
            {
                args.CanExecute = (dv.Document != null) && (dv._documentWriter == null);
                args.Handled = true;
            }
            else if (args.Command == ApplicationCommands.CancelPrint)
            {
                args.CanExecute = (dv._documentWriter != null);
            }
            else
            {
                args.CanExecute = true;
            }
        }

        /// <summary>
        /// Central handler for all ExecutedRouted events fired by Commands directed at DocumentViewerBase.
        /// </summary>
        /// <param name="target">The target of this Command, expected to be DocumentViewerBase.</param>
        /// <param name="args">The event arguments associated with this event.</param>
        private static void ExecutedRoutedEventHandler(object target, ExecutedRoutedEventArgs args)
        {
            DocumentViewerBase dv = target as DocumentViewerBase;
            Invariant.Assert(dv != null, "Target of ExecuteEvent must be DocumentViewerBase.");
            Invariant.Assert(args != null, "args cannot be null.");

            // Now we execute the method corresponding to the Command that fired this event;
            // each Command has its own protected virtual method that performs the operation
            // corresponding to the Command.
            if (args.Command == NavigationCommands.PreviousPage)
            {
                dv.OnPreviousPageCommand();
            }
            else if (args.Command == NavigationCommands.NextPage)
            {
                dv.OnNextPageCommand();
            }
            else if (args.Command == NavigationCommands.FirstPage)
            {
                dv.OnFirstPageCommand();
            }
            else if (args.Command == NavigationCommands.LastPage)
            {
                dv.OnLastPageCommand();
            }
            else if (args.Command == NavigationCommands.GoToPage)
            {
                // Ignore GoToPageCommand, if:
                //  a) there is no value for the page number.
                //  b) the value cannot be converted to Int32.
                if (args.Parameter != null)
                {
                    int pageNumber = -1;
                    try
                    {
                        pageNumber = Convert.ToInt32(args.Parameter, System.Globalization.CultureInfo.CurrentCulture);
                    }
#pragma warning disable 56502 // Allow empty catch statements.
                    catch (InvalidCastException) { }
                    catch (OverflowException) { }
                    catch (FormatException) { }
#pragma warning restore 56502

                    if (pageNumber >= 0)
                    {
                        dv.OnGoToPageCommand(pageNumber);
                    }
                }
            }
            else if (args.Command == ApplicationCommands.Print)
            {
                dv.OnPrintCommand();
            }
            else if (args.Command == ApplicationCommands.CancelPrint)
            {
                dv.OnCancelPrintCommand();
            }
            else
            {
                Invariant.Assert(false, "Command not handled in ExecutedRoutedEventHandler.");
            }
        }

        #endregion Commands

        #region Static Methods

        /// <summary>
        /// Called from the event handler to make sure the target is visible in the client
        /// area. May cause navigation to a different page.
        /// </summary>
        /// <param name="sender">The instance handling the event.</param>
        /// <param name="args">RequestBringIntoViewEventArgs indicates the element and region to scroll into view.</param>
        private static void HandleRequestBringIntoView(object sender, RequestBringIntoViewEventArgs args)
        {
            if (sender != null && sender is DocumentViewerBase)
            {
                ((DocumentViewerBase)sender).HandleRequestBringIntoView(args);
            }
        }

        /// <summary>
        /// The Document has changed and needs to be updated.
        /// </summary>
        private static void DocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is DocumentViewerBase);
            ((DocumentViewerBase) d).DocumentChanged((IDocumentPaginatorSource) e.OldValue, (IDocumentPaginatorSource) e.NewValue);

            // Since Document state is used to determine CanExecute state, we must invalidate that state.
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion Static Methods

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// ITextContainer associated with Document.
        /// </summary>
        private ITextContainer TextContainer
        {
            get
            {
                ITextContainer textContainer = null;
                if (_document != null)
                {
                    if (_document is IServiceProvider && CheckFlags(Flags.IsSelectionEnabled))
                    {
                        textContainer = ((IServiceProvider)_document).GetService(typeof(ITextContainer)) as ITextContainer;
                    }
                }
                return textContainer;
            }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private ReadOnlyCollection<DocumentPageView> _pageViews;    // Collection of DocumentPageViews presenting paginated Document.
        private FrameworkElement _textEditorRenderScope;            // RenderScope associated with the TextEditor.
        private MultiPageTextView _textView;                        // ITextView associated with DocumentViewer.
        private TextEditor _textEditor;                             // TextEditor associated with DocumentViewer.
        private IDocumentPaginatorSource _document;                 // IDocumentPaginatorSource representing Document.
        private Flags _flags;                                       // Flags reflecting various aspects of object's state.
#if !DONOTREFPRINTINGASMMETA
        private System.Windows.Xps.XpsDocumentWriter _documentWriter;                  // DocumentWriter used for printing.
#endif // DONOTREFPRINTINGASMMETA

        private static bool IsEditingEnabled = false;               // A flag enabling text editing within a document viewer
                                                                    // accessible only through reflection.

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  Private Types
        //
        //-------------------------------------------------------------------

        #region Private Types

        /// <summary>
        /// Flags reflecting various aspects of viewer's state.
        /// </summary>
        [System.Flags]
        private enum Flags
        {
            // free bit                 = 0x10,
            IsSelectionEnabled          = 0x20,     // Is text selection enabled.
            DocumentAsLogicalChild      = 0x40,     // Is Document part of logical tree.
        }

        /// <summary>
        /// State of BringIntoView operation.
        /// </summary>
        private class BringIntoViewState
        {
            internal BringIntoViewState(DocumentViewerBase source, ContentPosition contentPosition, DependencyObject targetObject, Rect targetRect)
            {
                this.Source = source;
                this.ContentPosition = contentPosition;
                this.TargetObject = targetObject;
                this.TargetRect = targetRect;
            }
            internal DocumentViewerBase Source;
            internal ContentPosition ContentPosition;
            internal DependencyObject TargetObject;
            internal Rect TargetRect;
        }

        #endregion Private Types

        //-------------------------------------------------------------------
        //
        //  IAddChild
        //
        //-------------------------------------------------------------------

        #region IAddChild

        /// <summary>
        /// Called to add the object as a Child.
        /// </summary>
        /// <param name="value">Object to add as a child.</param>
        /// <remarks>DocumentViewerBase only supports a single child of type IDocumentPaginatorSource.</remarks>
        void IAddChild.AddChild(Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            // Check if Content has already been set.
            if (this.Document != null)
            {
                throw new InvalidOperationException(SR.Get(SRID.DocumentViewerCanHaveOnlyOneChild));
            }
            // Only IDocumentPaginatorSource is a valid content.
            IDocumentPaginatorSource document = value as IDocumentPaginatorSource;
            if (document == null)
            {
                throw new ArgumentException(SR.Get(SRID.DocumentViewerChildMustImplementIDocumentPaginatorSource), "value");
            }
            this.Document = document;
        }

        /// <summary>
        /// Called when text appears under the tag in markup
        /// </summary>
        /// <param name="text">Text to add to the Object.</param>
        /// <remarks>DocumentViewer does not support Text children.</remarks>
        void IAddChild.AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        #endregion IAddChild

        //-------------------------------------------------------------------
        //
        //  IServiceProvider
        //
        //-------------------------------------------------------------------

        #region IServiceProvider

        /// <summary>
        /// Returns service objects associated with this control.
        /// </summary>
        /// <param name="serviceType">Specifies the type of service object to get.</param>
        object IServiceProvider.GetService(Type serviceType)
        {
            object service = null;
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            // Following services are available:
            // (1) TextView - wrapper for TextViews exposed by PageViews.
            // (2) TextContainer - the service object is retrieved from Document.
            if (serviceType == typeof(ITextView))
            {
                service = _textView;
            }
            else if (serviceType == typeof(TextContainer) || serviceType == typeof(ITextContainer))
            {
                service = this.TextContainer;
            }
            return service;
        }

        #endregion IServiceProvider
    }
}
#pragma warning enable 1634, 1691


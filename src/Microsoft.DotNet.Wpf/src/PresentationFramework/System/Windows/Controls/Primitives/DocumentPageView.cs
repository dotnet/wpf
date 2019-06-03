// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Provides a view port for a page of content for a DocumentPage.
//

using System.Windows.Automation;            // AutomationPattern
using System.Windows.Automation.Peers;      // AutomationPeer
using System.Windows.Controls;              // StretchDirection
using System.Windows.Controls.Primitives;   // DocumentViewerBase
using System.Windows.Documents;             // DocumentPaginator
using System.Windows.Media;                 // Visual
using System.Windows.Media.Imaging;         // RenderTargetBitmap
using System.Windows.Threading;             // Dispatcher
using MS.Internal;                          // Invariant
using MS.Internal.Documents;                // DocumentPageHost, DocumentPageTextView
using MS.Internal.Automation;               // TextAdaptor
using MS.Internal.KnownBoxes;               // BooleanBoxes


namespace System.Windows.Controls.Primitives
{
    /// <summary> 
    /// Provides a view port for a page of content for a DocumentPage.
    /// </summary>
    public class DocumentPageView : FrameworkElement, IServiceProvider, IDisposable
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary> 
        /// Create an instance of a DocumentPageView.
        /// </summary>
        /// <remarks>
        /// This does basic initialization of the DocumentPageView.  All subclasses
        /// must call the base constructor to perform this initialization.
        /// </remarks>
        public DocumentPageView() : base()
        {
            _pageZoom = 1.0;
        }

        /// <summary>
        /// Static ctor. Initializes property metadata.
        /// </summary>
        static DocumentPageView()
        {
            ClipToBoundsProperty.OverrideMetadata(typeof(DocumentPageView), new PropertyMetadata(BooleanBoxes.TrueBox));
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The Paginator from which this DocumentPageView retrieves pages.
        /// </summary>
        public DocumentPaginator DocumentPaginator 
        {
            get { return _documentPaginator; }
            set
            {
                CheckDisposed();
                if (_documentPaginator != value)
                {
                    // Cleanup all state associated with old Paginator.
                    if (_documentPaginator != null)
                    {
                        _documentPaginator.GetPageCompleted -= new GetPageCompletedEventHandler(HandleGetPageCompleted);
                        _documentPaginator.PagesChanged -= new PagesChangedEventHandler(HandlePagesChanged);
                        DisposeCurrentPage();
                        DisposeAsyncPage();
                    }

                    Invariant.Assert(_documentPage == null);
                    Invariant.Assert(_documentPageAsync == null);
                    _documentPaginator = value;
                    _textView = null;

                    // Register for events on new Paginator and invalidate 
                    // measure to force content update.
                    if (_documentPaginator != null)
                    {
                        _documentPaginator.GetPageCompleted += new GetPageCompletedEventHandler(HandleGetPageCompleted);
                        _documentPaginator.PagesChanged += new PagesChangedEventHandler(HandlePagesChanged);
                    }
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// The DocumentPage for the displayed page.
        /// </summary>
        public DocumentPage DocumentPage
        {
            get { return (_documentPage == null) ? DocumentPage.Missing : _documentPage; }
        }

        /// <summary>
        /// The page number displayed; no content is displayed if this number is negative.
        /// PageNumber is zero-based.
        /// </summary>
        public int PageNumber
        {
            get { return (int) GetValue(PageNumberProperty); }
            set { SetValue(PageNumberProperty, value); }
        }

        /// <summary>
        /// Controls the stretching behavior for the page.
        /// </summary>
        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Specifies the directions in which page may be stretched.
        /// </summary>
        public StretchDirection StretchDirection
        {
            get { return (StretchDirection)GetValue(StretchDirectionProperty); }
            set { SetValue(StretchDirectionProperty, value); }
        }

        #region Public Dynamic Properties

        /// <summary>
        /// <see cref="PageNumber"/>
        /// </summary>
        public static readonly DependencyProperty PageNumberProperty = 
                DependencyProperty.Register(
                        "PageNumber", 
                        typeof(int), 
                        typeof(DocumentPageView),
                        new FrameworkPropertyMetadata(
                                0, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure, 
                                new PropertyChangedCallback(OnPageNumberChanged)));

        /// <summary>
        /// <see cref="Stretch" />
        /// </summary>
        public static readonly DependencyProperty StretchProperty =
                Viewbox.StretchProperty.AddOwner(
                        typeof(DocumentPageView),
                        new FrameworkPropertyMetadata(
                                Stretch.Uniform, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// <see cref="StretchDirection" />
        /// </summary>
        public static readonly DependencyProperty StretchDirectionProperty = 
                Viewbox.StretchDirectionProperty.AddOwner(
                        typeof(DocumentPageView),
                        new FrameworkPropertyMetadata(
                                StretchDirection.DownOnly, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        #endregion Public Dynamic Properties

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Public Events
        //
        //-------------------------------------------------------------------

        #region Public Events

        /// <summary>
        /// Fired after a DocumentPage.Visual is connected.
        /// </summary>
        public event EventHandler PageConnected;

        /// <summary>
        /// Fired after a DocumentPage.Visual is disconnected.
        /// </summary>
        public event EventHandler PageDisconnected;

        #endregion Public Events

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        protected override void OnDpiChanged(DpiScale oldDpiScaleInfo, DpiScale newDpiScaleInfo)
        {
            DisposeCurrentPage();
            DisposeAsyncPage();
        }

        /// <summary>
        /// Content measurement.
        /// </summary>
        /// <param name="availableSize">Available size that parent can give to the child. This is soft constraint.</param>
        /// <returns>The DocumentPageView's desired size.</returns>
        protected override sealed Size MeasureOverride(Size availableSize)
        {
            Size newPageSize, pageZoom;
            Size pageSize;
            Size desiredSize = new Size(); // If no page is available, return (0,0) as size.

            CheckDisposed();

            if (_suspendLayout)
            {
                desiredSize = this.DesiredSize;
            }
            else if (_documentPaginator != null)
            {
                // Reflow content if needed.
                if (ShouldReflowContent())
                {
                    // Reflow is disabled when dealing with infinite size in both directions.
                    // If only one dimention is infinte, calculate value based on PageSize of the
                    // document and Stretching properties.
                    if (!Double.IsInfinity(availableSize.Width) || !Double.IsInfinity(availableSize.Height))
                    {
                        pageSize = _documentPaginator.PageSize;
                        if (Double.IsInfinity(availableSize.Width))
                        {
                            newPageSize = new Size();
                            newPageSize.Height = availableSize.Height / _pageZoom;
                            newPageSize.Width = newPageSize.Height * (pageSize.Width / pageSize.Height); // Keep aspect ratio.
                        }
                        else if (Double.IsInfinity(availableSize.Height))
                        {
                            newPageSize = new Size();
                            newPageSize.Width = availableSize.Width / _pageZoom;
                            newPageSize.Height = newPageSize.Width * (pageSize.Height / pageSize.Width); // Keep aspect ratio.
                        }
                        else
                        {
                            newPageSize = new Size(availableSize.Width / _pageZoom, availableSize.Height / _pageZoom);
                        }
                        if (!DoubleUtil.AreClose(pageSize, newPageSize))
                        {
                            _documentPaginator.PageSize = newPageSize;
                        }
                    }
                }

                // If the main page or pending async page are not available yet, 
                // asynchronously request new page from Paginator.
                if (_documentPage == null && _documentPageAsync == null)
                {
                    if (PageNumber >= 0)
                    {
                        if (_useAsynchronous)
                        {                            
                            _documentPaginator.GetPageAsync(PageNumber, this);
                        }
                        else
                        {
                            _documentPageAsync = _documentPaginator.GetPage(PageNumber);
                            if (_documentPageAsync == null)
                            {
                                _documentPageAsync = DocumentPage.Missing;
                            }
                        }
                    }
                    else
                    {
                        _documentPage = DocumentPage.Missing;
                    }
                }

                // If pending async page is available, discard the main page and
                // set _documentPage to _documentPageAsync. 
                if (_documentPageAsync != null)
                {
                    // Do cleanup for currently used page, because it gets replaced.
                    DisposeCurrentPage();
                    // DisposeCurrentPage raises PageDisposed and DocumentPage.PageDestroyed events.
                    // Handlers for those events may dispose _documentPageAsync. Treat this situation
                    // as missing page.
                    if (_documentPageAsync == null)
                    {
                        _documentPageAsync = DocumentPage.Missing;
                    }
                    if (_pageVisualClone != null)
                    {
                        RemoveDuplicateVisual();
                    }

                    // Replace the main page with cached async page.
                    _documentPage = _documentPageAsync;
                    if (_documentPage != DocumentPage.Missing)
                    {
                        _documentPage.PageDestroyed += new EventHandler(HandlePageDestroyed);
                        _documentPageAsync.PageDestroyed -= new EventHandler(HandleAsyncPageDestroyed);
                    }
                    _documentPageAsync = null;

                    // Set a flag that will indicate that a PageConnected must be fired in 
                    // ArrangeOverride
                    _newPageConnected = true;
                }

                // If page is available, return its size as desired size.
                if (_documentPage != null && _documentPage != DocumentPage.Missing)
                {
                    pageSize = new Size(_documentPage.Size.Width * _pageZoom, _documentPage.Size.Height * _pageZoom);
                    pageZoom = Viewbox.ComputeScaleFactor(availableSize, pageSize, this.Stretch, this.StretchDirection);
                    desiredSize = new Size(pageSize.Width * pageZoom.Width, pageSize.Height * pageZoom.Height);
                }

                if (_pageVisualClone != null)
                {
                    desiredSize = _visualCloneSize;
                }
            }

            return desiredSize;
        }
        
        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="finalSize">The final size that element should use to arrange itself and its children.</param>
        protected override sealed Size ArrangeOverride(Size finalSize)
        {
            Transform pageTransform;
            ScaleTransform pageScaleTransform;
            Visual pageVisual;
            Size pageSize, pageZoom;
            
            CheckDisposed();

            if (_pageVisualClone == null)
            {
                if (_pageHost == null)
                {
                    _pageHost = new DocumentPageHost();
                    this.AddVisualChild(_pageHost);
                }
                Invariant.Assert(_pageHost != null);

                pageVisual = (_documentPage == null) ? null : _documentPage.Visual;
                if (pageVisual == null)
                {
                    // Remove existing visiual children.
                    _pageHost.PageVisual = null;

                    // Reset offset and transform on the page host before Arrange
                    _pageHost.CachedOffset = new Point();
                    _pageHost.RenderTransform = null;

                    // Size for the page host needs to be set to finalSize
                    _pageHost.Arrange(new Rect(_pageHost.CachedOffset, finalSize));
                }
                else
                {
                    // Add visual representing the page contents. For performance reasons
                    // first check if it is already insered there.
                    if (_pageHost.PageVisual != pageVisual)
                    {
                        // There might be a case where a visual associated with a page was 
                        // inserted to a visual tree before. It got removed later, but GC did not
                        // destroy its parent yet. To workaround this case always check for the parent
                        // of page visual and disconnect it, when necessary.
                        DocumentPageHost.DisconnectPageVisual(pageVisual);

                        _pageHost.PageVisual = pageVisual;
                    }
                
                    // Compute transform to be applied to the page visual. First take into account
                    // mirroring transform, if necessary. Apply also scaling transform.
                    pageSize = _documentPage.Size;
                    pageTransform = Transform.Identity;

                    // DocumentPage.Visual is always LeftToRight, so if the current
                    // FlowDirection is RightToLeft, need to unmirror the child visual.
                    if (FlowDirection == FlowDirection.RightToLeft)
                    {
                        pageTransform = new MatrixTransform(-1.0, 0.0, 0.0, 1.0, pageSize.Width, 0.0);
                    }

                    // Apply zooming
                    if (!DoubleUtil.IsOne(_pageZoom))
                    {
                        pageScaleTransform = new ScaleTransform(_pageZoom, _pageZoom);
                        if (pageTransform == Transform.Identity)
                        {
                            pageTransform = pageScaleTransform;
                        }
                        else
                        {
                            pageTransform = new MatrixTransform(pageTransform.Value * pageScaleTransform.Value);
                        }
                        pageSize = new Size(pageSize.Width * _pageZoom, pageSize.Height * _pageZoom);
                    }

                    // Apply stretch properties
                    pageZoom = Viewbox.ComputeScaleFactor(finalSize, pageSize, this.Stretch, this.StretchDirection);
                    if (!DoubleUtil.IsOne(pageZoom.Width) || !DoubleUtil.IsOne(pageZoom.Height))
                    {
                        pageScaleTransform = new ScaleTransform(pageZoom.Width, pageZoom.Height);
                        if (pageTransform == Transform.Identity)
                        {
                            pageTransform = pageScaleTransform;
                        }
                        else
                        {
                            pageTransform = new MatrixTransform(pageTransform.Value * pageScaleTransform.Value);
                        }
                        pageSize = new Size(pageSize.Width * pageZoom.Width, pageSize.Height * pageZoom.Height);
                    }

                    // Set offset and transform on the page host before Arrange
                    _pageHost.CachedOffset = new Point((finalSize.Width - pageSize.Width) / 2, (finalSize.Height - pageSize.Height) / 2);
                    _pageHost.RenderTransform = pageTransform;

                    // Arrange pagehost to original size of the page.
                    _pageHost.Arrange(new Rect(_pageHost.CachedOffset, _documentPage.Size));
                }

                // Fire sync notification if new page was connected.
                if (_newPageConnected)
                {
                    OnPageConnected();
                }
                
                // Transform for the page has been changed, need to notify TextView about the changes.
                OnTransformChangedAsync();
            }
            else
            {
                if (_pageHost.PageVisual != _pageVisualClone)
                {
                    // Remove existing visiual children.
                    _pageHost.PageVisual = _pageVisualClone;
                    // Size for the page host needs to be set to finalSize

                    // Use previous offset and transform
                    _pageHost.Arrange(new Rect(_pageHost.CachedOffset, finalSize));
                }
            }

            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// Derived class must implement to support Visual children. The method must return
        /// the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (index != 0 || _pageHost == null)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
            return _pageHost;
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        protected void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                // Cleanup all state associated with Paginator.
                if (_documentPaginator != null)
                {
                    _documentPaginator.GetPageCompleted -= new GetPageCompletedEventHandler(HandleGetPageCompleted);
                    _documentPaginator.PagesChanged -= new PagesChangedEventHandler(HandlePagesChanged);
                    _documentPaginator.CancelAsync(this);
                    DisposeCurrentPage();
                    DisposeAsyncPage();                                        
                }
                Invariant.Assert(_documentPage == null);
                Invariant.Assert(_documentPageAsync == null);
                _documentPaginator = null;
                _textView = null;
            }
        }

        /// <summary>
        /// Returns service objects associated with this control.
        /// This method should be called by IServiceProvider.GetService implementation 
        /// for DocumentPageView or subclasses.
        /// </summary>
        /// <param name="serviceType">Specifies the type of service object to get.</param>
        protected object GetService(Type serviceType)
        {
            object service = null;
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }
            CheckDisposed();

            // No service is available if the Content does not provide
            // any services.
            if (_documentPaginator != null && _documentPaginator is IServiceProvider)
            {
                // Following services are available:
                // (1) TextView - wrapper for TextView exposed by the current page.
                // (2) TextContainer - the service object is retrieved from DocumentPaginator.
                if (serviceType == typeof(ITextView))
                {
                    if (_textView == null)
                    {
                        ITextContainer tc = ((IServiceProvider)_documentPaginator).GetService(typeof(ITextContainer)) as ITextContainer;
                        if (tc != null)
                        {
                            _textView = new DocumentPageTextView(this, tc);
                        }
                    }
                    service = _textView;
                }
                else if (serviceType == typeof(TextContainer) || serviceType == typeof(ITextContainer))
                {
                    service = ((IServiceProvider)_documentPaginator).GetService(serviceType);
                }
            }
            return service;
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DocumentPageViewAutomationPeer(this);
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Protected Properties
        //
        //-------------------------------------------------------------------

        #region Protected Properties

        /// <summary>
        /// Whether this DocumentPageView has been disposed.
        /// </summary>
        protected bool IsDisposed
        {
            get { return _disposed; }
        }

        /// <summary>
        /// Derived classes override this property to enable the Visual code to enumerate 
        /// the Visual children. Derived classes need to return the number of children
        /// from this method.
        /// </summary>        
        protected override int VisualChildrenCount
        {
            get { return _pageHost != null ? 1 : 0; }
        }

        #endregion Protected Properties

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Sets the zoom applied to the page being displayed.
        /// </summary>
        /// <param name="pageZoom">Page zooom value.</param>
        internal void SetPageZoom(double pageZoom)
        {
            Invariant.Assert(!DoubleUtil.LessThanOrClose(pageZoom, 0d) && !Double.IsInfinity(pageZoom));
            Invariant.Assert(!_disposed);

            if (!DoubleUtil.AreClose(_pageZoom, pageZoom))
            {
                _pageZoom = pageZoom;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Suspends page layout.
        /// </summary>
        internal void SuspendLayout()
        {
            _suspendLayout = true;
            _pageVisualClone = DuplicatePageVisual();
            _visualCloneSize = this.DesiredSize;
        }

        /// <summary>
        /// Resumes page layout.
        /// </summary>
        internal void ResumeLayout()
        {
            _suspendLayout = false;
            _pageVisualClone = null;
            InvalidateMeasure();
        }

        /// <summary>
        /// Duplicates the current page visual, if possible
        /// </summary>
        internal void DuplicateVisual()
        {
            if (_documentPage != null && _pageVisualClone == null)
            {
                _pageVisualClone = DuplicatePageVisual();
                _visualCloneSize = this.DesiredSize;
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Clears the duplicated page visual, if one exists.
        /// </summary>
        internal void RemoveDuplicateVisual()
        {
            if (_pageVisualClone != null)
            {
                _pageVisualClone = null;
                InvalidateArrange();
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        ///    Default is true.  Controls whether we use asynchronous mode to 
        ///    request the DocumentPage.  In some cases, such as synchronous
        ///    printing, we don't want to wait for the asynchronous events.
        /// </summary>
        internal bool UseAsynchronousGetPage
        {
            get { return _useAsynchronous; }
            set { _useAsynchronous = value; }
        }

        /// <summary>
        /// The DocumentPage for the displayed page.
        /// </summary>
        internal DocumentPage DocumentPageInternal
        {
            get { return _documentPage; }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Handles PageDestroyed event raised for the current DocumentPage.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Not used.</param>
        private void HandlePageDestroyed(object sender, EventArgs e)
        {
            if (!_disposed)
            {
                InvalidateMeasure();
                DisposeCurrentPage();
            }
        }

        /// <summary>
        /// Handles PageDestroyed event raised for the cached async DocumentPage.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Not used.</param>
        private void HandleAsyncPageDestroyed(object sender, EventArgs e)
        {
            if (!_disposed)
            {
                DisposeAsyncPage();
            }
        }

        /// <summary>
        /// Handles GetPageCompleted event raised by the DocumentPaginator.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Details about this event.</param>
        private void HandleGetPageCompleted(object sender, GetPageCompletedEventArgs e)
        {
            if (!_disposed && (e != null) && !e.Cancelled && e.Error == null)
            {
                if (e.PageNumber == this.PageNumber && e.UserState == this)
                {
                    if (_documentPageAsync != null && _documentPageAsync != DocumentPage.Missing)
                    {
                        _documentPageAsync.PageDestroyed -= new EventHandler(HandleAsyncPageDestroyed);
                    }
                    _documentPageAsync = e.DocumentPage;
                    if (_documentPageAsync == null)
                    {
                        _documentPageAsync = DocumentPage.Missing;
                    }
                    if (_documentPageAsync != DocumentPage.Missing)
                    {
                        _documentPageAsync.PageDestroyed += new EventHandler(HandleAsyncPageDestroyed);                            
                    }
                    InvalidateMeasure();                        
                }
                // else; the page is not ours
            }
        }

        /// <summary>
        /// Handles PagesChanged event raised by the DocumentPaginator.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Details about this event.</param>
        private void HandlePagesChanged(object sender, PagesChangedEventArgs e)
        {
            if (!_disposed && (e != null))
            {
                if (this.PageNumber >= e.Start && 
                    (e.Count == int.MaxValue || this.PageNumber <= e.Start + e.Count))
                {
                    OnPageContentChanged();
                }
            }
        }

        /// <summary>
        /// Async notification about transform changes for embedded page.
        /// </summary>
        private void OnTransformChangedAsync()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new DispatcherOperationCallback(OnTransformChanged), null);
        }

        /// <summary>
        /// Notification about transform changes for embedded page.
        /// </summary>
        /// <param name="arg">Not used.</param>
        /// <returns>Not used.</returns>
        private object OnTransformChanged(object arg)
        {
            if (_textView != null && _documentPage != null)
            {
                _textView.OnTransformChanged();
            }
            return null;
        }

        /// <summary>
        /// Raises PageConnected event.
        /// </summary>
        private void OnPageConnected()
        {
            _newPageConnected = false;
            if (_textView != null)
            {
                _textView.OnPageConnected();
            }
            if (this.PageConnected != null && _documentPage != null)
            {
                this.PageConnected(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raises PageDisconnected event.
        /// </summary>
        private void OnPageDisconnected()
        {
            if (_textView != null)
            {
                _textView.OnPageDisconnected();
            }
            if (this.PageDisconnected != null)
            {
                this.PageDisconnected(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Responds to page content change.
        /// </summary>
        private void OnPageContentChanged()
        {
            // Force remeasure which will cause to reget DocumentPage
            InvalidateMeasure();
            // Do cleanup for currently used page, because it gets replaced.
            DisposeCurrentPage();
            DisposeAsyncPage();
        }

        /// <summary>
        /// Disposes the current DocumentPage.
        /// </summary>
        private void DisposeCurrentPage()
        {
            // Do cleanup for currently used page, because it gets replaced.
            if (_documentPage != null)
            {
                // Remove visual for currently used page.
                if (_pageHost != null)
                {
                    _pageHost.PageVisual = null;
                }

                // Clear TextView & DocumentPage
                if (_documentPage != DocumentPage.Missing)
                {
                    _documentPage.PageDestroyed -= new EventHandler(HandlePageDestroyed);                    
                }
                if (_documentPage is IDisposable)
                {
                    ((IDisposable)_documentPage).Dispose();
                }
                _documentPage = null;               

                OnPageDisconnected();
            }          
        }

        /// <summary>
        /// Disposes pending async DocumentPage.
        /// </summary>
        private void DisposeAsyncPage()
        {
            // Do cleanup for cached async page.
            if (_documentPageAsync != null)
            {
                if (_documentPageAsync != DocumentPage.Missing)
                {
                    _documentPageAsync.PageDestroyed -= new EventHandler(HandleAsyncPageDestroyed);                    
                }
                if (_documentPageAsync is IDisposable)
                {
                    ((IDisposable)_documentPageAsync).Dispose();
                }                
                _documentPageAsync = null;
            }
        }

        /// <summary>
        /// Checks if the instance is already disposed. 
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(DocumentPageView).ToString());
            }
        }

        /// <summary>
        /// Check whether content needs to be reflowed.
        /// </summary>
        /// <returns>True, if content needs to be reflowed.</returns>
        private bool ShouldReflowContent()
        {
            bool shouldReflow = false;
            DocumentViewerBase hostViewer;

            if (DocumentViewerBase.GetIsMasterPage(this))
            {
                hostViewer = GetHostViewer();
                if (hostViewer != null)
                {
                    shouldReflow = hostViewer.IsMasterPageView(this);
                }
            }
            return shouldReflow;
        }

        /// <summary>
        /// Retrieves DocumentViewerBase that hosts this view.
        /// </summary>
        /// <returns>DocumentViewerBase that hosts this view.</returns>
        private DocumentViewerBase GetHostViewer()
        {
            DocumentViewerBase hostViewer = null;
            Visual visualParent;

            // First do quick check for TemplatedParent. It will cover good
            // amount of cases, because static viewers will have their 
            // DocumentPageViews defined in the style.
            // If quick check does not work, do a visual tree walk.
            if (this.TemplatedParent is DocumentViewerBase)
            {
                hostViewer = (DocumentViewerBase)this.TemplatedParent;
            }
            else
            {
                // Check if hosted by DocumentViewerBase.
                visualParent = VisualTreeHelper.GetParent(this) as Visual;
                while (visualParent != null)
                {
                    if (visualParent is DocumentViewerBase)
                    {
                        hostViewer = (DocumentViewerBase)visualParent;
                        break;
                    }
                    visualParent = VisualTreeHelper.GetParent(visualParent) as Visual;
                }
            }
            return hostViewer;
        }


        /// <summary>
        /// The PageNumber has changed and needs to be updated.
        /// </summary>
        private static void OnPageNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is DocumentPageView);
            ((DocumentPageView)d).OnPageContentChanged();
        }

        /// <summary>
        /// Duplicates content of the PageVisual.
        /// </summary>
        private DrawingVisual DuplicatePageVisual()
        {
            DrawingVisual drawingVisual = null;
            if (_pageHost != null && _pageHost.PageVisual != null && _documentPage.Size != Size.Empty)
            {
                const double maxWidth = 4096.0;
                const double maxHeight = maxWidth;

                Rect pageVisualRect = new Rect(_documentPage.Size);

                pageVisualRect.Width = Math.Min(pageVisualRect.Width, maxWidth);
                pageVisualRect.Height = Math.Min(pageVisualRect.Height, maxHeight);

                drawingVisual = new DrawingVisual();

                try
                {
                    if(pageVisualRect.Width > 1.0 && pageVisualRect.Height > 1.0)
                    {
                        RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)pageVisualRect.Width, (int)pageVisualRect.Height, 96.0, 96.0, PixelFormats.Pbgra32);
                        renderTargetBitmap.Render(_pageHost.PageVisual);

                        ImageBrush imageBrush = new ImageBrush(renderTargetBitmap);
                        drawingVisual.Opacity = 0.50;
                        using (DrawingContext dc = drawingVisual.RenderOpen())
                        {
                            dc.DrawRectangle(imageBrush, null, pageVisualRect);
                        }
                    }
                }
                catch(System.OverflowException)
                {
                    // Ignore overflow exception - caused by render target creation not possible under current memory conditions.
                }
            }
            return drawingVisual;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private DocumentPaginator _documentPaginator;
        private double _pageZoom;
        private DocumentPage _documentPage;
        private DocumentPage _documentPageAsync;
        private DocumentPageTextView _textView;
        private DocumentPageHost _pageHost;
        private Visual _pageVisualClone;
        private Size _visualCloneSize;
        private bool _useAsynchronous = true;
        private bool _suspendLayout;
        private bool _disposed;
        private bool _newPageConnected;        

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  IServiceProvider Members
        //
        //-------------------------------------------------------------------

        #region IServiceProvider Members

        /// <summary>
        /// Returns service objects associated with this control.
        /// </summary>
        /// <param name="serviceType">Specifies the type of service object to get.</param>
        object IServiceProvider.GetService(Type serviceType)
        {
            return this.GetService(serviceType);
        }

        #endregion IServiceProvider Members

        //-------------------------------------------------------------------
        //
        //  IDisposable Members
        //
        //-------------------------------------------------------------------

        #region IDisposable Members

        /// <summary>
        /// Dispose the object.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.Dispose();
        }

        #endregion IDisposable Members
    }
}




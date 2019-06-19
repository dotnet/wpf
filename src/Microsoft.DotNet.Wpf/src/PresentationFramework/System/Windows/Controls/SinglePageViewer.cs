// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: FlowDocumentPageViewer provides a simple user experience for viewing
//              one page of content at a time. The experience is optimized for
//              FlowDocument scenarios, however any IDocumentPaginatorSource
//              can be viewed using this control.
//

using System.Collections.Generic;           // Stack<T>
using System.Collections.ObjectModel;       // ReadOnlyCollection<T>
using System.Windows.Automation.Peers;      // AutomationPeer
using System.Windows.Documents;             // IDocumentPaginatorSouce, ...
using System.Windows.Documents.Serialization;  // WritingCompletedEventArgs
using System.Windows.Input;                 // UICommand
using System.Windows.Media;                 // VisualTreeHelper
using System.Windows.Controls.Primitives;   // DocumentViewerBase, DocumentPageView
using System.Windows.Threading;
using MS.Internal;                          // Invariant, DoubleUtil
using MS.Internal.Commands;                 // CommandHelpers
using MS.Internal.Documents;                // FindToolBar
using MS.Internal.KnownBoxes;               // BooleanBoxes
using MS.Internal.PresentationFramework;    // SecurityHelper
using MS.Internal.AppModel;                 // IJournalState
using System.Security;                      // SecurityCritical, SecurityTreatAsSafe

namespace System.Windows.Controls
{
    /// <summary>
    /// FlowDocumentPageViewer provides a simple user experience for viewing one page
    /// of content at a time. The experience is optimized for FlowDocument scenarios,
    /// however any IDocumentPaginatorSource can be viewed using this control.
    /// </summary>
    [TemplatePart(Name = "PART_FindToolBarHost", Type = typeof(Decorator))]
    public class FlowDocumentPageViewer : DocumentViewerBase, IJournalState
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
        static FlowDocumentPageViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FlowDocumentPageViewer),
                new FrameworkPropertyMetadata(new ComponentResourceKey(typeof(PresentationUIStyleResources), "PUIFlowDocumentPageViewer")));

            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(FlowDocumentPageViewer));

            TextBoxBase.SelectionBrushProperty.OverrideMetadata(typeof(FlowDocumentPageViewer),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(UpdateCaretElement)));
            TextBoxBase.SelectionOpacityProperty.OverrideMetadata(typeof(FlowDocumentPageViewer),
                new FrameworkPropertyMetadata(TextBoxBase.AdornerSelectionOpacityDefaultValue, new PropertyChangedCallback(UpdateCaretElement)));

            // Create our CommandBindings
            CreateCommandBindings();

            EventManager.RegisterClassHandler(typeof(FlowDocumentPageViewer), Keyboard.KeyDownEvent, new KeyEventHandler(KeyDownHandler), true);
        }

        /// <summary>
        /// Instantiates a new instance of a FlowDocumentPageViewer.
        /// </summary>
        public FlowDocumentPageViewer()
            : base()
        {
            this.LayoutUpdated += new EventHandler(HandleLayoutUpdated);
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

            // Initialize FindTooBar host.
            // If old FindToolBar is enabled, disable it first to ensure appropriate cleanup.
            if (FindToolBar != null)
            {
                ToggleFindToolBar(false);
            }
            _findToolBarHost = GetTemplateChild(_findToolBarHostTemplateName) as Decorator;
        }

        /// <summary>
        /// Increases the current zoom.
        /// </summary>
        public void IncreaseZoom()
        {
            OnIncreaseZoomCommand();
        }

        /// <summary>
        /// Decreases the current zoom.
        /// </summary>
        public void DecreaseZoom()
        {
            OnDecreaseZoomCommand();
        }

        /// <summary>
        /// Invokes the Find Toolbar. This is analogous to the ApplicationCommands.Find.
        /// </summary>
        public void Find()
        {
            OnFindCommand();
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Text Selection (readonly)
        /// </summary>
        public TextSelection Selection
        {
            get
            {
                ITextSelection textSelection = null;
                FlowDocument flowDocument = Document as FlowDocument;
                if (flowDocument != null)
                {
                    textSelection = flowDocument.StructuralCache.TextContainer.TextSelection;
                }
                return textSelection as TextSelection;
            }
        }

        /// <summary>
        /// The Zoom applied to all pages; this value is 100-based.
        /// </summary>
        public double Zoom
        {
            get { return (double) GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }

        /// <summary>
        /// The maximum allowed value of the Zoom property.
        /// </summary>
        public double MaxZoom
        {
            get { return (double) GetValue(MaxZoomProperty); }
            set { SetValue(MaxZoomProperty, value); }
        }

        /// <summary>
        /// The minimum allowed value of the Zoom property.
        /// </summary>
        public double MinZoom
        {
            get { return (double) GetValue(MinZoomProperty); }
            set { SetValue(MinZoomProperty, value); }
        }

        /// <summary>
        /// The amount the Zoom property is incremented or decremented when
        /// the IncreaseZoom or DecreaseZoom command is executed.
        /// </summary>
        public double ZoomIncrement
        {
            get { return (double) GetValue(ZoomIncrementProperty); }
            set { SetValue(ZoomIncrementProperty, value); }
        }

        /// <summary>
        /// Whether the viewer can increase the current zoom.
        /// </summary>
        public virtual bool CanIncreaseZoom
        {
            get { return (bool) GetValue(CanIncreaseZoomProperty); }
        }

        /// <summary>
        /// Whether the viewer can decrease the current zoom.
        /// </summary>
        public virtual bool CanDecreaseZoom
        {
            get { return (bool) GetValue(CanDecreaseZoomProperty); }
        }

        /// <summary>
        /// <see cref="TextBoxBase.SelectionBrushProperty" />
        /// </summary>
        public Brush SelectionBrush
        {
            get { return (Brush)GetValue(SelectionBrushProperty); }
            set { SetValue(SelectionBrushProperty, value); }
        }

        /// <summary>
        /// <see cref="TextBoxBase.SelectionOpacityProperty"/>
        /// </summary>
        public double SelectionOpacity
        {
            get { return (double)GetValue(SelectionOpacityProperty); }
            set { SetValue(SelectionOpacityProperty, value); }
        }

        /// <summary>
        /// <see cref="TextBoxBase.IsSelectionActive"/>
        /// </summary>
        public bool IsSelectionActive
        {
            get { return (bool)GetValue(IsSelectionActiveProperty); }
        }

        /// <summary>
        /// <see cref="TextBoxBase.IsInactiveSelectionHighlightEnabled"/>
        /// </summary>
        public bool IsInactiveSelectionHighlightEnabled
        {
            get { return (bool)GetValue(IsInactiveSelectionHighlightEnabledProperty); }
            set { SetValue(IsInactiveSelectionHighlightEnabledProperty, value); }
        }

        #region Public Dynamic Properties

        /// <summary>
        /// <see cref="Zoom"/>
        /// </summary>
        public static readonly DependencyProperty ZoomProperty =
                DependencyProperty.Register(
                        "Zoom",
                        typeof(double),
                        typeof(FlowDocumentPageViewer),
                        new FrameworkPropertyMetadata(
                                100d,
                                new PropertyChangedCallback(ZoomChanged),
                                new CoerceValueCallback(CoerceZoom)),
                        new ValidateValueCallback(ZoomValidateValue));

        /// <summary>
        /// <see cref="MaxZoom"/>
        /// </summary>
        public static readonly DependencyProperty MaxZoomProperty =
                DependencyProperty.Register(
                        "MaxZoom",
                        typeof(double),
                        typeof(FlowDocumentPageViewer),
                        new FrameworkPropertyMetadata(
                                200d,
                                new PropertyChangedCallback(MaxZoomChanged),
                                new CoerceValueCallback(CoerceMaxZoom)),
                        new ValidateValueCallback(ZoomValidateValue));

        /// <summary>
        /// <see cref="MinZoom"/>
        /// </summary>
        public static readonly DependencyProperty MinZoomProperty =
                DependencyProperty.Register(
                        "MinZoom",
                        typeof(double),
                        typeof(FlowDocumentPageViewer),
                        new FrameworkPropertyMetadata(
                                80d,
                                new PropertyChangedCallback(MinZoomChanged)),
                        new ValidateValueCallback(ZoomValidateValue));

        /// <summary>
        /// <see cref="ZoomIncrement"/>
        /// </summary>
        public static readonly DependencyProperty ZoomIncrementProperty =
                DependencyProperty.Register(
                        "ZoomIncrement",
                        typeof(double),
                        typeof(FlowDocumentPageViewer),
                        new FrameworkPropertyMetadata(10d),
                        new ValidateValueCallback(ZoomValidateValue));

        /// <summary>
        /// <see cref="CanIncreaseZoom"/>
        /// </summary>
        protected static readonly DependencyPropertyKey CanIncreaseZoomPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanIncreaseZoom",
                        typeof(bool),
                        typeof(FlowDocumentPageViewer),
                        new FrameworkPropertyMetadata(true));

        /// <summary>
        /// <see cref="CanIncreaseZoom"/>
        /// </summary>
        public static readonly DependencyProperty CanIncreaseZoomProperty = CanIncreaseZoomPropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="CanDecreaseZoom"/>
        /// </summary>
        protected static readonly DependencyPropertyKey CanDecreaseZoomPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanDecreaseZoom",
                        typeof(bool),
                        typeof(FlowDocumentPageViewer),
                        new FrameworkPropertyMetadata(true));

        /// <summary>
        /// <see cref="CanDecreaseZoom"/>
        /// </summary>
        public static readonly DependencyProperty CanDecreaseZoomProperty =
                CanDecreaseZoomPropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="TextBoxBase.SelectionBrushProperty"/>
        /// </summary>
        public static readonly DependencyProperty SelectionBrushProperty =
            TextBoxBase.SelectionBrushProperty.AddOwner(typeof(FlowDocumentPageViewer));

        /// <summary>
        /// <see cref="TextBoxBase.SelectionOpacityProperty"/>
        /// </summary>
        public static readonly DependencyProperty SelectionOpacityProperty =
            TextBoxBase.SelectionOpacityProperty.AddOwner(typeof(FlowDocumentPageViewer));


        /// <summary>
        /// <see cref="TextBoxBase.IsSelectionActiveProperty"/>
        /// </summary>
        public static readonly DependencyProperty IsSelectionActiveProperty =
            TextBoxBase.IsSelectionActiveProperty.AddOwner(typeof(FlowDocumentPageViewer));

        /// <summary>
        /// <see cref="TextBoxBase.IsInactiveSelectionHighlightEnabledProperty"/>
        /// </summary>
        public static readonly DependencyProperty IsInactiveSelectionHighlightEnabledProperty =
            TextBoxBase.IsInactiveSelectionHighlightEnabledProperty.AddOwner(typeof(FlowDocumentPageViewer));

        #endregion Public Dynamic Properties

        #endregion Public Properties

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
            return new FlowDocumentPageViewerAutomationPeer(this);
        }

        /// <summary>
        /// This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Esc -- Close FindToolBar
            if (e.Key == Key.Escape)
            {
                if (FindToolBar != null)
                {
                    ToggleFindToolBar(false);
                    e.Handled = true;
                }
            }

            // F3 -- Invoke Find
            if (e.Key == Key.F3)
            {
                if (CanShowFindToolBar)
                {
                    if (FindToolBar != null)
                    {
                        // If the Shift key is also pressed, then search up.
                        FindToolBar.SearchUp = ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift);
                        OnFindInvoked(this, EventArgs.Empty);
                    }
                    else
                    {
                        // Make the FindToolbar visible
                        ToggleFindToolBar(true);
                    }
                    e.Handled = true;
                }
            }

            // If not handled, do default handling.
            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        /// <summary>
        /// An event reporting a mouse wheel rotation.
        /// </summary>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            // [Mouse Wheel + Control] - zooming
            // [Mouse Wheel] - page navigation
            if (e.Delta != 0)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (e.Delta > 0)
                    {
                        IncreaseZoom();
                    }
                    else
                    {
                        DecreaseZoom();
                    }
                }
                else
                {
                    if (e.Delta > 0)
                    {
                        PreviousPage();
                    }
                    else
                    {
                        NextPage();
                    }
                }
                e.Handled = false;
            }

            // If not handled, do default handling.
            if (!e.Handled)
            {
                base.OnMouseWheel(e);
            }
        }

        /// <summary>
        /// Called when ContextMenuOpening is raised on this element.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            base.OnContextMenuOpening(e);
            DocumentViewerHelper.OnContextMenuOpening(Document as FlowDocument, this, e);
        }

        /// <summary>
        /// Called when the PageViews collection is modified; this occurs when GetPageViewsCollection
        /// returns True or if the control's template is modified.
        /// </summary>
        protected override void OnPageViewsChanged()
        {
            // Change of DocumentPageView collection resets current ContentPosition.
            // This position is updated when layout process is done.
            _contentPosition = null;

            // Apply zoom value to new DocumentPageViews
            ApplyZoom();

            base.OnPageViewsChanged();
        }

        /// <summary>
        /// Called when the Document property is changed.
        /// </summary>
        protected override void OnDocumentChanged()
        {
            // Document change resets current ContentPosition.
            _contentPosition = null;

            // _oldDocument is cached from previous document changed notification.
            if (_oldDocument != null)
            {
                DynamicDocumentPaginator dynamicDocumentPaginator = _oldDocument.DocumentPaginator as DynamicDocumentPaginator;
                if (dynamicDocumentPaginator != null)
                {
                    dynamicDocumentPaginator.GetPageNumberCompleted -= new GetPageNumberCompletedEventHandler(HandleGetPageNumberCompleted);
                }

                FlowDocumentPaginator flowDocumentPaginator = _oldDocument.DocumentPaginator as FlowDocumentPaginator;
                if (flowDocumentPaginator != null)
                {
                    flowDocumentPaginator.BreakRecordTableInvalidated -= new BreakRecordTableInvalidatedEventHandler(HandleAllBreakRecordsInvalidated);
                }
            }

            base.OnDocumentChanged();
            _oldDocument = Document;

            // Validate the new document type
            if (Document != null && !(Document is FlowDocument))
            {
                // Undo new document assignment.
                Document = null;
                // Throw exception.
                throw new NotSupportedException(SR.Get(SRID.FlowDocumentPageViewerOnlySupportsFlowDocument));
            }

            if(Document != null)
            {
                DynamicDocumentPaginator dynamicDocumentPaginator = Document.DocumentPaginator as DynamicDocumentPaginator;
                if (dynamicDocumentPaginator != null)
                {
                    dynamicDocumentPaginator.GetPageNumberCompleted += new GetPageNumberCompletedEventHandler(HandleGetPageNumberCompleted);
                }

                FlowDocumentPaginator flowDocumentPaginator = Document.DocumentPaginator as FlowDocumentPaginator;
                if (flowDocumentPaginator != null)
                {
                    flowDocumentPaginator.BreakRecordTableInvalidated += new BreakRecordTableInvalidatedEventHandler(HandleAllBreakRecordsInvalidated);
                }
            }

            // Update the toolbar with our current document state.
            if (!CanShowFindToolBar)
            {
                // Disable FindToolBar, if the content does not support it.
                if (FindToolBar != null)
                {
                    ToggleFindToolBar(false);
                }
            }

            // Go to the first page of new content.
            OnGoToPageCommand(1);
        }

        /// <summary>
        /// Called when print has been completed.
        /// </summary>
        protected virtual void OnPrintCompleted()
        {
            ClearPrintingState();
        }

        /// <summary>
        /// Handler for the PreviousPage command.
        /// </summary>
        protected override void OnPreviousPageCommand()
        {
            if (this.CanGoToPreviousPage)
            {
                // Page navigation resets current ContentPosition.
                // This position is updated when layout process is done.
                _contentPosition = null;

                base.OnPreviousPageCommand();
            }
        }

        /// <summary>
        /// Handler for the NextPage command.
        /// </summary>
        protected override void OnNextPageCommand()
        {
            if (this.CanGoToNextPage)
            {
                // Page navigation resets current ContentPosition.
                // This position is updated when layout process is done.
                _contentPosition = null;

                base.OnNextPageCommand();
            }
        }

        /// <summary>
        /// Handler for the FirstPage command.
        /// </summary>
        protected override void OnFirstPageCommand()
        {
            if (this.CanGoToPreviousPage)
            {
                // Page navigation resets current ContentPosition.
                // This position is updated when layout process is done.
                _contentPosition = null;

                base.OnFirstPageCommand();
            }
        }

        /// <summary>
        /// Handler for the LastPage command.
        /// </summary>
        protected override void OnLastPageCommand()
        {
            if (this.CanGoToNextPage)
            {
                // Page navigation resets current ContentPosition.
                // This position is updated when layout process is done.
                _contentPosition = null;

                base.OnLastPageCommand();
            }
        }

        /// <summary>
        /// Handler for the GoToPage command.
        /// </summary>
        protected override void OnGoToPageCommand(int pageNumber)
        {
            if (CanGoToPage(pageNumber) && this.MasterPageNumber != pageNumber)
            {
                // Page navigation resets current ContentPosition.
                // This position is updated when layout process is done.
                _contentPosition = null;

                base.OnGoToPageCommand(pageNumber);
            }
        }

        /// <summary>
        /// Handler for the Find command.
        /// </summary>
        protected virtual void OnFindCommand()
        {
            if (CanShowFindToolBar)
            {
                // Toggle on the FindToolBar between visible and hidden state.
                ToggleFindToolBar(FindToolBar == null);
            }
        }

        /// <summary>
        /// Handler for the Print command.
        /// </summary>
        protected override void OnPrintCommand()
        {
#if !DONOTREFPRINTINGASMMETA
            System.Windows.Xps.XpsDocumentWriter docWriter;
            System.Printing.PrintDocumentImageableArea ia = null;
            FlowDocumentPaginator paginator;
            FlowDocument document = Document as FlowDocument;
            Thickness pagePadding;

            // Only one printing job is allowed.
            if (_printingState != null)
            {
                return;
            }

            // If the document is FlowDocument, do custom printing. Otherwise go through default path.
            if (document != null)
            {
                // Show print dialog.
                docWriter = System.Printing.PrintQueue.CreateXpsDocumentWriter(ref ia);
                if (docWriter != null && ia != null)
                {
                    // Store the current state of the document in the PrintingState
                    paginator = ((IDocumentPaginatorSource)document).DocumentPaginator as FlowDocumentPaginator;
                    _printingState = new FlowDocumentPrintingState();
                    _printingState.XpsDocumentWriter = docWriter;
                    _printingState.PageSize = paginator.PageSize;
                    _printingState.PagePadding = document.PagePadding;
                    _printingState.IsSelectionEnabled = IsSelectionEnabled;

                    // Since _printingState value is used to determine CanExecute state, we must invalidate that state.
                    CommandManager.InvalidateRequerySuggested();

                    // Register for XpsDocumentWriter events.
                    docWriter.WritingCompleted += new WritingCompletedEventHandler(HandlePrintCompleted);
                    docWriter.WritingCancelled += new WritingCancelledEventHandler(HandlePrintCancelled);

                    // Add PreviewCanExecute handler to have a chance to disable UI Commands during printing.
                    CommandManager.AddPreviewCanExecuteHandler(this, new CanExecuteRoutedEventHandler(PreviewCanExecuteRoutedEventHandler));

                    // Suspend layout on DocumentPageViews.
                    ReadOnlyCollection<DocumentPageView> pageViews = PageViews;
                    for (int index = 0; index < pageViews.Count; index++)
                    {
                        pageViews[index].SuspendLayout();
                    }

                    // Disable TextSelection, if currently enabled.
                    if (IsSelectionEnabled)
                    {
                        IsSelectionEnabled = false;
                    }

                    // Change the PageSize and PagePadding for the document to match the CanvasSize for the printer device.
                    paginator.PageSize = new Size(ia.MediaSizeWidth, ia.MediaSizeHeight);
                    pagePadding = document.ComputePageMargin();
                    document.PagePadding = new Thickness(
                        Math.Max(ia.OriginWidth, pagePadding.Left),
                        Math.Max(ia.OriginHeight, pagePadding.Top),
                        Math.Max(ia.MediaSizeWidth - (ia.OriginWidth + ia.ExtentWidth), pagePadding.Right),
                        Math.Max(ia.MediaSizeHeight - (ia.OriginHeight + ia.ExtentHeight), pagePadding.Bottom));

                    // Send DocumentPaginator to the printer.
                    docWriter.WriteAsync(paginator);
                }
            }
            else
            {
                base.OnPrintCommand();
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        /// <summary>
        /// Handler for the CancelPrint command.
        /// </summary>
        protected override void OnCancelPrintCommand()
        {
#if !DONOTREFPRINTINGASMMETA
            if (_printingState != null)
            {
                _printingState.XpsDocumentWriter.CancelAsync();
            }
            else
            {
                base.OnCancelPrintCommand();
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        /// <summary>
        /// Handler for the IncreaseZoom command.
        /// </summary>
        protected virtual void OnIncreaseZoomCommand()
        {
            // If can zoom in, increase zoom by the zoom increment value.
            if (this.CanIncreaseZoom)
            {
                SetCurrentValueInternal(ZoomProperty, Math.Min(Zoom + ZoomIncrement, MaxZoom));
            }
        }

        /// <summary>
        /// Handler for the DecreaseZoom command.
        /// </summary>
        protected virtual void OnDecreaseZoomCommand()
        {
            // If can zoom out, decrease zoom by the zoom increment value.
            if (this.CanDecreaseZoom)
            {
                SetCurrentValueInternal(ZoomProperty, Math.Max(Zoom - ZoomIncrement, MinZoom));
            }
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Allows FrameworkElement to augment the EventRoute.
        /// </summary>
        internal override bool BuildRouteCore(EventRoute route, RoutedEventArgs args)
        {
            // If FlowDocumentPageViewer is used as embedded viewer (in FlowDocumentReader),
            // it is not part of logical tree, so default logic in FE to re-add logical
            // tree from branched node does not work here.
            // But before FlowDocumentPageViewer is added to the event route, logical
            // ancestors up to Document need to be added to the route. Otherwise
            // content will not have a chance to react to events first.
            // This breaks navigation cursor management logic, because TextEditor attached
            // to FlowDocumentPageViewer handles those events first.
            DependencyObject document = this.Document as DependencyObject;
            if (document != null && LogicalTreeHelper.GetParent(document) != this)
            {
                DependencyObject branchNode = route.PeekBranchNode() as DependencyObject;
                if (branchNode != null && DocumentViewerHelper.IsLogicalDescendent(branchNode, document))
                {
                    // Add intermediate ContentElements to the route.
                    FrameworkElement.AddIntermediateElementsToRoute(
                        LogicalTreeHelper.GetParent(document), route, args, LogicalTreeHelper.GetParent(branchNode));
                }
            }
            return base.BuildRouteCore(route, args);
        }

        internal override bool InvalidateAutomationAncestorsCore(Stack<DependencyObject> branchNodeStack, out bool continuePastCoreTree)
        {
            bool continueInvalidation = true;

            DependencyObject document = this.Document as DependencyObject;
            if (document != null && LogicalTreeHelper.GetParent(document) != this)
            {
                DependencyObject branchNode = (branchNodeStack.Count > 0) ? branchNodeStack.Peek() : null;
                if (branchNode != null && DocumentViewerHelper.IsLogicalDescendent(branchNode, document))
                {
                    continueInvalidation = FrameworkElement.InvalidateAutomationIntermediateElements(LogicalTreeHelper.GetParent(document), LogicalTreeHelper.GetParent(branchNode));
                }
            }

            continueInvalidation &= base.InvalidateAutomationAncestorsCore(branchNodeStack, out continuePastCoreTree);

            return continueInvalidation;
        }

        /// <summary>
        /// Brings specified point into view.
        /// </summary>
        /// <param name="point">Point in the viewer's coordinate system.</param>
        /// <returns>Whether operation is pending or not.</returns>
        /// <remarks>
        /// It is guaranteed that Point is not over any existing pages.
        /// </remarks>
        internal bool BringPointIntoView(Point point)
        {
            int index;
            Rect[] pageRects;
            Rect pageRect;
            ReadOnlyCollection<DocumentPageView> pageViews = this.PageViews;
            bool bringIntoViewPending = false;

            if (pageViews.Count > 0)
            {
                // Calculate rectangles for all pages.
                pageRects = new Rect[pageViews.Count];
                for (index = 0; index < pageViews.Count; index++)
                {
                    pageRect = new Rect(pageViews[index].RenderSize);
                    pageRect = pageViews[index].TransformToAncestor(this).TransformBounds(pageRect);
                    pageRects[index] = pageRect;
                }

                // Try to find exact hit
                for (index = 0; index < pageRects.Length; index++)
                {
                    if (pageRects[index].Contains(point))
                    {
                        break;
                    }
                }

                if (index >= pageRects.Length)
                {
                    // Union all rects.
                    pageRect = pageRects[0];
                    for (index = 1; index < pageRects.Length; index++)
                    {
                        pageRect.Union(pageRects[index]);
                    }
                    //
                    if (DoubleUtil.LessThan(point.X, pageRect.Left))
                    {
                        if (this.CanGoToPreviousPage)
                        {
                            OnPreviousPageCommand();
                            bringIntoViewPending = true;
                        }
                    }
                    else if (DoubleUtil.GreaterThan(point.X, pageRect.Right))
                    {
                        if (this.CanGoToNextPage)
                        {
                            OnNextPageCommand();
                            bringIntoViewPending = true;
                        }
                    }
                    else if (DoubleUtil.LessThan(point.Y, pageRect.Top))
                    {
                        if (this.CanGoToPreviousPage)
                        {
                            OnPreviousPageCommand();
                            bringIntoViewPending = true;
                        }
                    }
                    else if (DoubleUtil.GreaterThan(point.Y, pageRect.Bottom))
                    {
                        if (this.CanGoToNextPage)
                        {
                            OnNextPageCommand();
                            bringIntoViewPending = true;
                        }
                    }
                }
            }

            return bringIntoViewPending;
        }

        /// <summary>
        /// Bring specified content position into view.
        /// </summary>
        internal object BringContentPositionIntoView(object arg)
        {
            PrivateBringContentPositionIntoView(arg, false);

            return null;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// ContentPosition representing currently viewed content.
        /// </summary>
        internal ContentPosition ContentPosition
        {
            get { return _contentPosition; }
        }

        /// <summary>
        /// Whether FindToolBar can be enabled.
        /// </summary>
        internal bool CanShowFindToolBar
        {
            get { return (_findToolBarHost != null && this.Document != null && this.TextEditor != null); }
        }

        /// <summary>
        /// Whether currently printing the content.
        /// </summary>
        internal bool IsPrinting
        {
            get { return (_printingState != null); }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Handler for LayoutUpdated event raised by the LayoutSystem.
        /// </summary>
        private void HandleLayoutUpdated(object sender, EventArgs e)
        {
            DocumentPageView masterPageView;
            DynamicDocumentPaginator documentPaginator;

            // Ignore LayoutUpdated during printing.
            if (this.Document != null && _printingState == null)
            {
                documentPaginator = this.Document.DocumentPaginator as DynamicDocumentPaginator;
                if (documentPaginator != null)
                {
                    // Update ContentPosition, if not cached.
                    if (_contentPosition == null)
                    {
                        masterPageView = GetMasterPageView();
                        if (masterPageView != null && masterPageView.DocumentPage != null)
                        {
                            _contentPosition = documentPaginator.GetPagePosition(masterPageView.DocumentPage);
                        }
                        if (_contentPosition == ContentPosition.Missing)
                        {
                            _contentPosition = null;
                        }
                    }
                    // Otherwise bring this position into view.
                    else
                    {
                        PrivateBringContentPositionIntoView(_contentPosition, true);
                    }
                }
            }
        }

        /// <summary>
        /// Handler for GetPageNumberCompleted event raised by the Document.
        /// </summary>
        private void HandleGetPageNumberCompleted(object sender, GetPageNumberCompletedEventArgs e)
        {
            if (Document != null && sender == Document.DocumentPaginator && e != null)
            {
                if (!e.Cancelled && e.Error == null && e.UserState == _bringContentPositionIntoViewToken)
                {
                    int newMasterPageNumber = e.PageNumber + 1;

                    OnGoToPageCommand(newMasterPageNumber);
                }
            }
        }

        /// <summary>
        /// Handler for BreakRecordTableInvalidated event raised by the Paginator.
        /// </summary>
        private void HandleAllBreakRecordsInvalidated(object sender, EventArgs e)
        {
            ReadOnlyCollection<DocumentPageView> pageViews = PageViews;
            for (int index = 0; index < pageViews.Count; index++)
            {
                pageViews[index].DuplicateVisual();
            }
        }

        /// <summary>
        /// Handle the case when the position is no longer valid for the current document, conservatively returns true if
        /// is uknown.
        /// </summary>
        private bool IsValidContentPositionForDocument(IDocumentPaginatorSource document, ContentPosition contentPosition)
        {
            FlowDocument flowDocument = document as FlowDocument;
            TextPointer textPointer = contentPosition as TextPointer;

            if(flowDocument != null && textPointer != null)
            {
                return flowDocument.ContentStart.TextContainer == textPointer.TextContainer;
            }

            return true;
        }

        /// <summary>
        /// Bring specified content position into view. (If async, cancel any previous async request to bring into view.)
        /// </summary>
        private void PrivateBringContentPositionIntoView(object arg, bool isAsyncRequest)
        {
            ContentPosition contentPosition = arg as ContentPosition;
            if (contentPosition != null && Document != null)
            {
                DynamicDocumentPaginator documentPaginator = this.Document.DocumentPaginator as DynamicDocumentPaginator;
                if (documentPaginator != null && IsValidContentPositionForDocument(Document, contentPosition))
                {
                    documentPaginator.CancelAsync(_bringContentPositionIntoViewToken);

                    if(isAsyncRequest)
                    {
                        documentPaginator.GetPageNumberAsync(contentPosition, _bringContentPositionIntoViewToken);
                    }
                    else
                    {
                        int newMasterPageNumber = documentPaginator.GetPageNumber(contentPosition) + 1;

                        OnGoToPageCommand(newMasterPageNumber);
                    }

                    _contentPosition = contentPosition;
                }
            }
        }

        /// <summary>
        /// Called when WritingCompleted event raised by a DocumentWriter (during printing).
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void HandlePrintCompleted(object sender, WritingCompletedEventArgs e)
        {
            OnPrintCompleted();
        }

        /// <summary>
        /// Called when WritingCancelled event raised by a DocumentWriter (during printing).
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void HandlePrintCancelled(object sender, WritingCancelledEventArgs e)
        {
            ClearPrintingState();
        }

        /// <summary>
        /// Clears printing state.
        /// </summary>
        private void ClearPrintingState()
        {
#if !DONOTREFPRINTINGASMMETA
            if (_printingState != null)
            {
                // Resume layout on DocumentPageViews.
                ReadOnlyCollection<DocumentPageView> pageViews = PageViews;
                for (int index = 0; index < pageViews.Count; index++)
                {
                    pageViews[index].ResumeLayout();
                }

                // Enable TextSelection, if it was previously enabled.
                if (_printingState.IsSelectionEnabled)
                {
                    IsSelectionEnabled = true;
                }

                // Remove PreviewCanExecute handler (added when Print command was executed).
                CommandManager.RemovePreviewCanExecuteHandler(this, new CanExecuteRoutedEventHandler(PreviewCanExecuteRoutedEventHandler));

                // Unregister for XpsDocumentWriter events.
                _printingState.XpsDocumentWriter.WritingCompleted -= new WritingCompletedEventHandler(HandlePrintCompleted);
                _printingState.XpsDocumentWriter.WritingCancelled -= new WritingCancelledEventHandler(HandlePrintCancelled);

                // Restore old page metrics on FlowDocument.
                ((FlowDocument)Document).PagePadding = _printingState.PagePadding;
                Document.DocumentPaginator.PageSize = _printingState.PageSize;

                _printingState = null;
                // Since _documentWriter value is used to determine CanExecute state, we must invalidate that state.
                CommandManager.InvalidateRequerySuggested();
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        /// <summary>
        /// Apply zoom value to all existing DocumentPageViews.
        /// </summary>
        private void ApplyZoom()
        {
            int index;
            ReadOnlyCollection<DocumentPageView> pageViews;

            // Apply zoom value to all DocumentPageViews.
            pageViews = this.PageViews;
            for (index = 0; index < pageViews.Count; index++)
            {
                pageViews[index].SetPageZoom(Zoom / 100.0);
            }
        }

        /// <summary>
        /// Enables/disables the FindToolbar.
        /// </summary>
        /// <param name="enable">Whether to enable/disable FindToolBar.</param>
        private void ToggleFindToolBar(bool enable)
        {
            Invariant.Assert(enable == (FindToolBar == null));
            DocumentViewerHelper.ToggleFindToolBar(_findToolBarHost, new EventHandler(OnFindInvoked), enable);
        }

        /// <summary>
        /// Invoked when the "Find" button in the Find Toolbar is clicked.
        /// This method invokes the actual Find process.
        /// </summary>
        /// <param name="sender">The object that sent this event</param>
        /// <param name="e">The Click Events associated with this event</param>
        private void OnFindInvoked(object sender, EventArgs e)
        {
            ITextRange findResult;
            int newMasterPageNumber;
            FindToolBar findToolBar = FindToolBar;

            if (findToolBar != null && this.TextEditor != null)
            {
                // In order to show current text selection TextEditor requires Focus to be set on the UIScope.
                // If there embedded controls, it may happen that embedded control currently has focus and find
                // was invoked through hotkeys. To support this case we manually move focus to the appropriate element.
                Focus();

                findResult = Find(findToolBar);

                // If we found something, bring it into the view. Otherwise alert the user.
                if ((findResult != null) && (!findResult.IsEmpty))
                {
                    if (findResult.Start is ContentPosition)
                    {
                        // Bring find result into view. Set also _contentPosition to the beginning of the
                        // result, so it is visible during resizing.
                        _contentPosition = (ContentPosition)findResult.Start;
                        newMasterPageNumber = ((DynamicDocumentPaginator)this.Document.DocumentPaginator).GetPageNumber(_contentPosition) + 1;
                        OnBringIntoView(this, Rect.Empty, newMasterPageNumber);
                    }
                }
                else
                {
                    DocumentViewerHelper.ShowFindUnsuccessfulMessage(findToolBar);
                }
            }
        }

        /// <summary>
        /// The Zoom has changed and needs to be updated.
        /// </summary>
        private void ZoomChanged(double oldValue, double newValue)
        {
            if (!DoubleUtil.AreClose(oldValue, newValue))
            {
                UpdateCanIncreaseZoom();
                UpdateCanDecreaseZoom();

                // Changes to Zoom may potentially cause content reflowing.
                // In such case Paginator will notify DocumentViewerBase
                // about content changes through PagesChanged event and viewer
                // will invalidate appropriate properties.
                // Hence there is no need to invalidate any properties on
                // DocumentViewerBase right now.
                ApplyZoom();
            }
        }

        /// <summary>
        /// Update CanIncreaseZoom property.
        /// </summary>
        private void UpdateCanIncreaseZoom()
        {
            SetValue(CanIncreaseZoomPropertyKey, BooleanBoxes.Box(DoubleUtil.GreaterThan(MaxZoom, Zoom)));
        }

        /// <summary>
        /// Update CanDecreaseZoom property.
        /// </summary>
        private void UpdateCanDecreaseZoom()
        {
            SetValue(CanDecreaseZoomPropertyKey, BooleanBoxes.Box(DoubleUtil.LessThan(MinZoom, Zoom)));
        }

        /// <summary>
        /// The MaxZoom has changed and needs to be updated.
        /// </summary>
        private void MaxZoomChanged(double oldValue, double newValue)
        {
            CoerceValue(ZoomProperty);
            UpdateCanIncreaseZoom();
        }

        /// <summary>
        /// The MinZoom has changed and needs to be updated.
        /// </summary>
        private void MinZoomChanged(double oldValue, double newValue)
        {
            CoerceValue(MaxZoomProperty);
            CoerceValue(ZoomProperty);
            UpdateCanIncreaseZoom();
            UpdateCanDecreaseZoom();
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
            // Create our generic CanExecuteRoutedEventHandler
            canExecuteHandler = new CanExecuteRoutedEventHandler(CanExecuteRoutedEventHandler);

            // Command: ApplicationCommands.Find
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentPageViewer), ApplicationCommands.Find,
                executedHandler, canExecuteHandler);

            // Command: NavigationCommands.IncreaseZoom
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentPageViewer), NavigationCommands.IncreaseZoom,
                executedHandler, canExecuteHandler, new KeyGesture(Key.OemPlus, ModifierKeys.Control));

            // Command: NavigationCommands.DecreaseZoom
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentPageViewer), NavigationCommands.DecreaseZoom,
                executedHandler, canExecuteHandler, new KeyGesture(Key.OemMinus, ModifierKeys.Control));

            // Register input bindings
            CommandManager.RegisterClassInputBinding(typeof(FlowDocumentPageViewer), new InputBinding(NavigationCommands.PreviousPage, new KeyGesture(Key.Left)));
            CommandManager.RegisterClassInputBinding(typeof(FlowDocumentPageViewer), new InputBinding(NavigationCommands.PreviousPage, new KeyGesture(Key.Up)));
            CommandManager.RegisterClassInputBinding(typeof(FlowDocumentPageViewer), new InputBinding(NavigationCommands.PreviousPage, new KeyGesture(Key.PageUp)));
            CommandManager.RegisterClassInputBinding(typeof(FlowDocumentPageViewer), new InputBinding(NavigationCommands.NextPage, new KeyGesture(Key.Right)));
            CommandManager.RegisterClassInputBinding(typeof(FlowDocumentPageViewer), new InputBinding(NavigationCommands.NextPage, new KeyGesture(Key.Down)));
            CommandManager.RegisterClassInputBinding(typeof(FlowDocumentPageViewer), new InputBinding(NavigationCommands.NextPage, new KeyGesture(Key.PageDown)));
            CommandManager.RegisterClassInputBinding(typeof(FlowDocumentPageViewer), new InputBinding(NavigationCommands.FirstPage, new KeyGesture(Key.Home)));
            CommandManager.RegisterClassInputBinding(typeof(FlowDocumentPageViewer), new InputBinding(NavigationCommands.FirstPage, new KeyGesture(Key.Home, ModifierKeys.Control)));
            CommandManager.RegisterClassInputBinding(typeof(FlowDocumentPageViewer), new InputBinding(NavigationCommands.LastPage, new KeyGesture(Key.End)));
            CommandManager.RegisterClassInputBinding(typeof(FlowDocumentPageViewer), new InputBinding(NavigationCommands.LastPage, new KeyGesture(Key.End, ModifierKeys.Control)));
        }

        /// <summary>
        /// Central handler for CanExecuteRouted events fired by Commands directed at FlowDocumentPageViewer.
        /// </summary>
        /// <param name="target">The target of this Command, expected to be FlowDocumentPageViewer</param>
        /// <param name="args">The event arguments for this event.</param>
        private static void CanExecuteRoutedEventHandler(object target, CanExecuteRoutedEventArgs args)
        {
            FlowDocumentPageViewer fdpv = target as FlowDocumentPageViewer;
            Invariant.Assert(fdpv != null, "Target of QueryEnabledEvent must be FlowDocumentPageViewer.");
            Invariant.Assert(args != null, "args cannot be null.");

            // DocumentViewerBase is capable of execution of the majority of its commands.
            // Special rules:
            // a) Find command is enabled when Document is attached and printing is not in progress.
            if (args.Command == ApplicationCommands.Find)
            {
                args.CanExecute = fdpv.CanShowFindToolBar;
            }
            else
            {
                args.CanExecute = true;
            }
        }

        /// <summary>
        /// Central handler for all ExecutedRouted events fired by Commands directed at FlowDocumentPageViewer.
        /// </summary>
        /// <param name="target">The target of this Command, expected to be FlowDocumentPageViewer.</param>
        /// <param name="args">The event arguments associated with this event.</param>
        private static void ExecutedRoutedEventHandler(object target, ExecutedRoutedEventArgs args)
        {
            FlowDocumentPageViewer fdpv = target as FlowDocumentPageViewer;
            Invariant.Assert(fdpv != null, "Target of ExecuteEvent must be FlowDocumentPageViewer.");
            Invariant.Assert(args != null, "args cannot be null.");

            // Now we execute the method corresponding to the Command that fired this event;
            // each Command has its own protected virtual method that performs the operation
            // corresponding to the Command.
            if (args.Command == NavigationCommands.IncreaseZoom)
            {
                fdpv.OnIncreaseZoomCommand();
            }
            else if (args.Command == NavigationCommands.DecreaseZoom)
            {
                fdpv.OnDecreaseZoomCommand();
            }
            else if (args.Command == ApplicationCommands.Find)
            {
                fdpv.OnFindCommand();
            }
            else
            {
                Invariant.Assert(false, "Command not handled in ExecutedRoutedEventHandler.");
            }
        }

        /// <summary>
        /// Disable commands on DocumentViewerBase when this printing is in progress.
        /// </summary>
        private void PreviewCanExecuteRoutedEventHandler(object target, CanExecuteRoutedEventArgs args)
        {
            FlowDocumentPageViewer fdpv = target as FlowDocumentPageViewer;
            Invariant.Assert(fdpv != null, "Target of PreviewCanExecuteRoutedEventHandler must be FlowDocumentPageViewer.");
            Invariant.Assert(args != null, "args cannot be null.");

            // Disable UI commands, if printing is in progress.
            if (fdpv._printingState != null)
            {
                if (args.Command != ApplicationCommands.CancelPrint)
                {
                    args.CanExecute = false;
                    args.Handled = true;
                }
            }
        }

        /// <summary>
        /// Called when a key event occurs.
        /// </summary>
        private static void KeyDownHandler(object sender, KeyEventArgs e)
        {
            DocumentViewerHelper.KeyDownHelper(e, ((FlowDocumentPageViewer)sender)._findToolBarHost);
        }

        #endregion Commands

        #region Static Methods

        /// <summary>
        /// Coerce the value for Zoom property.
        /// </summary>
        private static object CoerceZoom(DependencyObject d, object value)
        {
            Invariant.Assert(d != null && d is FlowDocumentPageViewer);

            double maxZoom, minZoom;
            FlowDocumentPageViewer v = (FlowDocumentPageViewer) d;
            double zoom = (double)value;

            maxZoom = v.MaxZoom;
            if (DoubleUtil.LessThan(maxZoom, zoom))
            {
                return maxZoom;
            }

            minZoom = v.MinZoom;
            if (DoubleUtil.GreaterThan(minZoom, zoom))
            {
                return minZoom;
            }

            return value;
        }

        /// <summary>
        /// Coerce the value for MaxZoom property.
        /// </summary>
        private static object CoerceMaxZoom(DependencyObject d, object value)
        {
            Invariant.Assert(d != null && d is FlowDocumentPageViewer);

            FlowDocumentPageViewer v = (FlowDocumentPageViewer) d;
            double minZoom = v.MinZoom;
            if (DoubleUtil.LessThan((double)value, minZoom))
            {
                return minZoom;
            }
            return value;
        }

        /// <summary>
        /// The Zoom has changed and needs to be updated.
        /// </summary>
        private static void ZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentPageViewer);
            ((FlowDocumentPageViewer)d).ZoomChanged((double) e.OldValue, (double) e.NewValue);
        }

        /// <summary>
        /// The MaxZoom has changed and needs to be updated.
        /// </summary>
        private static void MaxZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentPageViewer);
            ((FlowDocumentPageViewer)d).MaxZoomChanged((double) e.OldValue, (double) e.NewValue);
        }

        /// <summary>
        /// The MinZoom has changed and needs to be updated.
        /// </summary>
        private static void MinZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentPageViewer);
            ((FlowDocumentPageViewer)d).MinZoomChanged((double) e.OldValue, (double) e.NewValue);
        }

        /// <summary>
        /// Validate Zoom, MaxZoom, MinZoom and ZoomIncrement value.
        /// </summary>
        /// <param name="o">Value to validate.</param>
        /// <returns>True if the value is valid, false otherwise.</returns>
        private static bool ZoomValidateValue(object o)
        {
            double value = (double)o;
            return (!Double.IsNaN(value) && !Double.IsInfinity(value) && DoubleUtil.GreaterThan(value, 0d));
        }

        /// <summary>
        /// PropertyChanged callback for a property that affects the selection or caret rendering.
        /// </summary>
        private static void UpdateCaretElement(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FlowDocumentPageViewer viewer = (FlowDocumentPageViewer)d;

            if (viewer.Selection != null)
            {
                CaretElement caretElement = viewer.Selection.CaretElement;
                if (caretElement != null)
                {
                    caretElement.InvalidateVisual();
                }
            }
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
        /// Returns FindToolBar, if enabled.
        /// </summary>
        private FindToolBar FindToolBar
        {
            get { return (_findToolBarHost != null) ? _findToolBarHost.Child as FindToolBar : null; }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private Decorator _findToolBarHost;         // Host for FindToolbar
        private ContentPosition _contentPosition;   // Current position to be maintained during zooming and resizing.
        private FlowDocumentPrintingState _printingState;   // Printing state
        private IDocumentPaginatorSource _oldDocument; // IDocumentPaginatorSource representing Document. Cached separately from DocumentViewerBase
                                                    // due to lifetime issues.
        private object _bringContentPositionIntoViewToken = new object(); // Bring content position into view user state

        private const string _findToolBarHostTemplateName = "PART_FindToolBarHost"; //Name for the Find Toolbar host.

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  IJournalState Members
        //
        //-------------------------------------------------------------------

        #region IJournalState Members

        [Serializable]
        private class JournalState : CustomJournalStateInternal
        {
            public JournalState(int contentPosition, LogicalDirection contentPositionDirection, double zoom)
            {
                ContentPosition = contentPosition;
                ContentPositionDirection = contentPositionDirection;
                Zoom = zoom;
            }
            public int ContentPosition;
            public LogicalDirection ContentPositionDirection;
            public double Zoom;
        }

        /// <summary>
        /// <see cref="IJournalState.GetJournalState"/>
        /// </summary>
        CustomJournalStateInternal IJournalState.GetJournalState(JournalReason journalReason)
        {
            int cp = -1;
            LogicalDirection cpDirection = LogicalDirection.Forward;
            TextPointer contentPosition = ContentPosition as TextPointer;
            if (contentPosition != null)
            {
                cp = contentPosition.Offset;
                cpDirection = contentPosition.LogicalDirection;
            }
            return new JournalState(cp, cpDirection, Zoom);
        }

        /// <summary>
        /// <see cref="IJournalState.RestoreJournalState"/>
        /// </summary>
        void IJournalState.RestoreJournalState(CustomJournalStateInternal state)
        {
            JournalState viewerState = state as JournalState;
            if (state != null)
            {
                SetCurrentValueInternal(ZoomProperty, viewerState.Zoom);
                if (viewerState.ContentPosition != -1)
                {
                    FlowDocument document = Document as FlowDocument;
                    if (document != null)
                    {
                        TextContainer textContainer = document.StructuralCache.TextContainer;
                        if (viewerState.ContentPosition <= textContainer.SymbolCount)
                        {
                            TextPointer contentPosition = textContainer.CreatePointerAtOffset(viewerState.ContentPosition, viewerState.ContentPositionDirection);
                            // This need be called because the UI may not be ready when Contentposition is set.
                            Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(BringContentPositionIntoView), contentPosition);
                        }
                    }
                }
            }
        }

        #endregion IJournalState Members

        //-------------------------------------------------------------------
        //
        //  DTypeThemeStyleKey
        //
        //-------------------------------------------------------------------

        #region DTypeThemeStyleKey

        /// <summary>
        /// Returns the DependencyObjectType for the registered ThemeStyleKey's default
        /// value. Controls will override this method to return approriate types
        /// </summary>
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey
    }
}


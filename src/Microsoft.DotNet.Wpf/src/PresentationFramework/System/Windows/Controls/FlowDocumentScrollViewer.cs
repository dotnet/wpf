// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;                           // Object
using System.Collections;               // IEnumerator
using System.Collections.Generic;       // Stack<T>
using System.Collections.ObjectModel;   // ReadOnlyCollection<T>
using System.Security;                  // SecurityCritical
using System.Windows.Annotations;       // AnnotationService
using System.Windows.Automation.Peers;  // AutomationPeer
using System.Windows.Data;              // BindingOperations
using System.Windows.Controls.Primitives;   // IScrollInfo
using System.Windows.Documents;         // FlowDocument
using System.Windows.Documents.Serialization;  // WritingCompletedEventArgs
using System.Windows.Input;             // KeyEventArgs
using System.Windows.Media;             // ScaleTransform, VisualTreeHelper
using System.Windows.Markup;            // IAddChild
using System.Windows.Threading;         // Dispatcher
using MS.Internal;                      // Invariant, DoubleUtil
using MS.Internal.Annotations.Anchoring;
using MS.Internal.Commands;             // CommandHelpers
using MS.Internal.Controls;             // EmptyEnumerator
using MS.Internal.Documents;            // FindToolBar
using MS.Internal.KnownBoxes;           // BooleanBoxes
using MS.Internal.AppModel;             // IJournalState

namespace System.Windows.Controls
{
    /// <summary>
    /// The FlowDocumentScrollViewer displays a FlowDocument within a bottomless scrolling view;
    /// the content of the FlowDocument is displayed in a single column.
    /// This bottomless scrolling view is similar to the text display provided by web browsers
    /// and most applications today.
    /// </summary>
    [TemplatePart(Name = "PART_ContentHost", Type = typeof(ScrollViewer))]
    [TemplatePart(Name = "PART_FindToolBarHost", Type = typeof(Decorator))]
    [TemplatePart(Name = "PART_ToolBarHost", Type = typeof(Decorator))]
    [ContentProperty("Document")]
    public class FlowDocumentScrollViewer : Control, IAddChild, IServiceProvider, IJournalState
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static Constructor
        /// </summary>
        static FlowDocumentScrollViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FlowDocumentScrollViewer),
                new FrameworkPropertyMetadata(new ComponentResourceKey(typeof(PresentationUIStyleResources), "PUIFlowDocumentScrollViewer")));

            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(FlowDocumentScrollViewer));

            TextBoxBase.SelectionBrushProperty.OverrideMetadata(typeof(FlowDocumentScrollViewer),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(UpdateCaretElement)));
            TextBoxBase.SelectionOpacityProperty.OverrideMetadata(typeof(FlowDocumentScrollViewer),
                new FrameworkPropertyMetadata(TextBoxBase.AdornerSelectionOpacityDefaultValue, new PropertyChangedCallback(UpdateCaretElement)));

            CreateCommandBindings();

            EventManager.RegisterClassHandler(typeof(FlowDocumentScrollViewer), RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(HandleRequestBringIntoView));
            EventManager.RegisterClassHandler(typeof(FlowDocumentScrollViewer), Keyboard.KeyDownEvent, new KeyEventHandler(KeyDownHandler), true);
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public FlowDocumentScrollViewer()
            : base()
        {
            // Set data ID which will be used to identify annotations on content in this viewer
            AnnotationService.SetDataId(this, "FlowDocument");
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Called when the template tree has been created.
        /// </summary>
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

            // Initialize TooBar host.
            _toolBarHost = GetTemplateChild(_toolBarHostTemplateName) as Decorator;
            if (_toolBarHost != null)
            {
                _toolBarHost.Visibility = IsToolBarVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            // Initialize ContentHost.
            // If old ContentHost is enabled, disable it first to ensure appropriate cleanup.
            if (_contentHost != null)
            {
                BindingOperations.ClearBinding(_contentHost, HorizontalScrollBarVisibilityProperty);
                BindingOperations.ClearBinding(_contentHost, VerticalScrollBarVisibilityProperty);
                _contentHost.ScrollChanged -= new ScrollChangedEventHandler(OnScrollChanged);
                RenderScope.Document = null;
                ClearValue(TextEditor.PageHeightProperty);
                _contentHost.Content = null;
            }
            _contentHost = GetTemplateChild(_contentHostTemplateName) as ScrollViewer;
            if (_contentHost != null)
            {
                if (_contentHost.Content != null)
                {
                    throw new NotSupportedException(SR.Get(SRID.FlowDocumentScrollViewerMarkedAsContentHostMustHaveNoContent));
                }
                _contentHost.ScrollChanged += new ScrollChangedEventHandler(OnScrollChanged);
                CreateTwoWayBinding(_contentHost, HorizontalScrollBarVisibilityProperty, "HorizontalScrollBarVisibility");
                CreateTwoWayBinding(_contentHost, VerticalScrollBarVisibilityProperty, "VerticalScrollBarVisibility");

                // Need to make ScrollViewer non-focusable, otherwise it will eat keyboard navigation from editor.
                _contentHost.Focusable = false;

                // Initialize the content of the ScrollViewer.
                _contentHost.Content = new FlowDocumentView();
                RenderScope.Document = Document;
            }

            // Initialize TextEditor.
            AttachTextEditor();

            // Apply the current zoom to the content host.
            ApplyZoom();
        }

        /// <summary>
        /// Invokes the Find Toolbar. This is analogous to the ApplicationCommands.Find.
        /// </summary>
        public void Find()
        {
            OnFindCommand();
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

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties
        /// <summary>
        /// A Property representing a content of this FlowDocumentScrollViewer.
        /// </summary>
        public FlowDocument Document
        {
            get { return (FlowDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }

        /// <summary>
        /// Text Selection (readonly)
        /// </summary>
        public TextSelection Selection
        {
            get
            {
                ITextSelection textSelection = null;
                FlowDocument flowDocument = Document;
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
            get { return (double)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }

        /// <summary>
        /// The maximum allowed value of the Zoom property.
        /// </summary>
        public double MaxZoom
        {
            get { return (double)GetValue(MaxZoomProperty); }
            set { SetValue(MaxZoomProperty, value); }
        }

        /// <summary>
        /// The minimum allowed value of the Zoom property.
        /// </summary>
        public double MinZoom
        {
            get { return (double)GetValue(MinZoomProperty); }
            set { SetValue(MinZoomProperty, value); }
        }

        /// <summary>
        /// The amount the Zoom property is incremented or decremented when
        /// the IncreaseZoom or DecreaseZoom command is executed.
        /// </summary>
        public double ZoomIncrement
        {
            get { return (double)GetValue(ZoomIncrementProperty); }
            set { SetValue(ZoomIncrementProperty, value); }
        }

        /// <summary>
        /// Whether the viewer can increase the current zoom.
        /// </summary>
        public bool CanIncreaseZoom
        {
            get { return (bool)GetValue(CanIncreaseZoomProperty); }
        }

        /// <summary>
        /// Whether the viewer can decrease the current zoom.
        /// </summary>
        public bool CanDecreaseZoom
        {
            get { return (bool)GetValue(CanDecreaseZoomProperty); }
        }

        /// <summary>
        /// Whether text selection is enabled or disabled.
        /// </summary>
        public bool IsSelectionEnabled
        {
            get { return (bool)GetValue(IsSelectionEnabledProperty); }
            set { SetValue(IsSelectionEnabledProperty, value); }
        }

        /// <summary>
        /// Whether the ToolBar is visible or not.
        /// </summary>
        public bool IsToolBarVisible
        {
            get { return (bool)GetValue(IsToolBarVisibleProperty); }
            set { SetValue(IsToolBarVisibleProperty, value); }
        }

        /// <summary>
        /// Whether or not a horizontal scrollbar is shown.
        /// </summary>
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// Whether or not a vertical scrollbar is shown.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
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
        /// <see cref="Document"/>
        /// </summary>
        public static readonly DependencyProperty DocumentProperty =
                DependencyProperty.Register(
                        "Document",
                        typeof(FlowDocument),
                        typeof(FlowDocumentScrollViewer),
                        new FrameworkPropertyMetadata(
                                null,
                                new PropertyChangedCallback(DocumentChanged)));

        /// <summary>
        /// <see cref="Zoom"/>
        /// </summary>
        public static readonly DependencyProperty ZoomProperty =
                FlowDocumentPageViewer.ZoomProperty.AddOwner(
                        typeof(FlowDocumentScrollViewer),
                        new FrameworkPropertyMetadata(
                                100d,
                                new PropertyChangedCallback(ZoomChanged),
                                new CoerceValueCallback(CoerceZoom)));

        /// <summary>
        /// <see cref="MaxZoom"/>
        /// </summary>
        public static readonly DependencyProperty MaxZoomProperty =
                FlowDocumentPageViewer.MaxZoomProperty.AddOwner(
                        typeof(FlowDocumentScrollViewer),
                        new FrameworkPropertyMetadata(
                                200d,
                                new PropertyChangedCallback(MaxZoomChanged),
                                new CoerceValueCallback(CoerceMaxZoom)));

        /// <summary>
        /// <see cref="MinZoom"/>
        /// </summary>
        public static readonly DependencyProperty MinZoomProperty =
                FlowDocumentPageViewer.MinZoomProperty.AddOwner(
                        typeof(FlowDocumentScrollViewer),
                        new FrameworkPropertyMetadata(
                                80d,
                                new PropertyChangedCallback(MinZoomChanged)));

        /// <summary>
        /// <see cref="ZoomIncrement"/>
        /// </summary>
        public static readonly DependencyProperty ZoomIncrementProperty =
                FlowDocumentPageViewer.ZoomIncrementProperty.AddOwner(
                        typeof(FlowDocumentScrollViewer));

        /// <summary>
        /// <see cref="CanIncreaseZoom"/>
        /// </summary>
        private static readonly DependencyPropertyKey CanIncreaseZoomPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanIncreaseZoom",
                        typeof(bool),
                        typeof(FlowDocumentScrollViewer),
                        new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// <see cref="CanIncreaseZoom"/>
        /// </summary>
        public static readonly DependencyProperty CanIncreaseZoomProperty = CanIncreaseZoomPropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="CanDecreaseZoom"/>
        /// </summary>
        private static readonly DependencyPropertyKey CanDecreaseZoomPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanDecreaseZoom",
                        typeof(bool),
                        typeof(FlowDocumentScrollViewer),
                        new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// <see cref="CanDecreaseZoom"/>
        /// </summary>
        public static readonly DependencyProperty CanDecreaseZoomProperty = CanDecreaseZoomPropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="IsSelectionEnabled"/>
        /// </summary>
        public static readonly DependencyProperty IsSelectionEnabledProperty =
                DependencyProperty.Register(
                        "IsSelectionEnabled",
                        typeof(bool),
                        typeof(FlowDocumentScrollViewer),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.TrueBox,
                                new PropertyChangedCallback(IsSelectionEnabledChanged)));

        /// <summary>
        /// <see cref="IsToolBarVisible"/>
        /// </summary>
        public static readonly DependencyProperty IsToolBarVisibleProperty =
                DependencyProperty.Register(
                        "IsToolBarVisible",
                        typeof(bool),
                        typeof(FlowDocumentScrollViewer),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                new PropertyChangedCallback(IsToolBarVisibleChanged)));

        /// <summary>
        /// <see cref="HorizontalScrollBarVisibility"/>
        /// </summary>
        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
                ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner(
                        typeof(FlowDocumentScrollViewer),
                        new FrameworkPropertyMetadata(ScrollBarVisibility.Auto));

        /// <summary>
        /// <see cref="VerticalScrollBarVisibility"/>
        /// </summary>
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
                ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner(
                        typeof(FlowDocumentScrollViewer),
                        new FrameworkPropertyMetadata(ScrollBarVisibility.Visible));

        /// <summary>
        /// <see cref="TextBoxBase.SelectionBrushProperty"/>
        /// </summary>
        public static readonly DependencyProperty SelectionBrushProperty =
            TextBoxBase.SelectionBrushProperty.AddOwner(typeof(FlowDocumentScrollViewer));

        /// <summary>
        /// <see cref="TextBoxBase.SelectionOpacityProperty"/>
        /// </summary>
        public static readonly DependencyProperty SelectionOpacityProperty =
            TextBoxBase.SelectionOpacityProperty.AddOwner(typeof(FlowDocumentScrollViewer));

        /// <summary>
        /// <see cref="TextBoxBase.IsSelectionActiveProperty"/>
        /// </summary>
        public static readonly DependencyProperty IsSelectionActiveProperty =
            TextBoxBase.IsSelectionActiveProperty.AddOwner(typeof(FlowDocumentScrollViewer));

        /// <summary>
        /// <see cref="TextBoxBase.IsInactiveSelectionHighlightEnabledProperty"/>
        /// </summary>
        public static readonly DependencyProperty IsInactiveSelectionHighlightEnabledProperty =
            TextBoxBase.IsInactiveSelectionHighlightEnabledProperty.AddOwner(typeof(FlowDocumentScrollViewer));

        #endregion Public Dynamic Properties

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Called when print has been completed.
        /// </summary>
        protected virtual void OnPrintCompleted()
        {
            ClearPrintingState();
        }

        /// <summary>
        /// Handler for the Find command
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
        protected virtual void OnPrintCommand()
        {
#if !DONOTREFPRINTINGASMMETA
            System.Windows.Xps.XpsDocumentWriter docWriter;
            System.Printing.PrintDocumentImageableArea ia = null;
            FlowDocumentPaginator paginator;
            Thickness pagePadding;

            // Only one printing job is allowed.
            if (_printingState != null)
            {
                return;
            }

            // If the document is FlowDocument, do custom printing. Otherwise go through default path.
            if (Document != null)
            {
                // Show print dialog.
                docWriter = System.Printing.PrintQueue.CreateXpsDocumentWriter(ref ia);
                if (docWriter != null && ia != null)
                {
                    // Suspend layout on FlowDocumentView.
                    if (RenderScope != null)
                    {
                        RenderScope.SuspendLayout();
                    }

                    // Store the current state of the document in the PrintingState
                    paginator = ((IDocumentPaginatorSource)Document).DocumentPaginator as FlowDocumentPaginator;
                    _printingState = new FlowDocumentPrintingState();
                    _printingState.XpsDocumentWriter = docWriter;
                    _printingState.PageSize = paginator.PageSize;
                    _printingState.PagePadding = Document.PagePadding;
                    _printingState.IsSelectionEnabled = IsSelectionEnabled;
                    _printingState.ColumnWidth = Document.ColumnWidth;

                    // Since _printingState value is used to determine CanExecute state, we must invalidate that state.
                    CommandManager.InvalidateRequerySuggested();

                    // Register for XpsDocumentWriter events.
                    docWriter.WritingCompleted += new WritingCompletedEventHandler(HandlePrintCompleted);
                    docWriter.WritingCancelled += new WritingCancelledEventHandler(HandlePrintCancelled);

                    // Add PreviewCanExecute handler to have a chance to disable UI Commands during printing.
                    if (_contentHost != null)
                    {
                        CommandManager.AddPreviewCanExecuteHandler(_contentHost, new CanExecuteRoutedEventHandler(PreviewCanExecuteRoutedEventHandler));
                    }

                    // Disable TextSelection, if currently enabled.
                    if (IsSelectionEnabled)
                    {
                        SetCurrentValueInternal(IsSelectionEnabledProperty, BooleanBoxes.FalseBox);
                    }

                    // Change the PageSize and PagePadding for the document to match the CanvasSize for the printer device.
                    paginator.PageSize = new Size(ia.MediaSizeWidth, ia.MediaSizeHeight);
                    pagePadding = Document.ComputePageMargin();
                    Document.PagePadding = new Thickness(
                        Math.Max(ia.OriginWidth, pagePadding.Left),
                        Math.Max(ia.OriginHeight, pagePadding.Top),
                        Math.Max(ia.MediaSizeWidth - (ia.OriginWidth + ia.ExtentWidth), pagePadding.Right),
                        Math.Max(ia.MediaSizeHeight - (ia.OriginHeight + ia.ExtentHeight), pagePadding.Bottom));
                    Document.ColumnWidth = double.PositiveInfinity;

                    // Send DocumentPaginator to the printer.
                    docWriter.WriteAsync(paginator);
                }
                else
                {
                    OnPrintCompleted();
                }
            }
            else
            {
                OnPrintCompleted();
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        /// <summary>
        /// Handler for the CancelPrint command.
        /// </summary>
        protected virtual void OnCancelPrintCommand()
        {
#if !DONOTREFPRINTINGASMMETA
            if (_printingState != null)
            {
                _printingState.XpsDocumentWriter.CancelAsync();
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        /// <summary>
        /// Handler for the IncreaseZoom command.
        /// </summary>
        protected virtual void OnIncreaseZoomCommand()
        {
            // If can zoom in, increase zoom by the zoom increment value.
            if (CanIncreaseZoom)
            {
                Zoom = Math.Min(Zoom + ZoomIncrement, MaxZoom);
            }
        }

        /// <summary>
        /// Handler for the DecreaseZoom command.
        /// </summary>
        protected virtual void OnDecreaseZoomCommand()
        {
            // If can zoom out, decrease zoom by the zoom increment value.
            if (CanDecreaseZoom)
            {
                Zoom = Math.Max(Zoom - ZoomIncrement, MinZoom);
            }
        }

        /// <summary>
        /// This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Handled) { return; }

            switch (e.Key)
            {
                // Esc -- Close FindToolBar
                case Key.Escape:
                    if (FindToolBar != null)
                    {
                        ToggleFindToolBar(false);
                        e.Handled = true;
                    }
                    break;

                // F3 -- Invoke Find
                case Key.F3:
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
                            // Make the FindToolBar visible
                            ToggleFindToolBar(true);
                        }
                        e.Handled = true;
                    }
                    break;
            }

            // If not handled, do default handling.
            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        /// <summary>
        /// Mouse wheel rotation handler.
        /// </summary>
        /// <param name="e">MouseWheelEventArgs</param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (e.Handled) { return; }

            if (_contentHost != null)
            {
                //Press Ctrl and scroll mouse wheel will zoom in/out the document
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    // If can zoom in, increase zoom by the zoom increment value.
                    if (e.Delta > 0 && CanIncreaseZoom)
                    {
                        SetCurrentValueInternal(ZoomProperty, Math.Min(Zoom + ZoomIncrement, MaxZoom));
                    }
                    else if (e.Delta < 0 && CanDecreaseZoom)
                    {
                        SetCurrentValueInternal(ZoomProperty, Math.Max(Zoom - ZoomIncrement, MinZoom));
                    }
                }
                else
                {
                    if (e.Delta < 0)
                    {
                        _contentHost.LineDown();
                    }
                    else
                    {
                        _contentHost.LineUp();
                    }
                }
                e.Handled = true;
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
            DocumentViewerHelper.OnContextMenuOpening(Document, this, e);
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new FlowDocumentScrollViewerAutomationPeer(this);
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
                if (HasLogicalChildren && Document != null)
                {
                    return new SingleChildEnumerator(Document);
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
        /// Allows FrameworkElement to augment the EventRoute.
        /// </summary>
        internal override bool BuildRouteCore(EventRoute route, RoutedEventArgs args)
        {
            // If FlowDocumentScrollViewer is used as embedded viewer (in FlowDocumentReader),
            // it is not part of logical tree, so default logic in FE to re-add logical
            // tree from branched node does not work here.
            // But before FlowDocumentScrollViewer is added to the event route, logical
            // ancestors up to Document need to be added to the route. Otherwise
            // content will not have a chance to react to events first.
            // This breaks navigation cursor management logic, because TextEditor attached
            // to FlowDocumentScrollViewer handles those events first.
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
        /// Bring specified content position into view.
        /// </summary>
        internal object BringContentPositionIntoView(object arg)
        {
            ITextPointer contentPosition = arg as ITextPointer;
            if (contentPosition != null)
            {
                ITextView textView = GetTextView();
                if (textView != null && textView.IsValid && textView.RenderScope is IScrollInfo && contentPosition.TextContainer == textView.TextContainer)
                {
                    if (textView.Contains(contentPosition))
                    {
                        Rect rect = textView.GetRectangleFromTextPosition(contentPosition);
                        if (rect != Rect.Empty)
                        {
                            IScrollInfo isi = (IScrollInfo)textView.RenderScope;
                            isi.SetVerticalOffset(rect.Top + isi.VerticalOffset);
                        }
                    }
                    else
                    {
                        // Wait until ContentPosition in in the view.
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(BringContentPositionIntoView), contentPosition);
                    }
                }
            }
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
        /// Access to the ScrollViewer in FlowDocumentScrollViewer style
        /// </summary>
        internal ScrollViewer ScrollViewer { get { return _contentHost; } }

        /// <summary>
        /// Whether FindToolBar can be enabled.
        /// </summary>
        internal bool CanShowFindToolBar
        {
            get { return (_findToolBarHost != null && Document != null && _textEditor != null); }
        }

        /// <summary>
        /// Whether currently printing the content.
        /// </summary>
        internal bool IsPrinting
        {
            get { return (_printingState != null); }
        }

        /// <summary>
        /// Returns textpointer for upper left corner of content.
        /// </summary>
        internal TextPointer ContentPosition
        {
            get
            {
                TextPointer contentPosition = null;
                ITextView textView = GetTextView();
                if (textView != null && textView.IsValid && textView.RenderScope is IScrollInfo)
                {
                    contentPosition = textView.GetTextPositionFromPoint(new Point(), true) as TextPointer;
                }
                return contentPosition;
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
        /// Enables/disables the FindToolBar.
        /// </summary>
        /// <param name="enable">Whether to enable/disable FindToolBar.</param>
        private void ToggleFindToolBar(bool enable)
        {
            Invariant.Assert(enable == (FindToolBar == null));
            DocumentViewerHelper.ToggleFindToolBar(_findToolBarHost, new EventHandler(OnFindInvoked), enable);
            // FindToolBar is embedded inside the toolbar, so event if the ToolBar is not visible
            // it needs to be shown when showing FindToolBar.
            if (!IsToolBarVisible && _toolBarHost != null)
            {
                _toolBarHost.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Apply the zoom value to the render scope.
        /// </summary>
        private void ApplyZoom()
        {
            if (RenderScope != null)
            {
                RenderScope.LayoutTransform = new ScaleTransform(Zoom / 100, Zoom / 100);
            }
        }

        /// <summary>
        /// Attach TextEditor to Document, if supports text.
        /// </summary>
        private void AttachTextEditor()
        {
            //get annotation service and state so we can restore it
            AnnotationService service = AnnotationService.GetService(this);
            bool serviceOldState = false;

            //disable the service if enabled
            if ((service != null) && service.IsEnabled)
            {
                serviceOldState = true;
                service.Disable();
            }

            // This method is called when Document is changing or control template
            // is replaced, so need to drop old TextEditor data.
            if (_textEditor != null)
            {
                _textEditor.TextContainer.TextView = null;
                _textEditor.OnDetach();
                _textEditor = null;
            }

            ITextView textView = null;
            if (Document != null)
            {
                textView = GetTextView();
                Document.StructuralCache.TextContainer.TextView = textView;
            }

            // If new Document supports TextEditor, create one.
            // If the Document is already attached to TextEditor (TextSelection != null),
            // do not create TextEditor for this instance of the viewer. (This situation may happen
            // when the same instance of Document is attached to more than one viewer).
            if (IsSelectionEnabled &&
                Document != null &&
                RenderScope != null &&
                Document.StructuralCache.TextContainer.TextSelection == null)
            {
                _textEditor = new TextEditor(Document.StructuralCache.TextContainer, this, false);
                _textEditor.IsReadOnly = !IsEditingEnabled;
                _textEditor.TextView = textView;
            }

            //restore AnnotationsService state
            if ((service != null) && serviceOldState)
            {
                service.Enable(service.Store);
            }

            // If TextEditor is not enabled, FindToolBar cannot be visible.
            if (_textEditor == null && FindToolBar != null)
            {
                ToggleFindToolBar(false);
            }
        }

        /// <summary>
        /// Handler for ScrollViewer's OnScrollChanged event.
        /// </summary>
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.OriginalSource == ScrollViewer)
            {
                // If ScrollViewer.ViewportHeight has been changed, TextEditor.PageHeight must be updated
                if (!DoubleUtil.IsZero(e.ViewportHeightChange))
                {
                    SetValue(TextEditor.PageHeightProperty, e.ViewportHeight);
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
        /// Clear printing state.
        /// </summary>
        private void ClearPrintingState()
        {
#if !DONOTREFPRINTINGASMMETA
            if (_printingState != null)
            {
                // Resume layout on FlowDocumentView.
                if (RenderScope != null)
                {
                    RenderScope.ResumeLayout();
                }

                // Enable TextSelection, if it was previously enabled.
                if (_printingState.IsSelectionEnabled)
                {
                    SetCurrentValueInternal(IsSelectionEnabledProperty, BooleanBoxes.TrueBox);
                }

                // Remove PreviewCanExecute handler (added when Print command was executed).
                if (_contentHost != null)
                {
                    CommandManager.RemovePreviewCanExecuteHandler(_contentHost, new CanExecuteRoutedEventHandler(PreviewCanExecuteRoutedEventHandler));
                }

                // Unregister for XpsDocumentWriter events.
                _printingState.XpsDocumentWriter.WritingCompleted -= new WritingCompletedEventHandler(HandlePrintCompleted);
                _printingState.XpsDocumentWriter.WritingCancelled -= new WritingCancelledEventHandler(HandlePrintCancelled);

                // Restore old page metrics on FlowDocument.
                Document.PagePadding = _printingState.PagePadding;
                Document.ColumnWidth = _printingState.ColumnWidth;
                ((IDocumentPaginatorSource)Document).DocumentPaginator.PageSize = _printingState.PageSize;

                _printingState = null;
                // Since _documentWriter value is used to determine CanExecute state, we must invalidate that state.
                CommandManager.InvalidateRequerySuggested();
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        /// <summary>
        /// Makes sure the target is visible in the client area.
        /// </summary>
        /// <param name="args">RequestBringIntoViewEventArgs indicates the element and region to scroll into view.</param>
        private void HandleRequestBringIntoView(RequestBringIntoViewEventArgs args)
        {
            DependencyObject child;
            DependencyObject document;
            IContentHost ich;
            ReadOnlyCollection<Rect> rects;
            UIElement targetUIElement;
            Rect targetRect = Rect.Empty;

            if (args != null && args.TargetObject != null && Document != null)
            {
                document = Document;

                // If the passed in object is a logical child of FlowDocumentScrollViewer's Document,
                // attempt to make it visible now.
                // Special case: TargetObject is the document itself. Then scroll to the top (page 1).
                // This supports navigating from baseURI#anchor to just baseURI.
                if (args.TargetObject == document)
                {
                    if (_contentHost != null)
                    {
                        _contentHost.ScrollToHome();
                    }
                    args.Handled = true; // Mark the event as handled.
                }
                else if (args.TargetObject is UIElement)
                {
                    targetUIElement = (UIElement)args.TargetObject;
                    // Since entire content of FlowDocument is represented by
                    // bottomless page, the target has to be connected to visual tree.
                    // Otherwise, it is not descendant of FlowDocument.
                    if (RenderScope != null && RenderScope.IsAncestorOf(targetUIElement))
                    {
                        targetRect = args.TargetRect;
                        if (targetRect.IsEmpty)
                        {
                            targetRect = new Rect(targetUIElement.RenderSize);
                        }

                        targetRect = MakeVisible((IScrollInfo)RenderScope, targetUIElement, targetRect);

                        if(!targetRect.IsEmpty)
                        {
                            GeneralTransform t = RenderScope.TransformToAncestor(this);
                            targetRect = t.TransformBounds(targetRect);
                        }
                        args.Handled = true; // Mark the event as handled.
                    }
                }
                else if (args.TargetObject is ContentElement)
                {
                    // Verify if TargetObject is in fact a child of Document.
                    child = args.TargetObject;
                    while (child != null && child != document)
                    {
                       child = LogicalTreeHelper.GetParent(child);
                    }

                    if (child != null)
                    {
                        ich = GetIContentHost();
                        if (ich != null)
                        {
                            // Get the position of the content.
                            rects = ich.GetRectangles((ContentElement)args.TargetObject);
                            if (rects.Count > 0)
                            {
                                targetRect = MakeVisible((IScrollInfo)RenderScope, (Visual)ich, rects[0]);

                                if(!targetRect.IsEmpty)
                                {
                                    GeneralTransform t = RenderScope.TransformToAncestor(this);
                                    targetRect = t.TransformBounds(targetRect);
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
        /// The Document has changed and needs to be updated.
        /// </summary>
        private void DocumentChanged(FlowDocument oldDocument, FlowDocument newDocument)
        {
            // Use TextSelection to determine whether the new document belongs to another
            // control or not.
            if (newDocument != null &&
                newDocument.StructuralCache.TextContainer != null &&
                newDocument.StructuralCache.TextContainer.TextSelection != null)
            {
                throw new ArgumentException(SR.Get(SRID.FlowDocumentScrollViewerDocumentBelongsToAnotherFlowDocumentScrollViewerAlready));
            }

            // Cleanup state associated with the old document.
            if (oldDocument != null)
            {
                // If Document was added to logical tree of FlowDocumentScrollViewer before, remove it.
                if (_documentAsLogicalChild)
                {
                    RemoveLogicalChild(oldDocument);
                }
                // Remove the document from the ContentHost.
                if (RenderScope != null)
                {
                    RenderScope.Document = null;
                }

                oldDocument.ClearValue(PathNode.HiddenParentProperty);
                oldDocument.StructuralCache.ClearUpdateInfo(true);
            }

            // If FlowDocumentScrollViewer was created through style, then do not modify
            // the logical tree. Instead, set "core parent" for the Document.
            if (newDocument != null && LogicalTreeHelper.GetParent(newDocument) != null)
            {
                // Set the "core parent" back to us.
                ContentOperations.SetParent(newDocument, this);
                _documentAsLogicalChild = false;
            }
            else
            {
                _documentAsLogicalChild = true;
            }

            // Initialize state associated with the new document.
            if (newDocument != null)
            {
                // Set the document on the ContentHost.
                if (RenderScope != null)
                {
                    RenderScope.Document = newDocument;
                }
                // If Document should be part of FlowDocumentScrollViewer's logical tree, add it.
                if (_documentAsLogicalChild)
                {
                    AddLogicalChild(newDocument);
                }

                // Set the hidden parent of the document
                newDocument.SetValue(PathNode.HiddenParentProperty, this);
                newDocument.StructuralCache.ClearUpdateInfo(true);
            }

            // Attach TextEditor, if content supports it. This method will also
            // detach TextEditor from old content.
            AttachTextEditor();

            // Update the toolbar with our current document state.
            if (!CanShowFindToolBar)
            {
                // Disable FindToolBar, if the content does not support it.
                if (FindToolBar != null)
                {
                    ToggleFindToolBar(false);
                }
            }

            // Document is also represented as Automation child. Need to invalidate peer to force update.
            FlowDocumentScrollViewerAutomationPeer peer = UIElementAutomationPeer.FromElement(this) as FlowDocumentScrollViewerAutomationPeer;
            if (peer != null)
            {
                peer.InvalidatePeer();
            }
        }

        /// <summary>
        /// Retrieves ITextView associated with the content.
        /// NOTE: Not retrieved from TextEditor, because it may not exist in some cases.
        /// </summary>
        private ITextView GetTextView()
        {
            ITextView textView = null;
            if (RenderScope is IServiceProvider)
            {
                textView = (ITextView)((IServiceProvider)RenderScope).GetService(typeof(ITextView));
            }
            return textView;
        }

        /// <summary>
        /// Retrieves IContentHost associated with the content.
        /// </summary>
        private IContentHost GetIContentHost()
        {
            IContentHost ich = null;
            if (RenderScope != null && VisualTreeHelper.GetChildrenCount(RenderScope) > 0)
            {
                ich = VisualTreeHelper.GetChild(RenderScope, 0) as IContentHost;
            }
            return ich;
        }

        /// <summary>
        /// Create two way property binding.
        /// </summary>
        private void CreateTwoWayBinding(FrameworkElement fe, DependencyProperty dp, string propertyPath)
        {
            Binding binding = new Binding(propertyPath);
            binding.Mode = BindingMode.TwoWay;
            binding.Source = this;
            fe.SetBinding(dp, binding);
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

            // Create private commands to control content scrolling. It is required because TextEditor binds
            // those Keys to its own commands (caret navigation), which are useless in viewing scenarios.
            // Since following commands are private, it is OK to pass String.Empty as descriptive text for the command.
            _commandLineDown = new RoutedUICommand(String.Empty, "FDSV_LineDown", typeof(FlowDocumentScrollViewer));
            _commandLineUp = new RoutedUICommand(String.Empty, "FDSV_LineUp", typeof(FlowDocumentScrollViewer));
            _commandLineLeft = new RoutedUICommand(String.Empty, "FDSV_LineLeft", typeof(FlowDocumentScrollViewer));
            _commandLineRight = new RoutedUICommand(String.Empty, "FDSV_LineRight", typeof(FlowDocumentScrollViewer));

            // Register editing command handlers
            TextEditor.RegisterCommandHandlers(typeof(FlowDocumentScrollViewer), /*acceptsRichContent:*/true, /*readOnly:*/!IsEditingEnabled, /*registerEventListeners*/true);

            // Command: ApplicationCommands.Find
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), ApplicationCommands.Find,
                executedHandler, canExecuteHandler);

            // Command: ApplicationCommands.Print
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), ApplicationCommands.Print,
                executedHandler, canExecuteHandler);

            // Command: ApplicationCommands.CancelPrint
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), ApplicationCommands.CancelPrint,
                executedHandler, canExecuteHandler); // no key gesture

            // Command: NavigationCommands.PreviousPage
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), NavigationCommands.PreviousPage,
                executedHandler, canExecuteHandler, Key.PageUp);

            // Command: NavigationCommands.NextPage
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), NavigationCommands.NextPage,
                executedHandler, canExecuteHandler, Key.PageDown);

            // Command: NavigationCommands.FirstPage
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), NavigationCommands.FirstPage,
                executedHandler, canExecuteHandler, new KeyGesture(Key.Home), new KeyGesture(Key.Home, ModifierKeys.Control));

            // Command: NavigationCommands.LastPage
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), NavigationCommands.LastPage,
                executedHandler, canExecuteHandler, new KeyGesture(Key.End), new KeyGesture(Key.End, ModifierKeys.Control));

            // Command: NavigationCommands.IncreaseZoom
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), NavigationCommands.IncreaseZoom,
                executedHandler, canExecuteHandler, new KeyGesture(Key.OemPlus, ModifierKeys.Control));

            // Command: NavigationCommands.DecreaseZoom
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), NavigationCommands.DecreaseZoom,
                executedHandler, canExecuteHandler, new KeyGesture(Key.OemMinus, ModifierKeys.Control));

            // Command: FDSV_LineDown
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), _commandLineDown,
                executedHandler, canExecuteHandler, Key.Down);

            // Command: FDSV_LineUp
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), _commandLineUp,
                executedHandler, canExecuteHandler, Key.Up);

            // Command: FDSV_LineLeft
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), _commandLineLeft,
                executedHandler, canExecuteHandler, Key.Left);

            // Command: FDSV_LineRight
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentScrollViewer), _commandLineRight,
                executedHandler, canExecuteHandler, Key.Right);
        }

        /// <summary>
        /// Central handler for CanExecuteRouted events fired by Commands directed at FlowDocumentScrollViewer.
        /// </summary>
        /// <param name="target">The target of this Command, expected to be FlowDocumentScrollViewer</param>
        /// <param name="args">The event arguments for this event.</param>
        private static void CanExecuteRoutedEventHandler(object target, CanExecuteRoutedEventArgs args)
        {
            FlowDocumentScrollViewer viewer = target as FlowDocumentScrollViewer;
            Invariant.Assert(viewer != null, "Target of QueryEnabledEvent must be FlowDocumentScrollViewer.");
            Invariant.Assert(args != null, "args cannot be null.");

            // FlowDocumentScrollViewer is capable of execution of the majority of its commands.
            // Special rules:
            // a) during printing only CancelPrint is enabled.
            // b) Find command is enabled only when FindToolBar is enabled.
            // c) Print command is enabled when Document is attached.
            // d) CancelPrint command is enabled only during printing.
            if (viewer._printingState == null)
            {
                if (args.Command == ApplicationCommands.Find)
                {
                    args.CanExecute = viewer.CanShowFindToolBar;
                }
                else if (args.Command == ApplicationCommands.Print)
                {
                    args.CanExecute = (viewer.Document != null);
                }
                else if (args.Command == ApplicationCommands.CancelPrint)
                {
                    args.CanExecute = false;
                }
                else
                {
                    args.CanExecute = true;
                }
            }
            else
            {
                args.CanExecute = (args.Command == ApplicationCommands.CancelPrint);
            }
        }

        /// <summary>
        /// Central handler for all ExecutedRoutedEvent fired by Commands directed at FlowDocumentScrollViewer.
        /// </summary>
        /// <param name="target">The target of this Command, expected to be FlowDocumentScrollViewer.</param>
        /// <param name="args">The event arguments associated with this event.</param>
        private static void ExecutedRoutedEventHandler(object target, ExecutedRoutedEventArgs args)
        {
            FlowDocumentScrollViewer viewer = target as FlowDocumentScrollViewer;
            Invariant.Assert(viewer != null, "Target of ExecuteEvent must be FlowDocumentScrollViewer.");
            Invariant.Assert(args != null, "args cannot be null.");

            // Now we execute the method corresponding to the Command that fired this event;
            // each Command has its own protected virtual method that performs the operation
            // corresponding to the Command.
            if (args.Command == ApplicationCommands.Find)
            {
                viewer.OnFindCommand();
            }
            else if (args.Command == ApplicationCommands.Print)
            {
                viewer.OnPrintCommand();
            }
            else if (args.Command == ApplicationCommands.CancelPrint)
            {
                viewer.OnCancelPrintCommand();
            }
            else if (args.Command == NavigationCommands.IncreaseZoom)
            {
                viewer.OnIncreaseZoomCommand();
            }
            else if (args.Command == NavigationCommands.DecreaseZoom)
            {
                viewer.OnDecreaseZoomCommand();
            }
            else if (args.Command == _commandLineDown)
            {
                if (viewer._contentHost != null)
                {
                    viewer._contentHost.LineDown();
                }
            }
            else if (args.Command == _commandLineUp)
            {
                if (viewer._contentHost != null)
                {
                    viewer._contentHost.LineUp();
                }
            }
            else if (args.Command == _commandLineLeft)
            {
                if (viewer._contentHost != null)
                {
                    viewer._contentHost.LineLeft();
                }
            }
            else if (args.Command == _commandLineRight)
            {
                if (viewer._contentHost != null)
                {
                    viewer._contentHost.LineRight();
                }
            }
            else if (args.Command == NavigationCommands.NextPage)
            {
                if (viewer._contentHost != null)
                {
                    viewer._contentHost.PageDown();
                }
            }
            else if (args.Command == NavigationCommands.PreviousPage)
            {
                if (viewer._contentHost != null)
                {
                    viewer._contentHost.PageUp();
                }
            }
            else if (args.Command == NavigationCommands.FirstPage)
            {
                if (viewer._contentHost != null)
                {
                    viewer._contentHost.ScrollToHome();
                }
            }
            else if (args.Command == NavigationCommands.LastPage)
            {
                if (viewer._contentHost != null)
                {
                    viewer._contentHost.ScrollToEnd();
                }
            }
            else
            {
                Invariant.Assert(false, "Command not handled in ExecutedRoutedEventHandler.");
            }
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
            FindToolBar findToolBar = FindToolBar;

            if (findToolBar != null && _textEditor != null)
            {
                // In order to show current text selection TextEditor requires Focus to be set on the UIScope.
                // If there embedded controls, it may happen that embedded control currently has focus and find
                // was invoked through hotkeys. To support this case we manually move focus to the appropriate element.
                Focus();

                findResult = DocumentViewerHelper.Find(findToolBar, _textEditor, _textEditor.TextView, _textEditor.TextView);

                // If we found something, TextEditor will bring the selection into view.
                // It is possible, because RenderScope is inside ScrollViewer.

                // If we did not find anything, alert the user.
                if ((findResult == null) || findResult.IsEmpty)
                {
                    DocumentViewerHelper.ShowFindUnsuccessfulMessage(findToolBar);
                }
            }
        }

        /// <summary>
        /// Disable commands on DocumentViewerBase when this printing is in progress.
        /// </summary>
        private void PreviewCanExecuteRoutedEventHandler(object target, CanExecuteRoutedEventArgs args)
        {
            ScrollViewer sv = target as ScrollViewer;
            Invariant.Assert(sv != null, "Target of PreviewCanExecuteRoutedEventHandler must be ScrollViewer.");
            Invariant.Assert(args != null, "args cannot be null.");

            // Disable UI commands, if printing is in progress.
            if (_printingState != null)
            {
                args.CanExecute = false;
                args.Handled = true;
            }
        }

        /// <summary>
        /// Called when a key event occurs.
        /// </summary>
        private static void KeyDownHandler(object sender, KeyEventArgs e)
        {
            DocumentViewerHelper.KeyDownHelper(e, ((FlowDocumentScrollViewer)sender)._findToolBarHost);
        }

        #endregion Commands

        #region Static Methods

        /// <summary>
        /// Wrapper around IScrollInfo.MakeVisible
        /// </summary>
        /// <param name="scrollInfo">The IScrollInfo to call MakeVisible on</param>
        /// <param name="visual">visual parameter for call to MakeVisible</param>
        /// <param name="rectangle">rectangle parameter for call to MakeVisible</param>
        /// <returns>Rectangle representing visible portion of visual relative to scrollInfo's viewport</returns>
        private static Rect MakeVisible(IScrollInfo scrollInfo, Visual visual, Rect rectangle)
        {
            // ScrollContentPresenter.MakeVisible can cause an exception when encountering an empty rectangle
            // Workaround for ScrollContentPresenter.MakeVisible
            // The method throws InvalidOperationException in some scenarios where it should return Rect.Empty
            // Do not workaround for derived classes

            Rect result;

            if(scrollInfo.GetType() == typeof(System.Windows.Controls.ScrollContentPresenter))
            {
                result = ((ScrollContentPresenter)scrollInfo).MakeVisible(visual, rectangle, false);
            }
            else
            {
                result = scrollInfo.MakeVisible(visual, rectangle);
            }

            return result;
        }

        /// <summary>
        /// The Document has changed and needs to be updated.
        /// </summary>
        private static void DocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentScrollViewer);
            ((FlowDocumentScrollViewer)d).DocumentChanged((FlowDocument)e.OldValue, (FlowDocument)e.NewValue);

            // Since Document state is used to determine CanExecute state, we must invalidate that state.
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// The Zoom has changed and needs to be updated.
        /// </summary>
        private static void ZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentScrollViewer);
            FlowDocumentScrollViewer viewer = (FlowDocumentScrollViewer)d;
            if (!DoubleUtil.AreClose((double)e.OldValue, (double)e.NewValue))
            {
                // If zoom has been changed, CanIncrease/DecreaseZoom property need to be updated.
                viewer.SetValue(CanIncreaseZoomPropertyKey, BooleanBoxes.Box(DoubleUtil.GreaterThan(viewer.MaxZoom, viewer.Zoom)));
                viewer.SetValue(CanDecreaseZoomPropertyKey, BooleanBoxes.Box(DoubleUtil.LessThan(viewer.MinZoom, viewer.Zoom)));

                // Apply the new zoom value.
                viewer.ApplyZoom();
            }
        }

        /// <summary>
        /// Coerce Zoom with Max/MinZoom, MinZoom works as the baseline.
        /// </summary>
        private static object CoerceZoom(DependencyObject d, object value)
        {
            Invariant.Assert(d != null && d is FlowDocumentScrollViewer);
            FlowDocumentScrollViewer viewer = (FlowDocumentScrollViewer)d;

            double zoom = (double)value;

            double maxZoom = viewer.MaxZoom;
            if (DoubleUtil.LessThan(maxZoom, zoom))
            {
                return maxZoom;
            }

            double minZoom = viewer.MinZoom;
            if (DoubleUtil.GreaterThan(minZoom, zoom))
            {
                return minZoom;
            }

            return value;
        }

        /// <summary>
        /// The MaxZoom has changed and needs to be updated.
        /// </summary>
        private static void MaxZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentScrollViewer);
            FlowDocumentScrollViewer viewer = (FlowDocumentScrollViewer)d;

            viewer.CoerceValue(ZoomProperty);
            viewer.SetValue(CanIncreaseZoomPropertyKey, BooleanBoxes.Box(DoubleUtil.GreaterThan(viewer.MaxZoom, viewer.Zoom)));
        }

        /// <summary>
        /// MaxZoom need to be coerced if MinZoom > MaxZoom
        /// </summary>
        private static object CoerceMaxZoom(DependencyObject d, object value)
        {
            Invariant.Assert(d != null && d is FlowDocumentScrollViewer);
            FlowDocumentScrollViewer viewer = (FlowDocumentScrollViewer)d;

            double min = viewer.MinZoom;
            return ((double)value < min) ? min : value;
        }

        /// <summary>
        /// The MinZoom has changed and needs to be updated.
        /// </summary>
        private static void MinZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentScrollViewer);
            FlowDocumentScrollViewer viewer = (FlowDocumentScrollViewer)d;

            viewer.CoerceValue(MaxZoomProperty);
            viewer.CoerceValue(ZoomProperty);
            viewer.SetValue(CanDecreaseZoomPropertyKey, BooleanBoxes.Box(DoubleUtil.LessThan(viewer.MinZoom, viewer.Zoom)));
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
        /// Called from the event handler to make sure the target is visible in the client area.
        /// </summary>
        /// <param name="sender">The instance handling the event.</param>
        /// <param name="args">RequestBringIntoViewEventArgs indicates the element and region to scroll into view.</param>
        private static void HandleRequestBringIntoView(object sender, RequestBringIntoViewEventArgs args)
        {
            if (sender != null && sender is FlowDocumentScrollViewer)
            {
                ((FlowDocumentScrollViewer)sender).HandleRequestBringIntoView(args);
            }
        }

        /// <summary>
        /// The IsSelectionEnabled has changed and needs to be updated.
        /// </summary>
        private static void IsSelectionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentScrollViewer);
            FlowDocumentScrollViewer viewer = (FlowDocumentScrollViewer)d;

            viewer.AttachTextEditor();
        }

        /// <summary>
        /// The IsToolBarVisible has changed and needs to be updated.
        /// </summary>
        private static void IsToolBarVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentScrollViewer);
            FlowDocumentScrollViewer viewer = (FlowDocumentScrollViewer)d;

            if (viewer._toolBarHost != null)
            {
                viewer._toolBarHost.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// PropertyChanged callback for a property that affects the selection or caret rendering.
        /// </summary>
        private static void UpdateCaretElement(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FlowDocumentScrollViewer viewer = (FlowDocumentScrollViewer)d;

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

        /// <summary>
        /// Content of the ScrollViewer which is treated as RenderScope for the TextEditor.
        /// </summary>
        private FlowDocumentView RenderScope
        {
            get { return (_contentHost != null) ? _contentHost.Content as FlowDocumentView : null; }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private TextEditor _textEditor;             // Text editor (enables text selection)
        private Decorator _findToolBarHost;         // Host for FindToolBar
        private Decorator _toolBarHost;             // Host for ToolBar
        private ScrollViewer _contentHost;          // Host for content viewer
        private bool _documentAsLogicalChild;       // Is Document part of logical tree
        private FlowDocumentPrintingState _printingState;   // Printing state

        private const string _contentHostTemplateName = "PART_ContentHost";         // Name for ContentHost
        private const string _findToolBarHostTemplateName = "PART_FindToolBarHost"; // Name for the Find ToolBar host
        private const string _toolBarHostTemplateName = "PART_ToolBarHost";         // Name for the ToolBar host

        private static bool IsEditingEnabled = false;       // A flag enabling text editing within the viewer
                                                            // accessible only through reflection.
        private static RoutedUICommand _commandLineDown;    // Private LineDown command
        private static RoutedUICommand _commandLineUp;      // Private LineUp command
        private static RoutedUICommand _commandLineLeft;    // Private LineLeft command
        private static RoutedUICommand _commandLineRight;   // Private LineRight command

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  IAddChild Members
        //
        //-------------------------------------------------------------------

        #region IAddChild Members

        /// <summary>
        /// Called to add the object as a Child.
        /// </summary>
        /// <param name="value">Object to add as a child.</param>
        /// <remarks>FlowDocumentScrollViewer only supports a single child of type IDocumentPaginator.</remarks>
        void IAddChild.AddChild(Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            // Check if Content has already been set.
            if (this.Document != null)
            {
                throw new ArgumentException(SR.Get(SRID.FlowDocumentScrollViewerCanHaveOnlyOneChild));
            }
            if (!(value is FlowDocument))
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(FlowDocument)), "value");
            }
            Document = value as FlowDocument;
        }

        /// <summary>
        /// Called when text appears under the tag in markup
        /// </summary>
        /// <param name="text">Text to add to the Object.</param>
        /// <remarks>FlowDocumentScrollViewer does not support Text children.</remarks>
        void IAddChild.AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        #endregion IAddChild Members

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
            object service = null;
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            // Following services are available:
            // (1) TextView
            // (2) TextContainer
            if (serviceType == typeof(ITextView))
            {
                service = GetTextView();
            }
            else if (serviceType == typeof(TextContainer) || serviceType == typeof(ITextContainer))
            {
                if (Document != null)
                {
                    service = ((IServiceProvider)Document).GetService(serviceType);
                }
            }
            return service;
        }

        #endregion IServiceProvider Members

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
            TextPointer contentPosition = ContentPosition;
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
                Zoom = viewerState.Zoom;
                if (viewerState.ContentPosition != -1)
                {
                    FlowDocument document = Document;
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

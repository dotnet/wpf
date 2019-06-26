// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;                               // Object
using System.Collections;                   // IEnumerator
using System.Collections.Generic;           // Stack<T>
using System.Collections.ObjectModel;       // ReadOnlyCollection<T>
using System.Security;                      // SecurityCritical
using System.Windows.Automation.Peers;      // AutomationPeer
using System.Windows.Data;                  // BindingOperations
using System.Windows.Controls.Primitives;   // PlacementMode
using System.Windows.Documents;             // FlowDocument
using System.Windows.Input;                 // KeyEventArgs
using System.Windows.Media;                 // ScaleTransform, VisualTreeHelper
using System.Windows.Markup;                // IAddChild
using System.Windows.Threading;             // Dispatcher
using MS.Internal;                          // Invariant, DoubleUtil
using MS.Internal.Commands;                 // CommandHelpers
using MS.Internal.Controls;                 // EmptyEnumerator
using MS.Internal.Documents;                // FindToolBar
using MS.Internal.KnownBoxes;               // BooleanBoxes
using MS.Internal.AppModel;                 // IJournalState

namespace System.Windows.Controls
{
    /// <summary>
    /// FlowDocumentReader provides a full user experience for consuming text content.
    /// It will be used as the default viewer for loose XAML or containers that contain
    /// text content, and can be styled and re-used by developers for use within their applications.
    /// </summary>
    [TemplatePart(Name = "PART_ContentHost", Type = typeof(Decorator))]
    [TemplatePart(Name = "PART_FindToolBarHost", Type = typeof(Decorator))]
    [ContentProperty("Document")]
    public class FlowDocumentReader : Control, IAddChild, IJournalState
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
        static FlowDocumentReader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FlowDocumentReader),
                new FrameworkPropertyMetadata(new ComponentResourceKey(typeof(PresentationUIStyleResources), "PUIFlowDocumentReader")));

            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(FlowDocumentReader));

            TextBoxBase.SelectionBrushProperty.OverrideMetadata(typeof(FlowDocumentReader),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(UpdateCaretElement)));
            TextBoxBase.SelectionOpacityProperty.OverrideMetadata(typeof(FlowDocumentReader),
                new FrameworkPropertyMetadata(TextBoxBase.AdornerSelectionOpacityDefaultValue, new PropertyChangedCallback(UpdateCaretElement)));

            CreateCommandBindings();

            EventManager.RegisterClassHandler(typeof(FlowDocumentReader), Keyboard.KeyDownEvent, new KeyEventHandler(KeyDownHandler), true);
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public FlowDocumentReader()
            : base()
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Build Visual tree
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Initialize ContentHost.
            // If old ContentHost is enabled, disable it first to ensure appropriate cleanup.
            if (CurrentViewer != null)
            {
                DetachViewer(CurrentViewer);
                _contentHost.Child = null;
            }
            _contentHost = GetTemplateChild(_contentHostTemplateName) as Decorator;
            if (_contentHost != null)
            {
                if (_contentHost.Child != null)
                {
                    throw new NotSupportedException(SR.Get(SRID.FlowDocumentReaderDecoratorMarkedAsContentHostMustHaveNoContent));
                }

                SwitchViewingModeCore(ViewingMode);
            }

            // Initialize FindTooBar host.
            // If old FindToolBar is enabled, disable it first to ensure appropriate cleanup.
            if (FindToolBar != null)
            {
                ToggleFindToolBar(false);
            }
            _findToolBarHost = GetTemplateChild(_findToolBarHostTemplateName) as Decorator;
            _findButton = GetTemplateChild(_findButtonTemplateName) as ToggleButton;
        }

        /// <summary>
        /// Whether the master page can be moved to the specified page.
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        public bool CanGoToPage(int pageNumber)
        {
            bool canGoToPage = false;
            if (CurrentViewer != null)
            {
                canGoToPage = CurrentViewer.CanGoToPage(pageNumber);
            }
            return canGoToPage;
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

        /// <summary>
        /// Switches the current viewing mode. This is analogous to the SwitchViewingModeCommand.
        /// </summary>
        /// <param name="viewingMode">Viewing mode.</param>
        public void SwitchViewingMode(FlowDocumentReaderViewingMode viewingMode)
        {
            OnSwitchViewingModeCommand(viewingMode);
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// ViewingMode of FlowDocumentReader.
        /// </summary>
        public FlowDocumentReaderViewingMode ViewingMode
        {
            get { return (FlowDocumentReaderViewingMode)GetValue(ViewingModeProperty); }
            set { SetValue(ViewingModeProperty, value); }
        }

        /// <summary>
        /// Text Selection (readonly)
        /// </summary>
        public TextSelection Selection
        {
            get
            {
                TextSelection result = null;
                IFlowDocumentViewer viewer;
                if (_contentHost != null)
                {
                    viewer = _contentHost.Child as IFlowDocumentViewer;
                    if(viewer != null)
                    {
                        result = viewer.TextSelection as TextSelection;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Whether Page view can be enabled or not.
        /// </summary>
        public bool IsPageViewEnabled
        {
            get { return (bool)GetValue(IsPageViewEnabledProperty); }
            set { SetValue(IsPageViewEnabledProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// Whether TwoPage view can be enabled or not.
        /// </summary>
        public bool IsTwoPageViewEnabled
        {
            get { return (bool)GetValue(IsTwoPageViewEnabledProperty); }
            set { SetValue(IsTwoPageViewEnabledProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// Whether Scroll view can be enabled or not.
        /// </summary>
        public bool IsScrollViewEnabled
        {
            get { return (bool)GetValue(IsScrollViewEnabledProperty); }
            set { SetValue(IsScrollViewEnabledProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// The number of pages currently available for viewing. This value
        /// is updated as content is paginated, and will change dramatically
        /// when the content is resized, or edited.
        /// </summary>
        public int PageCount
        {
            get { return (int)GetValue(PageCountProperty); }
        }

        /// <summary>
        /// The one-based page number of the page being displayed. If there is no content,
        /// this value will be 0.
        /// </summary>
        public int PageNumber
        {
            get { return (int)GetValue(PageNumberProperty); }
        }

        /// <summary>
        /// Whether the viewer can move the master page to the previous page.
        /// </summary>
        public bool CanGoToPreviousPage
        {
            get { return (bool)GetValue(CanGoToPreviousPageProperty); }
        }

        /// <summary>
        /// Whether the viewer can advance the master page to the next page.
        /// </summary>
        public bool CanGoToNextPage
        {
            get { return (bool)GetValue(CanGoToNextPageProperty); }
        }

        /// <summary>
        /// Is find function enabled or not
        /// </summary>
        public bool IsFindEnabled
        {
            get { return (bool)GetValue(IsFindEnabledProperty); }
            set { SetValue(IsFindEnabledProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// Is print function enabled or not
        /// </summary>
        public bool IsPrintEnabled
        {
            get { return (bool)GetValue(IsPrintEnabledProperty); }
            set { SetValue(IsPrintEnabledProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// A Property representing a content of this FlowDocumentScrollViewer.
        /// </summary>
        public FlowDocument Document
        {
            get { return (FlowDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
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
        /// <see cref="ViewingMode"/>
        /// </summary>
        public static readonly DependencyProperty ViewingModeProperty =
                DependencyProperty.Register(
                        "ViewingMode",
                        typeof(FlowDocumentReaderViewingMode),
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(
                                FlowDocumentReaderViewingMode.Page,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(ViewingModeChanged)),
                        new ValidateValueCallback(IsValidViewingMode));

        /// <summary>
        /// <see cref="IsPageViewEnabled"/>
        /// </summary>
        public static readonly DependencyProperty IsPageViewEnabledProperty =
                DependencyProperty.Register(
                        "IsPageViewEnabled",
                        typeof(bool),
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.TrueBox,
                                new PropertyChangedCallback(ViewingModeEnabledChanged)));

        /// <summary>
        /// <see cref="IsTwoPageViewEnabled"/>
        /// </summary>
        public static readonly DependencyProperty IsTwoPageViewEnabledProperty =
                DependencyProperty.Register(
                        "IsTwoPageViewEnabled",
                        typeof(bool),
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.TrueBox,
                                new PropertyChangedCallback(ViewingModeEnabledChanged)));

        /// <summary>
        /// <see cref="IsScrollViewEnabled"/>
        /// </summary>
        public static readonly DependencyProperty IsScrollViewEnabledProperty =
                DependencyProperty.Register(
                        "IsScrollViewEnabled",
                        typeof(bool),
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.TrueBox,
                                new PropertyChangedCallback(ViewingModeEnabledChanged)));

        /// <summary>
        /// <see cref="PageCount"/>
        /// </summary>
        private static readonly DependencyPropertyKey PageCountPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "PageCount",
                        typeof(int),
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(0));

        /// <summary>
        /// <see cref="PageCount"/>
        /// </summary>
        public static readonly DependencyProperty PageCountProperty = PageCountPropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="PageNumber"/>
        /// </summary>
        private static readonly DependencyPropertyKey PageNumberPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "PageNumber",
                        typeof(int),
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(0));

        /// <summary>
        /// <see cref="PageNumber"/>
        /// </summary>
        public static readonly DependencyProperty PageNumberProperty = PageNumberPropertyKey.DependencyProperty;
        /// <summary>
        /// <see cref="CanGoToPreviousPage"/>
        /// </summary>
        private static readonly DependencyPropertyKey CanGoToPreviousPagePropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanGoToPreviousPage",
                        typeof(bool),
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// <see cref="CanGoToPreviousPage"/>
        /// </summary>
        public static readonly DependencyProperty CanGoToPreviousPageProperty = CanGoToPreviousPagePropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="CanGoToNextPage"/>
        /// </summary>
        private static readonly DependencyPropertyKey CanGoToNextPagePropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanGoToNextPage",
                        typeof(bool),
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// <see cref="CanGoToNextPage"/>
        /// </summary>
        public static readonly DependencyProperty CanGoToNextPageProperty = CanGoToNextPagePropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="IsFindEnabled"/>
        /// </summary>
        public static readonly DependencyProperty IsFindEnabledProperty =
                DependencyProperty.Register(
                        "IsFindEnabled",
                        typeof(bool),
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.TrueBox,
                                new PropertyChangedCallback(IsFindEnabledChanged)));

        /// <summary>
        /// <see cref="IsPrintEnabled"/>
        /// </summary>
        public static readonly DependencyProperty IsPrintEnabledProperty =
                DependencyProperty.Register(
                        "IsPrintEnabled",
                        typeof(bool),
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.TrueBox,
                                new PropertyChangedCallback(IsPrintEnabledChanged)));

        /// <summary>
        /// <see cref="Document"/>
        /// </summary>
        public static readonly DependencyProperty DocumentProperty =
                DependencyProperty.Register(
                        "Document",
                        typeof(FlowDocument),
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(
                                null,
                                new PropertyChangedCallback(DocumentChanged)));

        /// <summary>
        /// <see cref="Zoom"/>
        /// </summary>
        public static readonly DependencyProperty ZoomProperty =
                FlowDocumentPageViewer.ZoomProperty.AddOwner(
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(
                                100d,
                                new PropertyChangedCallback(ZoomChanged),
                                new CoerceValueCallback(CoerceZoom)));

        /// <summary>
        /// <see cref="MaxZoom"/>
        /// </summary>
        public static readonly DependencyProperty MaxZoomProperty =
                FlowDocumentPageViewer.MaxZoomProperty.AddOwner(
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(
                                200d,
                                new PropertyChangedCallback(MaxZoomChanged),
                                new CoerceValueCallback(CoerceMaxZoom)));

        /// <summary>
        /// <see cref="MinZoom"/>
        /// </summary>
        public static readonly DependencyProperty MinZoomProperty =
                FlowDocumentPageViewer.MinZoomProperty.AddOwner(
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(
                                80d,
                                new PropertyChangedCallback(MinZoomChanged)));

        /// <summary>
        /// <see cref="ZoomIncrement"/>
        /// </summary>
        public static readonly DependencyProperty ZoomIncrementProperty =
                FlowDocumentPageViewer.ZoomIncrementProperty.AddOwner(
                        typeof(FlowDocumentReader));

        /// <summary>
        /// <see cref="CanIncreaseZoom"/>
        /// </summary>
        private static readonly DependencyPropertyKey CanIncreaseZoomPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanIncreaseZoom",
                        typeof(bool),
                        typeof(FlowDocumentReader),
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
                        typeof(FlowDocumentReader),
                        new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// <see cref="CanDecreaseZoom"/>
        /// </summary>
        public static readonly DependencyProperty CanDecreaseZoomProperty = CanDecreaseZoomPropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="TextBoxBase.SelectionBrushProperty"/>
        /// </summary>
        public static readonly DependencyProperty SelectionBrushProperty =
            TextBoxBase.SelectionBrushProperty.AddOwner(typeof(FlowDocumentReader));

        /// <summary>
        /// <see cref="TextBoxBase.SelectionOpacityProperty"/>
        /// </summary>
        public static readonly DependencyProperty SelectionOpacityProperty =
            TextBoxBase.SelectionOpacityProperty.AddOwner(typeof(FlowDocumentReader));

        /// <summary>
        /// <see cref="TextBoxBase.IsSelectionActiveProperty"/>
        /// </summary>
        public static readonly DependencyProperty IsSelectionActiveProperty =
            TextBoxBase.IsSelectionActiveProperty.AddOwner(typeof(FlowDocumentReader));

        /// <summary>
        /// <see cref="TextBoxBase.IsInactiveSelectionHighlightEnabledProperty"/>
        /// </summary>
        public static readonly DependencyProperty IsInactiveSelectionHighlightEnabledProperty =
            TextBoxBase.IsInactiveSelectionHighlightEnabledProperty.AddOwner(typeof(FlowDocumentReader));

        #endregion Public Dynamic Properties

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Public Commands
        //
        //-------------------------------------------------------------------

        #region Public Commands

        /// <summary>
        /// Switch ViewingMode command
        /// </summary>
        public static readonly RoutedUICommand SwitchViewingModeCommand = new RoutedUICommand(Switch_ViewingMode, "SwitchViewingMode", typeof(FlowDocumentReader), null);

        #endregion

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
            if (_printInProgress)
            {
                _printInProgress = false;
                // Since _printInProgress value is used to determine CanExecute state, we must invalidate that state.
                CommandManager.InvalidateRequerySuggested();
            }
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
            if (CurrentViewer != null)
            {
                CurrentViewer.Print();
            }
        }

        /// <summary>
        /// Handler for the CancelPrint command.
        /// </summary>
        protected virtual void OnCancelPrintCommand()
        {
            if (CurrentViewer != null)
            {
                CurrentViewer.CancelPrint();
            }
        }

        /// <summary>
        /// Handler for the IncreaseZoom command.
        /// </summary>
        protected virtual void OnIncreaseZoomCommand()
        {
            // If can zoom in, increase zoom by the zoom increment value.
            if (CanIncreaseZoom)
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
            if (CanDecreaseZoom)
            {
                SetCurrentValueInternal(ZoomProperty, Math.Max(Zoom - ZoomIncrement, MinZoom));
            }
        }

        /// <summary>
        /// Handler for the SwitchViewingMode command.
        /// </summary>
        /// <param name="viewingMode">Viewing mode.</param>
        protected virtual void OnSwitchViewingModeCommand(FlowDocumentReaderViewingMode viewingMode)
        {
            SwitchViewingModeCore(viewingMode);
        }

        /// <summary>
        /// Called when IsInitialized is set to true.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // Defer the Is*ViewEnabled & ViewingMode conflict exception.
            // Otherwise <FlowDocumentReader IsPageViewEnabled="false" ViewingMode="TwoPage"/> won't work.
            if (IsInitialized && !CanSwitchToViewingMode(ViewingMode))
            {
                throw new ArgumentException(SR.Get(SRID.FlowDocumentReaderViewingModeEnabledConflict));
            }
        }

        protected override void OnDpiChanged(DpiScale oldDpiScaleInfo, DpiScale newDpiScaleInfo)
        {
            Document?.SetDpi(newDpiScaleInfo);
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new FlowDocumentReaderAutomationPeer(this);
        }

        /// <summary>
        /// An event reporting that the IsKeyboardFocusWithin property changed.
        /// </summary>
        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            // In order to enable selection rendering and other similar services, the embedded viewer
            // needs to get focus, when any part of the control gets focused.
            // But if the focus is within the Document, do not change it. Otherwise it
            // will interfere with input handling inside the Document.
            if (IsKeyboardFocusWithin && CurrentViewer != null)
            {
                bool isFocusWithinDocument = IsFocusWithinDocument();
                if (!isFocusWithinDocument)
                {
                    ((FrameworkElement)CurrentViewer).Focus();
                }
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
            // Do not add intermediate ContentElements to the route,
            // because they were added by embedded viewer.
            return BuildRouteCoreHelper(route, args, false);
        }

        internal override bool InvalidateAutomationAncestorsCore(Stack<DependencyObject> branchNodeStack, out bool continuePastCoreTree)
        {
            bool shouldInvalidateIntermediateElements = false;
            return InvalidateAutomationAncestorsCoreHelper(branchNodeStack, out continuePastCoreTree, shouldInvalidateIntermediateElements);
        }
        
        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Handler for the SwitchViewingMode command.
        /// </summary>
        /// <param name="viewingMode">Viewing mode.</param>
        protected virtual void SwitchViewingModeCore(FlowDocumentReaderViewingMode viewingMode)
        {
            ITextSelection textSelection = null;
            ContentPosition contentPosition = null;
            IFlowDocumentViewer viewer;
            FrameworkElement feViewer;
            bool isKeyboardFocusWithin;
            DependencyObject focusedElement = null;

            if (_contentHost != null)
            {
                // Remember the current keyboard focus state.
                isKeyboardFocusWithin = IsKeyboardFocusWithin;

                // Detach old viewer
                viewer = _contentHost.Child as IFlowDocumentViewer;
                if (viewer != null)
                {
                    // Remember focused element, if the focus is within the Document.
                    // After switching to a different viewer, this focus needs to be restored.
                    if (isKeyboardFocusWithin)
                    {
                        bool isFocusWithinDocument = IsFocusWithinDocument();
                        if (isFocusWithinDocument)
                        {
                            focusedElement = Keyboard.FocusedElement as DependencyObject;
                        }
                    }

                    // Retrieve the current viewing state from the old viewer.
                    textSelection = viewer.TextSelection;
                    contentPosition = viewer.ContentPosition;

                    // Detach old viewer
                    DetachViewer(viewer);
                }

                viewer = GetViewerFromMode(viewingMode);
                feViewer = (FrameworkElement)viewer;
                if (viewer != null)
                {
                    // Attach new viewer
                    _contentHost.Child = feViewer;
                    AttachViewer(viewer);

                    // Restore viewing state.
                    viewer.TextSelection = textSelection;
                    viewer.ContentPosition = contentPosition;

                    // Bring the focus to previously focused element within the document
                    // or to the current viewer.
                    if (isKeyboardFocusWithin)
                    {
                        if (focusedElement is UIElement)
                        {
                            ((UIElement)focusedElement).Focus();
                        }
                        else if (focusedElement is ContentElement)
                        {
                            ((ContentElement)focusedElement).Focus();
                        }
                        else
                        {
                            feViewer.Focus();
                        }
                    }
                }

                // Viewer changes invalidates following properties:
                //      - PageCount
                //      - PageNumber
                //      - CanGoToPreviousPage
                //      - CanGoToNextPage
                UpdateReadOnlyProperties(true, true);
            }
        }

        /// <summary>
        /// Determines whether focus is within Document.
        /// </summary>
        private bool IsFocusWithinDocument()
        {
            DependencyObject focusedElement = Keyboard.FocusedElement as DependencyObject;
            while (focusedElement != null && focusedElement != Document)
            {
                // Skip elements in the control's template (if such exists) and
                // walk up logical tree to find if the focused element is within
                // the document.
                FrameworkElement fe = focusedElement as FrameworkElement;
                if (fe != null && fe.TemplatedParent != null)
                {
                    focusedElement = fe.TemplatedParent;
                }
                else
                {
                    focusedElement = LogicalTreeHelper.GetParent(focusedElement);
                }
            }
            return (focusedElement != null);
        }

        /// <summary>
        /// The Document has changed and needs to be updated.
        /// </summary>
        private void DocumentChanged(FlowDocument oldDocument, FlowDocument newDocument)
        {
            // Cleanup state associated with the old document.
            if (oldDocument != null)
            {
                // If Document was added to logical tree before, remove it.
                if (_documentAsLogicalChild)
                {
                    RemoveLogicalChild(oldDocument);
                }
            }

            // If FlowDocumentReader was created through style, then do not modify
            // the logical tree. Instead, set "core parent" for the Document.
            if (TemplatedParent != null && newDocument != null && LogicalTreeHelper.GetParent(newDocument) != null)
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
                newDocument.SetDpi(this.GetDpi());
                // If Document should be part of DocumentViewer's logical tree, add it.
                if (_documentAsLogicalChild)
                {
                    AddLogicalChild(newDocument);
                }
            }

            // Attach document to the current viewer.
            if (CurrentViewer != null)
            {
                CurrentViewer.SetDocument(newDocument);
            }

            // Document invalidation invalidates following properties:
            //      - PageCount
            //      - PageNumber
            //      - CanGoToPreviousPage
            //      - CanGoToNextPage
            UpdateReadOnlyProperties(true, true);

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
            FlowDocumentReaderAutomationPeer peer = UIElementAutomationPeer.FromElement(this) as FlowDocumentReaderAutomationPeer;
            if (peer != null)
            {
                peer.InvalidatePeer();
            }
        }

        /// <summary>
        /// Detach embedded viewer form the reader control.
        /// </summary>
        private void DetachViewer(IFlowDocumentViewer viewer)
        {
            Invariant.Assert(viewer != null && viewer is FrameworkElement);
            FrameworkElement feViewer = (FrameworkElement)viewer;
            // Clear property bindings.
            BindingOperations.ClearBinding(feViewer, ZoomProperty);
            BindingOperations.ClearBinding(feViewer, MaxZoomProperty);
            BindingOperations.ClearBinding(feViewer, MinZoomProperty);
            BindingOperations.ClearBinding(feViewer, ZoomIncrementProperty);
            // Unregister event handlers.
            viewer.PageCountChanged -= new EventHandler(OnPageCountChanged);
            viewer.PageNumberChanged -= new EventHandler(OnPageNumberChanged);
            viewer.PrintStarted -= new EventHandler(OnViewerPrintStarted);
            viewer.PrintCompleted -= new EventHandler(OnViewerPrintCompleted);
            // Clear TemplatedParent
            //feViewer._templatedParent = null;
            // Detach document
            viewer.SetDocument(null);
        }

        /// <summary>
        /// Attach embedded viewer to the reader control.
        /// </summary>
        private void AttachViewer(IFlowDocumentViewer viewer)
        {
            Invariant.Assert(viewer != null && viewer is FrameworkElement);
            FrameworkElement feViewer = (FrameworkElement)viewer;
            // Set document
            viewer.SetDocument(Document);
            // Set TemplatedParent
            //feViewer._templatedParent = TemplatedParent;
            // Register event handlers.
            viewer.PageCountChanged += new EventHandler(OnPageCountChanged);
            viewer.PageNumberChanged += new EventHandler(OnPageNumberChanged);
            viewer.PrintStarted += new EventHandler(OnViewerPrintStarted);
            viewer.PrintCompleted += new EventHandler(OnViewerPrintCompleted);
            // Create property bindings.
            CreateTwoWayBinding(feViewer, ZoomProperty, "Zoom");
            CreateTwoWayBinding(feViewer, MaxZoomProperty, "MaxZoom");
            CreateTwoWayBinding(feViewer, MinZoomProperty, "MinZoom");
            CreateTwoWayBinding(feViewer, ZoomIncrementProperty, "ZoomIncrement");
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

        /// <summary>
        /// Determines whether can switch to specified ViewingMode or not.
        /// </summary>
        private bool CanSwitchToViewingMode(FlowDocumentReaderViewingMode mode)
        {
            bool canSwitch = false;
            switch (mode)
            {
                case FlowDocumentReaderViewingMode.Page:
                    canSwitch = IsPageViewEnabled;
                    break;
                case FlowDocumentReaderViewingMode.TwoPage:
                    canSwitch = IsTwoPageViewEnabled;
                    break;
                case FlowDocumentReaderViewingMode.Scroll:
                    canSwitch = IsScrollViewEnabled;
                    break;
            }
            return canSwitch;
        }

        /// <summary>
        /// Retrieves viewer form specified ViewingMode.
        /// </summary>
        private IFlowDocumentViewer GetViewerFromMode(FlowDocumentReaderViewingMode mode)
        {
            IFlowDocumentViewer viewer = null;
            switch (mode)
            {
                case FlowDocumentReaderViewingMode.Page:
                    if (_pageViewer == null)
                    {
                        _pageViewer = new ReaderPageViewer();
                        _pageViewer.SetResourceReference(StyleProperty, PageViewStyleKey);
                        _pageViewer.Name = "PageViewer";
                        CommandManager.AddPreviewCanExecuteHandler(_pageViewer, new CanExecuteRoutedEventHandler(PreviewCanExecuteRoutedEventHandler));
                    }
                    viewer = _pageViewer;
                    break;
                case FlowDocumentReaderViewingMode.TwoPage:
                    if (_twoPageViewer == null)
                    {
                        _twoPageViewer = new ReaderTwoPageViewer();
                        _twoPageViewer.SetResourceReference(StyleProperty, TwoPageViewStyleKey);
                        _twoPageViewer.Name = "TwoPageViewer";
                        CommandManager.AddPreviewCanExecuteHandler(_twoPageViewer, new CanExecuteRoutedEventHandler(PreviewCanExecuteRoutedEventHandler));
                    }
                    viewer = _twoPageViewer;
                    break;
                case FlowDocumentReaderViewingMode.Scroll:
                    if (_scrollViewer == null)
                    {
                        _scrollViewer = new ReaderScrollViewer();
                        _scrollViewer.SetResourceReference(StyleProperty, ScrollViewStyleKey);
                        _scrollViewer.Name = "ScrollViewer";
                        CommandManager.AddPreviewCanExecuteHandler(_scrollViewer, new CanExecuteRoutedEventHandler(PreviewCanExecuteRoutedEventHandler));
                    }
                    viewer = _scrollViewer;
                    break;
            }
            return viewer;
        }

        /// <summary>
        /// Update values for readonly properties.
        /// </summary>
        /// <param name="pageCountChanged">Whether PageCount has been changed.</param>
        /// <param name="pageNumberChanged">Whether PageNumber has been changed.</param>
        private void UpdateReadOnlyProperties(bool pageCountChanged, bool pageNumberChanged)
        {
            if (pageCountChanged)
            {
                SetValue(PageCountPropertyKey, (CurrentViewer != null) ? CurrentViewer.PageCount : 0);
            }

            if (pageNumberChanged)
            {
                SetValue(PageNumberPropertyKey, (CurrentViewer != null) ? CurrentViewer.PageNumber : 0);
                SetValue(CanGoToPreviousPagePropertyKey, (CurrentViewer != null) ? CurrentViewer.CanGoToPreviousPage : false);
            }

            if (pageCountChanged || pageNumberChanged)
            {
                SetValue(CanGoToNextPagePropertyKey, (CurrentViewer != null) ? CurrentViewer.CanGoToNextPage : false);
            }
        }

        /// <summary>
        /// Event handler for IFlowDocumentViewer.PageCountChanged.
        /// </summary>
        private void OnPageCountChanged(object sender, EventArgs e)
        {
            Invariant.Assert(CurrentViewer != null && sender == CurrentViewer);
            UpdateReadOnlyProperties(true, false);
        }

        /// <summary>
        /// Event handler for IFlowDocumentViewer.PageNumberChanged.
        /// </summary>
        private void OnPageNumberChanged(object sender, EventArgs e)
        {
            Invariant.Assert(CurrentViewer != null && sender == CurrentViewer);
            UpdateReadOnlyProperties(false, true);
        }

        /// <summary>
        /// Event handler for IFlowDocumentViewer.PrintStarted.
        /// </summary>
        private void OnViewerPrintStarted(object sender, EventArgs e)
        {
            Invariant.Assert(CurrentViewer != null && sender == CurrentViewer);
            _printInProgress = true;
            // Since _printInProgress value is used to determine CanExecute state, we must invalidate that state.
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Event handler for IFlowDocumentViewer.PrintCompleted.
        /// </summary>
        private void OnViewerPrintCompleted(object sender, EventArgs e)
        {
            Invariant.Assert(CurrentViewer != null && sender == CurrentViewer);
            OnPrintCompleted();
        }

        /// <summary>
        /// Convert object value to FlowDocumentReaderViewingMode.
        /// </summary>
        private bool ConvertToViewingMode(object value, out FlowDocumentReaderViewingMode mode)
        {
            bool success;
            if (value is FlowDocumentReaderViewingMode)
            {
                mode = (FlowDocumentReaderViewingMode)value;
                success = true;
            }
            else if (value is String)
            {
                String str = (String)value;
                if (str == FlowDocumentReaderViewingMode.Page.ToString())
                {
                    mode = FlowDocumentReaderViewingMode.Page;
                    success = true;
                }
                else if (str == FlowDocumentReaderViewingMode.TwoPage.ToString())
                {
                    mode = FlowDocumentReaderViewingMode.TwoPage;
                    success = true;
                }
                else if (str == FlowDocumentReaderViewingMode.Scroll.ToString())
                {
                    mode = FlowDocumentReaderViewingMode.Scroll;
                    success = true;
                }
                else
                {
                    mode = FlowDocumentReaderViewingMode.Page;
                    success = false;
                }
            }
            else
            {
                mode = FlowDocumentReaderViewingMode.Page;
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Enables/disables the FindToolBar.
        /// </summary>
        /// <param name="enable">Whether to enable/disable FindToolBar.</param>
        private void ToggleFindToolBar(bool enable)
        {
            Invariant.Assert(enable == (FindToolBar == null));

            // Command event for toggle button is only fired in OnClick - Therefore we just need to change the state
            if(_findButton != null && _findButton.IsChecked.HasValue && _findButton.IsChecked.Value != enable)
            {
                _findButton.IsChecked = enable;
            }
            DocumentViewerHelper.ToggleFindToolBar(_findToolBarHost, new EventHandler(OnFindInvoked), enable);
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

            // Command: SwitchViewingMode
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentReader), FlowDocumentReader.SwitchViewingModeCommand,
                executedHandler, canExecuteHandler, KeyGesture.CreateFromResourceStrings(KeySwitchViewingMode, SRID.KeySwitchViewingModeDisplayString));

            // Command: ApplicationCommands.Find
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentReader), ApplicationCommands.Find,
                executedHandler, canExecuteHandler);

            // Command: ApplicationCommands.Print
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentReader), ApplicationCommands.Print,
                executedHandler, canExecuteHandler);

            // Command: ApplicationCommands.CancelPrint
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentReader), ApplicationCommands.CancelPrint,
                executedHandler, canExecuteHandler);

            // Command: NavigationCommands.PreviousPage
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentReader), NavigationCommands.PreviousPage,
                executedHandler, canExecuteHandler);

            // Command: NavigationCommands.NextPage
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentReader), NavigationCommands.NextPage,
                executedHandler, canExecuteHandler);

            // Command: NavigationCommands.FirstPage
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentReader), NavigationCommands.FirstPage,
                executedHandler, canExecuteHandler);

            // Command: NavigationCommands.LastPage
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentReader), NavigationCommands.LastPage,
                executedHandler, canExecuteHandler);

            // Command: NavigationCommands.IncreaseZoom
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentReader), NavigationCommands.IncreaseZoom,
                executedHandler, canExecuteHandler, new KeyGesture(Key.OemPlus, ModifierKeys.Control));

            // Command: NavigationCommands.DecreaseZoom
            CommandHelpers.RegisterCommandHandler(typeof(FlowDocumentReader), NavigationCommands.DecreaseZoom,
                executedHandler, canExecuteHandler, new KeyGesture(Key.OemMinus, ModifierKeys.Control));
        }

        /// <summary>
        /// Central handler for CanExecute events fired by Commands directed at FlowDocumentReader.
        /// </summary>
        /// <param name="target">The target of this Command, expected to be FlowDocumentReader</param>
        /// <param name="args">The event arguments for this event.</param>
        private static void CanExecuteRoutedEventHandler(object target, CanExecuteRoutedEventArgs args)
        {
            FlowDocumentReader viewer = target as FlowDocumentReader;
            Invariant.Assert(viewer != null, "Target of CanExecuteRoutedEventHandler must be FlowDocumentReader.");
            Invariant.Assert(args != null, "args cannot be null.");

            // FlowDocumentReader is capable of execution of the majority of its commands.
            // Special rules:
            // a) during printing only CancelPrint is enabled.
            // b) Find command is enabled only when FindToolBar is enabled.
            // c) Print command is enabled when Document is attached and printing is enabled.
            // d) CancelPrint command is enabled only during printing.
            // e) SwitchViewingMode command is enabled if the viewing mode is enabled, or
            //    the command has no parameters (will switch to the next available view).
            if (!viewer._printInProgress)
            {
                if (args.Command == FlowDocumentReader.SwitchViewingModeCommand)
                {
                    // This command is enabled if the viewing mode is enabled, or the command
                    // has no parameters (will switch to the next available view).
                    FlowDocumentReaderViewingMode mode;
                    if (viewer.ConvertToViewingMode(args.Parameter, out mode))
                    {
                        args.CanExecute = viewer.CanSwitchToViewingMode(mode);
                    }
                    else
                    {
                        args.CanExecute = (args.Parameter == null);
                    }
                }
                else if (args.Command == ApplicationCommands.Find)
                {
                    args.CanExecute = viewer.CanShowFindToolBar;
                }
                else if (args.Command == ApplicationCommands.Print)
                {
                    args.CanExecute = (viewer.Document != null) && viewer.IsPrintEnabled;
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
        /// Central handler for all ExecuteEvents fired by Commands directed at FlowDocumentReader.
        /// </summary>
        /// <param name="target">The target of this Command, expected to be FlowDocumentReader.</param>
        /// <param name="args">The event arguments associated with this event.</param>
        private static void ExecutedRoutedEventHandler(object target, ExecutedRoutedEventArgs args)
        {
            FlowDocumentReader viewer = target as FlowDocumentReader;
            Invariant.Assert(viewer != null, "Target of ExecutedRoutedEventHandler must be FlowDocumentReader.");
            Invariant.Assert(args != null, "args cannot be null.");

            if (args.Command == FlowDocumentReader.SwitchViewingModeCommand)
            {
                viewer.TrySwitchViewingMode(args.Parameter);
            }
            else if (args.Command == ApplicationCommands.Find)
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
            else if (args.Command == NavigationCommands.PreviousPage)
            {
                viewer.OnPreviousPageCommand();
            }
            else if (args.Command == NavigationCommands.NextPage)
            {
                viewer.OnNextPageCommand();
            }
            else if (args.Command == NavigationCommands.FirstPage)
            {
                viewer.OnFirstPageCommand();
            }
            else if (args.Command == NavigationCommands.LastPage)
            {
                viewer.OnLastPageCommand();
            }
            else
            {
                Invariant.Assert(false, "Command not handled in ExecutedRoutedEventHandler.");
            }
        }

        /// <summary>
        /// Changes the current viewing mode.
        /// </summary>
        private void TrySwitchViewingMode(object parameter)
        {
            FlowDocumentReaderViewingMode mode;
            // Convert command parameter to viewing mode value.
            // If parameter is not provided, the viewing mode is the next one available.
            // If parameter cannot be converted, the command is ignored.
            if (!ConvertToViewingMode(parameter, out mode))
            {
                if (parameter == null)
                {
                    mode = (FlowDocumentReaderViewingMode)((((int)ViewingMode) + 1) % 3);
                }
                else
                {
                    return;
                }
            }
            // If the current ViewingMode is disabled, go to next one.
            while (!CanSwitchToViewingMode(mode))
            {
                mode = (FlowDocumentReaderViewingMode)((((int)mode) + 1) % 3);
            }
            // Set new ViewingMode value.
            SetCurrentValueInternal(ViewingModeProperty, mode);
        }

        /// <summary>
        /// Handler for the PreviousPage command.
        /// </summary>
        private void OnPreviousPageCommand()
        {
            if (CurrentViewer != null)
            {
                CurrentViewer.PreviousPage();
            }
        }

        /// <summary>
        /// Handler for the NextPage command.
        /// </summary>
        private void OnNextPageCommand()
        {
            if (CurrentViewer != null)
            {
                CurrentViewer.NextPage();
            }
        }

        /// <summary>
        /// Handler for the FirstPage command.
        /// </summary>
        private void OnFirstPageCommand()
        {
            if (CurrentViewer != null)
            {
                CurrentViewer.FirstPage();
            }
        }

        /// <summary>
        /// Handler for the LastPage command.
        /// </summary>
        private void OnLastPageCommand()
        {
            if (CurrentViewer != null)
            {
                CurrentViewer.LastPage();
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
            TextEditor textEditor = TextEditor;
            FindToolBar findToolBar = FindToolBar;

            if (findToolBar != null && textEditor != null)
            {
                // In order to show current text selection TextEditor requires Focus to be set on the UIScope.
                // If there embedded controls, it may happen that embedded control currently has focus and find
                // was invoked through hotkeys. To support this case we manually move focus to the appropriate element.
                if (CurrentViewer != null && CurrentViewer is UIElement)
                {
                    ((UIElement)CurrentViewer).Focus();
                }

                findResult = DocumentViewerHelper.Find(findToolBar, textEditor, textEditor.TextView, textEditor.TextView);

                // If we found something, bring it into the view. Otherwise alert the user.
                if ((findResult != null) && (!findResult.IsEmpty))
                {
                    // Bring find result into view.
                    if (CurrentViewer != null)
                    {
                        CurrentViewer.ShowFindResult(findResult);
                    }
                }
                else
                {
                    DocumentViewerHelper.ShowFindUnsuccessfulMessage(findToolBar);
                }
            }
        }

        /// <summary>
        /// Disable commands on IFlowDocumentViewer when this funcionality is explicitly
        /// disabled on the reader control.
        /// </summary>
        private void PreviewCanExecuteRoutedEventHandler(object target, CanExecuteRoutedEventArgs args)
        {
            if (args.Command == ApplicationCommands.Find)
            {
                // Find is handled by FlowDocumentReader.
                args.CanExecute = false;
                args.Handled = true;
            }
            else if (args.Command == ApplicationCommands.Print)
            {
                args.CanExecute = IsPrintEnabled;
                args.Handled = !IsPrintEnabled;
            }
        }

        /// <summary>
        /// Called when a key event occurs.
        /// </summary>
        private static void KeyDownHandler(object sender, KeyEventArgs e)
        {
            DocumentViewerHelper.KeyDownHelper(e, ((FlowDocumentReader)sender)._findToolBarHost);
        }

        #endregion Commands

        #region Static Methods

        /// <summary>
        /// ViewingMode has been changed.
        /// </summary>
        private static void ViewingModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentReader);
            FlowDocumentReader viewer = (FlowDocumentReader)d;
            if (viewer.CanSwitchToViewingMode((FlowDocumentReaderViewingMode)e.NewValue))
            {
                viewer.SwitchViewingModeCore((FlowDocumentReaderViewingMode)e.NewValue);
            }
            else if (viewer.IsInitialized)
            {
                throw new ArgumentException(SR.Get(SRID.FlowDocumentReaderViewingModeEnabledConflict));
            }

            // Fire automation events if automation is active.
            FlowDocumentReaderAutomationPeer peer = UIElementAutomationPeer.FromElement(viewer) as FlowDocumentReaderAutomationPeer;
            if (peer != null)
            {
                peer.RaiseCurrentViewChangedEvent((FlowDocumentReaderViewingMode)e.NewValue, (FlowDocumentReaderViewingMode)e.OldValue);
            }
        }

        /// <summary>
        /// Validate value of ViewingMode property.
        /// </summary>
        private static bool IsValidViewingMode(object o)
        {
            FlowDocumentReaderViewingMode value = (FlowDocumentReaderViewingMode)o;
            return (value == FlowDocumentReaderViewingMode.Page ||
                    value == FlowDocumentReaderViewingMode.TwoPage ||
                    value == FlowDocumentReaderViewingMode.Scroll);
        }

        /// <summary>
        /// One of viewing modes has been enabled/disabled.
        /// </summary>
        private static void ViewingModeEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentReader);
            FlowDocumentReader viewer = (FlowDocumentReader)d;

            // Cannot disable all viewing modes.
            if (!viewer.IsPageViewEnabled &&
                !viewer.IsTwoPageViewEnabled &&
                !viewer.IsScrollViewEnabled)
            {
                throw new ArgumentException(SR.Get(SRID.FlowDocumentReaderCannotDisableAllViewingModes));
            }

            // Cannot disable the current viewing mode.
            if (viewer.IsInitialized && !viewer.CanSwitchToViewingMode(viewer.ViewingMode))
            {
                throw new ArgumentException(SR.Get(SRID.FlowDocumentReaderViewingModeEnabledConflict));
            }

            // Fire automation events if automation is active.
            FlowDocumentReaderAutomationPeer peer = UIElementAutomationPeer.FromElement(viewer) as FlowDocumentReaderAutomationPeer;
            if (peer != null)
            {
                peer.RaiseSupportedViewsChangedEvent(e);
            }
        }

        /// <summary>
        /// IsFindEnabled value has changed.
        /// </summary>
        private static void IsFindEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentReader);
            FlowDocumentReader viewer = (FlowDocumentReader)d;

            // Update the toolbar with our current state.
            if (!viewer.CanShowFindToolBar)
            {
                if (viewer.FindToolBar != null)
                {
                    viewer.ToggleFindToolBar(false);
                }
            }

            // Since IsFindEnabled state is used to determine CanExecute state, we must invalidate that state.
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// IsPrintEnabled value has changed.
        /// </summary>
        private static void IsPrintEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentReader);
            FlowDocumentReader viewer = (FlowDocumentReader)d;

            // Since IsPrintEnabled state is used to determine CanExecute state, we must invalidate that state.
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// The Document has changed and needs to be updated.
        /// </summary>
        private static void DocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentReader);
            FlowDocumentReader viewer = (FlowDocumentReader)d;
            viewer.DocumentChanged((FlowDocument)e.OldValue, (FlowDocument)e.NewValue);

            // Since Document state is used to determine CanExecute state, we must invalidate that state.
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// The Zoom has changed and needs to be updated.
        /// </summary>
        private static void ZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentReader);
            FlowDocumentReader viewer = (FlowDocumentReader)d;
            if (!DoubleUtil.AreClose((double)e.OldValue, (double)e.NewValue))
            {
                // If zoom has been changed, CanIncrease/DecreaseZoom property need to be updated.
                viewer.SetValue(CanIncreaseZoomPropertyKey, BooleanBoxes.Box(DoubleUtil.GreaterThan(viewer.MaxZoom, viewer.Zoom)));
                viewer.SetValue(CanDecreaseZoomPropertyKey, BooleanBoxes.Box(DoubleUtil.LessThan(viewer.MinZoom, viewer.Zoom)));
            }
        }

        /// <summary>
        /// Coerce Zoom with Max/MinZoom, MinZoom works as the baseline.
        /// </summary>
        private static object CoerceZoom(DependencyObject d, object value)
        {
            Invariant.Assert(d != null && d is FlowDocumentReader);
            FlowDocumentReader viewer = (FlowDocumentReader)d;

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
            Invariant.Assert(d != null && d is FlowDocumentReader);
            FlowDocumentReader viewer = (FlowDocumentReader)d;

            viewer.CoerceValue(ZoomProperty);
            viewer.SetValue(CanIncreaseZoomPropertyKey, BooleanBoxes.Box(DoubleUtil.GreaterThan(viewer.MaxZoom, viewer.Zoom)));
        }

        /// <summary>
        /// MaxZoom need to be coerced if MinZoom > MaxZoom
        /// </summary>
        private static object CoerceMaxZoom(DependencyObject d, object value)
        {
            Invariant.Assert(d != null && d is FlowDocumentReader);
            FlowDocumentReader viewer = (FlowDocumentReader)d;

            double min = viewer.MinZoom;
            return ((double)value < min) ? min : value;
        }

        /// <summary>
        /// The MinZoom has changed and needs to be updated.
        /// </summary>
        private static void MinZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Invariant.Assert(d != null && d is FlowDocumentReader);
            FlowDocumentReader viewer = (FlowDocumentReader)d;

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
        /// PropertyChanged callback for a property that affects the selection or caret rendering.
        /// </summary>
        private static void UpdateCaretElement(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FlowDocumentReader reader = (FlowDocumentReader)d;

            if (reader.Selection != null)
            {
                CaretElement caretElement = reader.Selection.CaretElement;
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
        /// Whether FindToolBar can be enabled.
        /// </summary>
        private bool CanShowFindToolBar
        {
            get { return ((_findToolBarHost != null) && IsFindEnabled && (Document != null)); }
        }

        /// <summary>
        /// Returns TextEditor, if availabe.
        /// </summary>
        private TextEditor TextEditor
        {
            get
            {
                TextEditor textEditor = null;
                IFlowDocumentViewer currentViewer = CurrentViewer;
                if (currentViewer != null && currentViewer.TextSelection != null)
                {
                    textEditor = currentViewer.TextSelection.TextEditor;
                }
                return textEditor;
            }
        }

        /// <summary>
        /// Returns FindToolBar, if enabled.
        /// </summary>
        private FindToolBar FindToolBar
        {
            get { return (_findToolBarHost != null) ? _findToolBarHost.Child as FindToolBar : null; }
        }

        /// <summary>
        /// Returns the current content viewer.
        /// </summary>
        private IFlowDocumentViewer CurrentViewer
        {
            get
            {
                if (_contentHost != null)
                {
                    return (IFlowDocumentViewer)_contentHost.Child;
                }
                return null;
            }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private Decorator _contentHost;                 // Host for content viewer
        private Decorator _findToolBarHost;             // Host for FindToolBar
        private ToggleButton _findButton;               // Find toggle button
        private ReaderPageViewer _pageViewer;           // Viewer for Page viewing mode
        private ReaderTwoPageViewer _twoPageViewer;     // Viewer for TwoPage viewing mode
        private ReaderScrollViewer _scrollViewer;       // Viewer for Scroll viewing mode
        private bool _documentAsLogicalChild;           // Is Document part of logical tree
        private bool _printInProgress;                  // Whether print is currently in progress.

        private const string _contentHostTemplateName = "PART_ContentHost";         // Name for ContentHost
        private const string _findToolBarHostTemplateName = "PART_FindToolBarHost"; // Name for the Find ToolBar host
        private const string _findButtonTemplateName = "FindButton"; // Name for the Find Button

        private const string KeySwitchViewingMode = "Ctrl+M";
        private const string Switch_ViewingMode =  "_Switch ViewingMode";

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
                throw new ArgumentException(SR.Get(SRID.FlowDocumentReaderCanHaveOnlyOneChild));
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
        //  IJournalState Members
        //
        //-------------------------------------------------------------------

        #region IJournalState Members

        [Serializable]
        private class JournalState : CustomJournalStateInternal
        {
            public JournalState(int contentPosition, LogicalDirection contentPositionDirection, double zoom, FlowDocumentReaderViewingMode viewingMode)
            {
                ContentPosition = contentPosition;
                ContentPositionDirection = contentPositionDirection;
                Zoom = zoom;
                ViewingMode = viewingMode;
            }
            public int ContentPosition;
            public LogicalDirection ContentPositionDirection;
            public double Zoom;
            public FlowDocumentReaderViewingMode ViewingMode;
        }

        /// <summary>
        /// <see cref="IJournalState.GetJournalState"/>
        /// </summary>
        CustomJournalStateInternal IJournalState.GetJournalState(JournalReason journalReason)
        {
            int cp = -1;
            LogicalDirection cpDirection = LogicalDirection.Forward;
            IFlowDocumentViewer viewer = CurrentViewer;
            if (viewer != null)
            {
                TextPointer contentPosition = viewer.ContentPosition as TextPointer;
                if (contentPosition != null)
                {
                    cp = contentPosition.Offset;
                    cpDirection = contentPosition.LogicalDirection;
                }
            }
            return new JournalState(cp, cpDirection, Zoom, ViewingMode);
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
                SetCurrentValueInternal(ViewingModeProperty, viewerState.ViewingMode);
                if (viewerState.ContentPosition != -1)
                {
                    IFlowDocumentViewer viewer = CurrentViewer;
                    FlowDocument document = Document;
                    if (viewer != null && document != null)
                    {
                        TextContainer textContainer = document.StructuralCache.TextContainer;
                        if (viewerState.ContentPosition <= textContainer.SymbolCount)
                        {
                            TextPointer contentPosition = textContainer.CreatePointerAtOffset(viewerState.ContentPosition, viewerState.ContentPositionDirection);
                            viewer.ContentPosition = contentPosition;
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

        //-------------------------------------------------------------------
        //
        //  Style Keys
        //
        //-------------------------------------------------------------------

        #region Style Keys

        /// <summary>
        /// Key used to mark the style for use by the PageView
        /// </summary>
        private static ResourceKey PageViewStyleKey
        {
            get
            {
                if (_pageViewStyleKey == null)
                {
                    _pageViewStyleKey = new ComponentResourceKey(typeof(PresentationUIStyleResources), "PUIPageViewStyleKey");
                }

                return _pageViewStyleKey;
            }
        }

        /// <summary>
        /// Key used to mark the style for use by the TwoPageView
        /// </summary>
        private static ResourceKey TwoPageViewStyleKey
        {
            get
            {
                if (_twoPageViewStyleKey == null)
                {
                    _twoPageViewStyleKey = new ComponentResourceKey(typeof(PresentationUIStyleResources), "PUITwoPageViewStyleKey");
                }

                return _twoPageViewStyleKey;
            }
        }

        /// <summary>
        /// Key used to mark the style for use by the ScrollView
        /// </summary>
        private static ResourceKey ScrollViewStyleKey
        {
            get
            {
                if (_scrollViewStyleKey == null)
                {
                    _scrollViewStyleKey = new ComponentResourceKey(typeof(PresentationUIStyleResources), "PUIScrollViewStyleKey");
                }

                return _scrollViewStyleKey;
            }
        }


        private static ComponentResourceKey _pageViewStyleKey;
        private static ComponentResourceKey _twoPageViewStyleKey;
        private static ComponentResourceKey _scrollViewStyleKey;

        #endregion
    }

    /// <summary>
    /// </summary>
    public enum FlowDocumentReaderViewingMode
    {
        /// <summary>
        /// </summary>
        Page,

        /// <summary>
        /// </summary>
        TwoPage,

        /// <summary>
        /// </summary>
        Scroll
    }
}

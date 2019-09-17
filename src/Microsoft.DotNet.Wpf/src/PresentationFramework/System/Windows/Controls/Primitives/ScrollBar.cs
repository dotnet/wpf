// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Markup;

using MS.Internal;
using MS.Internal.Commands;
using MS.Internal.KnownBoxes;
using MS.Internal.Telemetry.PresentationFramework;
using MS.Utility;
using MS.Internal.Utility;
using MS.Win32;
using System.Security;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///    ScrollBars are the UI widgets that both let a user drive scrolling from the UI
    ///    and indicate status of scrolled content.
    ///    These are used inside the ScrollViewer.
    ///    Their visibility is determined by the scroller visibility properties on ScrollViewer.
    /// </summary>
    /// <seealso cref="Control" />
    [Localizability(LocalizationCategory.NeverLocalize)]
    [TemplatePart(Name = "PART_Track", Type = typeof(Track))]
    public class ScrollBar : RangeBase
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Instantiates a new instance of a ScrollBar.
        /// </summary>
        public ScrollBar() : base()
        {
        }


        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     Event fires when user press mouse's left button on the thumb.
        /// </summary>
        public static readonly RoutedEvent ScrollEvent = EventManager.RegisterRoutedEvent("Scroll", RoutingStrategy.Bubble, typeof(ScrollEventHandler), typeof(ScrollBar));

        /// <summary>
        /// Add / Remove Scroll event handler
        /// </summary>
        [Category("Behavior")]
        public event ScrollEventHandler Scroll { add { AddHandler(ScrollEvent, value); } remove { RemoveHandler(ScrollEvent, value); } }

        /// <summary>
        /// This property represents the ScrollBar's <see cref="Orientation" />: Vertical or Horizontal.
        /// On vertical ScrollBars, the thumb moves up and down.  On horizontal bars, the thumb moves left to right.
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// ViewportSize is the amount of the scrolled extent currently visible.  For most scrolled content, this value
        /// will be bound to one of <see cref="ScrollViewer" />'s ViewportSize properties.
        /// This property is in logical scrolling units.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double ViewportSize
        {
            get { return (double)GetValue(ViewportSizeProperty); }
            set { SetValue(ViewportSizeProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Orientation" /> property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty
            = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(ScrollBar),
                                          new FrameworkPropertyMetadata(Orientation.Vertical),
                                          new ValidateValueCallback(IsValidOrientation));


        /// <summary>
        /// DependencyProperty for <see cref="ViewportSize" /> property.
        /// </summary>
        public static readonly DependencyProperty ViewportSizeProperty
            = DependencyProperty.Register("ViewportSize", typeof(double), typeof(ScrollBar),
                                          new FrameworkPropertyMetadata(0.0d),
                                          new ValidateValueCallback(System.Windows.Shapes.Shape.IsDoubleFiniteNonNegative));


        /// <summary>
        /// Gets reference to ScrollBar's Track element.
        /// </summary>
        public Track Track
        {
            get
            {
                return _track;
            }
        }

        #endregion Properties

        #region Method Overrides

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ScrollBarAutomationPeer(this);
        }

        /// <summary>
        /// ScrollBar supports 'Move-To-Point' by pre-processes Shift+MouseLeftButton Click.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _thumbOffset = new Vector();
            if ((Track != null) &&
                (Track.IsMouseOver) &&
                ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift))
            {
                // Move Thumb to the Mouse location
                Point pt = e.MouseDevice.GetPosition((IInputElement)Track);
                double newValue = Track.ValueFromPoint(pt);
                if (System.Windows.Shapes.Shape.IsDoubleFinite(newValue))
                {
                    ChangeValue(newValue, false /* defer */);
                }

                if (Track.Thumb != null && Track.Thumb.IsMouseOver)
                {
                    Point thumbPoint = e.MouseDevice.GetPosition((IInputElement)Track.Thumb);
                    _thumbOffset = thumbPoint - new Point(Track.Thumb.ActualWidth * 0.5, Track.Thumb.ActualHeight * 0.5);
                }
                else
                {
                    e.Handled = true;
                }
            }

            base.OnPreviewMouseLeftButtonDown(e);
        }

        /// <summary>
        /// ScrollBar need to remember the point which ContextMenu is invoke in order to perform 'Scroll Here' command correctly.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
        {
            if (Track != null)
            {
                // Remember the mouse point (relative to Track's co-ordinate).
                _latestRightButtonClickPoint = e.MouseDevice.GetPosition((IInputElement)Track);
            }
            else
            {
                // Clear the mouse point
                _latestRightButtonClickPoint = new Point(-1,-1);
            }

            base.OnPreviewMouseRightButtonUp(e);
        }


        /// <summary>
        ///     Fetches the value of the IsEnabled property
        /// </summary>
        /// <remarks>
        ///     The reason this property is overridden is so that ScrollBar
        ///     could infuse the value for EnoughContentToScroll into it.
        /// </remarks>
        protected override bool IsEnabledCore
        {
            get
            {
                return base.IsEnabledCore && _canScroll;
            }
        }

        /// <summary>
        /// ScrollBar locates the Track element when its visual tree is created
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _track = GetTemplateChild(TrackName) as Track;
        }

        #endregion

        /// <summary>
        /// Scroll content by one line to the top.
        /// </summary>
        public static readonly RoutedCommand LineUpCommand = new RoutedCommand("LineUp", typeof(ScrollBar));
        /// <summary>
        /// Scroll content by one line to the bottom.
        /// </summary>
        public static readonly RoutedCommand LineDownCommand = new RoutedCommand("LineDown", typeof(ScrollBar));
        /// <summary>
        /// Scroll content by one line to the left.
        /// </summary>
        public static readonly RoutedCommand LineLeftCommand = new RoutedCommand("LineLeft", typeof(ScrollBar));
        /// <summary>
        /// Scroll content by one line to the right.
        /// </summary>
        public static readonly RoutedCommand LineRightCommand = new RoutedCommand("LineRight", typeof(ScrollBar));
        /// <summary>
        /// Scroll content by one page to the top.
        /// </summary>
        public static readonly RoutedCommand PageUpCommand = new RoutedCommand("PageUp", typeof(ScrollBar));
        /// <summary>
        /// Scroll content by one page to the bottom.
        /// </summary>
        public static readonly RoutedCommand PageDownCommand = new RoutedCommand("PageDown", typeof(ScrollBar));
        /// <summary>
        /// Scroll content by one page to the left.
        /// </summary>
        public static readonly RoutedCommand PageLeftCommand = new RoutedCommand("PageLeft", typeof(ScrollBar));
        /// <summary>
        /// Scroll content by one page to the right.
        /// </summary>
        public static readonly RoutedCommand PageRightCommand = new RoutedCommand("PageRight", typeof(ScrollBar));
        /// <summary>
        /// Horizontally scroll to the beginning of the content.
        /// </summary>
        public static readonly RoutedCommand ScrollToEndCommand = new RoutedCommand("ScrollToEnd", typeof(ScrollBar));
        /// <summary>
        /// Horizontally scroll to the end of the content.
        /// </summary>
        public static readonly RoutedCommand ScrollToHomeCommand = new RoutedCommand("ScrollToHome", typeof(ScrollBar));
        /// <summary>
        /// Horizontally scroll to the beginning of the content.
        /// </summary>
        public static readonly RoutedCommand ScrollToRightEndCommand = new RoutedCommand("ScrollToRightEnd", typeof(ScrollBar));
        /// <summary>
        /// Horizontally scroll to the end of the content.
        /// </summary>
        public static readonly RoutedCommand ScrollToLeftEndCommand = new RoutedCommand("ScrollToLeftEnd", typeof(ScrollBar));
        /// <summary>
        /// Vertically scroll to the beginning of the content.
        /// </summary>
        public static readonly RoutedCommand ScrollToTopCommand = new RoutedCommand("ScrollToTop", typeof(ScrollBar));
        /// <summary>
        /// Vertically scroll to the end of the content.
        /// </summary>
        public static readonly RoutedCommand ScrollToBottomCommand = new RoutedCommand("ScrollToBottom", typeof(ScrollBar));
        /// <summary>
        /// Scrolls horizontally to the double value provided in <see cref="System.Windows.Input.ExecutedRoutedEventArgs.Parameter" />.
        /// </summary>
        public static readonly RoutedCommand ScrollToHorizontalOffsetCommand = new RoutedCommand("ScrollToHorizontalOffset", typeof(ScrollBar));
        /// <summary>
        /// Scrolls vertically to the double value provided in <see cref="System.Windows.Input.ExecutedRoutedEventArgs.Parameter" />.
        /// </summary>
        public static readonly RoutedCommand ScrollToVerticalOffsetCommand = new RoutedCommand("ScrollToVerticalOffset", typeof(ScrollBar));
        /// <summary>
        /// Scrolls horizontally by dragging to the double value provided in <see cref="System.Windows.Input.ExecutedRoutedEventArgs.Parameter" />.
        /// </summary>
        public static readonly RoutedCommand DeferScrollToHorizontalOffsetCommand = new RoutedCommand("DeferScrollToToHorizontalOffset", typeof(ScrollBar));
        /// <summary>
        /// Scrolls vertically by dragging to the double value provided in <see cref="System.Windows.Input.ExecutedRoutedEventArgs.Parameter" />.
        /// </summary>
        public static readonly RoutedCommand DeferScrollToVerticalOffsetCommand = new RoutedCommand("DeferScrollToVerticalOffset", typeof(ScrollBar));

        /// <summary>
        /// Scroll to the point where user invoke ScrollBar ContextMenu.  This command is always handled by ScrollBar.
        /// </summary>
        public static readonly RoutedCommand ScrollHereCommand = new RoutedCommand("ScrollHere", typeof(ScrollBar));


        //-------------------------------------------------------------------
        //
        //  Private Methods (and Event Handlers)
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static void OnThumbDragStarted(object sender, DragStartedEventArgs e)
        {
            ScrollBar scrollBar = sender as ScrollBar;
            if (scrollBar == null) { return; }

            scrollBar._hasScrolled = false;
            scrollBar._previousValue = scrollBar.Value;
        }

        // Event handler to listen to thumb events.
        private static void OnThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            ScrollBar scrollBar = sender as ScrollBar;
            if (scrollBar == null) { return; }

            scrollBar.UpdateValue(e.HorizontalChange + scrollBar._thumbOffset.X, e.VerticalChange + scrollBar._thumbOffset.Y);
        }

        // Update ScrollBar Value based on the Thumb drag delta.
        // Deal with pixel -> logical scrolling unit conversion, and restrict the value to within [Minimum, Maximum].
        private void UpdateValue(double horizontalDragDelta, double verticalDragDelta)
        {
            if (Track != null)
            {
                double valueDelta = Track.ValueFromDistance(horizontalDragDelta, verticalDragDelta);
                if (    System.Windows.Shapes.Shape.IsDoubleFinite(valueDelta)
                    &&  !DoubleUtil.IsZero(valueDelta))
                {
                    double currentValue = Value;
                    double newValue = currentValue + valueDelta;

                    double perpendicularDragDelta;

                    // Compare distances from thumb for horizontal and vertical orientations
                    if (Orientation == Orientation.Horizontal)
                    {
                        perpendicularDragDelta = Math.Abs(verticalDragDelta);
                    }
                    else //Orientation == Orientation.Vertical
                    {
                        perpendicularDragDelta = Math.Abs(horizontalDragDelta);
                    }

                    if (DoubleUtil.GreaterThan(perpendicularDragDelta, MaxPerpendicularDelta))
                    {
                        newValue = _previousValue;
                    }

                    if (!DoubleUtil.AreClose(currentValue, newValue))
                    {
                        _hasScrolled = true;
                        ChangeValue(newValue, true /* defer */);
                        RaiseScrollEvent(ScrollEventType.ThumbTrack);
                    }
                }
            }
        }

        /// <summary>
        /// Listen to Thumb DragCompleted event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            ((ScrollBar)sender).OnThumbDragCompleted(e);
        }

        /// <summary>
        /// Called when user dragging the Thumb.
        /// This function can be override to customize the way Slider handles Thumb movement.
        /// </summary>
        /// <param name="e"></param>
        private void OnThumbDragCompleted(DragCompletedEventArgs e)
        {
            if (_hasScrolled)
            {
                FinishDrag();
                RaiseScrollEvent(ScrollEventType.EndScroll);
            }
        }

        private IInputElement CommandTarget
        {
            get
            {
                IInputElement target = TemplatedParent as IInputElement;
                if (target == null)
                {
                    target = this;
                }

                return target;
            }
        }

        private void FinishDrag()
        {
            double value = Value;
            IInputElement target = CommandTarget;
            RoutedCommand command = (Orientation == Orientation.Horizontal) ? DeferScrollToHorizontalOffsetCommand : DeferScrollToVerticalOffsetCommand;

            if (command.CanExecute(value, target))
            {
                // If we were reporting drag commands, we need to give a final scroll command
                ChangeValue(value, false /* defer */);
            }
        }
        

        private void ChangeValue(double newValue, bool defer)
        {
            newValue = Math.Min(Math.Max(newValue, Minimum), Maximum);
            if (IsStandalone) { Value = newValue; }
            else
            {
                IInputElement target = CommandTarget;
                RoutedCommand command = null;
                bool horizontal = (Orientation == Orientation.Horizontal);

                // Fire the deferred (drag) version of the command
                if (defer)
                {
                    command = horizontal ? DeferScrollToHorizontalOffsetCommand : DeferScrollToVerticalOffsetCommand;
                    if (command.CanExecute(newValue, target))
                    {
                        // The defer version of the command is enabled, fire this command and not the scroll version
                        command.Execute(newValue, target);
                    }
                    else
                    {
                        // The defer version of the command is not enabled, reset and try the scroll version
                        command = null;
                    }
                }

                if (command == null)
                {
                    // Either we're not dragging or the drag command is not enabled, try the scroll version
                    command = horizontal ? ScrollToHorizontalOffsetCommand : ScrollToVerticalOffsetCommand;
                    if (command.CanExecute(newValue, target))
                    {
                        command.Execute(newValue, target);
                    }
                }
            }
        }

        /// <summary>
        /// Scroll to the position where ContextMenu was invoked.
        /// </summary>
        internal void ScrollToLastMousePoint()
        {
            Point pt = new Point(-1,-1);
            if ((Track != null) && (_latestRightButtonClickPoint != pt))
            {
                double newValue = Track.ValueFromPoint(_latestRightButtonClickPoint);
                if (System.Windows.Shapes.Shape.IsDoubleFinite(newValue))
                {
                    ChangeValue(newValue, false /* defer */);
                    _latestRightButtonClickPoint = pt;
                    RaiseScrollEvent(ScrollEventType.ThumbPosition);
                }
            }
        }

        internal void RaiseScrollEvent(ScrollEventType scrollEventType)
        {
            ScrollEventArgs newEvent = new ScrollEventArgs(scrollEventType, Value);
            newEvent.Source=this;
            RaiseEvent(newEvent);
        }

        private static void OnScrollCommand(object target, ExecutedRoutedEventArgs args)
        {
            ScrollBar scrollBar = ((ScrollBar)target);
            if (args.Command == ScrollBar.ScrollHereCommand)
            {
                scrollBar.ScrollToLastMousePoint();
            }

            if (scrollBar.IsStandalone)
            {
                if (scrollBar.Orientation == Orientation.Vertical)
                {
                    if (args.Command == ScrollBar.LineUpCommand)
                    {
                        scrollBar.LineUp();
                    }
                    else if (args.Command == ScrollBar.LineDownCommand)
                    {
                        scrollBar.LineDown();
                    }
                    else if (args.Command == ScrollBar.PageUpCommand)
                    {
                        scrollBar.PageUp();
                    }
                    else if (args.Command == ScrollBar.PageDownCommand)
                    {
                        scrollBar.PageDown();
                    }
                    else if (args.Command == ScrollBar.ScrollToTopCommand)
                    {
                        scrollBar.ScrollToTop();
                    }
                    else if (args.Command == ScrollBar.ScrollToBottomCommand)
                    {
                        scrollBar.ScrollToBottom();
                    }
                }
                else //Horizontal
                {
                    if (args.Command == ScrollBar.LineLeftCommand)
                    {
                        scrollBar.LineLeft();
                    }
                    else if (args.Command == ScrollBar.LineRightCommand)
                    {
                        scrollBar.LineRight();
                    }
                    else if (args.Command == ScrollBar.PageLeftCommand)
                    {
                        scrollBar.PageLeft();
                    }
                    else if (args.Command == ScrollBar.PageRightCommand)
                    {
                        scrollBar.PageRight();
                    }
                    else if (args.Command == ScrollBar.ScrollToLeftEndCommand)
                    {
                        scrollBar.ScrollToLeftEnd();
                    }
                    else if (args.Command == ScrollBar.ScrollToRightEndCommand)
                    {
                        scrollBar.ScrollToRightEnd();
                    }
                }
            }
        }

        private void SmallDecrement()
        {
            double newValue = Math.Max(Value - SmallChange, Minimum);
            if (Value != newValue)
            {
                Value = newValue;
                RaiseScrollEvent(ScrollEventType.SmallDecrement);
            }
        }
        private void SmallIncrement()
        {
            double newValue = Math.Min(Value + SmallChange, Maximum);
            if (Value != newValue)
            {
                Value = newValue;
                RaiseScrollEvent(ScrollEventType.SmallIncrement);
            }
        }
        private void LargeDecrement()
        {
            double newValue = Math.Max(Value - LargeChange, Minimum);
            if (Value != newValue)
            {
                Value = newValue;
                RaiseScrollEvent(ScrollEventType.LargeDecrement);
            }
        }
        private void LargeIncrement()
        {
            double newValue = Math.Min(Value + LargeChange, Maximum);
            if (Value != newValue)
            {
                Value = newValue;
                RaiseScrollEvent(ScrollEventType.LargeIncrement);
            }
        }
        private void ToMinimum()
        {
            if (Value != Minimum)
            {
                Value = Minimum;
                RaiseScrollEvent(ScrollEventType.First);
            }
        }
        private void ToMaximum()
        {
            if (Value != Maximum)
            {
                Value = Maximum;
                RaiseScrollEvent(ScrollEventType.Last);
            }
        }
        private void LineUp()
        {
            SmallDecrement();
        }
        private void LineDown()
        {
            SmallIncrement();
        }
        private void PageUp()
        {
            LargeDecrement();
        }
        private void PageDown()
        {
            LargeIncrement();
        }
        private void ScrollToTop()
        {
            ToMinimum();
        }
        private void ScrollToBottom()
        {
            ToMaximum();
        }
        private void LineLeft()
        {
            SmallDecrement();
        }
        private void LineRight()
        {
            SmallIncrement();
        }
        private void PageLeft()
        {
            LargeDecrement();
        }
        private void PageRight()
        {
            LargeIncrement();
        }
        private void ScrollToLeftEnd()
        {
            ToMinimum();
        }
        private void ScrollToRightEnd()
        {
            ToMaximum();
        }

        private static void OnQueryScrollHereCommand(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = (args.Command == ScrollBar.ScrollHereCommand);
        }

        private static void OnQueryScrollCommand(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = ((ScrollBar)target).IsStandalone;
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Members
        //
        //-------------------------------------------------------------------

        #region Private Members

        #endregion

        //-------------------------------------------------------------------
        //
        //  Static Constructor & Delegates
        //
        //-------------------------------------------------------------------

        #region Static Constructor & Delegates

        static ScrollBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ScrollBar), new FrameworkPropertyMetadata(typeof(ScrollBar)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ScrollBar));

            var onScrollCommand = new ExecutedRoutedEventHandler(OnScrollCommand);
            var onQueryScrollCommand = new CanExecuteRoutedEventHandler(OnQueryScrollCommand);
            
            FocusableProperty.OverrideMetadata(typeof(ScrollBar), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

            // Register Event Handler for the Thumb
            EventManager.RegisterClassHandler(typeof(ScrollBar), Thumb.DragStartedEvent, new DragStartedEventHandler(OnThumbDragStarted));
            EventManager.RegisterClassHandler(typeof(ScrollBar), Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnThumbDragDelta));
            EventManager.RegisterClassHandler(typeof(ScrollBar), Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnThumbDragCompleted));

            // ScrollBar has common handler for ScrollHere command.
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.ScrollHereCommand, onScrollCommand, new CanExecuteRoutedEventHandler(OnQueryScrollHereCommand));
            // Vertical Commands
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.LineUpCommand, onScrollCommand, onQueryScrollCommand, Key.Up);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.LineDownCommand, onScrollCommand, onQueryScrollCommand, Key.Down);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.PageUpCommand, onScrollCommand, onQueryScrollCommand, Key.PageUp);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.PageDownCommand, onScrollCommand, onQueryScrollCommand, Key.PageDown);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.ScrollToTopCommand, onScrollCommand, onQueryScrollCommand, new KeyGesture(Key.Home, ModifierKeys.Control));
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.ScrollToBottomCommand, onScrollCommand, onQueryScrollCommand, new KeyGesture(Key.End, ModifierKeys.Control));
            // Horizontal Commands
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.LineLeftCommand, onScrollCommand, onQueryScrollCommand, Key.Left);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.LineRightCommand, onScrollCommand, onQueryScrollCommand, Key.Right);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.PageLeftCommand, onScrollCommand, onQueryScrollCommand);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.PageRightCommand, onScrollCommand, onQueryScrollCommand);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.ScrollToLeftEndCommand, onScrollCommand, onQueryScrollCommand, Key.Home);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollBar), ScrollBar.ScrollToRightEndCommand, onScrollCommand, onQueryScrollCommand, Key.End);

            MaximumProperty.OverrideMetadata(typeof(ScrollBar), new FrameworkPropertyMetadata(new PropertyChangedCallback(ViewChanged)));
            MinimumProperty.OverrideMetadata(typeof(ScrollBar), new FrameworkPropertyMetadata(new PropertyChangedCallback(ViewChanged)));

            ContextMenuProperty.OverrideMetadata(typeof(ScrollBar), new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceContextMenu)));

            ControlsTraceLogger.AddControl(TelemetryControls.ScrollBar);
        }

        private static void ViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScrollBar scrollBar = (ScrollBar)d;

            bool canScrollNew = scrollBar.Maximum > scrollBar.Minimum;

            if (canScrollNew != scrollBar._canScroll)
            {
                scrollBar._canScroll = canScrollNew;
                scrollBar.CoerceValue(IsEnabledProperty);
            }
        }

        // Consider adding a verify for ViewportSize to check > 0.0

        internal static bool IsValidOrientation(object o)
        {
            Orientation value = (Orientation)o;
            return value == Orientation.Horizontal
                || value == Orientation.Vertical;
        }

        // Is the scrollbar outside of a scrollviewer?
        internal bool IsStandalone
        {
            get { return _isStandalone; }
            set { _isStandalone = value; }
        }

        // Maximum distance you can drag from thumb before it snaps back
        private const double MaxPerpendicularDelta = 150;
        private const string TrackName = "PART_Track";

        private Track _track;

        private Point _latestRightButtonClickPoint = new Point(-1,-1);

        private bool _canScroll = true;  // Maximum > Minimum by default
        private bool _hasScrolled;  // Has the thumb been dragged
        private bool _isStandalone = true;
        private bool _openingContextMenu;
        private double _previousValue;
        private Vector _thumbOffset;



        #endregion


        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey

        #region ContextMenu

        private static object CoerceContextMenu(DependencyObject o, object value)
        {
            bool hasModifiers;
            ScrollBar sb = (ScrollBar)o;
            if (sb._openingContextMenu &&
                sb.GetValueSource(ContextMenuProperty, null, out hasModifiers) == BaseValueSourceInternal.Default && !hasModifiers)
            {
                // Use a default menu
                if (sb.Orientation == Orientation.Vertical)
                {
                    return VerticalContextMenu;
                }
                else if (sb.FlowDirection == FlowDirection.LeftToRight)
                {
                    return HorizontalContextMenuLTR;
                }
                else
                {
                    return HorizontalContextMenuRTL;
                }
            }
            return value;
        }

        /// <summary>
        ///     Called when ContextMenuOpening is raised on this element.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            base.OnContextMenuOpening(e);

            if (!e.Handled)
            {
                _openingContextMenu = true;
                CoerceValue(ContextMenuProperty);
            }
        }

        /// <summary>
        ///     Called when ContextMenuOpening is raised on this element.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnContextMenuClosing(ContextMenuEventArgs e)
        {
            base.OnContextMenuClosing(e);

            _openingContextMenu = false;
            CoerceValue(ContextMenuProperty);
        }

        private static ContextMenu VerticalContextMenu
        {
            get
            {
                ContextMenu verticalContextMenu = new ContextMenu();
                verticalContextMenu.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_ScrollHere, "ScrollHere", ScrollBar.ScrollHereCommand));
                verticalContextMenu.Items.Add(new Separator());
                verticalContextMenu.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_Top, "Top", ScrollBar.ScrollToTopCommand));
                verticalContextMenu.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_Bottom, "Bottom", ScrollBar.ScrollToBottomCommand));
                verticalContextMenu.Items.Add(new Separator());
                verticalContextMenu.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_PageUp, "PageUp", ScrollBar.PageUpCommand));
                verticalContextMenu.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_PageDown, "PageDown", ScrollBar.PageDownCommand));
                verticalContextMenu.Items.Add(new Separator());
                verticalContextMenu.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_ScrollUp, "ScrollUp", ScrollBar.LineUpCommand));
                verticalContextMenu.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_ScrollDown, "ScrollDown", ScrollBar.LineDownCommand));
                return verticalContextMenu;
            }
        }

        // LeftToRight menu
        private static ContextMenu HorizontalContextMenuLTR
        {
            get
            {
                ContextMenu horizontalContextMenuLeftToRight = new ContextMenu();
                horizontalContextMenuLeftToRight.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_ScrollHere, "ScrollHere", ScrollBar.ScrollHereCommand));
                horizontalContextMenuLeftToRight.Items.Add(new Separator());
                horizontalContextMenuLeftToRight.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_LeftEdge, "LeftEdge", ScrollBar.ScrollToLeftEndCommand));
                horizontalContextMenuLeftToRight.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_RightEdge, "RightEdge", ScrollBar.ScrollToRightEndCommand));
                horizontalContextMenuLeftToRight.Items.Add(new Separator());
                horizontalContextMenuLeftToRight.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_PageLeft, "PageLeft", ScrollBar.PageLeftCommand));
                horizontalContextMenuLeftToRight.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_PageRight, "PageRight", ScrollBar.PageRightCommand));
                horizontalContextMenuLeftToRight.Items.Add(new Separator());
                horizontalContextMenuLeftToRight.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_ScrollLeft, "ScrollLeft", ScrollBar.LineLeftCommand));
                horizontalContextMenuLeftToRight.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_ScrollRight, "ScrollRight", ScrollBar.LineRightCommand));
                return horizontalContextMenuLeftToRight;
            }
        }

        // RightToLeft menu
        private static ContextMenu HorizontalContextMenuRTL
        {
            get
            {
                ContextMenu horizontalContextMenuRightToLeft = new ContextMenu();
                horizontalContextMenuRightToLeft.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_ScrollHere, "ScrollHere", ScrollBar.ScrollHereCommand));
                horizontalContextMenuRightToLeft.Items.Add(new Separator());
                horizontalContextMenuRightToLeft.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_LeftEdge, "LeftEdge", ScrollBar.ScrollToRightEndCommand));
                horizontalContextMenuRightToLeft.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_RightEdge, "RightEdge", ScrollBar.ScrollToLeftEndCommand));
                horizontalContextMenuRightToLeft.Items.Add(new Separator());
                horizontalContextMenuRightToLeft.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_PageLeft, "PageLeft", ScrollBar.PageRightCommand));
                horizontalContextMenuRightToLeft.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_PageRight, "PageRight", ScrollBar.PageLeftCommand));
                horizontalContextMenuRightToLeft.Items.Add(new Separator());
                horizontalContextMenuRightToLeft.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_ScrollLeft, "ScrollLeft", ScrollBar.LineRightCommand));
                horizontalContextMenuRightToLeft.Items.Add(CreateMenuItem(SRID.ScrollBar_ContextMenu_ScrollRight, "ScrollRight", ScrollBar.LineLeftCommand));
                return horizontalContextMenuRightToLeft;
            }
        }

        private static MenuItem CreateMenuItem(string name, string automationId, RoutedCommand command)
        {
            MenuItem menuItem = new MenuItem();
            menuItem.Header = SR.Get(name);
            menuItem.Command = command;
            AutomationProperties.SetAutomationId(menuItem, automationId);

            Binding binding = new Binding();
            binding.Path = new PropertyPath(ContextMenu.PlacementTargetProperty);
            binding.Mode = BindingMode.OneWay;
            binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ContextMenu), 1);
            menuItem.SetBinding(MenuItem.CommandTargetProperty, binding);

            return menuItem;
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 42; }
        }

        #endregion ContextMenu
    }

    /// <summary>
    /// Type of scrolling that causes ScrollEvent.
    /// </summary>
    public enum ScrollEventType
    {
        /// <summary>
        /// Thumb has stopped moving.
        /// </summary>
        EndScroll,
        /// <summary>
        /// Thumb was moved to the Minimum position.
        /// </summary>
        First,
        /// <summary>
        /// Thumb was moved a large distance. The user clicked the scroll bar to the left(horizontal) or above(vertical) the scroll box.
        /// </summary>
        LargeDecrement,
        /// <summary>
        /// Thumb was moved a large distance. The user clicked the scroll bar to the right(horizontal) or below(vertical) the scroll box.
        /// </summary>
        LargeIncrement,
        /// <summary>
        /// Thumb was moved to the Maximum position
        /// </summary>
        Last,
        /// <summary>
        /// Thumb was moved a small distance. The user clicked the left(horizontal) or top(vertical) scroll arrow.
        /// </summary>
        SmallDecrement,
        /// <summary>
        /// Thumb was moved a small distance. The user clicked the right(horizontal) or bottom(vertical) scroll arrow.
        /// </summary>
        SmallIncrement,
        /// <summary>
        /// Thumb was moved.
        /// </summary>
        ThumbPosition,
        /// <summary>
        /// Thumb is currently being moved.
        /// </summary>
        ThumbTrack
    }
}



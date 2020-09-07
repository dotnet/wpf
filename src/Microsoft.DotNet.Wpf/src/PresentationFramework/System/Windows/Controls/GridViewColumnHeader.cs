// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;       // SafeHandle
using System.Security;                      // 
using System.Windows.Automation.Peers;      // AutomationPeer
using System.Windows.Controls.Primitives;   // ButtonBase
using System.Windows.Input;                 // MouseButtonEventArgs
using System.Windows.Media;                 // VisualBrush

using MS.Internal;                          // DoubleUtil
using MS.Internal.KnownBoxes;               // BooleanBoxes
using MS.Win32;                             // SafeNativeMethods


namespace System.Windows.Controls
{
    /// <summary>
    /// Defines the different roles of GridViewColumnHeaders
    /// </summary>
    public enum GridViewColumnHeaderRole
    {
        /// <summary>
        /// The normal header
        /// </summary>
        Normal,
        /// <summary>
        /// The floating header (when dragging a header)
        /// </summary>
        Floating,
        /// <summary>
        /// The padding header (the very last header in header bar)
        /// </summary>
        Padding
    }

    /// <summary>
    /// column header of GridView
    /// </summary>
#if OLD_AUTOMATION
    [Automation(AccessibilityControlType = "Button")]
#endif
    [TemplatePart(Name = "PART_HeaderGripper", Type = typeof(Thumb))]
    [TemplatePart(Name = "PART_FloatingHeaderCanvas", Type = typeof(Canvas))]
    public class GridViewColumnHeader : ButtonBase
#if OLD_AUTOMATION
    , IInvokeProvider
#endif
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructor

        static GridViewColumnHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GridViewColumnHeader), new FrameworkPropertyMetadata(typeof(GridViewColumnHeader)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(GridViewColumnHeader));

            FocusableProperty.OverrideMetadata(typeof(GridViewColumnHeader), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

            // hookup property change event.
            StyleProperty.OverrideMetadata(typeof(GridViewColumnHeader), new FrameworkPropertyMetadata(new PropertyChangedCallback(PropertyChanged)));
            ContentTemplateProperty.OverrideMetadata(typeof(GridViewColumnHeader), new FrameworkPropertyMetadata(new PropertyChangedCallback(PropertyChanged)));
            ContentTemplateSelectorProperty.OverrideMetadata(typeof(GridViewColumnHeader), new FrameworkPropertyMetadata(new PropertyChangedCallback(PropertyChanged)));
            ContextMenuProperty.OverrideMetadata(typeof(GridViewColumnHeader), new FrameworkPropertyMetadata(new PropertyChangedCallback(PropertyChanged)));
            ToolTipProperty.OverrideMetadata(typeof(GridViewColumnHeader), new FrameworkPropertyMetadata(new PropertyChangedCallback(PropertyChanged)));
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Called when the Template's tree has been generated
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            GridViewColumnHeaderRole role = Role;

            if (role == GridViewColumnHeaderRole.Normal)
            {
                HookupGripperEvents();
            }
            else if (role == GridViewColumnHeaderRole.Floating)
            {
                // if this is a floating header, try to find the FloatingHeaderCanvas,
                // and copy source header's visual to it
                _floatingHeaderCanvas = GetTemplateChild(FloatingHeaderCanvasTemplateName) as Canvas;

                UpdateFloatingHeaderCanvas();
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The key for Column (read-only property)
        /// </summary>
        internal static readonly DependencyPropertyKey ColumnPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "Column",
                        typeof(GridViewColumn),
                        typeof(GridViewColumnHeader),
                        null);

        /// <summary>
        /// The DependencyProperty for the Column property.
        /// </summary>
        public static readonly DependencyProperty ColumnProperty =
                ColumnPropertyKey.DependencyProperty;

        /// <summary>
        /// Column associated with this header
        /// </summary>
        public GridViewColumn Column
        {
            get { return (GridViewColumn)GetValue(ColumnProperty); }
        }

        /// <summary>
        /// The key for Role (read-only property)
        /// </summary>
        internal static readonly DependencyPropertyKey RolePropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "Role",
                        typeof(GridViewColumnHeaderRole),
                        typeof(GridViewColumnHeader),
                        new FrameworkPropertyMetadata(GridViewColumnHeaderRole.Normal));

        /// <summary>
        /// The DependencyProperty for the Role property.
        /// </summary>
        public static readonly DependencyProperty RoleProperty =
                RolePropertyKey.DependencyProperty;

        /// <summary>
        /// What the role of the header is: Normal, Floating, Padding.
        /// </summary>
        [Category("Behavior")]
        public GridViewColumnHeaderRole Role
        {
            get { return (GridViewColumnHeaderRole)GetValue(RoleProperty); }
        }

        #endregion Public Properties

#if OLD_AUTOMATION
        //-------------------------------------------------------------------
        //
        //  IInvodeProvider
        //
        //-------------------------------------------------------------------

        void IInvokeProvider.Invoke()
        {
            IsAccessKeyOrAutomation = true;
            OnClick();
        }
#endif
        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            // give parent a chance to handle MouseButtonEvent (for GridViewHeaderRowPresenter by default)
            e.Handled = false;

            if (ClickMode == ClickMode.Hover && IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }
        }

        /// <summary>
        /// This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // give parent a chance to handle MouseButtonEvent (for GridViewHeaderRowPresenter by default)
            e.Handled = false;

            //If ClickMode is Hover, we must capture mouse in order to let column reorder work correctly (Bug#1496673)
            if (ClickMode == ClickMode.Hover && e.ButtonState == MouseButtonState.Pressed)
            {
                CaptureMouse();
            }
        }

        /// <summary>
        /// This is the method that responds to the MouseMoveEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // Override base method: if left mouse is pressed, always set IsPressed as true
            if ((ClickMode != ClickMode.Hover) &&
                (IsMouseCaptured && (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)))
            {
                SetValue(ButtonBase.IsPressedPropertyKey, BooleanBoxes.TrueBox);
            }

            e.Handled = false;
        }

        /// <summary>
        /// Override for <seealso cref="UIElement.OnRenderSizeChanged"/>
        /// </summary>
        protected internal override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            // when render size is changed, check to hide the previous header's right half gripper
            CheckWidthForPreviousHeaderGripper();
        }

        /// <summary>
        /// Override base method: raises the Click event only when not re-ordering
        /// </summary>
        protected override void OnClick()
        {
            // if not suppress click event
            if (!SuppressClickEvent)
            {
                // if is clicked by access key or automation,
                // otherwise should be clicked by mouse
                if (IsAccessKeyOrAutomation || !IsMouseOutside())
                {
                    IsAccessKeyOrAutomation = false;
                    ClickImplement();
                    MakeParentGotFocus();
                }
            }
        }

        /// <summary>
        /// The Access key for this control was invoked.
        /// </summary>
        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            IsAccessKeyOrAutomation = true;

            base.OnAccessKey(e);
        }

        /// <summary>
        /// Stop Style, ContentTemplate , ContentTemplateSelector, ContextMenu and ToolTip properties
        /// from been serialized in case the value are pushed from GridView or GridViewHeaderRowPresenter.
        /// </summary>
        protected internal override bool ShouldSerializeProperty(DependencyProperty dp)
        {
            if (IsInternalGenerated)
            {
                // we should never reach here since this header is instantiated by HeaderRowPresenter.
                Debug.Assert(false, "Method ShouldSerializeProperty is called on an internally generated GridViewColumnHeader.");

                // nothing should be serialized from this object.
                return false;
            }

            Flags flag, ignoreFlag;
            PropertyToFlags(dp, out flag, out ignoreFlag); //ignoreFlag is never used in this method.

            return ((flag == Flags.None) || GetFlag(flag))
                && base.ShouldSerializeProperty(dp);
        }

        /// <summary>
        /// An event reporting the mouse entered this element.
        /// </summary>
        /// <param name="e">Event arguments</param>
        //Override OnMouseEnter/Leave to process the ClickMode == Hover case
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            if (HandleIsMouseOverChanged())
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// An event reporting the mouse left this element.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (HandleIsMouseOverChanged())
            {
                e.Handled = true;
            }
        }

        /// <summary>
        ///     An event announcing that the keyboard is no longer focused
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);

            if (ClickMode == ClickMode.Hover && IsMouseCaptured)
            {
                ReleaseMouseCapture();
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
        /// This method is called when column header is clicked via IInvokeProvider.
        /// </summary>
        internal void AutomationClick()
        {
            IsAccessKeyOrAutomation = true;
            OnClick();
        }

        // cancel resizing if Escape key down.
        internal void OnColumnHeaderKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _headerGripper != null && _headerGripper.IsDragging)
            {
                // NOTE: this will cause Thumb to complete the dragging and fire drag
                // complete event with the Canceled property as 'True'. Handler
                // OnColumnHeaderGripperDragCompleted will restore the width.
                _headerGripper.CancelDrag();
                e.Handled = true;
            }
        }

        // Check to see if hide previous header's right half gripper
        internal void CheckWidthForPreviousHeaderGripper()
        {
            bool hideGripperRightHalf = false;

            if (_headerGripper != null)
            {
                // when header's width is less than gripper's width,
                // hide the right half of the left header's gripper
                hideGripperRightHalf = DoubleUtil.LessThan(ActualWidth, _headerGripper.Width);
            }

            if (_previousHeader != null)
            {
                _previousHeader.HideGripperRightHalf(hideGripperRightHalf);
            }

            UpdateGripperCursor();
        }

        // Reset the background visual brush ref to
        // avoid keeping it alive.  Keeping a VisualBrush alive causes us to assume that the
        // entire Visual tree is a graph, preventing an optimized render walk of only
        // the dirty subtree.  We would end up rendering all of our realizations on each
        // frame, causing high CPU consumption when a large realization tree is present.
        internal void ResetFloatingHeaderCanvasBackground()
        {
            if (_floatingHeaderCanvas != null)
            {
                _floatingHeaderCanvas.Background = null;
            }
        }

        /// <summary>
        /// This method is called iff related properties are passed from GirdView/GridViewColumn to header.
        /// And must use this method to update property from GirdView/GridViewColumn to header.
        ///
        /// If this header is instantiated by user, before actually update the property,
        /// this method will turn on the IgnoreXXX flag. And the PropertyChangeCallBack
        /// will check this flag, and know that this update is an internal operation. By
        /// doing this, we can distinguish {the property change by user} from {the change
        /// by HeaderRowPresenter}.
        /// </summary>
        /// <param name="dp">the property you want to update</param>
        /// <param name="value">a null value will result in ClearValue operation</param>
        internal void UpdateProperty(DependencyProperty dp, object value)
        {
            Flags ignoreFlag = Flags.None;

            if (!IsInternalGenerated)
            {
                Flags flag;
                PropertyToFlags(dp, out flag, out ignoreFlag);
                Debug.Assert(flag != Flags.None && ignoreFlag != Flags.None, "Invalid parameter dp.");

                if (GetFlag(flag)) /* user has provided value for the property */
                {
                    return;
                }
                else
                {
                    SetFlag(ignoreFlag, true);
                }
            }

            if (value != null)
            {
                SetValue(dp, value);
            }
            else
            {
                ClearValue(dp);
            }

            SetFlag(ignoreFlag, false);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey

        #region PreviousVisualHeader

        // Link to the previous visual column header, the value is filled by GridViewHeaderRowPresenter
        internal GridViewColumnHeader PreviousVisualHeader
        {
            get { return _previousHeader; }
            set { _previousHeader = value; }
        }

        private GridViewColumnHeader _previousHeader;

        #endregion PreviousVisualHeader

        #region SuppressClickEvent

        // indicating whether to fire click event
        internal bool SuppressClickEvent
        {
            get { return GetFlag(Flags.SuppressClickEvent); }
            set { SetFlag(Flags.SuppressClickEvent, value); }
        }

        #endregion SuppressClickEvent

        // the source header for floating
        // This property is only used to create VisualBrush for floating header,
        // and will be set to null when VisualBrush is created. Set to null for GC.
        internal GridViewColumnHeader FloatSourceHeader
        {
            get { return _srcHeader; }
            set { _srcHeader = value; }
        }

        // whether this header is generated by GVHeaderRowPresenter or user
        internal bool IsInternalGenerated
        {
            get { return GetFlag(Flags.IsInternalGenerated); }
            set { SetFlag(Flags.IsInternalGenerated, value); }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Accessibility
        //
        //-------------------------------------------------------------------

        #region Accessibility

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new GridViewColumnHeaderAutomationPeer(this);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridViewColumnHeader header = (GridViewColumnHeader)d;
            if (!header.IsInternalGenerated)
            {
                Flags flag, ignoreFlag;
                PropertyToFlags(e.Property, out flag, out ignoreFlag);

                if (!header.GetFlag(ignoreFlag)) // value is updated by user
                {
                    if (e.NewValueSource == BaseValueSourceInternal.Local)
                    {
                        header.SetFlag(flag, true);
                    }
                    else
                    {
                        header.SetFlag(flag, false);

                        GridViewHeaderRowPresenter headerRowPresenter = header.Parent as GridViewHeaderRowPresenter;
                        if (headerRowPresenter != null)
                        {
                            headerRowPresenter.UpdateHeaderProperty(header, e.Property);
                        }
                    }
                }
            }
        }

        private static void PropertyToFlags(DependencyProperty dp, out Flags flag, out Flags ignoreFlag)
        {
            if (dp == GridViewColumnHeader.StyleProperty)
            {
                flag = Flags.StyleSetByUser;
                ignoreFlag = Flags.IgnoreStyle;
            }
            else if (dp == GridViewColumnHeader.ContentTemplateProperty)
            {
                flag = Flags.ContentTemplateSetByUser;
                ignoreFlag = Flags.IgnoreContentTemplate;
            }
            else if (dp == GridViewColumnHeader.ContentTemplateSelectorProperty)
            {
                flag = Flags.ContentTemplateSelectorSetByUser;
                ignoreFlag = Flags.IgnoreContentTemplateSelector;
            }
            else if (dp == GridViewColumnHeader.ContentStringFormatProperty)
            {
                flag = Flags.ContentStringFormatSetByUser;
                ignoreFlag = Flags.IgnoreContentStringFormat;
            }
            else if (dp == GridViewColumnHeader.ContextMenuProperty)
            {
                flag = Flags.ContextMenuSetByUser;
                ignoreFlag = Flags.IgnoreContextMenu;
            }
            else if (dp == GridViewColumnHeader.ToolTipProperty)
            {
                flag = Flags.ToolTipSetByUser;
                ignoreFlag = Flags.IgnoreToolTip;
            }
            else
            {
                flag = ignoreFlag = Flags.None;
            }
        }

        /// <summary>
        /// Hide the right half of gripper
        /// +-----------------+
        /// +            +----+
        /// +  Header    + Re +
        /// +            +    +
        /// +            +----+
        /// +-----------------+
        /// </summary>
        /// <param name="hide"></param>
        private void HideGripperRightHalf(bool hide)
        {
            if (_headerGripper != null)
            {
                // hide gripper's right half by setting Parent.ClipToBounds=true
                FrameworkElement gripperContainer = _headerGripper.Parent as FrameworkElement;
                if (gripperContainer != null)
                {
                    gripperContainer.ClipToBounds = hide;
                }
            }
        }

        // Save the original width before header resize
        private void OnColumnHeaderGripperDragStarted(object sender, DragStartedEventArgs e)
        {
            MakeParentGotFocus();
            _originalWidth = ColumnActualWidth;
            e.Handled = true;
        }

        //Because ColumnHeader isn't focusable, we must forward focus to ListView when user invoke the header by access key
        private void MakeParentGotFocus()
        {
            GridViewHeaderRowPresenter headerRP = this.Parent as GridViewHeaderRowPresenter;
            if (headerRP != null)
            {
                headerRP.MakeParentItemsControlGotFocus();
            }
        }

        // Resize the header
        private void OnColumnHeaderResize(object sender, DragDeltaEventArgs e)
        {
            double width = ColumnActualWidth + e.HorizontalChange;
            if (DoubleUtil.LessThanOrClose(width, 0.0))
            {
                width = 0.0;
            }

            UpdateColumnHeaderWidth(width);
            e.Handled = true;
        }

        private void OnColumnHeaderGripperDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (e.Canceled)
            {
                // restore to original width
                UpdateColumnHeaderWidth(_originalWidth);
            }

            UpdateGripperCursor();
            e.Handled = true;
        }

        /// <summary>
        /// Find gripper and register drag event
        ///
        /// The default style for GridViewColumnHeader is
        /// +-----------------+
        /// +            +----------+
        /// +  Header    + Gripper  +
        /// +            +          +
        /// +            +----------+
        /// +-----------------+
        /// </summary>
        private void HookupGripperEvents()
        {
            UnhookGripperEvents();

            _headerGripper = GetTemplateChild(HeaderGripperTemplateName) as Thumb;

            if (_headerGripper != null)
            {
                _headerGripper.DragStarted += new DragStartedEventHandler(OnColumnHeaderGripperDragStarted);
                _headerGripper.DragDelta += new DragDeltaEventHandler(OnColumnHeaderResize);
                _headerGripper.DragCompleted += new DragCompletedEventHandler(OnColumnHeaderGripperDragCompleted);
                _headerGripper.MouseDoubleClick += new MouseButtonEventHandler(OnGripperDoubleClicked);
                _headerGripper.MouseEnter += new MouseEventHandler(OnGripperMouseEnterLeave);
                _headerGripper.MouseLeave += new MouseEventHandler(OnGripperMouseEnterLeave);

                _headerGripper.Cursor = SplitCursor;
            }
        }

        private void OnGripperDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (Column != null)
            {
                if (Double.IsNaN(Column.Width))
                {
                    // force update will be triggered
                    Column.Width = Column.ActualWidth;
                }

                Column.Width = Double.NaN;

                e.Handled = true;
            }
        }

        /// <summary>
        /// Clear gripper event
        /// </summary>
        private void UnhookGripperEvents()
        {
            if (_headerGripper != null)
            {
                _headerGripper.DragStarted -= new DragStartedEventHandler(OnColumnHeaderGripperDragStarted);
                _headerGripper.DragDelta -= new DragDeltaEventHandler(OnColumnHeaderResize);
                _headerGripper.DragCompleted -= new DragCompletedEventHandler(OnColumnHeaderGripperDragCompleted);
                _headerGripper.MouseDoubleClick -= new MouseButtonEventHandler(OnGripperDoubleClicked);
                _headerGripper.MouseEnter -= new MouseEventHandler(OnGripperMouseEnterLeave);
                _headerGripper.MouseLeave -= new MouseEventHandler(OnGripperMouseEnterLeave);
                _headerGripper = null;
            }
        }


        private Cursor GetCursor(int cursorID)
        {
            Invariant.Assert(cursorID == c_SPLIT || cursorID == c_SPLITOPEN, "incorrect cursor type");

            Cursor cursor = null;
            System.IO.Stream stream = null;
            System.Reflection.Assembly assembly = this.GetType().Assembly;
 
            if (cursorID == c_SPLIT)
            {
                stream = assembly.GetManifestResourceStream("split.cur"); 
            }
            else if (cursorID == c_SPLITOPEN)
            {
                stream = assembly.GetManifestResourceStream("splitopen.cur"); 
            }

            Debug.Assert(stream != null, "stream is null");
            if (stream != null)
            {
                cursor = new Cursor(stream);
            }

            return cursor;
        }

        private void UpdateGripperCursor()
        {
            if (_headerGripper != null && !_headerGripper.IsDragging)
            {
                Cursor gripperCursor;

                if (DoubleUtil.IsZero(ActualWidth))
                {
                    gripperCursor = SplitOpenCursor;
                }
                else
                {
                    gripperCursor = SplitCursor;
                }

                Debug.Assert(gripperCursor != null, "gripper cursor is null");
                if (gripperCursor != null)
                {
                    _headerGripper.Cursor = gripperCursor;
                }
            }
        }

        // Set column header width and associated column width
        private void UpdateColumnHeaderWidth(double width)
        {
            if (Column != null)
            {
                Column.Width = width;
            }
            else
            {
                Width = width;
            }
        }

        private bool IsMouseOutside()
        {
            Point pos = Mouse.PrimaryDevice.GetPosition(this);

            return !((pos.X >= 0) && (pos.X <= ActualWidth) && (pos.Y >= 0) && (pos.Y <= ActualHeight));
        }

        private void ClickImplement()
        {
            if (AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
            {
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this);
                if (peer != null)
                    peer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
            }

            base.OnClick();
        }

        private bool GetFlag(Flags flag)
        {
            return (_flags & flag) == flag;
        }

        private void SetFlag(Flags flag, bool set)
        {
            if (set)
            {
                _flags |= flag;
            }
            else
            {
                _flags &= (~flag);
            }
        }

        // update the background visual brush
        private void UpdateFloatingHeaderCanvas()
        {
            if (_floatingHeaderCanvas != null
                && FloatSourceHeader != null)
            {
                // because the gripper is partially positioned out of the header, we need to
                // map the appropriate area(viewbox) in the source header to visual brush
                // to avoid a distorded image on the floating header.
                Vector offsetVector = VisualTreeHelper.GetOffset(FloatSourceHeader);
                VisualBrush visualBrush = new VisualBrush(FloatSourceHeader);

                // set visual brush's mapping
                visualBrush.ViewboxUnits = BrushMappingMode.Absolute;
                visualBrush.Viewbox = new Rect(offsetVector.X, offsetVector.Y, FloatSourceHeader.ActualWidth, FloatSourceHeader.ActualHeight);

                _floatingHeaderCanvas.Background = visualBrush;
                FloatSourceHeader = null;
            }
        }

        /// <summary>
        /// Handle IsMouseOverChanged when ClickMode is Hover
        /// </summary>
        // Note: When ClickMode is Hover, ColumnHeader will be click when mouse is over it
        // Here are 2 cases:
        // 1) Mouse is over column header
        //    OnClick will be called
        // 2) Mouse is over gripper
        //    OnClick won't be called, only when the mouse leaves the gripper and move to header, OnClick will be called.
        private bool HandleIsMouseOverChanged()
        {
            if (ClickMode == ClickMode.Hover)
            {
                if (IsMouseOver &&
                    //1) Gripper doesn't exist; 2) Gripper exists and Mouse isn't on Gripper;
                    (_headerGripper == null || !_headerGripper.IsMouseOver))
                {
                    // Hovering over the button will click in the OnHover click mode
                    SetValue(IsPressedPropertyKey, BooleanBoxes.Box(true));
                    OnClick();
                }
                else
                {
                    ClearValue(IsPressedPropertyKey);
                }
                return true;
            }
            return false;
        }

        // When mouse enters/leaves gripper, recall HandleIsMouseOverChanged to verify is mouse over header or not
        private void OnGripperMouseEnterLeave(object sender, MouseEventArgs e)
        {
            HandleIsMouseOverChanged();
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        #region SplitCursor

        private Cursor SplitCursor
        {
            get
            {
                if (_splitCursorCache == null)
                {
                    _splitCursorCache = GetCursor(c_SPLIT);
                }
                return _splitCursorCache;
            }
        }

        static private Cursor _splitCursorCache = null;

        #endregion SplitCursor

        #region SplitOpenCursor

        private Cursor SplitOpenCursor
        {
            get
            {
                if (_splitOpenCursorCache == null)
                {
                    _splitOpenCursorCache = GetCursor(c_SPLITOPEN);
                }
                return _splitOpenCursorCache;
            }
        }

        static private Cursor _splitOpenCursorCache = null;

        #endregion SplitOpenCursor

        // is clicked by access key or automation
        private bool IsAccessKeyOrAutomation
        {
            get { return GetFlag(Flags.IsAccessKeyOrAutomation); }
            set { SetFlag(Flags.IsAccessKeyOrAutomation, value); }
        }

        private double ColumnActualWidth
        {
            get { return (Column != null ? Column.ActualWidth : ActualWidth); }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// StyleSetByUser: the value of Style property is set by user.
        /// IgnoreStyle: the OnStyleChanged is triggered by HeaderRowPresenter,
        /// not by user. Don't turn on the StyleSetByUser flag.
        /// And so on
        /// (Only for user provided header. Ignored for internal generated header)
        ///
        /// Go to UpdateProperty and OnPropetyChanged for how these flags work.
        /// </summary>
        [Flags]
        private enum Flags
        {
            // IgnoreXXX can't be combined into one flag.
            // Reason:
            // Define a Style with ContentTemplate and assign it to GridViewColumn.HeaderContainerStyle property. GridViewColumnHeader.OnPropertyChagned method will be called twice.
            // The first call is for ContentTemplate property. In this call, IgnoreContentTemplate is false.
            // The second call is for Style property. In this call, IgnoreStyle is true.
            // One flag can’t distinguish them.
            None                                = 0,
            StyleSetByUser                      = 0x00000001,
            IgnoreStyle                         = 0x00000002,
            ContentTemplateSetByUser            = 0x00000004,
            IgnoreContentTemplate               = 0x00000008,
            ContentTemplateSelectorSetByUser    = 0x00000010,
            IgnoreContentTemplateSelector       = 0x00000020,
            ContextMenuSetByUser                = 0x00000040,
            IgnoreContextMenu                   = 0x00000080,
            ToolTipSetByUser                    = 0x00000100,
            IgnoreToolTip                       = 0x00000200,

            SuppressClickEvent                  = 0x00000400,
            IsInternalGenerated                 = 0x00000800,
            IsAccessKeyOrAutomation             = 0x00001000,

            ContentStringFormatSetByUser        = 0x00002000,
            IgnoreContentStringFormat           = 0x00004000,
        }

        private Flags _flags;

        private Thumb _headerGripper;

        private double _originalWidth;

        // canvas for floating header
        private Canvas _floatingHeaderCanvas;

        private GridViewColumnHeader _srcHeader;

        // cursor id in embedded win32 resource
        private const int c_SPLIT = 100;
        private const int c_SPLITOPEN = 101;

        // Part name used in the style. The class TemplatePartAttribute should use the same name
        private const string HeaderGripperTemplateName = "PART_HeaderGripper";
        private const string FloatingHeaderCanvasTemplateName = "PART_FloatingHeaderCanvas";

        #endregion Private Fields
    }
}

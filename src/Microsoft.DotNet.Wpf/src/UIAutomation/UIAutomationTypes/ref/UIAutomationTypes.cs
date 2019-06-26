namespace System.Windows.Automation
{
    public sealed partial class AsyncContentLoadedEventArgs : System.Windows.Automation.AutomationEventArgs
    {
        public AsyncContentLoadedEventArgs(System.Windows.Automation.AsyncContentLoadedState asyncContentState, double percentComplete) : base (default(System.Windows.Automation.AutomationEvent)) { }
        public System.Windows.Automation.AsyncContentLoadedState AsyncContentLoadedState { get { throw null; } }
        public double PercentComplete { get { throw null; } }
    }
    public enum AsyncContentLoadedState
    {
        Beginning = 0,
        Progress = 1,
        Completed = 2,
    }
    public static partial class AutomationElementIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty AcceleratorKeyProperty;
        public static readonly System.Windows.Automation.AutomationProperty AccessKeyProperty;
        public static readonly System.Windows.Automation.AutomationEvent AsyncContentLoadedEvent;
        public static readonly System.Windows.Automation.AutomationEvent AutomationFocusChangedEvent;
        public static readonly System.Windows.Automation.AutomationProperty AutomationIdProperty;
        public static readonly System.Windows.Automation.AutomationEvent AutomationPropertyChangedEvent;
        public static readonly System.Windows.Automation.AutomationProperty BoundingRectangleProperty;
        public static readonly System.Windows.Automation.AutomationProperty ClassNameProperty;
        public static readonly System.Windows.Automation.AutomationProperty ClickablePointProperty;
        public static readonly System.Windows.Automation.AutomationProperty ControllerForProperty;
        public static readonly System.Windows.Automation.AutomationProperty ControlTypeProperty;
        public static readonly System.Windows.Automation.AutomationProperty CultureProperty;
        public static readonly System.Windows.Automation.AutomationProperty FrameworkIdProperty;
        public static readonly System.Windows.Automation.AutomationProperty HasKeyboardFocusProperty;
        public static readonly System.Windows.Automation.AutomationProperty HelpTextProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsContentElementProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsControlElementProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsDockPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsEnabledProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsExpandCollapsePatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsGridItemPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsGridPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsInvokePatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsItemContainerPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsKeyboardFocusableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsMultipleViewPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsOffscreenProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsPasswordProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsRangeValuePatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsRequiredForFormProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsScrollItemPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsScrollPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsSelectionItemPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsSelectionPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsSynchronizedInputPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsTableItemPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsTablePatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsTextPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsTogglePatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsTransformPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsValuePatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsVirtualizedItemPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsWindowPatternAvailableProperty;
        public static readonly System.Windows.Automation.AutomationProperty ItemStatusProperty;
        public static readonly System.Windows.Automation.AutomationProperty ItemTypeProperty;
        public static readonly System.Windows.Automation.AutomationProperty LabeledByProperty;
        public static readonly System.Windows.Automation.AutomationEvent LayoutInvalidatedEvent;
        public static readonly System.Windows.Automation.AutomationEvent LiveRegionChangedEvent;
        public static readonly System.Windows.Automation.AutomationProperty LiveSettingProperty;
        public static readonly System.Windows.Automation.AutomationProperty LocalizedControlTypeProperty;
        public static readonly System.Windows.Automation.AutomationEvent MenuClosedEvent;
        public static readonly System.Windows.Automation.AutomationEvent MenuOpenedEvent;
        public static readonly System.Windows.Automation.AutomationProperty NameProperty;
        public static readonly System.Windows.Automation.AutomationProperty NativeWindowHandleProperty;
        public static readonly object NotSupported;
        public static readonly System.Windows.Automation.AutomationProperty OrientationProperty;
        public static readonly System.Windows.Automation.AutomationProperty PositionInSetProperty;
        public static readonly System.Windows.Automation.AutomationProperty ProcessIdProperty;
        public static readonly System.Windows.Automation.AutomationProperty RuntimeIdProperty;
        public static readonly System.Windows.Automation.AutomationProperty SizeOfSetProperty;
        public static readonly System.Windows.Automation.AutomationEvent StructureChangedEvent;
        public static readonly System.Windows.Automation.AutomationEvent ToolTipClosedEvent;
        public static readonly System.Windows.Automation.AutomationEvent ToolTipOpenedEvent;
    }
    public partial class AutomationEvent : System.Windows.Automation.AutomationIdentifier
    {
        internal AutomationEvent() { }
        public static System.Windows.Automation.AutomationEvent LookupById(int id) { throw null; }
    }
    public partial class AutomationEventArgs : System.EventArgs
    {
        public AutomationEventArgs(System.Windows.Automation.AutomationEvent eventId) { }
        public System.Windows.Automation.AutomationEvent EventId { get { throw null; } }
    }
    public delegate void AutomationEventHandler(object sender, System.Windows.Automation.AutomationEventArgs e);
    public partial class AutomationIdentifier : System.IComparable
    {
        internal AutomationIdentifier() { }
        public int Id { get { throw null; } }
        public string ProgrammaticName { get { throw null; } }
        public int CompareTo(object obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public partial class AutomationPattern : System.Windows.Automation.AutomationIdentifier
    {
        internal AutomationPattern() { }
        public static System.Windows.Automation.AutomationPattern LookupById(int id) { throw null; }
    }
    public partial class AutomationProperty : System.Windows.Automation.AutomationIdentifier
    {
        internal AutomationProperty() { }
        public static System.Windows.Automation.AutomationProperty LookupById(int id) { throw null; }
    }
    public sealed partial class AutomationPropertyChangedEventArgs : System.Windows.Automation.AutomationEventArgs
    {
        public AutomationPropertyChangedEventArgs(System.Windows.Automation.AutomationProperty property, object oldValue, object newValue) : base (default(System.Windows.Automation.AutomationEvent)) { }
        public object NewValue { get { throw null; } }
        public object OldValue { get { throw null; } }
        public System.Windows.Automation.AutomationProperty Property { get { throw null; } }
    }
    public delegate void AutomationPropertyChangedEventHandler(object sender, System.Windows.Automation.AutomationPropertyChangedEventArgs e);
    public partial class AutomationTextAttribute : System.Windows.Automation.AutomationIdentifier
    {
        internal AutomationTextAttribute() { }
        public static System.Windows.Automation.AutomationTextAttribute LookupById(int id) { throw null; }
    }
    public partial class ControlType : System.Windows.Automation.AutomationIdentifier
    {
        internal ControlType() { }
        public static readonly System.Windows.Automation.ControlType Button;
        public static readonly System.Windows.Automation.ControlType Calendar;
        public static readonly System.Windows.Automation.ControlType CheckBox;
        public static readonly System.Windows.Automation.ControlType ComboBox;
        public static readonly System.Windows.Automation.ControlType Custom;
        public static readonly System.Windows.Automation.ControlType DataGrid;
        public static readonly System.Windows.Automation.ControlType DataItem;
        public static readonly System.Windows.Automation.ControlType Document;
        public static readonly System.Windows.Automation.ControlType Edit;
        public static readonly System.Windows.Automation.ControlType Group;
        public static readonly System.Windows.Automation.ControlType Header;
        public static readonly System.Windows.Automation.ControlType HeaderItem;
        public static readonly System.Windows.Automation.ControlType Hyperlink;
        public static readonly System.Windows.Automation.ControlType Image;
        public static readonly System.Windows.Automation.ControlType List;
        public static readonly System.Windows.Automation.ControlType ListItem;
        public static readonly System.Windows.Automation.ControlType Menu;
        public static readonly System.Windows.Automation.ControlType MenuBar;
        public static readonly System.Windows.Automation.ControlType MenuItem;
        public static readonly System.Windows.Automation.ControlType Pane;
        public static readonly System.Windows.Automation.ControlType ProgressBar;
        public static readonly System.Windows.Automation.ControlType RadioButton;
        public static readonly System.Windows.Automation.ControlType ScrollBar;
        public static readonly System.Windows.Automation.ControlType Separator;
        public static readonly System.Windows.Automation.ControlType Slider;
        public static readonly System.Windows.Automation.ControlType Spinner;
        public static readonly System.Windows.Automation.ControlType SplitButton;
        public static readonly System.Windows.Automation.ControlType StatusBar;
        public static readonly System.Windows.Automation.ControlType Tab;
        public static readonly System.Windows.Automation.ControlType TabItem;
        public static readonly System.Windows.Automation.ControlType Table;
        public static readonly System.Windows.Automation.ControlType Text;
        public static readonly System.Windows.Automation.ControlType Thumb;
        public static readonly System.Windows.Automation.ControlType TitleBar;
        public static readonly System.Windows.Automation.ControlType ToolBar;
        public static readonly System.Windows.Automation.ControlType ToolTip;
        public static readonly System.Windows.Automation.ControlType Tree;
        public static readonly System.Windows.Automation.ControlType TreeItem;
        public static readonly System.Windows.Automation.ControlType Window;
        public string LocalizedControlType { get { throw null; } }
        public System.Windows.Automation.AutomationPattern[] GetNeverSupportedPatterns() { throw null; }
        public System.Windows.Automation.AutomationPattern[][] GetRequiredPatternSets() { throw null; }
        public System.Windows.Automation.AutomationProperty[] GetRequiredProperties() { throw null; }
        public static System.Windows.Automation.ControlType LookupById(int id) { throw null; }
    }
    public static partial class DockPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty DockPositionProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
    }
    public enum DockPosition
    {
        Top = 0,
        Left = 1,
        Bottom = 2,
        Right = 3,
        Fill = 4,
        None = 5,
    }
    public partial class ElementNotAvailableException : System.SystemException
    {
        public ElementNotAvailableException() { }
        public ElementNotAvailableException(System.Exception innerException) { }
        protected ElementNotAvailableException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ElementNotAvailableException(string message) { }
        public ElementNotAvailableException(string message, System.Exception innerException) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class ElementNotEnabledException : System.InvalidOperationException
    {
        public ElementNotEnabledException() { }
        protected ElementNotEnabledException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ElementNotEnabledException(string message) { }
        public ElementNotEnabledException(string message, System.Exception innerException) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public static partial class ExpandCollapsePatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty ExpandCollapseStateProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
    }
    public enum ExpandCollapseState
    {
        Collapsed = 0,
        Expanded = 1,
        PartiallyExpanded = 2,
        LeafNode = 3,
    }
    public static partial class GridItemPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty ColumnProperty;
        public static readonly System.Windows.Automation.AutomationProperty ColumnSpanProperty;
        public static readonly System.Windows.Automation.AutomationProperty ContainingGridProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty RowProperty;
        public static readonly System.Windows.Automation.AutomationProperty RowSpanProperty;
    }
    public static partial class GridPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty ColumnCountProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty RowCountProperty;
    }
    public static partial class InvokePatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationEvent InvokedEvent;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
    }
    public static partial class ItemContainerPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
    }
    public static partial class MultipleViewPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty CurrentViewProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty SupportedViewsProperty;
    }
    public partial class NoClickablePointException : System.Exception
    {
        public NoClickablePointException() { }
        protected NoClickablePointException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public NoClickablePointException(string message) { }
        public NoClickablePointException(string message, System.Exception innerException) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public enum OrientationType
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
    }
    public partial class ProxyAssemblyNotLoadedException : System.Exception
    {
        public ProxyAssemblyNotLoadedException() { }
        protected ProxyAssemblyNotLoadedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ProxyAssemblyNotLoadedException(string message) { }
        public ProxyAssemblyNotLoadedException(string message, System.Exception innerException) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public static partial class RangeValuePatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty IsReadOnlyProperty;
        public static readonly System.Windows.Automation.AutomationProperty LargeChangeProperty;
        public static readonly System.Windows.Automation.AutomationProperty MaximumProperty;
        public static readonly System.Windows.Automation.AutomationProperty MinimumProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty SmallChangeProperty;
        public static readonly System.Windows.Automation.AutomationProperty ValueProperty;
    }
    public enum RowOrColumnMajor
    {
        RowMajor = 0,
        ColumnMajor = 1,
        Indeterminate = 2,
    }
    public enum ScrollAmount
    {
        LargeDecrement = 0,
        SmallDecrement = 1,
        NoAmount = 2,
        LargeIncrement = 3,
        SmallIncrement = 4,
    }
    public static partial class ScrollItemPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
    }
    public static partial class ScrollPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty HorizontallyScrollableProperty;
        public static readonly System.Windows.Automation.AutomationProperty HorizontalScrollPercentProperty;
        public static readonly System.Windows.Automation.AutomationProperty HorizontalViewSizeProperty;
        public const double NoScroll = -1;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty VerticallyScrollableProperty;
        public static readonly System.Windows.Automation.AutomationProperty VerticalScrollPercentProperty;
        public static readonly System.Windows.Automation.AutomationProperty VerticalViewSizeProperty;
    }
    public static partial class SelectionItemPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationEvent ElementAddedToSelectionEvent;
        public static readonly System.Windows.Automation.AutomationEvent ElementRemovedFromSelectionEvent;
        public static readonly System.Windows.Automation.AutomationEvent ElementSelectedEvent;
        public static readonly System.Windows.Automation.AutomationProperty IsSelectedProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty SelectionContainerProperty;
    }
    public static partial class SelectionPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty CanSelectMultipleProperty;
        public static readonly System.Windows.Automation.AutomationEvent InvalidatedEvent;
        public static readonly System.Windows.Automation.AutomationProperty IsSelectionRequiredProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty SelectionProperty;
    }
    public sealed partial class StructureChangedEventArgs : System.Windows.Automation.AutomationEventArgs
    {
        public StructureChangedEventArgs(System.Windows.Automation.StructureChangeType structureChangeType, int[] runtimeId) : base (default(System.Windows.Automation.AutomationEvent)) { }
        public System.Windows.Automation.StructureChangeType StructureChangeType { get { throw null; } }
        public int[] GetRuntimeId() { throw null; }
    }
    public delegate void StructureChangedEventHandler(object sender, System.Windows.Automation.StructureChangedEventArgs e);
    public enum StructureChangeType
    {
        ChildAdded = 0,
        ChildRemoved = 1,
        ChildrenInvalidated = 2,
        ChildrenBulkAdded = 3,
        ChildrenBulkRemoved = 4,
        ChildrenReordered = 5,
    }
    [System.FlagsAttribute]
    public enum SupportedTextSelection
    {
        None = 0,
        Single = 1,
        Multiple = 2,
    }
    public static partial class SynchronizedInputPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationEvent InputDiscardedEvent;
        public static readonly System.Windows.Automation.AutomationEvent InputReachedOtherElementEvent;
        public static readonly System.Windows.Automation.AutomationEvent InputReachedTargetEvent;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
    }
    public enum SynchronizedInputType
    {
        KeyUp = 1,
        KeyDown = 2,
        MouseLeftButtonUp = 4,
        MouseLeftButtonDown = 8,
        MouseRightButtonUp = 16,
        MouseRightButtonDown = 32,
    }
    public static partial class TableItemPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty ColumnHeaderItemsProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty RowHeaderItemsProperty;
    }
    public static partial class TablePatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty ColumnHeadersProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty RowHeadersProperty;
        public static readonly System.Windows.Automation.AutomationProperty RowOrColumnMajorProperty;
    }
    public static partial class TextPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationTextAttribute AnimationStyleAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute BackgroundColorAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute BulletStyleAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute CapStyleAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute CultureAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute FontNameAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute FontSizeAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute FontWeightAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute ForegroundColorAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute HorizontalTextAlignmentAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute IndentationFirstLineAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute IndentationLeadingAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute IndentationTrailingAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute IsHiddenAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute IsItalicAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute IsReadOnlyAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute IsSubscriptAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute IsSuperscriptAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute MarginBottomAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute MarginLeadingAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute MarginTopAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute MarginTrailingAttribute;
        public static readonly object MixedAttributeValue;
        public static readonly System.Windows.Automation.AutomationTextAttribute OutlineStylesAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute OverlineColorAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute OverlineStyleAttribute;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationTextAttribute StrikethroughColorAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute StrikethroughStyleAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute TabsAttribute;
        public static readonly System.Windows.Automation.AutomationEvent TextChangedEvent;
        public static readonly System.Windows.Automation.AutomationTextAttribute TextFlowDirectionsAttribute;
        public static readonly System.Windows.Automation.AutomationEvent TextSelectionChangedEvent;
        public static readonly System.Windows.Automation.AutomationTextAttribute UnderlineColorAttribute;
        public static readonly System.Windows.Automation.AutomationTextAttribute UnderlineStyleAttribute;
    }
    public static partial class TogglePatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty ToggleStateProperty;
    }
    public enum ToggleState
    {
        Off = 0,
        On = 1,
        Indeterminate = 2,
    }
    public static partial class TransformPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty CanMoveProperty;
        public static readonly System.Windows.Automation.AutomationProperty CanResizeProperty;
        public static readonly System.Windows.Automation.AutomationProperty CanRotateProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
    }
    [System.FlagsAttribute]
    public enum TreeScope
    {
        Element = 1,
        Children = 2,
        Descendants = 4,
        Subtree = 7,
        Parent = 8,
        Ancestors = 16,
    }
    public static partial class ValuePatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty IsReadOnlyProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty ValueProperty;
    }
    public static partial class VirtualizedItemPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
    }
    public sealed partial class WindowClosedEventArgs : System.Windows.Automation.AutomationEventArgs
    {
        public WindowClosedEventArgs(int[] runtimeId) : base (default(System.Windows.Automation.AutomationEvent)) { }
        public int[] GetRuntimeId() { throw null; }
    }
    public enum WindowInteractionState
    {
        Running = 0,
        Closing = 1,
        ReadyForUserInteraction = 2,
        BlockedByModalWindow = 3,
        NotResponding = 4,
    }
    public static partial class WindowPatternIdentifiers
    {
        public static readonly System.Windows.Automation.AutomationProperty CanMaximizeProperty;
        public static readonly System.Windows.Automation.AutomationProperty CanMinimizeProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsModalProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsTopmostProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationEvent WindowClosedEvent;
        public static readonly System.Windows.Automation.AutomationProperty WindowInteractionStateProperty;
        public static readonly System.Windows.Automation.AutomationEvent WindowOpenedEvent;
        public static readonly System.Windows.Automation.AutomationProperty WindowVisualStateProperty;
    }
    public enum WindowVisualState
    {
        Normal = 0,
        Maximized = 1,
        Minimized = 2,
    }
}
namespace System.Windows.Automation.Text
{
    public enum AnimationStyle
    {
        Other = -1,
        None = 0,
        LasVegasLights = 1,
        BlinkingBackground = 2,
        SparkleText = 3,
        MarchingBlackAnts = 4,
        MarchingRedAnts = 5,
        Shimmer = 6,
    }
    public enum BulletStyle
    {
        Other = -1,
        None = 0,
        HollowRoundBullet = 1,
        FilledRoundBullet = 2,
        HollowSquareBullet = 3,
        FilledSquareBullet = 4,
        DashBullet = 5,
    }
    public enum CapStyle
    {
        Other = -1,
        None = 0,
        SmallCap = 1,
        AllCap = 2,
        AllPetiteCaps = 3,
        PetiteCaps = 4,
        Unicase = 5,
        Titling = 6,
    }
    [System.FlagsAttribute]
    public enum FlowDirections
    {
        Default = 0,
        RightToLeft = 1,
        BottomToTop = 2,
        Vertical = 4,
    }
    public enum HorizontalTextAlignment
    {
        Left = 0,
        Centered = 1,
        Right = 2,
        Justified = 3,
    }
    [System.FlagsAttribute]
    public enum OutlineStyles
    {
        None = 0,
        Outline = 1,
        Shadow = 2,
        Engraved = 4,
        Embossed = 8,
    }
    public enum TextDecorationLineStyle
    {
        Other = -1,
        None = 0,
        Single = 1,
        WordsOnly = 2,
        Double = 3,
        Dot = 4,
        Dash = 5,
        DashDot = 6,
        DashDotDot = 7,
        Wavy = 8,
        ThickSingle = 9,
        DoubleWavy = 11,
        ThickWavy = 12,
        LongDash = 13,
        ThickDash = 14,
        ThickDashDot = 15,
        ThickDashDotDot = 16,
        ThickDot = 17,
        ThickLongDash = 18,
    }
    public enum TextPatternRangeEndpoint
    {
        Start = 0,
        End = 1,
    }
    public enum TextUnit
    {
        Character = 0,
        Format = 1,
        Word = 2,
        Line = 3,
        Paragraph = 4,
        Page = 5,
        Document = 6,
    }
}

namespace System.Windows.Automation
{
    public partial class AndCondition : System.Windows.Automation.Condition
    {
        public AndCondition(params System.Windows.Automation.Condition[] conditions) { }
        public System.Windows.Automation.Condition[] GetConditions() { throw null; }
    }
    public static partial class Automation
    {
        public static readonly System.Windows.Automation.Condition ContentViewCondition;
        public static readonly System.Windows.Automation.Condition ControlViewCondition;
        public static readonly System.Windows.Automation.Condition RawViewCondition;
        public static void AddAutomationEventHandler(System.Windows.Automation.AutomationEvent eventId, System.Windows.Automation.AutomationElement element, System.Windows.Automation.TreeScope scope, System.Windows.Automation.AutomationEventHandler eventHandler) { }
        public static void AddAutomationFocusChangedEventHandler(System.Windows.Automation.AutomationFocusChangedEventHandler eventHandler) { }
        public static void AddAutomationPropertyChangedEventHandler(System.Windows.Automation.AutomationElement element, System.Windows.Automation.TreeScope scope, System.Windows.Automation.AutomationPropertyChangedEventHandler eventHandler, params System.Windows.Automation.AutomationProperty[] properties) { }
        public static void AddStructureChangedEventHandler(System.Windows.Automation.AutomationElement element, System.Windows.Automation.TreeScope scope, System.Windows.Automation.StructureChangedEventHandler eventHandler) { }
        public static bool Compare(int[] runtimeId1, int[] runtimeId2) { throw null; }
        public static bool Compare(System.Windows.Automation.AutomationElement el1, System.Windows.Automation.AutomationElement el2) { throw null; }
        public static string PatternName(System.Windows.Automation.AutomationPattern pattern) { throw null; }
        public static string PropertyName(System.Windows.Automation.AutomationProperty property) { throw null; }
        public static void RemoveAllEventHandlers() { }
        public static void RemoveAutomationEventHandler(System.Windows.Automation.AutomationEvent eventId, System.Windows.Automation.AutomationElement element, System.Windows.Automation.AutomationEventHandler eventHandler) { }
        public static void RemoveAutomationFocusChangedEventHandler(System.Windows.Automation.AutomationFocusChangedEventHandler eventHandler) { }
        public static void RemoveAutomationPropertyChangedEventHandler(System.Windows.Automation.AutomationElement element, System.Windows.Automation.AutomationPropertyChangedEventHandler eventHandler) { }
        public static void RemoveStructureChangedEventHandler(System.Windows.Automation.AutomationElement element, System.Windows.Automation.StructureChangedEventHandler eventHandler) { }
    }
    public sealed partial class AutomationElement
    {
        internal AutomationElement() { }
        public static readonly System.Windows.Automation.AutomationProperty AcceleratorKeyProperty;
        public static readonly System.Windows.Automation.AutomationProperty AccessKeyProperty;
        public static readonly System.Windows.Automation.AutomationEvent AsyncContentLoadedEvent;
        public static readonly System.Windows.Automation.AutomationEvent AutomationFocusChangedEvent;
        public static readonly System.Windows.Automation.AutomationProperty AutomationIdProperty;
        public static readonly System.Windows.Automation.AutomationEvent AutomationPropertyChangedEvent;
        public static readonly System.Windows.Automation.AutomationProperty BoundingRectangleProperty;
        public static readonly System.Windows.Automation.AutomationProperty ClassNameProperty;
        public static readonly System.Windows.Automation.AutomationProperty ClickablePointProperty;
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
        public System.Windows.Automation.AutomationElement.AutomationElementInformation Cached { get { throw null; } }
        public System.Windows.Automation.AutomationElementCollection CachedChildren { get { throw null; } }
        public System.Windows.Automation.AutomationElement CachedParent { get { throw null; } }
        public System.Windows.Automation.AutomationElement.AutomationElementInformation Current { get { throw null; } }
        public static System.Windows.Automation.AutomationElement FocusedElement { get { throw null; } }
        public static System.Windows.Automation.AutomationElement RootElement { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        ~AutomationElement() { }
        public System.Windows.Automation.AutomationElementCollection FindAll(System.Windows.Automation.TreeScope scope, System.Windows.Automation.Condition condition) { throw null; }
        public System.Windows.Automation.AutomationElement FindFirst(System.Windows.Automation.TreeScope scope, System.Windows.Automation.Condition condition) { throw null; }
        public static System.Windows.Automation.AutomationElement FromHandle(System.IntPtr hwnd) { throw null; }
        public static System.Windows.Automation.AutomationElement FromLocalProvider(System.Windows.Automation.Provider.IRawElementProviderSimple localImpl) { throw null; }
        public static System.Windows.Automation.AutomationElement FromPoint(System.Windows.Point pt) { throw null; }
        public object GetCachedPattern(System.Windows.Automation.AutomationPattern pattern) { throw null; }
        public object GetCachedPropertyValue(System.Windows.Automation.AutomationProperty property) { throw null; }
        public object GetCachedPropertyValue(System.Windows.Automation.AutomationProperty property, bool ignoreDefaultValue) { throw null; }
        public System.Windows.Point GetClickablePoint() { throw null; }
        public object GetCurrentPattern(System.Windows.Automation.AutomationPattern pattern) { throw null; }
        public object GetCurrentPropertyValue(System.Windows.Automation.AutomationProperty property) { throw null; }
        public object GetCurrentPropertyValue(System.Windows.Automation.AutomationProperty property, bool ignoreDefaultValue) { throw null; }
        public override int GetHashCode() { throw null; }
        public int[] GetRuntimeId() { throw null; }
        public System.Windows.Automation.AutomationPattern[] GetSupportedPatterns() { throw null; }
        public System.Windows.Automation.AutomationProperty[] GetSupportedProperties() { throw null; }
        public System.Windows.Automation.AutomationElement GetUpdatedCache(System.Windows.Automation.CacheRequest request) { throw null; }
        public static bool operator ==(System.Windows.Automation.AutomationElement left, System.Windows.Automation.AutomationElement right) { throw null; }
        public static bool operator !=(System.Windows.Automation.AutomationElement left, System.Windows.Automation.AutomationElement right) { throw null; }
        public void SetFocus() { }
        public bool TryGetCachedPattern(System.Windows.Automation.AutomationPattern pattern, out object patternObject) { throw null; }
        public bool TryGetClickablePoint(out System.Windows.Point pt) { throw null; }
        public bool TryGetCurrentPattern(System.Windows.Automation.AutomationPattern pattern, out object patternObject) { throw null; }
        public partial struct AutomationElementInformation
        {
            public string AcceleratorKey { get { throw null; } }
            public string AccessKey { get { throw null; } }
            public string AutomationId { get { throw null; } }
            public System.Windows.Rect BoundingRectangle { get { throw null; } }
            public string ClassName { get { throw null; } }
            public System.Windows.Automation.ControlType ControlType { get { throw null; } }
            public string FrameworkId { get { throw null; } }
            public bool HasKeyboardFocus { get { throw null; } }
            public string HelpText { get { throw null; } }
            public bool IsContentElement { get { throw null; } }
            public bool IsControlElement { get { throw null; } }
            public bool IsEnabled { get { throw null; } }
            public bool IsKeyboardFocusable { get { throw null; } }
            public bool IsOffscreen { get { throw null; } }
            public bool IsPassword { get { throw null; } }
            public bool IsRequiredForForm { get { throw null; } }
            public string ItemStatus { get { throw null; } }
            public string ItemType { get { throw null; } }
            public System.Windows.Automation.AutomationElement LabeledBy { get { throw null; } }
            public string LocalizedControlType { get { throw null; } }
            public string Name { get { throw null; } }
            public int NativeWindowHandle { get { throw null; } }
            public System.Windows.Automation.OrientationType Orientation { get { throw null; } }
            public int ProcessId { get { throw null; } }
        }
    }
    public partial class AutomationElementCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal AutomationElementCollection() { }
        public int Count { get { throw null; } }
        public virtual bool IsSynchronized { get { throw null; } }
        public System.Windows.Automation.AutomationElement this[int index] { get { throw null; } }
        public virtual object SyncRoot { get { throw null; } }
        public virtual void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Windows.Automation.AutomationElement[] array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
    }
    public enum AutomationElementMode
    {
        None = 0,
        Full = 1,
    }
    public partial class AutomationFocusChangedEventArgs : System.Windows.Automation.AutomationEventArgs
    {
        public AutomationFocusChangedEventArgs(int idObject, int idChild) : base (default(System.Windows.Automation.AutomationEvent)) { }
        public int ChildId { get { throw null; } }
        public int ObjectId { get { throw null; } }
    }
    public delegate void AutomationFocusChangedEventHandler(object sender, System.Windows.Automation.AutomationFocusChangedEventArgs e);
    public partial class BasePattern
    {
        internal BasePattern() { }
        ~BasePattern() { }
    }
    public sealed partial class CacheRequest
    {
        public CacheRequest() { }
        public System.Windows.Automation.AutomationElementMode AutomationElementMode { get { throw null; } set { } }
        public static System.Windows.Automation.CacheRequest Current { get { throw null; } }
        public System.Windows.Automation.Condition TreeFilter { get { throw null; } set { } }
        public System.Windows.Automation.TreeScope TreeScope { get { throw null; } set { } }
        public System.IDisposable Activate() { throw null; }
        public void Add(System.Windows.Automation.AutomationPattern pattern) { }
        public void Add(System.Windows.Automation.AutomationProperty property) { }
        public System.Windows.Automation.CacheRequest Clone() { throw null; }
        public void Pop() { }
        public void Push() { }
    }
    public static partial class ClientSettings
    {
        public static void RegisterClientSideProviderAssembly(System.Reflection.AssemblyName assemblyName) { }
        public static void RegisterClientSideProviders(System.Windows.Automation.ClientSideProviderDescription[] clientSideProviderDescription) { }
    }
    public partial struct ClientSideProviderDescription
    {
        public ClientSideProviderDescription(System.Windows.Automation.ClientSideProviderFactoryCallback clientSideProviderFactoryCallback, string className) { throw null; }
        public ClientSideProviderDescription(System.Windows.Automation.ClientSideProviderFactoryCallback clientSideProviderFactoryCallback, string className, string imageName, System.Windows.Automation.ClientSideProviderMatchIndicator flags) { throw null; }
        public string ClassName { get { throw null; } }
        public System.Windows.Automation.ClientSideProviderFactoryCallback ClientSideProviderFactoryCallback { get { throw null; } }
        public System.Windows.Automation.ClientSideProviderMatchIndicator Flags { get { throw null; } }
        public string ImageName { get { throw null; } }
    }
    public delegate System.Windows.Automation.Provider.IRawElementProviderSimple ClientSideProviderFactoryCallback(System.IntPtr hwnd, int idChild, int idObject);
    [System.FlagsAttribute]
    public enum ClientSideProviderMatchIndicator
    {
        None = 0,
        AllowSubstringMatch = 1,
        DisallowBaseClassNameMatch = 2,
    }
    public abstract partial class Condition
    {
        internal Condition() { }
        public static readonly System.Windows.Automation.Condition FalseCondition;
        public static readonly System.Windows.Automation.Condition TrueCondition;
    }
    public partial class DockPattern : System.Windows.Automation.BasePattern
    {
        internal DockPattern() { }
        public static readonly System.Windows.Automation.AutomationProperty DockPositionProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public System.Windows.Automation.DockPattern.DockPatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.DockPattern.DockPatternInformation Current { get { throw null; } }
        public void SetDockPosition(System.Windows.Automation.DockPosition dockPosition) { }
        public partial struct DockPatternInformation
        {
            public System.Windows.Automation.DockPosition DockPosition { get { throw null; } }
        }
    }
    public partial class ExpandCollapsePattern : System.Windows.Automation.BasePattern
    {
        internal ExpandCollapsePattern() { }
        public static readonly System.Windows.Automation.AutomationProperty ExpandCollapseStateProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public System.Windows.Automation.ExpandCollapsePattern.ExpandCollapsePatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.ExpandCollapsePattern.ExpandCollapsePatternInformation Current { get { throw null; } }
        public void Collapse() { }
        public void Expand() { }
        public partial struct ExpandCollapsePatternInformation
        {
            public System.Windows.Automation.ExpandCollapseState ExpandCollapseState { get { throw null; } }
        }
    }
    public partial class GridItemPattern : System.Windows.Automation.BasePattern
    {
        internal GridItemPattern() { }
        public static readonly System.Windows.Automation.AutomationProperty ColumnProperty;
        public static readonly System.Windows.Automation.AutomationProperty ColumnSpanProperty;
        public static readonly System.Windows.Automation.AutomationProperty ContainingGridProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty RowProperty;
        public static readonly System.Windows.Automation.AutomationProperty RowSpanProperty;
        public System.Windows.Automation.GridItemPattern.GridItemPatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.GridItemPattern.GridItemPatternInformation Current { get { throw null; } }
        public partial struct GridItemPatternInformation
        {
            public int Column { get { throw null; } }
            public int ColumnSpan { get { throw null; } }
            public System.Windows.Automation.AutomationElement ContainingGrid { get { throw null; } }
            public int Row { get { throw null; } }
            public int RowSpan { get { throw null; } }
        }
    }
    public partial class GridPattern : System.Windows.Automation.BasePattern
    {
        internal GridPattern() { }
        public static readonly System.Windows.Automation.AutomationProperty ColumnCountProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty RowCountProperty;
        public System.Windows.Automation.GridPattern.GridPatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.GridPattern.GridPatternInformation Current { get { throw null; } }
        public System.Windows.Automation.AutomationElement GetItem(int row, int column) { throw null; }
        public partial struct GridPatternInformation
        {
            public int ColumnCount { get { throw null; } }
            public int RowCount { get { throw null; } }
        }
    }
    public partial class InvokePattern : System.Windows.Automation.BasePattern
    {
        internal InvokePattern() { }
        public static readonly System.Windows.Automation.AutomationEvent InvokedEvent;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public void Invoke() { }
    }
    public partial class ItemContainerPattern : System.Windows.Automation.BasePattern
    {
        internal ItemContainerPattern() { }
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public System.Windows.Automation.AutomationElement FindItemByProperty(System.Windows.Automation.AutomationElement startAfter, System.Windows.Automation.AutomationProperty property, object value) { throw null; }
    }
    public partial class MultipleViewPattern : System.Windows.Automation.BasePattern
    {
        internal MultipleViewPattern() { }
        public static readonly System.Windows.Automation.AutomationProperty CurrentViewProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty SupportedViewsProperty;
        public System.Windows.Automation.MultipleViewPattern.MultipleViewPatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.MultipleViewPattern.MultipleViewPatternInformation Current { get { throw null; } }
        public string GetViewName(int viewId) { throw null; }
        public void SetCurrentView(int viewId) { }
        public partial struct MultipleViewPatternInformation
        {
            public int CurrentView { get { throw null; } }
            public int[] GetSupportedViews() { throw null; }
        }
    }
    public partial class NotCondition : System.Windows.Automation.Condition
    {
        public NotCondition(System.Windows.Automation.Condition condition) { }
        public System.Windows.Automation.Condition Condition { get { throw null; } }
    }
    public partial class OrCondition : System.Windows.Automation.Condition
    {
        public OrCondition(params System.Windows.Automation.Condition[] conditions) { }
        public System.Windows.Automation.Condition[] GetConditions() { throw null; }
    }
    public partial class PropertyCondition : System.Windows.Automation.Condition
    {
        public PropertyCondition(System.Windows.Automation.AutomationProperty property, object value) { }
        public PropertyCondition(System.Windows.Automation.AutomationProperty property, object value, System.Windows.Automation.PropertyConditionFlags flags) { }
        public System.Windows.Automation.PropertyConditionFlags Flags { get { throw null; } }
        public System.Windows.Automation.AutomationProperty Property { get { throw null; } }
        public object Value { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum PropertyConditionFlags
    {
        None = 0,
        IgnoreCase = 1,
    }
    public partial class RangeValuePattern : System.Windows.Automation.BasePattern
    {
        internal RangeValuePattern() { }
        public static readonly System.Windows.Automation.AutomationProperty IsReadOnlyProperty;
        public static readonly System.Windows.Automation.AutomationProperty LargeChangeProperty;
        public static readonly System.Windows.Automation.AutomationProperty MaximumProperty;
        public static readonly System.Windows.Automation.AutomationProperty MinimumProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty SmallChangeProperty;
        public static readonly System.Windows.Automation.AutomationProperty ValueProperty;
        public System.Windows.Automation.RangeValuePattern.RangeValuePatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.RangeValuePattern.RangeValuePatternInformation Current { get { throw null; } }
        public void SetValue(double value) { }
        public partial struct RangeValuePatternInformation
        {
            public bool IsReadOnly { get { throw null; } }
            public double LargeChange { get { throw null; } }
            public double Maximum { get { throw null; } }
            public double Minimum { get { throw null; } }
            public double SmallChange { get { throw null; } }
            public double Value { get { throw null; } }
        }
    }
    public partial class ScrollItemPattern : System.Windows.Automation.BasePattern
    {
        internal ScrollItemPattern() { }
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public void ScrollIntoView() { }
    }
    public partial class ScrollPattern : System.Windows.Automation.BasePattern
    {
        internal ScrollPattern() { }
        public static readonly System.Windows.Automation.AutomationProperty HorizontallyScrollableProperty;
        public static readonly System.Windows.Automation.AutomationProperty HorizontalScrollPercentProperty;
        public static readonly System.Windows.Automation.AutomationProperty HorizontalViewSizeProperty;
        public const double NoScroll = -1;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty VerticallyScrollableProperty;
        public static readonly System.Windows.Automation.AutomationProperty VerticalScrollPercentProperty;
        public static readonly System.Windows.Automation.AutomationProperty VerticalViewSizeProperty;
        public System.Windows.Automation.ScrollPattern.ScrollPatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.ScrollPattern.ScrollPatternInformation Current { get { throw null; } }
        public void Scroll(System.Windows.Automation.ScrollAmount horizontalAmount, System.Windows.Automation.ScrollAmount verticalAmount) { }
        public void ScrollHorizontal(System.Windows.Automation.ScrollAmount amount) { }
        public void ScrollVertical(System.Windows.Automation.ScrollAmount amount) { }
        public void SetScrollPercent(double horizontalPercent, double verticalPercent) { }
        public partial struct ScrollPatternInformation
        {
            public bool HorizontallyScrollable { get { throw null; } }
            public double HorizontalScrollPercent { get { throw null; } }
            public double HorizontalViewSize { get { throw null; } }
            public bool VerticallyScrollable { get { throw null; } }
            public double VerticalScrollPercent { get { throw null; } }
            public double VerticalViewSize { get { throw null; } }
        }
    }
    public partial class SelectionItemPattern : System.Windows.Automation.BasePattern
    {
        internal SelectionItemPattern() { }
        public static readonly System.Windows.Automation.AutomationEvent ElementAddedToSelectionEvent;
        public static readonly System.Windows.Automation.AutomationEvent ElementRemovedFromSelectionEvent;
        public static readonly System.Windows.Automation.AutomationEvent ElementSelectedEvent;
        public static readonly System.Windows.Automation.AutomationProperty IsSelectedProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty SelectionContainerProperty;
        public System.Windows.Automation.SelectionItemPattern.SelectionItemPatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.SelectionItemPattern.SelectionItemPatternInformation Current { get { throw null; } }
        public void AddToSelection() { }
        public void RemoveFromSelection() { }
        public void Select() { }
        public partial struct SelectionItemPatternInformation
        {
            public bool IsSelected { get { throw null; } }
            public System.Windows.Automation.AutomationElement SelectionContainer { get { throw null; } }
        }
    }
    public partial class SelectionPattern : System.Windows.Automation.BasePattern
    {
        internal SelectionPattern() { }
        public static readonly System.Windows.Automation.AutomationProperty CanSelectMultipleProperty;
        public static readonly System.Windows.Automation.AutomationEvent InvalidatedEvent;
        public static readonly System.Windows.Automation.AutomationProperty IsSelectionRequiredProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty SelectionProperty;
        public System.Windows.Automation.SelectionPattern.SelectionPatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.SelectionPattern.SelectionPatternInformation Current { get { throw null; } }
        public partial struct SelectionPatternInformation
        {
            public bool CanSelectMultiple { get { throw null; } }
            public bool IsSelectionRequired { get { throw null; } }
            public System.Windows.Automation.AutomationElement[] GetSelection() { throw null; }
        }
    }
    public partial class SynchronizedInputPattern : System.Windows.Automation.BasePattern
    {
        internal SynchronizedInputPattern() { }
        public static readonly System.Windows.Automation.AutomationEvent InputDiscardedEvent;
        public static readonly System.Windows.Automation.AutomationEvent InputReachedOtherElementEvent;
        public static readonly System.Windows.Automation.AutomationEvent InputReachedTargetEvent;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public void Cancel() { }
        public void StartListening(System.Windows.Automation.SynchronizedInputType inputType) { }
    }
    public partial class TableItemPattern : System.Windows.Automation.GridItemPattern
    {
        internal TableItemPattern() { }
        public static readonly System.Windows.Automation.AutomationProperty ColumnHeaderItemsProperty;
        public static readonly new System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty RowHeaderItemsProperty;
        public new System.Windows.Automation.TableItemPattern.TableItemPatternInformation Cached { get { throw null; } }
        public new System.Windows.Automation.TableItemPattern.TableItemPatternInformation Current { get { throw null; } }
        public partial struct TableItemPatternInformation
        {
            public int Column { get { throw null; } }
            public int ColumnSpan { get { throw null; } }
            public System.Windows.Automation.AutomationElement ContainingGrid { get { throw null; } }
            public int Row { get { throw null; } }
            public int RowSpan { get { throw null; } }
            public System.Windows.Automation.AutomationElement[] GetColumnHeaderItems() { throw null; }
            public System.Windows.Automation.AutomationElement[] GetRowHeaderItems() { throw null; }
        }
    }
    public partial class TablePattern : System.Windows.Automation.GridPattern
    {
        internal TablePattern() { }
        public static readonly System.Windows.Automation.AutomationProperty ColumnHeadersProperty;
        public static readonly new System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty RowHeadersProperty;
        public static readonly System.Windows.Automation.AutomationProperty RowOrColumnMajorProperty;
        public new System.Windows.Automation.TablePattern.TablePatternInformation Cached { get { throw null; } }
        public new System.Windows.Automation.TablePattern.TablePatternInformation Current { get { throw null; } }
        public partial struct TablePatternInformation
        {
            public int ColumnCount { get { throw null; } }
            public int RowCount { get { throw null; } }
            public System.Windows.Automation.RowOrColumnMajor RowOrColumnMajor { get { throw null; } }
            public System.Windows.Automation.AutomationElement[] GetColumnHeaders() { throw null; }
            public System.Windows.Automation.AutomationElement[] GetRowHeaders() { throw null; }
        }
    }
    public partial class TextPattern : System.Windows.Automation.BasePattern
    {
        internal TextPattern() { }
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
        public System.Windows.Automation.Text.TextPatternRange DocumentRange { get { throw null; } }
        public System.Windows.Automation.SupportedTextSelection SupportedTextSelection { get { throw null; } }
        public System.Windows.Automation.Text.TextPatternRange[] GetSelection() { throw null; }
        public System.Windows.Automation.Text.TextPatternRange[] GetVisibleRanges() { throw null; }
        public System.Windows.Automation.Text.TextPatternRange RangeFromChild(System.Windows.Automation.AutomationElement childElement) { throw null; }
        public System.Windows.Automation.Text.TextPatternRange RangeFromPoint(System.Windows.Point screenLocation) { throw null; }
    }
    public partial class TogglePattern : System.Windows.Automation.BasePattern
    {
        internal TogglePattern() { }
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty ToggleStateProperty;
        public System.Windows.Automation.TogglePattern.TogglePatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.TogglePattern.TogglePatternInformation Current { get { throw null; } }
        public void Toggle() { }
        public partial struct TogglePatternInformation
        {
            public System.Windows.Automation.ToggleState ToggleState { get { throw null; } }
        }
    }
    public partial class TransformPattern : System.Windows.Automation.BasePattern
    {
        internal TransformPattern() { }
        public static readonly System.Windows.Automation.AutomationProperty CanMoveProperty;
        public static readonly System.Windows.Automation.AutomationProperty CanResizeProperty;
        public static readonly System.Windows.Automation.AutomationProperty CanRotateProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public System.Windows.Automation.TransformPattern.TransformPatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.TransformPattern.TransformPatternInformation Current { get { throw null; } }
        public void Move(double x, double y) { }
        public void Resize(double width, double height) { }
        public void Rotate(double degrees) { }
        public partial struct TransformPatternInformation
        {
            public bool CanMove { get { throw null; } }
            public bool CanResize { get { throw null; } }
            public bool CanRotate { get { throw null; } }
        }
    }
    public sealed partial class TreeWalker
    {
        public static readonly System.Windows.Automation.TreeWalker ContentViewWalker;
        public static readonly System.Windows.Automation.TreeWalker ControlViewWalker;
        public static readonly System.Windows.Automation.TreeWalker RawViewWalker;
        public TreeWalker(System.Windows.Automation.Condition condition) { }
        public System.Windows.Automation.Condition Condition { get { throw null; } }
        public System.Windows.Automation.AutomationElement GetFirstChild(System.Windows.Automation.AutomationElement element) { throw null; }
        public System.Windows.Automation.AutomationElement GetFirstChild(System.Windows.Automation.AutomationElement element, System.Windows.Automation.CacheRequest request) { throw null; }
        public System.Windows.Automation.AutomationElement GetLastChild(System.Windows.Automation.AutomationElement element) { throw null; }
        public System.Windows.Automation.AutomationElement GetLastChild(System.Windows.Automation.AutomationElement element, System.Windows.Automation.CacheRequest request) { throw null; }
        public System.Windows.Automation.AutomationElement GetNextSibling(System.Windows.Automation.AutomationElement element) { throw null; }
        public System.Windows.Automation.AutomationElement GetNextSibling(System.Windows.Automation.AutomationElement element, System.Windows.Automation.CacheRequest request) { throw null; }
        public System.Windows.Automation.AutomationElement GetParent(System.Windows.Automation.AutomationElement element) { throw null; }
        public System.Windows.Automation.AutomationElement GetParent(System.Windows.Automation.AutomationElement element, System.Windows.Automation.CacheRequest request) { throw null; }
        public System.Windows.Automation.AutomationElement GetPreviousSibling(System.Windows.Automation.AutomationElement element) { throw null; }
        public System.Windows.Automation.AutomationElement GetPreviousSibling(System.Windows.Automation.AutomationElement element, System.Windows.Automation.CacheRequest request) { throw null; }
        public System.Windows.Automation.AutomationElement Normalize(System.Windows.Automation.AutomationElement element) { throw null; }
        public System.Windows.Automation.AutomationElement Normalize(System.Windows.Automation.AutomationElement element, System.Windows.Automation.CacheRequest request) { throw null; }
    }
    public partial class ValuePattern : System.Windows.Automation.BasePattern
    {
        internal ValuePattern() { }
        public static readonly System.Windows.Automation.AutomationProperty IsReadOnlyProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationProperty ValueProperty;
        public System.Windows.Automation.ValuePattern.ValuePatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.ValuePattern.ValuePatternInformation Current { get { throw null; } }
        public void SetValue(string value) { }
        public partial struct ValuePatternInformation
        {
            public bool IsReadOnly { get { throw null; } }
            public string Value { get { throw null; } }
        }
    }
    public partial class VirtualizedItemPattern : System.Windows.Automation.BasePattern
    {
        internal VirtualizedItemPattern() { }
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public void Realize() { }
    }
    public partial class WindowPattern : System.Windows.Automation.BasePattern
    {
        internal WindowPattern() { }
        public static readonly System.Windows.Automation.AutomationProperty CanMaximizeProperty;
        public static readonly System.Windows.Automation.AutomationProperty CanMinimizeProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsModalProperty;
        public static readonly System.Windows.Automation.AutomationProperty IsTopmostProperty;
        public static readonly System.Windows.Automation.AutomationPattern Pattern;
        public static readonly System.Windows.Automation.AutomationEvent WindowClosedEvent;
        public static readonly System.Windows.Automation.AutomationProperty WindowInteractionStateProperty;
        public static readonly System.Windows.Automation.AutomationEvent WindowOpenedEvent;
        public static readonly System.Windows.Automation.AutomationProperty WindowVisualStateProperty;
        public System.Windows.Automation.WindowPattern.WindowPatternInformation Cached { get { throw null; } }
        public System.Windows.Automation.WindowPattern.WindowPatternInformation Current { get { throw null; } }
        public void Close() { }
        public void SetWindowVisualState(System.Windows.Automation.WindowVisualState state) { }
        public bool WaitForInputIdle(int milliseconds) { throw null; }
        public partial struct WindowPatternInformation
        {
            public bool CanMaximize { get { throw null; } }
            public bool CanMinimize { get { throw null; } }
            public bool IsModal { get { throw null; } }
            public bool IsTopmost { get { throw null; } }
            public System.Windows.Automation.WindowInteractionState WindowInteractionState { get { throw null; } }
            public System.Windows.Automation.WindowVisualState WindowVisualState { get { throw null; } }
        }
    }
}
namespace System.Windows.Automation.Text
{
    public partial class TextPatternRange
    {
        internal TextPatternRange() { }
        public System.Windows.Automation.TextPattern TextPattern { get { throw null; } }
        public void AddToSelection() { }
        public System.Windows.Automation.Text.TextPatternRange Clone() { throw null; }
        public bool Compare(System.Windows.Automation.Text.TextPatternRange range) { throw null; }
        public int CompareEndpoints(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, System.Windows.Automation.Text.TextPatternRange targetRange, System.Windows.Automation.Text.TextPatternRangeEndpoint targetEndpoint) { throw null; }
        public void ExpandToEnclosingUnit(System.Windows.Automation.Text.TextUnit unit) { }
        public System.Windows.Automation.Text.TextPatternRange FindAttribute(System.Windows.Automation.AutomationTextAttribute attribute, object value, bool backward) { throw null; }
        public System.Windows.Automation.Text.TextPatternRange FindText(string text, bool backward, bool ignoreCase) { throw null; }
        public object GetAttributeValue(System.Windows.Automation.AutomationTextAttribute attribute) { throw null; }
        public System.Windows.Rect[] GetBoundingRectangles() { throw null; }
        public System.Windows.Automation.AutomationElement[] GetChildren() { throw null; }
        public System.Windows.Automation.AutomationElement GetEnclosingElement() { throw null; }
        public string GetText(int maxLength) { throw null; }
        public int Move(System.Windows.Automation.Text.TextUnit unit, int count) { throw null; }
        public void MoveEndpointByRange(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, System.Windows.Automation.Text.TextPatternRange targetRange, System.Windows.Automation.Text.TextPatternRangeEndpoint targetEndpoint) { }
        public int MoveEndpointByUnit(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, System.Windows.Automation.Text.TextUnit unit, int count) { throw null; }
        public void RemoveFromSelection() { }
        public void ScrollIntoView(bool alignToTop) { }
        public void Select() { }
    }
}

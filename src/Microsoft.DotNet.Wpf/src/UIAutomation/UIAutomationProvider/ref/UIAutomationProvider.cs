namespace System.Windows.Automation.Provider
{
    public static partial class AutomationInteropProvider
    {
        public const int AppendRuntimeId = 3;
        public const int InvalidateLimit = 20;
        public const int ItemsInvalidateLimit = 5;
        public const int RootObjectId = -25;
        public static bool ClientsAreListening { get { throw null; } }
        public static System.Windows.Automation.Provider.IRawElementProviderSimple HostProviderFromHandle(System.IntPtr hwnd) { throw null; }
        public static void RaiseAutomationEvent(System.Windows.Automation.AutomationEvent eventId, System.Windows.Automation.Provider.IRawElementProviderSimple provider, System.Windows.Automation.AutomationEventArgs e) { }
        public static void RaiseAutomationPropertyChangedEvent(System.Windows.Automation.Provider.IRawElementProviderSimple element, System.Windows.Automation.AutomationPropertyChangedEventArgs e) { }
        public static void RaiseStructureChangedEvent(System.Windows.Automation.Provider.IRawElementProviderSimple provider, System.Windows.Automation.StructureChangedEventArgs e) { }
        public static System.IntPtr ReturnRawElementProvider(System.IntPtr hwnd, System.IntPtr wParam, System.IntPtr lParam, System.Windows.Automation.Provider.IRawElementProviderSimple el) { throw null; }
    }
    public partial interface IDockProvider
    {
        System.Windows.Automation.DockPosition DockPosition { get; }
        void SetDockPosition(System.Windows.Automation.DockPosition dockPosition);
    }
    public partial interface IExpandCollapseProvider
    {
        System.Windows.Automation.ExpandCollapseState ExpandCollapseState { get; }
        void Collapse();
        void Expand();
    }
    public partial interface IGridItemProvider
    {
        int Column { get; }
        int ColumnSpan { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple ContainingGrid { get; }
        int Row { get; }
        int RowSpan { get; }
    }
    public partial interface IGridProvider
    {
        int ColumnCount { get; }
        int RowCount { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple GetItem(int row, int column);
    }
    public partial interface IInvokeProvider
    {
        void Invoke();
    }
    public partial interface IItemContainerProvider
    {
        System.Windows.Automation.Provider.IRawElementProviderSimple FindItemByProperty(System.Windows.Automation.Provider.IRawElementProviderSimple startAfter, int propertyId, object value);
    }
    public partial interface IMultipleViewProvider
    {
        int CurrentView { get; }
        int[] GetSupportedViews();
        string GetViewName(int viewId);
        void SetCurrentView(int viewId);
    }
    public partial interface IRangeValueProvider
    {
        bool IsReadOnly { get; }
        double LargeChange { get; }
        double Maximum { get; }
        double Minimum { get; }
        double SmallChange { get; }
        double Value { get; }
        void SetValue(double value);
    }
    public partial interface IRawElementProviderAdviseEvents : System.Windows.Automation.Provider.IRawElementProviderSimple
    {
        void AdviseEventAdded(int eventId, int[] properties);
        void AdviseEventRemoved(int eventId, int[] properties);
    }
    public partial interface IRawElementProviderFragment : System.Windows.Automation.Provider.IRawElementProviderSimple
    {
        System.Windows.Rect BoundingRectangle { get; }
        System.Windows.Automation.Provider.IRawElementProviderFragmentRoot FragmentRoot { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetEmbeddedFragmentRoots();
        int[] GetRuntimeId();
        System.Windows.Automation.Provider.IRawElementProviderFragment Navigate(System.Windows.Automation.Provider.NavigateDirection direction);
        void SetFocus();
    }
    public partial interface IRawElementProviderFragmentRoot : System.Windows.Automation.Provider.IRawElementProviderFragment, System.Windows.Automation.Provider.IRawElementProviderSimple
    {
        System.Windows.Automation.Provider.IRawElementProviderFragment ElementProviderFromPoint(double x, double y);
        System.Windows.Automation.Provider.IRawElementProviderFragment GetFocus();
    }
    public partial interface IRawElementProviderHwndOverride : System.Windows.Automation.Provider.IRawElementProviderSimple
    {
        System.Windows.Automation.Provider.IRawElementProviderSimple GetOverrideProviderForHwnd(System.IntPtr hwnd);
    }
    public partial interface IRawElementProviderSimple
    {
        System.Windows.Automation.Provider.IRawElementProviderSimple HostRawElementProvider { get; }
        System.Windows.Automation.Provider.ProviderOptions ProviderOptions { get; }
        object GetPatternProvider(int patternId);
        object GetPropertyValue(int propertyId);
    }
    public partial interface IScrollItemProvider
    {
        void ScrollIntoView();
    }
    public partial interface IScrollProvider
    {
        bool HorizontallyScrollable { get; }
        double HorizontalScrollPercent { get; }
        double HorizontalViewSize { get; }
        bool VerticallyScrollable { get; }
        double VerticalScrollPercent { get; }
        double VerticalViewSize { get; }
        void Scroll(System.Windows.Automation.ScrollAmount horizontalAmount, System.Windows.Automation.ScrollAmount verticalAmount);
        void SetScrollPercent(double horizontalPercent, double verticalPercent);
    }
    public partial interface ISelectionItemProvider
    {
        bool IsSelected { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple SelectionContainer { get; }
        void AddToSelection();
        void RemoveFromSelection();
        void Select();
    }
    public partial interface ISelectionProvider
    {
        bool CanSelectMultiple { get; }
        bool IsSelectionRequired { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetSelection();
    }
    public partial interface ISynchronizedInputProvider
    {
        void Cancel();
        void StartListening(System.Windows.Automation.SynchronizedInputType inputType);
    }
    public partial interface ITableItemProvider : System.Windows.Automation.Provider.IGridItemProvider
    {
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetColumnHeaderItems();
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetRowHeaderItems();
    }
    public partial interface ITableProvider : System.Windows.Automation.Provider.IGridProvider
    {
        System.Windows.Automation.RowOrColumnMajor RowOrColumnMajor { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetColumnHeaders();
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetRowHeaders();
    }
    public partial interface ITextProvider
    {
        System.Windows.Automation.Provider.ITextRangeProvider DocumentRange { get; }
        System.Windows.Automation.SupportedTextSelection SupportedTextSelection { get; }
        System.Windows.Automation.Provider.ITextRangeProvider[] GetSelection();
        System.Windows.Automation.Provider.ITextRangeProvider[] GetVisibleRanges();
        System.Windows.Automation.Provider.ITextRangeProvider RangeFromChild(System.Windows.Automation.Provider.IRawElementProviderSimple childElement);
        System.Windows.Automation.Provider.ITextRangeProvider RangeFromPoint(System.Windows.Point screenLocation);
    }
    public partial interface ITextRangeProvider
    {
        void AddToSelection();
        System.Windows.Automation.Provider.ITextRangeProvider Clone();
        bool Compare(System.Windows.Automation.Provider.ITextRangeProvider range);
        int CompareEndpoints(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, System.Windows.Automation.Provider.ITextRangeProvider targetRange, System.Windows.Automation.Text.TextPatternRangeEndpoint targetEndpoint);
        void ExpandToEnclosingUnit(System.Windows.Automation.Text.TextUnit unit);
        System.Windows.Automation.Provider.ITextRangeProvider FindAttribute(int attribute, object value, bool backward);
        System.Windows.Automation.Provider.ITextRangeProvider FindText(string text, bool backward, bool ignoreCase);
        object GetAttributeValue(int attribute);
        double[] GetBoundingRectangles();
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetChildren();
        System.Windows.Automation.Provider.IRawElementProviderSimple GetEnclosingElement();
        string GetText(int maxLength);
        int Move(System.Windows.Automation.Text.TextUnit unit, int count);
        void MoveEndpointByRange(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, System.Windows.Automation.Provider.ITextRangeProvider targetRange, System.Windows.Automation.Text.TextPatternRangeEndpoint targetEndpoint);
        int MoveEndpointByUnit(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, System.Windows.Automation.Text.TextUnit unit, int count);
        void RemoveFromSelection();
        void ScrollIntoView(bool alignToTop);
        void Select();
    }
    public partial interface IToggleProvider
    {
        System.Windows.Automation.ToggleState ToggleState { get; }
        void Toggle();
    }
    public partial interface ITransformProvider
    {
        bool CanMove { get; }
        bool CanResize { get; }
        bool CanRotate { get; }
        void Move(double x, double y);
        void Resize(double width, double height);
        void Rotate(double degrees);
    }
    public partial interface IValueProvider
    {
        bool IsReadOnly { get; }
        string Value { get; }
        void SetValue(string value);
    }
    public partial interface IVirtualizedItemProvider
    {
        void Realize();
    }
    public partial interface IWindowProvider
    {
        System.Windows.Automation.WindowInteractionState InteractionState { get; }
        bool IsModal { get; }
        bool IsTopmost { get; }
        bool Maximizable { get; }
        bool Minimizable { get; }
        System.Windows.Automation.WindowVisualState VisualState { get; }
        void Close();
        void SetVisualState(System.Windows.Automation.WindowVisualState state);
        bool WaitForInputIdle(int milliseconds);
    }
    public enum NavigateDirection
    {
        Parent = 0,
        NextSibling = 1,
        PreviousSibling = 2,
        FirstChild = 3,
        LastChild = 4,
    }
    [System.FlagsAttribute]
    public enum ProviderOptions
    {
        ClientSideProvider = 1,
        ServerSideProvider = 2,
        NonClientAreaProvider = 4,
        OverrideProvider = 8,
        ProviderOwnsSetFocus = 16,
        UseComThreading = 32,
    }
}

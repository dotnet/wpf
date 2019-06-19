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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("159bc72c-4ad3-485e-9637-d7052edf0146")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IDockProvider
    {
        System.Windows.Automation.DockPosition DockPosition { get; }
        void SetDockPosition(System.Windows.Automation.DockPosition dockPosition);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("d847d3a5-cab0-4a98-8c32-ecb45c59ad24")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IExpandCollapseProvider
    {
        System.Windows.Automation.ExpandCollapseState ExpandCollapseState { get; }
        void Collapse();
        void Expand();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("d02541f1-fb81-4d64-ae32-f520f8a6dbd1")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IGridItemProvider
    {
        int Column { get; }
        int ColumnSpan { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple ContainingGrid { get; }
        int Row { get; }
        int RowSpan { get; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("b17d6187-0907-464b-a168-0ef17a1572b1")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IGridProvider
    {
        int ColumnCount { get; }
        int RowCount { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple GetItem(int row, int column);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("54fcb24b-e18e-47a2-b4d3-eccbe77599a2")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IInvokeProvider
    {
        void Invoke();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("e747770b-39ce-4382-ab30-d8fb3f336f24")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IItemContainerProvider
    {
        System.Windows.Automation.Provider.IRawElementProviderSimple FindItemByProperty(System.Windows.Automation.Provider.IRawElementProviderSimple startAfter, int propertyId, object value);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("6278cab1-b556-4a1a-b4e0-418acc523201")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMultipleViewProvider
    {
        int CurrentView { get; }
        int[] GetSupportedViews();
        string GetViewName(int viewId);
        void SetCurrentView(int viewId);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("36dc7aef-33e6-4691-afe1-2be7274b3d33")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("a407b27b-0f6d-4427-9292-473c7bf93258")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IRawElementProviderAdviseEvents : System.Windows.Automation.Provider.IRawElementProviderSimple
    {
        void AdviseEventAdded(int eventId, int[] properties);
        void AdviseEventRemoved(int eventId, int[] properties);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("f7063da8-8359-439c-9297-bbc5299a7d87")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IRawElementProviderFragment : System.Windows.Automation.Provider.IRawElementProviderSimple
    {
        System.Windows.Rect BoundingRectangle { get; }
        System.Windows.Automation.Provider.IRawElementProviderFragmentRoot FragmentRoot { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetEmbeddedFragmentRoots();
        int[] GetRuntimeId();
        System.Windows.Automation.Provider.IRawElementProviderFragment Navigate(System.Windows.Automation.Provider.NavigateDirection direction);
        void SetFocus();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("620ce2a5-ab8f-40a9-86cb-de3c75599b58")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IRawElementProviderFragmentRoot : System.Windows.Automation.Provider.IRawElementProviderFragment, System.Windows.Automation.Provider.IRawElementProviderSimple
    {
        System.Windows.Automation.Provider.IRawElementProviderFragment ElementProviderFromPoint(double x, double y);
        System.Windows.Automation.Provider.IRawElementProviderFragment GetFocus();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("1d5df27c-8947-4425-b8d9-79787bb460b8")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IRawElementProviderHwndOverride : System.Windows.Automation.Provider.IRawElementProviderSimple
    {
        System.Windows.Automation.Provider.IRawElementProviderSimple GetOverrideProviderForHwnd(System.IntPtr hwnd);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("d6dd68d1-86fd-4332-8666-9abedea2d24c")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IRawElementProviderSimple
    {
        System.Windows.Automation.Provider.IRawElementProviderSimple HostRawElementProvider { get; }
        System.Windows.Automation.Provider.ProviderOptions ProviderOptions { get; }
        object GetPatternProvider(int patternId);
        object GetPropertyValue(int propertyId);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("2360c714-4bf1-4b26-ba65-9b21316127eb")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IScrollItemProvider
    {
        void ScrollIntoView();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("b38b8077-1fc3-42a5-8cae-d40c2215055a")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("2acad808-b2d4-452d-a407-91ff1ad167b2")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ISelectionItemProvider
    {
        bool IsSelected { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple SelectionContainer { get; }
        void AddToSelection();
        void RemoveFromSelection();
        void Select();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("fb8b03af-3bdf-48d4-bd36-1a65793be168")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ISelectionProvider
    {
        bool CanSelectMultiple { get; }
        bool IsSelectionRequired { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetSelection();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("29db1a06-02ce-4cf7-9b42-565d4fab20ee")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ISynchronizedInputProvider
    {
        void Cancel();
        void StartListening(System.Windows.Automation.SynchronizedInputType inputType);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("b9734fa6-771f-4d78-9c90-2517999349cd")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ITableItemProvider : System.Windows.Automation.Provider.IGridItemProvider
    {
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetColumnHeaderItems();
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetRowHeaderItems();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("9c860395-97b3-490a-b52a-858cc22af166")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ITableProvider : System.Windows.Automation.Provider.IGridProvider
    {
        System.Windows.Automation.RowOrColumnMajor RowOrColumnMajor { get; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetColumnHeaders();
        System.Windows.Automation.Provider.IRawElementProviderSimple[] GetRowHeaders();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("3589c92c-63f3-4367-99bb-ada653b77cf2")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ITextProvider
    {
        System.Windows.Automation.Provider.ITextRangeProvider DocumentRange { get; }
        System.Windows.Automation.SupportedTextSelection SupportedTextSelection { get; }
        System.Windows.Automation.Provider.ITextRangeProvider[] GetSelection();
        System.Windows.Automation.Provider.ITextRangeProvider[] GetVisibleRanges();
        System.Windows.Automation.Provider.ITextRangeProvider RangeFromChild(System.Windows.Automation.Provider.IRawElementProviderSimple childElement);
        System.Windows.Automation.Provider.ITextRangeProvider RangeFromPoint(System.Windows.Point screenLocation);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("5347ad7b-c355-46f8-aff5-909033582f63")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("56d00bd0-c4f4-433c-a836-1a52a57e0892")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IToggleProvider
    {
        System.Windows.Automation.ToggleState ToggleState { get; }
        void Toggle();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("6829ddc4-4f91-4ffa-b86f-bd3e2987cb4c")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ITransformProvider
    {
        bool CanMove { get; }
        bool CanResize { get; }
        bool CanRotate { get; }
        void Move(double x, double y);
        void Resize(double width, double height);
        void Rotate(double degrees);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("c7935180-6fb3-4201-b174-7df73adbf64a")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IValueProvider
    {
        bool IsReadOnly { get; }
        string Value { get; }
        void SetValue(string value);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("cb98b665-2d35-4fac-ad35-f3c60d0c0b8b")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IVirtualizedItemProvider
    {
        void Realize();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("987df77b-db06-4d77-8f8a-86a9c3bb90b9")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("670c3006-bf4c-428b-8534-e1848f645122")]
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

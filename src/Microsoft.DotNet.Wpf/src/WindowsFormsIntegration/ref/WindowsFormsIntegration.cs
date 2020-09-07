// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace System.Windows.Automation.Peers
{
    public sealed partial class WindowsFormsHostAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public WindowsFormsHostAutomationPeer(System.Windows.Forms.Integration.WindowsFormsHost owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        [System.Security.SecurityTreatAsSafeAttribute]
        protected override System.Windows.Automation.Peers.HostedWindowWrapper GetHostRawElementProviderCore() { throw null; }
    }
}
namespace System.Windows.Forms.Integration
{
    public partial class ChildChangedEventArgs : System.EventArgs
    {
        public ChildChangedEventArgs(object previousChild) { }
        public object PreviousChild { get { throw null; } }
    }
    [System.ComponentModel.DefaultEventAttribute("ChildChanged")]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Windows.Markup.ContentPropertyAttribute("Child")]
    public partial class ElementHost : System.Windows.Forms.Control
    {
        public ElementHost() { }
        [System.ComponentModel.BrowsableAttribute(true)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
        public override bool AutoSize { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool BackColorTransparent { get { throw null; } set { } }
        protected override bool CanEnableIme { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.UIElement Child { get { throw null; } set { } }
        protected override System.Drawing.Size DefaultSize { get { throw null; } }
        public override bool Focused { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.Panel HostContainer { get { throw null; } }
        protected override System.Windows.Forms.ImeMode ImeModeBase { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Windows.Forms.Integration.PropertyMap PropertyMap { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler BindingContextChanged { add { } remove { } }
        public event System.EventHandler<System.Windows.Forms.Integration.ChildChangedEventArgs> ChildChanged { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler Click { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler ClientSizeChanged { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.ControlEventHandler ControlAdded { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.ControlEventHandler ControlRemoved { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler CursorChanged { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler DoubleClick { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.DragEventHandler DragDrop { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.DragEventHandler DragEnter { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler DragLeave { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.DragEventHandler DragOver { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler Enter { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler FontChanged { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler ForeColorChanged { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.GiveFeedbackEventHandler GiveFeedback { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler GotFocus { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.InvalidateEventHandler Invalidated { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.KeyEventHandler KeyDown { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.KeyPressEventHandler KeyPress { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.KeyEventHandler KeyUp { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.LayoutEventHandler Layout { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler Leave { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler LostFocus { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler MouseCaptureChanged { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.MouseEventHandler MouseClick { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.MouseEventHandler MouseDoubleClick { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.MouseEventHandler MouseDown { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler MouseEnter { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler MouseHover { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler MouseLeave { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.MouseEventHandler MouseMove { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.MouseEventHandler MouseUp { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.MouseEventHandler MouseWheel { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler PaddingChanged { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.PaintEventHandler Paint { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.PreviewKeyDownEventHandler PreviewKeyDown { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.Windows.Forms.QueryContinueDragEventHandler QueryContinueDrag { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler Resize { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler RightToLeftChanged { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler SizeChanged { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new event System.EventHandler TextChanged { add { } remove { } }
        protected override void Dispose(bool disposing) { }
        public static void EnableModelessKeyboardInterop(System.Windows.Window window) { }
        public override System.Drawing.Size GetPreferredSize(System.Drawing.Size proposedSize) { throw null; }
        protected override bool IsInputChar(char charCode) { throw null; }
        protected override void OnEnabledChanged(System.EventArgs e) { }
        protected override void OnGotFocus(System.EventArgs e) { }
        protected override void OnHandleCreated(System.EventArgs e) { }
        protected override void OnLeave(System.EventArgs e) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected override void OnPaintBackground(System.Windows.Forms.PaintEventArgs pevent) { }
        protected override void OnPrint(System.Windows.Forms.PaintEventArgs e) { }
        public virtual void OnPropertyChanged(string propertyName, object value) { }
        protected override void OnVisibleChanged(System.EventArgs e) { }
        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, System.Windows.Forms.Keys keyData) { throw null; }
        protected override bool ProcessMnemonic(char charCode) { throw null; }
        protected override void ScaleCore(float dx, float dy) { }
        protected override void Select(bool directed, bool forward) { }
        protected override void WndProc(ref System.Windows.Forms.Message m) { }
    }
    public partial class IntegrationExceptionEventArgs : System.EventArgs
    {
        public IntegrationExceptionEventArgs(bool throwException, System.Exception exception) { }
        public System.Exception Exception { get { throw null; } }
        public bool ThrowException { get { throw null; } set { } }
    }
    public partial class LayoutExceptionEventArgs : System.Windows.Forms.Integration.IntegrationExceptionEventArgs
    {
        public LayoutExceptionEventArgs(System.Exception exception) : base (default(bool), default(System.Exception)) { }
    }
    public partial class PropertyMap
    {
        public PropertyMap() { }
        public PropertyMap(object source) { }
        protected System.Collections.Generic.Dictionary<string, System.Windows.Forms.Integration.PropertyTranslator> DefaultTranslators { get { throw null; } }
        public System.Windows.Forms.Integration.PropertyTranslator this[string propertyName] { get { throw null; } set { } }
        public System.Collections.ICollection Keys { get { throw null; } }
        protected object SourceObject { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public event System.EventHandler<System.Windows.Forms.Integration.PropertyMappingExceptionEventArgs> PropertyMappingError { add { } remove { } }
        public void Add(string propertyName, System.Windows.Forms.Integration.PropertyTranslator translator) { }
        public void Apply(string propertyName) { }
        public void ApplyAll() { }
        public void Clear() { }
        public bool Contains(string propertyName) { throw null; }
        public void Remove(string propertyName) { }
        public void Reset(string propertyName) { }
        public void ResetAll() { }
    }
    public partial class PropertyMappingExceptionEventArgs : System.Windows.Forms.Integration.IntegrationExceptionEventArgs
    {
        public PropertyMappingExceptionEventArgs(System.Exception exception, string propertyName, object propertyValue) : base (default(bool), default(System.Exception)) { }
        public string PropertyName { get { throw null; } }
        public object PropertyValue { get { throw null; } }
    }
    public delegate void PropertyTranslator(object host, string propertyName, object value);
    [System.ComponentModel.DefaultEventAttribute("ChildChanged")]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Windows.Markup.ContentPropertyAttribute("Child")]
    public partial class WindowsFormsHost : System.Windows.Interop.HwndHost, System.Windows.Interop.IKeyboardInputSink
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty FontFamilyProperty;
        public static readonly System.Windows.DependencyProperty FontSizeProperty;
        public static readonly System.Windows.DependencyProperty FontStyleProperty;
        public static readonly System.Windows.DependencyProperty FontWeightProperty;
        public static readonly System.Windows.DependencyProperty ForegroundProperty;
        public static readonly System.Windows.DependencyProperty PaddingProperty;
        public static readonly System.Windows.DependencyProperty TabIndexProperty;
        public WindowsFormsHost() { }
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        public System.Windows.Forms.Control Child { get { throw null; } set { } }
        public System.Windows.Media.FontFamily FontFamily { get { throw null; } set { } }
        public double FontSize { get { throw null; } set { } }
        public System.Windows.FontStyle FontStyle { get { throw null; } set { } }
        public System.Windows.FontWeight FontWeight { get { throw null; } set { } }
        public System.Windows.Media.Brush Foreground { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public System.Windows.Thickness Padding { get { throw null; } set { } }
        public System.Windows.Forms.Integration.PropertyMap PropertyMap { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public int TabIndex { get { throw null; } set { } }
        public event System.EventHandler<System.Windows.Forms.Integration.ChildChangedEventArgs> ChildChanged { add { } remove { } }
        public event System.EventHandler<System.Windows.Forms.Integration.LayoutExceptionEventArgs> LayoutError { add { } remove { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override System.Runtime.InteropServices.HandleRef BuildWindowCore(System.Runtime.InteropServices.HandleRef hwndParent) { throw null; }
        protected override void DestroyWindowCore(System.Runtime.InteropServices.HandleRef hwnd) { }
        protected override void Dispose(bool disposing) { }
        public static void EnableWindowsFormsInterop() { }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected virtual System.Windows.Vector ScaleChild(System.Windows.Vector newScale) { throw null; }
        public virtual bool TabInto(System.Windows.Input.TraversalRequest request) { throw null; }
        protected override System.IntPtr WndProc(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam, ref bool handled) { throw null; }
    }
}

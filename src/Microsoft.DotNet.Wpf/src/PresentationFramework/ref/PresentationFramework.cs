namespace Microsoft.Win32
{
    public abstract partial class CommonDialog
    {
        protected CommonDialog() { }
        public object Tag { get { throw null; } set { } }
        protected virtual void CheckPermissionsToShowDialog() { }
        protected virtual System.IntPtr HookProc(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam) { throw null; }
        public abstract void Reset();
        protected abstract bool RunDialog(System.IntPtr hwndOwner);
        public virtual bool? ShowDialog() { throw null; }
        public bool? ShowDialog(System.Windows.Window owner) { throw null; }
    }
    public abstract partial class FileDialog : Microsoft.Win32.CommonDialog
    {
        protected FileDialog() { }
        public bool AddExtension { get { throw null; } set { } }
        public virtual bool CheckFileExists { get { throw null; } set { } }
        public bool CheckPathExists { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.Win32.FileDialogCustomPlace> CustomPlaces { get { throw null; } set { } }
        public string DefaultExt { get { throw null; } set { } }
        public bool DereferenceLinks { get { throw null; } set { } }
        public string FileName { get { throw null; } set { } }
        public string[] FileNames { get { throw null; } }
        public string Filter { get { throw null; } set { } }
        public int FilterIndex { get { throw null; } set { } }
        public string InitialDirectory { get { throw null; } set { } }
        protected int Options { get { throw null; } }
        public bool RestoreDirectory { get { throw null; } set { } }
        public string SafeFileName { get { throw null; } }
        public string[] SafeFileNames { get { throw null; } }
        public string Title { get { throw null; } set { } }
        public bool ValidateNames { get { throw null; } set { } }
        public event System.ComponentModel.CancelEventHandler FileOk { add { } remove { } }
        protected override System.IntPtr HookProc(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam) { throw null; }
        protected void OnFileOk(System.ComponentModel.CancelEventArgs e) { }
        public override void Reset() { }
        protected override bool RunDialog(System.IntPtr hwndOwner) { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class FileDialogCustomPlace
    {
        public FileDialogCustomPlace(System.Guid knownFolder) { }
        public FileDialogCustomPlace(string path) { }
        public System.Guid KnownFolder { get { throw null; } }
        public string Path { get { throw null; } }
    }
    public static partial class FileDialogCustomPlaces
    {
        public static Microsoft.Win32.FileDialogCustomPlace Contacts { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace Cookies { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace Desktop { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace Documents { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace Favorites { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace LocalApplicationData { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace Music { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace Pictures { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace ProgramFiles { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace ProgramFilesCommon { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace Programs { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace RoamingApplicationData { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace SendTo { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace StartMenu { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace Startup { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace System { get { throw null; } }
        public static Microsoft.Win32.FileDialogCustomPlace Templates { get { throw null; } }
    }
    public sealed partial class OpenFileDialog : Microsoft.Win32.FileDialog
    {
        public OpenFileDialog() { }
        public bool Multiselect { get { throw null; } set { } }
        public bool ReadOnlyChecked { get { throw null; } set { } }
        public bool ShowReadOnly { get { throw null; } set { } }
        protected override void CheckPermissionsToShowDialog() { }
        public System.IO.Stream OpenFile() { throw null; }
        public System.IO.Stream[] OpenFiles() { throw null; }
        public override void Reset() { }
    }
    public sealed partial class SaveFileDialog : Microsoft.Win32.FileDialog
    {
        public SaveFileDialog() { }
        public bool CreatePrompt { get { throw null; } set { } }
        public bool OverwritePrompt { get { throw null; } set { } }
        public System.IO.Stream OpenFile() { throw null; }
        public override void Reset() { }
    }
}
namespace System.ComponentModel
{
    public static partial class DesignerProperties
    {
        public static readonly System.Windows.DependencyProperty IsInDesignModeProperty;
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public static bool GetIsInDesignMode(System.Windows.DependencyObject element) { throw null; }
        public static void SetIsInDesignMode(System.Windows.DependencyObject element, bool value) { }
    }
}
namespace System.Windows
{
    public partial class Application : System.Windows.Threading.DispatcherObject, System.Windows.Markup.IQueryAmbient
    {
        public Application() { }
        public static System.Windows.Application Current { get { throw null; } }
        public System.Windows.Window MainWindow { get { throw null; } set { } }
        public System.Collections.IDictionary Properties { get { throw null; } }
        public static System.Reflection.Assembly ResourceAssembly { get { throw null; } set { } }
        [System.Windows.Markup.AmbientAttribute]
        public System.Windows.ResourceDictionary Resources { get { throw null; } set { } }
        public System.Windows.ShutdownMode ShutdownMode { get { throw null; } set { } }
        public System.Uri StartupUri { get { throw null; } set { } }
        public System.Windows.WindowCollection Windows { get { throw null; } }
        public event System.EventHandler Activated { add { } remove { } }
        public event System.EventHandler Deactivated { add { } remove { } }
        public event System.Windows.Threading.DispatcherUnhandledExceptionEventHandler DispatcherUnhandledException { add { } remove { } }
        public event System.Windows.ExitEventHandler Exit { add { } remove { } }
        public event System.Windows.Navigation.FragmentNavigationEventHandler FragmentNavigation { add { } remove { } }
        public event System.Windows.Navigation.LoadCompletedEventHandler LoadCompleted { add { } remove { } }
        public event System.Windows.Navigation.NavigatedEventHandler Navigated { add { } remove { } }
        public event System.Windows.Navigation.NavigatingCancelEventHandler Navigating { add { } remove { } }
        public event System.Windows.Navigation.NavigationFailedEventHandler NavigationFailed { add { } remove { } }
        public event System.Windows.Navigation.NavigationProgressEventHandler NavigationProgress { add { } remove { } }
        public event System.Windows.Navigation.NavigationStoppedEventHandler NavigationStopped { add { } remove { } }
        public event System.Windows.SessionEndingCancelEventHandler SessionEnding { add { } remove { } }
        public event System.Windows.StartupEventHandler Startup { add { } remove { } }
        public object FindResource(object resourceKey) { throw null; }
        public static System.Windows.Resources.StreamResourceInfo GetContentStream(System.Uri uriContent) { throw null; }
        public static string GetCookie(System.Uri uri) { throw null; }
        public static System.Windows.Resources.StreamResourceInfo GetRemoteStream(System.Uri uriRemote) { throw null; }
        public static System.Windows.Resources.StreamResourceInfo GetResourceStream(System.Uri uriResource) { throw null; }
        public static void LoadComponent(object component, System.Uri resourceLocator) { }
        public static object LoadComponent(System.Uri resourceLocator) { throw null; }
        protected virtual void OnActivated(System.EventArgs e) { }
        protected virtual void OnDeactivated(System.EventArgs e) { }
        protected virtual void OnExit(System.Windows.ExitEventArgs e) { }
        protected virtual void OnFragmentNavigation(System.Windows.Navigation.FragmentNavigationEventArgs e) { }
        protected virtual void OnLoadCompleted(System.Windows.Navigation.NavigationEventArgs e) { }
        protected virtual void OnNavigated(System.Windows.Navigation.NavigationEventArgs e) { }
        protected virtual void OnNavigating(System.Windows.Navigation.NavigatingCancelEventArgs e) { }
        protected virtual void OnNavigationFailed(System.Windows.Navigation.NavigationFailedEventArgs e) { }
        protected virtual void OnNavigationProgress(System.Windows.Navigation.NavigationProgressEventArgs e) { }
        protected virtual void OnNavigationStopped(System.Windows.Navigation.NavigationEventArgs e) { }
        protected virtual void OnSessionEnding(System.Windows.SessionEndingCancelEventArgs e) { }
        protected virtual void OnStartup(System.Windows.StartupEventArgs e) { }
        public int Run() { throw null; }
        public int Run(System.Windows.Window window) { throw null; }
        public static void SetCookie(System.Uri uri, string value) { }
        public void Shutdown() { }
        public void Shutdown(int exitCode) { }
        bool System.Windows.Markup.IQueryAmbient.IsAmbientPropertyAvailable(string propertyName) { throw null; }
        public object TryFindResource(object resourceKey) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false)]
    public sealed partial class AttachedPropertyBrowsableForChildrenAttribute : System.Windows.AttachedPropertyBrowsableAttribute
    {
        public AttachedPropertyBrowsableForChildrenAttribute() { }
        public bool IncludeDescendants { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        internal override bool IsBrowsable(System.Windows.DependencyObject d, System.Windows.DependencyProperty dp) { throw null; }
    }
    public enum BaseValueSource
    {
        Unknown = 0,
        Default = 1,
        Inherited = 2,
        DefaultStyle = 3,
        DefaultStyleTrigger = 4,
        Style = 5,
        TemplateTrigger = 6,
        StyleTrigger = 7,
        ImplicitStyleReference = 8,
        ParentTemplate = 9,
        ParentTemplateTrigger = 10,
        Local = 11,
    }
    [System.Windows.Markup.MarkupExtensionReturnTypeAttribute(typeof(System.Windows.Media.Imaging.ColorConvertedBitmap))]
    public partial class ColorConvertedBitmapExtension : System.Windows.Markup.MarkupExtension
    {
        public ColorConvertedBitmapExtension() { }
        public ColorConvertedBitmapExtension(object image) { }
        public override object ProvideValue(System.IServiceProvider serviceProvider) { throw null; }
    }
    public enum ColumnSpaceDistribution
    {
        Left = 0,
        Right = 1,
        Between = 2,
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Markup.ComponentResourceKeyConverter))]
    public partial class ComponentResourceKey : System.Windows.ResourceKey
    {
        public ComponentResourceKey() { }
        public ComponentResourceKey(System.Type typeInTargetAssembly, object resourceId) { }
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public object ResourceId { get { throw null; } set { } }
        public System.Type TypeInTargetAssembly { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Windows.Markup.XamlSetMarkupExtensionAttribute("ReceiveMarkupExtension")]
    [System.Windows.Markup.XamlSetTypeConverterAttribute("ReceiveTypeConverter")]
    public sealed partial class Condition : System.ComponentModel.ISupportInitialize
    {
        public Condition() { }
        public Condition(System.Windows.Data.BindingBase binding, object conditionValue) { }
        public Condition(System.Windows.DependencyProperty conditionProperty, object conditionValue) { }
        public Condition(System.Windows.DependencyProperty conditionProperty, object conditionValue, string sourceName) { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Data.BindingBase Binding { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Windows.Markup.AmbientAttribute]
        public System.Windows.DependencyProperty Property { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string SourceName { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Markup.SetterTriggerConditionValueConverter))]
        public object Value { get { throw null; } set { } }
        public static void ReceiveMarkupExtension(object targetObject, System.Windows.Markup.XamlSetMarkupExtensionEventArgs eventArgs) { }
        public static void ReceiveTypeConverter(object targetObject, System.Windows.Markup.XamlSetTypeConverterEventArgs eventArgs) { }
        void System.ComponentModel.ISupportInitialize.BeginInit() { }
        void System.ComponentModel.ISupportInitialize.EndInit() { }
    }
    public sealed partial class ConditionCollection : System.Collections.ObjectModel.Collection<System.Windows.Condition>
    {
        public ConditionCollection() { }
        public bool IsSealed { get { throw null; } }
        protected override void ClearItems() { }
        protected override void InsertItem(int index, System.Windows.Condition item) { }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, System.Windows.Condition item) { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.CornerRadiusConverter))]
    public partial struct CornerRadius : System.IEquatable<System.Windows.CornerRadius>
    {
        public CornerRadius(double uniformRadius) { throw null; }
        public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft) { throw null; }
        public double BottomLeft { get { throw null; } set { } }
        public double BottomRight { get { throw null; } set { } }
        public double TopLeft { get { throw null; } set { } }
        public double TopRight { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Windows.CornerRadius cornerRadius) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.CornerRadius cr1, System.Windows.CornerRadius cr2) { throw null; }
        public static bool operator !=(System.Windows.CornerRadius cr1, System.Windows.CornerRadius cr2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class CornerRadiusConverter : System.ComponentModel.TypeConverter
    {
        public CornerRadiusConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object value, System.Type destinationType) { throw null; }
    }
    [System.Windows.Markup.DictionaryKeyPropertyAttribute("DataTemplateKey")]
    public partial class DataTemplate : System.Windows.FrameworkTemplate
    {
        public DataTemplate() { }
        public DataTemplate(object dataType) { }
        public object DataTemplateKey { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Windows.Markup.AmbientAttribute]
        public object DataType { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        [System.Windows.Markup.DependsOnAttribute("Template")]
        [System.Windows.Markup.DependsOnAttribute("VisualTree")]
        public System.Windows.TriggerCollection Triggers { get { throw null; } }
        protected override void ValidateTemplatedParent(System.Windows.FrameworkElement templatedParent) { }
    }
    public partial class DataTemplateKey : System.Windows.TemplateKey
    {
        public DataTemplateKey() : base (default(System.Windows.TemplateKey.TemplateType)) { }
        public DataTemplateKey(object dataType) : base (default(System.Windows.TemplateKey.TemplateType)) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Setters")]
    [System.Windows.Markup.XamlSetMarkupExtensionAttribute("ReceiveMarkupExtension")]
    public partial class DataTrigger : System.Windows.TriggerBase, System.Windows.Markup.IAddChild
    {
        public DataTrigger() { }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        public System.Windows.Data.BindingBase Binding { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.SetterBaseCollection Setters { get { throw null; } }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        [System.Windows.Markup.DependsOnAttribute("Binding")]
        public object Value { get { throw null; } set { } }
        public static void ReceiveMarkupExtension(object targetObject, System.Windows.Markup.XamlSetMarkupExtensionEventArgs eventArgs) { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.DeferrableContentConverter))]
    public partial class DeferrableContent
    {
        internal DeferrableContent() { }
    }
    public partial class DeferrableContentConverter : System.ComponentModel.TypeConverter
    {
        public DeferrableContentConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
    }
    public static partial class DependencyPropertyHelper
    {
        public static System.Windows.ValueSource GetValueSource(System.Windows.DependencyObject dependencyObject, System.Windows.DependencyProperty dependencyProperty) { throw null; }
        public static bool IsTemplatedValueDynamic(System.Windows.DependencyObject elementInTemplate, System.Windows.DependencyProperty dependencyProperty) { throw null; }
    }
    public partial class DialogResultConverter : System.ComponentModel.TypeConverter
    {
        public DialogResultConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object value, System.Type destinationType) { throw null; }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.DynamicResourceExtensionConverter))]
    [System.Windows.Markup.MarkupExtensionReturnTypeAttribute(typeof(object))]
    public partial class DynamicResourceExtension : System.Windows.Markup.MarkupExtension
    {
        public DynamicResourceExtension() { }
        public DynamicResourceExtension(object resourceKey) { }
        [System.Windows.Markup.ConstructorArgumentAttribute("resourceKey")]
        public object ResourceKey { get { throw null; } set { } }
        public override object ProvideValue(System.IServiceProvider serviceProvider) { throw null; }
    }
    public partial class DynamicResourceExtensionConverter : System.ComponentModel.TypeConverter
    {
        public DynamicResourceExtensionConverter() { }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public partial class EventSetter : System.Windows.SetterBase
    {
        public EventSetter() { }
        public EventSetter(System.Windows.RoutedEvent routedEvent, System.Delegate handler) { }
        public System.Windows.RoutedEvent Event { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool HandledEventsToo { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Markup.EventSetterHandlerConverter))]
        public System.Delegate Handler { get { throw null; } set { } }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Actions")]
    public partial class EventTrigger : System.Windows.TriggerBase, System.Windows.Markup.IAddChild
    {
        public EventTrigger() { }
        public EventTrigger(System.Windows.RoutedEvent routedEvent) { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.TriggerActionCollection Actions { get { throw null; } }
        public System.Windows.RoutedEvent RoutedEvent { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string SourceName { get { throw null; } set { } }
        protected virtual void AddChild(object value) { }
        protected virtual void AddText(string text) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeActions() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public sealed partial class ExceptionRoutedEventArgs : System.Windows.RoutedEventArgs
    {
        internal ExceptionRoutedEventArgs() { }
        public System.Exception ErrorException { get { throw null; } }
    }
    public partial class ExitEventArgs : System.EventArgs
    {
        internal ExitEventArgs() { }
        public int ApplicationExitCode { get { throw null; } set { } }
    }
    public delegate void ExitEventHandler(object sender, System.Windows.ExitEventArgs e);
    public enum FigureHorizontalAnchor
    {
        PageLeft = 0,
        PageCenter = 1,
        PageRight = 2,
        ContentLeft = 3,
        ContentCenter = 4,
        ContentRight = 5,
        ColumnLeft = 6,
        ColumnCenter = 7,
        ColumnRight = 8,
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.FigureLengthConverter))]
    public partial struct FigureLength : System.IEquatable<System.Windows.FigureLength>
    {
        public FigureLength(double pixels) { throw null; }
        public FigureLength(double value, System.Windows.FigureUnitType type) { throw null; }
        public System.Windows.FigureUnitType FigureUnitType { get { throw null; } }
        public bool IsAbsolute { get { throw null; } }
        public bool IsAuto { get { throw null; } }
        public bool IsColumn { get { throw null; } }
        public bool IsContent { get { throw null; } }
        public bool IsPage { get { throw null; } }
        public double Value { get { throw null; } }
        public override bool Equals(object oCompare) { throw null; }
        public bool Equals(System.Windows.FigureLength figureLength) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.FigureLength fl1, System.Windows.FigureLength fl2) { throw null; }
        public static bool operator !=(System.Windows.FigureLength fl1, System.Windows.FigureLength fl2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class FigureLengthConverter : System.ComponentModel.TypeConverter
    {
        public FigureLengthConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object value, System.Type destinationType) { throw null; }
    }
    public enum FigureUnitType
    {
        Auto = 0,
        Pixel = 1,
        Column = 2,
        Content = 3,
        Page = 4,
    }
    public enum FigureVerticalAnchor
    {
        PageTop = 0,
        PageCenter = 1,
        PageBottom = 2,
        ContentTop = 3,
        ContentCenter = 4,
        ContentBottom = 5,
        ParagraphTop = 6,
    }
    public partial class FontSizeConverter : System.ComponentModel.TypeConverter
    {
        public FontSizeConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public static partial class FrameworkCompatibilityPreferences
    {
        public static bool AreInactiveSelectionHighlightBrushKeysSupported { get { throw null; } set { } }
        public static bool KeepTextBoxDisplaySynchronizedWithTextProperty { get { throw null; } set { } }
        public static bool ShouldThrowOnCopyOrCutFailure { get { throw null; } set { } }
    }
    [System.Windows.Markup.RuntimeNamePropertyAttribute("Name")]
    [System.Windows.Markup.UsableDuringInitializationAttribute(true)]
    [System.Windows.Markup.XmlLangPropertyAttribute("Language")]
    [System.Windows.StyleTypedPropertyAttribute(Property="FocusVisualStyle", StyleTargetType=typeof(System.Windows.Controls.Control))]
    public partial class FrameworkContentElement : System.Windows.ContentElement, System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.Markup.IQueryAmbient
    {
        public static readonly System.Windows.DependencyProperty BindingGroupProperty;
        public static readonly System.Windows.RoutedEvent ContextMenuClosingEvent;
        public static readonly System.Windows.RoutedEvent ContextMenuOpeningEvent;
        public static readonly System.Windows.DependencyProperty ContextMenuProperty;
        public static readonly System.Windows.DependencyProperty CursorProperty;
        public static readonly System.Windows.DependencyProperty DataContextProperty;
        protected internal static readonly System.Windows.DependencyProperty DefaultStyleKeyProperty;
        public static readonly System.Windows.DependencyProperty FocusVisualStyleProperty;
        public static readonly System.Windows.DependencyProperty ForceCursorProperty;
        public static readonly System.Windows.DependencyProperty InputScopeProperty;
        public static readonly System.Windows.DependencyProperty LanguageProperty;
        public static readonly System.Windows.RoutedEvent LoadedEvent;
        public static readonly System.Windows.DependencyProperty NameProperty;
        public static readonly System.Windows.DependencyProperty OverridesDefaultStyleProperty;
        public static readonly System.Windows.DependencyProperty StyleProperty;
        public static readonly System.Windows.DependencyProperty TagProperty;
        public static readonly System.Windows.RoutedEvent ToolTipClosingEvent;
        public static readonly System.Windows.RoutedEvent ToolTipOpeningEvent;
        public static readonly System.Windows.DependencyProperty ToolTipProperty;
        public static readonly System.Windows.RoutedEvent UnloadedEvent;
        public FrameworkContentElement() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public System.Windows.Data.BindingGroup BindingGroup { get { throw null; } set { } }
        public System.Windows.Controls.ContextMenu ContextMenu { get { throw null; } set { } }
        public System.Windows.Input.Cursor Cursor { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public object DataContext { get { throw null; } set { } }
        protected internal object DefaultStyleKey { get { throw null; } set { } }
        public System.Windows.Style FocusVisualStyle { get { throw null; } set { } }
        public bool ForceCursor { get { throw null; } set { } }
        public System.Windows.Input.InputScope InputScope { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public bool IsInitialized { get { throw null; } }
        public bool IsLoaded { get { throw null; } }
        public System.Windows.Markup.XmlLanguage Language { get { throw null; } set { } }
        protected internal virtual System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        [System.ComponentModel.MergablePropertyAttribute(false)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public string Name { get { throw null; } set { } }
        public bool OverridesDefaultStyle { get { throw null; } set { } }
        public System.Windows.DependencyObject Parent { get { throw null; } }
        [System.Windows.Markup.AmbientAttribute]
        public System.Windows.ResourceDictionary Resources { get { throw null; } set { } }
        public System.Windows.Style Style { get { throw null; } set { } }
        public object Tag { get { throw null; } set { } }
        public System.Windows.DependencyObject TemplatedParent { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public object ToolTip { get { throw null; } set { } }
        public event System.Windows.Controls.ContextMenuEventHandler ContextMenuClosing { add { } remove { } }
        public event System.Windows.Controls.ContextMenuEventHandler ContextMenuOpening { add { } remove { } }
        public event System.Windows.DependencyPropertyChangedEventHandler DataContextChanged { add { } remove { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public event System.EventHandler Initialized { add { } remove { } }
        public event System.Windows.RoutedEventHandler Loaded { add { } remove { } }
        public event System.EventHandler<System.Windows.Data.DataTransferEventArgs> SourceUpdated { add { } remove { } }
        public event System.EventHandler<System.Windows.Data.DataTransferEventArgs> TargetUpdated { add { } remove { } }
        public event System.Windows.Controls.ToolTipEventHandler ToolTipClosing { add { } remove { } }
        public event System.Windows.Controls.ToolTipEventHandler ToolTipOpening { add { } remove { } }
        public event System.Windows.RoutedEventHandler Unloaded { add { } remove { } }
        protected internal void AddLogicalChild(object child) { }
        public virtual void BeginInit() { }
        public void BeginStoryboard(System.Windows.Media.Animation.Storyboard storyboard) { }
        public void BeginStoryboard(System.Windows.Media.Animation.Storyboard storyboard, System.Windows.Media.Animation.HandoffBehavior handoffBehavior) { }
        public void BeginStoryboard(System.Windows.Media.Animation.Storyboard storyboard, System.Windows.Media.Animation.HandoffBehavior handoffBehavior, bool isControllable) { }
        public void BringIntoView() { }
        public virtual void EndInit() { }
        public object FindName(string name) { throw null; }
        public object FindResource(object resourceKey) { throw null; }
        public System.Windows.Data.BindingExpression GetBindingExpression(System.Windows.DependencyProperty dp) { throw null; }
        protected internal override System.Windows.DependencyObject GetUIParentCore() { throw null; }
        public sealed override bool MoveFocus(System.Windows.Input.TraversalRequest request) { throw null; }
        protected virtual void OnContextMenuClosing(System.Windows.Controls.ContextMenuEventArgs e) { }
        protected virtual void OnContextMenuOpening(System.Windows.Controls.ContextMenuEventArgs e) { }
        protected override void OnGotFocus(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnInitialized(System.EventArgs e) { }
        protected override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected internal virtual void OnStyleChanged(System.Windows.Style oldStyle, System.Windows.Style newStyle) { }
        protected virtual void OnToolTipClosing(System.Windows.Controls.ToolTipEventArgs e) { }
        protected virtual void OnToolTipOpening(System.Windows.Controls.ToolTipEventArgs e) { }
        public sealed override System.Windows.DependencyObject PredictFocus(System.Windows.Input.FocusNavigationDirection direction) { throw null; }
        public void RegisterName(string name, object scopedElement) { }
        protected internal void RemoveLogicalChild(object child) { }
        public System.Windows.Data.BindingExpression SetBinding(System.Windows.DependencyProperty dp, string path) { throw null; }
        public System.Windows.Data.BindingExpressionBase SetBinding(System.Windows.DependencyProperty dp, System.Windows.Data.BindingBase binding) { throw null; }
        public void SetResourceReference(System.Windows.DependencyProperty dp, object name) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeResources() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeStyle() { throw null; }
        bool System.Windows.Markup.IQueryAmbient.IsAmbientPropertyAvailable(string propertyName) { throw null; }
        public object TryFindResource(object resourceKey) { throw null; }
        public void UnregisterName(string name) { }
        public void UpdateDefaultStyle() { }
    }
    [System.Windows.Markup.RuntimeNamePropertyAttribute("Name")]
    [System.Windows.Markup.UsableDuringInitializationAttribute(true)]
    [System.Windows.Markup.XmlLangPropertyAttribute("Language")]
    [System.Windows.StyleTypedPropertyAttribute(Property="FocusVisualStyle", StyleTargetType=typeof(System.Windows.Controls.Control))]
    public partial class FrameworkElement : System.Windows.UIElement, System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.Markup.IQueryAmbient
    {
        public static readonly System.Windows.DependencyProperty ActualHeightProperty;
        public static readonly System.Windows.DependencyProperty ActualWidthProperty;
        public static readonly System.Windows.DependencyProperty BindingGroupProperty;
        public static readonly System.Windows.RoutedEvent ContextMenuClosingEvent;
        public static readonly System.Windows.RoutedEvent ContextMenuOpeningEvent;
        public static readonly System.Windows.DependencyProperty ContextMenuProperty;
        public static readonly System.Windows.DependencyProperty CursorProperty;
        public static readonly System.Windows.DependencyProperty DataContextProperty;
        protected internal static readonly System.Windows.DependencyProperty DefaultStyleKeyProperty;
        public static readonly System.Windows.DependencyProperty FlowDirectionProperty;
        public static readonly System.Windows.DependencyProperty FocusVisualStyleProperty;
        public static readonly System.Windows.DependencyProperty ForceCursorProperty;
        public static readonly System.Windows.DependencyProperty HeightProperty;
        public static readonly System.Windows.DependencyProperty HorizontalAlignmentProperty;
        public static readonly System.Windows.DependencyProperty InputScopeProperty;
        public static readonly System.Windows.DependencyProperty LanguageProperty;
        public static readonly System.Windows.DependencyProperty LayoutTransformProperty;
        public static readonly System.Windows.RoutedEvent LoadedEvent;
        public static readonly System.Windows.DependencyProperty MarginProperty;
        public static readonly System.Windows.DependencyProperty MaxHeightProperty;
        public static readonly System.Windows.DependencyProperty MaxWidthProperty;
        public static readonly System.Windows.DependencyProperty MinHeightProperty;
        public static readonly System.Windows.DependencyProperty MinWidthProperty;
        public static readonly System.Windows.DependencyProperty NameProperty;
        public static readonly System.Windows.DependencyProperty OverridesDefaultStyleProperty;
        public static readonly System.Windows.RoutedEvent RequestBringIntoViewEvent;
        public static readonly System.Windows.RoutedEvent SizeChangedEvent;
        public static readonly System.Windows.DependencyProperty StyleProperty;
        public static readonly System.Windows.DependencyProperty TagProperty;
        public static readonly System.Windows.RoutedEvent ToolTipClosingEvent;
        public static readonly System.Windows.RoutedEvent ToolTipOpeningEvent;
        public static readonly System.Windows.DependencyProperty ToolTipProperty;
        public static readonly System.Windows.RoutedEvent UnloadedEvent;
        public static readonly System.Windows.DependencyProperty UseLayoutRoundingProperty;
        public static readonly System.Windows.DependencyProperty VerticalAlignmentProperty;
        public static readonly System.Windows.DependencyProperty WidthProperty;
        public FrameworkElement() { }
        public double ActualHeight { get { throw null; } }
        public double ActualWidth { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public System.Windows.Data.BindingGroup BindingGroup { get { throw null; } set { } }
        public System.Windows.Controls.ContextMenu ContextMenu { get { throw null; } set { } }
        public System.Windows.Input.Cursor Cursor { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public object DataContext { get { throw null; } set { } }
        protected internal object DefaultStyleKey { get { throw null; } set { } }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None)]
        public System.Windows.FlowDirection FlowDirection { get { throw null; } set { } }
        public System.Windows.Style FocusVisualStyle { get { throw null; } set { } }
        public bool ForceCursor { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        public double Height { get { throw null; } set { } }
        public System.Windows.HorizontalAlignment HorizontalAlignment { get { throw null; } set { } }
        protected internal System.Windows.InheritanceBehavior InheritanceBehavior { get { throw null; } set { } }
        public System.Windows.Input.InputScope InputScope { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public bool IsInitialized { get { throw null; } }
        public bool IsLoaded { get { throw null; } }
        public System.Windows.Markup.XmlLanguage Language { get { throw null; } set { } }
        public System.Windows.Media.Transform LayoutTransform { get { throw null; } set { } }
        protected internal virtual System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public System.Windows.Thickness Margin { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        public double MaxHeight { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        public double MaxWidth { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        public double MinHeight { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        public double MinWidth { get { throw null; } set { } }
        [System.ComponentModel.MergablePropertyAttribute(false)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        [System.Windows.Markup.DesignerSerializationOptionsAttribute(System.Windows.Markup.DesignerSerializationOptions.SerializeAsAttribute)]
        public string Name { get { throw null; } set { } }
        public bool OverridesDefaultStyle { get { throw null; } set { } }
        public System.Windows.DependencyObject Parent { get { throw null; } }
        [System.Windows.Markup.AmbientAttribute]
        public System.Windows.ResourceDictionary Resources { get { throw null; } set { } }
        public System.Windows.Style Style { get { throw null; } set { } }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public object Tag { get { throw null; } set { } }
        public System.Windows.DependencyObject TemplatedParent { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.ToolTip)]
        public object ToolTip { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.TriggerCollection Triggers { get { throw null; } }
        public bool UseLayoutRounding { get { throw null; } set { } }
        public System.Windows.VerticalAlignment VerticalAlignment { get { throw null; } set { } }
        protected override int VisualChildrenCount { get { throw null; } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        public double Width { get { throw null; } set { } }
        public event System.Windows.Controls.ContextMenuEventHandler ContextMenuClosing { add { } remove { } }
        public event System.Windows.Controls.ContextMenuEventHandler ContextMenuOpening { add { } remove { } }
        public event System.Windows.DependencyPropertyChangedEventHandler DataContextChanged { add { } remove { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public event System.EventHandler Initialized { add { } remove { } }
        public event System.Windows.RoutedEventHandler Loaded { add { } remove { } }
        public event System.Windows.RequestBringIntoViewEventHandler RequestBringIntoView { add { } remove { } }
        public event System.Windows.SizeChangedEventHandler SizeChanged { add { } remove { } }
        public event System.EventHandler<System.Windows.Data.DataTransferEventArgs> SourceUpdated { add { } remove { } }
        public event System.EventHandler<System.Windows.Data.DataTransferEventArgs> TargetUpdated { add { } remove { } }
        public event System.Windows.Controls.ToolTipEventHandler ToolTipClosing { add { } remove { } }
        public event System.Windows.Controls.ToolTipEventHandler ToolTipOpening { add { } remove { } }
        public event System.Windows.RoutedEventHandler Unloaded { add { } remove { } }
        protected internal void AddLogicalChild(object child) { }
        public bool ApplyTemplate() { throw null; }
        protected sealed override void ArrangeCore(System.Windows.Rect finalRect) { }
        protected virtual System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        public virtual void BeginInit() { }
        public void BeginStoryboard(System.Windows.Media.Animation.Storyboard storyboard) { }
        public void BeginStoryboard(System.Windows.Media.Animation.Storyboard storyboard, System.Windows.Media.Animation.HandoffBehavior handoffBehavior) { }
        public void BeginStoryboard(System.Windows.Media.Animation.Storyboard storyboard, System.Windows.Media.Animation.HandoffBehavior handoffBehavior, bool isControllable) { }
        public void BringIntoView() { }
        public void BringIntoView(System.Windows.Rect targetRectangle) { }
        public virtual void EndInit() { }
        public object FindName(string name) { throw null; }
        public object FindResource(object resourceKey) { throw null; }
        public System.Windows.Data.BindingExpression GetBindingExpression(System.Windows.DependencyProperty dp) { throw null; }
        public static System.Windows.FlowDirection GetFlowDirection(System.Windows.DependencyObject element) { throw null; }
        protected override System.Windows.Media.Geometry GetLayoutClip(System.Windows.Size layoutSlotSize) { throw null; }
        protected internal System.Windows.DependencyObject GetTemplateChild(string childName) { throw null; }
        protected internal override System.Windows.DependencyObject GetUIParentCore() { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected sealed override System.Windows.Size MeasureCore(System.Windows.Size availableSize) { throw null; }
        protected virtual System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        public sealed override bool MoveFocus(System.Windows.Input.TraversalRequest request) { throw null; }
        public virtual void OnApplyTemplate() { }
        protected virtual void OnContextMenuClosing(System.Windows.Controls.ContextMenuEventArgs e) { }
        protected virtual void OnContextMenuOpening(System.Windows.Controls.ContextMenuEventArgs e) { }
        protected override void OnGotFocus(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnInitialized(System.EventArgs e) { }
        protected override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected internal override void OnRenderSizeChanged(System.Windows.SizeChangedInfo sizeInfo) { }
        protected internal virtual void OnStyleChanged(System.Windows.Style oldStyle, System.Windows.Style newStyle) { }
        protected virtual void OnToolTipClosing(System.Windows.Controls.ToolTipEventArgs e) { }
        protected virtual void OnToolTipOpening(System.Windows.Controls.ToolTipEventArgs e) { }
        protected internal override void OnVisualParentChanged(System.Windows.DependencyObject oldParent) { }
        protected internal virtual void ParentLayoutInvalidated(System.Windows.UIElement child) { }
        public sealed override System.Windows.DependencyObject PredictFocus(System.Windows.Input.FocusNavigationDirection direction) { throw null; }
        public void RegisterName(string name, object scopedElement) { }
        protected internal void RemoveLogicalChild(object child) { }
        public System.Windows.Data.BindingExpression SetBinding(System.Windows.DependencyProperty dp, string path) { throw null; }
        public System.Windows.Data.BindingExpressionBase SetBinding(System.Windows.DependencyProperty dp, System.Windows.Data.BindingBase binding) { throw null; }
        public static void SetFlowDirection(System.Windows.DependencyObject element, System.Windows.FlowDirection value) { }
        public void SetResourceReference(System.Windows.DependencyProperty dp, object name) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeResources() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeStyle() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeTriggers() { throw null; }
        bool System.Windows.Markup.IQueryAmbient.IsAmbientPropertyAvailable(string propertyName) { throw null; }
        public object TryFindResource(object resourceKey) { throw null; }
        public void UnregisterName(string name) { }
        public void UpdateDefaultStyle() { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
    public partial class FrameworkElementFactory
    {
        public FrameworkElementFactory() { }
        public FrameworkElementFactory(string text) { }
        public FrameworkElementFactory(System.Type type) { }
        public FrameworkElementFactory(System.Type type, string name) { }
        public System.Windows.FrameworkElementFactory FirstChild { get { throw null; } }
        public bool IsSealed { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public System.Windows.FrameworkElementFactory NextSibling { get { throw null; } }
        public System.Windows.FrameworkElementFactory Parent { get { throw null; } }
        public string Text { get { throw null; } set { } }
        public System.Type Type { get { throw null; } set { } }
        public void AddHandler(System.Windows.RoutedEvent routedEvent, System.Delegate handler) { }
        public void AddHandler(System.Windows.RoutedEvent routedEvent, System.Delegate handler, bool handledEventsToo) { }
        public void AppendChild(System.Windows.FrameworkElementFactory child) { }
        public void RemoveHandler(System.Windows.RoutedEvent routedEvent, System.Delegate handler) { }
        public void SetBinding(System.Windows.DependencyProperty dp, System.Windows.Data.BindingBase binding) { }
        public void SetResourceReference(System.Windows.DependencyProperty dp, object name) { }
        public void SetValue(System.Windows.DependencyProperty dp, object value) { }
    }
    public partial class FrameworkPropertyMetadata : System.Windows.UIPropertyMetadata
    {
        public FrameworkPropertyMetadata() { }
        public FrameworkPropertyMetadata(object defaultValue) { }
        public FrameworkPropertyMetadata(object defaultValue, System.Windows.FrameworkPropertyMetadataOptions flags) { }
        public FrameworkPropertyMetadata(object defaultValue, System.Windows.FrameworkPropertyMetadataOptions flags, System.Windows.PropertyChangedCallback propertyChangedCallback) { }
        public FrameworkPropertyMetadata(object defaultValue, System.Windows.FrameworkPropertyMetadataOptions flags, System.Windows.PropertyChangedCallback propertyChangedCallback, System.Windows.CoerceValueCallback coerceValueCallback) { }
        public FrameworkPropertyMetadata(object defaultValue, System.Windows.FrameworkPropertyMetadataOptions flags, System.Windows.PropertyChangedCallback propertyChangedCallback, System.Windows.CoerceValueCallback coerceValueCallback, bool isAnimationProhibited) { }
        public FrameworkPropertyMetadata(object defaultValue, System.Windows.FrameworkPropertyMetadataOptions flags, System.Windows.PropertyChangedCallback propertyChangedCallback, System.Windows.CoerceValueCallback coerceValueCallback, bool isAnimationProhibited, System.Windows.Data.UpdateSourceTrigger defaultUpdateSourceTrigger) { }
        public FrameworkPropertyMetadata(object defaultValue, System.Windows.PropertyChangedCallback propertyChangedCallback) { }
        public FrameworkPropertyMetadata(object defaultValue, System.Windows.PropertyChangedCallback propertyChangedCallback, System.Windows.CoerceValueCallback coerceValueCallback) { }
        public FrameworkPropertyMetadata(System.Windows.PropertyChangedCallback propertyChangedCallback) { }
        public FrameworkPropertyMetadata(System.Windows.PropertyChangedCallback propertyChangedCallback, System.Windows.CoerceValueCallback coerceValueCallback) { }
        public bool AffectsArrange { get { throw null; } set { } }
        public bool AffectsMeasure { get { throw null; } set { } }
        public bool AffectsParentArrange { get { throw null; } set { } }
        public bool AffectsParentMeasure { get { throw null; } set { } }
        public bool AffectsRender { get { throw null; } set { } }
        public bool BindsTwoWayByDefault { get { throw null; } set { } }
        public System.Windows.Data.UpdateSourceTrigger DefaultUpdateSourceTrigger { get { throw null; } set { } }
        public bool Inherits { get { throw null; } set { } }
        public bool IsDataBindingAllowed { get { throw null; } }
        public bool IsNotDataBindable { get { throw null; } set { } }
        public bool Journal { get { throw null; } set { } }
        public bool OverridesInheritanceBehavior { get { throw null; } set { } }
        public bool SubPropertiesDoNotAffectRender { get { throw null; } set { } }
        protected override void Merge(System.Windows.PropertyMetadata baseMetadata, System.Windows.DependencyProperty dp) { }
        protected override void OnApply(System.Windows.DependencyProperty dp, System.Type targetType) { }
    }
    [System.FlagsAttribute]
    public enum FrameworkPropertyMetadataOptions
    {
        None = 0,
        AffectsMeasure = 1,
        AffectsArrange = 2,
        AffectsParentMeasure = 4,
        AffectsParentArrange = 8,
        AffectsRender = 16,
        Inherits = 32,
        OverridesInheritanceBehavior = 64,
        NotDataBindable = 128,
        BindsTwoWayByDefault = 256,
        Journal = 1024,
        SubPropertiesDoNotAffectRender = 2048,
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
    [System.Windows.Markup.ContentPropertyAttribute("VisualTree")]
    public abstract partial class FrameworkTemplate : System.Windows.Threading.DispatcherObject, System.Windows.Markup.INameScope, System.Windows.Markup.IQueryAmbient
    {
        protected FrameworkTemplate() { }
        public bool HasContent { get { throw null; } }
        public bool IsSealed { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        [System.Windows.Markup.AmbientAttribute]
        public System.Windows.ResourceDictionary Resources { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Windows.Markup.AmbientAttribute]
        public System.Windows.TemplateContent Template { get { throw null; } set { } }
        public System.Windows.FrameworkElementFactory VisualTree { get { throw null; } set { } }
        public object FindName(string name, System.Windows.FrameworkElement templatedParent) { throw null; }
        public System.Windows.DependencyObject LoadContent() { throw null; }
        public void RegisterName(string name, object scopedElement) { }
        public void Seal() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeResources(System.Windows.Markup.XamlDesignerSerializationManager manager) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeVisualTree() { throw null; }
        object System.Windows.Markup.INameScope.FindName(string name) { throw null; }
        bool System.Windows.Markup.IQueryAmbient.IsAmbientPropertyAvailable(string propertyName) { throw null; }
        public void UnregisterName(string name) { }
        protected virtual void ValidateTemplatedParent(System.Windows.FrameworkElement templatedParent) { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.GridLengthConverter))]
    public partial struct GridLength : System.IEquatable<System.Windows.GridLength>
    {
        public GridLength(double pixels) { throw null; }
        public GridLength(double value, System.Windows.GridUnitType type) { throw null; }
        public static System.Windows.GridLength Auto { get { throw null; } }
        public System.Windows.GridUnitType GridUnitType { get { throw null; } }
        public bool IsAbsolute { get { throw null; } }
        public bool IsAuto { get { throw null; } }
        public bool IsStar { get { throw null; } }
        public double Value { get { throw null; } }
        public override bool Equals(object oCompare) { throw null; }
        public bool Equals(System.Windows.GridLength gridLength) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.GridLength gl1, System.Windows.GridLength gl2) { throw null; }
        public static bool operator !=(System.Windows.GridLength gl1, System.Windows.GridLength gl2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class GridLengthConverter : System.ComponentModel.TypeConverter
    {
        public GridLengthConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object value, System.Type destinationType) { throw null; }
    }
    public enum GridUnitType
    {
        Auto = 0,
        Pixel = 1,
        Star = 2,
    }
    public partial class HierarchicalDataTemplate : System.Windows.DataTemplate
    {
        public HierarchicalDataTemplate() { }
        public HierarchicalDataTemplate(object dataType) { }
        public int AlternationCount { get { throw null; } set { } }
        public System.Windows.Data.BindingGroup ItemBindingGroup { get { throw null; } set { } }
        public System.Windows.Style ItemContainerStyle { get { throw null; } set { } }
        public System.Windows.Controls.StyleSelector ItemContainerStyleSelector { get { throw null; } set { } }
        public System.Windows.Data.BindingBase ItemsSource { get { throw null; } set { } }
        public string ItemStringFormat { get { throw null; } set { } }
        public System.Windows.DataTemplate ItemTemplate { get { throw null; } set { } }
        public System.Windows.Controls.DataTemplateSelector ItemTemplateSelector { get { throw null; } set { } }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public enum HorizontalAlignment
    {
        Left = 0,
        Center = 1,
        Right = 2,
        Stretch = 3,
    }
    public partial interface IFrameworkInputElement : System.Windows.IInputElement
    {
        string Name { get; set; }
    }
    public enum InheritanceBehavior
    {
        Default = 0,
        SkipToAppNow = 1,
        SkipToAppNext = 2,
        SkipToThemeNow = 3,
        SkipToThemeNext = 4,
        SkipAllNow = 5,
        SkipAllNext = 6,
    }
    public partial class LengthConverter : System.ComponentModel.TypeConverter
    {
        public LengthConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object value, System.Type destinationType) { throw null; }
    }
    public enum LineStackingStrategy
    {
        BlockLineHeight = 0,
        MaxHeight = 1,
    }
    public static partial class Localization
    {
        public static readonly System.Windows.DependencyProperty AttributesProperty;
        public static readonly System.Windows.DependencyProperty CommentsProperty;
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(object))]
        public static string GetAttributes(object element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(object))]
        public static string GetComments(object element) { throw null; }
        public static void SetAttributes(object element, string attributes) { }
        public static void SetComments(object element, string comments) { }
    }
    public static partial class LogicalTreeHelper
    {
        public static void BringIntoView(System.Windows.DependencyObject current) { }
        public static System.Windows.DependencyObject FindLogicalNode(System.Windows.DependencyObject logicalTreeNode, string elementName) { throw null; }
        public static System.Collections.IEnumerable GetChildren(System.Windows.DependencyObject current) { throw null; }
        public static System.Collections.IEnumerable GetChildren(System.Windows.FrameworkContentElement current) { throw null; }
        public static System.Collections.IEnumerable GetChildren(System.Windows.FrameworkElement current) { throw null; }
        public static System.Windows.DependencyObject GetParent(System.Windows.DependencyObject current) { throw null; }
    }
    public partial class LostFocusEventManager : System.Windows.WeakEventManager
    {
        internal LostFocusEventManager() { }
        public static void AddHandler(System.Windows.DependencyObject source, System.EventHandler<System.Windows.RoutedEventArgs> handler) { }
        public static void AddListener(System.Windows.DependencyObject source, System.Windows.IWeakEventListener listener) { }
        protected override System.Windows.WeakEventManager.ListenerList NewListenerList() { throw null; }
        public static void RemoveHandler(System.Windows.DependencyObject source, System.EventHandler<System.Windows.RoutedEventArgs> handler) { }
        public static void RemoveListener(System.Windows.DependencyObject source, System.Windows.IWeakEventListener listener) { }
        protected override void StartListening(object source) { }
        protected override void StopListening(object source) { }
    }
    public sealed partial class MediaScriptCommandRoutedEventArgs : System.Windows.RoutedEventArgs
    {
        internal MediaScriptCommandRoutedEventArgs() { }
        public string ParameterType { get { throw null; } }
        public string ParameterValue { get { throw null; } }
    }
    public sealed partial class MessageBox
    {
        internal MessageBox() { }
        public static System.Windows.MessageBoxResult Show(string messageBoxText) { throw null; }
        public static System.Windows.MessageBoxResult Show(string messageBoxText, string caption) { throw null; }
        public static System.Windows.MessageBoxResult Show(string messageBoxText, string caption, System.Windows.MessageBoxButton button) { throw null; }
        public static System.Windows.MessageBoxResult Show(string messageBoxText, string caption, System.Windows.MessageBoxButton button, System.Windows.MessageBoxImage icon) { throw null; }
        public static System.Windows.MessageBoxResult Show(string messageBoxText, string caption, System.Windows.MessageBoxButton button, System.Windows.MessageBoxImage icon, System.Windows.MessageBoxResult defaultResult) { throw null; }
        public static System.Windows.MessageBoxResult Show(string messageBoxText, string caption, System.Windows.MessageBoxButton button, System.Windows.MessageBoxImage icon, System.Windows.MessageBoxResult defaultResult, System.Windows.MessageBoxOptions options) { throw null; }
        public static System.Windows.MessageBoxResult Show(System.Windows.Window owner, string messageBoxText) { throw null; }
        public static System.Windows.MessageBoxResult Show(System.Windows.Window owner, string messageBoxText, string caption) { throw null; }
        public static System.Windows.MessageBoxResult Show(System.Windows.Window owner, string messageBoxText, string caption, System.Windows.MessageBoxButton button) { throw null; }
        public static System.Windows.MessageBoxResult Show(System.Windows.Window owner, string messageBoxText, string caption, System.Windows.MessageBoxButton button, System.Windows.MessageBoxImage icon) { throw null; }
        public static System.Windows.MessageBoxResult Show(System.Windows.Window owner, string messageBoxText, string caption, System.Windows.MessageBoxButton button, System.Windows.MessageBoxImage icon, System.Windows.MessageBoxResult defaultResult) { throw null; }
        public static System.Windows.MessageBoxResult Show(System.Windows.Window owner, string messageBoxText, string caption, System.Windows.MessageBoxButton button, System.Windows.MessageBoxImage icon, System.Windows.MessageBoxResult defaultResult, System.Windows.MessageBoxOptions options) { throw null; }
    }
    public enum MessageBoxButton
    {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4,
    }
    public enum MessageBoxImage
    {
        None = 0,
        Error = 16,
        Hand = 16,
        Stop = 16,
        Question = 32,
        Exclamation = 48,
        Warning = 48,
        Asterisk = 64,
        Information = 64,
    }
    [System.FlagsAttribute]
    public enum MessageBoxOptions
    {
        None = 0,
        DefaultDesktopOnly = 131072,
        RightAlign = 524288,
        RtlReading = 1048576,
        ServiceNotification = 2097152,
    }
    public enum MessageBoxResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7,
    }
    [System.Windows.Markup.ContentPropertyAttribute("Setters")]
    public sealed partial class MultiDataTrigger : System.Windows.TriggerBase, System.Windows.Markup.IAddChild
    {
        public MultiDataTrigger() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.ConditionCollection Conditions { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.SetterBaseCollection Setters { get { throw null; } }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Setters")]
    public sealed partial class MultiTrigger : System.Windows.TriggerBase, System.Windows.Markup.IAddChild
    {
        public MultiTrigger() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.ConditionCollection Conditions { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.SetterBaseCollection Setters { get { throw null; } }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class NullableBoolConverter : System.ComponentModel.NullableConverter
    {
        public NullableBoolConverter() : base (default(System.Type)) { }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
    }
    public enum PowerLineStatus
    {
        Offline = 0,
        Online = 1,
        Unknown = 255,
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.PropertyPathConverter))]
    public sealed partial class PropertyPath
    {
        public PropertyPath(object parameter) { }
        public PropertyPath(string path, params object[] pathParameters) { }
        public string Path { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<object> PathParameters { get { throw null; } }
    }
    public sealed partial class PropertyPathConverter : System.ComponentModel.TypeConverter
    {
        public PropertyPathConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object value, System.Type destinationType) { throw null; }
    }
    public enum ReasonSessionEnding : byte
    {
        Logoff = (byte)0,
        Shutdown = (byte)1,
    }
    public partial class RequestBringIntoViewEventArgs : System.Windows.RoutedEventArgs
    {
        internal RequestBringIntoViewEventArgs() { }
        public System.Windows.DependencyObject TargetObject { get { throw null; } }
        public System.Windows.Rect TargetRect { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void RequestBringIntoViewEventHandler(object sender, System.Windows.RequestBringIntoViewEventArgs e);
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public enum ResizeMode
    {
        NoResize = 0,
        CanMinimize = 1,
        CanResize = 2,
        CanResizeWithGrip = 3,
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore)]
    [System.Windows.Markup.AmbientAttribute]
    [System.Windows.Markup.UsableDuringInitializationAttribute(true)]
    public partial class ResourceDictionary : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable, System.ComponentModel.ISupportInitialize, System.Windows.Markup.INameScope, System.Windows.Markup.IUriContext
    {
        public ResourceDictionary() { }
        public int Count { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.DeferrableContent DeferrableContent { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool InvalidatesImplicitDataTemplateResources { get { throw null; } set { } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public object this[object key] { get { throw null; } set { } }
        public System.Collections.ICollection Keys { get { throw null; } }
        public System.Collections.ObjectModel.Collection<System.Windows.ResourceDictionary> MergedDictionaries { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Uri Source { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        public System.Collections.ICollection Values { get { throw null; } }
        public void Add(object key, object value) { }
        public void BeginInit() { }
        public void Clear() { }
        public bool Contains(object key) { throw null; }
        public void CopyTo(System.Collections.DictionaryEntry[] array, int arrayIndex) { }
        public void EndInit() { }
        public object FindName(string name) { throw null; }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        protected virtual void OnGettingValue(object key, ref object value, out bool canCache) { throw null; }
        public void RegisterName(string name, object scopedElement) { }
        public void Remove(object key) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public void UnregisterName(string name) { }
    }
    public enum ResourceDictionaryLocation
    {
        None = 0,
        SourceAssembly = 1,
        ExternalAssembly = 2,
    }
    [System.Windows.Markup.MarkupExtensionReturnTypeAttribute(typeof(System.Windows.ResourceKey))]
    public abstract partial class ResourceKey : System.Windows.Markup.MarkupExtension
    {
        protected ResourceKey() { }
        public abstract System.Reflection.Assembly Assembly { get; }
        public override object ProvideValue(System.IServiceProvider serviceProvider) { throw null; }
    }
    public partial class ResourceReferenceKeyNotFoundException : System.InvalidOperationException
    {
        public ResourceReferenceKeyNotFoundException() { }
        protected ResourceReferenceKeyNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ResourceReferenceKeyNotFoundException(string message, object resourceKey) { }
        public object Key { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class RoutedPropertyChangedEventArgs<T> : System.Windows.RoutedEventArgs
    {
        public RoutedPropertyChangedEventArgs(T oldValue, T newValue) { }
        public RoutedPropertyChangedEventArgs(T oldValue, T newValue, System.Windows.RoutedEvent routedEvent) { }
        public T NewValue { get { throw null; } }
        public T OldValue { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void RoutedPropertyChangedEventHandler<T>(object sender, System.Windows.RoutedPropertyChangedEventArgs<T> e);
    public partial class SessionEndingCancelEventArgs : System.ComponentModel.CancelEventArgs
    {
        internal SessionEndingCancelEventArgs() { }
        public System.Windows.ReasonSessionEnding ReasonSessionEnding { get { throw null; } }
    }
    public delegate void SessionEndingCancelEventHandler(object sender, System.Windows.SessionEndingCancelEventArgs e);
    [System.Windows.Markup.XamlSetMarkupExtensionAttribute("ReceiveMarkupExtension")]
    [System.Windows.Markup.XamlSetTypeConverterAttribute("ReceiveTypeConverter")]
    public partial class Setter : System.Windows.SetterBase, System.ComponentModel.ISupportInitialize
    {
        public Setter() { }
        public Setter(System.Windows.DependencyProperty property, object value) { }
        public Setter(System.Windows.DependencyProperty property, object value, string targetName) { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Modifiability=System.Windows.Modifiability.Unmodifiable, Readability=System.Windows.Readability.Unreadable)]
        [System.Windows.Markup.AmbientAttribute]
        public System.Windows.DependencyProperty Property { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Windows.Markup.AmbientAttribute]
        public string TargetName { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Markup.SetterTriggerConditionValueConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        [System.Windows.Markup.DependsOnAttribute("Property")]
        [System.Windows.Markup.DependsOnAttribute("TargetName")]
        public object Value { get { throw null; } set { } }
        public static void ReceiveMarkupExtension(object targetObject, System.Windows.Markup.XamlSetMarkupExtensionEventArgs eventArgs) { }
        public static void ReceiveTypeConverter(object targetObject, System.Windows.Markup.XamlSetTypeConverterEventArgs eventArgs) { }
        void System.ComponentModel.ISupportInitialize.BeginInit() { }
        void System.ComponentModel.ISupportInitialize.EndInit() { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore)]
    public abstract partial class SetterBase
    {
        internal SetterBase() { }
        public bool IsSealed { get { throw null; } }
        protected void CheckSealed() { }
    }
    public sealed partial class SetterBaseCollection : System.Collections.ObjectModel.Collection<System.Windows.SetterBase>
    {
        public SetterBaseCollection() { }
        public bool IsSealed { get { throw null; } }
        protected override void ClearItems() { }
        protected override void InsertItem(int index, System.Windows.SetterBase item) { }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, System.Windows.SetterBase item) { }
    }
    public enum ShutdownMode : byte
    {
        OnLastWindowClose = (byte)0,
        OnMainWindowClose = (byte)1,
        OnExplicitShutdown = (byte)2,
    }
    public partial class SizeChangedEventArgs : System.Windows.RoutedEventArgs
    {
        internal SizeChangedEventArgs() { }
        public bool HeightChanged { get { throw null; } }
        public System.Windows.Size NewSize { get { throw null; } }
        public System.Windows.Size PreviousSize { get { throw null; } }
        public bool WidthChanged { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void SizeChangedEventHandler(object sender, System.Windows.SizeChangedEventArgs e);
    public partial class StartupEventArgs : System.EventArgs
    {
        internal StartupEventArgs() { }
        public string[] Args { get { throw null; } }
    }
    public delegate void StartupEventHandler(object sender, System.Windows.StartupEventArgs e);
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
    [System.Windows.Markup.MarkupExtensionReturnTypeAttribute(typeof(object))]
    public partial class StaticResourceExtension : System.Windows.Markup.MarkupExtension
    {
        public StaticResourceExtension() { }
        public StaticResourceExtension(object resourceKey) { }
        [System.Windows.Markup.ConstructorArgumentAttribute("resourceKey")]
        public object ResourceKey { get { throw null; } set { } }
        public override object ProvideValue(System.IServiceProvider serviceProvider) { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore)]
    [System.Windows.Markup.ContentPropertyAttribute("Setters")]
    [System.Windows.Markup.DictionaryKeyPropertyAttribute("TargetType")]
    public partial class Style : System.Windows.Threading.DispatcherObject, System.Windows.Markup.IAddChild, System.Windows.Markup.INameScope, System.Windows.Markup.IQueryAmbient
    {
        public Style() { }
        public Style(System.Type targetType) { }
        public Style(System.Type targetType, System.Windows.Style basedOn) { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Windows.Markup.AmbientAttribute]
        public System.Windows.Style BasedOn { get { throw null; } set { } }
        public bool IsSealed { get { throw null; } }
        [System.Windows.Markup.AmbientAttribute]
        public System.Windows.ResourceDictionary Resources { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.SetterBaseCollection Setters { get { throw null; } }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        [System.Windows.Markup.AmbientAttribute]
        public System.Type TargetType { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.TriggerCollection Triggers { get { throw null; } }
        public override int GetHashCode() { throw null; }
        public void RegisterName(string name, object scopedElement) { }
        public void Seal() { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
        object System.Windows.Markup.INameScope.FindName(string name) { throw null; }
        bool System.Windows.Markup.IQueryAmbient.IsAmbientPropertyAvailable(string propertyName) { throw null; }
        public void UnregisterName(string name) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true)]
    public sealed partial class StyleTypedPropertyAttribute : System.Attribute
    {
        public StyleTypedPropertyAttribute() { }
        public string Property { get { throw null; } set { } }
        public System.Type StyleTargetType { get { throw null; } set { } }
    }
    public static partial class SystemColors
    {
        public static System.Windows.Media.SolidColorBrush ActiveBorderBrush { get { throw null; } }
        public static System.Windows.ResourceKey ActiveBorderBrushKey { get { throw null; } }
        public static System.Windows.Media.Color ActiveBorderColor { get { throw null; } }
        public static System.Windows.ResourceKey ActiveBorderColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush ActiveCaptionBrush { get { throw null; } }
        public static System.Windows.ResourceKey ActiveCaptionBrushKey { get { throw null; } }
        public static System.Windows.Media.Color ActiveCaptionColor { get { throw null; } }
        public static System.Windows.ResourceKey ActiveCaptionColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush ActiveCaptionTextBrush { get { throw null; } }
        public static System.Windows.ResourceKey ActiveCaptionTextBrushKey { get { throw null; } }
        public static System.Windows.Media.Color ActiveCaptionTextColor { get { throw null; } }
        public static System.Windows.ResourceKey ActiveCaptionTextColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush AppWorkspaceBrush { get { throw null; } }
        public static System.Windows.ResourceKey AppWorkspaceBrushKey { get { throw null; } }
        public static System.Windows.Media.Color AppWorkspaceColor { get { throw null; } }
        public static System.Windows.ResourceKey AppWorkspaceColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush ControlBrush { get { throw null; } }
        public static System.Windows.ResourceKey ControlBrushKey { get { throw null; } }
        public static System.Windows.Media.Color ControlColor { get { throw null; } }
        public static System.Windows.ResourceKey ControlColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush ControlDarkBrush { get { throw null; } }
        public static System.Windows.ResourceKey ControlDarkBrushKey { get { throw null; } }
        public static System.Windows.Media.Color ControlDarkColor { get { throw null; } }
        public static System.Windows.ResourceKey ControlDarkColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush ControlDarkDarkBrush { get { throw null; } }
        public static System.Windows.ResourceKey ControlDarkDarkBrushKey { get { throw null; } }
        public static System.Windows.Media.Color ControlDarkDarkColor { get { throw null; } }
        public static System.Windows.ResourceKey ControlDarkDarkColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush ControlLightBrush { get { throw null; } }
        public static System.Windows.ResourceKey ControlLightBrushKey { get { throw null; } }
        public static System.Windows.Media.Color ControlLightColor { get { throw null; } }
        public static System.Windows.ResourceKey ControlLightColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush ControlLightLightBrush { get { throw null; } }
        public static System.Windows.ResourceKey ControlLightLightBrushKey { get { throw null; } }
        public static System.Windows.Media.Color ControlLightLightColor { get { throw null; } }
        public static System.Windows.ResourceKey ControlLightLightColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush ControlTextBrush { get { throw null; } }
        public static System.Windows.ResourceKey ControlTextBrushKey { get { throw null; } }
        public static System.Windows.Media.Color ControlTextColor { get { throw null; } }
        public static System.Windows.ResourceKey ControlTextColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush DesktopBrush { get { throw null; } }
        public static System.Windows.ResourceKey DesktopBrushKey { get { throw null; } }
        public static System.Windows.Media.Color DesktopColor { get { throw null; } }
        public static System.Windows.ResourceKey DesktopColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush GradientActiveCaptionBrush { get { throw null; } }
        public static System.Windows.ResourceKey GradientActiveCaptionBrushKey { get { throw null; } }
        public static System.Windows.Media.Color GradientActiveCaptionColor { get { throw null; } }
        public static System.Windows.ResourceKey GradientActiveCaptionColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush GradientInactiveCaptionBrush { get { throw null; } }
        public static System.Windows.ResourceKey GradientInactiveCaptionBrushKey { get { throw null; } }
        public static System.Windows.Media.Color GradientInactiveCaptionColor { get { throw null; } }
        public static System.Windows.ResourceKey GradientInactiveCaptionColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush GrayTextBrush { get { throw null; } }
        public static System.Windows.ResourceKey GrayTextBrushKey { get { throw null; } }
        public static System.Windows.Media.Color GrayTextColor { get { throw null; } }
        public static System.Windows.ResourceKey GrayTextColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush HighlightBrush { get { throw null; } }
        public static System.Windows.ResourceKey HighlightBrushKey { get { throw null; } }
        public static System.Windows.Media.Color HighlightColor { get { throw null; } }
        public static System.Windows.ResourceKey HighlightColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush HighlightTextBrush { get { throw null; } }
        public static System.Windows.ResourceKey HighlightTextBrushKey { get { throw null; } }
        public static System.Windows.Media.Color HighlightTextColor { get { throw null; } }
        public static System.Windows.ResourceKey HighlightTextColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush HotTrackBrush { get { throw null; } }
        public static System.Windows.ResourceKey HotTrackBrushKey { get { throw null; } }
        public static System.Windows.Media.Color HotTrackColor { get { throw null; } }
        public static System.Windows.ResourceKey HotTrackColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush InactiveBorderBrush { get { throw null; } }
        public static System.Windows.ResourceKey InactiveBorderBrushKey { get { throw null; } }
        public static System.Windows.Media.Color InactiveBorderColor { get { throw null; } }
        public static System.Windows.ResourceKey InactiveBorderColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush InactiveCaptionBrush { get { throw null; } }
        public static System.Windows.ResourceKey InactiveCaptionBrushKey { get { throw null; } }
        public static System.Windows.Media.Color InactiveCaptionColor { get { throw null; } }
        public static System.Windows.ResourceKey InactiveCaptionColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush InactiveCaptionTextBrush { get { throw null; } }
        public static System.Windows.ResourceKey InactiveCaptionTextBrushKey { get { throw null; } }
        public static System.Windows.Media.Color InactiveCaptionTextColor { get { throw null; } }
        public static System.Windows.ResourceKey InactiveCaptionTextColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush InactiveSelectionHighlightBrush { get { throw null; } }
        public static System.Windows.ResourceKey InactiveSelectionHighlightBrushKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush InactiveSelectionHighlightTextBrush { get { throw null; } }
        public static System.Windows.ResourceKey InactiveSelectionHighlightTextBrushKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush InfoBrush { get { throw null; } }
        public static System.Windows.ResourceKey InfoBrushKey { get { throw null; } }
        public static System.Windows.Media.Color InfoColor { get { throw null; } }
        public static System.Windows.ResourceKey InfoColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush InfoTextBrush { get { throw null; } }
        public static System.Windows.ResourceKey InfoTextBrushKey { get { throw null; } }
        public static System.Windows.Media.Color InfoTextColor { get { throw null; } }
        public static System.Windows.ResourceKey InfoTextColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush MenuBarBrush { get { throw null; } }
        public static System.Windows.ResourceKey MenuBarBrushKey { get { throw null; } }
        public static System.Windows.Media.Color MenuBarColor { get { throw null; } }
        public static System.Windows.ResourceKey MenuBarColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush MenuBrush { get { throw null; } }
        public static System.Windows.ResourceKey MenuBrushKey { get { throw null; } }
        public static System.Windows.Media.Color MenuColor { get { throw null; } }
        public static System.Windows.ResourceKey MenuColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush MenuHighlightBrush { get { throw null; } }
        public static System.Windows.ResourceKey MenuHighlightBrushKey { get { throw null; } }
        public static System.Windows.Media.Color MenuHighlightColor { get { throw null; } }
        public static System.Windows.ResourceKey MenuHighlightColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush MenuTextBrush { get { throw null; } }
        public static System.Windows.ResourceKey MenuTextBrushKey { get { throw null; } }
        public static System.Windows.Media.Color MenuTextColor { get { throw null; } }
        public static System.Windows.ResourceKey MenuTextColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush ScrollBarBrush { get { throw null; } }
        public static System.Windows.ResourceKey ScrollBarBrushKey { get { throw null; } }
        public static System.Windows.Media.Color ScrollBarColor { get { throw null; } }
        public static System.Windows.ResourceKey ScrollBarColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush WindowBrush { get { throw null; } }
        public static System.Windows.ResourceKey WindowBrushKey { get { throw null; } }
        public static System.Windows.Media.Color WindowColor { get { throw null; } }
        public static System.Windows.ResourceKey WindowColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush WindowFrameBrush { get { throw null; } }
        public static System.Windows.ResourceKey WindowFrameBrushKey { get { throw null; } }
        public static System.Windows.Media.Color WindowFrameColor { get { throw null; } }
        public static System.Windows.ResourceKey WindowFrameColorKey { get { throw null; } }
        public static System.Windows.Media.SolidColorBrush WindowTextBrush { get { throw null; } }
        public static System.Windows.ResourceKey WindowTextBrushKey { get { throw null; } }
        public static System.Windows.Media.Color WindowTextColor { get { throw null; } }
        public static System.Windows.ResourceKey WindowTextColorKey { get { throw null; } }
    }
    public static partial class SystemCommands
    {
        public static System.Windows.Input.RoutedCommand CloseWindowCommand { get { throw null; } }
        public static System.Windows.Input.RoutedCommand MaximizeWindowCommand { get { throw null; } }
        public static System.Windows.Input.RoutedCommand MinimizeWindowCommand { get { throw null; } }
        public static System.Windows.Input.RoutedCommand RestoreWindowCommand { get { throw null; } }
        public static System.Windows.Input.RoutedCommand ShowSystemMenuCommand { get { throw null; } }
        public static void CloseWindow(System.Windows.Window window) { }
        public static void MaximizeWindow(System.Windows.Window window) { }
        public static void MinimizeWindow(System.Windows.Window window) { }
        public static void RestoreWindow(System.Windows.Window window) { }
        public static void ShowSystemMenu(System.Windows.Window window, System.Windows.Point screenLocation) { }
    }
    public static partial class SystemFonts
    {
        public static System.Windows.Media.FontFamily CaptionFontFamily { get { throw null; } }
        public static System.Windows.ResourceKey CaptionFontFamilyKey { get { throw null; } }
        public static double CaptionFontSize { get { throw null; } }
        public static System.Windows.ResourceKey CaptionFontSizeKey { get { throw null; } }
        public static System.Windows.FontStyle CaptionFontStyle { get { throw null; } }
        public static System.Windows.ResourceKey CaptionFontStyleKey { get { throw null; } }
        public static System.Windows.TextDecorationCollection CaptionFontTextDecorations { get { throw null; } }
        public static System.Windows.ResourceKey CaptionFontTextDecorationsKey { get { throw null; } }
        public static System.Windows.FontWeight CaptionFontWeight { get { throw null; } }
        public static System.Windows.ResourceKey CaptionFontWeightKey { get { throw null; } }
        public static System.Windows.Media.FontFamily IconFontFamily { get { throw null; } }
        public static System.Windows.ResourceKey IconFontFamilyKey { get { throw null; } }
        public static double IconFontSize { get { throw null; } }
        public static System.Windows.ResourceKey IconFontSizeKey { get { throw null; } }
        public static System.Windows.FontStyle IconFontStyle { get { throw null; } }
        public static System.Windows.ResourceKey IconFontStyleKey { get { throw null; } }
        public static System.Windows.TextDecorationCollection IconFontTextDecorations { get { throw null; } }
        public static System.Windows.ResourceKey IconFontTextDecorationsKey { get { throw null; } }
        public static System.Windows.FontWeight IconFontWeight { get { throw null; } }
        public static System.Windows.ResourceKey IconFontWeightKey { get { throw null; } }
        public static System.Windows.Media.FontFamily MenuFontFamily { get { throw null; } }
        public static System.Windows.ResourceKey MenuFontFamilyKey { get { throw null; } }
        public static double MenuFontSize { get { throw null; } }
        public static System.Windows.ResourceKey MenuFontSizeKey { get { throw null; } }
        public static System.Windows.FontStyle MenuFontStyle { get { throw null; } }
        public static System.Windows.ResourceKey MenuFontStyleKey { get { throw null; } }
        public static System.Windows.TextDecorationCollection MenuFontTextDecorations { get { throw null; } }
        public static System.Windows.ResourceKey MenuFontTextDecorationsKey { get { throw null; } }
        public static System.Windows.FontWeight MenuFontWeight { get { throw null; } }
        public static System.Windows.ResourceKey MenuFontWeightKey { get { throw null; } }
        public static System.Windows.Media.FontFamily MessageFontFamily { get { throw null; } }
        public static System.Windows.ResourceKey MessageFontFamilyKey { get { throw null; } }
        public static double MessageFontSize { get { throw null; } }
        public static System.Windows.ResourceKey MessageFontSizeKey { get { throw null; } }
        public static System.Windows.FontStyle MessageFontStyle { get { throw null; } }
        public static System.Windows.ResourceKey MessageFontStyleKey { get { throw null; } }
        public static System.Windows.TextDecorationCollection MessageFontTextDecorations { get { throw null; } }
        public static System.Windows.ResourceKey MessageFontTextDecorationsKey { get { throw null; } }
        public static System.Windows.FontWeight MessageFontWeight { get { throw null; } }
        public static System.Windows.ResourceKey MessageFontWeightKey { get { throw null; } }
        public static System.Windows.Media.FontFamily SmallCaptionFontFamily { get { throw null; } }
        public static System.Windows.ResourceKey SmallCaptionFontFamilyKey { get { throw null; } }
        public static double SmallCaptionFontSize { get { throw null; } }
        public static System.Windows.ResourceKey SmallCaptionFontSizeKey { get { throw null; } }
        public static System.Windows.FontStyle SmallCaptionFontStyle { get { throw null; } }
        public static System.Windows.ResourceKey SmallCaptionFontStyleKey { get { throw null; } }
        public static System.Windows.TextDecorationCollection SmallCaptionFontTextDecorations { get { throw null; } }
        public static System.Windows.ResourceKey SmallCaptionFontTextDecorationsKey { get { throw null; } }
        public static System.Windows.FontWeight SmallCaptionFontWeight { get { throw null; } }
        public static System.Windows.ResourceKey SmallCaptionFontWeightKey { get { throw null; } }
        public static System.Windows.Media.FontFamily StatusFontFamily { get { throw null; } }
        public static System.Windows.ResourceKey StatusFontFamilyKey { get { throw null; } }
        public static double StatusFontSize { get { throw null; } }
        public static System.Windows.ResourceKey StatusFontSizeKey { get { throw null; } }
        public static System.Windows.FontStyle StatusFontStyle { get { throw null; } }
        public static System.Windows.ResourceKey StatusFontStyleKey { get { throw null; } }
        public static System.Windows.TextDecorationCollection StatusFontTextDecorations { get { throw null; } }
        public static System.Windows.ResourceKey StatusFontTextDecorationsKey { get { throw null; } }
        public static System.Windows.FontWeight StatusFontWeight { get { throw null; } }
        public static System.Windows.ResourceKey StatusFontWeightKey { get { throw null; } }
    }
    public static partial class SystemParameters
    {
        public static int Border { get { throw null; } }
        public static System.Windows.ResourceKey BorderKey { get { throw null; } }
        public static double BorderWidth { get { throw null; } }
        public static System.Windows.ResourceKey BorderWidthKey { get { throw null; } }
        public static double CaptionHeight { get { throw null; } }
        public static System.Windows.ResourceKey CaptionHeightKey { get { throw null; } }
        public static double CaptionWidth { get { throw null; } }
        public static System.Windows.ResourceKey CaptionWidthKey { get { throw null; } }
        public static double CaretWidth { get { throw null; } }
        public static System.Windows.ResourceKey CaretWidthKey { get { throw null; } }
        public static bool ClientAreaAnimation { get { throw null; } }
        public static System.Windows.ResourceKey ClientAreaAnimationKey { get { throw null; } }
        public static bool ComboBoxAnimation { get { throw null; } }
        public static System.Windows.ResourceKey ComboBoxAnimationKey { get { throw null; } }
        public static System.Windows.Controls.Primitives.PopupAnimation ComboBoxPopupAnimation { get { throw null; } }
        public static System.Windows.ResourceKey ComboBoxPopupAnimationKey { get { throw null; } }
        public static double CursorHeight { get { throw null; } }
        public static System.Windows.ResourceKey CursorHeightKey { get { throw null; } }
        public static bool CursorShadow { get { throw null; } }
        public static System.Windows.ResourceKey CursorShadowKey { get { throw null; } }
        public static double CursorWidth { get { throw null; } }
        public static System.Windows.ResourceKey CursorWidthKey { get { throw null; } }
        public static bool DragFullWindows { get { throw null; } }
        public static System.Windows.ResourceKey DragFullWindowsKey { get { throw null; } }
        public static bool DropShadow { get { throw null; } }
        public static System.Windows.ResourceKey DropShadowKey { get { throw null; } }
        public static double FixedFrameHorizontalBorderHeight { get { throw null; } }
        public static System.Windows.ResourceKey FixedFrameHorizontalBorderHeightKey { get { throw null; } }
        public static double FixedFrameVerticalBorderWidth { get { throw null; } }
        public static System.Windows.ResourceKey FixedFrameVerticalBorderWidthKey { get { throw null; } }
        public static bool FlatMenu { get { throw null; } }
        public static System.Windows.ResourceKey FlatMenuKey { get { throw null; } }
        public static double FocusBorderHeight { get { throw null; } }
        public static System.Windows.ResourceKey FocusBorderHeightKey { get { throw null; } }
        public static double FocusBorderWidth { get { throw null; } }
        public static System.Windows.ResourceKey FocusBorderWidthKey { get { throw null; } }
        public static double FocusHorizontalBorderHeight { get { throw null; } }
        public static System.Windows.ResourceKey FocusHorizontalBorderHeightKey { get { throw null; } }
        public static double FocusVerticalBorderWidth { get { throw null; } }
        public static System.Windows.ResourceKey FocusVerticalBorderWidthKey { get { throw null; } }
        public static System.Windows.ResourceKey FocusVisualStyleKey { get { throw null; } }
        public static int ForegroundFlashCount { get { throw null; } }
        public static System.Windows.ResourceKey ForegroundFlashCountKey { get { throw null; } }
        public static double FullPrimaryScreenHeight { get { throw null; } }
        public static System.Windows.ResourceKey FullPrimaryScreenHeightKey { get { throw null; } }
        public static double FullPrimaryScreenWidth { get { throw null; } }
        public static System.Windows.ResourceKey FullPrimaryScreenWidthKey { get { throw null; } }
        public static bool GradientCaptions { get { throw null; } }
        public static System.Windows.ResourceKey GradientCaptionsKey { get { throw null; } }
        public static bool HighContrast { get { throw null; } }
        public static System.Windows.ResourceKey HighContrastKey { get { throw null; } }
        public static double HorizontalScrollBarButtonWidth { get { throw null; } }
        public static System.Windows.ResourceKey HorizontalScrollBarButtonWidthKey { get { throw null; } }
        public static double HorizontalScrollBarHeight { get { throw null; } }
        public static System.Windows.ResourceKey HorizontalScrollBarHeightKey { get { throw null; } }
        public static double HorizontalScrollBarThumbWidth { get { throw null; } }
        public static System.Windows.ResourceKey HorizontalScrollBarThumbWidthKey { get { throw null; } }
        public static bool HotTracking { get { throw null; } }
        public static System.Windows.ResourceKey HotTrackingKey { get { throw null; } }
        public static double IconGridHeight { get { throw null; } }
        public static System.Windows.ResourceKey IconGridHeightKey { get { throw null; } }
        public static double IconGridWidth { get { throw null; } }
        public static System.Windows.ResourceKey IconGridWidthKey { get { throw null; } }
        public static double IconHeight { get { throw null; } }
        public static System.Windows.ResourceKey IconHeightKey { get { throw null; } }
        public static double IconHorizontalSpacing { get { throw null; } }
        public static System.Windows.ResourceKey IconHorizontalSpacingKey { get { throw null; } }
        public static bool IconTitleWrap { get { throw null; } }
        public static System.Windows.ResourceKey IconTitleWrapKey { get { throw null; } }
        public static double IconVerticalSpacing { get { throw null; } }
        public static System.Windows.ResourceKey IconVerticalSpacingKey { get { throw null; } }
        public static double IconWidth { get { throw null; } }
        public static System.Windows.ResourceKey IconWidthKey { get { throw null; } }
        public static bool IsGlassEnabled { get { throw null; } }
        public static bool IsImmEnabled { get { throw null; } }
        public static System.Windows.ResourceKey IsImmEnabledKey { get { throw null; } }
        public static bool IsMediaCenter { get { throw null; } }
        public static System.Windows.ResourceKey IsMediaCenterKey { get { throw null; } }
        public static bool IsMenuDropRightAligned { get { throw null; } }
        public static System.Windows.ResourceKey IsMenuDropRightAlignedKey { get { throw null; } }
        public static bool IsMiddleEastEnabled { get { throw null; } }
        public static System.Windows.ResourceKey IsMiddleEastEnabledKey { get { throw null; } }
        public static bool IsMousePresent { get { throw null; } }
        public static System.Windows.ResourceKey IsMousePresentKey { get { throw null; } }
        public static bool IsMouseWheelPresent { get { throw null; } }
        public static System.Windows.ResourceKey IsMouseWheelPresentKey { get { throw null; } }
        public static bool IsPenWindows { get { throw null; } }
        public static System.Windows.ResourceKey IsPenWindowsKey { get { throw null; } }
        public static bool IsRemotelyControlled { get { throw null; } }
        public static System.Windows.ResourceKey IsRemotelyControlledKey { get { throw null; } }
        public static bool IsRemoteSession { get { throw null; } }
        public static System.Windows.ResourceKey IsRemoteSessionKey { get { throw null; } }
        public static bool IsSlowMachine { get { throw null; } }
        public static System.Windows.ResourceKey IsSlowMachineKey { get { throw null; } }
        public static bool IsTabletPC { get { throw null; } }
        public static System.Windows.ResourceKey IsTabletPCKey { get { throw null; } }
        public static double KanjiWindowHeight { get { throw null; } }
        public static System.Windows.ResourceKey KanjiWindowHeightKey { get { throw null; } }
        public static bool KeyboardCues { get { throw null; } }
        public static System.Windows.ResourceKey KeyboardCuesKey { get { throw null; } }
        public static int KeyboardDelay { get { throw null; } }
        public static System.Windows.ResourceKey KeyboardDelayKey { get { throw null; } }
        public static bool KeyboardPreference { get { throw null; } }
        public static System.Windows.ResourceKey KeyboardPreferenceKey { get { throw null; } }
        public static int KeyboardSpeed { get { throw null; } }
        public static System.Windows.ResourceKey KeyboardSpeedKey { get { throw null; } }
        public static bool ListBoxSmoothScrolling { get { throw null; } }
        public static System.Windows.ResourceKey ListBoxSmoothScrollingKey { get { throw null; } }
        public static double MaximizedPrimaryScreenHeight { get { throw null; } }
        public static System.Windows.ResourceKey MaximizedPrimaryScreenHeightKey { get { throw null; } }
        public static double MaximizedPrimaryScreenWidth { get { throw null; } }
        public static System.Windows.ResourceKey MaximizedPrimaryScreenWidthKey { get { throw null; } }
        public static double MaximumWindowTrackHeight { get { throw null; } }
        public static System.Windows.ResourceKey MaximumWindowTrackHeightKey { get { throw null; } }
        public static double MaximumWindowTrackWidth { get { throw null; } }
        public static System.Windows.ResourceKey MaximumWindowTrackWidthKey { get { throw null; } }
        public static bool MenuAnimation { get { throw null; } }
        public static System.Windows.ResourceKey MenuAnimationKey { get { throw null; } }
        public static double MenuBarHeight { get { throw null; } }
        public static System.Windows.ResourceKey MenuBarHeightKey { get { throw null; } }
        public static double MenuButtonHeight { get { throw null; } }
        public static System.Windows.ResourceKey MenuButtonHeightKey { get { throw null; } }
        public static double MenuButtonWidth { get { throw null; } }
        public static System.Windows.ResourceKey MenuButtonWidthKey { get { throw null; } }
        public static double MenuCheckmarkHeight { get { throw null; } }
        public static System.Windows.ResourceKey MenuCheckmarkHeightKey { get { throw null; } }
        public static double MenuCheckmarkWidth { get { throw null; } }
        public static System.Windows.ResourceKey MenuCheckmarkWidthKey { get { throw null; } }
        public static bool MenuDropAlignment { get { throw null; } }
        public static System.Windows.ResourceKey MenuDropAlignmentKey { get { throw null; } }
        public static bool MenuFade { get { throw null; } }
        public static System.Windows.ResourceKey MenuFadeKey { get { throw null; } }
        public static double MenuHeight { get { throw null; } }
        public static System.Windows.ResourceKey MenuHeightKey { get { throw null; } }
        public static System.Windows.Controls.Primitives.PopupAnimation MenuPopupAnimation { get { throw null; } }
        public static System.Windows.ResourceKey MenuPopupAnimationKey { get { throw null; } }
        public static int MenuShowDelay { get { throw null; } }
        public static System.Windows.ResourceKey MenuShowDelayKey { get { throw null; } }
        public static double MenuWidth { get { throw null; } }
        public static System.Windows.ResourceKey MenuWidthKey { get { throw null; } }
        public static bool MinimizeAnimation { get { throw null; } }
        public static System.Windows.ResourceKey MinimizeAnimationKey { get { throw null; } }
        public static double MinimizedGridHeight { get { throw null; } }
        public static System.Windows.ResourceKey MinimizedGridHeightKey { get { throw null; } }
        public static double MinimizedGridWidth { get { throw null; } }
        public static System.Windows.ResourceKey MinimizedGridWidthKey { get { throw null; } }
        public static double MinimizedWindowHeight { get { throw null; } }
        public static System.Windows.ResourceKey MinimizedWindowHeightKey { get { throw null; } }
        public static double MinimizedWindowWidth { get { throw null; } }
        public static System.Windows.ResourceKey MinimizedWindowWidthKey { get { throw null; } }
        public static double MinimumHorizontalDragDistance { get { throw null; } }
        public static double MinimumVerticalDragDistance { get { throw null; } }
        public static double MinimumWindowHeight { get { throw null; } }
        public static System.Windows.ResourceKey MinimumWindowHeightKey { get { throw null; } }
        public static double MinimumWindowTrackHeight { get { throw null; } }
        public static System.Windows.ResourceKey MinimumWindowTrackHeightKey { get { throw null; } }
        public static double MinimumWindowTrackWidth { get { throw null; } }
        public static System.Windows.ResourceKey MinimumWindowTrackWidthKey { get { throw null; } }
        public static double MinimumWindowWidth { get { throw null; } }
        public static System.Windows.ResourceKey MinimumWindowWidthKey { get { throw null; } }
        public static double MouseHoverHeight { get { throw null; } }
        public static System.Windows.ResourceKey MouseHoverHeightKey { get { throw null; } }
        public static System.TimeSpan MouseHoverTime { get { throw null; } }
        public static System.Windows.ResourceKey MouseHoverTimeKey { get { throw null; } }
        public static double MouseHoverWidth { get { throw null; } }
        public static System.Windows.ResourceKey MouseHoverWidthKey { get { throw null; } }
        public static System.Windows.ResourceKey NavigationChromeDownLevelStyleKey { get { throw null; } }
        public static System.Windows.ResourceKey NavigationChromeStyleKey { get { throw null; } }
        public static System.Windows.PowerLineStatus PowerLineStatus { get { throw null; } }
        public static System.Windows.ResourceKey PowerLineStatusKey { get { throw null; } }
        public static double PrimaryScreenHeight { get { throw null; } }
        public static System.Windows.ResourceKey PrimaryScreenHeightKey { get { throw null; } }
        public static double PrimaryScreenWidth { get { throw null; } }
        public static System.Windows.ResourceKey PrimaryScreenWidthKey { get { throw null; } }
        public static double ResizeFrameHorizontalBorderHeight { get { throw null; } }
        public static System.Windows.ResourceKey ResizeFrameHorizontalBorderHeightKey { get { throw null; } }
        public static double ResizeFrameVerticalBorderWidth { get { throw null; } }
        public static System.Windows.ResourceKey ResizeFrameVerticalBorderWidthKey { get { throw null; } }
        public static double ScrollHeight { get { throw null; } }
        public static System.Windows.ResourceKey ScrollHeightKey { get { throw null; } }
        public static double ScrollWidth { get { throw null; } }
        public static System.Windows.ResourceKey ScrollWidthKey { get { throw null; } }
        public static bool SelectionFade { get { throw null; } }
        public static System.Windows.ResourceKey SelectionFadeKey { get { throw null; } }
        public static bool ShowSounds { get { throw null; } }
        public static System.Windows.ResourceKey ShowSoundsKey { get { throw null; } }
        public static double SmallCaptionHeight { get { throw null; } }
        public static System.Windows.ResourceKey SmallCaptionHeightKey { get { throw null; } }
        public static double SmallCaptionWidth { get { throw null; } }
        public static System.Windows.ResourceKey SmallCaptionWidthKey { get { throw null; } }
        public static double SmallIconHeight { get { throw null; } }
        public static System.Windows.ResourceKey SmallIconHeightKey { get { throw null; } }
        public static double SmallIconWidth { get { throw null; } }
        public static System.Windows.ResourceKey SmallIconWidthKey { get { throw null; } }
        public static double SmallWindowCaptionButtonHeight { get { throw null; } }
        public static System.Windows.ResourceKey SmallWindowCaptionButtonHeightKey { get { throw null; } }
        public static double SmallWindowCaptionButtonWidth { get { throw null; } }
        public static System.Windows.ResourceKey SmallWindowCaptionButtonWidthKey { get { throw null; } }
        public static bool SnapToDefaultButton { get { throw null; } }
        public static System.Windows.ResourceKey SnapToDefaultButtonKey { get { throw null; } }
        public static bool StylusHotTracking { get { throw null; } }
        public static System.Windows.ResourceKey StylusHotTrackingKey { get { throw null; } }
        public static bool SwapButtons { get { throw null; } }
        public static System.Windows.ResourceKey SwapButtonsKey { get { throw null; } }
        public static double ThickHorizontalBorderHeight { get { throw null; } }
        public static System.Windows.ResourceKey ThickHorizontalBorderHeightKey { get { throw null; } }
        public static double ThickVerticalBorderWidth { get { throw null; } }
        public static System.Windows.ResourceKey ThickVerticalBorderWidthKey { get { throw null; } }
        public static double ThinHorizontalBorderHeight { get { throw null; } }
        public static System.Windows.ResourceKey ThinHorizontalBorderHeightKey { get { throw null; } }
        public static double ThinVerticalBorderWidth { get { throw null; } }
        public static System.Windows.ResourceKey ThinVerticalBorderWidthKey { get { throw null; } }
        public static bool ToolTipAnimation { get { throw null; } }
        public static System.Windows.ResourceKey ToolTipAnimationKey { get { throw null; } }
        public static bool ToolTipFade { get { throw null; } }
        public static System.Windows.ResourceKey ToolTipFadeKey { get { throw null; } }
        public static System.Windows.Controls.Primitives.PopupAnimation ToolTipPopupAnimation { get { throw null; } }
        public static System.Windows.ResourceKey ToolTipPopupAnimationKey { get { throw null; } }
        public static bool UIEffects { get { throw null; } }
        public static System.Windows.ResourceKey UIEffectsKey { get { throw null; } }
        public static string UxThemeColor { get { throw null; } }
        public static string UxThemeName { get { throw null; } }
        public static double VerticalScrollBarButtonHeight { get { throw null; } }
        public static System.Windows.ResourceKey VerticalScrollBarButtonHeightKey { get { throw null; } }
        public static double VerticalScrollBarThumbHeight { get { throw null; } }
        public static System.Windows.ResourceKey VerticalScrollBarThumbHeightKey { get { throw null; } }
        public static double VerticalScrollBarWidth { get { throw null; } }
        public static System.Windows.ResourceKey VerticalScrollBarWidthKey { get { throw null; } }
        public static double VirtualScreenHeight { get { throw null; } }
        public static System.Windows.ResourceKey VirtualScreenHeightKey { get { throw null; } }
        public static double VirtualScreenLeft { get { throw null; } }
        public static System.Windows.ResourceKey VirtualScreenLeftKey { get { throw null; } }
        public static double VirtualScreenTop { get { throw null; } }
        public static System.Windows.ResourceKey VirtualScreenTopKey { get { throw null; } }
        public static double VirtualScreenWidth { get { throw null; } }
        public static System.Windows.ResourceKey VirtualScreenWidthKey { get { throw null; } }
        public static int WheelScrollLines { get { throw null; } }
        public static System.Windows.ResourceKey WheelScrollLinesKey { get { throw null; } }
        public static double WindowCaptionButtonHeight { get { throw null; } }
        public static System.Windows.ResourceKey WindowCaptionButtonHeightKey { get { throw null; } }
        public static double WindowCaptionButtonWidth { get { throw null; } }
        public static System.Windows.ResourceKey WindowCaptionButtonWidthKey { get { throw null; } }
        public static double WindowCaptionHeight { get { throw null; } }
        public static System.Windows.ResourceKey WindowCaptionHeightKey { get { throw null; } }
        public static System.Windows.CornerRadius WindowCornerRadius { get { throw null; } }
        public static System.Windows.Media.Brush WindowGlassBrush { get { throw null; } }
        public static System.Windows.Media.Color WindowGlassColor { get { throw null; } }
        public static System.Windows.Thickness WindowNonClientFrameThickness { get { throw null; } }
        public static System.Windows.Thickness WindowResizeBorderThickness { get { throw null; } }
        public static System.Windows.Rect WorkArea { get { throw null; } }
        public static System.Windows.ResourceKey WorkAreaKey { get { throw null; } }
        public static event System.ComponentModel.PropertyChangedEventHandler StaticPropertyChanged { add { } remove { } }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.TemplateBindingExpressionConverter))]
    public partial class TemplateBindingExpression : System.Windows.Expression
    {
        internal TemplateBindingExpression() { }
        public System.Windows.TemplateBindingExtension TemplateBindingExtension { get { throw null; } }
    }
    public partial class TemplateBindingExpressionConverter : System.ComponentModel.TypeConverter
    {
        public TemplateBindingExpressionConverter() { }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.TemplateBindingExtensionConverter))]
    [System.Windows.Markup.MarkupExtensionReturnTypeAttribute(typeof(object))]
    public partial class TemplateBindingExtension : System.Windows.Markup.MarkupExtension
    {
        public TemplateBindingExtension() { }
        public TemplateBindingExtension(System.Windows.DependencyProperty property) { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Data.IValueConverter Converter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public object ConverterParameter { get { throw null; } set { } }
        [System.Windows.Markup.ConstructorArgumentAttribute("property")]
        public System.Windows.DependencyProperty Property { get { throw null; } set { } }
        public override object ProvideValue(System.IServiceProvider serviceProvider) { throw null; }
    }
    public partial class TemplateBindingExtensionConverter : System.ComponentModel.TypeConverter
    {
        public TemplateBindingExtensionConverter() { }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    [System.Windows.Markup.XamlDeferLoadAttribute(typeof(System.Windows.TemplateContentLoader), typeof(System.Windows.FrameworkElement))]
    public partial class TemplateContent
    {
        internal TemplateContent() { }
    }
    public partial class TemplateContentLoader : System.Xaml.XamlDeferringLoader
    {
        public TemplateContentLoader() { }
        public override object Load(System.Xaml.XamlReader xamlReader, System.IServiceProvider serviceProvider) { throw null; }
        public override System.Xaml.XamlReader Save(object value, System.IServiceProvider serviceProvider) { throw null; }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Markup.TemplateKeyConverter))]
    public abstract partial class TemplateKey : System.Windows.ResourceKey, System.ComponentModel.ISupportInitialize
    {
        protected TemplateKey(System.Windows.TemplateKey.TemplateType templateType) { }
        protected TemplateKey(System.Windows.TemplateKey.TemplateType templateType, object dataType) { }
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public object DataType { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        void System.ComponentModel.ISupportInitialize.BeginInit() { }
        void System.ComponentModel.ISupportInitialize.EndInit() { }
        public override string ToString() { throw null; }
        protected enum TemplateType
        {
            DataTemplate = 0,
            TableTemplate = 1,
        }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true)]
    public sealed partial class TemplatePartAttribute : System.Attribute
    {
        public TemplatePartAttribute() { }
        public string Name { get { throw null; } set { } }
        public System.Type Type { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true)]
    public sealed partial class TemplateVisualStateAttribute : System.Attribute
    {
        public TemplateVisualStateAttribute() { }
        public string GroupName { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
    }
    [System.Windows.Markup.MarkupExtensionReturnTypeAttribute(typeof(System.Uri))]
    public partial class ThemeDictionaryExtension : System.Windows.Markup.MarkupExtension
    {
        public ThemeDictionaryExtension() { }
        public ThemeDictionaryExtension(string assemblyName) { }
        public string AssemblyName { get { throw null; } set { } }
        public override object ProvideValue(System.IServiceProvider serviceProvider) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly)]
    public sealed partial class ThemeInfoAttribute : System.Attribute
    {
        public ThemeInfoAttribute(System.Windows.ResourceDictionaryLocation themeDictionaryLocation, System.Windows.ResourceDictionaryLocation genericDictionaryLocation) { }
        public System.Windows.ResourceDictionaryLocation GenericDictionaryLocation { get { throw null; } }
        public System.Windows.ResourceDictionaryLocation ThemeDictionaryLocation { get { throw null; } }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.ThicknessConverter))]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public partial struct Thickness : System.IEquatable<System.Windows.Thickness>
    {
        public Thickness(double uniformLength) { throw null; }
        public Thickness(double left, double top, double right, double bottom) { throw null; }
        public double Bottom { get { throw null; } set { } }
        public double Left { get { throw null; } set { } }
        public double Right { get { throw null; } set { } }
        public double Top { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Windows.Thickness thickness) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Thickness t1, System.Windows.Thickness t2) { throw null; }
        public static bool operator !=(System.Windows.Thickness t1, System.Windows.Thickness t2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class ThicknessConverter : System.ComponentModel.TypeConverter
    {
        public ThicknessConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object value, System.Type destinationType) { throw null; }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Setters")]
    [System.Windows.Markup.XamlSetTypeConverterAttribute("ReceiveTypeConverter")]
    public partial class Trigger : System.Windows.TriggerBase, System.ComponentModel.ISupportInitialize, System.Windows.Markup.IAddChild
    {
        public Trigger() { }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Modifiability=System.Windows.Modifiability.Unmodifiable, Readability=System.Windows.Readability.Unreadable)]
        [System.Windows.Markup.AmbientAttribute]
        public System.Windows.DependencyProperty Property { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.SetterBaseCollection Setters { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Windows.Markup.AmbientAttribute]
        public string SourceName { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Markup.SetterTriggerConditionValueConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        [System.Windows.Markup.DependsOnAttribute("Property")]
        [System.Windows.Markup.DependsOnAttribute("SourceName")]
        public object Value { get { throw null; } set { } }
        public static void ReceiveTypeConverter(object targetObject, System.Windows.Markup.XamlSetTypeConverterEventArgs eventArgs) { }
        void System.ComponentModel.ISupportInitialize.BeginInit() { }
        void System.ComponentModel.ISupportInitialize.EndInit() { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public abstract partial class TriggerAction : System.Windows.DependencyObject
    {
        internal TriggerAction() { }
    }
    public sealed partial class TriggerActionCollection : System.Collections.Generic.ICollection<System.Windows.TriggerAction>, System.Collections.Generic.IEnumerable<System.Windows.TriggerAction>, System.Collections.Generic.IList<System.Windows.TriggerAction>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public TriggerActionCollection() { }
        public TriggerActionCollection(int initialSize) { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public System.Windows.TriggerAction this[int index] { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(System.Windows.TriggerAction value) { }
        public void Clear() { }
        public bool Contains(System.Windows.TriggerAction value) { throw null; }
        public void CopyTo(System.Windows.TriggerAction[] array, int index) { }
        [System.CLSCompliantAttribute(false)]
        public System.Collections.Generic.IEnumerator<System.Windows.TriggerAction> GetEnumerator() { throw null; }
        public int IndexOf(System.Windows.TriggerAction value) { throw null; }
        public void Insert(int index, System.Windows.TriggerAction value) { }
        public bool Remove(System.Windows.TriggerAction value) { throw null; }
        public void RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public abstract partial class TriggerBase : System.Windows.DependencyObject
    {
        internal TriggerBase() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.TriggerActionCollection EnterActions { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.TriggerActionCollection ExitActions { get { throw null; } }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public sealed partial class TriggerCollection : System.Collections.ObjectModel.Collection<System.Windows.TriggerBase>
    {
        internal TriggerCollection() { }
        public bool IsSealed { get { throw null; } }
        protected override void ClearItems() { }
        protected override void InsertItem(int index, System.Windows.TriggerBase item) { }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, System.Windows.TriggerBase item) { }
    }
    public partial struct ValueSource
    {
        public System.Windows.BaseValueSource BaseValueSource { get { throw null; } }
        public bool IsAnimated { get { throw null; } }
        public bool IsCoerced { get { throw null; } }
        public bool IsCurrent { get { throw null; } }
        public bool IsExpression { get { throw null; } }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.ValueSource vs1, System.Windows.ValueSource vs2) { throw null; }
        public static bool operator !=(System.Windows.ValueSource vs1, System.Windows.ValueSource vs2) { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public enum VerticalAlignment
    {
        Top = 0,
        Center = 1,
        Bottom = 2,
        Stretch = 3,
    }
    [System.Windows.Markup.ContentPropertyAttribute("Storyboard")]
    [System.Windows.Markup.RuntimeNamePropertyAttribute("Name")]
    public partial class VisualState : System.Windows.DependencyObject
    {
        public VisualState() { }
        public string Name { get { throw null; } set { } }
        public System.Windows.Media.Animation.Storyboard Storyboard { get { throw null; } set { } }
    }
    public sealed partial class VisualStateChangedEventArgs : System.EventArgs
    {
        internal VisualStateChangedEventArgs() { }
        public System.Windows.FrameworkElement Control { get { throw null; } }
        public System.Windows.VisualState NewState { get { throw null; } }
        public System.Windows.VisualState OldState { get { throw null; } }
        public System.Windows.FrameworkElement StateGroupsRoot { get { throw null; } }
    }
    [System.Windows.Markup.ContentPropertyAttribute("States")]
    [System.Windows.Markup.RuntimeNamePropertyAttribute("Name")]
    public partial class VisualStateGroup : System.Windows.DependencyObject
    {
        public VisualStateGroup() { }
        public System.Windows.VisualState CurrentState { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public System.Collections.IList States { get { throw null; } }
        public System.Collections.IList Transitions { get { throw null; } }
        public event System.EventHandler<System.Windows.VisualStateChangedEventArgs> CurrentStateChanged { add { } remove { } }
        public event System.EventHandler<System.Windows.VisualStateChangedEventArgs> CurrentStateChanging { add { } remove { } }
    }
    public partial class VisualStateManager : System.Windows.DependencyObject
    {
        public static readonly System.Windows.DependencyProperty CustomVisualStateManagerProperty;
        public static readonly System.Windows.DependencyProperty VisualStateGroupsProperty;
        public VisualStateManager() { }
        public static System.Windows.VisualStateManager GetCustomVisualStateManager(System.Windows.FrameworkElement obj) { throw null; }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public static System.Collections.IList GetVisualStateGroups(System.Windows.FrameworkElement obj) { throw null; }
        public static bool GoToElementState(System.Windows.FrameworkElement stateGroupsRoot, string stateName, bool useTransitions) { throw null; }
        public static bool GoToState(System.Windows.FrameworkElement control, string stateName, bool useTransitions) { throw null; }
        protected virtual bool GoToStateCore(System.Windows.FrameworkElement control, System.Windows.FrameworkElement stateGroupsRoot, string stateName, System.Windows.VisualStateGroup group, System.Windows.VisualState state, bool useTransitions) { throw null; }
        protected void RaiseCurrentStateChanged(System.Windows.VisualStateGroup stateGroup, System.Windows.VisualState oldState, System.Windows.VisualState newState, System.Windows.FrameworkElement control, System.Windows.FrameworkElement stateGroupsRoot) { }
        protected void RaiseCurrentStateChanging(System.Windows.VisualStateGroup stateGroup, System.Windows.VisualState oldState, System.Windows.VisualState newState, System.Windows.FrameworkElement control, System.Windows.FrameworkElement stateGroupsRoot) { }
        public static void SetCustomVisualStateManager(System.Windows.FrameworkElement obj, System.Windows.VisualStateManager value) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Storyboard")]
    public partial class VisualTransition : System.Windows.DependencyObject
    {
        public VisualTransition() { }
        public string From { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.DurationConverter))]
        public System.Windows.Duration GeneratedDuration { get { throw null; } set { } }
        public System.Windows.Media.Animation.IEasingFunction GeneratedEasingFunction { get { throw null; } set { } }
        public System.Windows.Media.Animation.Storyboard Storyboard { get { throw null; } set { } }
        public string To { get { throw null; } set { } }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore)]
    public partial class Window : System.Windows.Controls.ContentControl
    {
        public static readonly System.Windows.DependencyProperty AllowsTransparencyProperty;
        public static readonly System.Windows.RoutedEvent DpiChangedEvent;
        public static readonly System.Windows.DependencyProperty IconProperty;
        public static readonly System.Windows.DependencyProperty IsActiveProperty;
        public static readonly System.Windows.DependencyProperty LeftProperty;
        public static readonly System.Windows.DependencyProperty ResizeModeProperty;
        public static readonly System.Windows.DependencyProperty ShowActivatedProperty;
        public static readonly System.Windows.DependencyProperty ShowInTaskbarProperty;
        public static readonly System.Windows.DependencyProperty SizeToContentProperty;
        public static readonly System.Windows.DependencyProperty TaskbarItemInfoProperty;
        public static readonly System.Windows.DependencyProperty TitleProperty;
        public static readonly System.Windows.DependencyProperty TopmostProperty;
        public static readonly System.Windows.DependencyProperty TopProperty;
        public static readonly System.Windows.DependencyProperty WindowStateProperty;
        public static readonly System.Windows.DependencyProperty WindowStyleProperty;
        public Window() { }
        public bool AllowsTransparency { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.DialogResultConverter))]
        public bool? DialogResult { get { throw null; } set { } }
        public System.Windows.Media.ImageSource Icon { get { throw null; } set { } }
        public bool IsActive { get { throw null; } }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        public double Left { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public System.Windows.WindowCollection OwnedWindows { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Window Owner { get { throw null; } set { } }
        public System.Windows.ResizeMode ResizeMode { get { throw null; } set { } }
        public System.Windows.Rect RestoreBounds { get { throw null; } }
        public bool ShowActivated { get { throw null; } set { } }
        public bool ShowInTaskbar { get { throw null; } set { } }
        public System.Windows.SizeToContent SizeToContent { get { throw null; } set { } }
        public System.Windows.Shell.TaskbarItemInfo TaskbarItemInfo { get { throw null; } set { } }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Title)]
        public string Title { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        public double Top { get { throw null; } set { } }
        public bool Topmost { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Windows.WindowStartupLocation.Manual)]
        public System.Windows.WindowStartupLocation WindowStartupLocation { get { throw null; } set { } }
        public System.Windows.WindowState WindowState { get { throw null; } set { } }
        public System.Windows.WindowStyle WindowStyle { get { throw null; } set { } }
        public event System.EventHandler Activated { add { } remove { } }
        public event System.EventHandler Closed { add { } remove { } }
        public event System.ComponentModel.CancelEventHandler Closing { add { } remove { } }
        public event System.EventHandler ContentRendered { add { } remove { } }
        public event System.EventHandler Deactivated { add { } remove { } }
        public event System.Windows.DpiChangedEventHandler DpiChanged { add { } remove { } }
        public event System.EventHandler LocationChanged { add { } remove { } }
        public event System.EventHandler SourceInitialized { add { } remove { } }
        public event System.EventHandler StateChanged { add { } remove { } }
        public bool Activate() { throw null; }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeBounds) { throw null; }
        public void Close() { }
        public void DragMove() { }
        public static System.Windows.Window GetWindow(System.Windows.DependencyObject dependencyObject) { throw null; }
        public void Hide() { }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        protected virtual void OnActivated(System.EventArgs e) { }
        protected virtual void OnClosed(System.EventArgs e) { }
        protected virtual void OnClosing(System.ComponentModel.CancelEventArgs e) { }
        protected override void OnContentChanged(object oldContent, object newContent) { }
        protected virtual void OnContentRendered(System.EventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDeactivated(System.EventArgs e) { }
        protected override void OnDpiChanged(System.Windows.DpiScale oldDpi, System.Windows.DpiScale newDpi) { }
        protected virtual void OnLocationChanged(System.EventArgs e) { }
        protected override void OnManipulationBoundaryFeedback(System.Windows.Input.ManipulationBoundaryFeedbackEventArgs e) { }
        protected virtual void OnSourceInitialized(System.EventArgs e) { }
        protected virtual void OnStateChanged(System.EventArgs e) { }
        protected internal override void OnVisualChildrenChanged(System.Windows.DependencyObject visualAdded, System.Windows.DependencyObject visualRemoved) { }
        protected internal sealed override void OnVisualParentChanged(System.Windows.DependencyObject oldParent) { }
        public void Show() { }
        public bool? ShowDialog() { throw null; }
    }
    public sealed partial class WindowCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public WindowCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Windows.Window this[int index] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public void CopyTo(System.Windows.Window[] array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public enum WindowStartupLocation
    {
        Manual = 0,
        CenterScreen = 1,
        CenterOwner = 2,
    }
    public enum WindowState
    {
        Normal = 0,
        Minimized = 1,
        Maximized = 2,
    }
    public enum WindowStyle
    {
        None = 0,
        SingleBorderWindow = 1,
        ThreeDBorderWindow = 2,
        ToolWindow = 3,
    }
    public enum WrapDirection
    {
        None = 0,
        Left = 1,
        Right = 2,
        Both = 3,
    }
}
namespace System.Windows.Annotations
{
    public sealed partial class Annotation : System.Xml.Serialization.IXmlSerializable
    {
        public Annotation() { }
        public Annotation(System.Xml.XmlQualifiedName annotationType) { }
        public Annotation(System.Xml.XmlQualifiedName annotationType, System.Guid id, System.DateTime creationTime, System.DateTime lastModificationTime) { }
        public System.Collections.ObjectModel.Collection<System.Windows.Annotations.AnnotationResource> Anchors { get { throw null; } }
        public System.Xml.XmlQualifiedName AnnotationType { get { throw null; } }
        public System.Collections.ObjectModel.Collection<string> Authors { get { throw null; } }
        public System.Collections.ObjectModel.Collection<System.Windows.Annotations.AnnotationResource> Cargos { get { throw null; } }
        public System.DateTime CreationTime { get { throw null; } }
        public System.Guid Id { get { throw null; } }
        public System.DateTime LastModificationTime { get { throw null; } }
        public event System.Windows.Annotations.AnnotationResourceChangedEventHandler AnchorChanged { add { } remove { } }
        public event System.Windows.Annotations.AnnotationAuthorChangedEventHandler AuthorChanged { add { } remove { } }
        public event System.Windows.Annotations.AnnotationResourceChangedEventHandler CargoChanged { add { } remove { } }
        public System.Xml.Schema.XmlSchema GetSchema() { throw null; }
        public void ReadXml(System.Xml.XmlReader reader) { }
        public void WriteXml(System.Xml.XmlWriter writer) { }
    }
    public enum AnnotationAction
    {
        Added = 0,
        Removed = 1,
        Modified = 2,
    }
    public sealed partial class AnnotationAuthorChangedEventArgs : System.EventArgs
    {
        public AnnotationAuthorChangedEventArgs(System.Windows.Annotations.Annotation annotation, System.Windows.Annotations.AnnotationAction action, object author) { }
        public System.Windows.Annotations.AnnotationAction Action { get { throw null; } }
        public System.Windows.Annotations.Annotation Annotation { get { throw null; } }
        public object Author { get { throw null; } }
    }
    public delegate void AnnotationAuthorChangedEventHandler(object sender, System.Windows.Annotations.AnnotationAuthorChangedEventArgs e);
    public sealed partial class AnnotationDocumentPaginator : System.Windows.Documents.DocumentPaginator
    {
        public AnnotationDocumentPaginator(System.Windows.Documents.DocumentPaginator originalPaginator, System.IO.Stream annotationStore) { }
        public AnnotationDocumentPaginator(System.Windows.Documents.DocumentPaginator originalPaginator, System.IO.Stream annotationStore, System.Windows.FlowDirection flowDirection) { }
        public AnnotationDocumentPaginator(System.Windows.Documents.DocumentPaginator originalPaginator, System.Windows.Annotations.Storage.AnnotationStore annotationStore) { }
        public AnnotationDocumentPaginator(System.Windows.Documents.DocumentPaginator originalPaginator, System.Windows.Annotations.Storage.AnnotationStore annotationStore, System.Windows.FlowDirection flowDirection) { }
        public override bool IsPageCountValid { get { throw null; } }
        public override int PageCount { get { throw null; } }
        public override System.Windows.Size PageSize { get { throw null; } set { } }
        public override System.Windows.Documents.IDocumentPaginatorSource Source { get { throw null; } }
        public override void CancelAsync(object userState) { }
        public override void ComputePageCount() { }
        public override void ComputePageCountAsync(object userState) { }
        public override System.Windows.Documents.DocumentPage GetPage(int pageNumber) { throw null; }
        public override void GetPageAsync(int pageNumber, object userState) { }
    }
    public static partial class AnnotationHelper
    {
        public static void ClearHighlightsForSelection(System.Windows.Annotations.AnnotationService service) { }
        public static System.Windows.Annotations.Annotation CreateHighlightForSelection(System.Windows.Annotations.AnnotationService service, string author, System.Windows.Media.Brush highlightBrush) { throw null; }
        public static System.Windows.Annotations.Annotation CreateInkStickyNoteForSelection(System.Windows.Annotations.AnnotationService service, string author) { throw null; }
        public static System.Windows.Annotations.Annotation CreateTextStickyNoteForSelection(System.Windows.Annotations.AnnotationService service, string author) { throw null; }
        public static void DeleteInkStickyNotesForSelection(System.Windows.Annotations.AnnotationService service) { }
        public static void DeleteTextStickyNotesForSelection(System.Windows.Annotations.AnnotationService service) { }
        public static System.Windows.Annotations.IAnchorInfo GetAnchorInfo(System.Windows.Annotations.AnnotationService service, System.Windows.Annotations.Annotation annotation) { throw null; }
    }
    public sealed partial class AnnotationResource : System.ComponentModel.INotifyPropertyChanged, System.Xml.Serialization.IXmlSerializable
    {
        public AnnotationResource() { }
        public AnnotationResource(System.Guid id) { }
        public AnnotationResource(string name) { }
        public System.Collections.ObjectModel.Collection<System.Windows.Annotations.ContentLocatorBase> ContentLocators { get { throw null; } }
        public System.Collections.ObjectModel.Collection<System.Xml.XmlElement> Contents { get { throw null; } }
        public System.Guid Id { get { throw null; } }
        public string Name { get { throw null; } set { } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        public System.Xml.Schema.XmlSchema GetSchema() { throw null; }
        public void ReadXml(System.Xml.XmlReader reader) { }
        public void WriteXml(System.Xml.XmlWriter writer) { }
    }
    public sealed partial class AnnotationResourceChangedEventArgs : System.EventArgs
    {
        public AnnotationResourceChangedEventArgs(System.Windows.Annotations.Annotation annotation, System.Windows.Annotations.AnnotationAction action, System.Windows.Annotations.AnnotationResource resource) { }
        public System.Windows.Annotations.AnnotationAction Action { get { throw null; } }
        public System.Windows.Annotations.Annotation Annotation { get { throw null; } }
        public System.Windows.Annotations.AnnotationResource Resource { get { throw null; } }
    }
    public delegate void AnnotationResourceChangedEventHandler(object sender, System.Windows.Annotations.AnnotationResourceChangedEventArgs e);
    public sealed partial class AnnotationService : System.Windows.Threading.DispatcherObject
    {
        public static readonly System.Windows.Input.RoutedUICommand ClearHighlightsCommand;
        public static readonly System.Windows.Input.RoutedUICommand CreateHighlightCommand;
        public static readonly System.Windows.Input.RoutedUICommand CreateInkStickyNoteCommand;
        public static readonly System.Windows.Input.RoutedUICommand CreateTextStickyNoteCommand;
        public static readonly System.Windows.Input.RoutedUICommand DeleteAnnotationsCommand;
        public static readonly System.Windows.Input.RoutedUICommand DeleteStickyNotesCommand;
        public AnnotationService(System.Windows.Controls.FlowDocumentReader viewer) { }
        public AnnotationService(System.Windows.Controls.FlowDocumentScrollViewer viewer) { }
        public AnnotationService(System.Windows.Controls.Primitives.DocumentViewerBase viewer) { }
        public bool IsEnabled { get { throw null; } }
        public System.Windows.Annotations.Storage.AnnotationStore Store { get { throw null; } }
        public void Disable() { }
        public void Enable(System.Windows.Annotations.Storage.AnnotationStore annotationStore) { }
        public static System.Windows.Annotations.AnnotationService GetService(System.Windows.Controls.FlowDocumentReader reader) { throw null; }
        public static System.Windows.Annotations.AnnotationService GetService(System.Windows.Controls.FlowDocumentScrollViewer viewer) { throw null; }
        public static System.Windows.Annotations.AnnotationService GetService(System.Windows.Controls.Primitives.DocumentViewerBase viewer) { throw null; }
    }
    public sealed partial class ContentLocator : System.Windows.Annotations.ContentLocatorBase, System.Xml.Serialization.IXmlSerializable
    {
        public ContentLocator() { }
        public System.Collections.ObjectModel.Collection<System.Windows.Annotations.ContentLocatorPart> Parts { get { throw null; } }
        public override object Clone() { throw null; }
        public System.Xml.Schema.XmlSchema GetSchema() { throw null; }
        public void ReadXml(System.Xml.XmlReader reader) { }
        public bool StartsWith(System.Windows.Annotations.ContentLocator locator) { throw null; }
        public void WriteXml(System.Xml.XmlWriter writer) { }
    }
    public abstract partial class ContentLocatorBase : System.ComponentModel.INotifyPropertyChanged
    {
        internal ContentLocatorBase() { }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        public abstract object Clone();
    }
    public sealed partial class ContentLocatorGroup : System.Windows.Annotations.ContentLocatorBase, System.Xml.Serialization.IXmlSerializable
    {
        public ContentLocatorGroup() { }
        public System.Collections.ObjectModel.Collection<System.Windows.Annotations.ContentLocator> Locators { get { throw null; } }
        public override object Clone() { throw null; }
        public System.Xml.Schema.XmlSchema GetSchema() { throw null; }
        public void ReadXml(System.Xml.XmlReader reader) { }
        public void WriteXml(System.Xml.XmlWriter writer) { }
    }
    public sealed partial class ContentLocatorPart : System.ComponentModel.INotifyPropertyChanged
    {
        public ContentLocatorPart(System.Xml.XmlQualifiedName partType) { }
        public System.Collections.Generic.IDictionary<string, string> NameValuePairs { get { throw null; } }
        public System.Xml.XmlQualifiedName PartType { get { throw null; } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        public object Clone() { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public partial interface IAnchorInfo
    {
        System.Windows.Annotations.AnnotationResource Anchor { get; }
        System.Windows.Annotations.Annotation Annotation { get; }
        object ResolvedAnchor { get; }
    }
    public sealed partial class TextAnchor
    {
        internal TextAnchor() { }
        public System.Windows.Documents.ContentPosition BoundingEnd { get { throw null; } }
        public System.Windows.Documents.ContentPosition BoundingStart { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
}
namespace System.Windows.Annotations.Storage
{
    public abstract partial class AnnotationStore : System.IDisposable
    {
        protected AnnotationStore() { }
        public abstract bool AutoFlush { get; set; }
        protected bool IsDisposed { get { throw null; } }
        protected object SyncRoot { get { throw null; } }
        public event System.Windows.Annotations.AnnotationResourceChangedEventHandler AnchorChanged { add { } remove { } }
        public event System.Windows.Annotations.AnnotationAuthorChangedEventHandler AuthorChanged { add { } remove { } }
        public event System.Windows.Annotations.AnnotationResourceChangedEventHandler CargoChanged { add { } remove { } }
        public event System.Windows.Annotations.Storage.StoreContentChangedEventHandler StoreContentChanged { add { } remove { } }
        public abstract void AddAnnotation(System.Windows.Annotations.Annotation newAnnotation);
        public abstract System.Windows.Annotations.Annotation DeleteAnnotation(System.Guid annotationId);
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~AnnotationStore() { }
        public abstract void Flush();
        public abstract System.Windows.Annotations.Annotation GetAnnotation(System.Guid annotationId);
        public abstract System.Collections.Generic.IList<System.Windows.Annotations.Annotation> GetAnnotations();
        public abstract System.Collections.Generic.IList<System.Windows.Annotations.Annotation> GetAnnotations(System.Windows.Annotations.ContentLocator anchorLocator);
        protected virtual void OnAnchorChanged(System.Windows.Annotations.AnnotationResourceChangedEventArgs args) { }
        protected virtual void OnAuthorChanged(System.Windows.Annotations.AnnotationAuthorChangedEventArgs args) { }
        protected virtual void OnCargoChanged(System.Windows.Annotations.AnnotationResourceChangedEventArgs args) { }
        protected virtual void OnStoreContentChanged(System.Windows.Annotations.Storage.StoreContentChangedEventArgs e) { }
    }
    public enum StoreContentAction
    {
        Added = 0,
        Deleted = 1,
    }
    public partial class StoreContentChangedEventArgs : System.EventArgs
    {
        public StoreContentChangedEventArgs(System.Windows.Annotations.Storage.StoreContentAction action, System.Windows.Annotations.Annotation annotation) { }
        public System.Windows.Annotations.Storage.StoreContentAction Action { get { throw null; } }
        public System.Windows.Annotations.Annotation Annotation { get { throw null; } }
    }
    public delegate void StoreContentChangedEventHandler(object sender, System.Windows.Annotations.Storage.StoreContentChangedEventArgs e);
    public sealed partial class XmlStreamStore : System.Windows.Annotations.Storage.AnnotationStore
    {
        public XmlStreamStore(System.IO.Stream stream) { }
        public XmlStreamStore(System.IO.Stream stream, System.Collections.Generic.IDictionary<System.Uri, System.Collections.Generic.IList<System.Uri>> knownNamespaces) { }
        public override bool AutoFlush { get { throw null; } set { } }
        public System.Collections.Generic.IList<System.Uri> IgnoredNamespaces { get { throw null; } }
        public static System.Collections.Generic.IList<System.Uri> WellKnownNamespaces { get { throw null; } }
        public override void AddAnnotation(System.Windows.Annotations.Annotation newAnnotation) { }
        public override System.Windows.Annotations.Annotation DeleteAnnotation(System.Guid annotationId) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override System.Windows.Annotations.Annotation GetAnnotation(System.Guid annotationId) { throw null; }
        public override System.Collections.Generic.IList<System.Windows.Annotations.Annotation> GetAnnotations() { throw null; }
        public override System.Collections.Generic.IList<System.Windows.Annotations.Annotation> GetAnnotations(System.Windows.Annotations.ContentLocator anchorLocator) { throw null; }
        public static System.Collections.Generic.IList<System.Uri> GetWellKnownCompatibleNamespaces(System.Uri name) { throw null; }
        protected override void OnStoreContentChanged(System.Windows.Annotations.Storage.StoreContentChangedEventArgs e) { }
    }
}
namespace System.Windows.Automation.Peers
{
    public partial class ButtonAutomationPeer : System.Windows.Automation.Peers.ButtonBaseAutomationPeer, System.Windows.Automation.Provider.IInvokeProvider
    {
        public ButtonAutomationPeer(System.Windows.Controls.Button owner) : base (default(System.Windows.Controls.Primitives.ButtonBase)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.IInvokeProvider.Invoke() { }
    }
    public abstract partial class ButtonBaseAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        protected ButtonBaseAutomationPeer(System.Windows.Controls.Primitives.ButtonBase owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override string GetAcceleratorKeyCore() { throw null; }
        protected override string GetAutomationIdCore() { throw null; }
        protected override string GetNameCore() { throw null; }
    }
    public sealed partial class CalendarAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer, System.Windows.Automation.Provider.IGridProvider, System.Windows.Automation.Provider.IItemContainerProvider, System.Windows.Automation.Provider.IMultipleViewProvider, System.Windows.Automation.Provider.ISelectionProvider, System.Windows.Automation.Provider.ITableProvider
    {
        public CalendarAutomationPeer(System.Windows.Controls.Calendar owner) : base (default(System.Windows.FrameworkElement)) { }
        int System.Windows.Automation.Provider.IGridProvider.ColumnCount { get { throw null; } }
        int System.Windows.Automation.Provider.IGridProvider.RowCount { get { throw null; } }
        int System.Windows.Automation.Provider.IMultipleViewProvider.CurrentView { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionProvider.CanSelectMultiple { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionProvider.IsSelectionRequired { get { throw null; } }
        System.Windows.Automation.RowOrColumnMajor System.Windows.Automation.Provider.ITableProvider.RowOrColumnMajor { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override void SetFocusCore() { }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IGridProvider.GetItem(int row, int column) { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IItemContainerProvider.FindItemByProperty(System.Windows.Automation.Provider.IRawElementProviderSimple startAfterProvider, int propertyId, object value) { throw null; }
        int[] System.Windows.Automation.Provider.IMultipleViewProvider.GetSupportedViews() { throw null; }
        string System.Windows.Automation.Provider.IMultipleViewProvider.GetViewName(int viewId) { throw null; }
        void System.Windows.Automation.Provider.IMultipleViewProvider.SetCurrentView(int viewId) { }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ISelectionProvider.GetSelection() { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableProvider.GetColumnHeaders() { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableProvider.GetRowHeaders() { throw null; }
    }
    public sealed partial class CalendarButtonAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public CalendarButtonAutomationPeer(System.Windows.Controls.Button owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override string GetLocalizedControlTypeCore() { throw null; }
    }
    public partial class CheckBoxAutomationPeer : System.Windows.Automation.Peers.ToggleButtonAutomationPeer
    {
        public CheckBoxAutomationPeer(System.Windows.Controls.CheckBox owner) : base (default(System.Windows.Controls.Primitives.ToggleButton)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class ComboBoxAutomationPeer : System.Windows.Automation.Peers.SelectorAutomationPeer, System.Windows.Automation.Provider.IExpandCollapseProvider, System.Windows.Automation.Provider.IValueProvider
    {
        public ComboBoxAutomationPeer(System.Windows.Controls.ComboBox owner) : base (default(System.Windows.Controls.Primitives.Selector)) { }
        System.Windows.Automation.ExpandCollapseState System.Windows.Automation.Provider.IExpandCollapseProvider.ExpandCollapseState { get { throw null; } }
        bool System.Windows.Automation.Provider.IValueProvider.IsReadOnly { get { throw null; } }
        string System.Windows.Automation.Provider.IValueProvider.Value { get { throw null; } }
        protected override System.Windows.Automation.Peers.ItemAutomationPeer CreateItemAutomationPeer(object item) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface pattern) { throw null; }
        protected override void SetFocusCore() { }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Collapse() { }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Expand() { }
        void System.Windows.Automation.Provider.IValueProvider.SetValue(string val) { }
    }
    public abstract partial class ContentTextAutomationPeer : System.Windows.Automation.Peers.FrameworkContentElementAutomationPeer
    {
        protected ContentTextAutomationPeer(System.Windows.FrameworkContentElement owner) : base (default(System.Windows.FrameworkContentElement)) { }
    }
    public partial class ContextMenuAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public ContextMenuAutomationPeer(System.Windows.Controls.ContextMenu owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
    }
    public sealed partial class DataGridAutomationPeer : System.Windows.Automation.Peers.ItemsControlAutomationPeer, System.Windows.Automation.Provider.IGridProvider, System.Windows.Automation.Provider.ISelectionProvider, System.Windows.Automation.Provider.ITableProvider
    {
        public DataGridAutomationPeer(System.Windows.Controls.DataGrid owner) : base (default(System.Windows.Controls.ItemsControl)) { }
        int System.Windows.Automation.Provider.IGridProvider.ColumnCount { get { throw null; } }
        int System.Windows.Automation.Provider.IGridProvider.RowCount { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionProvider.CanSelectMultiple { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionProvider.IsSelectionRequired { get { throw null; } }
        System.Windows.Automation.RowOrColumnMajor System.Windows.Automation.Provider.ITableProvider.RowOrColumnMajor { get { throw null; } }
        protected override System.Windows.Automation.Peers.ItemAutomationPeer CreateItemAutomationPeer(object item) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IGridProvider.GetItem(int row, int column) { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ISelectionProvider.GetSelection() { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableProvider.GetColumnHeaders() { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableProvider.GetRowHeaders() { throw null; }
    }
    public sealed partial class DataGridCellAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public DataGridCellAutomationPeer(System.Windows.Controls.DataGridCell owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public sealed partial class DataGridCellItemAutomationPeer : System.Windows.Automation.Peers.AutomationPeer, System.Windows.Automation.Provider.IGridItemProvider, System.Windows.Automation.Provider.IInvokeProvider, System.Windows.Automation.Provider.IScrollItemProvider, System.Windows.Automation.Provider.ISelectionItemProvider, System.Windows.Automation.Provider.ITableItemProvider, System.Windows.Automation.Provider.IValueProvider, System.Windows.Automation.Provider.IVirtualizedItemProvider
    {
        public DataGridCellItemAutomationPeer(object item, System.Windows.Controls.DataGridColumn dataGridColumn) { }
        int System.Windows.Automation.Provider.IGridItemProvider.Column { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.ColumnSpan { get { throw null; } }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IGridItemProvider.ContainingGrid { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.Row { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.RowSpan { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionItemProvider.IsSelected { get { throw null; } }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.ISelectionItemProvider.SelectionContainer { get { throw null; } }
        bool System.Windows.Automation.Provider.IValueProvider.IsReadOnly { get { throw null; } }
        string System.Windows.Automation.Provider.IValueProvider.Value { get { throw null; } }
        protected override string GetAcceleratorKeyCore() { throw null; }
        protected override string GetAccessKeyCore() { throw null; }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetAutomationIdCore() { throw null; }
        protected override System.Windows.Rect GetBoundingRectangleCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override System.Windows.Point GetClickablePointCore() { throw null; }
        protected override string GetHelpTextCore() { throw null; }
        protected override string GetItemStatusCore() { throw null; }
        protected override string GetItemTypeCore() { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer GetLabeledByCore() { throw null; }
        protected override System.Windows.Automation.AutomationLiveSetting GetLiveSettingCore() { throw null; }
        protected override string GetLocalizedControlTypeCore() { throw null; }
        protected override string GetNameCore() { throw null; }
        protected override System.Windows.Automation.Peers.AutomationOrientation GetOrientationCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override int GetPositionInSetCore() { throw null; }
        protected override int GetSizeOfSetCore() { throw null; }
        protected override bool HasKeyboardFocusCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
        protected override bool IsControlElementCore() { throw null; }
        protected override bool IsEnabledCore() { throw null; }
        protected override bool IsKeyboardFocusableCore() { throw null; }
        protected override bool IsOffscreenCore() { throw null; }
        protected override bool IsPasswordCore() { throw null; }
        protected override bool IsRequiredForFormCore() { throw null; }
        protected override void SetFocusCore() { }
        void System.Windows.Automation.Provider.IInvokeProvider.Invoke() { }
        void System.Windows.Automation.Provider.IScrollItemProvider.ScrollIntoView() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.AddToSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.RemoveFromSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.Select() { }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableItemProvider.GetColumnHeaderItems() { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableItemProvider.GetRowHeaderItems() { throw null; }
        void System.Windows.Automation.Provider.IValueProvider.SetValue(string value) { }
        void System.Windows.Automation.Provider.IVirtualizedItemProvider.Realize() { }
    }
    public sealed partial class DataGridColumnHeaderAutomationPeer : System.Windows.Automation.Peers.ButtonBaseAutomationPeer
    {
        public DataGridColumnHeaderAutomationPeer(System.Windows.Controls.Primitives.DataGridColumnHeader owner) : base (default(System.Windows.Controls.Primitives.ButtonBase)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
    }
    public partial class DataGridColumnHeaderItemAutomationPeer : System.Windows.Automation.Peers.ItemAutomationPeer, System.Windows.Automation.Provider.IInvokeProvider, System.Windows.Automation.Provider.IScrollItemProvider, System.Windows.Automation.Provider.ITransformProvider, System.Windows.Automation.Provider.IVirtualizedItemProvider
    {
        public DataGridColumnHeaderItemAutomationPeer(object item, System.Windows.Controls.DataGridColumn column, System.Windows.Automation.Peers.DataGridColumnHeadersPresenterAutomationPeer peer) : base (default(object), default(System.Windows.Automation.Peers.ItemsControlAutomationPeer)) { }
        bool System.Windows.Automation.Provider.ITransformProvider.CanMove { get { throw null; } }
        bool System.Windows.Automation.Provider.ITransformProvider.CanResize { get { throw null; } }
        bool System.Windows.Automation.Provider.ITransformProvider.CanRotate { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override bool IsContentElementCore() { throw null; }
        void System.Windows.Automation.Provider.IInvokeProvider.Invoke() { }
        void System.Windows.Automation.Provider.IScrollItemProvider.ScrollIntoView() { }
        void System.Windows.Automation.Provider.ITransformProvider.Move(double x, double y) { }
        void System.Windows.Automation.Provider.ITransformProvider.Resize(double width, double height) { }
        void System.Windows.Automation.Provider.ITransformProvider.Rotate(double degrees) { }
        void System.Windows.Automation.Provider.IVirtualizedItemProvider.Realize() { }
    }
    public sealed partial class DataGridColumnHeadersPresenterAutomationPeer : System.Windows.Automation.Peers.ItemsControlAutomationPeer, System.Windows.Automation.Provider.IItemContainerProvider
    {
        public DataGridColumnHeadersPresenterAutomationPeer(System.Windows.Controls.Primitives.DataGridColumnHeadersPresenter owner) : base (default(System.Windows.Controls.ItemsControl)) { }
        protected override System.Windows.Automation.Peers.ItemAutomationPeer CreateItemAutomationPeer(object column) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IItemContainerProvider.FindItemByProperty(System.Windows.Automation.Provider.IRawElementProviderSimple startAfter, int propertyId, object value) { throw null; }
    }
    public sealed partial class DataGridDetailsPresenterAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public DataGridDetailsPresenterAutomationPeer(System.Windows.Controls.Primitives.DataGridDetailsPresenter owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override string GetClassNameCore() { throw null; }
    }
    public sealed partial class DataGridItemAutomationPeer : System.Windows.Automation.Peers.ItemAutomationPeer, System.Windows.Automation.Provider.IInvokeProvider, System.Windows.Automation.Provider.IItemContainerProvider, System.Windows.Automation.Provider.IScrollItemProvider, System.Windows.Automation.Provider.ISelectionItemProvider, System.Windows.Automation.Provider.ISelectionProvider
    {
        public DataGridItemAutomationPeer(object item, System.Windows.Automation.Peers.DataGridAutomationPeer dataGridPeer) : base (default(object), default(System.Windows.Automation.Peers.ItemsControlAutomationPeer)) { }
        bool System.Windows.Automation.Provider.ISelectionItemProvider.IsSelected { get { throw null; } }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.ISelectionItemProvider.SelectionContainer { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionProvider.CanSelectMultiple { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionProvider.IsSelectionRequired { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer GetPeerFromPointCore(System.Windows.Point point) { throw null; }
        void System.Windows.Automation.Provider.IInvokeProvider.Invoke() { }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IItemContainerProvider.FindItemByProperty(System.Windows.Automation.Provider.IRawElementProviderSimple startAfter, int propertyId, object value) { throw null; }
        void System.Windows.Automation.Provider.IScrollItemProvider.ScrollIntoView() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.AddToSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.RemoveFromSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.Select() { }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ISelectionProvider.GetSelection() { throw null; }
    }
    public sealed partial class DataGridRowAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public DataGridRowAutomationPeer(System.Windows.Controls.DataGridRow owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public sealed partial class DataGridRowHeaderAutomationPeer : System.Windows.Automation.Peers.ButtonBaseAutomationPeer
    {
        public DataGridRowHeaderAutomationPeer(System.Windows.Controls.Primitives.DataGridRowHeader owner) : base (default(System.Windows.Controls.Primitives.ButtonBase)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
    }
    public sealed partial class DatePickerAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer, System.Windows.Automation.Provider.IExpandCollapseProvider, System.Windows.Automation.Provider.IValueProvider
    {
        public DatePickerAutomationPeer(System.Windows.Controls.DatePicker owner) : base (default(System.Windows.FrameworkElement)) { }
        System.Windows.Automation.ExpandCollapseState System.Windows.Automation.Provider.IExpandCollapseProvider.ExpandCollapseState { get { throw null; } }
        bool System.Windows.Automation.Provider.IValueProvider.IsReadOnly { get { throw null; } }
        string System.Windows.Automation.Provider.IValueProvider.Value { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override string GetLocalizedControlTypeCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override void SetFocusCore() { }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Collapse() { }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Expand() { }
        void System.Windows.Automation.Provider.IValueProvider.SetValue(string value) { }
    }
    public sealed partial class DateTimeAutomationPeer : System.Windows.Automation.Peers.AutomationPeer, System.Windows.Automation.Provider.IGridItemProvider, System.Windows.Automation.Provider.IInvokeProvider, System.Windows.Automation.Provider.ISelectionItemProvider, System.Windows.Automation.Provider.ITableItemProvider, System.Windows.Automation.Provider.IVirtualizedItemProvider
    {
        internal DateTimeAutomationPeer() { }
        int System.Windows.Automation.Provider.IGridItemProvider.Column { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.ColumnSpan { get { throw null; } }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IGridItemProvider.ContainingGrid { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.Row { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.RowSpan { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionItemProvider.IsSelected { get { throw null; } }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.ISelectionItemProvider.SelectionContainer { get { throw null; } }
        protected override string GetAcceleratorKeyCore() { throw null; }
        protected override string GetAccessKeyCore() { throw null; }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetAutomationIdCore() { throw null; }
        protected override System.Windows.Rect GetBoundingRectangleCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override System.Windows.Point GetClickablePointCore() { throw null; }
        protected override string GetHelpTextCore() { throw null; }
        protected override string GetItemStatusCore() { throw null; }
        protected override string GetItemTypeCore() { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer GetLabeledByCore() { throw null; }
        protected override System.Windows.Automation.AutomationLiveSetting GetLiveSettingCore() { throw null; }
        protected override string GetLocalizedControlTypeCore() { throw null; }
        protected override string GetNameCore() { throw null; }
        protected override System.Windows.Automation.Peers.AutomationOrientation GetOrientationCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override int GetPositionInSetCore() { throw null; }
        protected override int GetSizeOfSetCore() { throw null; }
        protected override bool HasKeyboardFocusCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
        protected override bool IsControlElementCore() { throw null; }
        protected override bool IsEnabledCore() { throw null; }
        protected override bool IsKeyboardFocusableCore() { throw null; }
        protected override bool IsOffscreenCore() { throw null; }
        protected override bool IsPasswordCore() { throw null; }
        protected override bool IsRequiredForFormCore() { throw null; }
        protected override void SetFocusCore() { }
        void System.Windows.Automation.Provider.IInvokeProvider.Invoke() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.AddToSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.RemoveFromSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.Select() { }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableItemProvider.GetColumnHeaderItems() { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableItemProvider.GetRowHeaderItems() { throw null; }
        void System.Windows.Automation.Provider.IVirtualizedItemProvider.Realize() { }
    }
    public partial class DocumentAutomationPeer : System.Windows.Automation.Peers.ContentTextAutomationPeer
    {
        public DocumentAutomationPeer(System.Windows.FrameworkContentElement owner) : base (default(System.Windows.FrameworkContentElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Windows.Rect GetBoundingRectangleCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override System.Windows.Point GetClickablePointCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override bool IsControlElementCore() { throw null; }
        protected override bool IsOffscreenCore() { throw null; }
    }
    public partial class DocumentPageViewAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public DocumentPageViewAutomationPeer(System.Windows.Controls.Primitives.DocumentPageView owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override string GetAutomationIdCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
    }
    public partial class DocumentViewerAutomationPeer : System.Windows.Automation.Peers.DocumentViewerBaseAutomationPeer
    {
        public DocumentViewerAutomationPeer(System.Windows.Controls.DocumentViewer owner) : base (default(System.Windows.Controls.Primitives.DocumentViewerBase)) { }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
    }
    public partial class DocumentViewerBaseAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public DocumentViewerBaseAutomationPeer(System.Windows.Controls.Primitives.DocumentViewerBase owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
    }
    public partial class ExpanderAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer, System.Windows.Automation.Provider.IExpandCollapseProvider
    {
        public ExpanderAutomationPeer(System.Windows.Controls.Expander owner) : base (default(System.Windows.FrameworkElement)) { }
        System.Windows.Automation.ExpandCollapseState System.Windows.Automation.Provider.IExpandCollapseProvider.ExpandCollapseState { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface pattern) { throw null; }
        protected override bool HasKeyboardFocusCore() { throw null; }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Collapse() { }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Expand() { }
    }
    public partial class FixedPageAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public FixedPageAutomationPeer(System.Windows.Documents.FixedPage owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class FlowDocumentPageViewerAutomationPeer : System.Windows.Automation.Peers.DocumentViewerBaseAutomationPeer
    {
        public FlowDocumentPageViewerAutomationPeer(System.Windows.Controls.FlowDocumentPageViewer owner) : base (default(System.Windows.Controls.Primitives.DocumentViewerBase)) { }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class FlowDocumentReaderAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer, System.Windows.Automation.Provider.IMultipleViewProvider
    {
        public FlowDocumentReaderAutomationPeer(System.Windows.Controls.FlowDocumentReader owner) : base (default(System.Windows.FrameworkElement)) { }
        int System.Windows.Automation.Provider.IMultipleViewProvider.CurrentView { get { throw null; } }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        int[] System.Windows.Automation.Provider.IMultipleViewProvider.GetSupportedViews() { throw null; }
        string System.Windows.Automation.Provider.IMultipleViewProvider.GetViewName(int viewId) { throw null; }
        void System.Windows.Automation.Provider.IMultipleViewProvider.SetCurrentView(int viewId) { }
    }
    public partial class FlowDocumentScrollViewerAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public FlowDocumentScrollViewerAutomationPeer(System.Windows.Controls.FlowDocumentScrollViewer owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
    }
    public partial class FrameAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public FrameAutomationPeer(System.Windows.Controls.Frame owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class FrameworkContentElementAutomationPeer : System.Windows.Automation.Peers.ContentElementAutomationPeer
    {
        public FrameworkContentElementAutomationPeer(System.Windows.FrameworkContentElement owner) : base (default(System.Windows.ContentElement)) { }
        protected override string GetAutomationIdCore() { throw null; }
        protected override string GetHelpTextCore() { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer GetLabeledByCore() { throw null; }
    }
    public partial class FrameworkElementAutomationPeer : System.Windows.Automation.Peers.UIElementAutomationPeer
    {
        public FrameworkElementAutomationPeer(System.Windows.FrameworkElement owner) : base (default(System.Windows.UIElement)) { }
        protected override string GetAutomationIdCore() { throw null; }
        protected override string GetHelpTextCore() { throw null; }
        protected override string GetNameCore() { throw null; }
    }
    public partial class GridSplitterAutomationPeer : System.Windows.Automation.Peers.ThumbAutomationPeer, System.Windows.Automation.Provider.ITransformProvider
    {
        public GridSplitterAutomationPeer(System.Windows.Controls.GridSplitter owner) : base (default(System.Windows.Controls.Primitives.Thumb)) { }
        bool System.Windows.Automation.Provider.ITransformProvider.CanMove { get { throw null; } }
        bool System.Windows.Automation.Provider.ITransformProvider.CanResize { get { throw null; } }
        bool System.Windows.Automation.Provider.ITransformProvider.CanRotate { get { throw null; } }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.ITransformProvider.Move(double x, double y) { }
        void System.Windows.Automation.Provider.ITransformProvider.Resize(double width, double height) { }
        void System.Windows.Automation.Provider.ITransformProvider.Rotate(double degrees) { }
    }
    public partial class GridViewAutomationPeer : System.Windows.Automation.Peers.IViewAutomationPeer, System.Windows.Automation.Provider.IGridProvider, System.Windows.Automation.Provider.ITableProvider
    {
        public GridViewAutomationPeer(System.Windows.Controls.GridView owner, System.Windows.Controls.ListView listview) { }
        int System.Windows.Automation.Provider.IGridProvider.ColumnCount { get { throw null; } }
        int System.Windows.Automation.Provider.IGridProvider.RowCount { get { throw null; } }
        System.Windows.Automation.RowOrColumnMajor System.Windows.Automation.Provider.ITableProvider.RowOrColumnMajor { get { throw null; } }
        System.Windows.Automation.Peers.ItemAutomationPeer System.Windows.Automation.Peers.IViewAutomationPeer.CreateItemAutomationPeer(object item) { throw null; }
        System.Windows.Automation.Peers.AutomationControlType System.Windows.Automation.Peers.IViewAutomationPeer.GetAutomationControlType() { throw null; }
        System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> System.Windows.Automation.Peers.IViewAutomationPeer.GetChildren(System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> children) { throw null; }
        object System.Windows.Automation.Peers.IViewAutomationPeer.GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Peers.IViewAutomationPeer.ItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        void System.Windows.Automation.Peers.IViewAutomationPeer.ViewDetached() { }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IGridProvider.GetItem(int row, int column) { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableProvider.GetColumnHeaders() { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableProvider.GetRowHeaders() { throw null; }
    }
    public partial class GridViewCellAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer, System.Windows.Automation.Provider.IGridItemProvider, System.Windows.Automation.Provider.ITableItemProvider
    {
        internal GridViewCellAutomationPeer() : base (default(System.Windows.FrameworkElement)) { }
        int System.Windows.Automation.Provider.IGridItemProvider.Column { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.ColumnSpan { get { throw null; } }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IGridItemProvider.ContainingGrid { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.Row { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.RowSpan { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override bool IsControlElementCore() { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableItemProvider.GetColumnHeaderItems() { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ITableItemProvider.GetRowHeaderItems() { throw null; }
    }
    public partial class GridViewColumnHeaderAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer, System.Windows.Automation.Provider.IInvokeProvider, System.Windows.Automation.Provider.ITransformProvider
    {
        public GridViewColumnHeaderAutomationPeer(System.Windows.Controls.GridViewColumnHeader owner) : base (default(System.Windows.FrameworkElement)) { }
        bool System.Windows.Automation.Provider.ITransformProvider.CanMove { get { throw null; } }
        bool System.Windows.Automation.Provider.ITransformProvider.CanResize { get { throw null; } }
        bool System.Windows.Automation.Provider.ITransformProvider.CanRotate { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override bool IsContentElementCore() { throw null; }
        void System.Windows.Automation.Provider.IInvokeProvider.Invoke() { }
        void System.Windows.Automation.Provider.ITransformProvider.Move(double x, double y) { }
        void System.Windows.Automation.Provider.ITransformProvider.Resize(double width, double height) { }
        void System.Windows.Automation.Provider.ITransformProvider.Rotate(double degrees) { }
    }
    public partial class GridViewHeaderRowPresenterAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public GridViewHeaderRowPresenterAutomationPeer(System.Windows.Controls.GridViewHeaderRowPresenter owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
    }
    public partial class GridViewItemAutomationPeer : System.Windows.Automation.Peers.ListBoxItemAutomationPeer
    {
        public GridViewItemAutomationPeer(object owner, System.Windows.Automation.Peers.ListViewAutomationPeer listviewAP) : base (default(object), default(System.Windows.Automation.Peers.SelectorAutomationPeer)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class GroupBoxAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public GroupBoxAutomationPeer(System.Windows.Controls.GroupBox owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override string GetNameCore() { throw null; }
    }
    public partial class GroupItemAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public GroupItemAutomationPeer(System.Windows.Controls.GroupItem owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override int GetPositionInSetCore() { throw null; }
        protected override int GetSizeOfSetCore() { throw null; }
        protected override bool HasKeyboardFocusCore() { throw null; }
        protected override bool IsKeyboardFocusableCore() { throw null; }
        protected override void SetFocusCore() { }
    }
    public partial class HyperlinkAutomationPeer : System.Windows.Automation.Peers.TextElementAutomationPeer, System.Windows.Automation.Provider.IInvokeProvider
    {
        public HyperlinkAutomationPeer(System.Windows.Documents.Hyperlink owner) : base (default(System.Windows.Documents.TextElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override string GetNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override bool IsControlElementCore() { throw null; }
        void System.Windows.Automation.Provider.IInvokeProvider.Invoke() { }
    }
    public partial class ImageAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public ImageAutomationPeer(System.Windows.Controls.Image owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class InkCanvasAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public InkCanvasAutomationPeer(System.Windows.Controls.InkCanvas owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class InkPresenterAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public InkPresenterAutomationPeer(System.Windows.Controls.InkPresenter owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public abstract partial class ItemAutomationPeer : System.Windows.Automation.Peers.AutomationPeer, System.Windows.Automation.Provider.IVirtualizedItemProvider
    {
        protected ItemAutomationPeer(object item, System.Windows.Automation.Peers.ItemsControlAutomationPeer itemsControlAutomationPeer) { }
        public object Item { get { throw null; } }
        public System.Windows.Automation.Peers.ItemsControlAutomationPeer ItemsControlAutomationPeer { get { throw null; } }
        protected override string GetAcceleratorKeyCore() { throw null; }
        protected override string GetAccessKeyCore() { throw null; }
        protected override string GetAutomationIdCore() { throw null; }
        protected override System.Windows.Rect GetBoundingRectangleCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override System.Windows.Point GetClickablePointCore() { throw null; }
        protected override string GetHelpTextCore() { throw null; }
        protected override string GetItemStatusCore() { throw null; }
        protected override string GetItemTypeCore() { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer GetLabeledByCore() { throw null; }
        protected override System.Windows.Automation.AutomationLiveSetting GetLiveSettingCore() { throw null; }
        protected override string GetNameCore() { throw null; }
        protected override System.Windows.Automation.Peers.AutomationOrientation GetOrientationCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override int GetPositionInSetCore() { throw null; }
        protected override int GetSizeOfSetCore() { throw null; }
        protected override bool HasKeyboardFocusCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
        protected override bool IsControlElementCore() { throw null; }
        protected override bool IsEnabledCore() { throw null; }
        protected override bool IsKeyboardFocusableCore() { throw null; }
        protected override bool IsOffscreenCore() { throw null; }
        protected override bool IsPasswordCore() { throw null; }
        protected override bool IsRequiredForFormCore() { throw null; }
        protected override void SetFocusCore() { }
        void System.Windows.Automation.Provider.IVirtualizedItemProvider.Realize() { }
    }
    public abstract partial class ItemsControlAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer, System.Windows.Automation.Provider.IItemContainerProvider
    {
        protected ItemsControlAutomationPeer(System.Windows.Controls.ItemsControl owner) : base (default(System.Windows.FrameworkElement)) { }
        protected virtual bool IsVirtualized { get { throw null; } }
        protected abstract System.Windows.Automation.Peers.ItemAutomationPeer CreateItemAutomationPeer(object item);
        protected internal virtual System.Windows.Automation.Peers.ItemAutomationPeer FindOrCreateItemAutomationPeer(object item) { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IItemContainerProvider.FindItemByProperty(System.Windows.Automation.Provider.IRawElementProviderSimple startAfter, int propertyId, object value) { throw null; }
    }
    public partial interface IViewAutomationPeer
    {
        System.Windows.Automation.Peers.ItemAutomationPeer CreateItemAutomationPeer(object item);
        System.Windows.Automation.Peers.AutomationControlType GetAutomationControlType();
        System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildren(System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> children);
        object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface);
        void ItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e);
        void ViewDetached();
    }
    public partial class LabelAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public LabelAutomationPeer(System.Windows.Controls.Label owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override string GetNameCore() { throw null; }
    }
    public partial class ListBoxAutomationPeer : System.Windows.Automation.Peers.SelectorAutomationPeer
    {
        public ListBoxAutomationPeer(System.Windows.Controls.ListBox owner) : base (default(System.Windows.Controls.Primitives.Selector)) { }
        protected override System.Windows.Automation.Peers.ItemAutomationPeer CreateItemAutomationPeer(object item) { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class ListBoxItemAutomationPeer : System.Windows.Automation.Peers.SelectorItemAutomationPeer, System.Windows.Automation.Provider.IScrollItemProvider
    {
        public ListBoxItemAutomationPeer(object owner, System.Windows.Automation.Peers.SelectorAutomationPeer selectorAutomationPeer) : base (default(object), default(System.Windows.Automation.Peers.SelectorAutomationPeer)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.IScrollItemProvider.ScrollIntoView() { }
    }
    public partial class ListBoxItemWrapperAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public ListBoxItemWrapperAutomationPeer(System.Windows.Controls.ListBoxItem owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class ListViewAutomationPeer : System.Windows.Automation.Peers.ListBoxAutomationPeer
    {
        public ListViewAutomationPeer(System.Windows.Controls.ListView owner) : base (default(System.Windows.Controls.ListBox)) { }
        protected internal System.Windows.Automation.Peers.IViewAutomationPeer ViewAutomationPeer { get { throw null; } set { } }
        protected override System.Windows.Automation.Peers.ItemAutomationPeer CreateItemAutomationPeer(object item) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
    }
    public partial class MediaElementAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public MediaElementAutomationPeer(System.Windows.Controls.MediaElement owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class MenuAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public MenuAutomationPeer(System.Windows.Controls.Menu owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
    }
    public partial class MenuItemAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer, System.Windows.Automation.Provider.IExpandCollapseProvider, System.Windows.Automation.Provider.IInvokeProvider, System.Windows.Automation.Provider.IToggleProvider
    {
        public MenuItemAutomationPeer(System.Windows.Controls.MenuItem owner) : base (default(System.Windows.FrameworkElement)) { }
        System.Windows.Automation.ExpandCollapseState System.Windows.Automation.Provider.IExpandCollapseProvider.ExpandCollapseState { get { throw null; } }
        System.Windows.Automation.ToggleState System.Windows.Automation.Provider.IToggleProvider.ToggleState { get { throw null; } }
        protected override string GetAccessKeyCore() { throw null; }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override string GetNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override int GetPositionInSetCore() { throw null; }
        protected override int GetSizeOfSetCore() { throw null; }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Collapse() { }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Expand() { }
        void System.Windows.Automation.Provider.IInvokeProvider.Invoke() { }
        void System.Windows.Automation.Provider.IToggleProvider.Toggle() { }
    }
    public partial class NavigationWindowAutomationPeer : System.Windows.Automation.Peers.WindowAutomationPeer
    {
        public NavigationWindowAutomationPeer(System.Windows.Navigation.NavigationWindow owner) : base (default(System.Windows.Window)) { }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class PasswordBoxAutomationPeer : System.Windows.Automation.Peers.TextAutomationPeer, System.Windows.Automation.Provider.IValueProvider
    {
        public PasswordBoxAutomationPeer(System.Windows.Controls.PasswordBox owner) : base (default(System.Windows.FrameworkElement)) { }
        bool System.Windows.Automation.Provider.IValueProvider.IsReadOnly { get { throw null; } }
        string System.Windows.Automation.Provider.IValueProvider.Value { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override bool IsPasswordCore() { throw null; }
        void System.Windows.Automation.Provider.IValueProvider.SetValue(string value) { }
    }
    public partial class ProgressBarAutomationPeer : System.Windows.Automation.Peers.RangeBaseAutomationPeer, System.Windows.Automation.Provider.IRangeValueProvider
    {
        public ProgressBarAutomationPeer(System.Windows.Controls.ProgressBar owner) : base (default(System.Windows.Controls.Primitives.RangeBase)) { }
        bool System.Windows.Automation.Provider.IRangeValueProvider.IsReadOnly { get { throw null; } }
        double System.Windows.Automation.Provider.IRangeValueProvider.LargeChange { get { throw null; } }
        double System.Windows.Automation.Provider.IRangeValueProvider.SmallChange { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.IRangeValueProvider.SetValue(double val) { }
    }
    public partial class RadioButtonAutomationPeer : System.Windows.Automation.Peers.ToggleButtonAutomationPeer, System.Windows.Automation.Provider.ISelectionItemProvider
    {
        public RadioButtonAutomationPeer(System.Windows.Controls.RadioButton owner) : base (default(System.Windows.Controls.Primitives.ToggleButton)) { }
        bool System.Windows.Automation.Provider.ISelectionItemProvider.IsSelected { get { throw null; } }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.ISelectionItemProvider.SelectionContainer { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.ISelectionItemProvider.AddToSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.RemoveFromSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.Select() { }
    }
    public partial class RangeBaseAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer, System.Windows.Automation.Provider.IRangeValueProvider
    {
        public RangeBaseAutomationPeer(System.Windows.Controls.Primitives.RangeBase owner) : base (default(System.Windows.FrameworkElement)) { }
        bool System.Windows.Automation.Provider.IRangeValueProvider.IsReadOnly { get { throw null; } }
        double System.Windows.Automation.Provider.IRangeValueProvider.LargeChange { get { throw null; } }
        double System.Windows.Automation.Provider.IRangeValueProvider.Maximum { get { throw null; } }
        double System.Windows.Automation.Provider.IRangeValueProvider.Minimum { get { throw null; } }
        double System.Windows.Automation.Provider.IRangeValueProvider.SmallChange { get { throw null; } }
        double System.Windows.Automation.Provider.IRangeValueProvider.Value { get { throw null; } }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.IRangeValueProvider.SetValue(double val) { }
    }
    public partial class RepeatButtonAutomationPeer : System.Windows.Automation.Peers.ButtonBaseAutomationPeer, System.Windows.Automation.Provider.IInvokeProvider
    {
        public RepeatButtonAutomationPeer(System.Windows.Controls.Primitives.RepeatButton owner) : base (default(System.Windows.Controls.Primitives.ButtonBase)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.IInvokeProvider.Invoke() { }
    }
    public partial class RichTextBoxAutomationPeer : System.Windows.Automation.Peers.TextAutomationPeer
    {
        public RichTextBoxAutomationPeer(System.Windows.Controls.RichTextBox owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
    }
    public partial class ScrollBarAutomationPeer : System.Windows.Automation.Peers.RangeBaseAutomationPeer
    {
        public ScrollBarAutomationPeer(System.Windows.Controls.Primitives.ScrollBar owner) : base (default(System.Windows.Controls.Primitives.RangeBase)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override System.Windows.Point GetClickablePointCore() { throw null; }
        protected override System.Windows.Automation.Peers.AutomationOrientation GetOrientationCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
    }
    public partial class ScrollViewerAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer, System.Windows.Automation.Provider.IScrollProvider
    {
        public ScrollViewerAutomationPeer(System.Windows.Controls.ScrollViewer owner) : base (default(System.Windows.FrameworkElement)) { }
        bool System.Windows.Automation.Provider.IScrollProvider.HorizontallyScrollable { get { throw null; } }
        double System.Windows.Automation.Provider.IScrollProvider.HorizontalScrollPercent { get { throw null; } }
        double System.Windows.Automation.Provider.IScrollProvider.HorizontalViewSize { get { throw null; } }
        bool System.Windows.Automation.Provider.IScrollProvider.VerticallyScrollable { get { throw null; } }
        double System.Windows.Automation.Provider.IScrollProvider.VerticalScrollPercent { get { throw null; } }
        double System.Windows.Automation.Provider.IScrollProvider.VerticalViewSize { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override bool IsControlElementCore() { throw null; }
        void System.Windows.Automation.Provider.IScrollProvider.Scroll(System.Windows.Automation.ScrollAmount horizontalAmount, System.Windows.Automation.ScrollAmount verticalAmount) { }
        void System.Windows.Automation.Provider.IScrollProvider.SetScrollPercent(double horizontalPercent, double verticalPercent) { }
    }
    public abstract partial class SelectorAutomationPeer : System.Windows.Automation.Peers.ItemsControlAutomationPeer, System.Windows.Automation.Provider.ISelectionProvider
    {
        protected SelectorAutomationPeer(System.Windows.Controls.Primitives.Selector owner) : base (default(System.Windows.Controls.ItemsControl)) { }
        bool System.Windows.Automation.Provider.ISelectionProvider.CanSelectMultiple { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionProvider.IsSelectionRequired { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ISelectionProvider.GetSelection() { throw null; }
    }
    public abstract partial class SelectorItemAutomationPeer : System.Windows.Automation.Peers.ItemAutomationPeer, System.Windows.Automation.Provider.ISelectionItemProvider
    {
        protected SelectorItemAutomationPeer(object owner, System.Windows.Automation.Peers.SelectorAutomationPeer selectorAutomationPeer) : base (default(object), default(System.Windows.Automation.Peers.ItemsControlAutomationPeer)) { }
        bool System.Windows.Automation.Provider.ISelectionItemProvider.IsSelected { get { throw null; } }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.ISelectionItemProvider.SelectionContainer { get { throw null; } }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.ISelectionItemProvider.AddToSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.RemoveFromSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.Select() { }
    }
    public partial class SeparatorAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public SeparatorAutomationPeer(System.Windows.Controls.Separator owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
    }
    public partial class SliderAutomationPeer : System.Windows.Automation.Peers.RangeBaseAutomationPeer
    {
        public SliderAutomationPeer(System.Windows.Controls.Slider owner) : base (default(System.Windows.Controls.Primitives.RangeBase)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override System.Windows.Point GetClickablePointCore() { throw null; }
    }
    public partial class StatusBarAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public StatusBarAutomationPeer(System.Windows.Controls.Primitives.StatusBar owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class StatusBarItemAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public StatusBarItemAutomationPeer(System.Windows.Controls.Primitives.StatusBarItem owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class TabControlAutomationPeer : System.Windows.Automation.Peers.SelectorAutomationPeer, System.Windows.Automation.Provider.ISelectionProvider
    {
        public TabControlAutomationPeer(System.Windows.Controls.TabControl owner) : base (default(System.Windows.Controls.Primitives.Selector)) { }
        bool System.Windows.Automation.Provider.ISelectionProvider.IsSelectionRequired { get { throw null; } }
        protected override System.Windows.Automation.Peers.ItemAutomationPeer CreateItemAutomationPeer(object item) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override System.Windows.Point GetClickablePointCore() { throw null; }
    }
    public partial class TabItemAutomationPeer : System.Windows.Automation.Peers.SelectorItemAutomationPeer, System.Windows.Automation.Provider.ISelectionItemProvider
    {
        public TabItemAutomationPeer(object owner, System.Windows.Automation.Peers.TabControlAutomationPeer tabControlAutomationPeer) : base (default(object), default(System.Windows.Automation.Peers.SelectorAutomationPeer)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override string GetNameCore() { throw null; }
        void System.Windows.Automation.Provider.ISelectionItemProvider.RemoveFromSelection() { }
    }
    public partial class TabItemWrapperAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public TabItemWrapperAutomationPeer(System.Windows.Controls.TabItem owner) : base (default(System.Windows.FrameworkElement)) { }
    }
    public partial class TableAutomationPeer : System.Windows.Automation.Peers.TextElementAutomationPeer, System.Windows.Automation.Provider.IGridProvider
    {
        public TableAutomationPeer(System.Windows.Documents.Table owner) : base (default(System.Windows.Documents.TextElement)) { }
        int System.Windows.Automation.Provider.IGridProvider.ColumnCount { get { throw null; } }
        int System.Windows.Automation.Provider.IGridProvider.RowCount { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override bool IsControlElementCore() { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IGridProvider.GetItem(int row, int column) { throw null; }
    }
    public partial class TableCellAutomationPeer : System.Windows.Automation.Peers.TextElementAutomationPeer, System.Windows.Automation.Provider.IGridItemProvider
    {
        public TableCellAutomationPeer(System.Windows.Documents.TableCell owner) : base (default(System.Windows.Documents.TextElement)) { }
        int System.Windows.Automation.Provider.IGridItemProvider.Column { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.ColumnSpan { get { throw null; } }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.IGridItemProvider.ContainingGrid { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.Row { get { throw null; } }
        int System.Windows.Automation.Provider.IGridItemProvider.RowSpan { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override string GetLocalizedControlTypeCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        protected override bool IsControlElementCore() { throw null; }
    }
    public abstract partial class TextAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        protected TextAutomationPeer(System.Windows.FrameworkElement owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override string GetNameCore() { throw null; }
    }
    public partial class TextBlockAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public TextBlockAutomationPeer(System.Windows.Controls.TextBlock owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override bool IsControlElementCore() { throw null; }
    }
    public partial class TextBoxAutomationPeer : System.Windows.Automation.Peers.TextAutomationPeer, System.Windows.Automation.Provider.IValueProvider
    {
        public TextBoxAutomationPeer(System.Windows.Controls.TextBox owner) : base (default(System.Windows.FrameworkElement)) { }
        bool System.Windows.Automation.Provider.IValueProvider.IsReadOnly { get { throw null; } }
        string System.Windows.Automation.Provider.IValueProvider.Value { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.IValueProvider.SetValue(string value) { }
    }
    public partial class TextElementAutomationPeer : System.Windows.Automation.Peers.ContentTextAutomationPeer
    {
        public TextElementAutomationPeer(System.Windows.Documents.TextElement owner) : base (default(System.Windows.FrameworkContentElement)) { }
        protected override System.Windows.Rect GetBoundingRectangleCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override System.Windows.Point GetClickablePointCore() { throw null; }
        protected override bool IsOffscreenCore() { throw null; }
    }
    public partial class ThumbAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public ThumbAutomationPeer(System.Windows.Controls.Primitives.Thumb owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override bool IsContentElementCore() { throw null; }
    }
    public partial class ToggleButtonAutomationPeer : System.Windows.Automation.Peers.ButtonBaseAutomationPeer, System.Windows.Automation.Provider.IToggleProvider
    {
        public ToggleButtonAutomationPeer(System.Windows.Controls.Primitives.ToggleButton owner) : base (default(System.Windows.Controls.Primitives.ButtonBase)) { }
        System.Windows.Automation.ToggleState System.Windows.Automation.Provider.IToggleProvider.ToggleState { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.IToggleProvider.Toggle() { }
    }
    public partial class ToolBarAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public ToolBarAutomationPeer(System.Windows.Controls.ToolBar owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class ToolTipAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public ToolTipAutomationPeer(System.Windows.Controls.ToolTip owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class TreeViewAutomationPeer : System.Windows.Automation.Peers.ItemsControlAutomationPeer, System.Windows.Automation.Provider.ISelectionProvider
    {
        public TreeViewAutomationPeer(System.Windows.Controls.TreeView owner) : base (default(System.Windows.Controls.ItemsControl)) { }
        bool System.Windows.Automation.Provider.ISelectionProvider.CanSelectMultiple { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionProvider.IsSelectionRequired { get { throw null; } }
        protected override System.Windows.Automation.Peers.ItemAutomationPeer CreateItemAutomationPeer(object item) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        System.Windows.Automation.Provider.IRawElementProviderSimple[] System.Windows.Automation.Provider.ISelectionProvider.GetSelection() { throw null; }
    }
    public partial class TreeViewDataItemAutomationPeer : System.Windows.Automation.Peers.ItemAutomationPeer, System.Windows.Automation.Provider.IExpandCollapseProvider, System.Windows.Automation.Provider.IScrollItemProvider, System.Windows.Automation.Provider.ISelectionItemProvider
    {
        public TreeViewDataItemAutomationPeer(object item, System.Windows.Automation.Peers.ItemsControlAutomationPeer itemsControlAutomationPeer, System.Windows.Automation.Peers.TreeViewDataItemAutomationPeer parentDataItemAutomationPeer) : base (default(object), default(System.Windows.Automation.Peers.ItemsControlAutomationPeer)) { }
        public System.Windows.Automation.Peers.TreeViewDataItemAutomationPeer ParentDataItemAutomationPeer { get { throw null; } }
        System.Windows.Automation.ExpandCollapseState System.Windows.Automation.Provider.IExpandCollapseProvider.ExpandCollapseState { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionItemProvider.IsSelected { get { throw null; } }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.ISelectionItemProvider.SelectionContainer { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Collapse() { }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Expand() { }
        void System.Windows.Automation.Provider.IScrollItemProvider.ScrollIntoView() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.AddToSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.RemoveFromSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.Select() { }
    }
    public partial class TreeViewItemAutomationPeer : System.Windows.Automation.Peers.ItemsControlAutomationPeer, System.Windows.Automation.Provider.IExpandCollapseProvider, System.Windows.Automation.Provider.IScrollItemProvider, System.Windows.Automation.Provider.ISelectionItemProvider
    {
        public TreeViewItemAutomationPeer(System.Windows.Controls.TreeViewItem owner) : base (default(System.Windows.Controls.ItemsControl)) { }
        System.Windows.Automation.ExpandCollapseState System.Windows.Automation.Provider.IExpandCollapseProvider.ExpandCollapseState { get { throw null; } }
        bool System.Windows.Automation.Provider.ISelectionItemProvider.IsSelected { get { throw null; } }
        System.Windows.Automation.Provider.IRawElementProviderSimple System.Windows.Automation.Provider.ISelectionItemProvider.SelectionContainer { get { throw null; } }
        protected override System.Windows.Automation.Peers.ItemAutomationPeer CreateItemAutomationPeer(object item) { throw null; }
        protected internal override System.Windows.Automation.Peers.ItemAutomationPeer FindOrCreateItemAutomationPeer(object item) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Collections.Generic.List<System.Windows.Automation.Peers.AutomationPeer> GetChildrenCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface) { throw null; }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Collapse() { }
        void System.Windows.Automation.Provider.IExpandCollapseProvider.Expand() { }
        void System.Windows.Automation.Provider.IScrollItemProvider.ScrollIntoView() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.AddToSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.RemoveFromSelection() { }
        void System.Windows.Automation.Provider.ISelectionItemProvider.Select() { }
    }
    public partial class UserControlAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public UserControlAutomationPeer(System.Windows.Controls.UserControl owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class Viewport3DAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public Viewport3DAutomationPeer(System.Windows.Controls.Viewport3D owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
    }
    public partial class WindowAutomationPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
    {
        public WindowAutomationPeer(System.Windows.Window owner) : base (default(System.Windows.FrameworkElement)) { }
        protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore() { throw null; }
        protected override System.Windows.Rect GetBoundingRectangleCore() { throw null; }
        protected override string GetClassNameCore() { throw null; }
        protected override string GetNameCore() { throw null; }
    }
}
namespace System.Windows.Baml2006
{
    public partial class Baml2006Reader : System.Xaml.XamlReader, System.Xaml.IXamlLineInfo
    {
        public Baml2006Reader(System.IO.Stream stream) { }
        public Baml2006Reader(System.IO.Stream stream, System.Xaml.XamlReaderSettings xamlReaderSettings) { }
        public Baml2006Reader(string fileName) { }
        public override bool IsEof { get { throw null; } }
        public override System.Xaml.XamlMember Member { get { throw null; } }
        public override System.Xaml.NamespaceDeclaration Namespace { get { throw null; } }
        public override System.Xaml.XamlNodeType NodeType { get { throw null; } }
        public override System.Xaml.XamlSchemaContext SchemaContext { get { throw null; } }
        bool System.Xaml.IXamlLineInfo.HasLineInfo { get { throw null; } }
        int System.Xaml.IXamlLineInfo.LineNumber { get { throw null; } }
        int System.Xaml.IXamlLineInfo.LinePosition { get { throw null; } }
        public override System.Xaml.XamlType Type { get { throw null; } }
        public override object Value { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override bool Read() { throw null; }
    }
}
namespace System.Windows.Controls
{
    [System.Windows.Markup.ContentPropertyAttribute("Text")]
    public partial class AccessText : System.Windows.FrameworkElement, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty BaselineOffsetProperty;
        public static readonly System.Windows.DependencyProperty FontFamilyProperty;
        public static readonly System.Windows.DependencyProperty FontSizeProperty;
        public static readonly System.Windows.DependencyProperty FontStretchProperty;
        public static readonly System.Windows.DependencyProperty FontStyleProperty;
        public static readonly System.Windows.DependencyProperty FontWeightProperty;
        public static readonly System.Windows.DependencyProperty ForegroundProperty;
        public static readonly System.Windows.DependencyProperty LineHeightProperty;
        public static readonly System.Windows.DependencyProperty LineStackingStrategyProperty;
        public static readonly System.Windows.DependencyProperty TextAlignmentProperty;
        public static readonly System.Windows.DependencyProperty TextDecorationsProperty;
        public static readonly System.Windows.DependencyProperty TextEffectsProperty;
        public static readonly System.Windows.DependencyProperty TextProperty;
        public static readonly System.Windows.DependencyProperty TextTrimmingProperty;
        public static readonly System.Windows.DependencyProperty TextWrappingProperty;
        public AccessText() { }
        public char AccessKey { get { throw null; } }
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        public double BaselineOffset { get { throw null; } set { } }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Font, Modifiability=System.Windows.Modifiability.Unmodifiable)]
        public System.Windows.Media.FontFamily FontFamily { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.FontSizeConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None)]
        public double FontSize { get { throw null; } set { } }
        public System.Windows.FontStretch FontStretch { get { throw null; } set { } }
        public System.Windows.FontStyle FontStyle { get { throw null; } set { } }
        public System.Windows.FontWeight FontWeight { get { throw null; } set { } }
        public System.Windows.Media.Brush Foreground { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double LineHeight { get { throw null; } set { } }
        public System.Windows.LineStackingStrategy LineStackingStrategy { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string Text { get { throw null; } set { } }
        public System.Windows.TextAlignment TextAlignment { get { throw null; } set { } }
        public System.Windows.TextDecorationCollection TextDecorations { get { throw null; } set { } }
        public System.Windows.Media.TextEffectCollection TextEffects { get { throw null; } set { } }
        public System.Windows.TextTrimming TextTrimming { get { throw null; } set { } }
        public System.Windows.TextWrapping TextWrapping { get { throw null; } set { } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected sealed override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected sealed override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class AddingNewItemEventArgs : System.EventArgs
    {
        public AddingNewItemEventArgs() { }
        public object NewItem { get { throw null; } set { } }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Child")]
    public partial class AdornedElementPlaceholder : System.Windows.FrameworkElement, System.Windows.Markup.IAddChild
    {
        public AdornedElementPlaceholder() { }
        public System.Windows.UIElement AdornedElement { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public virtual System.Windows.UIElement Child { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeBounds) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override void OnInitialized(System.EventArgs e) { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Values")]
    public partial class AlternationConverter : System.Windows.Data.IValueConverter
    {
        public AlternationConverter() { }
        public System.Collections.IList Values { get { throw null; } }
        public object Convert(object o, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
        public object ConvertBack(object o, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
    public sealed partial class BooleanToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public BooleanToVisibilityConverter() { }
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
    }
    public partial class Border : System.Windows.Controls.Decorator
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty BorderBrushProperty;
        public static readonly System.Windows.DependencyProperty BorderThicknessProperty;
        public static readonly System.Windows.DependencyProperty CornerRadiusProperty;
        public static readonly System.Windows.DependencyProperty PaddingProperty;
        public Border() { }
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        public System.Windows.Media.Brush BorderBrush { get { throw null; } set { } }
        public System.Windows.Thickness BorderThickness { get { throw null; } set { } }
        public System.Windows.CornerRadius CornerRadius { get { throw null; } set { } }
        public System.Windows.Thickness Padding { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext dc) { }
    }
    public partial class BorderGapMaskConverter : System.Windows.Data.IMultiValueConverter
    {
        public BorderGapMaskConverter() { }
        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
        public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) { throw null; }
    }
    public partial class Button : System.Windows.Controls.Primitives.ButtonBase
    {
        public static readonly System.Windows.DependencyProperty IsCancelProperty;
        public static readonly System.Windows.DependencyProperty IsDefaultedProperty;
        public static readonly System.Windows.DependencyProperty IsDefaultProperty;
        public Button() { }
        public bool IsCancel { get { throw null; } set { } }
        public bool IsDefault { get { throw null; } set { } }
        public bool IsDefaulted { get { throw null; } }
        protected override void OnClick() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_CalendarItem", Type=typeof(System.Windows.Controls.Primitives.CalendarItem))]
    [System.Windows.TemplatePartAttribute(Name="PART_Root", Type=typeof(System.Windows.Controls.Panel))]
    public partial class Calendar : System.Windows.Controls.Control
    {
        public static readonly System.Windows.DependencyProperty CalendarButtonStyleProperty;
        public static readonly System.Windows.DependencyProperty CalendarDayButtonStyleProperty;
        public static readonly System.Windows.DependencyProperty CalendarItemStyleProperty;
        public static readonly System.Windows.DependencyProperty DisplayDateEndProperty;
        public static readonly System.Windows.DependencyProperty DisplayDateProperty;
        public static readonly System.Windows.DependencyProperty DisplayDateStartProperty;
        public static readonly System.Windows.DependencyProperty DisplayModeProperty;
        public static readonly System.Windows.DependencyProperty FirstDayOfWeekProperty;
        public static readonly System.Windows.DependencyProperty IsTodayHighlightedProperty;
        public static readonly System.Windows.DependencyProperty SelectedDateProperty;
        public static readonly System.Windows.RoutedEvent SelectedDatesChangedEvent;
        public static readonly System.Windows.DependencyProperty SelectionModeProperty;
        public Calendar() { }
        public System.Windows.Controls.CalendarBlackoutDatesCollection BlackoutDates { get { throw null; } }
        public System.Windows.Style CalendarButtonStyle { get { throw null; } set { } }
        public System.Windows.Style CalendarDayButtonStyle { get { throw null; } set { } }
        public System.Windows.Style CalendarItemStyle { get { throw null; } set { } }
        public System.DateTime DisplayDate { get { throw null; } set { } }
        public System.DateTime? DisplayDateEnd { get { throw null; } set { } }
        public System.DateTime? DisplayDateStart { get { throw null; } set { } }
        public System.Windows.Controls.CalendarMode DisplayMode { get { throw null; } set { } }
        public System.DayOfWeek FirstDayOfWeek { get { throw null; } set { } }
        public bool IsTodayHighlighted { get { throw null; } set { } }
        public System.DateTime? SelectedDate { get { throw null; } set { } }
        public System.Windows.Controls.SelectedDatesCollection SelectedDates { get { throw null; } }
        public System.Windows.Controls.CalendarSelectionMode SelectionMode { get { throw null; } set { } }
        public event System.EventHandler<System.Windows.Controls.CalendarDateChangedEventArgs> DisplayDateChanged { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.CalendarModeChangedEventArgs> DisplayModeChanged { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.SelectionChangedEventArgs> SelectedDatesChanged { add { } remove { } }
        public event System.EventHandler<System.EventArgs> SelectionModeChanged { add { } remove { } }
        public override void OnApplyTemplate() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDisplayDateChanged(System.Windows.Controls.CalendarDateChangedEventArgs e) { }
        protected virtual void OnDisplayModeChanged(System.Windows.Controls.CalendarModeChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnKeyUp(System.Windows.Input.KeyEventArgs e) { }
        protected virtual void OnSelectedDatesChanged(System.Windows.Controls.SelectionChangedEventArgs e) { }
        protected virtual void OnSelectionModeChanged(System.EventArgs e) { }
        public override string ToString() { throw null; }
    }
    public sealed partial class CalendarBlackoutDatesCollection : System.Collections.ObjectModel.ObservableCollection<System.Windows.Controls.CalendarDateRange>
    {
        public CalendarBlackoutDatesCollection(System.Windows.Controls.Calendar owner) { }
        public void AddDatesInPast() { }
        protected override void ClearItems() { }
        public bool Contains(System.DateTime date) { throw null; }
        public bool Contains(System.DateTime start, System.DateTime end) { throw null; }
        public bool ContainsAny(System.Windows.Controls.CalendarDateRange range) { throw null; }
        protected override void InsertItem(int index, System.Windows.Controls.CalendarDateRange item) { }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, System.Windows.Controls.CalendarDateRange item) { }
    }
    public partial class CalendarDateChangedEventArgs : System.Windows.RoutedEventArgs
    {
        internal CalendarDateChangedEventArgs() { }
        public System.DateTime? AddedDate { get { throw null; } }
        public System.DateTime? RemovedDate { get { throw null; } }
    }
    public sealed partial class CalendarDateRange : System.ComponentModel.INotifyPropertyChanged
    {
        public CalendarDateRange() { }
        public CalendarDateRange(System.DateTime day) { }
        public CalendarDateRange(System.DateTime start, System.DateTime end) { }
        public System.DateTime End { get { throw null; } set { } }
        public System.DateTime Start { get { throw null; } set { } }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }
    }
    public enum CalendarMode
    {
        Month = 0,
        Year = 1,
        Decade = 2,
    }
    public partial class CalendarModeChangedEventArgs : System.Windows.RoutedEventArgs
    {
        public CalendarModeChangedEventArgs(System.Windows.Controls.CalendarMode oldMode, System.Windows.Controls.CalendarMode newMode) { }
        public System.Windows.Controls.CalendarMode NewMode { get { throw null; } }
        public System.Windows.Controls.CalendarMode OldMode { get { throw null; } }
    }
    public enum CalendarSelectionMode
    {
        SingleDate = 0,
        SingleRange = 1,
        MultipleRange = 2,
        None = 3,
    }
    public partial class Canvas : System.Windows.Controls.Panel
    {
        public static readonly System.Windows.DependencyProperty BottomProperty;
        public static readonly System.Windows.DependencyProperty LeftProperty;
        public static readonly System.Windows.DependencyProperty RightProperty;
        public static readonly System.Windows.DependencyProperty TopProperty;
        public Canvas() { }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetBottom(System.Windows.UIElement element) { throw null; }
        protected override System.Windows.Media.Geometry GetLayoutClip(System.Windows.Size layoutSlotSize) { throw null; }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetLeft(System.Windows.UIElement element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetRight(System.Windows.UIElement element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetTop(System.Windows.UIElement element) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        public static void SetBottom(System.Windows.UIElement element, double length) { }
        public static void SetLeft(System.Windows.UIElement element, double length) { }
        public static void SetRight(System.Windows.UIElement element, double length) { }
        public static void SetTop(System.Windows.UIElement element, double length) { }
    }
    public enum CharacterCasing
    {
        Normal = 0,
        Lower = 1,
        Upper = 2,
    }
    [System.ComponentModel.DefaultEventAttribute("CheckStateChanged")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.CheckBox)]
    public partial class CheckBox : System.Windows.Controls.Primitives.ToggleButton
    {
        public CheckBox() { }
        protected override void OnAccessKey(System.Windows.Input.AccessKeyEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
    }
    public partial class CleanUpVirtualizedItemEventArgs : System.Windows.RoutedEventArgs
    {
        public CleanUpVirtualizedItemEventArgs(object value, System.Windows.UIElement element) { }
        public bool Cancel { get { throw null; } set { } }
        public System.Windows.UIElement UIElement { get { throw null; } }
        public object Value { get { throw null; } }
    }
    public delegate void CleanUpVirtualizedItemEventHandler(object sender, System.Windows.Controls.CleanUpVirtualizedItemEventArgs e);
    public enum ClickMode
    {
        Release = 0,
        Press = 1,
        Hover = 2,
    }
    public partial class ColumnDefinition : System.Windows.Controls.DefinitionBase
    {
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        public static readonly System.Windows.DependencyProperty MaxWidthProperty;
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        public static readonly System.Windows.DependencyProperty MinWidthProperty;
        public static readonly System.Windows.DependencyProperty WidthProperty;
        public ColumnDefinition() { }
        public double ActualWidth { get { throw null; } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double MaxWidth { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double MinWidth { get { throw null; } set { } }
        public double Offset { get { throw null; } }
        public System.Windows.GridLength Width { get { throw null; } set { } }
    }
    public sealed partial class ColumnDefinitionCollection : System.Collections.Generic.ICollection<System.Windows.Controls.ColumnDefinition>, System.Collections.Generic.IEnumerable<System.Windows.Controls.ColumnDefinition>, System.Collections.Generic.IList<System.Windows.Controls.ColumnDefinition>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal ColumnDefinitionCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Windows.Controls.ColumnDefinition this[int index] { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(System.Windows.Controls.ColumnDefinition value) { }
        public void Clear() { }
        public bool Contains(System.Windows.Controls.ColumnDefinition value) { throw null; }
        public void CopyTo(System.Windows.Controls.ColumnDefinition[] array, int index) { }
        public int IndexOf(System.Windows.Controls.ColumnDefinition value) { throw null; }
        public void Insert(int index, System.Windows.Controls.ColumnDefinition value) { }
        public bool Remove(System.Windows.Controls.ColumnDefinition value) { throw null; }
        public void RemoveAt(int index) { }
        public void RemoveRange(int index, int count) { }
        System.Collections.Generic.IEnumerator<System.Windows.Controls.ColumnDefinition> System.Collections.Generic.IEnumerable<System.Windows.Controls.ColumnDefinition>.GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.ComboBox)]
    [System.Windows.StyleTypedPropertyAttribute(Property="ItemContainerStyle", StyleTargetType=typeof(System.Windows.Controls.ComboBoxItem))]
    [System.Windows.TemplatePartAttribute(Name="PART_EditableTextBox", Type=typeof(System.Windows.Controls.TextBox))]
    [System.Windows.TemplatePartAttribute(Name="PART_Popup", Type=typeof(System.Windows.Controls.Primitives.Popup))]
    public partial class ComboBox : System.Windows.Controls.Primitives.Selector
    {
        public static readonly System.Windows.DependencyProperty IsDropDownOpenProperty;
        public static readonly System.Windows.DependencyProperty IsEditableProperty;
        public static readonly System.Windows.DependencyProperty IsReadOnlyProperty;
        public static readonly System.Windows.DependencyProperty MaxDropDownHeightProperty;
        public static readonly System.Windows.DependencyProperty SelectionBoxItemProperty;
        public static readonly System.Windows.DependencyProperty SelectionBoxItemStringFormatProperty;
        public static readonly System.Windows.DependencyProperty SelectionBoxItemTemplateProperty;
        public static readonly System.Windows.DependencyProperty ShouldPreserveUserEnteredPrefixProperty;
        public static readonly System.Windows.DependencyProperty StaysOpenOnEditProperty;
        public static readonly System.Windows.DependencyProperty TextProperty;
        public ComboBox() { }
        protected internal override bool HandlesScrolling { get { throw null; } }
        protected internal override bool HasEffectiveKeyboardFocus { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsDropDownOpen { get { throw null; } set { } }
        public bool IsEditable { get { throw null; } set { } }
        public bool IsReadOnly { get { throw null; } set { } }
        public bool IsSelectionBoxHighlighted { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double MaxDropDownHeight { get { throw null; } set { } }
        public object SelectionBoxItem { get { throw null; } }
        public string SelectionBoxItemStringFormat { get { throw null; } }
        public System.Windows.DataTemplate SelectionBoxItemTemplate { get { throw null; } }
        public bool ShouldPreserveUserEnteredPrefix { get { throw null; } set { } }
        public bool StaysOpenOnEdit { get { throw null; } set { } }
        public string Text { get { throw null; } set { } }
        public event System.EventHandler DropDownClosed { add { } remove { } }
        public event System.EventHandler DropDownOpened { add { } remove { } }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        public override void OnApplyTemplate() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDropDownClosed(System.EventArgs e) { }
        protected virtual void OnDropDownOpened(System.EventArgs e) { }
        protected override void OnIsKeyboardFocusWithinChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnIsMouseCapturedChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnSelectionChanged(System.Windows.Controls.SelectionChangedEventArgs e) { }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.ComboBox)]
    public partial class ComboBoxItem : System.Windows.Controls.ListBoxItem
    {
        public static readonly System.Windows.DependencyProperty IsHighlightedProperty;
        public ComboBoxItem() { }
        public bool IsHighlighted { get { throw null; } protected set { } }
        protected override void OnContentChanged(object oldContent, object newContent) { }
        protected override void OnGotKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e) { }
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
    }
    [System.ComponentModel.DefaultPropertyAttribute("Content")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    [System.Windows.Markup.ContentPropertyAttribute("Content")]
    public partial class ContentControl : System.Windows.Controls.Control, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty ContentProperty;
        public static readonly System.Windows.DependencyProperty ContentStringFormatProperty;
        public static readonly System.Windows.DependencyProperty ContentTemplateProperty;
        public static readonly System.Windows.DependencyProperty ContentTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty HasContentProperty;
        public ContentControl() { }
        [System.ComponentModel.BindableAttribute(true)]
        public object Content { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public string ContentStringFormat { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public System.Windows.DataTemplate ContentTemplate { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.DataTemplateSelector ContentTemplateSelector { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public bool HasContent { get { throw null; } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        protected virtual void AddChild(object value) { }
        protected virtual void AddText(string text) { }
        protected virtual void OnContentChanged(object oldContent, object newContent) { }
        protected virtual void OnContentStringFormatChanged(string oldContentStringFormat, string newContentStringFormat) { }
        protected virtual void OnContentTemplateChanged(System.Windows.DataTemplate oldContentTemplate, System.Windows.DataTemplate newContentTemplate) { }
        protected virtual void OnContentTemplateSelectorChanged(System.Windows.Controls.DataTemplateSelector oldContentTemplateSelector, System.Windows.Controls.DataTemplateSelector newContentTemplateSelector) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeContent() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public partial class ContentPresenter : System.Windows.FrameworkElement
    {
        public static readonly System.Windows.DependencyProperty ContentProperty;
        public static readonly System.Windows.DependencyProperty ContentSourceProperty;
        public static readonly System.Windows.DependencyProperty ContentStringFormatProperty;
        public static readonly System.Windows.DependencyProperty ContentTemplateProperty;
        public static readonly System.Windows.DependencyProperty ContentTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty RecognizesAccessKeyProperty;
        public ContentPresenter() { }
        public object Content { get { throw null; } set { } }
        public string ContentSource { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public string ContentStringFormat { get { throw null; } set { } }
        public System.Windows.DataTemplate ContentTemplate { get { throw null; } set { } }
        public System.Windows.Controls.DataTemplateSelector ContentTemplateSelector { get { throw null; } set { } }
        public bool RecognizesAccessKey { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected virtual System.Windows.DataTemplate ChooseTemplate() { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected virtual void OnContentStringFormatChanged(string oldContentStringFormat, string newContentStringFormat) { }
        protected virtual void OnContentTemplateChanged(System.Windows.DataTemplate oldContentTemplate, System.Windows.DataTemplate newContentTemplate) { }
        protected virtual void OnContentTemplateSelectorChanged(System.Windows.Controls.DataTemplateSelector oldContentTemplateSelector, System.Windows.Controls.DataTemplateSelector newContentTemplateSelector) { }
        protected virtual void OnTemplateChanged(System.Windows.DataTemplate oldTemplate, System.Windows.DataTemplate newTemplate) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeContentTemplateSelector() { throw null; }
    }
    [System.ComponentModel.DefaultEventAttribute("Opened")]
    public partial class ContextMenu : System.Windows.Controls.Primitives.MenuBase
    {
        public static readonly System.Windows.RoutedEvent ClosedEvent;
        public static readonly System.Windows.DependencyProperty CustomPopupPlacementCallbackProperty;
        public static readonly System.Windows.DependencyProperty HasDropShadowProperty;
        public static readonly System.Windows.DependencyProperty HorizontalOffsetProperty;
        public static readonly System.Windows.DependencyProperty IsOpenProperty;
        public static readonly System.Windows.RoutedEvent OpenedEvent;
        public static readonly System.Windows.DependencyProperty PlacementProperty;
        public static readonly System.Windows.DependencyProperty PlacementRectangleProperty;
        public static readonly System.Windows.DependencyProperty PlacementTargetProperty;
        public static readonly System.Windows.DependencyProperty StaysOpenProperty;
        public static readonly System.Windows.DependencyProperty VerticalOffsetProperty;
        public ContextMenu() { }
        [System.ComponentModel.BindableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.Controls.Primitives.CustomPopupPlacementCallback CustomPopupPlacementCallback { get { throw null; } set { } }
        protected internal override bool HandlesScrolling { get { throw null; } }
        public bool HasDropShadow { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double HorizontalOffset { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsOpen { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.Controls.Primitives.PlacementMode Placement { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.Rect PlacementRectangle { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.UIElement PlacementTarget { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public bool StaysOpen { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double VerticalOffset { get { throw null; } set { } }
        public event System.Windows.RoutedEventHandler Closed { add { } remove { } }
        public event System.Windows.RoutedEventHandler Opened { add { } remove { } }
        protected virtual void OnClosed(System.Windows.RoutedEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnIsKeyboardFocusWithinChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnKeyUp(System.Windows.Input.KeyEventArgs e) { }
        protected virtual void OnOpened(System.Windows.RoutedEventArgs e) { }
        protected internal override void OnVisualParentChanged(System.Windows.DependencyObject oldParent) { }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
    }
    public sealed partial class ContextMenuEventArgs : System.Windows.RoutedEventArgs
    {
        internal ContextMenuEventArgs() { }
        public double CursorLeft { get { throw null; } }
        public double CursorTop { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void ContextMenuEventHandler(object sender, System.Windows.Controls.ContextMenuEventArgs e);
    public static partial class ContextMenuService
    {
        public static readonly System.Windows.RoutedEvent ContextMenuClosingEvent;
        public static readonly System.Windows.RoutedEvent ContextMenuOpeningEvent;
        public static readonly System.Windows.DependencyProperty ContextMenuProperty;
        public static readonly System.Windows.DependencyProperty HasDropShadowProperty;
        public static readonly System.Windows.DependencyProperty HorizontalOffsetProperty;
        public static readonly System.Windows.DependencyProperty IsEnabledProperty;
        public static readonly System.Windows.DependencyProperty PlacementProperty;
        public static readonly System.Windows.DependencyProperty PlacementRectangleProperty;
        public static readonly System.Windows.DependencyProperty PlacementTargetProperty;
        public static readonly System.Windows.DependencyProperty ShowOnDisabledProperty;
        public static readonly System.Windows.DependencyProperty VerticalOffsetProperty;
        public static void AddContextMenuClosingHandler(System.Windows.DependencyObject element, System.Windows.Controls.ContextMenuEventHandler handler) { }
        public static void AddContextMenuOpeningHandler(System.Windows.DependencyObject element, System.Windows.Controls.ContextMenuEventHandler handler) { }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.Controls.ContextMenu GetContextMenu(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetHasDropShadow(System.Windows.DependencyObject element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static double GetHorizontalOffset(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetIsEnabled(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.Controls.Primitives.PlacementMode GetPlacement(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.Rect GetPlacementRectangle(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.UIElement GetPlacementTarget(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetShowOnDisabled(System.Windows.DependencyObject element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static double GetVerticalOffset(System.Windows.DependencyObject element) { throw null; }
        public static void RemoveContextMenuClosingHandler(System.Windows.DependencyObject element, System.Windows.Controls.ContextMenuEventHandler handler) { }
        public static void RemoveContextMenuOpeningHandler(System.Windows.DependencyObject element, System.Windows.Controls.ContextMenuEventHandler handler) { }
        public static void SetContextMenu(System.Windows.DependencyObject element, System.Windows.Controls.ContextMenu value) { }
        public static void SetHasDropShadow(System.Windows.DependencyObject element, bool value) { }
        public static void SetHorizontalOffset(System.Windows.DependencyObject element, double value) { }
        public static void SetIsEnabled(System.Windows.DependencyObject element, bool value) { }
        public static void SetPlacement(System.Windows.DependencyObject element, System.Windows.Controls.Primitives.PlacementMode value) { }
        public static void SetPlacementRectangle(System.Windows.DependencyObject element, System.Windows.Rect value) { }
        public static void SetPlacementTarget(System.Windows.DependencyObject element, System.Windows.UIElement value) { }
        public static void SetShowOnDisabled(System.Windows.DependencyObject element, bool value) { }
        public static void SetVerticalOffset(System.Windows.DependencyObject element, double value) { }
    }
    public partial class Control : System.Windows.FrameworkElement
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty BorderBrushProperty;
        public static readonly System.Windows.DependencyProperty BorderThicknessProperty;
        public static readonly System.Windows.DependencyProperty FontFamilyProperty;
        public static readonly System.Windows.DependencyProperty FontSizeProperty;
        public static readonly System.Windows.DependencyProperty FontStretchProperty;
        public static readonly System.Windows.DependencyProperty FontStyleProperty;
        public static readonly System.Windows.DependencyProperty FontWeightProperty;
        public static readonly System.Windows.DependencyProperty ForegroundProperty;
        public static readonly System.Windows.DependencyProperty HorizontalContentAlignmentProperty;
        public static readonly System.Windows.DependencyProperty IsTabStopProperty;
        public static readonly System.Windows.RoutedEvent MouseDoubleClickEvent;
        public static readonly System.Windows.DependencyProperty PaddingProperty;
        public static readonly System.Windows.RoutedEvent PreviewMouseDoubleClickEvent;
        public static readonly System.Windows.DependencyProperty TabIndexProperty;
        public static readonly System.Windows.DependencyProperty TemplateProperty;
        public static readonly System.Windows.DependencyProperty VerticalContentAlignmentProperty;
        public Control() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Media.Brush BorderBrush { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Thickness BorderThickness { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Font)]
        public System.Windows.Media.FontFamily FontFamily { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.FontSizeConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None)]
        public double FontSize { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.FontStretch FontStretch { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.FontStyle FontStyle { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.FontWeight FontWeight { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Media.Brush Foreground { get { throw null; } set { } }
        protected internal virtual bool HandlesScrolling { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.HorizontalAlignment HorizontalContentAlignment { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public bool IsTabStop { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.Thickness Padding { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public int TabIndex { get { throw null; } set { } }
        public System.Windows.Controls.ControlTemplate Template { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.VerticalAlignment VerticalContentAlignment { get { throw null; } set { } }
        public event System.Windows.Input.MouseButtonEventHandler MouseDoubleClick { add { } remove { } }
        public event System.Windows.Input.MouseButtonEventHandler PreviewMouseDoubleClick { add { } remove { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeBounds) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected virtual void OnMouseDoubleClick(System.Windows.Input.MouseButtonEventArgs e) { }
        protected virtual void OnPreviewMouseDoubleClick(System.Windows.Input.MouseButtonEventArgs e) { }
        protected virtual void OnTemplateChanged(System.Windows.Controls.ControlTemplate oldTemplate, System.Windows.Controls.ControlTemplate newTemplate) { }
        public override string ToString() { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    [System.Windows.Markup.DictionaryKeyPropertyAttribute("TargetType")]
    public partial class ControlTemplate : System.Windows.FrameworkTemplate
    {
        public ControlTemplate() { }
        public ControlTemplate(System.Type targetType) { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Windows.Markup.AmbientAttribute]
        public System.Type TargetType { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        [System.Windows.Markup.DependsOnAttribute("Template")]
        [System.Windows.Markup.DependsOnAttribute("VisualTree")]
        public System.Windows.TriggerCollection Triggers { get { throw null; } }
        protected override void ValidateTemplatedParent(System.Windows.FrameworkElement templatedParent) { }
    }
    public sealed partial class DataErrorValidationRule : System.Windows.Controls.ValidationRule
    {
        public DataErrorValidationRule() { }
        public override System.Windows.Controls.ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo) { throw null; }
    }
    public partial class DataGrid : System.Windows.Controls.Primitives.MultiSelector
    {
        public static readonly System.Windows.DependencyProperty AlternatingRowBackgroundProperty;
        public static readonly System.Windows.DependencyProperty AreRowDetailsFrozenProperty;
        public static readonly System.Windows.DependencyProperty AutoGenerateColumnsProperty;
        public static readonly System.Windows.Input.RoutedCommand BeginEditCommand;
        public static readonly System.Windows.Input.RoutedCommand CancelEditCommand;
        public static readonly System.Windows.DependencyProperty CanUserAddRowsProperty;
        public static readonly System.Windows.DependencyProperty CanUserDeleteRowsProperty;
        public static readonly System.Windows.DependencyProperty CanUserReorderColumnsProperty;
        public static readonly System.Windows.DependencyProperty CanUserResizeColumnsProperty;
        public static readonly System.Windows.DependencyProperty CanUserResizeRowsProperty;
        public static readonly System.Windows.DependencyProperty CanUserSortColumnsProperty;
        public static readonly System.Windows.DependencyProperty CellsPanelHorizontalOffsetProperty;
        public static readonly System.Windows.DependencyProperty CellStyleProperty;
        public static readonly System.Windows.DependencyProperty ClipboardCopyModeProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderHeightProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderStyleProperty;
        public static readonly System.Windows.DependencyProperty ColumnWidthProperty;
        public static readonly System.Windows.Input.RoutedCommand CommitEditCommand;
        public static readonly System.Windows.DependencyProperty CurrentCellProperty;
        public static readonly System.Windows.DependencyProperty CurrentColumnProperty;
        public static readonly System.Windows.DependencyProperty CurrentItemProperty;
        public static readonly System.Windows.DependencyProperty DragIndicatorStyleProperty;
        public static readonly System.Windows.DependencyProperty DropLocationIndicatorStyleProperty;
        public static readonly System.Windows.DependencyProperty EnableColumnVirtualizationProperty;
        public static readonly System.Windows.DependencyProperty EnableRowVirtualizationProperty;
        public static readonly System.Windows.DependencyProperty FrozenColumnCountProperty;
        public static readonly System.Windows.DependencyProperty GridLinesVisibilityProperty;
        public static readonly System.Windows.DependencyProperty HeadersVisibilityProperty;
        public static readonly System.Windows.DependencyProperty HorizontalGridLinesBrushProperty;
        public static readonly System.Windows.DependencyProperty HorizontalScrollBarVisibilityProperty;
        public static readonly System.Windows.DependencyProperty IsReadOnlyProperty;
        public static readonly System.Windows.DependencyProperty MaxColumnWidthProperty;
        public static readonly System.Windows.DependencyProperty MinColumnWidthProperty;
        public static readonly System.Windows.DependencyProperty MinRowHeightProperty;
        public static readonly System.Windows.DependencyProperty NewItemMarginProperty;
        public static readonly System.Windows.DependencyProperty NonFrozenColumnsViewportHorizontalOffsetProperty;
        public static readonly System.Windows.DependencyProperty RowBackgroundProperty;
        public static readonly System.Windows.DependencyProperty RowDetailsTemplateProperty;
        public static readonly System.Windows.DependencyProperty RowDetailsTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty RowDetailsVisibilityModeProperty;
        public static readonly System.Windows.DependencyProperty RowHeaderActualWidthProperty;
        public static readonly System.Windows.DependencyProperty RowHeaderStyleProperty;
        public static readonly System.Windows.DependencyProperty RowHeaderTemplateProperty;
        public static readonly System.Windows.DependencyProperty RowHeaderTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty RowHeaderWidthProperty;
        public static readonly System.Windows.DependencyProperty RowHeightProperty;
        public static readonly System.Windows.DependencyProperty RowStyleProperty;
        public static readonly System.Windows.DependencyProperty RowStyleSelectorProperty;
        public static readonly System.Windows.DependencyProperty RowValidationErrorTemplateProperty;
        public static readonly System.Windows.DependencyProperty SelectionModeProperty;
        public static readonly System.Windows.DependencyProperty SelectionUnitProperty;
        public static readonly System.Windows.DependencyProperty VerticalGridLinesBrushProperty;
        public static readonly System.Windows.DependencyProperty VerticalScrollBarVisibilityProperty;
        public DataGrid() { }
        public System.Windows.Media.Brush AlternatingRowBackground { get { throw null; } set { } }
        public bool AreRowDetailsFrozen { get { throw null; } set { } }
        public bool AutoGenerateColumns { get { throw null; } set { } }
        public bool CanUserAddRows { get { throw null; } set { } }
        public bool CanUserDeleteRows { get { throw null; } set { } }
        public bool CanUserReorderColumns { get { throw null; } set { } }
        public bool CanUserResizeColumns { get { throw null; } set { } }
        public bool CanUserResizeRows { get { throw null; } set { } }
        public bool CanUserSortColumns { get { throw null; } set { } }
        public double CellsPanelHorizontalOffset { get { throw null; } }
        public System.Windows.Style CellStyle { get { throw null; } set { } }
        public System.Windows.Controls.DataGridClipboardCopyMode ClipboardCopyMode { get { throw null; } set { } }
        public double ColumnHeaderHeight { get { throw null; } set { } }
        public System.Windows.Style ColumnHeaderStyle { get { throw null; } set { } }
        public System.Collections.ObjectModel.ObservableCollection<System.Windows.Controls.DataGridColumn> Columns { get { throw null; } }
        public System.Windows.Controls.DataGridLength ColumnWidth { get { throw null; } set { } }
        public System.Windows.Controls.DataGridCellInfo CurrentCell { get { throw null; } set { } }
        public System.Windows.Controls.DataGridColumn CurrentColumn { get { throw null; } set { } }
        public object CurrentItem { get { throw null; } set { } }
        public static System.Windows.Input.RoutedUICommand DeleteCommand { get { throw null; } }
        public System.Windows.Style DragIndicatorStyle { get { throw null; } set { } }
        public System.Windows.Style DropLocationIndicatorStyle { get { throw null; } set { } }
        public bool EnableColumnVirtualization { get { throw null; } set { } }
        public bool EnableRowVirtualization { get { throw null; } set { } }
        public static System.Windows.ComponentResourceKey FocusBorderBrushKey { get { throw null; } }
        public int FrozenColumnCount { get { throw null; } set { } }
        public System.Windows.Controls.DataGridGridLinesVisibility GridLinesVisibility { get { throw null; } set { } }
        protected internal override bool HandlesScrolling { get { throw null; } }
        public System.Windows.Controls.DataGridHeadersVisibility HeadersVisibility { get { throw null; } set { } }
        public static System.Windows.Data.IValueConverter HeadersVisibilityConverter { get { throw null; } }
        public System.Windows.Media.Brush HorizontalGridLinesBrush { get { throw null; } set { } }
        public System.Windows.Controls.ScrollBarVisibility HorizontalScrollBarVisibility { get { throw null; } set { } }
        public bool IsReadOnly { get { throw null; } set { } }
        public double MaxColumnWidth { get { throw null; } set { } }
        public double MinColumnWidth { get { throw null; } set { } }
        public double MinRowHeight { get { throw null; } set { } }
        public System.Windows.Thickness NewItemMargin { get { throw null; } }
        public double NonFrozenColumnsViewportHorizontalOffset { get { throw null; } }
        public System.Windows.Media.Brush RowBackground { get { throw null; } set { } }
        public static System.Windows.Data.IValueConverter RowDetailsScrollingConverter { get { throw null; } }
        public System.Windows.DataTemplate RowDetailsTemplate { get { throw null; } set { } }
        public System.Windows.Controls.DataTemplateSelector RowDetailsTemplateSelector { get { throw null; } set { } }
        public System.Windows.Controls.DataGridRowDetailsVisibilityMode RowDetailsVisibilityMode { get { throw null; } set { } }
        public double RowHeaderActualWidth { get { throw null; } }
        public System.Windows.Style RowHeaderStyle { get { throw null; } set { } }
        public System.Windows.DataTemplate RowHeaderTemplate { get { throw null; } set { } }
        public System.Windows.Controls.DataTemplateSelector RowHeaderTemplateSelector { get { throw null; } set { } }
        public double RowHeaderWidth { get { throw null; } set { } }
        public double RowHeight { get { throw null; } set { } }
        public System.Windows.Style RowStyle { get { throw null; } set { } }
        public System.Windows.Controls.StyleSelector RowStyleSelector { get { throw null; } set { } }
        public System.Windows.Controls.ControlTemplate RowValidationErrorTemplate { get { throw null; } set { } }
        public System.Collections.ObjectModel.ObservableCollection<System.Windows.Controls.ValidationRule> RowValidationRules { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectAllCommand { get { throw null; } }
        public System.Collections.Generic.IList<System.Windows.Controls.DataGridCellInfo> SelectedCells { get { throw null; } }
        public System.Windows.Controls.DataGridSelectionMode SelectionMode { get { throw null; } set { } }
        public System.Windows.Controls.DataGridSelectionUnit SelectionUnit { get { throw null; } set { } }
        public System.Windows.Media.Brush VerticalGridLinesBrush { get { throw null; } set { } }
        public System.Windows.Controls.ScrollBarVisibility VerticalScrollBarVisibility { get { throw null; } set { } }
        public event System.EventHandler<System.Windows.Controls.AddingNewItemEventArgs> AddingNewItem { add { } remove { } }
        public event System.EventHandler AutoGeneratedColumns { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridAutoGeneratingColumnEventArgs> AutoGeneratingColumn { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridBeginningEditEventArgs> BeginningEdit { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridCellEditEndingEventArgs> CellEditEnding { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridColumnEventArgs> ColumnDisplayIndexChanged { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.Primitives.DragCompletedEventArgs> ColumnHeaderDragCompleted { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.Primitives.DragDeltaEventArgs> ColumnHeaderDragDelta { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.Primitives.DragStartedEventArgs> ColumnHeaderDragStarted { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridColumnEventArgs> ColumnReordered { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridColumnReorderingEventArgs> ColumnReordering { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridRowClipboardEventArgs> CopyingRowClipboardContent { add { } remove { } }
        public event System.EventHandler<System.EventArgs> CurrentCellChanged { add { } remove { } }
        public event System.Windows.Controls.InitializingNewItemEventHandler InitializingNewItem { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridRowEventArgs> LoadingRow { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridRowDetailsEventArgs> LoadingRowDetails { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridPreparingCellForEditEventArgs> PreparingCellForEdit { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridRowDetailsEventArgs> RowDetailsVisibilityChanged { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridRowEditEndingEventArgs> RowEditEnding { add { } remove { } }
        public event System.Windows.Controls.SelectedCellsChangedEventHandler SelectedCellsChanged { add { } remove { } }
        public event System.Windows.Controls.DataGridSortingEventHandler Sorting { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridRowEventArgs> UnloadingRow { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridRowDetailsEventArgs> UnloadingRowDetails { add { } remove { } }
        public bool BeginEdit() { throw null; }
        public bool BeginEdit(System.Windows.RoutedEventArgs editingEventArgs) { throw null; }
        public bool CancelEdit() { throw null; }
        public bool CancelEdit(System.Windows.Controls.DataGridEditingUnit editingUnit) { throw null; }
        protected override void ClearContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        public void ClearDetailsVisibilityForItem(object item) { }
        public System.Windows.Controls.DataGridColumn ColumnFromDisplayIndex(int displayIndex) { throw null; }
        public bool CommitEdit() { throw null; }
        public bool CommitEdit(System.Windows.Controls.DataGridEditingUnit editingUnit, bool exitEditingMode) { throw null; }
        public static System.Collections.ObjectModel.Collection<System.Windows.Controls.DataGridColumn> GenerateColumns(System.ComponentModel.IItemProperties itemProperties) { throw null; }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        public System.Windows.Visibility GetDetailsVisibilityForItem(object item) { throw null; }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        protected virtual void OnAddingNewItem(System.Windows.Controls.AddingNewItemEventArgs e) { }
        public override void OnApplyTemplate() { }
        protected virtual void OnAutoGeneratedColumns(System.EventArgs e) { }
        protected virtual void OnAutoGeneratingColumn(System.Windows.Controls.DataGridAutoGeneratingColumnEventArgs e) { }
        protected virtual void OnBeginningEdit(System.Windows.Controls.DataGridBeginningEditEventArgs e) { }
        protected virtual void OnCanExecuteBeginEdit(System.Windows.Input.CanExecuteRoutedEventArgs e) { }
        protected virtual void OnCanExecuteCancelEdit(System.Windows.Input.CanExecuteRoutedEventArgs e) { }
        protected virtual void OnCanExecuteCommitEdit(System.Windows.Input.CanExecuteRoutedEventArgs e) { }
        protected virtual void OnCanExecuteCopy(System.Windows.Input.CanExecuteRoutedEventArgs args) { }
        protected virtual void OnCanExecuteDelete(System.Windows.Input.CanExecuteRoutedEventArgs e) { }
        protected virtual void OnCellEditEnding(System.Windows.Controls.DataGridCellEditEndingEventArgs e) { }
        protected internal virtual void OnColumnDisplayIndexChanged(System.Windows.Controls.DataGridColumnEventArgs e) { }
        protected internal virtual void OnColumnHeaderDragCompleted(System.Windows.Controls.Primitives.DragCompletedEventArgs e) { }
        protected internal virtual void OnColumnHeaderDragDelta(System.Windows.Controls.Primitives.DragDeltaEventArgs e) { }
        protected internal virtual void OnColumnHeaderDragStarted(System.Windows.Controls.Primitives.DragStartedEventArgs e) { }
        protected internal virtual void OnColumnReordered(System.Windows.Controls.DataGridColumnEventArgs e) { }
        protected internal virtual void OnColumnReordering(System.Windows.Controls.DataGridColumnReorderingEventArgs e) { }
        protected override void OnContextMenuOpening(System.Windows.Controls.ContextMenuEventArgs e) { }
        protected virtual void OnCopyingRowClipboardContent(System.Windows.Controls.DataGridRowClipboardEventArgs args) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnCurrentCellChanged(System.EventArgs e) { }
        protected virtual void OnExecutedBeginEdit(System.Windows.Input.ExecutedRoutedEventArgs e) { }
        protected virtual void OnExecutedCancelEdit(System.Windows.Input.ExecutedRoutedEventArgs e) { }
        protected virtual void OnExecutedCommitEdit(System.Windows.Input.ExecutedRoutedEventArgs e) { }
        protected virtual void OnExecutedCopy(System.Windows.Input.ExecutedRoutedEventArgs args) { }
        protected virtual void OnExecutedDelete(System.Windows.Input.ExecutedRoutedEventArgs e) { }
        protected virtual void OnInitializingNewItem(System.Windows.Controls.InitializingNewItemEventArgs e) { }
        protected override void OnIsMouseCapturedChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected override void OnItemsSourceChanged(System.Collections.IEnumerable oldValue, System.Collections.IEnumerable newValue) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected virtual void OnLoadingRow(System.Windows.Controls.DataGridRowEventArgs e) { }
        protected virtual void OnLoadingRowDetails(System.Windows.Controls.DataGridRowDetailsEventArgs e) { }
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) { }
        protected internal virtual void OnPreparingCellForEdit(System.Windows.Controls.DataGridPreparingCellForEditEventArgs e) { }
        protected internal virtual void OnRowDetailsVisibilityChanged(System.Windows.Controls.DataGridRowDetailsEventArgs e) { }
        protected virtual void OnRowEditEnding(System.Windows.Controls.DataGridRowEditEndingEventArgs e) { }
        protected virtual void OnSelectedCellsChanged(System.Windows.Controls.SelectedCellsChangedEventArgs e) { }
        protected override void OnSelectionChanged(System.Windows.Controls.SelectionChangedEventArgs e) { }
        protected virtual void OnSorting(System.Windows.Controls.DataGridSortingEventArgs eventArgs) { }
        protected override void OnTemplateChanged(System.Windows.Controls.ControlTemplate oldTemplate, System.Windows.Controls.ControlTemplate newTemplate) { }
        protected override void OnTextInput(System.Windows.Input.TextCompositionEventArgs e) { }
        protected virtual void OnUnloadingRow(System.Windows.Controls.DataGridRowEventArgs e) { }
        protected virtual void OnUnloadingRowDetails(System.Windows.Controls.DataGridRowDetailsEventArgs e) { }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        public void ScrollIntoView(object item) { }
        public void ScrollIntoView(object item, System.Windows.Controls.DataGridColumn column) { }
        public void SelectAllCells() { }
        public void SetDetailsVisibilityForItem(object item, System.Windows.Visibility detailsVisibility) { }
        public void UnselectAllCells() { }
    }
    public partial class DataGridAutoGeneratingColumnEventArgs : System.EventArgs
    {
        public DataGridAutoGeneratingColumnEventArgs(string propertyName, System.Type propertyType, System.Windows.Controls.DataGridColumn column) { }
        public bool Cancel { get { throw null; } set { } }
        public System.Windows.Controls.DataGridColumn Column { get { throw null; } set { } }
        public object PropertyDescriptor { get { throw null; } }
        public string PropertyName { get { throw null; } }
        public System.Type PropertyType { get { throw null; } }
    }
    public partial class DataGridBeginningEditEventArgs : System.EventArgs
    {
        public DataGridBeginningEditEventArgs(System.Windows.Controls.DataGridColumn column, System.Windows.Controls.DataGridRow row, System.Windows.RoutedEventArgs editingEventArgs) { }
        public bool Cancel { get { throw null; } set { } }
        public System.Windows.Controls.DataGridColumn Column { get { throw null; } }
        public System.Windows.RoutedEventArgs EditingEventArgs { get { throw null; } }
        public System.Windows.Controls.DataGridRow Row { get { throw null; } }
    }
    public abstract partial class DataGridBoundColumn : System.Windows.Controls.DataGridColumn
    {
        public static readonly System.Windows.DependencyProperty EditingElementStyleProperty;
        public static readonly System.Windows.DependencyProperty ElementStyleProperty;
        protected DataGridBoundColumn() { }
        public virtual System.Windows.Data.BindingBase Binding { get { throw null; } set { } }
        public override System.Windows.Data.BindingBase ClipboardContentBinding { get { throw null; } set { } }
        public System.Windows.Style EditingElementStyle { get { throw null; } set { } }
        public System.Windows.Style ElementStyle { get { throw null; } set { } }
        protected virtual void OnBindingChanged(System.Windows.Data.BindingBase oldBinding, System.Windows.Data.BindingBase newBinding) { }
        protected override bool OnCoerceIsReadOnly(bool baseValue) { throw null; }
        protected internal override void RefreshCellContent(System.Windows.FrameworkElement element, string propertyName) { }
    }
    public partial class DataGridCell : System.Windows.Controls.ContentControl
    {
        public static readonly System.Windows.DependencyProperty ColumnProperty;
        public static readonly System.Windows.DependencyProperty IsEditingProperty;
        public static readonly System.Windows.DependencyProperty IsReadOnlyProperty;
        public static readonly System.Windows.DependencyProperty IsSelectedProperty;
        public static readonly System.Windows.RoutedEvent SelectedEvent;
        public static readonly System.Windows.RoutedEvent UnselectedEvent;
        public DataGridCell() { }
        public System.Windows.Controls.DataGridColumn Column { get { throw null; } }
        public bool IsEditing { get { throw null; } set { } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSelected { get { throw null; } set { } }
        public event System.Windows.RoutedEventHandler Selected { add { } remove { } }
        public event System.Windows.RoutedEventHandler Unselected { add { } remove { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected virtual void OnColumnChanged(System.Windows.Controls.DataGridColumn oldColumn, System.Windows.Controls.DataGridColumn newColumn) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnIsEditingChanged(bool isEditing) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) { }
        protected virtual void OnSelected(System.Windows.RoutedEventArgs e) { }
        protected override void OnTextInput(System.Windows.Input.TextCompositionEventArgs e) { }
        protected virtual void OnUnselected(System.Windows.RoutedEventArgs e) { }
    }
    public partial class DataGridCellClipboardEventArgs : System.EventArgs
    {
        public DataGridCellClipboardEventArgs(object item, System.Windows.Controls.DataGridColumn column, object content) { }
        public System.Windows.Controls.DataGridColumn Column { get { throw null; } }
        public object Content { get { throw null; } set { } }
        public object Item { get { throw null; } }
    }
    public partial class DataGridCellEditEndingEventArgs : System.EventArgs
    {
        public DataGridCellEditEndingEventArgs(System.Windows.Controls.DataGridColumn column, System.Windows.Controls.DataGridRow row, System.Windows.FrameworkElement editingElement, System.Windows.Controls.DataGridEditAction editAction) { }
        public bool Cancel { get { throw null; } set { } }
        public System.Windows.Controls.DataGridColumn Column { get { throw null; } }
        public System.Windows.Controls.DataGridEditAction EditAction { get { throw null; } }
        public System.Windows.FrameworkElement EditingElement { get { throw null; } }
        public System.Windows.Controls.DataGridRow Row { get { throw null; } }
    }
    public partial struct DataGridCellInfo
    {
        public DataGridCellInfo(object item, System.Windows.Controls.DataGridColumn column) { throw null; }
        public DataGridCellInfo(System.Windows.Controls.DataGridCell cell) { throw null; }
        public System.Windows.Controls.DataGridColumn Column { get { throw null; } }
        public bool IsValid { get { throw null; } }
        public object Item { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Controls.DataGridCellInfo cell1, System.Windows.Controls.DataGridCellInfo cell2) { throw null; }
        public static bool operator !=(System.Windows.Controls.DataGridCellInfo cell1, System.Windows.Controls.DataGridCellInfo cell2) { throw null; }
    }
    public partial class DataGridCellsPanel : System.Windows.Controls.VirtualizingPanel
    {
        public DataGridCellsPanel() { }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected internal override void BringIndexIntoView(int index) { }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override void OnClearChildren() { }
        protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost) { }
        protected override void OnItemsChanged(object sender, System.Windows.Controls.Primitives.ItemsChangedEventArgs args) { }
    }
    public partial class DataGridCheckBoxColumn : System.Windows.Controls.DataGridBoundColumn
    {
        public static readonly System.Windows.DependencyProperty IsThreeStateProperty;
        public DataGridCheckBoxColumn() { }
        public static System.Windows.Style DefaultEditingElementStyle { get { throw null; } }
        public static System.Windows.Style DefaultElementStyle { get { throw null; } }
        public bool IsThreeState { get { throw null; } set { } }
        protected override System.Windows.FrameworkElement GenerateEditingElement(System.Windows.Controls.DataGridCell cell, object dataItem) { throw null; }
        protected override System.Windows.FrameworkElement GenerateElement(System.Windows.Controls.DataGridCell cell, object dataItem) { throw null; }
        protected override object PrepareCellForEdit(System.Windows.FrameworkElement editingElement, System.Windows.RoutedEventArgs editingEventArgs) { throw null; }
        protected internal override void RefreshCellContent(System.Windows.FrameworkElement element, string propertyName) { }
    }
    public partial struct DataGridClipboardCellContent
    {
        public DataGridClipboardCellContent(object item, System.Windows.Controls.DataGridColumn column, object content) { throw null; }
        public System.Windows.Controls.DataGridColumn Column { get { throw null; } }
        public object Content { get { throw null; } }
        public object Item { get { throw null; } }
        public override bool Equals(object data) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Controls.DataGridClipboardCellContent clipboardCellContent1, System.Windows.Controls.DataGridClipboardCellContent clipboardCellContent2) { throw null; }
        public static bool operator !=(System.Windows.Controls.DataGridClipboardCellContent clipboardCellContent1, System.Windows.Controls.DataGridClipboardCellContent clipboardCellContent2) { throw null; }
    }
    public enum DataGridClipboardCopyMode
    {
        None = 0,
        ExcludeHeader = 1,
        IncludeHeader = 2,
    }
    public abstract partial class DataGridColumn : System.Windows.DependencyObject
    {
        public static readonly System.Windows.DependencyProperty ActualWidthProperty;
        public static readonly System.Windows.DependencyProperty CanUserReorderProperty;
        public static readonly System.Windows.DependencyProperty CanUserResizeProperty;
        public static readonly System.Windows.DependencyProperty CanUserSortProperty;
        public static readonly System.Windows.DependencyProperty CellStyleProperty;
        public static readonly System.Windows.DependencyProperty DisplayIndexProperty;
        public static readonly System.Windows.DependencyProperty DragIndicatorStyleProperty;
        public static readonly System.Windows.DependencyProperty HeaderProperty;
        public static readonly System.Windows.DependencyProperty HeaderStringFormatProperty;
        public static readonly System.Windows.DependencyProperty HeaderStyleProperty;
        public static readonly System.Windows.DependencyProperty HeaderTemplateProperty;
        public static readonly System.Windows.DependencyProperty HeaderTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty IsAutoGeneratedProperty;
        public static readonly System.Windows.DependencyProperty IsFrozenProperty;
        public static readonly System.Windows.DependencyProperty IsReadOnlyProperty;
        public static readonly System.Windows.DependencyProperty MaxWidthProperty;
        public static readonly System.Windows.DependencyProperty MinWidthProperty;
        public static readonly System.Windows.DependencyProperty SortDirectionProperty;
        public static readonly System.Windows.DependencyProperty SortMemberPathProperty;
        public static readonly System.Windows.DependencyProperty VisibilityProperty;
        public static readonly System.Windows.DependencyProperty WidthProperty;
        protected DataGridColumn() { }
        public double ActualWidth { get { throw null; } }
        public bool CanUserReorder { get { throw null; } set { } }
        public bool CanUserResize { get { throw null; } set { } }
        public bool CanUserSort { get { throw null; } set { } }
        public System.Windows.Style CellStyle { get { throw null; } set { } }
        public virtual System.Windows.Data.BindingBase ClipboardContentBinding { get { throw null; } set { } }
        protected internal System.Windows.Controls.DataGrid DataGridOwner { get { throw null; } }
        public int DisplayIndex { get { throw null; } set { } }
        public System.Windows.Style DragIndicatorStyle { get { throw null; } set { } }
        public object Header { get { throw null; } set { } }
        public string HeaderStringFormat { get { throw null; } set { } }
        public System.Windows.Style HeaderStyle { get { throw null; } set { } }
        public System.Windows.DataTemplate HeaderTemplate { get { throw null; } set { } }
        public System.Windows.Controls.DataTemplateSelector HeaderTemplateSelector { get { throw null; } set { } }
        public bool IsAutoGenerated { get { throw null; } }
        public bool IsFrozen { get { throw null; } }
        public bool IsReadOnly { get { throw null; } set { } }
        public double MaxWidth { get { throw null; } set { } }
        public double MinWidth { get { throw null; } set { } }
        public System.ComponentModel.ListSortDirection? SortDirection { get { throw null; } set { } }
        public string SortMemberPath { get { throw null; } set { } }
        public System.Windows.Visibility Visibility { get { throw null; } set { } }
        public System.Windows.Controls.DataGridLength Width { get { throw null; } set { } }
        public event System.EventHandler<System.Windows.Controls.DataGridCellClipboardEventArgs> CopyingCellClipboardContent { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DataGridCellClipboardEventArgs> PastingCellClipboardContent { add { } remove { } }
        protected virtual void CancelCellEdit(System.Windows.FrameworkElement editingElement, object uneditedValue) { }
        protected virtual bool CommitCellEdit(System.Windows.FrameworkElement editingElement) { throw null; }
        protected abstract System.Windows.FrameworkElement GenerateEditingElement(System.Windows.Controls.DataGridCell cell, object dataItem);
        protected abstract System.Windows.FrameworkElement GenerateElement(System.Windows.Controls.DataGridCell cell, object dataItem);
        public System.Windows.FrameworkElement GetCellContent(object dataItem) { throw null; }
        public System.Windows.FrameworkElement GetCellContent(System.Windows.Controls.DataGridRow dataGridRow) { throw null; }
        protected void NotifyPropertyChanged(string propertyName) { }
        protected virtual bool OnCoerceIsReadOnly(bool baseValue) { throw null; }
        public virtual object OnCopyingCellClipboardContent(object item) { throw null; }
        public virtual void OnPastingCellClipboardContent(object item, object cellContent) { }
        protected virtual object PrepareCellForEdit(System.Windows.FrameworkElement editingElement, System.Windows.RoutedEventArgs editingEventArgs) { throw null; }
        protected internal virtual void RefreshCellContent(System.Windows.FrameworkElement element, string propertyName) { }
    }
    public partial class DataGridColumnEventArgs : System.EventArgs
    {
        public DataGridColumnEventArgs(System.Windows.Controls.DataGridColumn column) { }
        public System.Windows.Controls.DataGridColumn Column { get { throw null; } }
    }
    public partial class DataGridColumnReorderingEventArgs : System.Windows.Controls.DataGridColumnEventArgs
    {
        public DataGridColumnReorderingEventArgs(System.Windows.Controls.DataGridColumn dataGridColumn) : base (default(System.Windows.Controls.DataGridColumn)) { }
        public bool Cancel { get { throw null; } set { } }
        public System.Windows.Controls.Control DragIndicator { get { throw null; } set { } }
        public System.Windows.Controls.Control DropLocationIndicator { get { throw null; } set { } }
    }
    public partial class DataGridComboBoxColumn : System.Windows.Controls.DataGridColumn
    {
        public static readonly System.Windows.DependencyProperty DisplayMemberPathProperty;
        public static readonly System.Windows.DependencyProperty EditingElementStyleProperty;
        public static readonly System.Windows.DependencyProperty ElementStyleProperty;
        public static readonly System.Windows.DependencyProperty ItemsSourceProperty;
        public static readonly System.Windows.DependencyProperty SelectedValuePathProperty;
        public DataGridComboBoxColumn() { }
        public override System.Windows.Data.BindingBase ClipboardContentBinding { get { throw null; } set { } }
        public static System.Windows.Style DefaultEditingElementStyle { get { throw null; } }
        public static System.Windows.Style DefaultElementStyle { get { throw null; } }
        public string DisplayMemberPath { get { throw null; } set { } }
        public System.Windows.Style EditingElementStyle { get { throw null; } set { } }
        public System.Windows.Style ElementStyle { get { throw null; } set { } }
        public System.Collections.IEnumerable ItemsSource { get { throw null; } set { } }
        public virtual System.Windows.Data.BindingBase SelectedItemBinding { get { throw null; } set { } }
        public virtual System.Windows.Data.BindingBase SelectedValueBinding { get { throw null; } set { } }
        public string SelectedValuePath { get { throw null; } set { } }
        public virtual System.Windows.Data.BindingBase TextBinding { get { throw null; } set { } }
        public static System.Windows.ComponentResourceKey TextBlockComboBoxStyleKey { get { throw null; } }
        protected override void CancelCellEdit(System.Windows.FrameworkElement editingElement, object uneditedValue) { }
        protected override bool CommitCellEdit(System.Windows.FrameworkElement editingElement) { throw null; }
        protected override System.Windows.FrameworkElement GenerateEditingElement(System.Windows.Controls.DataGridCell cell, object dataItem) { throw null; }
        protected override System.Windows.FrameworkElement GenerateElement(System.Windows.Controls.DataGridCell cell, object dataItem) { throw null; }
        protected override bool OnCoerceIsReadOnly(bool baseValue) { throw null; }
        protected virtual void OnSelectedItemBindingChanged(System.Windows.Data.BindingBase oldBinding, System.Windows.Data.BindingBase newBinding) { }
        protected virtual void OnSelectedValueBindingChanged(System.Windows.Data.BindingBase oldBinding, System.Windows.Data.BindingBase newBinding) { }
        protected virtual void OnTextBindingChanged(System.Windows.Data.BindingBase oldBinding, System.Windows.Data.BindingBase newBinding) { }
        protected override object PrepareCellForEdit(System.Windows.FrameworkElement editingElement, System.Windows.RoutedEventArgs editingEventArgs) { throw null; }
        protected internal override void RefreshCellContent(System.Windows.FrameworkElement element, string propertyName) { }
    }
    public enum DataGridEditAction
    {
        Cancel = 0,
        Commit = 1,
    }
    public enum DataGridEditingUnit
    {
        Cell = 0,
        Row = 1,
    }
    public enum DataGridGridLinesVisibility
    {
        All = 0,
        Horizontal = 1,
        None = 2,
        Vertical = 3,
    }
    [System.FlagsAttribute]
    public enum DataGridHeadersVisibility
    {
        None = 0,
        Column = 1,
        Row = 2,
        All = 3,
    }
    public partial class DataGridHyperlinkColumn : System.Windows.Controls.DataGridBoundColumn
    {
        public static readonly System.Windows.DependencyProperty TargetNameProperty;
        public DataGridHyperlinkColumn() { }
        public System.Windows.Data.BindingBase ContentBinding { get { throw null; } set { } }
        public static System.Windows.Style DefaultEditingElementStyle { get { throw null; } }
        public static System.Windows.Style DefaultElementStyle { get { throw null; } }
        public string TargetName { get { throw null; } set { } }
        protected override void CancelCellEdit(System.Windows.FrameworkElement editingElement, object uneditedValue) { }
        protected override bool CommitCellEdit(System.Windows.FrameworkElement editingElement) { throw null; }
        protected override System.Windows.FrameworkElement GenerateEditingElement(System.Windows.Controls.DataGridCell cell, object dataItem) { throw null; }
        protected override System.Windows.FrameworkElement GenerateElement(System.Windows.Controls.DataGridCell cell, object dataItem) { throw null; }
        protected virtual void OnContentBindingChanged(System.Windows.Data.BindingBase oldBinding, System.Windows.Data.BindingBase newBinding) { }
        protected override object PrepareCellForEdit(System.Windows.FrameworkElement editingElement, System.Windows.RoutedEventArgs editingEventArgs) { throw null; }
        protected internal override void RefreshCellContent(System.Windows.FrameworkElement element, string propertyName) { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Controls.DataGridLengthConverter))]
    public partial struct DataGridLength : System.IEquatable<System.Windows.Controls.DataGridLength>
    {
        public DataGridLength(double pixels) { throw null; }
        public DataGridLength(double value, System.Windows.Controls.DataGridLengthUnitType type) { throw null; }
        public DataGridLength(double value, System.Windows.Controls.DataGridLengthUnitType type, double desiredValue, double displayValue) { throw null; }
        public static System.Windows.Controls.DataGridLength Auto { get { throw null; } }
        public double DesiredValue { get { throw null; } }
        public double DisplayValue { get { throw null; } }
        public bool IsAbsolute { get { throw null; } }
        public bool IsAuto { get { throw null; } }
        public bool IsSizeToCells { get { throw null; } }
        public bool IsSizeToHeader { get { throw null; } }
        public bool IsStar { get { throw null; } }
        public static System.Windows.Controls.DataGridLength SizeToCells { get { throw null; } }
        public static System.Windows.Controls.DataGridLength SizeToHeader { get { throw null; } }
        public System.Windows.Controls.DataGridLengthUnitType UnitType { get { throw null; } }
        public double Value { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Windows.Controls.DataGridLength other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Controls.DataGridLength gl1, System.Windows.Controls.DataGridLength gl2) { throw null; }
        public static implicit operator System.Windows.Controls.DataGridLength (double value) { throw null; }
        public static bool operator !=(System.Windows.Controls.DataGridLength gl1, System.Windows.Controls.DataGridLength gl2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class DataGridLengthConverter : System.ComponentModel.TypeConverter
    {
        public DataGridLengthConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public enum DataGridLengthUnitType
    {
        Auto = 0,
        Pixel = 1,
        SizeToCells = 2,
        SizeToHeader = 3,
        Star = 4,
    }
    public partial class DataGridPreparingCellForEditEventArgs : System.EventArgs
    {
        public DataGridPreparingCellForEditEventArgs(System.Windows.Controls.DataGridColumn column, System.Windows.Controls.DataGridRow row, System.Windows.RoutedEventArgs editingEventArgs, System.Windows.FrameworkElement editingElement) { }
        public System.Windows.Controls.DataGridColumn Column { get { throw null; } }
        public System.Windows.FrameworkElement EditingElement { get { throw null; } }
        public System.Windows.RoutedEventArgs EditingEventArgs { get { throw null; } }
        public System.Windows.Controls.DataGridRow Row { get { throw null; } }
    }
    public partial class DataGridRow : System.Windows.Controls.Control
    {
        public static readonly System.Windows.DependencyProperty AlternationIndexProperty;
        public static readonly System.Windows.DependencyProperty DetailsTemplateProperty;
        public static readonly System.Windows.DependencyProperty DetailsTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty DetailsVisibilityProperty;
        public static readonly System.Windows.DependencyProperty HeaderProperty;
        public static readonly System.Windows.DependencyProperty HeaderStyleProperty;
        public static readonly System.Windows.DependencyProperty HeaderTemplateProperty;
        public static readonly System.Windows.DependencyProperty HeaderTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty IsEditingProperty;
        public static readonly System.Windows.DependencyProperty IsNewItemProperty;
        public static readonly System.Windows.DependencyProperty IsSelectedProperty;
        public static readonly System.Windows.DependencyProperty ItemProperty;
        public static readonly System.Windows.DependencyProperty ItemsPanelProperty;
        public static readonly System.Windows.RoutedEvent SelectedEvent;
        public static readonly System.Windows.RoutedEvent UnselectedEvent;
        public static readonly System.Windows.DependencyProperty ValidationErrorTemplateProperty;
        public DataGridRow() { }
        public int AlternationIndex { get { throw null; } }
        public System.Windows.DataTemplate DetailsTemplate { get { throw null; } set { } }
        public System.Windows.Controls.DataTemplateSelector DetailsTemplateSelector { get { throw null; } set { } }
        public System.Windows.Visibility DetailsVisibility { get { throw null; } set { } }
        public object Header { get { throw null; } set { } }
        public System.Windows.Style HeaderStyle { get { throw null; } set { } }
        public System.Windows.DataTemplate HeaderTemplate { get { throw null; } set { } }
        public System.Windows.Controls.DataTemplateSelector HeaderTemplateSelector { get { throw null; } set { } }
        public bool IsEditing { get { throw null; } }
        public bool IsNewItem { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsSelected { get { throw null; } set { } }
        public object Item { get { throw null; } set { } }
        public System.Windows.Controls.ItemsPanelTemplate ItemsPanel { get { throw null; } set { } }
        public System.Windows.Controls.ControlTemplate ValidationErrorTemplate { get { throw null; } set { } }
        public event System.Windows.RoutedEventHandler Selected { add { } remove { } }
        public event System.Windows.RoutedEventHandler Unselected { add { } remove { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeBounds) { throw null; }
        public int GetIndex() { throw null; }
        public static System.Windows.Controls.DataGridRow GetRowContainingElement(System.Windows.FrameworkElement element) { throw null; }
        protected internal virtual void OnColumnsChanged(System.Collections.ObjectModel.ObservableCollection<System.Windows.Controls.DataGridColumn> columns, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnHeaderChanged(object oldHeader, object newHeader) { }
        protected virtual void OnItemChanged(object oldItem, object newItem) { }
        protected override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected virtual void OnSelected(System.Windows.RoutedEventArgs e) { }
        protected override void OnTemplateChanged(System.Windows.Controls.ControlTemplate oldTemplate, System.Windows.Controls.ControlTemplate newTemplate) { }
        protected virtual void OnUnselected(System.Windows.RoutedEventArgs e) { }
    }
    public partial class DataGridRowClipboardEventArgs : System.EventArgs
    {
        public DataGridRowClipboardEventArgs(object item, int startColumnDisplayIndex, int endColumnDisplayIndex, bool isColumnHeadersRow) { }
        public System.Collections.Generic.List<System.Windows.Controls.DataGridClipboardCellContent> ClipboardRowContent { get { throw null; } }
        public int EndColumnDisplayIndex { get { throw null; } }
        public bool IsColumnHeadersRow { get { throw null; } }
        public object Item { get { throw null; } }
        public int StartColumnDisplayIndex { get { throw null; } }
        public string FormatClipboardCellValues(string format) { throw null; }
    }
    public partial class DataGridRowDetailsEventArgs : System.EventArgs
    {
        public DataGridRowDetailsEventArgs(System.Windows.Controls.DataGridRow row, System.Windows.FrameworkElement detailsElement) { }
        public System.Windows.FrameworkElement DetailsElement { get { throw null; } }
        public System.Windows.Controls.DataGridRow Row { get { throw null; } }
    }
    public enum DataGridRowDetailsVisibilityMode
    {
        Collapsed = 0,
        Visible = 1,
        VisibleWhenSelected = 2,
    }
    public partial class DataGridRowEditEndingEventArgs : System.EventArgs
    {
        public DataGridRowEditEndingEventArgs(System.Windows.Controls.DataGridRow row, System.Windows.Controls.DataGridEditAction editAction) { }
        public bool Cancel { get { throw null; } set { } }
        public System.Windows.Controls.DataGridEditAction EditAction { get { throw null; } }
        public System.Windows.Controls.DataGridRow Row { get { throw null; } }
    }
    public partial class DataGridRowEventArgs : System.EventArgs
    {
        public DataGridRowEventArgs(System.Windows.Controls.DataGridRow row) { }
        public System.Windows.Controls.DataGridRow Row { get { throw null; } }
    }
    public enum DataGridSelectionMode
    {
        Single = 0,
        Extended = 1,
    }
    public enum DataGridSelectionUnit
    {
        Cell = 0,
        FullRow = 1,
        CellOrRowHeader = 2,
    }
    public partial class DataGridSortingEventArgs : System.Windows.Controls.DataGridColumnEventArgs
    {
        public DataGridSortingEventArgs(System.Windows.Controls.DataGridColumn column) : base (default(System.Windows.Controls.DataGridColumn)) { }
        public bool Handled { get { throw null; } set { } }
    }
    public delegate void DataGridSortingEventHandler(object sender, System.Windows.Controls.DataGridSortingEventArgs e);
    public partial class DataGridTemplateColumn : System.Windows.Controls.DataGridColumn
    {
        public static readonly System.Windows.DependencyProperty CellEditingTemplateProperty;
        public static readonly System.Windows.DependencyProperty CellEditingTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty CellTemplateProperty;
        public static readonly System.Windows.DependencyProperty CellTemplateSelectorProperty;
        public DataGridTemplateColumn() { }
        public System.Windows.DataTemplate CellEditingTemplate { get { throw null; } set { } }
        public System.Windows.Controls.DataTemplateSelector CellEditingTemplateSelector { get { throw null; } set { } }
        public System.Windows.DataTemplate CellTemplate { get { throw null; } set { } }
        public System.Windows.Controls.DataTemplateSelector CellTemplateSelector { get { throw null; } set { } }
        protected override System.Windows.FrameworkElement GenerateEditingElement(System.Windows.Controls.DataGridCell cell, object dataItem) { throw null; }
        protected override System.Windows.FrameworkElement GenerateElement(System.Windows.Controls.DataGridCell cell, object dataItem) { throw null; }
        protected internal override void RefreshCellContent(System.Windows.FrameworkElement element, string propertyName) { }
    }
    public partial class DataGridTextColumn : System.Windows.Controls.DataGridBoundColumn
    {
        public static readonly System.Windows.DependencyProperty FontFamilyProperty;
        public static readonly System.Windows.DependencyProperty FontSizeProperty;
        public static readonly System.Windows.DependencyProperty FontStyleProperty;
        public static readonly System.Windows.DependencyProperty FontWeightProperty;
        public static readonly System.Windows.DependencyProperty ForegroundProperty;
        public DataGridTextColumn() { }
        public static System.Windows.Style DefaultEditingElementStyle { get { throw null; } }
        public static System.Windows.Style DefaultElementStyle { get { throw null; } }
        public System.Windows.Media.FontFamily FontFamily { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.FontSizeConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None)]
        public double FontSize { get { throw null; } set { } }
        public System.Windows.FontStyle FontStyle { get { throw null; } set { } }
        public System.Windows.FontWeight FontWeight { get { throw null; } set { } }
        public System.Windows.Media.Brush Foreground { get { throw null; } set { } }
        protected override void CancelCellEdit(System.Windows.FrameworkElement editingElement, object uneditedValue) { }
        protected override bool CommitCellEdit(System.Windows.FrameworkElement editingElement) { throw null; }
        protected override System.Windows.FrameworkElement GenerateEditingElement(System.Windows.Controls.DataGridCell cell, object dataItem) { throw null; }
        protected override System.Windows.FrameworkElement GenerateElement(System.Windows.Controls.DataGridCell cell, object dataItem) { throw null; }
        protected override object PrepareCellForEdit(System.Windows.FrameworkElement editingElement, System.Windows.RoutedEventArgs editingEventArgs) { throw null; }
        protected internal override void RefreshCellContent(System.Windows.FrameworkElement element, string propertyName) { }
    }
    public partial class DataTemplateSelector
    {
        public DataTemplateSelector() { }
        public virtual System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container) { throw null; }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_Button", Type=typeof(System.Windows.Controls.Button))]
    [System.Windows.TemplatePartAttribute(Name="PART_Popup", Type=typeof(System.Windows.Controls.Primitives.Popup))]
    [System.Windows.TemplatePartAttribute(Name="PART_Root", Type=typeof(System.Windows.Controls.Grid))]
    [System.Windows.TemplatePartAttribute(Name="PART_TextBox", Type=typeof(System.Windows.Controls.Primitives.DatePickerTextBox))]
    public partial class DatePicker : System.Windows.Controls.Control
    {
        public static readonly System.Windows.DependencyProperty CalendarStyleProperty;
        public static readonly System.Windows.DependencyProperty DisplayDateEndProperty;
        public static readonly System.Windows.DependencyProperty DisplayDateProperty;
        public static readonly System.Windows.DependencyProperty DisplayDateStartProperty;
        public static readonly System.Windows.DependencyProperty FirstDayOfWeekProperty;
        public static readonly System.Windows.DependencyProperty IsDropDownOpenProperty;
        public static readonly System.Windows.DependencyProperty IsTodayHighlightedProperty;
        public static readonly System.Windows.RoutedEvent SelectedDateChangedEvent;
        public static readonly System.Windows.DependencyProperty SelectedDateFormatProperty;
        public static readonly System.Windows.DependencyProperty SelectedDateProperty;
        public static readonly System.Windows.DependencyProperty TextProperty;
        public DatePicker() { }
        public System.Windows.Controls.CalendarBlackoutDatesCollection BlackoutDates { get { throw null; } }
        public System.Windows.Style CalendarStyle { get { throw null; } set { } }
        public System.DateTime DisplayDate { get { throw null; } set { } }
        public System.DateTime? DisplayDateEnd { get { throw null; } set { } }
        public System.DateTime? DisplayDateStart { get { throw null; } set { } }
        public System.DayOfWeek FirstDayOfWeek { get { throw null; } set { } }
        protected internal override bool HasEffectiveKeyboardFocus { get { throw null; } }
        public bool IsDropDownOpen { get { throw null; } set { } }
        public bool IsTodayHighlighted { get { throw null; } set { } }
        public System.DateTime? SelectedDate { get { throw null; } set { } }
        public System.Windows.Controls.DatePickerFormat SelectedDateFormat { get { throw null; } set { } }
        public string Text { get { throw null; } set { } }
        public event System.Windows.RoutedEventHandler CalendarClosed { add { } remove { } }
        public event System.Windows.RoutedEventHandler CalendarOpened { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.DatePickerDateValidationErrorEventArgs> DateValidationError { add { } remove { } }
        public event System.EventHandler<System.Windows.Controls.SelectionChangedEventArgs> SelectedDateChanged { add { } remove { } }
        public override void OnApplyTemplate() { }
        protected virtual void OnCalendarClosed(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnCalendarOpened(System.Windows.RoutedEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDateValidationError(System.Windows.Controls.DatePickerDateValidationErrorEventArgs e) { }
        protected virtual void OnSelectedDateChanged(System.Windows.Controls.SelectionChangedEventArgs e) { }
        public override string ToString() { throw null; }
    }
    public partial class DatePickerDateValidationErrorEventArgs : System.EventArgs
    {
        public DatePickerDateValidationErrorEventArgs(System.Exception exception, string text) { }
        public System.Exception Exception { get { throw null; } }
        public string Text { get { throw null; } }
        public bool ThrowException { get { throw null; } set { } }
    }
    public enum DatePickerFormat
    {
        Long = 0,
        Short = 1,
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore, Readability=System.Windows.Readability.Unreadable)]
    [System.Windows.Markup.ContentPropertyAttribute("Child")]
    public partial class Decorator : System.Windows.FrameworkElement, System.Windows.Markup.IAddChild
    {
        public Decorator() { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public virtual System.Windows.UIElement Child { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore)]
    public abstract partial class DefinitionBase : System.Windows.FrameworkContentElement
    {
        internal DefinitionBase() { }
        public static readonly System.Windows.DependencyProperty SharedSizeGroupProperty;
        public string SharedSizeGroup { get { throw null; } set { } }
    }
    public enum Dock
    {
        Left = 0,
        Top = 1,
        Right = 2,
        Bottom = 3,
    }
    public partial class DockPanel : System.Windows.Controls.Panel
    {
        public static readonly System.Windows.DependencyProperty DockProperty;
        public static readonly System.Windows.DependencyProperty LastChildFillProperty;
        public DockPanel() { }
        public bool LastChildFill { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static System.Windows.Controls.Dock GetDock(System.Windows.UIElement element) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        public static void SetDock(System.Windows.UIElement element, System.Windows.Controls.Dock dock) { }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_ContentHost", Type=typeof(System.Windows.Controls.ScrollViewer))]
    [System.Windows.TemplatePartAttribute(Name="PART_FindToolBarHost", Type=typeof(System.Windows.Controls.ContentControl))]
    public partial class DocumentViewer : System.Windows.Controls.Primitives.DocumentViewerBase
    {
        public static readonly System.Windows.DependencyProperty CanDecreaseZoomProperty;
        public static readonly System.Windows.DependencyProperty CanIncreaseZoomProperty;
        public static readonly System.Windows.DependencyProperty CanMoveDownProperty;
        public static readonly System.Windows.DependencyProperty CanMoveLeftProperty;
        public static readonly System.Windows.DependencyProperty CanMoveRightProperty;
        public static readonly System.Windows.DependencyProperty CanMoveUpProperty;
        public static readonly System.Windows.DependencyProperty ExtentHeightProperty;
        public static readonly System.Windows.DependencyProperty ExtentWidthProperty;
        public static readonly System.Windows.DependencyProperty HorizontalOffsetProperty;
        public static readonly System.Windows.DependencyProperty HorizontalPageSpacingProperty;
        public static readonly System.Windows.DependencyProperty MaxPagesAcrossProperty;
        public static readonly System.Windows.DependencyProperty ShowPageBordersProperty;
        public static readonly System.Windows.DependencyProperty VerticalOffsetProperty;
        public static readonly System.Windows.DependencyProperty VerticalPageSpacingProperty;
        public static readonly System.Windows.DependencyProperty ViewportHeightProperty;
        public static readonly System.Windows.DependencyProperty ViewportWidthProperty;
        public static readonly System.Windows.DependencyProperty ZoomProperty;
        public DocumentViewer() { }
        public bool CanDecreaseZoom { get { throw null; } }
        public bool CanIncreaseZoom { get { throw null; } }
        public bool CanMoveDown { get { throw null; } }
        public bool CanMoveLeft { get { throw null; } }
        public bool CanMoveRight { get { throw null; } }
        public bool CanMoveUp { get { throw null; } }
        public double ExtentHeight { get { throw null; } }
        public double ExtentWidth { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand FitToHeightCommand { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand FitToMaxPagesAcrossCommand { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand FitToWidthCommand { get { throw null; } }
        public double HorizontalOffset { get { throw null; } set { } }
        public double HorizontalPageSpacing { get { throw null; } set { } }
        public int MaxPagesAcross { get { throw null; } set { } }
        public bool ShowPageBorders { get { throw null; } set { } }
        public double VerticalOffset { get { throw null; } set { } }
        public double VerticalPageSpacing { get { throw null; } set { } }
        public double ViewportHeight { get { throw null; } }
        public double ViewportWidth { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ViewThumbnailsCommand { get { throw null; } }
        public double Zoom { get { throw null; } set { } }
        public void DecreaseZoom() { }
        public void Find() { }
        public void FitToHeight() { }
        public void FitToMaxPagesAcross() { }
        public void FitToMaxPagesAcross(int pagesAcross) { }
        public void FitToWidth() { }
        protected override System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Controls.Primitives.DocumentPageView> GetPageViewsCollection(out bool changed) { throw null; }
        public void IncreaseZoom() { }
        public void MoveDown() { }
        public void MoveLeft() { }
        public void MoveRight() { }
        public void MoveUp() { }
        public override void OnApplyTemplate() { }
        protected override void OnBringIntoView(System.Windows.DependencyObject element, System.Windows.Rect rect, int pageNumber) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDecreaseZoomCommand() { }
        protected override void OnDocumentChanged() { }
        protected virtual void OnFindCommand() { }
        protected override void OnFirstPageCommand() { }
        protected virtual void OnFitToHeightCommand() { }
        protected virtual void OnFitToMaxPagesAcrossCommand() { }
        protected virtual void OnFitToMaxPagesAcrossCommand(int pagesAcross) { }
        protected virtual void OnFitToWidthCommand() { }
        protected override void OnGoToPageCommand(int pageNumber) { }
        protected virtual void OnIncreaseZoomCommand() { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnLastPageCommand() { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected virtual void OnMoveDownCommand() { }
        protected virtual void OnMoveLeftCommand() { }
        protected virtual void OnMoveRightCommand() { }
        protected virtual void OnMoveUpCommand() { }
        protected override void OnNextPageCommand() { }
        protected override void OnPreviewMouseWheel(System.Windows.Input.MouseWheelEventArgs e) { }
        protected override void OnPreviousPageCommand() { }
        protected virtual void OnScrollPageDownCommand() { }
        protected virtual void OnScrollPageLeftCommand() { }
        protected virtual void OnScrollPageRightCommand() { }
        protected virtual void OnScrollPageUpCommand() { }
        protected virtual void OnViewThumbnailsCommand() { }
        public void ScrollPageDown() { }
        public void ScrollPageLeft() { }
        public void ScrollPageRight() { }
        public void ScrollPageUp() { }
        public void ViewThumbnails() { }
    }
    public sealed partial class ExceptionValidationRule : System.Windows.Controls.ValidationRule
    {
        public ExceptionValidationRule() { }
        public override System.Windows.Controls.ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo) { throw null; }
    }
    public enum ExpandDirection
    {
        Down = 0,
        Up = 1,
        Left = 2,
        Right = 3,
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public partial class Expander : System.Windows.Controls.HeaderedContentControl
    {
        public static readonly System.Windows.RoutedEvent CollapsedEvent;
        public static readonly System.Windows.DependencyProperty ExpandDirectionProperty;
        public static readonly System.Windows.RoutedEvent ExpandedEvent;
        public static readonly System.Windows.DependencyProperty IsExpandedProperty;
        public Expander() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public System.Windows.Controls.ExpandDirection ExpandDirection { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsExpanded { get { throw null; } set { } }
        public event System.Windows.RoutedEventHandler Collapsed { add { } remove { } }
        public event System.Windows.RoutedEventHandler Expanded { add { } remove { } }
        public override void OnApplyTemplate() { }
        protected virtual void OnCollapsed() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnExpanded() { }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_FindToolBarHost", Type=typeof(System.Windows.Controls.Decorator))]
    public partial class FlowDocumentPageViewer : System.Windows.Controls.Primitives.DocumentViewerBase
    {
        public static readonly System.Windows.DependencyProperty CanDecreaseZoomProperty;
        protected static readonly System.Windows.DependencyPropertyKey CanDecreaseZoomPropertyKey;
        public static readonly System.Windows.DependencyProperty CanIncreaseZoomProperty;
        protected static readonly System.Windows.DependencyPropertyKey CanIncreaseZoomPropertyKey;
        public static readonly System.Windows.DependencyProperty IsInactiveSelectionHighlightEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsSelectionActiveProperty;
        public static readonly System.Windows.DependencyProperty MaxZoomProperty;
        public static readonly System.Windows.DependencyProperty MinZoomProperty;
        public static readonly System.Windows.DependencyProperty SelectionBrushProperty;
        public static readonly System.Windows.DependencyProperty SelectionOpacityProperty;
        public static readonly System.Windows.DependencyProperty ZoomIncrementProperty;
        public static readonly System.Windows.DependencyProperty ZoomProperty;
        public FlowDocumentPageViewer() { }
        public virtual bool CanDecreaseZoom { get { throw null; } }
        public virtual bool CanIncreaseZoom { get { throw null; } }
        public bool IsInactiveSelectionHighlightEnabled { get { throw null; } set { } }
        public bool IsSelectionActive { get { throw null; } }
        public double MaxZoom { get { throw null; } set { } }
        public double MinZoom { get { throw null; } set { } }
        public System.Windows.Documents.TextSelection Selection { get { throw null; } }
        public System.Windows.Media.Brush SelectionBrush { get { throw null; } set { } }
        public double SelectionOpacity { get { throw null; } set { } }
        public double Zoom { get { throw null; } set { } }
        public double ZoomIncrement { get { throw null; } set { } }
        public void DecreaseZoom() { }
        public void Find() { }
        public void IncreaseZoom() { }
        public override void OnApplyTemplate() { }
        protected override void OnCancelPrintCommand() { }
        protected override void OnContextMenuOpening(System.Windows.Controls.ContextMenuEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDecreaseZoomCommand() { }
        protected override void OnDocumentChanged() { }
        protected virtual void OnFindCommand() { }
        protected override void OnFirstPageCommand() { }
        protected override void OnGoToPageCommand(int pageNumber) { }
        protected virtual void OnIncreaseZoomCommand() { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnLastPageCommand() { }
        protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e) { }
        protected override void OnNextPageCommand() { }
        protected override void OnPageViewsChanged() { }
        protected override void OnPreviousPageCommand() { }
        protected override void OnPrintCommand() { }
        protected virtual void OnPrintCompleted() { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Document")]
    [System.Windows.TemplatePartAttribute(Name="PART_ContentHost", Type=typeof(System.Windows.Controls.Decorator))]
    [System.Windows.TemplatePartAttribute(Name="PART_FindToolBarHost", Type=typeof(System.Windows.Controls.Decorator))]
    public partial class FlowDocumentReader : System.Windows.Controls.Control, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty CanDecreaseZoomProperty;
        public static readonly System.Windows.DependencyProperty CanGoToNextPageProperty;
        public static readonly System.Windows.DependencyProperty CanGoToPreviousPageProperty;
        public static readonly System.Windows.DependencyProperty CanIncreaseZoomProperty;
        public static readonly System.Windows.DependencyProperty DocumentProperty;
        public static readonly System.Windows.DependencyProperty IsFindEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsInactiveSelectionHighlightEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsPageViewEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsPrintEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsScrollViewEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsSelectionActiveProperty;
        public static readonly System.Windows.DependencyProperty IsTwoPageViewEnabledProperty;
        public static readonly System.Windows.DependencyProperty MaxZoomProperty;
        public static readonly System.Windows.DependencyProperty MinZoomProperty;
        public static readonly System.Windows.DependencyProperty PageCountProperty;
        public static readonly System.Windows.DependencyProperty PageNumberProperty;
        public static readonly System.Windows.DependencyProperty SelectionBrushProperty;
        public static readonly System.Windows.DependencyProperty SelectionOpacityProperty;
        public static readonly System.Windows.Input.RoutedUICommand SwitchViewingModeCommand;
        public static readonly System.Windows.DependencyProperty ViewingModeProperty;
        public static readonly System.Windows.DependencyProperty ZoomIncrementProperty;
        public static readonly System.Windows.DependencyProperty ZoomProperty;
        public FlowDocumentReader() { }
        public bool CanDecreaseZoom { get { throw null; } }
        public bool CanGoToNextPage { get { throw null; } }
        public bool CanGoToPreviousPage { get { throw null; } }
        public bool CanIncreaseZoom { get { throw null; } }
        public System.Windows.Documents.FlowDocument Document { get { throw null; } set { } }
        public bool IsFindEnabled { get { throw null; } set { } }
        public bool IsInactiveSelectionHighlightEnabled { get { throw null; } set { } }
        public bool IsPageViewEnabled { get { throw null; } set { } }
        public bool IsPrintEnabled { get { throw null; } set { } }
        public bool IsScrollViewEnabled { get { throw null; } set { } }
        public bool IsSelectionActive { get { throw null; } }
        public bool IsTwoPageViewEnabled { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public double MaxZoom { get { throw null; } set { } }
        public double MinZoom { get { throw null; } set { } }
        public int PageCount { get { throw null; } }
        public int PageNumber { get { throw null; } }
        public System.Windows.Documents.TextSelection Selection { get { throw null; } }
        public System.Windows.Media.Brush SelectionBrush { get { throw null; } set { } }
        public double SelectionOpacity { get { throw null; } set { } }
        public System.Windows.Controls.FlowDocumentReaderViewingMode ViewingMode { get { throw null; } set { } }
        public double Zoom { get { throw null; } set { } }
        public double ZoomIncrement { get { throw null; } set { } }
        public void CancelPrint() { }
        public bool CanGoToPage(int pageNumber) { throw null; }
        public void DecreaseZoom() { }
        public void Find() { }
        public void IncreaseZoom() { }
        public override void OnApplyTemplate() { }
        protected virtual void OnCancelPrintCommand() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDecreaseZoomCommand() { }
        protected override void OnDpiChanged(System.Windows.DpiScale oldDpiScaleInfo, System.Windows.DpiScale newDpiScaleInfo) { }
        protected virtual void OnFindCommand() { }
        protected virtual void OnIncreaseZoomCommand() { }
        protected override void OnInitialized(System.EventArgs e) { }
        protected override void OnIsKeyboardFocusWithinChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected virtual void OnPrintCommand() { }
        protected virtual void OnPrintCompleted() { }
        protected virtual void OnSwitchViewingModeCommand(System.Windows.Controls.FlowDocumentReaderViewingMode viewingMode) { }
        public void Print() { }
        public void SwitchViewingMode(System.Windows.Controls.FlowDocumentReaderViewingMode viewingMode) { }
        protected virtual void SwitchViewingModeCore(System.Windows.Controls.FlowDocumentReaderViewingMode viewingMode) { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public enum FlowDocumentReaderViewingMode
    {
        Page = 0,
        TwoPage = 1,
        Scroll = 2,
    }
    [System.Windows.Markup.ContentPropertyAttribute("Document")]
    [System.Windows.TemplatePartAttribute(Name="PART_ContentHost", Type=typeof(System.Windows.Controls.ScrollViewer))]
    [System.Windows.TemplatePartAttribute(Name="PART_FindToolBarHost", Type=typeof(System.Windows.Controls.Decorator))]
    [System.Windows.TemplatePartAttribute(Name="PART_ToolBarHost", Type=typeof(System.Windows.Controls.Decorator))]
    public partial class FlowDocumentScrollViewer : System.Windows.Controls.Control, System.IServiceProvider, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty CanDecreaseZoomProperty;
        public static readonly System.Windows.DependencyProperty CanIncreaseZoomProperty;
        public static readonly System.Windows.DependencyProperty DocumentProperty;
        public static readonly System.Windows.DependencyProperty HorizontalScrollBarVisibilityProperty;
        public static readonly System.Windows.DependencyProperty IsInactiveSelectionHighlightEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsSelectionActiveProperty;
        public static readonly System.Windows.DependencyProperty IsSelectionEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsToolBarVisibleProperty;
        public static readonly System.Windows.DependencyProperty MaxZoomProperty;
        public static readonly System.Windows.DependencyProperty MinZoomProperty;
        public static readonly System.Windows.DependencyProperty SelectionBrushProperty;
        public static readonly System.Windows.DependencyProperty SelectionOpacityProperty;
        public static readonly System.Windows.DependencyProperty VerticalScrollBarVisibilityProperty;
        public static readonly System.Windows.DependencyProperty ZoomIncrementProperty;
        public static readonly System.Windows.DependencyProperty ZoomProperty;
        public FlowDocumentScrollViewer() { }
        public bool CanDecreaseZoom { get { throw null; } }
        public bool CanIncreaseZoom { get { throw null; } }
        public System.Windows.Documents.FlowDocument Document { get { throw null; } set { } }
        public System.Windows.Controls.ScrollBarVisibility HorizontalScrollBarVisibility { get { throw null; } set { } }
        public bool IsInactiveSelectionHighlightEnabled { get { throw null; } set { } }
        public bool IsSelectionActive { get { throw null; } }
        public bool IsSelectionEnabled { get { throw null; } set { } }
        public bool IsToolBarVisible { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public double MaxZoom { get { throw null; } set { } }
        public double MinZoom { get { throw null; } set { } }
        public System.Windows.Documents.TextSelection Selection { get { throw null; } }
        public System.Windows.Media.Brush SelectionBrush { get { throw null; } set { } }
        public double SelectionOpacity { get { throw null; } set { } }
        public System.Windows.Controls.ScrollBarVisibility VerticalScrollBarVisibility { get { throw null; } set { } }
        public double Zoom { get { throw null; } set { } }
        public double ZoomIncrement { get { throw null; } set { } }
        public void CancelPrint() { }
        public void DecreaseZoom() { }
        public void Find() { }
        public void IncreaseZoom() { }
        public override void OnApplyTemplate() { }
        protected virtual void OnCancelPrintCommand() { }
        protected override void OnContextMenuOpening(System.Windows.Controls.ContextMenuEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDecreaseZoomCommand() { }
        protected virtual void OnFindCommand() { }
        protected virtual void OnIncreaseZoomCommand() { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e) { }
        protected virtual void OnPrintCommand() { }
        protected virtual void OnPrintCompleted() { }
        public void Print() { }
        object System.IServiceProvider.GetService(System.Type serviceType) { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.ComponentModel.DefaultEventAttribute("Navigated")]
    [System.ComponentModel.DefaultPropertyAttribute("Source")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore)]
    [System.Windows.Markup.ContentPropertyAttribute]
    [System.Windows.TemplatePartAttribute(Name="PART_FrameCP", Type=typeof(System.Windows.Controls.ContentPresenter))]
    public partial class Frame : System.Windows.Controls.ContentControl, System.Windows.Markup.IAddChild, System.Windows.Markup.IUriContext
    {
        public static readonly System.Windows.DependencyProperty BackStackProperty;
        public static readonly System.Windows.DependencyProperty CanGoBackProperty;
        public static readonly System.Windows.DependencyProperty CanGoForwardProperty;
        public static readonly System.Windows.DependencyProperty ForwardStackProperty;
        public static readonly System.Windows.DependencyProperty JournalOwnershipProperty;
        public static readonly System.Windows.DependencyProperty NavigationUIVisibilityProperty;
        public static readonly System.Windows.DependencyProperty SandboxExternalContentProperty;
        public static readonly System.Windows.DependencyProperty SourceProperty;
        public Frame() { }
        public System.Collections.IEnumerable BackStack { get { throw null; } }
        protected virtual System.Uri BaseUri { get { throw null; } set { } }
        public bool CanGoBack { get { throw null; } }
        public bool CanGoForward { get { throw null; } }
        public System.Uri CurrentSource { get { throw null; } }
        public System.Collections.IEnumerable ForwardStack { get { throw null; } }
        public System.Windows.Navigation.JournalOwnership JournalOwnership { get { throw null; } set { } }
        public System.Windows.Navigation.NavigationService NavigationService { get { throw null; } }
        public System.Windows.Navigation.NavigationUIVisibility NavigationUIVisibility { get { throw null; } set { } }
        public bool SandboxExternalContent { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public System.Uri Source { get { throw null; } set { } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        public event System.EventHandler ContentRendered { add { } remove { } }
        public event System.Windows.Navigation.FragmentNavigationEventHandler FragmentNavigation { add { } remove { } }
        public event System.Windows.Navigation.LoadCompletedEventHandler LoadCompleted { add { } remove { } }
        public event System.Windows.Navigation.NavigatedEventHandler Navigated { add { } remove { } }
        public event System.Windows.Navigation.NavigatingCancelEventHandler Navigating { add { } remove { } }
        public event System.Windows.Navigation.NavigationFailedEventHandler NavigationFailed { add { } remove { } }
        public event System.Windows.Navigation.NavigationProgressEventHandler NavigationProgress { add { } remove { } }
        public event System.Windows.Navigation.NavigationStoppedEventHandler NavigationStopped { add { } remove { } }
        public void AddBackEntry(System.Windows.Navigation.CustomContentState state) { }
        protected override void AddChild(object value) { }
        protected override void AddText(string text) { }
        public void GoBack() { }
        public void GoForward() { }
        public bool Navigate(object content) { throw null; }
        public bool Navigate(object content, object extraData) { throw null; }
        public bool Navigate(System.Uri source) { throw null; }
        public bool Navigate(System.Uri source, object extraData) { throw null; }
        public override void OnApplyTemplate() { }
        protected virtual void OnContentRendered(System.EventArgs args) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        public void Refresh() { }
        public System.Windows.Navigation.JournalEntry RemoveBackEntry() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool ShouldSerializeContent() { throw null; }
        public void StopLoading() { }
    }
    public partial class Grid : System.Windows.Controls.Panel, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty ColumnProperty;
        public static readonly System.Windows.DependencyProperty ColumnSpanProperty;
        public static readonly System.Windows.DependencyProperty IsSharedSizeScopeProperty;
        public static readonly System.Windows.DependencyProperty RowProperty;
        public static readonly System.Windows.DependencyProperty RowSpanProperty;
        public static readonly System.Windows.DependencyProperty ShowGridLinesProperty;
        public Grid() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Controls.ColumnDefinitionCollection ColumnDefinitions { get { throw null; } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Controls.RowDefinitionCollection RowDefinitions { get { throw null; } }
        public bool ShowGridLines { get { throw null; } set { } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static int GetColumn(System.Windows.UIElement element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static int GetColumnSpan(System.Windows.UIElement element) { throw null; }
        public static bool GetIsSharedSizeScope(System.Windows.UIElement element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static int GetRow(System.Windows.UIElement element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static int GetRowSpan(System.Windows.UIElement element) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected internal override void OnVisualChildrenChanged(System.Windows.DependencyObject visualAdded, System.Windows.DependencyObject visualRemoved) { }
        public static void SetColumn(System.Windows.UIElement element, int value) { }
        public static void SetColumnSpan(System.Windows.UIElement element, int value) { }
        public static void SetIsSharedSizeScope(System.Windows.UIElement element, bool value) { }
        public static void SetRow(System.Windows.UIElement element, int value) { }
        public static void SetRowSpan(System.Windows.UIElement element, int value) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeColumnDefinitions() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeRowDefinitions() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public enum GridResizeBehavior
    {
        BasedOnAlignment = 0,
        CurrentAndNext = 1,
        PreviousAndCurrent = 2,
        PreviousAndNext = 3,
    }
    public enum GridResizeDirection
    {
        Auto = 0,
        Columns = 1,
        Rows = 2,
    }
    [System.Windows.StyleTypedPropertyAttribute(Property="PreviewStyle", StyleTargetType=typeof(System.Windows.Controls.Control))]
    public partial class GridSplitter : System.Windows.Controls.Primitives.Thumb
    {
        public static readonly System.Windows.DependencyProperty DragIncrementProperty;
        public static readonly System.Windows.DependencyProperty KeyboardIncrementProperty;
        public static readonly System.Windows.DependencyProperty PreviewStyleProperty;
        public static readonly System.Windows.DependencyProperty ResizeBehaviorProperty;
        public static readonly System.Windows.DependencyProperty ResizeDirectionProperty;
        public static readonly System.Windows.DependencyProperty ShowsPreviewProperty;
        public GridSplitter() { }
        public double DragIncrement { get { throw null; } set { } }
        public double KeyboardIncrement { get { throw null; } set { } }
        public System.Windows.Style PreviewStyle { get { throw null; } set { } }
        public System.Windows.Controls.GridResizeBehavior ResizeBehavior { get { throw null; } set { } }
        public System.Windows.Controls.GridResizeDirection ResizeDirection { get { throw null; } set { } }
        public bool ShowsPreview { get { throw null; } set { } }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnLostKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e) { }
        protected internal override void OnRenderSizeChanged(System.Windows.SizeChangedInfo sizeInfo) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Columns")]
    [System.Windows.StyleTypedPropertyAttribute(Property="ColumnHeaderContainerStyle", StyleTargetType=typeof(System.Windows.Controls.GridViewColumnHeader))]
    public partial class GridView : System.Windows.Controls.ViewBase, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty AllowsColumnReorderProperty;
        public static readonly System.Windows.DependencyProperty ColumnCollectionProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderContainerStyleProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderContextMenuProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderStringFormatProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderTemplateProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderToolTipProperty;
        public GridView() { }
        public bool AllowsColumnReorder { get { throw null; } set { } }
        public System.Windows.Style ColumnHeaderContainerStyle { get { throw null; } set { } }
        public System.Windows.Controls.ContextMenu ColumnHeaderContextMenu { get { throw null; } set { } }
        public string ColumnHeaderStringFormat { get { throw null; } set { } }
        public System.Windows.DataTemplate ColumnHeaderTemplate { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.DataTemplateSelector ColumnHeaderTemplateSelector { get { throw null; } set { } }
        public object ColumnHeaderToolTip { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Controls.GridViewColumnCollection Columns { get { throw null; } }
        protected internal override object DefaultStyleKey { get { throw null; } }
        public static System.Windows.ResourceKey GridViewItemContainerStyleKey { get { throw null; } }
        public static System.Windows.ResourceKey GridViewScrollViewerStyleKey { get { throw null; } }
        public static System.Windows.ResourceKey GridViewStyleKey { get { throw null; } }
        protected internal override object ItemContainerDefaultStyleKey { get { throw null; } }
        protected virtual void AddChild(object column) { }
        protected virtual void AddText(string text) { }
        protected internal override void ClearItem(System.Windows.Controls.ListViewItem item) { }
        protected internal override System.Windows.Automation.Peers.IViewAutomationPeer GetAutomationPeer(System.Windows.Controls.ListView parent) { throw null; }
        public static System.Windows.Controls.GridViewColumnCollection GetColumnCollection(System.Windows.DependencyObject element) { throw null; }
        protected internal override void PrepareItem(System.Windows.Controls.ListViewItem item) { }
        public static void SetColumnCollection(System.Windows.DependencyObject element, System.Windows.Controls.GridViewColumnCollection collection) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static bool ShouldSerializeColumnCollection(System.Windows.DependencyObject obj) { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object column) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
        public override string ToString() { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    [System.Windows.Markup.ContentPropertyAttribute("Header")]
    [System.Windows.StyleTypedPropertyAttribute(Property="HeaderContainerStyle", StyleTargetType=typeof(System.Windows.Controls.GridViewColumnHeader))]
    public partial class GridViewColumn : System.Windows.DependencyObject, System.ComponentModel.INotifyPropertyChanged
    {
        public static readonly System.Windows.DependencyProperty CellTemplateProperty;
        public static readonly System.Windows.DependencyProperty CellTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty HeaderContainerStyleProperty;
        public static readonly System.Windows.DependencyProperty HeaderProperty;
        public static readonly System.Windows.DependencyProperty HeaderStringFormatProperty;
        public static readonly System.Windows.DependencyProperty HeaderTemplateProperty;
        public static readonly System.Windows.DependencyProperty HeaderTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty WidthProperty;
        public GridViewColumn() { }
        public double ActualWidth { get { throw null; } }
        public System.Windows.DataTemplate CellTemplate { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.DataTemplateSelector CellTemplateSelector { get { throw null; } set { } }
        public System.Windows.Data.BindingBase DisplayMemberBinding { get { throw null; } set { } }
        public object Header { get { throw null; } set { } }
        public System.Windows.Style HeaderContainerStyle { get { throw null; } set { } }
        public string HeaderStringFormat { get { throw null; } set { } }
        public System.Windows.DataTemplate HeaderTemplate { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.DataTemplateSelector HeaderTemplateSelector { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double Width { get { throw null; } set { } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        protected virtual void OnHeaderStringFormatChanged(string oldHeaderStringFormat, string newHeaderStringFormat) { }
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e) { }
        public override string ToString() { throw null; }
    }
    public partial class GridViewColumnCollection : System.Collections.ObjectModel.ObservableCollection<System.Windows.Controls.GridViewColumn>
    {
        public GridViewColumnCollection() { }
        protected override void ClearItems() { }
        protected override void InsertItem(int index, System.Windows.Controls.GridViewColumn column) { }
        protected override void MoveItem(int oldIndex, int newIndex) { }
        protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, System.Windows.Controls.GridViewColumn column) { }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_FloatingHeaderCanvas", Type=typeof(System.Windows.Controls.Canvas))]
    [System.Windows.TemplatePartAttribute(Name="PART_HeaderGripper", Type=typeof(System.Windows.Controls.Primitives.Thumb))]
    public partial class GridViewColumnHeader : System.Windows.Controls.Primitives.ButtonBase
    {
        public static readonly System.Windows.DependencyProperty ColumnProperty;
        public static readonly System.Windows.DependencyProperty RoleProperty;
        public GridViewColumnHeader() { }
        public System.Windows.Controls.GridViewColumn Column { get { throw null; } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public System.Windows.Controls.GridViewColumnHeaderRole Role { get { throw null; } }
        protected override void OnAccessKey(System.Windows.Input.AccessKeyEventArgs e) { }
        public override void OnApplyTemplate() { }
        protected override void OnClick() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnLostKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e) { }
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) { }
        protected internal override void OnRenderSizeChanged(System.Windows.SizeChangedInfo sizeInfo) { }
        protected internal override bool ShouldSerializeProperty(System.Windows.DependencyProperty dp) { throw null; }
    }
    public enum GridViewColumnHeaderRole
    {
        Normal = 0,
        Floating = 1,
        Padding = 2,
    }
    [System.Windows.StyleTypedPropertyAttribute(Property="ColumnHeaderContainerStyle", StyleTargetType=typeof(System.Windows.Controls.GridViewColumnHeader))]
    public partial class GridViewHeaderRowPresenter : System.Windows.Controls.Primitives.GridViewRowPresenterBase
    {
        public static readonly System.Windows.DependencyProperty AllowsColumnReorderProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderContainerStyleProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderContextMenuProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderStringFormatProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderTemplateProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty ColumnHeaderToolTipProperty;
        public GridViewHeaderRowPresenter() { }
        public bool AllowsColumnReorder { get { throw null; } set { } }
        public System.Windows.Style ColumnHeaderContainerStyle { get { throw null; } set { } }
        public System.Windows.Controls.ContextMenu ColumnHeaderContextMenu { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string ColumnHeaderStringFormat { get { throw null; } set { } }
        public System.Windows.DataTemplate ColumnHeaderTemplate { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.DataTemplateSelector ColumnHeaderTemplateSelector { get { throw null; } set { } }
        public object ColumnHeaderToolTip { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnLostMouseCapture(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) { }
    }
    public partial class GridViewRowPresenter : System.Windows.Controls.Primitives.GridViewRowPresenterBase
    {
        public static readonly System.Windows.DependencyProperty ContentProperty;
        public GridViewRowPresenter() { }
        public object Content { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public partial class GroupBox : System.Windows.Controls.HeaderedContentControl
    {
        public GroupBox() { }
        protected override void OnAccessKey(System.Windows.Input.AccessKeyEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
    }
    public partial class GroupItem : System.Windows.Controls.ContentControl, System.Windows.Controls.Primitives.IContainItemStorage, System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo
    {
        public GroupItem() { }
        System.Windows.Controls.HierarchicalVirtualizationConstraints System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.Constraints { get { throw null; } set { } }
        System.Windows.Controls.HierarchicalVirtualizationHeaderDesiredSizes System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.HeaderDesiredSizes { get { throw null; } }
        bool System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.InBackgroundLayout { get { throw null; } set { } }
        System.Windows.Controls.HierarchicalVirtualizationItemDesiredSizes System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.ItemDesiredSizes { get { throw null; } set { } }
        System.Windows.Controls.Panel System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.ItemsHost { get { throw null; } }
        bool System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.MustDisableVirtualization { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        public override void OnApplyTemplate() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        void System.Windows.Controls.Primitives.IContainItemStorage.Clear() { }
        void System.Windows.Controls.Primitives.IContainItemStorage.ClearItemValue(object item, System.Windows.DependencyProperty dp) { }
        void System.Windows.Controls.Primitives.IContainItemStorage.ClearValue(System.Windows.DependencyProperty dp) { }
        object System.Windows.Controls.Primitives.IContainItemStorage.ReadItemValue(object item, System.Windows.DependencyProperty dp) { throw null; }
        void System.Windows.Controls.Primitives.IContainItemStorage.StoreItemValue(object item, System.Windows.DependencyProperty dp, object value) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public partial class GroupStyle : System.ComponentModel.INotifyPropertyChanged
    {
        public static readonly System.Windows.Controls.ItemsPanelTemplate DefaultGroupPanel;
        public GroupStyle() { }
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int AlternationCount { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Style ContainerStyle { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Controls.StyleSelector ContainerStyleSelector { get { throw null; } set { } }
        public static System.Windows.Controls.GroupStyle Default { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string HeaderStringFormat { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.DataTemplate HeaderTemplate { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Controls.DataTemplateSelector HeaderTemplateSelector { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool HidesIfEmpty { get { throw null; } set { } }
        public System.Windows.Controls.ItemsPanelTemplate Panel { get { throw null; } set { } }
        protected virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e) { }
    }
    public delegate System.Windows.Controls.GroupStyle GroupStyleSelector(System.Windows.Data.CollectionViewGroup group, int level);
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Text)]
    public partial class HeaderedContentControl : System.Windows.Controls.ContentControl
    {
        public static readonly System.Windows.DependencyProperty HasHeaderProperty;
        public static readonly System.Windows.DependencyProperty HeaderProperty;
        public static readonly System.Windows.DependencyProperty HeaderStringFormatProperty;
        public static readonly System.Windows.DependencyProperty HeaderTemplateProperty;
        public static readonly System.Windows.DependencyProperty HeaderTemplateSelectorProperty;
        public HeaderedContentControl() { }
        [System.ComponentModel.BindableAttribute(false)]
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool HasHeader { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Content")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Label)]
        public object Header { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public string HeaderStringFormat { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Content")]
        public System.Windows.DataTemplate HeaderTemplate { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Content")]
        public System.Windows.Controls.DataTemplateSelector HeaderTemplateSelector { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        protected virtual void OnHeaderChanged(object oldHeader, object newHeader) { }
        protected virtual void OnHeaderStringFormatChanged(string oldHeaderStringFormat, string newHeaderStringFormat) { }
        protected virtual void OnHeaderTemplateChanged(System.Windows.DataTemplate oldHeaderTemplate, System.Windows.DataTemplate newHeaderTemplate) { }
        protected virtual void OnHeaderTemplateSelectorChanged(System.Windows.Controls.DataTemplateSelector oldHeaderTemplateSelector, System.Windows.Controls.DataTemplateSelector newHeaderTemplateSelector) { }
        public override string ToString() { throw null; }
    }
    [System.ComponentModel.DefaultPropertyAttribute("Header")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Menu)]
    public partial class HeaderedItemsControl : System.Windows.Controls.ItemsControl
    {
        public static readonly System.Windows.DependencyProperty HasHeaderProperty;
        public static readonly System.Windows.DependencyProperty HeaderProperty;
        public static readonly System.Windows.DependencyProperty HeaderStringFormatProperty;
        public static readonly System.Windows.DependencyProperty HeaderTemplateProperty;
        public static readonly System.Windows.DependencyProperty HeaderTemplateSelectorProperty;
        public HeaderedItemsControl() { }
        [System.ComponentModel.BindableAttribute(false)]
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool HasHeader { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        public object Header { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public string HeaderStringFormat { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public System.Windows.DataTemplate HeaderTemplate { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public System.Windows.Controls.DataTemplateSelector HeaderTemplateSelector { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        protected virtual void OnHeaderChanged(object oldHeader, object newHeader) { }
        protected virtual void OnHeaderStringFormatChanged(string oldHeaderStringFormat, string newHeaderStringFormat) { }
        protected virtual void OnHeaderTemplateChanged(System.Windows.DataTemplate oldHeaderTemplate, System.Windows.DataTemplate newHeaderTemplate) { }
        protected virtual void OnHeaderTemplateSelectorChanged(System.Windows.Controls.DataTemplateSelector oldHeaderTemplateSelector, System.Windows.Controls.DataTemplateSelector newHeaderTemplateSelector) { }
        public override string ToString() { throw null; }
    }
    public partial struct HierarchicalVirtualizationConstraints
    {
        public HierarchicalVirtualizationConstraints(System.Windows.Controls.VirtualizationCacheLength cacheLength, System.Windows.Controls.VirtualizationCacheLengthUnit cacheLengthUnit, System.Windows.Rect viewport) { throw null; }
        public System.Windows.Controls.VirtualizationCacheLength CacheLength { get { throw null; } }
        public System.Windows.Controls.VirtualizationCacheLengthUnit CacheLengthUnit { get { throw null; } }
        public System.Windows.Rect Viewport { get { throw null; } }
        public override bool Equals(object oCompare) { throw null; }
        public bool Equals(System.Windows.Controls.HierarchicalVirtualizationConstraints comparisonConstraints) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Controls.HierarchicalVirtualizationConstraints constraints1, System.Windows.Controls.HierarchicalVirtualizationConstraints constraints2) { throw null; }
        public static bool operator !=(System.Windows.Controls.HierarchicalVirtualizationConstraints constraints1, System.Windows.Controls.HierarchicalVirtualizationConstraints constraints2) { throw null; }
    }
    public partial struct HierarchicalVirtualizationHeaderDesiredSizes
    {
        public HierarchicalVirtualizationHeaderDesiredSizes(System.Windows.Size logicalSize, System.Windows.Size pixelSize) { throw null; }
        public System.Windows.Size LogicalSize { get { throw null; } }
        public System.Windows.Size PixelSize { get { throw null; } }
        public override bool Equals(object oCompare) { throw null; }
        public bool Equals(System.Windows.Controls.HierarchicalVirtualizationHeaderDesiredSizes comparisonHeaderSizes) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Controls.HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes1, System.Windows.Controls.HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes2) { throw null; }
        public static bool operator !=(System.Windows.Controls.HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes1, System.Windows.Controls.HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes2) { throw null; }
    }
    public partial struct HierarchicalVirtualizationItemDesiredSizes
    {
        public HierarchicalVirtualizationItemDesiredSizes(System.Windows.Size logicalSize, System.Windows.Size logicalSizeInViewport, System.Windows.Size logicalSizeBeforeViewport, System.Windows.Size logicalSizeAfterViewport, System.Windows.Size pixelSize, System.Windows.Size pixelSizeInViewport, System.Windows.Size pixelSizeBeforeViewport, System.Windows.Size pixelSizeAfterViewport) { throw null; }
        public System.Windows.Size LogicalSize { get { throw null; } }
        public System.Windows.Size LogicalSizeAfterViewport { get { throw null; } }
        public System.Windows.Size LogicalSizeBeforeViewport { get { throw null; } }
        public System.Windows.Size LogicalSizeInViewport { get { throw null; } }
        public System.Windows.Size PixelSize { get { throw null; } }
        public System.Windows.Size PixelSizeAfterViewport { get { throw null; } }
        public System.Windows.Size PixelSizeBeforeViewport { get { throw null; } }
        public System.Windows.Size PixelSizeInViewport { get { throw null; } }
        public override bool Equals(object oCompare) { throw null; }
        public bool Equals(System.Windows.Controls.HierarchicalVirtualizationItemDesiredSizes comparisonItemSizes) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Controls.HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes1, System.Windows.Controls.HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes2) { throw null; }
        public static bool operator !=(System.Windows.Controls.HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes1, System.Windows.Controls.HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes2) { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public partial class Image : System.Windows.FrameworkElement, System.Windows.Markup.IUriContext
    {
        public static readonly System.Windows.RoutedEvent DpiChangedEvent;
        public static readonly System.Windows.RoutedEvent ImageFailedEvent;
        public static readonly System.Windows.DependencyProperty SourceProperty;
        public static readonly System.Windows.DependencyProperty StretchDirectionProperty;
        public static readonly System.Windows.DependencyProperty StretchProperty;
        public Image() { }
        protected virtual System.Uri BaseUri { get { throw null; } set { } }
        public System.Windows.Media.ImageSource Source { get { throw null; } set { } }
        public System.Windows.Media.Stretch Stretch { get { throw null; } set { } }
        public System.Windows.Controls.StretchDirection StretchDirection { get { throw null; } set { } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        public event System.Windows.DpiChangedEventHandler DpiChanged { add { } remove { } }
        public event System.EventHandler<System.Windows.ExceptionRoutedEventArgs> ImageFailed { add { } remove { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnDpiChanged(System.Windows.DpiScale oldDpi, System.Windows.DpiScale newDpi) { }
        protected override void OnRender(System.Windows.Media.DrawingContext dc) { }
    }
    public partial class InitializingNewItemEventArgs : System.EventArgs
    {
        public InitializingNewItemEventArgs(object newItem) { }
        public object NewItem { get { throw null; } }
    }
    public delegate void InitializingNewItemEventHandler(object sender, System.Windows.Controls.InitializingNewItemEventArgs e);
    [System.Windows.Markup.ContentPropertyAttribute("Children")]
    public partial class InkCanvas : System.Windows.FrameworkElement, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.RoutedEvent ActiveEditingModeChangedEvent;
        public static readonly System.Windows.DependencyProperty ActiveEditingModeProperty;
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty BottomProperty;
        public static readonly System.Windows.DependencyProperty DefaultDrawingAttributesProperty;
        public static readonly System.Windows.RoutedEvent EditingModeChangedEvent;
        public static readonly System.Windows.RoutedEvent EditingModeInvertedChangedEvent;
        public static readonly System.Windows.DependencyProperty EditingModeInvertedProperty;
        public static readonly System.Windows.DependencyProperty EditingModeProperty;
        public static readonly System.Windows.RoutedEvent GestureEvent;
        public static readonly System.Windows.DependencyProperty LeftProperty;
        public static readonly System.Windows.DependencyProperty RightProperty;
        public static readonly System.Windows.RoutedEvent StrokeCollectedEvent;
        public static readonly System.Windows.RoutedEvent StrokeErasedEvent;
        public static readonly System.Windows.DependencyProperty StrokesProperty;
        public static readonly System.Windows.DependencyProperty TopProperty;
        public InkCanvas() { }
        public System.Windows.Controls.InkCanvasEditingMode ActiveEditingMode { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Controls.UIElementCollection Children { get { throw null; } }
        public System.Windows.Ink.DrawingAttributes DefaultDrawingAttributes { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Input.StylusPointDescription DefaultStylusPointDescription { get { throw null; } set { } }
        protected System.Windows.Input.StylusPlugIns.DynamicRenderer DynamicRenderer { get { throw null; } set { } }
        public System.Windows.Controls.InkCanvasEditingMode EditingMode { get { throw null; } set { } }
        public System.Windows.Controls.InkCanvasEditingMode EditingModeInverted { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Ink.StylusShape EraserShape { get { throw null; } set { } }
        protected System.Windows.Controls.InkPresenter InkPresenter { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsGestureRecognizerAvailable { get { throw null; } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public bool MoveEnabled { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Collections.Generic.IEnumerable<System.Windows.Controls.InkCanvasClipboardFormat> PreferredPasteFormats { get { throw null; } set { } }
        public bool ResizeEnabled { get { throw null; } set { } }
        public System.Windows.Ink.StrokeCollection Strokes { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool UseCustomCursor { get { throw null; } set { } }
        protected override int VisualChildrenCount { get { throw null; } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler ActiveEditingModeChanged { add { } remove { } }
        public event System.Windows.Ink.DrawingAttributesReplacedEventHandler DefaultDrawingAttributesReplaced { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler EditingModeChanged { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler EditingModeInvertedChanged { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.Controls.InkCanvasGestureEventHandler Gesture { add { } remove { } }
        public event System.EventHandler SelectionChanged { add { } remove { } }
        public event System.Windows.Controls.InkCanvasSelectionChangingEventHandler SelectionChanging { add { } remove { } }
        public event System.EventHandler SelectionMoved { add { } remove { } }
        public event System.Windows.Controls.InkCanvasSelectionEditingEventHandler SelectionMoving { add { } remove { } }
        public event System.EventHandler SelectionResized { add { } remove { } }
        public event System.Windows.Controls.InkCanvasSelectionEditingEventHandler SelectionResizing { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.Controls.InkCanvasStrokeCollectedEventHandler StrokeCollected { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler StrokeErased { add { } remove { } }
        public event System.Windows.Controls.InkCanvasStrokeErasingEventHandler StrokeErasing { add { } remove { } }
        public event System.Windows.Controls.InkCanvasStrokesReplacedEventHandler StrokesReplaced { add { } remove { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        public bool CanPaste() { throw null; }
        public void CopySelection() { }
        public void CutSelection() { }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetBottom(System.Windows.UIElement element) { throw null; }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Ink.ApplicationGesture> GetEnabledGestures() { throw null; }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetLeft(System.Windows.UIElement element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetRight(System.Windows.UIElement element) { throw null; }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.UIElement> GetSelectedElements() { throw null; }
        public System.Windows.Ink.StrokeCollection GetSelectedStrokes() { throw null; }
        public System.Windows.Rect GetSelectionBounds() { throw null; }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetTop(System.Windows.UIElement element) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Media.HitTestResult HitTestCore(System.Windows.Media.PointHitTestParameters hitTestParams) { throw null; }
        public System.Windows.Controls.InkCanvasSelectionHitResult HitTestSelection(System.Windows.Point point) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        protected virtual void OnActiveEditingModeChanged(System.Windows.RoutedEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDefaultDrawingAttributesReplaced(System.Windows.Ink.DrawingAttributesReplacedEventArgs e) { }
        protected virtual void OnEditingModeChanged(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnEditingModeInvertedChanged(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnGesture(System.Windows.Controls.InkCanvasGestureEventArgs e) { }
        protected override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected virtual void OnSelectionChanged(System.EventArgs e) { }
        protected virtual void OnSelectionChanging(System.Windows.Controls.InkCanvasSelectionChangingEventArgs e) { }
        protected virtual void OnSelectionMoved(System.EventArgs e) { }
        protected virtual void OnSelectionMoving(System.Windows.Controls.InkCanvasSelectionEditingEventArgs e) { }
        protected virtual void OnSelectionResized(System.EventArgs e) { }
        protected virtual void OnSelectionResizing(System.Windows.Controls.InkCanvasSelectionEditingEventArgs e) { }
        protected virtual void OnStrokeCollected(System.Windows.Controls.InkCanvasStrokeCollectedEventArgs e) { }
        protected virtual void OnStrokeErased(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnStrokeErasing(System.Windows.Controls.InkCanvasStrokeErasingEventArgs e) { }
        protected virtual void OnStrokesReplaced(System.Windows.Controls.InkCanvasStrokesReplacedEventArgs e) { }
        public void Paste() { }
        public void Paste(System.Windows.Point point) { }
        public void Select(System.Collections.Generic.IEnumerable<System.Windows.UIElement> selectedElements) { }
        public void Select(System.Windows.Ink.StrokeCollection selectedStrokes) { }
        public void Select(System.Windows.Ink.StrokeCollection selectedStrokes, System.Collections.Generic.IEnumerable<System.Windows.UIElement> selectedElements) { }
        public static void SetBottom(System.Windows.UIElement element, double length) { }
        public void SetEnabledGestures(System.Collections.Generic.IEnumerable<System.Windows.Ink.ApplicationGesture> applicationGestures) { }
        public static void SetLeft(System.Windows.UIElement element, double length) { }
        public static void SetRight(System.Windows.UIElement element, double length) { }
        public static void SetTop(System.Windows.UIElement element, double length) { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string textData) { }
    }
    public enum InkCanvasClipboardFormat
    {
        InkSerializedFormat = 0,
        Text = 1,
        Xaml = 2,
    }
    public enum InkCanvasEditingMode
    {
        None = 0,
        Ink = 1,
        GestureOnly = 2,
        InkAndGesture = 3,
        Select = 4,
        EraseByPoint = 5,
        EraseByStroke = 6,
    }
    public partial class InkCanvasGestureEventArgs : System.Windows.RoutedEventArgs
    {
        public InkCanvasGestureEventArgs(System.Windows.Ink.StrokeCollection strokes, System.Collections.Generic.IEnumerable<System.Windows.Ink.GestureRecognitionResult> gestureRecognitionResults) { }
        public bool Cancel { get { throw null; } set { } }
        public System.Windows.Ink.StrokeCollection Strokes { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Ink.GestureRecognitionResult> GetGestureRecognitionResults() { throw null; }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void InkCanvasGestureEventHandler(object sender, System.Windows.Controls.InkCanvasGestureEventArgs e);
    public partial class InkCanvasSelectionChangingEventArgs : System.ComponentModel.CancelEventArgs
    {
        internal InkCanvasSelectionChangingEventArgs() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.UIElement> GetSelectedElements() { throw null; }
        public System.Windows.Ink.StrokeCollection GetSelectedStrokes() { throw null; }
        public void SetSelectedElements(System.Collections.Generic.IEnumerable<System.Windows.UIElement> selectedElements) { }
        public void SetSelectedStrokes(System.Windows.Ink.StrokeCollection selectedStrokes) { }
    }
    public delegate void InkCanvasSelectionChangingEventHandler(object sender, System.Windows.Controls.InkCanvasSelectionChangingEventArgs e);
    public partial class InkCanvasSelectionEditingEventArgs : System.ComponentModel.CancelEventArgs
    {
        internal InkCanvasSelectionEditingEventArgs() { }
        public System.Windows.Rect NewRectangle { get { throw null; } set { } }
        public System.Windows.Rect OldRectangle { get { throw null; } }
    }
    public delegate void InkCanvasSelectionEditingEventHandler(object sender, System.Windows.Controls.InkCanvasSelectionEditingEventArgs e);
    public enum InkCanvasSelectionHitResult
    {
        None = 0,
        TopLeft = 1,
        Top = 2,
        TopRight = 3,
        Right = 4,
        BottomRight = 5,
        Bottom = 6,
        BottomLeft = 7,
        Left = 8,
        Selection = 9,
    }
    public partial class InkCanvasStrokeCollectedEventArgs : System.Windows.RoutedEventArgs
    {
        public InkCanvasStrokeCollectedEventArgs(System.Windows.Ink.Stroke stroke) { }
        public System.Windows.Ink.Stroke Stroke { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void InkCanvasStrokeCollectedEventHandler(object sender, System.Windows.Controls.InkCanvasStrokeCollectedEventArgs e);
    public partial class InkCanvasStrokeErasingEventArgs : System.ComponentModel.CancelEventArgs
    {
        internal InkCanvasStrokeErasingEventArgs() { }
        public System.Windows.Ink.Stroke Stroke { get { throw null; } }
    }
    public delegate void InkCanvasStrokeErasingEventHandler(object sender, System.Windows.Controls.InkCanvasStrokeErasingEventArgs e);
    public partial class InkCanvasStrokesReplacedEventArgs : System.EventArgs
    {
        internal InkCanvasStrokesReplacedEventArgs() { }
        public System.Windows.Ink.StrokeCollection NewStrokes { get { throw null; } }
        public System.Windows.Ink.StrokeCollection PreviousStrokes { get { throw null; } }
    }
    public delegate void InkCanvasStrokesReplacedEventHandler(object sender, System.Windows.Controls.InkCanvasStrokesReplacedEventArgs e);
    public partial class InkPresenter : System.Windows.Controls.Decorator
    {
        public static readonly System.Windows.DependencyProperty StrokesProperty;
        public InkPresenter() { }
        public System.Windows.Ink.StrokeCollection Strokes { get { throw null; } set { } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        public void AttachVisuals(System.Windows.Media.Visual visual, System.Windows.Ink.DrawingAttributes drawingAttributes) { }
        public void DetachVisuals(System.Windows.Media.Visual visual) { }
        protected override System.Windows.Media.Geometry GetLayoutClip(System.Windows.Size layoutSlotSize) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore)]
    public sealed partial class ItemCollection : System.Windows.Data.CollectionView, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.ComponentModel.ICollectionViewLiveShaping, System.ComponentModel.IEditableCollectionView, System.ComponentModel.IEditableCollectionViewAddNewItem, System.ComponentModel.IItemProperties, System.Windows.IWeakEventListener
    {
        internal ItemCollection() : base (default(System.Collections.IEnumerable)) { }
        public bool CanChangeLiveFiltering { get { throw null; } }
        public bool CanChangeLiveGrouping { get { throw null; } }
        public bool CanChangeLiveSorting { get { throw null; } }
        public override bool CanFilter { get { throw null; } }
        public override bool CanGroup { get { throw null; } }
        public override bool CanSort { get { throw null; } }
        public override int Count { get { throw null; } }
        public override object CurrentItem { get { throw null; } }
        public override int CurrentPosition { get { throw null; } }
        public override System.Predicate<object> Filter { get { throw null; } set { } }
        public override System.Collections.ObjectModel.ObservableCollection<System.ComponentModel.GroupDescription> GroupDescriptions { get { throw null; } }
        public override System.Collections.ObjectModel.ReadOnlyObservableCollection<object> Groups { get { throw null; } }
        public override bool IsCurrentAfterLast { get { throw null; } }
        public override bool IsCurrentBeforeFirst { get { throw null; } }
        public override bool IsEmpty { get { throw null; } }
        public bool? IsLiveFiltering { get { throw null; } set { } }
        public bool? IsLiveGrouping { get { throw null; } set { } }
        public bool? IsLiveSorting { get { throw null; } set { } }
        public object this[int index] { get { throw null; } set { } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveFilteringProperties { get { throw null; } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveGroupingProperties { get { throw null; } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveSortingProperties { get { throw null; } }
        public override bool NeedsRefresh { get { throw null; } }
        public override System.ComponentModel.SortDescriptionCollection SortDescriptions { get { throw null; } }
        public override System.Collections.IEnumerable SourceCollection { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        bool System.ComponentModel.IEditableCollectionView.CanAddNew { get { throw null; } }
        bool System.ComponentModel.IEditableCollectionView.CanCancelEdit { get { throw null; } }
        bool System.ComponentModel.IEditableCollectionView.CanRemove { get { throw null; } }
        object System.ComponentModel.IEditableCollectionView.CurrentAddItem { get { throw null; } }
        object System.ComponentModel.IEditableCollectionView.CurrentEditItem { get { throw null; } }
        bool System.ComponentModel.IEditableCollectionView.IsAddingNew { get { throw null; } }
        bool System.ComponentModel.IEditableCollectionView.IsEditingItem { get { throw null; } }
        System.ComponentModel.NewItemPlaceholderPosition System.ComponentModel.IEditableCollectionView.NewItemPlaceholderPosition { get { throw null; } set { } }
        bool System.ComponentModel.IEditableCollectionViewAddNewItem.CanAddNewItem { get { throw null; } }
        System.Collections.ObjectModel.ReadOnlyCollection<System.ComponentModel.ItemPropertyInfo> System.ComponentModel.IItemProperties.ItemProperties { get { throw null; } }
        public int Add(object newItem) { throw null; }
        public void Clear() { }
        public override bool Contains(object containItem) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public override System.IDisposable DeferRefresh() { throw null; }
        protected override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public override object GetItemAt(int index) { throw null; }
        public override int IndexOf(object item) { throw null; }
        public void Insert(int insertIndex, object insertItem) { }
        public override bool MoveCurrentTo(object item) { throw null; }
        public override bool MoveCurrentToFirst() { throw null; }
        public override bool MoveCurrentToLast() { throw null; }
        public override bool MoveCurrentToNext() { throw null; }
        public override bool MoveCurrentToPosition(int position) { throw null; }
        public override bool MoveCurrentToPrevious() { throw null; }
        public override bool PassesFilter(object item) { throw null; }
        protected override void RefreshOverride() { }
        public void Remove(object removeItem) { }
        public void RemoveAt(int removeIndex) { }
        object System.ComponentModel.IEditableCollectionView.AddNew() { throw null; }
        void System.ComponentModel.IEditableCollectionView.CancelEdit() { }
        void System.ComponentModel.IEditableCollectionView.CancelNew() { }
        void System.ComponentModel.IEditableCollectionView.CommitEdit() { }
        void System.ComponentModel.IEditableCollectionView.CommitNew() { }
        void System.ComponentModel.IEditableCollectionView.EditItem(object item) { }
        void System.ComponentModel.IEditableCollectionView.Remove(object item) { }
        void System.ComponentModel.IEditableCollectionView.RemoveAt(int index) { }
        object System.ComponentModel.IEditableCollectionViewAddNewItem.AddNewItem(object newItem) { throw null; }
        bool System.Windows.IWeakEventListener.ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e) { throw null; }
    }
    public sealed partial class ItemContainerGenerator : System.Windows.Controls.Primitives.IItemContainerGenerator, System.Windows.Controls.Primitives.IRecyclingItemContainerGenerator, System.Windows.IWeakEventListener
    {
        internal ItemContainerGenerator() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<object> Items { get { throw null; } }
        public System.Windows.Controls.Primitives.GeneratorStatus Status { get { throw null; } }
        public event System.Windows.Controls.Primitives.ItemsChangedEventHandler ItemsChanged { add { } remove { } }
        public event System.EventHandler StatusChanged { add { } remove { } }
        public System.Windows.DependencyObject ContainerFromIndex(int index) { throw null; }
        public System.Windows.DependencyObject ContainerFromItem(object item) { throw null; }
        public System.IDisposable GenerateBatches() { throw null; }
        public int IndexFromContainer(System.Windows.DependencyObject container) { throw null; }
        public int IndexFromContainer(System.Windows.DependencyObject container, bool returnLocalIndex) { throw null; }
        public object ItemFromContainer(System.Windows.DependencyObject container) { throw null; }
        System.Windows.DependencyObject System.Windows.Controls.Primitives.IItemContainerGenerator.GenerateNext() { throw null; }
        System.Windows.DependencyObject System.Windows.Controls.Primitives.IItemContainerGenerator.GenerateNext(out bool isNewlyRealized) { throw null; }
        System.Windows.Controls.Primitives.GeneratorPosition System.Windows.Controls.Primitives.IItemContainerGenerator.GeneratorPositionFromIndex(int itemIndex) { throw null; }
        System.Windows.Controls.ItemContainerGenerator System.Windows.Controls.Primitives.IItemContainerGenerator.GetItemContainerGeneratorForPanel(System.Windows.Controls.Panel panel) { throw null; }
        int System.Windows.Controls.Primitives.IItemContainerGenerator.IndexFromGeneratorPosition(System.Windows.Controls.Primitives.GeneratorPosition position) { throw null; }
        void System.Windows.Controls.Primitives.IItemContainerGenerator.PrepareItemContainer(System.Windows.DependencyObject container) { }
        void System.Windows.Controls.Primitives.IItemContainerGenerator.Remove(System.Windows.Controls.Primitives.GeneratorPosition position, int count) { }
        void System.Windows.Controls.Primitives.IItemContainerGenerator.RemoveAll() { }
        System.IDisposable System.Windows.Controls.Primitives.IItemContainerGenerator.StartAt(System.Windows.Controls.Primitives.GeneratorPosition position, System.Windows.Controls.Primitives.GeneratorDirection direction) { throw null; }
        System.IDisposable System.Windows.Controls.Primitives.IItemContainerGenerator.StartAt(System.Windows.Controls.Primitives.GeneratorPosition position, System.Windows.Controls.Primitives.GeneratorDirection direction, bool allowStartAtRealizedItem) { throw null; }
        void System.Windows.Controls.Primitives.IRecyclingItemContainerGenerator.Recycle(System.Windows.Controls.Primitives.GeneratorPosition position, int count) { }
        bool System.Windows.IWeakEventListener.ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e) { throw null; }
    }
    [System.Windows.Markup.DictionaryKeyPropertyAttribute("ItemContainerTemplateKey")]
    public partial class ItemContainerTemplate : System.Windows.DataTemplate
    {
        public ItemContainerTemplate() { }
        public object ItemContainerTemplateKey { get { throw null; } }
    }
    public partial class ItemContainerTemplateKey : System.Windows.TemplateKey
    {
        public ItemContainerTemplateKey() : base (default(System.Windows.TemplateKey.TemplateType)) { }
        public ItemContainerTemplateKey(object dataType) : base (default(System.Windows.TemplateKey.TemplateType)) { }
    }
    public abstract partial class ItemContainerTemplateSelector
    {
        protected ItemContainerTemplateSelector() { }
        public virtual System.Windows.DataTemplate SelectTemplate(object item, System.Windows.Controls.ItemsControl parentItemsControl) { throw null; }
    }
    [System.ComponentModel.DefaultEventAttribute("OnItemsChanged")]
    [System.ComponentModel.DefaultPropertyAttribute("Items")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    [System.Windows.Markup.ContentPropertyAttribute("Items")]
    [System.Windows.StyleTypedPropertyAttribute(Property="ItemContainerStyle", StyleTargetType=typeof(System.Windows.FrameworkElement))]
    public partial class ItemsControl : System.Windows.Controls.Control, System.Windows.Controls.Primitives.IContainItemStorage, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty AlternationCountProperty;
        public static readonly System.Windows.DependencyProperty AlternationIndexProperty;
        public static readonly System.Windows.DependencyProperty DisplayMemberPathProperty;
        public static readonly System.Windows.DependencyProperty GroupStyleSelectorProperty;
        public static readonly System.Windows.DependencyProperty HasItemsProperty;
        public static readonly System.Windows.DependencyProperty IsGroupingProperty;
        public static readonly System.Windows.DependencyProperty IsTextSearchCaseSensitiveProperty;
        public static readonly System.Windows.DependencyProperty IsTextSearchEnabledProperty;
        public static readonly System.Windows.DependencyProperty ItemBindingGroupProperty;
        public static readonly System.Windows.DependencyProperty ItemContainerStyleProperty;
        public static readonly System.Windows.DependencyProperty ItemContainerStyleSelectorProperty;
        public static readonly System.Windows.DependencyProperty ItemsPanelProperty;
        public static readonly System.Windows.DependencyProperty ItemsSourceProperty;
        public static readonly System.Windows.DependencyProperty ItemStringFormatProperty;
        public static readonly System.Windows.DependencyProperty ItemTemplateProperty;
        public static readonly System.Windows.DependencyProperty ItemTemplateSelectorProperty;
        public ItemsControl() { }
        [System.ComponentModel.BindableAttribute(true)]
        public int AlternationCount { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public string DisplayMemberPath { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Collections.ObjectModel.ObservableCollection<System.Windows.Controls.GroupStyle> GroupStyle { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.GroupStyleSelector GroupStyleSelector { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(false)]
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool HasItems { get { throw null; } }
        [System.ComponentModel.BindableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsGrouping { get { throw null; } }
        public bool IsTextSearchCaseSensitive { get { throw null; } set { } }
        public bool IsTextSearchEnabled { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public System.Windows.Data.BindingGroup ItemBindingGroup { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(false)]
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public System.Windows.Controls.ItemContainerGenerator ItemContainerGenerator { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Content")]
        public System.Windows.Style ItemContainerStyle { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Content")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.StyleSelector ItemContainerStyleSelector { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Controls.ItemCollection Items { get { throw null; } }
        [System.ComponentModel.BindableAttribute(false)]
        public System.Windows.Controls.ItemsPanelTemplate ItemsPanel { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Collections.IEnumerable ItemsSource { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public string ItemStringFormat { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public System.Windows.DataTemplate ItemTemplate { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.DataTemplateSelector ItemTemplateSelector { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        protected virtual void AddChild(object value) { }
        protected virtual void AddText(string text) { }
        public override void BeginInit() { }
        protected virtual void ClearContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        public static System.Windows.DependencyObject ContainerFromElement(System.Windows.Controls.ItemsControl itemsControl, System.Windows.DependencyObject element) { throw null; }
        public System.Windows.DependencyObject ContainerFromElement(System.Windows.DependencyObject element) { throw null; }
        public override void EndInit() { }
        public static int GetAlternationIndex(System.Windows.DependencyObject element) { throw null; }
        protected virtual System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        public static System.Windows.Controls.ItemsControl GetItemsOwner(System.Windows.DependencyObject element) { throw null; }
        public bool IsItemItsOwnContainer(object item) { throw null; }
        protected virtual bool IsItemItsOwnContainerOverride(object item) { throw null; }
        public static System.Windows.Controls.ItemsControl ItemsControlFromItemContainer(System.Windows.DependencyObject container) { throw null; }
        protected virtual void OnAlternationCountChanged(int oldAlternationCount, int newAlternationCount) { }
        protected virtual void OnDisplayMemberPathChanged(string oldDisplayMemberPath, string newDisplayMemberPath) { }
        protected virtual void OnGroupStyleSelectorChanged(System.Windows.Controls.GroupStyleSelector oldGroupStyleSelector, System.Windows.Controls.GroupStyleSelector newGroupStyleSelector) { }
        protected virtual void OnItemBindingGroupChanged(System.Windows.Data.BindingGroup oldItemBindingGroup, System.Windows.Data.BindingGroup newItemBindingGroup) { }
        protected virtual void OnItemContainerStyleChanged(System.Windows.Style oldItemContainerStyle, System.Windows.Style newItemContainerStyle) { }
        protected virtual void OnItemContainerStyleSelectorChanged(System.Windows.Controls.StyleSelector oldItemContainerStyleSelector, System.Windows.Controls.StyleSelector newItemContainerStyleSelector) { }
        protected virtual void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected virtual void OnItemsPanelChanged(System.Windows.Controls.ItemsPanelTemplate oldItemsPanel, System.Windows.Controls.ItemsPanelTemplate newItemsPanel) { }
        protected virtual void OnItemsSourceChanged(System.Collections.IEnumerable oldValue, System.Collections.IEnumerable newValue) { }
        protected virtual void OnItemStringFormatChanged(string oldItemStringFormat, string newItemStringFormat) { }
        protected virtual void OnItemTemplateChanged(System.Windows.DataTemplate oldItemTemplate, System.Windows.DataTemplate newItemTemplate) { }
        protected virtual void OnItemTemplateSelectorChanged(System.Windows.Controls.DataTemplateSelector oldItemTemplateSelector, System.Windows.Controls.DataTemplateSelector newItemTemplateSelector) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnTextInput(System.Windows.Input.TextCompositionEventArgs e) { }
        protected virtual void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        protected virtual bool ShouldApplyItemContainerStyle(System.Windows.DependencyObject container, object item) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeGroupStyle() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeItems() { throw null; }
        void System.Windows.Controls.Primitives.IContainItemStorage.Clear() { }
        void System.Windows.Controls.Primitives.IContainItemStorage.ClearItemValue(object item, System.Windows.DependencyProperty dp) { }
        void System.Windows.Controls.Primitives.IContainItemStorage.ClearValue(System.Windows.DependencyProperty dp) { }
        object System.Windows.Controls.Primitives.IContainItemStorage.ReadItemValue(object item, System.Windows.DependencyProperty dp) { throw null; }
        void System.Windows.Controls.Primitives.IContainItemStorage.StoreItemValue(object item, System.Windows.DependencyProperty dp, object value) { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
        public override string ToString() { throw null; }
    }
    public partial class ItemsPanelTemplate : System.Windows.FrameworkTemplate
    {
        public ItemsPanelTemplate() { }
        public ItemsPanelTemplate(System.Windows.FrameworkElementFactory root) { }
        protected override void ValidateTemplatedParent(System.Windows.FrameworkElement templatedParent) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
    public partial class ItemsPresenter : System.Windows.FrameworkElement
    {
        public ItemsPresenter() { }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        public override void OnApplyTemplate() { }
        protected virtual void OnTemplateChanged(System.Windows.Controls.ItemsPanelTemplate oldTemplate, System.Windows.Controls.ItemsPanelTemplate newTemplate) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Label)]
    public partial class Label : System.Windows.Controls.ContentControl
    {
        public static readonly System.Windows.DependencyProperty TargetProperty;
        public Label() { }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Markup.NameReferenceConverter))]
        public System.Windows.UIElement Target { get { throw null; } set { } }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.ListBox)]
    [System.Windows.StyleTypedPropertyAttribute(Property="ItemContainerStyle", StyleTargetType=typeof(System.Windows.Controls.ListBoxItem))]
    public partial class ListBox : System.Windows.Controls.Primitives.Selector
    {
        public static readonly System.Windows.DependencyProperty SelectedItemsProperty;
        public static readonly System.Windows.DependencyProperty SelectionModeProperty;
        public ListBox() { }
        protected object AnchorItem { get { throw null; } set { } }
        protected internal override bool HandlesScrolling { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Collections.IList SelectedItems { get { throw null; } }
        public System.Windows.Controls.SelectionMode SelectionMode { get { throw null; } set { } }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnIsMouseCapturedChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnSelectionChanged(System.Windows.Controls.SelectionChangedEventArgs e) { }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        public void ScrollIntoView(object item) { }
        public void SelectAll() { }
        protected bool SetSelectedItems(System.Collections.IEnumerable selectedItems) { throw null; }
        public void UnselectAll() { }
    }
    [System.ComponentModel.DefaultEventAttribute("Selected")]
    public partial class ListBoxItem : System.Windows.Controls.ContentControl
    {
        public static readonly System.Windows.DependencyProperty IsSelectedProperty;
        public static readonly System.Windows.RoutedEvent SelectedEvent;
        public static readonly System.Windows.RoutedEvent UnselectedEvent;
        public ListBoxItem() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsSelected { get { throw null; } set { } }
        public event System.Windows.RoutedEventHandler Selected { add { } remove { } }
        public event System.Windows.RoutedEventHandler Unselected { add { } remove { } }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseRightButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected virtual void OnSelected(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnUnselected(System.Windows.RoutedEventArgs e) { }
        protected internal override void OnVisualParentChanged(System.Windows.DependencyObject oldParent) { }
    }
    [System.Windows.StyleTypedPropertyAttribute(Property="ItemContainerStyle", StyleTargetType=typeof(System.Windows.Controls.ListViewItem))]
    public partial class ListView : System.Windows.Controls.ListBox
    {
        public static readonly System.Windows.DependencyProperty ViewProperty;
        public ListView() { }
        public System.Windows.Controls.ViewBase View { get { throw null; } set { } }
        protected override void ClearContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
    }
    public partial class ListViewItem : System.Windows.Controls.ListBoxItem
    {
        public ListViewItem() { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
    public partial class MediaElement : System.Windows.FrameworkElement, System.Windows.Markup.IUriContext
    {
        public static readonly System.Windows.DependencyProperty BalanceProperty;
        public static readonly System.Windows.RoutedEvent BufferingEndedEvent;
        public static readonly System.Windows.RoutedEvent BufferingStartedEvent;
        public static readonly System.Windows.DependencyProperty IsMutedProperty;
        public static readonly System.Windows.DependencyProperty LoadedBehaviorProperty;
        public static readonly System.Windows.RoutedEvent MediaEndedEvent;
        public static readonly System.Windows.RoutedEvent MediaFailedEvent;
        public static readonly System.Windows.RoutedEvent MediaOpenedEvent;
        public static readonly System.Windows.RoutedEvent ScriptCommandEvent;
        public static readonly System.Windows.DependencyProperty ScrubbingEnabledProperty;
        public static readonly System.Windows.DependencyProperty SourceProperty;
        public static readonly System.Windows.DependencyProperty StretchDirectionProperty;
        public static readonly System.Windows.DependencyProperty StretchProperty;
        public static readonly System.Windows.DependencyProperty UnloadedBehaviorProperty;
        public static readonly System.Windows.DependencyProperty VolumeProperty;
        public MediaElement() { }
        public double Balance { get { throw null; } set { } }
        public double BufferingProgress { get { throw null; } }
        public bool CanPause { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Media.MediaClock Clock { get { throw null; } set { } }
        public double DownloadProgress { get { throw null; } }
        public bool HasAudio { get { throw null; } }
        public bool HasVideo { get { throw null; } }
        public bool IsBuffering { get { throw null; } }
        public bool IsMuted { get { throw null; } set { } }
        public System.Windows.Controls.MediaState LoadedBehavior { get { throw null; } set { } }
        public System.Windows.Duration NaturalDuration { get { throw null; } }
        public int NaturalVideoHeight { get { throw null; } }
        public int NaturalVideoWidth { get { throw null; } }
        public System.TimeSpan Position { get { throw null; } set { } }
        public bool ScrubbingEnabled { get { throw null; } set { } }
        public System.Uri Source { get { throw null; } set { } }
        public double SpeedRatio { get { throw null; } set { } }
        public System.Windows.Media.Stretch Stretch { get { throw null; } set { } }
        public System.Windows.Controls.StretchDirection StretchDirection { get { throw null; } set { } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        public System.Windows.Controls.MediaState UnloadedBehavior { get { throw null; } set { } }
        public double Volume { get { throw null; } set { } }
        public event System.Windows.RoutedEventHandler BufferingEnded { add { } remove { } }
        public event System.Windows.RoutedEventHandler BufferingStarted { add { } remove { } }
        public event System.Windows.RoutedEventHandler MediaEnded { add { } remove { } }
        public event System.EventHandler<System.Windows.ExceptionRoutedEventArgs> MediaFailed { add { } remove { } }
        public event System.Windows.RoutedEventHandler MediaOpened { add { } remove { } }
        public event System.EventHandler<System.Windows.MediaScriptCommandRoutedEventArgs> ScriptCommand { add { } remove { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        public void Close() { }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) { }
        public void Pause() { }
        public void Play() { }
        public void Stop() { }
    }
    public enum MediaState
    {
        Manual = 0,
        Play = 1,
        Close = 2,
        Pause = 3,
        Stop = 4,
    }
    public partial class Menu : System.Windows.Controls.Primitives.MenuBase
    {
        public static readonly System.Windows.DependencyProperty IsMainMenuProperty;
        public Menu() { }
        public bool IsMainMenu { get { throw null; } set { } }
        protected override void HandleMouseButton(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnInitialized(System.EventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnTextInput(System.Windows.Input.TextCompositionEventArgs e) { }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
    }
    [System.ComponentModel.DefaultEventAttribute("Click")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Menu)]
    [System.Windows.StyleTypedPropertyAttribute(Property="ItemContainerStyle", StyleTargetType=typeof(System.Windows.Controls.MenuItem))]
    [System.Windows.TemplatePartAttribute(Name="PART_Popup", Type=typeof(System.Windows.Controls.Primitives.Popup))]
    public partial class MenuItem : System.Windows.Controls.HeaderedItemsControl, System.Windows.Input.ICommandSource
    {
        public static readonly System.Windows.RoutedEvent CheckedEvent;
        public static readonly System.Windows.RoutedEvent ClickEvent;
        public static readonly System.Windows.DependencyProperty CommandParameterProperty;
        public static readonly System.Windows.DependencyProperty CommandProperty;
        public static readonly System.Windows.DependencyProperty CommandTargetProperty;
        public static readonly System.Windows.DependencyProperty IconProperty;
        public static readonly System.Windows.DependencyProperty InputGestureTextProperty;
        public static readonly System.Windows.DependencyProperty IsCheckableProperty;
        public static readonly System.Windows.DependencyProperty IsCheckedProperty;
        public static readonly System.Windows.DependencyProperty IsHighlightedProperty;
        public static readonly System.Windows.DependencyProperty IsPressedProperty;
        public static readonly System.Windows.DependencyProperty IsSubmenuOpenProperty;
        public static readonly System.Windows.DependencyProperty IsSuspendingPopupAnimationProperty;
        public static readonly System.Windows.DependencyProperty ItemContainerTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty RoleProperty;
        public static readonly System.Windows.DependencyProperty StaysOpenOnClickProperty;
        public static readonly System.Windows.RoutedEvent SubmenuClosedEvent;
        public static readonly System.Windows.RoutedEvent SubmenuOpenedEvent;
        public static readonly System.Windows.RoutedEvent UncheckedEvent;
        public static readonly System.Windows.DependencyProperty UsesItemContainerTemplateProperty;
        public MenuItem() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Action")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public System.Windows.Input.ICommand Command { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Action")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public object CommandParameter { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Action")]
        public System.Windows.IInputElement CommandTarget { get { throw null; } set { } }
        protected internal override bool HandlesScrolling { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        public object Icon { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public string InputGestureText { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public bool IsCheckable { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsChecked { get { throw null; } set { } }
        protected override bool IsEnabledCore { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsHighlighted { get { throw null; } protected set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsPressed { get { throw null; } protected set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsSubmenuOpen { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsSuspendingPopupAnimation { get { throw null; } }
        public System.Windows.Controls.ItemContainerTemplateSelector ItemContainerTemplateSelector { get { throw null; } set { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public System.Windows.Controls.MenuItemRole Role { get { throw null; } }
        public static System.Windows.ResourceKey SeparatorStyleKey { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public bool StaysOpenOnClick { get { throw null; } set { } }
        public static System.Windows.ResourceKey SubmenuHeaderTemplateKey { get { throw null; } }
        public static System.Windows.ResourceKey SubmenuItemTemplateKey { get { throw null; } }
        public static System.Windows.ResourceKey TopLevelHeaderTemplateKey { get { throw null; } }
        public static System.Windows.ResourceKey TopLevelItemTemplateKey { get { throw null; } }
        public bool UsesItemContainerTemplate { get { throw null; } set { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Checked { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Click { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler SubmenuClosed { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler SubmenuOpened { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Unchecked { add { } remove { } }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        protected override void OnAccessKey(System.Windows.Input.AccessKeyEventArgs e) { }
        public override void OnApplyTemplate() { }
        protected virtual void OnChecked(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnClick() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnGotKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e) { }
        protected override void OnInitialized(System.EventArgs e) { }
        protected override void OnIsKeyboardFocusWithinChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseRightButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseRightButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
        protected virtual void OnSubmenuClosed(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnSubmenuOpened(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnUnchecked(System.Windows.RoutedEventArgs e) { }
        protected internal override void OnVisualParentChanged(System.Windows.DependencyObject oldParent) { }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        protected override bool ShouldApplyItemContainerStyle(System.Windows.DependencyObject container, object item) { throw null; }
    }
    public enum MenuItemRole
    {
        TopLevelItem = 0,
        TopLevelHeader = 1,
        SubmenuItem = 2,
        SubmenuHeader = 3,
    }
    public sealed partial class MenuScrollingVisibilityConverter : System.Windows.Data.IMultiValueConverter
    {
        public MenuScrollingVisibilityConverter() { }
        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
        public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) { throw null; }
    }
    public sealed partial class NotifyDataErrorValidationRule : System.Windows.Controls.ValidationRule
    {
        public NotifyDataErrorValidationRule() { }
        public override System.Windows.Controls.ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo) { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public enum Orientation
    {
        Horizontal = 0,
        Vertical = 1,
    }
    public enum OverflowMode
    {
        AsNeeded = 0,
        Always = 1,
        Never = 2,
    }
    [System.Windows.Markup.ContentPropertyAttribute("Content")]
    public partial class Page : System.Windows.FrameworkElement, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty ContentProperty;
        public static readonly System.Windows.DependencyProperty FontFamilyProperty;
        public static readonly System.Windows.DependencyProperty FontSizeProperty;
        public static readonly System.Windows.DependencyProperty ForegroundProperty;
        public static readonly System.Windows.DependencyProperty KeepAliveProperty;
        public static readonly System.Windows.DependencyProperty TemplateProperty;
        public static readonly System.Windows.DependencyProperty TitleProperty;
        public Page() { }
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        public object Content { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Font, Modifiability=System.Windows.Modifiability.Unmodifiable)]
        public System.Windows.Media.FontFamily FontFamily { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.FontSizeConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None)]
        public double FontSize { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Media.Brush Foreground { get { throw null; } set { } }
        public bool KeepAlive { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public System.Windows.Navigation.NavigationService NavigationService { get { throw null; } }
        public bool ShowsNavigationUI { get { throw null; } set { } }
        public System.Windows.Controls.ControlTemplate Template { get { throw null; } set { } }
        public string Title { get { throw null; } set { } }
        public double WindowHeight { get { throw null; } set { } }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Title)]
        public string WindowTitle { get { throw null; } set { } }
        public double WindowWidth { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeBounds) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected virtual void OnTemplateChanged(System.Windows.Controls.ControlTemplate oldTemplate, System.Windows.Controls.ControlTemplate newTemplate) { }
        protected internal sealed override void OnVisualParentChanged(System.Windows.DependencyObject oldParent) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeShowsNavigationUI() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeTitle() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeWindowHeight() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeWindowTitle() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeWindowWidth() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object obj) { }
        void System.Windows.Markup.IAddChild.AddText(string str) { }
    }
    public partial struct PageRange
    {
        public PageRange(int page) { throw null; }
        public PageRange(int pageFrom, int pageTo) { throw null; }
        public int PageFrom { get { throw null; } set { } }
        public int PageTo { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Windows.Controls.PageRange pageRange) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Controls.PageRange pr1, System.Windows.Controls.PageRange pr2) { throw null; }
        public static bool operator !=(System.Windows.Controls.PageRange pr1, System.Windows.Controls.PageRange pr2) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum PageRangeSelection
    {
        AllPages = 0,
        UserPages = 1,
        CurrentPage = 2,
        SelectedPages = 3,
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore)]
    [System.Windows.Markup.ContentPropertyAttribute("Children")]
    public abstract partial class Panel : System.Windows.FrameworkElement, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty IsItemsHostProperty;
        public static readonly System.Windows.DependencyProperty ZIndexProperty;
        protected Panel() { }
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Controls.UIElementCollection Children { get { throw null; } }
        protected internal virtual bool HasLogicalOrientation { get { throw null; } }
        public bool HasLogicalOrientationPublic { get { throw null; } }
        protected internal System.Windows.Controls.UIElementCollection InternalChildren { get { throw null; } }
        [System.ComponentModel.BindableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public bool IsItemsHost { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        protected internal virtual System.Windows.Controls.Orientation LogicalOrientation { get { throw null; } }
        public System.Windows.Controls.Orientation LogicalOrientationPublic { get { throw null; } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected virtual System.Windows.Controls.UIElementCollection CreateUIElementCollection(System.Windows.FrameworkElement logicalParent) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        public static int GetZIndex(System.Windows.UIElement element) { throw null; }
        protected virtual void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost) { }
        protected override void OnRender(System.Windows.Media.DrawingContext dc) { }
        protected internal override void OnVisualChildrenChanged(System.Windows.DependencyObject visualAdded, System.Windows.DependencyObject visualRemoved) { }
        public static void SetZIndex(System.Windows.UIElement element, int value) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeChildren() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public enum PanningMode
    {
        None = 0,
        HorizontalOnly = 1,
        VerticalOnly = 2,
        Both = 3,
        HorizontalFirst = 4,
        VerticalFirst = 5,
    }
    [System.Windows.TemplatePartAttribute(Name="PART_ContentHost", Type=typeof(System.Windows.FrameworkElement))]
    public sealed partial class PasswordBox : System.Windows.Controls.Control
    {
        public static readonly System.Windows.DependencyProperty CaretBrushProperty;
        public static readonly System.Windows.DependencyProperty IsInactiveSelectionHighlightEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsSelectionActiveProperty;
        public static readonly System.Windows.DependencyProperty MaxLengthProperty;
        public static readonly System.Windows.RoutedEvent PasswordChangedEvent;
        public static readonly System.Windows.DependencyProperty PasswordCharProperty;
        public static readonly System.Windows.DependencyProperty SelectionBrushProperty;
        public static readonly System.Windows.DependencyProperty SelectionOpacityProperty;
        public static readonly System.Windows.DependencyProperty SelectionTextBrushProperty;
        public PasswordBox() { }
        public System.Windows.Media.Brush CaretBrush { get { throw null; } set { } }
        public bool IsInactiveSelectionHighlightEnabled { get { throw null; } set { } }
        public bool IsSelectionActive { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int MaxLength { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Password { get { throw null; } set { } }
        public char PasswordChar { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Security.SecureString SecurePassword { get { throw null; } }
        public System.Windows.Media.Brush SelectionBrush { get { throw null; } set { } }
        public double SelectionOpacity { get { throw null; } set { } }
        public System.Windows.Media.Brush SelectionTextBrush { get { throw null; } set { } }
        public event System.Windows.RoutedEventHandler PasswordChanged { add { } remove { } }
        public void Clear() { }
        public override void OnApplyTemplate() { }
        protected override void OnContextMenuOpening(System.Windows.Controls.ContextMenuEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnDragEnter(System.Windows.DragEventArgs e) { }
        protected override void OnDragLeave(System.Windows.DragEventArgs e) { }
        protected override void OnDragOver(System.Windows.DragEventArgs e) { }
        protected override void OnDrop(System.Windows.DragEventArgs e) { }
        protected override void OnGiveFeedback(System.Windows.GiveFeedbackEventArgs e) { }
        protected override void OnGotKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnKeyUp(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnLostFocus(System.Windows.RoutedEventArgs e) { }
        protected override void OnLostKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e) { }
        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnQueryContinueDrag(System.Windows.QueryContinueDragEventArgs e) { }
        protected override void OnQueryCursor(System.Windows.Input.QueryCursorEventArgs e) { }
        protected override void OnTemplateChanged(System.Windows.Controls.ControlTemplate oldTemplate, System.Windows.Controls.ControlTemplate newTemplate) { }
        protected override void OnTextInput(System.Windows.Input.TextCompositionEventArgs e) { }
        public void Paste() { }
        public void SelectAll() { }
    }
    public partial class PrintDialog
    {
        public PrintDialog() { }
        public bool CurrentPageEnabled { get { throw null; } set { } }
        public uint MaxPage { get { throw null; } set { } }
        public uint MinPage { get { throw null; } set { } }
        public System.Windows.Controls.PageRange PageRange { get { throw null; } set { } }
        public System.Windows.Controls.PageRangeSelection PageRangeSelection { get { throw null; } set { } }
        public double PrintableAreaHeight { get { throw null; } }
        public double PrintableAreaWidth { get { throw null; } }
        public System.Printing.PrintQueue PrintQueue { get { throw null; } set { } }
        public System.Printing.PrintTicket PrintTicket { get { throw null; } set { } }
        public bool SelectedPagesEnabled { get { throw null; } set { } }
        public bool UserPageRangeEnabled { get { throw null; } set { } }
        public void PrintDocument(System.Windows.Documents.DocumentPaginator documentPaginator, string description) { }
        public void PrintVisual(System.Windows.Media.Visual visual, string description) { }
        public bool? ShowDialog() { throw null; }
    }
    public partial class PrintDialogException : System.Exception
    {
        public PrintDialogException() { }
        protected PrintDialogException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PrintDialogException(string message) { }
        public PrintDialogException(string message, System.Exception innerException) { }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_GlowRect", Type=typeof(System.Windows.FrameworkElement))]
    [System.Windows.TemplatePartAttribute(Name="PART_Indicator", Type=typeof(System.Windows.FrameworkElement))]
    [System.Windows.TemplatePartAttribute(Name="PART_Track", Type=typeof(System.Windows.FrameworkElement))]
    public partial class ProgressBar : System.Windows.Controls.Primitives.RangeBase
    {
        public static readonly System.Windows.DependencyProperty IsIndeterminateProperty;
        public static readonly System.Windows.DependencyProperty OrientationProperty;
        public ProgressBar() { }
        public bool IsIndeterminate { get { throw null; } set { } }
        public System.Windows.Controls.Orientation Orientation { get { throw null; } set { } }
        public override void OnApplyTemplate() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnMaximumChanged(double oldMaximum, double newMaximum) { }
        protected override void OnMinimumChanged(double oldMinimum, double newMinimum) { }
        protected override void OnValueChanged(double oldValue, double newValue) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.RadioButton)]
    public partial class RadioButton : System.Windows.Controls.Primitives.ToggleButton
    {
        public static readonly System.Windows.DependencyProperty GroupNameProperty;
        public RadioButton() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public string GroupName { get { throw null; } set { } }
        protected override void OnAccessKey(System.Windows.Input.AccessKeyEventArgs e) { }
        protected override void OnChecked(System.Windows.RoutedEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected internal override void OnToggle() { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Inherit)]
    [System.Windows.Markup.ContentPropertyAttribute("Document")]
    public partial class RichTextBox : System.Windows.Controls.Primitives.TextBoxBase, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty IsDocumentEnabledProperty;
        public RichTextBox() { }
        public RichTextBox(System.Windows.Documents.FlowDocument document) { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Documents.TextPointer CaretPosition { get { throw null; } set { } }
        public System.Windows.Documents.FlowDocument Document { get { throw null; } set { } }
        public bool IsDocumentEnabled { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public System.Windows.Documents.TextSelection Selection { get { throw null; } }
        public System.Windows.Documents.TextPointer GetNextSpellingErrorPosition(System.Windows.Documents.TextPointer position, System.Windows.Documents.LogicalDirection direction) { throw null; }
        public System.Windows.Documents.TextPointer GetPositionFromPoint(System.Windows.Point point, bool snapToText) { throw null; }
        public System.Windows.Controls.SpellingError GetSpellingError(System.Windows.Documents.TextPointer position) { throw null; }
        public System.Windows.Documents.TextRange GetSpellingErrorRange(System.Windows.Documents.TextPointer position) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnDpiChanged(System.Windows.DpiScale oldDpiScaleInfo, System.Windows.DpiScale newDpiScaleInfo) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeDocument() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class RowDefinition : System.Windows.Controls.DefinitionBase
    {
        public static readonly System.Windows.DependencyProperty HeightProperty;
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        public static readonly System.Windows.DependencyProperty MaxHeightProperty;
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        public static readonly System.Windows.DependencyProperty MinHeightProperty;
        public RowDefinition() { }
        public double ActualHeight { get { throw null; } }
        public System.Windows.GridLength Height { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double MaxHeight { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double MinHeight { get { throw null; } set { } }
        public double Offset { get { throw null; } }
    }
    public sealed partial class RowDefinitionCollection : System.Collections.Generic.ICollection<System.Windows.Controls.RowDefinition>, System.Collections.Generic.IEnumerable<System.Windows.Controls.RowDefinition>, System.Collections.Generic.IList<System.Windows.Controls.RowDefinition>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal RowDefinitionCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Windows.Controls.RowDefinition this[int index] { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(System.Windows.Controls.RowDefinition value) { }
        public void Clear() { }
        public bool Contains(System.Windows.Controls.RowDefinition value) { throw null; }
        public void CopyTo(System.Windows.Controls.RowDefinition[] array, int index) { }
        public int IndexOf(System.Windows.Controls.RowDefinition value) { throw null; }
        public void Insert(int index, System.Windows.Controls.RowDefinition value) { }
        public bool Remove(System.Windows.Controls.RowDefinition value) { throw null; }
        public void RemoveAt(int index) { }
        public void RemoveRange(int index, int count) { }
        System.Collections.Generic.IEnumerator<System.Windows.Controls.RowDefinition> System.Collections.Generic.IEnumerable<System.Windows.Controls.RowDefinition>.GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    public enum ScrollBarVisibility
    {
        Disabled = 0,
        Auto = 1,
        Hidden = 2,
        Visible = 3,
    }
    public partial class ScrollChangedEventArgs : System.Windows.RoutedEventArgs
    {
        internal ScrollChangedEventArgs() { }
        public double ExtentHeight { get { throw null; } }
        public double ExtentHeightChange { get { throw null; } }
        public double ExtentWidth { get { throw null; } }
        public double ExtentWidthChange { get { throw null; } }
        public double HorizontalChange { get { throw null; } }
        public double HorizontalOffset { get { throw null; } }
        public double VerticalChange { get { throw null; } }
        public double VerticalOffset { get { throw null; } }
        public double ViewportHeight { get { throw null; } }
        public double ViewportHeightChange { get { throw null; } }
        public double ViewportWidth { get { throw null; } }
        public double ViewportWidthChange { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void ScrollChangedEventHandler(object sender, System.Windows.Controls.ScrollChangedEventArgs e);
    public sealed partial class ScrollContentPresenter : System.Windows.Controls.ContentPresenter, System.Windows.Controls.Primitives.IScrollInfo
    {
        public static readonly System.Windows.DependencyProperty CanContentScrollProperty;
        public ScrollContentPresenter() { }
        public System.Windows.Documents.AdornerLayer AdornerLayer { get { throw null; } }
        public bool CanContentScroll { get { throw null; } set { } }
        public bool CanHorizontallyScroll { get { throw null; } set { } }
        public bool CanVerticallyScroll { get { throw null; } set { } }
        public double ExtentHeight { get { throw null; } }
        public double ExtentWidth { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public double HorizontalOffset { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.ScrollViewer ScrollOwner { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public double VerticalOffset { get { throw null; } }
        public double ViewportHeight { get { throw null; } }
        public double ViewportWidth { get { throw null; } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Media.Geometry GetLayoutClip(System.Windows.Size layoutSlotSize) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        public void LineDown() { }
        public void LineLeft() { }
        public void LineRight() { }
        public void LineUp() { }
        public System.Windows.Rect MakeVisible(System.Windows.Media.Visual visual, System.Windows.Rect rectangle) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        public void MouseWheelDown() { }
        public void MouseWheelLeft() { }
        public void MouseWheelRight() { }
        public void MouseWheelUp() { }
        public override void OnApplyTemplate() { }
        public void PageDown() { }
        public void PageLeft() { }
        public void PageRight() { }
        public void PageUp() { }
        public void SetHorizontalOffset(double offset) { }
        public void SetVerticalOffset(double offset) { }
    }
    public enum ScrollUnit
    {
        Pixel = 0,
        Item = 1,
    }
    [System.ComponentModel.DefaultEventAttribute("ScrollChangedEvent")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore)]
    [System.Windows.TemplatePartAttribute(Name="PART_HorizontalScrollBar", Type=typeof(System.Windows.Controls.Primitives.ScrollBar))]
    [System.Windows.TemplatePartAttribute(Name="PART_ScrollContentPresenter", Type=typeof(System.Windows.Controls.ScrollContentPresenter))]
    [System.Windows.TemplatePartAttribute(Name="PART_VerticalScrollBar", Type=typeof(System.Windows.Controls.Primitives.ScrollBar))]
    public partial class ScrollViewer : System.Windows.Controls.ContentControl
    {
        public static readonly System.Windows.DependencyProperty CanContentScrollProperty;
        public static readonly System.Windows.DependencyProperty ComputedHorizontalScrollBarVisibilityProperty;
        public static readonly System.Windows.DependencyProperty ComputedVerticalScrollBarVisibilityProperty;
        public static readonly System.Windows.DependencyProperty ContentHorizontalOffsetProperty;
        public static readonly System.Windows.DependencyProperty ContentVerticalOffsetProperty;
        public static readonly System.Windows.DependencyProperty ExtentHeightProperty;
        public static readonly System.Windows.DependencyProperty ExtentWidthProperty;
        public static readonly System.Windows.DependencyProperty HorizontalOffsetProperty;
        public static readonly System.Windows.DependencyProperty HorizontalScrollBarVisibilityProperty;
        public static readonly System.Windows.DependencyProperty IsDeferredScrollingEnabledProperty;
        public static readonly System.Windows.DependencyProperty PanningDecelerationProperty;
        public static readonly System.Windows.DependencyProperty PanningModeProperty;
        public static readonly System.Windows.DependencyProperty PanningRatioProperty;
        public static readonly System.Windows.DependencyProperty ScrollableHeightProperty;
        public static readonly System.Windows.DependencyProperty ScrollableWidthProperty;
        public static readonly System.Windows.RoutedEvent ScrollChangedEvent;
        public static readonly System.Windows.DependencyProperty VerticalOffsetProperty;
        public static readonly System.Windows.DependencyProperty VerticalScrollBarVisibilityProperty;
        public static readonly System.Windows.DependencyProperty ViewportHeightProperty;
        public static readonly System.Windows.DependencyProperty ViewportWidthProperty;
        public ScrollViewer() { }
        public bool CanContentScroll { get { throw null; } set { } }
        public System.Windows.Visibility ComputedHorizontalScrollBarVisibility { get { throw null; } }
        public System.Windows.Visibility ComputedVerticalScrollBarVisibility { get { throw null; } }
        public double ContentHorizontalOffset { get { throw null; } }
        public double ContentVerticalOffset { get { throw null; } }
        [System.ComponentModel.CategoryAttribute("Layout")]
        public double ExtentHeight { get { throw null; } }
        [System.ComponentModel.CategoryAttribute("Layout")]
        public double ExtentWidth { get { throw null; } }
        protected internal override bool HandlesScrolling { get { throw null; } }
        public double HorizontalOffset { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Controls.ScrollBarVisibility HorizontalScrollBarVisibility { get { throw null; } set { } }
        public bool IsDeferredScrollingEnabled { get { throw null; } set { } }
        public double PanningDeceleration { get { throw null; } set { } }
        public System.Windows.Controls.PanningMode PanningMode { get { throw null; } set { } }
        public double PanningRatio { get { throw null; } set { } }
        public double ScrollableHeight { get { throw null; } }
        public double ScrollableWidth { get { throw null; } }
        protected internal System.Windows.Controls.Primitives.IScrollInfo ScrollInfo { get { throw null; } set { } }
        public double VerticalOffset { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Controls.ScrollBarVisibility VerticalScrollBarVisibility { get { throw null; } set { } }
        [System.ComponentModel.CategoryAttribute("Layout")]
        public double ViewportHeight { get { throw null; } }
        [System.ComponentModel.CategoryAttribute("Layout")]
        public double ViewportWidth { get { throw null; } }
        [System.ComponentModel.CategoryAttribute("Action")]
        public event System.Windows.Controls.ScrollChangedEventHandler ScrollChanged { add { } remove { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        public static bool GetCanContentScroll(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.Controls.ScrollBarVisibility GetHorizontalScrollBarVisibility(System.Windows.DependencyObject element) { throw null; }
        public static bool GetIsDeferredScrollingEnabled(System.Windows.DependencyObject element) { throw null; }
        public static double GetPanningDeceleration(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.Controls.PanningMode GetPanningMode(System.Windows.DependencyObject element) { throw null; }
        public static double GetPanningRatio(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.Controls.ScrollBarVisibility GetVerticalScrollBarVisibility(System.Windows.DependencyObject element) { throw null; }
        protected override System.Windows.Media.HitTestResult HitTestCore(System.Windows.Media.PointHitTestParameters hitTestParameters) { throw null; }
        public void InvalidateScrollInfo() { }
        public void LineDown() { }
        public void LineLeft() { }
        public void LineRight() { }
        public void LineUp() { }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        public override void OnApplyTemplate() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnManipulationCompleted(System.Windows.Input.ManipulationCompletedEventArgs e) { }
        protected override void OnManipulationDelta(System.Windows.Input.ManipulationDeltaEventArgs e) { }
        protected override void OnManipulationInertiaStarting(System.Windows.Input.ManipulationInertiaStartingEventArgs e) { }
        protected override void OnManipulationStarting(System.Windows.Input.ManipulationStartingEventArgs e) { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e) { }
        protected virtual void OnScrollChanged(System.Windows.Controls.ScrollChangedEventArgs e) { }
        protected override void OnStylusSystemGesture(System.Windows.Input.StylusSystemGestureEventArgs e) { }
        public void PageDown() { }
        public void PageLeft() { }
        public void PageRight() { }
        public void PageUp() { }
        public void ScrollToBottom() { }
        public void ScrollToEnd() { }
        public void ScrollToHome() { }
        public void ScrollToHorizontalOffset(double offset) { }
        public void ScrollToLeftEnd() { }
        public void ScrollToRightEnd() { }
        public void ScrollToTop() { }
        public void ScrollToVerticalOffset(double offset) { }
        public static void SetCanContentScroll(System.Windows.DependencyObject element, bool canContentScroll) { }
        public static void SetHorizontalScrollBarVisibility(System.Windows.DependencyObject element, System.Windows.Controls.ScrollBarVisibility horizontalScrollBarVisibility) { }
        public static void SetIsDeferredScrollingEnabled(System.Windows.DependencyObject element, bool value) { }
        public static void SetPanningDeceleration(System.Windows.DependencyObject element, double value) { }
        public static void SetPanningMode(System.Windows.DependencyObject element, System.Windows.Controls.PanningMode panningMode) { }
        public static void SetPanningRatio(System.Windows.DependencyObject element, double value) { }
        public static void SetVerticalScrollBarVisibility(System.Windows.DependencyObject element, System.Windows.Controls.ScrollBarVisibility verticalScrollBarVisibility) { }
    }
    public partial class SelectedCellsChangedEventArgs : System.EventArgs
    {
        public SelectedCellsChangedEventArgs(System.Collections.Generic.List<System.Windows.Controls.DataGridCellInfo> addedCells, System.Collections.Generic.List<System.Windows.Controls.DataGridCellInfo> removedCells) { }
        public SelectedCellsChangedEventArgs(System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Controls.DataGridCellInfo> addedCells, System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Controls.DataGridCellInfo> removedCells) { }
        public System.Collections.Generic.IList<System.Windows.Controls.DataGridCellInfo> AddedCells { get { throw null; } }
        public System.Collections.Generic.IList<System.Windows.Controls.DataGridCellInfo> RemovedCells { get { throw null; } }
    }
    public delegate void SelectedCellsChangedEventHandler(object sender, System.Windows.Controls.SelectedCellsChangedEventArgs e);
    public sealed partial class SelectedDatesCollection : System.Collections.ObjectModel.ObservableCollection<System.DateTime>
    {
        public SelectedDatesCollection(System.Windows.Controls.Calendar owner) { }
        public void AddRange(System.DateTime start, System.DateTime end) { }
        protected override void ClearItems() { }
        protected override void InsertItem(int index, System.DateTime item) { }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, System.DateTime item) { }
    }
    public partial class SelectionChangedEventArgs : System.Windows.RoutedEventArgs
    {
        public SelectionChangedEventArgs(System.Windows.RoutedEvent id, System.Collections.IList removedItems, System.Collections.IList addedItems) { }
        public System.Collections.IList AddedItems { get { throw null; } }
        public System.Collections.IList RemovedItems { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void SelectionChangedEventHandler(object sender, System.Windows.Controls.SelectionChangedEventArgs e);
    public enum SelectionMode
    {
        Single = 0,
        Multiple = 1,
        Extended = 2,
    }
    public enum SelectiveScrollingOrientation
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        Both = 3,
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public partial class Separator : System.Windows.Controls.Control
    {
        public Separator() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
    }
    [System.ComponentModel.DefaultEventAttribute("ValueChanged")]
    [System.ComponentModel.DefaultPropertyAttribute("Value")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore)]
    [System.Windows.TemplatePartAttribute(Name="PART_SelectionRange", Type=typeof(System.Windows.FrameworkElement))]
    [System.Windows.TemplatePartAttribute(Name="PART_Track", Type=typeof(System.Windows.Controls.Primitives.Track))]
    public partial class Slider : System.Windows.Controls.Primitives.RangeBase
    {
        public static readonly System.Windows.DependencyProperty AutoToolTipPlacementProperty;
        public static readonly System.Windows.DependencyProperty AutoToolTipPrecisionProperty;
        public static readonly System.Windows.DependencyProperty DelayProperty;
        public static readonly System.Windows.DependencyProperty IntervalProperty;
        public static readonly System.Windows.DependencyProperty IsDirectionReversedProperty;
        public static readonly System.Windows.DependencyProperty IsMoveToPointEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsSelectionRangeEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsSnapToTickEnabledProperty;
        public static readonly System.Windows.DependencyProperty OrientationProperty;
        public static readonly System.Windows.DependencyProperty SelectionEndProperty;
        public static readonly System.Windows.DependencyProperty SelectionStartProperty;
        public static readonly System.Windows.DependencyProperty TickFrequencyProperty;
        public static readonly System.Windows.DependencyProperty TickPlacementProperty;
        public static readonly System.Windows.DependencyProperty TicksProperty;
        public Slider() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public System.Windows.Controls.Primitives.AutoToolTipPlacement AutoToolTipPlacement { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public int AutoToolTipPrecision { get { throw null; } set { } }
        public static System.Windows.Input.RoutedCommand DecreaseLarge { get { throw null; } }
        public static System.Windows.Input.RoutedCommand DecreaseSmall { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public int Delay { get { throw null; } set { } }
        public static System.Windows.Input.RoutedCommand IncreaseLarge { get { throw null; } }
        public static System.Windows.Input.RoutedCommand IncreaseSmall { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public int Interval { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsDirectionReversed { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public bool IsMoveToPointEnabled { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsSelectionRangeEnabled { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public bool IsSnapToTickEnabled { get { throw null; } set { } }
        public static System.Windows.Input.RoutedCommand MaximizeValue { get { throw null; } }
        public static System.Windows.Input.RoutedCommand MinimizeValue { get { throw null; } }
        public System.Windows.Controls.Orientation Orientation { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public double SelectionEnd { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public double SelectionStart { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public double TickFrequency { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Controls.Primitives.TickPlacement TickPlacement { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Media.DoubleCollection Ticks { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        public override void OnApplyTemplate() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDecreaseLarge() { }
        protected virtual void OnDecreaseSmall() { }
        protected virtual void OnIncreaseLarge() { }
        protected virtual void OnIncreaseSmall() { }
        protected virtual void OnMaximizeValue() { }
        protected override void OnMaximumChanged(double oldMaximum, double newMaximum) { }
        protected virtual void OnMinimizeValue() { }
        protected override void OnMinimumChanged(double oldMinimum, double newMinimum) { }
        protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected virtual void OnThumbDragCompleted(System.Windows.Controls.Primitives.DragCompletedEventArgs e) { }
        protected virtual void OnThumbDragDelta(System.Windows.Controls.Primitives.DragDeltaEventArgs e) { }
        protected virtual void OnThumbDragStarted(System.Windows.Controls.Primitives.DragStartedEventArgs e) { }
        protected override void OnValueChanged(double oldValue, double newValue) { }
    }
    public partial class SoundPlayerAction : System.Windows.TriggerAction, System.IDisposable
    {
        public static readonly System.Windows.DependencyProperty SourceProperty;
        public SoundPlayerAction() { }
        public System.Uri Source { get { throw null; } set { } }
        public void Dispose() { }
    }
    public sealed partial class SpellCheck
    {
        internal SpellCheck() { }
        public static readonly System.Windows.DependencyProperty CustomDictionariesProperty;
        public static readonly System.Windows.DependencyProperty IsEnabledProperty;
        public static readonly System.Windows.DependencyProperty SpellingReformProperty;
        public System.Collections.IList CustomDictionaries { get { throw null; } }
        public bool IsEnabled { get { throw null; } set { } }
        public System.Windows.Controls.SpellingReform SpellingReform { get { throw null; } set { } }
        public static System.Collections.IList GetCustomDictionaries(System.Windows.Controls.Primitives.TextBoxBase textBoxBase) { throw null; }
        public static bool GetIsEnabled(System.Windows.Controls.Primitives.TextBoxBase textBoxBase) { throw null; }
        public static void SetIsEnabled(System.Windows.Controls.Primitives.TextBoxBase textBoxBase, bool value) { }
        public static void SetSpellingReform(System.Windows.Controls.Primitives.TextBoxBase textBoxBase, System.Windows.Controls.SpellingReform value) { }
    }
    public partial class SpellingError
    {
        internal SpellingError() { }
        public System.Collections.Generic.IEnumerable<string> Suggestions { get { throw null; } }
        public void Correct(string correctedText) { }
        public void IgnoreAll() { }
    }
    public enum SpellingReform
    {
        PreAndPostreform = 0,
        Prereform = 1,
        Postreform = 2,
    }
    public partial class StackPanel : System.Windows.Controls.Panel, System.Windows.Controls.Primitives.IScrollInfo
    {
        public static readonly System.Windows.DependencyProperty OrientationProperty;
        public StackPanel() { }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool CanHorizontallyScroll { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool CanVerticallyScroll { get { throw null; } set { } }
        public double ExtentHeight { get { throw null; } }
        public double ExtentWidth { get { throw null; } }
        protected internal override bool HasLogicalOrientation { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public double HorizontalOffset { get { throw null; } }
        protected internal override System.Windows.Controls.Orientation LogicalOrientation { get { throw null; } }
        public System.Windows.Controls.Orientation Orientation { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.ScrollViewer ScrollOwner { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public double VerticalOffset { get { throw null; } }
        public double ViewportHeight { get { throw null; } }
        public double ViewportWidth { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        public void LineDown() { }
        public void LineLeft() { }
        public void LineRight() { }
        public void LineUp() { }
        public System.Windows.Rect MakeVisible(System.Windows.Media.Visual visual, System.Windows.Rect rectangle) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        public void MouseWheelDown() { }
        public void MouseWheelLeft() { }
        public void MouseWheelRight() { }
        public void MouseWheelUp() { }
        public void PageDown() { }
        public void PageLeft() { }
        public void PageRight() { }
        public void PageUp() { }
        public void SetHorizontalOffset(double offset) { }
        public void SetVerticalOffset(double offset) { }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_CloseButton", Type=typeof(System.Windows.Controls.Button))]
    [System.Windows.TemplatePartAttribute(Name="PART_ContentControl", Type=typeof(System.Windows.Controls.ContentControl))]
    [System.Windows.TemplatePartAttribute(Name="PART_CopyMenuItem", Type=typeof(System.Windows.Controls.MenuItem))]
    [System.Windows.TemplatePartAttribute(Name="PART_EraseMenuItem", Type=typeof(System.Windows.Controls.MenuItem))]
    [System.Windows.TemplatePartAttribute(Name="PART_IconButton", Type=typeof(System.Windows.Controls.Button))]
    [System.Windows.TemplatePartAttribute(Name="PART_InkMenuItem", Type=typeof(System.Windows.Controls.MenuItem))]
    [System.Windows.TemplatePartAttribute(Name="PART_PasteMenuItem", Type=typeof(System.Windows.Controls.MenuItem))]
    [System.Windows.TemplatePartAttribute(Name="PART_ResizeBottomRightThumb", Type=typeof(System.Windows.Controls.Primitives.Thumb))]
    [System.Windows.TemplatePartAttribute(Name="PART_SelectMenuItem", Type=typeof(System.Windows.Controls.MenuItem))]
    [System.Windows.TemplatePartAttribute(Name="PART_TitleThumb", Type=typeof(System.Windows.Controls.Primitives.Thumb))]
    public sealed partial class StickyNoteControl : System.Windows.Controls.Control
    {
        internal StickyNoteControl() { }
        public static readonly System.Windows.DependencyProperty AuthorProperty;
        public static readonly System.Windows.DependencyProperty CaptionFontFamilyProperty;
        public static readonly System.Windows.DependencyProperty CaptionFontSizeProperty;
        public static readonly System.Windows.DependencyProperty CaptionFontStretchProperty;
        public static readonly System.Windows.DependencyProperty CaptionFontStyleProperty;
        public static readonly System.Windows.DependencyProperty CaptionFontWeightProperty;
        public static readonly System.Windows.Input.RoutedCommand DeleteNoteCommand;
        public static readonly System.Windows.Input.RoutedCommand InkCommand;
        public static readonly System.Xml.XmlQualifiedName InkSchemaName;
        public static readonly System.Windows.DependencyProperty IsActiveProperty;
        public static readonly System.Windows.DependencyProperty IsExpandedProperty;
        public static readonly System.Windows.DependencyProperty IsMouseOverAnchorProperty;
        public static readonly System.Windows.DependencyProperty PenWidthProperty;
        public static readonly System.Windows.DependencyProperty StickyNoteTypeProperty;
        public static readonly System.Xml.XmlQualifiedName TextSchemaName;
        public System.Windows.Annotations.IAnchorInfo AnchorInfo { get { throw null; } }
        public string Author { get { throw null; } }
        public System.Windows.Media.FontFamily CaptionFontFamily { get { throw null; } set { } }
        public double CaptionFontSize { get { throw null; } set { } }
        public System.Windows.FontStretch CaptionFontStretch { get { throw null; } set { } }
        public System.Windows.FontStyle CaptionFontStyle { get { throw null; } set { } }
        public System.Windows.FontWeight CaptionFontWeight { get { throw null; } set { } }
        public bool IsActive { get { throw null; } }
        public bool IsExpanded { get { throw null; } set { } }
        public bool IsMouseOverAnchor { get { throw null; } }
        public double PenWidth { get { throw null; } set { } }
        public System.Windows.Controls.StickyNoteType StickyNoteType { get { throw null; } }
        public override void OnApplyTemplate() { }
        protected override void OnGotKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs args) { }
        protected override void OnIsKeyboardFocusWithinChanged(System.Windows.DependencyPropertyChangedEventArgs args) { }
        protected override void OnTemplateChanged(System.Windows.Controls.ControlTemplate oldTemplate, System.Windows.Controls.ControlTemplate newTemplate) { }
    }
    public enum StickyNoteType
    {
        Text = 0,
        Ink = 1,
    }
    public enum StretchDirection
    {
        UpOnly = 0,
        DownOnly = 1,
        Both = 2,
    }
    public partial class StyleSelector
    {
        public StyleSelector() { }
        public virtual System.Windows.Style SelectStyle(object item, System.Windows.DependencyObject container) { throw null; }
    }
    [System.Windows.StyleTypedPropertyAttribute(Property="ItemContainerStyle", StyleTargetType=typeof(System.Windows.Controls.TabItem))]
    [System.Windows.TemplatePartAttribute(Name="PART_SelectedContentHost", Type=typeof(System.Windows.Controls.ContentPresenter))]
    public partial class TabControl : System.Windows.Controls.Primitives.Selector
    {
        public static readonly System.Windows.DependencyProperty ContentStringFormatProperty;
        public static readonly System.Windows.DependencyProperty ContentTemplateProperty;
        public static readonly System.Windows.DependencyProperty ContentTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty SelectedContentProperty;
        public static readonly System.Windows.DependencyProperty SelectedContentStringFormatProperty;
        public static readonly System.Windows.DependencyProperty SelectedContentTemplateProperty;
        public static readonly System.Windows.DependencyProperty SelectedContentTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty TabStripPlacementProperty;
        public TabControl() { }
        public string ContentStringFormat { get { throw null; } set { } }
        public System.Windows.DataTemplate ContentTemplate { get { throw null; } set { } }
        public System.Windows.Controls.DataTemplateSelector ContentTemplateSelector { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public object SelectedContent { get { throw null; } }
        public string SelectedContentStringFormat { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.DataTemplate SelectedContentTemplate { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.DataTemplateSelector SelectedContentTemplateSelector { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public System.Windows.Controls.Dock TabStripPlacement { get { throw null; } set { } }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        public override void OnApplyTemplate() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnInitialized(System.EventArgs e) { }
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnSelectionChanged(System.Windows.Controls.SelectionChangedEventArgs e) { }
    }
    [System.ComponentModel.DefaultEventAttribute("IsSelectedChanged")]
    public partial class TabItem : System.Windows.Controls.HeaderedContentControl
    {
        public static readonly System.Windows.DependencyProperty IsSelectedProperty;
        public static readonly System.Windows.DependencyProperty TabStripPlacementProperty;
        public TabItem() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsSelected { get { throw null; } set { } }
        public System.Windows.Controls.Dock TabStripPlacement { get { throw null; } }
        protected override void OnAccessKey(System.Windows.Input.AccessKeyEventArgs e) { }
        protected override void OnContentChanged(object oldContent, object newContent) { }
        protected override void OnContentTemplateChanged(System.Windows.DataTemplate oldContentTemplate, System.Windows.DataTemplate newContentTemplate) { }
        protected override void OnContentTemplateSelectorChanged(System.Windows.Controls.DataTemplateSelector oldContentTemplateSelector, System.Windows.Controls.DataTemplateSelector newContentTemplateSelector) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnPreviewGotKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e) { }
        protected virtual void OnSelected(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnUnselected(System.Windows.RoutedEventArgs e) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Text)]
    [System.Windows.Markup.ContentPropertyAttribute("Inlines")]
    public partial class TextBlock : System.Windows.FrameworkElement, System.IServiceProvider, System.Windows.IContentHost, System.Windows.Markup.IAddChild, System.Windows.Markup.IAddChildInternal
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty BaselineOffsetProperty;
        public static readonly System.Windows.DependencyProperty FontFamilyProperty;
        public static readonly System.Windows.DependencyProperty FontSizeProperty;
        public static readonly System.Windows.DependencyProperty FontStretchProperty;
        public static readonly System.Windows.DependencyProperty FontStyleProperty;
        public static readonly System.Windows.DependencyProperty FontWeightProperty;
        public static readonly System.Windows.DependencyProperty ForegroundProperty;
        public static readonly System.Windows.DependencyProperty IsHyphenationEnabledProperty;
        public static readonly System.Windows.DependencyProperty LineHeightProperty;
        public static readonly System.Windows.DependencyProperty LineStackingStrategyProperty;
        public static readonly System.Windows.DependencyProperty PaddingProperty;
        public static readonly System.Windows.DependencyProperty TextAlignmentProperty;
        public static readonly System.Windows.DependencyProperty TextDecorationsProperty;
        public static readonly System.Windows.DependencyProperty TextEffectsProperty;
        public static readonly System.Windows.DependencyProperty TextProperty;
        public static readonly System.Windows.DependencyProperty TextTrimmingProperty;
        public static readonly System.Windows.DependencyProperty TextWrappingProperty;
        public TextBlock() { }
        public TextBlock(System.Windows.Documents.Inline inline) { }
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        public double BaselineOffset { get { throw null; } set { } }
        public System.Windows.LineBreakCondition BreakAfter { get { throw null; } }
        public System.Windows.LineBreakCondition BreakBefore { get { throw null; } }
        public System.Windows.Documents.TextPointer ContentEnd { get { throw null; } }
        public System.Windows.Documents.TextPointer ContentStart { get { throw null; } }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Font)]
        public System.Windows.Media.FontFamily FontFamily { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.FontSizeConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None)]
        public double FontSize { get { throw null; } set { } }
        public System.Windows.FontStretch FontStretch { get { throw null; } set { } }
        public System.Windows.FontStyle FontStyle { get { throw null; } set { } }
        public System.Windows.FontWeight FontWeight { get { throw null; } set { } }
        public System.Windows.Media.Brush Foreground { get { throw null; } set { } }
        protected virtual System.Collections.Generic.IEnumerator<System.Windows.IInputElement> HostedElementsCore { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.InlineCollection Inlines { get { throw null; } }
        public bool IsHyphenationEnabled { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double LineHeight { get { throw null; } set { } }
        public System.Windows.LineStackingStrategy LineStackingStrategy { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public System.Windows.Thickness Padding { get { throw null; } set { } }
        System.Collections.Generic.IEnumerator<System.Windows.IInputElement> System.Windows.IContentHost.HostedElements { get { throw null; } }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Text)]
        public string Text { get { throw null; } set { } }
        public System.Windows.TextAlignment TextAlignment { get { throw null; } set { } }
        public System.Windows.TextDecorationCollection TextDecorations { get { throw null; } set { } }
        public System.Windows.Media.TextEffectCollection TextEffects { get { throw null; } set { } }
        public System.Windows.TextTrimming TextTrimming { get { throw null; } set { } }
        public System.Windows.TextWrapping TextWrapping { get { throw null; } set { } }
        public System.Windows.Documents.Typography Typography { get { throw null; } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected sealed override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        public static double GetBaselineOffset(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.Media.FontFamily GetFontFamily(System.Windows.DependencyObject element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.FontSizeConverter))]
        public static double GetFontSize(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.FontStretch GetFontStretch(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.FontStyle GetFontStyle(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.FontWeight GetFontWeight(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.Media.Brush GetForeground(System.Windows.DependencyObject element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public static double GetLineHeight(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.LineStackingStrategy GetLineStackingStrategy(System.Windows.DependencyObject element) { throw null; }
        public System.Windows.Documents.TextPointer GetPositionFromPoint(System.Windows.Point point, bool snapToText) { throw null; }
        protected virtual System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Rect> GetRectanglesCore(System.Windows.ContentElement child) { throw null; }
        public static System.Windows.TextAlignment GetTextAlignment(System.Windows.DependencyObject element) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected sealed override System.Windows.Media.HitTestResult HitTestCore(System.Windows.Media.PointHitTestParameters hitTestParameters) { throw null; }
        protected virtual System.Windows.IInputElement InputHitTestCore(System.Windows.Point point) { throw null; }
        protected sealed override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected virtual void OnChildDesiredSizeChangedCore(System.Windows.UIElement child) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected sealed override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected sealed override void OnRender(System.Windows.Media.DrawingContext ctx) { }
        public static void SetBaselineOffset(System.Windows.DependencyObject element, double value) { }
        public static void SetFontFamily(System.Windows.DependencyObject element, System.Windows.Media.FontFamily value) { }
        public static void SetFontSize(System.Windows.DependencyObject element, double value) { }
        public static void SetFontStretch(System.Windows.DependencyObject element, System.Windows.FontStretch value) { }
        public static void SetFontStyle(System.Windows.DependencyObject element, System.Windows.FontStyle value) { }
        public static void SetFontWeight(System.Windows.DependencyObject element, System.Windows.FontWeight value) { }
        public static void SetForeground(System.Windows.DependencyObject element, System.Windows.Media.Brush value) { }
        public static void SetLineHeight(System.Windows.DependencyObject element, double value) { }
        public static void SetLineStackingStrategy(System.Windows.DependencyObject element, System.Windows.LineStackingStrategy value) { }
        public static void SetTextAlignment(System.Windows.DependencyObject element, System.Windows.TextAlignment value) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeBaselineOffset() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeInlines(System.Windows.Markup.XamlDesignerSerializationManager manager) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeText() { throw null; }
        object System.IServiceProvider.GetService(System.Type serviceType) { throw null; }
        System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Rect> System.Windows.IContentHost.GetRectangles(System.Windows.ContentElement child) { throw null; }
        System.Windows.IInputElement System.Windows.IContentHost.InputHitTest(System.Windows.Point point) { throw null; }
        void System.Windows.IContentHost.OnChildDesiredSizeChanged(System.Windows.UIElement child) { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Text)]
    [System.Windows.Markup.ContentPropertyAttribute("Text")]
    public partial class TextBox : System.Windows.Controls.Primitives.TextBoxBase, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty CharacterCasingProperty;
        public static readonly System.Windows.DependencyProperty MaxLengthProperty;
        public static readonly System.Windows.DependencyProperty MaxLinesProperty;
        public static readonly System.Windows.DependencyProperty MinLinesProperty;
        public static readonly System.Windows.DependencyProperty TextAlignmentProperty;
        public static readonly System.Windows.DependencyProperty TextDecorationsProperty;
        public static readonly System.Windows.DependencyProperty TextProperty;
        public static readonly System.Windows.DependencyProperty TextWrappingProperty;
        public TextBox() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int CaretIndex { get { throw null; } set { } }
        public System.Windows.Controls.CharacterCasing CharacterCasing { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int LineCount { get { throw null; } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(0)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Modifiability=System.Windows.Modifiability.Unmodifiable)]
        public int MaxLength { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(2147483647)]
        public int MaxLines { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(1)]
        public int MinLines { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string SelectedText { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(0)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int SelectionLength { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(0)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int SelectionStart { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Text)]
        public string Text { get { throw null; } set { } }
        public System.Windows.TextAlignment TextAlignment { get { throw null; } set { } }
        public System.Windows.TextDecorationCollection TextDecorations { get { throw null; } set { } }
        public System.Windows.TextWrapping TextWrapping { get { throw null; } set { } }
        public System.Windows.Documents.Typography Typography { get { throw null; } }
        public void Clear() { }
        public int GetCharacterIndexFromLineIndex(int lineIndex) { throw null; }
        public int GetCharacterIndexFromPoint(System.Windows.Point point, bool snapToText) { throw null; }
        public int GetFirstVisibleLineIndex() { throw null; }
        public int GetLastVisibleLineIndex() { throw null; }
        public int GetLineIndexFromCharacterIndex(int charIndex) { throw null; }
        public int GetLineLength(int lineIndex) { throw null; }
        public string GetLineText(int lineIndex) { throw null; }
        public int GetNextSpellingErrorCharacterIndex(int charIndex, System.Windows.Documents.LogicalDirection direction) { throw null; }
        public System.Windows.Rect GetRectFromCharacterIndex(int charIndex) { throw null; }
        public System.Windows.Rect GetRectFromCharacterIndex(int charIndex, bool trailingEdge) { throw null; }
        public System.Windows.Controls.SpellingError GetSpellingError(int charIndex) { throw null; }
        public int GetSpellingErrorLength(int charIndex) { throw null; }
        public int GetSpellingErrorStart(int charIndex) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        public void ScrollToLine(int lineIndex) { }
        public void Select(int start, int length) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeText(System.Windows.Markup.XamlDesignerSerializationManager manager) { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class TextChange
    {
        internal TextChange() { }
        public int AddedLength { get { throw null; } }
        public int Offset { get { throw null; } }
        public int RemovedLength { get { throw null; } }
    }
    public partial class TextChangedEventArgs : System.Windows.RoutedEventArgs
    {
        public TextChangedEventArgs(System.Windows.RoutedEvent id, System.Windows.Controls.UndoAction action) { }
        public TextChangedEventArgs(System.Windows.RoutedEvent id, System.Windows.Controls.UndoAction action, System.Collections.Generic.ICollection<System.Windows.Controls.TextChange> changes) { }
        public System.Collections.Generic.ICollection<System.Windows.Controls.TextChange> Changes { get { throw null; } }
        public System.Windows.Controls.UndoAction UndoAction { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void TextChangedEventHandler(object sender, System.Windows.Controls.TextChangedEventArgs e);
    public sealed partial class TextSearch : System.Windows.DependencyObject
    {
        internal TextSearch() { }
        public static readonly System.Windows.DependencyProperty TextPathProperty;
        public static readonly System.Windows.DependencyProperty TextProperty;
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static string GetText(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static string GetTextPath(System.Windows.DependencyObject element) { throw null; }
        public static void SetText(System.Windows.DependencyObject element, string text) { }
        public static void SetTextPath(System.Windows.DependencyObject element, string path) { }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_ToolBarOverflowPanel", Type=typeof(System.Windows.Controls.Primitives.ToolBarOverflowPanel))]
    [System.Windows.TemplatePartAttribute(Name="PART_ToolBarPanel", Type=typeof(System.Windows.Controls.Primitives.ToolBarPanel))]
    public partial class ToolBar : System.Windows.Controls.HeaderedItemsControl
    {
        public static readonly System.Windows.DependencyProperty BandIndexProperty;
        public static readonly System.Windows.DependencyProperty BandProperty;
        public static readonly System.Windows.DependencyProperty HasOverflowItemsProperty;
        public static readonly System.Windows.DependencyProperty IsOverflowItemProperty;
        public static readonly System.Windows.DependencyProperty IsOverflowOpenProperty;
        public static readonly System.Windows.DependencyProperty OrientationProperty;
        public static readonly System.Windows.DependencyProperty OverflowModeProperty;
        public ToolBar() { }
        public int Band { get { throw null; } set { } }
        public int BandIndex { get { throw null; } set { } }
        public static System.Windows.ResourceKey ButtonStyleKey { get { throw null; } }
        public static System.Windows.ResourceKey CheckBoxStyleKey { get { throw null; } }
        public static System.Windows.ResourceKey ComboBoxStyleKey { get { throw null; } }
        public bool HasOverflowItems { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsOverflowOpen { get { throw null; } set { } }
        public static System.Windows.ResourceKey MenuStyleKey { get { throw null; } }
        public System.Windows.Controls.Orientation Orientation { get { throw null; } }
        public static System.Windows.ResourceKey RadioButtonStyleKey { get { throw null; } }
        public static System.Windows.ResourceKey SeparatorStyleKey { get { throw null; } }
        public static System.Windows.ResourceKey TextBoxStyleKey { get { throw null; } }
        public static System.Windows.ResourceKey ToggleButtonStyleKey { get { throw null; } }
        public static bool GetIsOverflowItem(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute(IncludeDescendants=true)]
        public static System.Windows.Controls.OverflowMode GetOverflowMode(System.Windows.DependencyObject element) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnLostMouseCapture(System.Windows.Input.MouseEventArgs e) { }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        public static void SetOverflowMode(System.Windows.DependencyObject element, System.Windows.Controls.OverflowMode mode) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("ToolBars")]
    public partial class ToolBarTray : System.Windows.FrameworkElement, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty IsLockedProperty;
        public static readonly System.Windows.DependencyProperty OrientationProperty;
        public ToolBarTray() { }
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        public bool IsLocked { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public System.Windows.Controls.Orientation Orientation { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Collections.ObjectModel.Collection<System.Windows.Controls.ToolBar> ToolBars { get { throw null; } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        public static bool GetIsLocked(System.Windows.DependencyObject element) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext dc) { }
        public static void SetIsLocked(System.Windows.DependencyObject element, bool value) { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.ComponentModel.DefaultEventAttribute("Opened")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.ToolTip)]
    public partial class ToolTip : System.Windows.Controls.ContentControl
    {
        public static readonly System.Windows.RoutedEvent ClosedEvent;
        public static readonly System.Windows.DependencyProperty CustomPopupPlacementCallbackProperty;
        public static readonly System.Windows.DependencyProperty HasDropShadowProperty;
        public static readonly System.Windows.DependencyProperty HorizontalOffsetProperty;
        public static readonly System.Windows.DependencyProperty IsOpenProperty;
        public static readonly System.Windows.RoutedEvent OpenedEvent;
        public static readonly System.Windows.DependencyProperty PlacementProperty;
        public static readonly System.Windows.DependencyProperty PlacementRectangleProperty;
        public static readonly System.Windows.DependencyProperty PlacementTargetProperty;
        public static readonly System.Windows.DependencyProperty StaysOpenProperty;
        public static readonly System.Windows.DependencyProperty VerticalOffsetProperty;
        public ToolTip() { }
        [System.ComponentModel.BindableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.Controls.Primitives.CustomPopupPlacementCallback CustomPopupPlacementCallback { get { throw null; } set { } }
        public bool HasDropShadow { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double HorizontalOffset { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsOpen { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.Controls.Primitives.PlacementMode Placement { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.Rect PlacementRectangle { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.UIElement PlacementTarget { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public bool StaysOpen { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double VerticalOffset { get { throw null; } set { } }
        public event System.Windows.RoutedEventHandler Closed { add { } remove { } }
        public event System.Windows.RoutedEventHandler Opened { add { } remove { } }
        protected virtual void OnClosed(System.Windows.RoutedEventArgs e) { }
        protected override void OnContentChanged(object oldContent, object newContent) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnOpened(System.Windows.RoutedEventArgs e) { }
        protected internal override void OnVisualParentChanged(System.Windows.DependencyObject oldParent) { }
    }
    public sealed partial class ToolTipEventArgs : System.Windows.RoutedEventArgs
    {
        internal ToolTipEventArgs() { }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void ToolTipEventHandler(object sender, System.Windows.Controls.ToolTipEventArgs e);
    public static partial class ToolTipService
    {
        public static readonly System.Windows.DependencyProperty BetweenShowDelayProperty;
        public static readonly System.Windows.DependencyProperty HasDropShadowProperty;
        public static readonly System.Windows.DependencyProperty HorizontalOffsetProperty;
        public static readonly System.Windows.DependencyProperty InitialShowDelayProperty;
        public static readonly System.Windows.DependencyProperty IsEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsOpenProperty;
        public static readonly System.Windows.DependencyProperty PlacementProperty;
        public static readonly System.Windows.DependencyProperty PlacementRectangleProperty;
        public static readonly System.Windows.DependencyProperty PlacementTargetProperty;
        public static readonly System.Windows.DependencyProperty ShowDurationProperty;
        public static readonly System.Windows.DependencyProperty ShowOnDisabledProperty;
        public static readonly System.Windows.RoutedEvent ToolTipClosingEvent;
        public static readonly System.Windows.RoutedEvent ToolTipOpeningEvent;
        public static readonly System.Windows.DependencyProperty ToolTipProperty;
        public static readonly System.Windows.DependencyProperty VerticalOffsetProperty;
        public static void AddToolTipClosingHandler(System.Windows.DependencyObject element, System.Windows.Controls.ToolTipEventHandler handler) { }
        public static void AddToolTipOpeningHandler(System.Windows.DependencyObject element, System.Windows.Controls.ToolTipEventHandler handler) { }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static int GetBetweenShowDelay(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetHasDropShadow(System.Windows.DependencyObject element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static double GetHorizontalOffset(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static int GetInitialShowDelay(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetIsEnabled(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetIsOpen(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.Controls.Primitives.PlacementMode GetPlacement(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.Rect GetPlacementRectangle(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.UIElement GetPlacementTarget(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static int GetShowDuration(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetShowOnDisabled(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static object GetToolTip(System.Windows.DependencyObject element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static double GetVerticalOffset(System.Windows.DependencyObject element) { throw null; }
        public static void RemoveToolTipClosingHandler(System.Windows.DependencyObject element, System.Windows.Controls.ToolTipEventHandler handler) { }
        public static void RemoveToolTipOpeningHandler(System.Windows.DependencyObject element, System.Windows.Controls.ToolTipEventHandler handler) { }
        public static void SetBetweenShowDelay(System.Windows.DependencyObject element, int value) { }
        public static void SetHasDropShadow(System.Windows.DependencyObject element, bool value) { }
        public static void SetHorizontalOffset(System.Windows.DependencyObject element, double value) { }
        public static void SetInitialShowDelay(System.Windows.DependencyObject element, int value) { }
        public static void SetIsEnabled(System.Windows.DependencyObject element, bool value) { }
        public static void SetPlacement(System.Windows.DependencyObject element, System.Windows.Controls.Primitives.PlacementMode value) { }
        public static void SetPlacementRectangle(System.Windows.DependencyObject element, System.Windows.Rect value) { }
        public static void SetPlacementTarget(System.Windows.DependencyObject element, System.Windows.UIElement value) { }
        public static void SetShowDuration(System.Windows.DependencyObject element, int value) { }
        public static void SetShowOnDisabled(System.Windows.DependencyObject element, bool value) { }
        public static void SetToolTip(System.Windows.DependencyObject element, object value) { }
        public static void SetVerticalOffset(System.Windows.DependencyObject element, double value) { }
    }
    [System.Windows.StyleTypedPropertyAttribute(Property="ItemContainerStyle", StyleTargetType=typeof(System.Windows.Controls.TreeViewItem))]
    public partial class TreeView : System.Windows.Controls.ItemsControl
    {
        public static readonly System.Windows.RoutedEvent SelectedItemChangedEvent;
        public static readonly System.Windows.DependencyProperty SelectedItemProperty;
        public static readonly System.Windows.DependencyProperty SelectedValuePathProperty;
        public static readonly System.Windows.DependencyProperty SelectedValueProperty;
        public TreeView() { }
        protected internal override bool HandlesScrolling { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public object SelectedItem { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public object SelectedValue { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public string SelectedValuePath { get { throw null; } set { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedPropertyChangedEventHandler<object> SelectedItemChanged { add { } remove { } }
        protected virtual bool ExpandSubtree(System.Windows.Controls.TreeViewItem container) { throw null; }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnGotFocus(System.Windows.RoutedEventArgs e) { }
        protected override void OnIsKeyboardFocusWithinChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected virtual void OnSelectedItemChanged(System.Windows.RoutedPropertyChangedEventArgs<object> e) { }
    }
    [System.Windows.StyleTypedPropertyAttribute(Property="ItemContainerStyle", StyleTargetType=typeof(System.Windows.Controls.TreeViewItem))]
    [System.Windows.TemplatePartAttribute(Name="ItemsHost", Type=typeof(System.Windows.Controls.ItemsPresenter))]
    [System.Windows.TemplatePartAttribute(Name="PART_Header", Type=typeof(System.Windows.FrameworkElement))]
    public partial class TreeViewItem : System.Windows.Controls.HeaderedItemsControl, System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo
    {
        public static readonly System.Windows.RoutedEvent CollapsedEvent;
        public static readonly System.Windows.RoutedEvent ExpandedEvent;
        public static readonly System.Windows.DependencyProperty IsExpandedProperty;
        public static readonly System.Windows.DependencyProperty IsSelectedProperty;
        public static readonly System.Windows.DependencyProperty IsSelectionActiveProperty;
        public static readonly System.Windows.RoutedEvent SelectedEvent;
        public static readonly System.Windows.RoutedEvent UnselectedEvent;
        public TreeViewItem() { }
        public bool IsExpanded { get { throw null; } set { } }
        public bool IsSelected { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public bool IsSelectionActive { get { throw null; } }
        System.Windows.Controls.HierarchicalVirtualizationConstraints System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.Constraints { get { throw null; } set { } }
        System.Windows.Controls.HierarchicalVirtualizationHeaderDesiredSizes System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.HeaderDesiredSizes { get { throw null; } }
        bool System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.InBackgroundLayout { get { throw null; } set { } }
        System.Windows.Controls.HierarchicalVirtualizationItemDesiredSizes System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.ItemDesiredSizes { get { throw null; } set { } }
        System.Windows.Controls.Panel System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.ItemsHost { get { throw null; } }
        bool System.Windows.Controls.Primitives.IHierarchicalVirtualizationAndScrollInfo.MustDisableVirtualization { get { throw null; } set { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Collapsed { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Expanded { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Selected { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Unselected { add { } remove { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        public void ExpandSubtree() { }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        protected virtual void OnCollapsed(System.Windows.RoutedEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnExpanded(System.Windows.RoutedEventArgs e) { }
        protected override void OnGotFocus(System.Windows.RoutedEventArgs e) { }
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected virtual void OnSelected(System.Windows.RoutedEventArgs e) { }
        protected virtual void OnUnselected(System.Windows.RoutedEventArgs e) { }
        protected internal override void OnVisualParentChanged(System.Windows.DependencyObject oldParent) { }
    }
    public partial class UIElementCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public UIElementCollection(System.Windows.UIElement visualParent, System.Windows.FrameworkElement logicalParent) { }
        public virtual int Capacity { get { throw null; } set { } }
        public virtual int Count { get { throw null; } }
        public virtual bool IsSynchronized { get { throw null; } }
        public virtual System.Windows.UIElement this[int index] { get { throw null; } set { } }
        public virtual object SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public virtual int Add(System.Windows.UIElement element) { throw null; }
        public virtual void Clear() { }
        protected void ClearLogicalParent(System.Windows.UIElement element) { }
        public virtual bool Contains(System.Windows.UIElement element) { throw null; }
        public virtual void CopyTo(System.Array array, int index) { }
        public virtual void CopyTo(System.Windows.UIElement[] array, int index) { }
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        public virtual int IndexOf(System.Windows.UIElement element) { throw null; }
        public virtual void Insert(int index, System.Windows.UIElement element) { }
        public virtual void Remove(System.Windows.UIElement element) { }
        public virtual void RemoveAt(int index) { }
        public virtual void RemoveRange(int index, int count) { }
        protected void SetLogicalParent(System.Windows.UIElement element) { }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    public enum UndoAction
    {
        None = 0,
        Merge = 1,
        Undo = 2,
        Redo = 3,
        Clear = 4,
        Create = 5,
    }
    public partial class UserControl : System.Windows.Controls.ContentControl
    {
        public UserControl() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
    }
    public static partial class Validation
    {
        public static readonly System.Windows.RoutedEvent ErrorEvent;
        public static readonly System.Windows.DependencyProperty ErrorsProperty;
        public static readonly System.Windows.DependencyProperty ErrorTemplateProperty;
        public static readonly System.Windows.DependencyProperty HasErrorProperty;
        public static readonly System.Windows.DependencyProperty ValidationAdornerSiteForProperty;
        public static readonly System.Windows.DependencyProperty ValidationAdornerSiteProperty;
        public static void AddErrorHandler(System.Windows.DependencyObject element, System.EventHandler<System.Windows.Controls.ValidationErrorEventArgs> handler) { }
        public static void ClearInvalid(System.Windows.Data.BindingExpressionBase bindingExpression) { }
        public static System.Collections.ObjectModel.ReadOnlyObservableCollection<System.Windows.Controls.ValidationError> GetErrors(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.Controls.ControlTemplate GetErrorTemplate(System.Windows.DependencyObject element) { throw null; }
        public static bool GetHasError(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.DependencyObject GetValidationAdornerSite(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.DependencyObject GetValidationAdornerSiteFor(System.Windows.DependencyObject element) { throw null; }
        public static void MarkInvalid(System.Windows.Data.BindingExpressionBase bindingExpression, System.Windows.Controls.ValidationError validationError) { }
        public static void RemoveErrorHandler(System.Windows.DependencyObject element, System.EventHandler<System.Windows.Controls.ValidationErrorEventArgs> handler) { }
        public static void SetErrorTemplate(System.Windows.DependencyObject element, System.Windows.Controls.ControlTemplate value) { }
        public static void SetValidationAdornerSite(System.Windows.DependencyObject element, System.Windows.DependencyObject value) { }
        public static void SetValidationAdornerSiteFor(System.Windows.DependencyObject element, System.Windows.DependencyObject value) { }
    }
    public partial class ValidationError
    {
        public ValidationError(System.Windows.Controls.ValidationRule ruleInError, object bindingInError) { }
        public ValidationError(System.Windows.Controls.ValidationRule ruleInError, object bindingInError, object errorContent, System.Exception exception) { }
        public object BindingInError { get { throw null; } }
        public object ErrorContent { get { throw null; } set { } }
        public System.Exception Exception { get { throw null; } set { } }
        public System.Windows.Controls.ValidationRule RuleInError { get { throw null; } set { } }
    }
    public enum ValidationErrorEventAction
    {
        Added = 0,
        Removed = 1,
    }
    public partial class ValidationErrorEventArgs : System.Windows.RoutedEventArgs
    {
        internal ValidationErrorEventArgs() { }
        public System.Windows.Controls.ValidationErrorEventAction Action { get { throw null; } }
        public System.Windows.Controls.ValidationError Error { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public partial class ValidationResult
    {
        public ValidationResult(bool isValid, object errorContent) { }
        public object ErrorContent { get { throw null; } }
        public bool IsValid { get { throw null; } }
        public static System.Windows.Controls.ValidationResult ValidResult { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Controls.ValidationResult left, System.Windows.Controls.ValidationResult right) { throw null; }
        public static bool operator !=(System.Windows.Controls.ValidationResult left, System.Windows.Controls.ValidationResult right) { throw null; }
    }
    public abstract partial class ValidationRule
    {
        protected ValidationRule() { }
        protected ValidationRule(System.Windows.Controls.ValidationStep validationStep, bool validatesOnTargetUpdated) { }
        public bool ValidatesOnTargetUpdated { get { throw null; } set { } }
        public System.Windows.Controls.ValidationStep ValidationStep { get { throw null; } set { } }
        public abstract System.Windows.Controls.ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo);
        public virtual System.Windows.Controls.ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo, System.Windows.Data.BindingExpressionBase owner) { throw null; }
        public virtual System.Windows.Controls.ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo, System.Windows.Data.BindingGroup owner) { throw null; }
    }
    public enum ValidationStep
    {
        RawProposedValue = 0,
        ConvertedProposedValue = 1,
        UpdatedValue = 2,
        CommittedValue = 3,
    }
    public abstract partial class ViewBase : System.Windows.DependencyObject
    {
        protected ViewBase() { }
        protected internal virtual object DefaultStyleKey { get { throw null; } }
        protected internal virtual object ItemContainerDefaultStyleKey { get { throw null; } }
        protected internal virtual void ClearItem(System.Windows.Controls.ListViewItem item) { }
        protected internal virtual System.Windows.Automation.Peers.IViewAutomationPeer GetAutomationPeer(System.Windows.Controls.ListView parent) { throw null; }
        protected internal virtual void PrepareItem(System.Windows.Controls.ListViewItem item) { }
    }
    public partial class Viewbox : System.Windows.Controls.Decorator
    {
        public static readonly System.Windows.DependencyProperty StretchDirectionProperty;
        public static readonly System.Windows.DependencyProperty StretchProperty;
        public Viewbox() { }
        public override System.Windows.UIElement Child { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public System.Windows.Media.Stretch Stretch { get { throw null; } set { } }
        public System.Windows.Controls.StretchDirection StretchDirection { get { throw null; } set { } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
    [System.Windows.Markup.ContentPropertyAttribute("Children")]
    public partial class Viewport3D : System.Windows.FrameworkElement, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty CameraProperty;
        public static readonly System.Windows.DependencyProperty ChildrenProperty;
        public Viewport3D() { }
        public System.Windows.Media.Media3D.Camera Camera { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Media.Media3D.Visual3DCollection Children { get { throw null; } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Controls.VirtualizationCacheLengthConverter))]
    public partial struct VirtualizationCacheLength : System.IEquatable<System.Windows.Controls.VirtualizationCacheLength>
    {
        public VirtualizationCacheLength(double cacheBeforeAndAfterViewport) { throw null; }
        public VirtualizationCacheLength(double cacheBeforeViewport, double cacheAfterViewport) { throw null; }
        public double CacheAfterViewport { get { throw null; } }
        public double CacheBeforeViewport { get { throw null; } }
        public override bool Equals(object oCompare) { throw null; }
        public bool Equals(System.Windows.Controls.VirtualizationCacheLength cacheLength) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Controls.VirtualizationCacheLength cl1, System.Windows.Controls.VirtualizationCacheLength cl2) { throw null; }
        public static bool operator !=(System.Windows.Controls.VirtualizationCacheLength cl1, System.Windows.Controls.VirtualizationCacheLength cl2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class VirtualizationCacheLengthConverter : System.ComponentModel.TypeConverter
    {
        public VirtualizationCacheLengthConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object value, System.Type destinationType) { throw null; }
    }
    public enum VirtualizationCacheLengthUnit
    {
        Pixel = 0,
        Item = 1,
        Page = 2,
    }
    public enum VirtualizationMode
    {
        Standard = 0,
        Recycling = 1,
    }
    public abstract partial class VirtualizingPanel : System.Windows.Controls.Panel
    {
        public static readonly System.Windows.DependencyProperty CacheLengthProperty;
        public static readonly System.Windows.DependencyProperty CacheLengthUnitProperty;
        public static readonly System.Windows.DependencyProperty IsContainerVirtualizableProperty;
        public static readonly System.Windows.DependencyProperty IsVirtualizingProperty;
        public static readonly System.Windows.DependencyProperty IsVirtualizingWhenGroupingProperty;
        public static readonly System.Windows.DependencyProperty ScrollUnitProperty;
        public static readonly System.Windows.DependencyProperty VirtualizationModeProperty;
        protected VirtualizingPanel() { }
        public bool CanHierarchicallyScrollAndVirtualize { get { throw null; } }
        protected virtual bool CanHierarchicallyScrollAndVirtualizeCore { get { throw null; } }
        public System.Windows.Controls.Primitives.IItemContainerGenerator ItemContainerGenerator { get { throw null; } }
        protected void AddInternalChild(System.Windows.UIElement child) { }
        protected internal virtual void BringIndexIntoView(int index) { }
        public void BringIndexIntoViewPublic(int index) { }
        public static System.Windows.Controls.VirtualizationCacheLength GetCacheLength(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.Controls.VirtualizationCacheLengthUnit GetCacheLengthUnit(System.Windows.DependencyObject element) { throw null; }
        public static bool GetIsContainerVirtualizable(System.Windows.DependencyObject element) { throw null; }
        public static bool GetIsVirtualizing(System.Windows.DependencyObject element) { throw null; }
        public static bool GetIsVirtualizingWhenGrouping(System.Windows.DependencyObject element) { throw null; }
        public double GetItemOffset(System.Windows.UIElement child) { throw null; }
        protected virtual double GetItemOffsetCore(System.Windows.UIElement child) { throw null; }
        public static System.Windows.Controls.ScrollUnit GetScrollUnit(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.Controls.VirtualizationMode GetVirtualizationMode(System.Windows.DependencyObject element) { throw null; }
        protected void InsertInternalChild(int index, System.Windows.UIElement child) { }
        protected virtual void OnClearChildren() { }
        protected virtual void OnItemsChanged(object sender, System.Windows.Controls.Primitives.ItemsChangedEventArgs args) { }
        protected void RemoveInternalChildRange(int index, int range) { }
        public static void SetCacheLength(System.Windows.DependencyObject element, System.Windows.Controls.VirtualizationCacheLength value) { }
        public static void SetCacheLengthUnit(System.Windows.DependencyObject element, System.Windows.Controls.VirtualizationCacheLengthUnit value) { }
        public static void SetIsContainerVirtualizable(System.Windows.DependencyObject element, bool value) { }
        public static void SetIsVirtualizing(System.Windows.DependencyObject element, bool value) { }
        public static void SetIsVirtualizingWhenGrouping(System.Windows.DependencyObject element, bool value) { }
        public static void SetScrollUnit(System.Windows.DependencyObject element, System.Windows.Controls.ScrollUnit value) { }
        public static void SetVirtualizationMode(System.Windows.DependencyObject element, System.Windows.Controls.VirtualizationMode value) { }
        public bool ShouldItemsChangeAffectLayout(bool areItemChangesLocal, System.Windows.Controls.Primitives.ItemsChangedEventArgs args) { throw null; }
        protected virtual bool ShouldItemsChangeAffectLayoutCore(bool areItemChangesLocal, System.Windows.Controls.Primitives.ItemsChangedEventArgs args) { throw null; }
    }
    public partial class VirtualizingStackPanel : System.Windows.Controls.VirtualizingPanel, System.Windows.Controls.Primitives.IScrollInfo
    {
        public static readonly System.Windows.RoutedEvent CleanUpVirtualizedItemEvent;
        public static readonly new System.Windows.DependencyProperty IsVirtualizingProperty;
        public static readonly System.Windows.DependencyProperty OrientationProperty;
        public static readonly new System.Windows.DependencyProperty VirtualizationModeProperty;
        public VirtualizingStackPanel() { }
        protected override bool CanHierarchicallyScrollAndVirtualizeCore { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool CanHorizontallyScroll { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool CanVerticallyScroll { get { throw null; } set { } }
        public double ExtentHeight { get { throw null; } }
        public double ExtentWidth { get { throw null; } }
        protected internal override bool HasLogicalOrientation { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public double HorizontalOffset { get { throw null; } }
        protected internal override System.Windows.Controls.Orientation LogicalOrientation { get { throw null; } }
        public System.Windows.Controls.Orientation Orientation { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Controls.ScrollViewer ScrollOwner { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public double VerticalOffset { get { throw null; } }
        public double ViewportHeight { get { throw null; } }
        public double ViewportWidth { get { throw null; } }
        public static void AddCleanUpVirtualizedItemHandler(System.Windows.DependencyObject element, System.Windows.Controls.CleanUpVirtualizedItemEventHandler handler) { }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected internal override void BringIndexIntoView(int index) { }
        protected override double GetItemOffsetCore(System.Windows.UIElement child) { throw null; }
        public virtual void LineDown() { }
        public virtual void LineLeft() { }
        public virtual void LineRight() { }
        public virtual void LineUp() { }
        public System.Windows.Rect MakeVisible(System.Windows.Media.Visual visual, System.Windows.Rect rectangle) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        public virtual void MouseWheelDown() { }
        public virtual void MouseWheelLeft() { }
        public virtual void MouseWheelRight() { }
        public virtual void MouseWheelUp() { }
        protected virtual void OnCleanUpVirtualizedItem(System.Windows.Controls.CleanUpVirtualizedItemEventArgs e) { }
        protected override void OnClearChildren() { }
        protected override void OnItemsChanged(object sender, System.Windows.Controls.Primitives.ItemsChangedEventArgs args) { }
        protected virtual void OnViewportOffsetChanged(System.Windows.Vector oldViewportOffset, System.Windows.Vector newViewportOffset) { }
        protected virtual void OnViewportSizeChanged(System.Windows.Size oldViewportSize, System.Windows.Size newViewportSize) { }
        public virtual void PageDown() { }
        public virtual void PageLeft() { }
        public virtual void PageRight() { }
        public virtual void PageUp() { }
        public static void RemoveCleanUpVirtualizedItemHandler(System.Windows.DependencyObject element, System.Windows.Controls.CleanUpVirtualizedItemEventHandler handler) { }
        public void SetHorizontalOffset(double offset) { }
        public void SetVerticalOffset(double offset) { }
        protected override bool ShouldItemsChangeAffectLayoutCore(bool areItemChangesLocal, System.Windows.Controls.Primitives.ItemsChangedEventArgs args) { throw null; }
    }
    public sealed partial class WebBrowser : System.Windows.Interop.ActiveXHost
    {
        public WebBrowser() { }
        public bool CanGoBack { get { throw null; } }
        public bool CanGoForward { get { throw null; } }
        public object Document { get { throw null; } }
        public object ObjectForScripting { get { throw null; } set { } }
        public System.Uri Source { get { throw null; } set { } }
        public event System.Windows.Navigation.LoadCompletedEventHandler LoadCompleted { add { } remove { } }
        public event System.Windows.Navigation.NavigatedEventHandler Navigated { add { } remove { } }
        public event System.Windows.Navigation.NavigatingCancelEventHandler Navigating { add { } remove { } }
        public void GoBack() { }
        public void GoForward() { }
        public object InvokeScript(string scriptName) { throw null; }
        public object InvokeScript(string scriptName, params object[] args) { throw null; }
        public void Navigate(string source) { }
        public void Navigate(string source, string targetFrameName, byte[] postData, string additionalHeaders) { }
        public void Navigate(System.Uri source) { }
        public void Navigate(System.Uri source, string targetFrameName, byte[] postData, string additionalHeaders) { }
        public void NavigateToStream(System.IO.Stream stream) { }
        public void NavigateToString(string text) { }
        public void Refresh() { }
        public void Refresh(bool noCache) { }
        protected override bool TabIntoCore(System.Windows.Input.TraversalRequest request) { throw null; }
        protected override bool TranslateAcceleratorCore(ref System.Windows.Interop.MSG msg, System.Windows.Input.ModifierKeys modifiers) { throw null; }
    }
    public partial class WrapPanel : System.Windows.Controls.Panel
    {
        public static readonly System.Windows.DependencyProperty ItemHeightProperty;
        public static readonly System.Windows.DependencyProperty ItemWidthProperty;
        public static readonly System.Windows.DependencyProperty OrientationProperty;
        public WrapPanel() { }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double ItemHeight { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double ItemWidth { get { throw null; } set { } }
        public System.Windows.Controls.Orientation Orientation { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
    }
}
namespace System.Windows.Controls.Primitives
{
    public enum AutoToolTipPlacement
    {
        None = 0,
        TopLeft = 1,
        BottomRight = 2,
    }
    public partial class BulletDecorator : System.Windows.Controls.Decorator
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public BulletDecorator() { }
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        public System.Windows.UIElement Bullet { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext dc) { }
    }
    [System.ComponentModel.DefaultEventAttribute("Click")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Button)]
    public abstract partial class ButtonBase : System.Windows.Controls.ContentControl, System.Windows.Input.ICommandSource
    {
        public static readonly System.Windows.RoutedEvent ClickEvent;
        public static readonly System.Windows.DependencyProperty ClickModeProperty;
        public static readonly System.Windows.DependencyProperty CommandParameterProperty;
        public static readonly System.Windows.DependencyProperty CommandProperty;
        public static readonly System.Windows.DependencyProperty CommandTargetProperty;
        public static readonly System.Windows.DependencyProperty IsPressedProperty;
        protected ButtonBase() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public System.Windows.Controls.ClickMode ClickMode { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Action")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public System.Windows.Input.ICommand Command { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Action")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public object CommandParameter { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Action")]
        public System.Windows.IInputElement CommandTarget { get { throw null; } set { } }
        protected override bool IsEnabledCore { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public bool IsPressed { get { throw null; } protected set { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Click { add { } remove { } }
        protected override void OnAccessKey(System.Windows.Input.AccessKeyEventArgs e) { }
        protected virtual void OnClick() { }
        protected virtual void OnIsPressedChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnKeyUp(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnLostKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e) { }
        protected override void OnLostMouseCapture(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) { }
        protected internal override void OnRenderSizeChanged(System.Windows.SizeChangedInfo sizeInfo) { }
    }
    public sealed partial class CalendarButton : System.Windows.Controls.Button
    {
        public static readonly System.Windows.DependencyProperty HasSelectedDaysProperty;
        public static readonly System.Windows.DependencyProperty IsInactiveProperty;
        public CalendarButton() { }
        public bool HasSelectedDays { get { throw null; } }
        public bool IsInactive { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
    }
    public sealed partial class CalendarDayButton : System.Windows.Controls.Button
    {
        public static readonly System.Windows.DependencyProperty IsBlackedOutProperty;
        public static readonly System.Windows.DependencyProperty IsHighlightedProperty;
        public static readonly System.Windows.DependencyProperty IsInactiveProperty;
        public static readonly System.Windows.DependencyProperty IsSelectedProperty;
        public static readonly System.Windows.DependencyProperty IsTodayProperty;
        public CalendarDayButton() { }
        public bool IsBlackedOut { get { throw null; } }
        public bool IsHighlighted { get { throw null; } }
        public bool IsInactive { get { throw null; } }
        public bool IsSelected { get { throw null; } }
        public bool IsToday { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
    }
    [System.Windows.TemplatePartAttribute(Name="DayTitleTemplate", Type=typeof(System.Windows.DataTemplate))]
    [System.Windows.TemplatePartAttribute(Name="PART_DisabledVisual", Type=typeof(System.Windows.FrameworkElement))]
    [System.Windows.TemplatePartAttribute(Name="PART_HeaderButton", Type=typeof(System.Windows.Controls.Button))]
    [System.Windows.TemplatePartAttribute(Name="PART_MonthView", Type=typeof(System.Windows.Controls.Grid))]
    [System.Windows.TemplatePartAttribute(Name="PART_NextButton", Type=typeof(System.Windows.Controls.Button))]
    [System.Windows.TemplatePartAttribute(Name="PART_PreviousButton", Type=typeof(System.Windows.Controls.Button))]
    [System.Windows.TemplatePartAttribute(Name="PART_Root", Type=typeof(System.Windows.FrameworkElement))]
    [System.Windows.TemplatePartAttribute(Name="PART_YearView", Type=typeof(System.Windows.Controls.Grid))]
    public sealed partial class CalendarItem : System.Windows.Controls.Control
    {
        public CalendarItem() { }
        public static System.Windows.ComponentResourceKey DayTitleTemplateResourceKey { get { throw null; } }
        public override void OnApplyTemplate() { }
        protected override void OnLostMouseCapture(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e) { }
    }
    public partial struct CustomPopupPlacement
    {
        public CustomPopupPlacement(System.Windows.Point point, System.Windows.Controls.Primitives.PopupPrimaryAxis primaryAxis) { throw null; }
        public System.Windows.Point Point { get { throw null; } set { } }
        public System.Windows.Controls.Primitives.PopupPrimaryAxis PrimaryAxis { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Controls.Primitives.CustomPopupPlacement placement1, System.Windows.Controls.Primitives.CustomPopupPlacement placement2) { throw null; }
        public static bool operator !=(System.Windows.Controls.Primitives.CustomPopupPlacement placement1, System.Windows.Controls.Primitives.CustomPopupPlacement placement2) { throw null; }
    }
    public delegate System.Windows.Controls.Primitives.CustomPopupPlacement[] CustomPopupPlacementCallback(System.Windows.Size popupSize, System.Windows.Size targetSize, System.Windows.Point offset);
    public partial class DataGridCellsPresenter : System.Windows.Controls.ItemsControl
    {
        public DataGridCellsPresenter() { }
        public object Item { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override void ClearContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        public override void OnApplyTemplate() { }
        protected internal virtual void OnColumnsChanged(System.Collections.ObjectModel.ObservableCollection<System.Windows.Controls.DataGridColumn> columns, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected virtual void OnItemChanged(object oldItem, object newItem) { }
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) { }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_LeftHeaderGripper", Type=typeof(System.Windows.Controls.Primitives.Thumb))]
    [System.Windows.TemplatePartAttribute(Name="PART_RightHeaderGripper", Type=typeof(System.Windows.Controls.Primitives.Thumb))]
    public partial class DataGridColumnHeader : System.Windows.Controls.Primitives.ButtonBase
    {
        public static readonly System.Windows.DependencyProperty CanUserSortProperty;
        public static readonly System.Windows.DependencyProperty DisplayIndexProperty;
        public static readonly System.Windows.DependencyProperty IsFrozenProperty;
        public static readonly System.Windows.DependencyProperty SeparatorBrushProperty;
        public static readonly System.Windows.DependencyProperty SeparatorVisibilityProperty;
        public static readonly System.Windows.DependencyProperty SortDirectionProperty;
        public DataGridColumnHeader() { }
        public bool CanUserSort { get { throw null; } }
        public System.Windows.Controls.DataGridColumn Column { get { throw null; } }
        public static System.Windows.ComponentResourceKey ColumnFloatingHeaderStyleKey { get { throw null; } }
        public static System.Windows.ComponentResourceKey ColumnHeaderDropSeparatorStyleKey { get { throw null; } }
        public int DisplayIndex { get { throw null; } }
        public bool IsFrozen { get { throw null; } }
        public System.Windows.Media.Brush SeparatorBrush { get { throw null; } set { } }
        public System.Windows.Visibility SeparatorVisibility { get { throw null; } set { } }
        public System.ComponentModel.ListSortDirection? SortDirection { get { throw null; } }
        public override void OnApplyTemplate() { }
        protected override void OnClick() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnLostMouseCapture(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) { }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_FillerColumnHeader", Type=typeof(System.Windows.Controls.Primitives.DataGridColumnHeader))]
    public partial class DataGridColumnHeadersPresenter : System.Windows.Controls.ItemsControl
    {
        public DataGridColumnHeadersPresenter() { }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override void ClearContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        protected override System.Windows.Media.Geometry GetLayoutClip(System.Windows.Size layoutSlotSize) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        public override void OnApplyTemplate() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
    }
    public partial class DataGridDetailsPresenter : System.Windows.Controls.ContentPresenter
    {
        public DataGridDetailsPresenter() { }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) { }
        protected internal override void OnVisualParentChanged(System.Windows.DependencyObject oldParent) { }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_BottomHeaderGripper", Type=typeof(System.Windows.Controls.Primitives.Thumb))]
    [System.Windows.TemplatePartAttribute(Name="PART_TopHeaderGripper", Type=typeof(System.Windows.Controls.Primitives.Thumb))]
    public partial class DataGridRowHeader : System.Windows.Controls.Primitives.ButtonBase
    {
        public static readonly System.Windows.DependencyProperty IsRowSelectedProperty;
        public static readonly System.Windows.DependencyProperty SeparatorBrushProperty;
        public static readonly System.Windows.DependencyProperty SeparatorVisibilityProperty;
        public DataGridRowHeader() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsRowSelected { get { throw null; } }
        public System.Windows.Media.Brush SeparatorBrush { get { throw null; } set { } }
        public System.Windows.Visibility SeparatorVisibility { get { throw null; } set { } }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        public override void OnApplyTemplate() { }
        protected override void OnClick() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
    }
    public partial class DataGridRowsPresenter : System.Windows.Controls.VirtualizingStackPanel
    {
        public DataGridRowsPresenter() { }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override void OnCleanUpVirtualizedItem(System.Windows.Controls.CleanUpVirtualizedItemEventArgs e) { }
        protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost) { }
        protected override void OnViewportSizeChanged(System.Windows.Size oldViewportSize, System.Windows.Size newViewportSize) { }
    }
    [System.Windows.TemplatePartAttribute(Name="PART_Watermark", Type=typeof(System.Windows.Controls.ContentControl))]
    public sealed partial class DatePickerTextBox : System.Windows.Controls.TextBox
    {
        public DatePickerTextBox() { }
        public override void OnApplyTemplate() { }
        protected override void OnGotFocus(System.Windows.RoutedEventArgs e) { }
    }
    public partial class DocumentPageView : System.Windows.FrameworkElement, System.IDisposable, System.IServiceProvider
    {
        public static readonly System.Windows.DependencyProperty PageNumberProperty;
        public static readonly System.Windows.DependencyProperty StretchDirectionProperty;
        public static readonly System.Windows.DependencyProperty StretchProperty;
        public DocumentPageView() { }
        public System.Windows.Documents.DocumentPage DocumentPage { get { throw null; } }
        public System.Windows.Documents.DocumentPaginator DocumentPaginator { get { throw null; } set { } }
        protected bool IsDisposed { get { throw null; } }
        public int PageNumber { get { throw null; } set { } }
        public System.Windows.Media.Stretch Stretch { get { throw null; } set { } }
        public System.Windows.Controls.StretchDirection StretchDirection { get { throw null; } set { } }
        protected override int VisualChildrenCount { get { throw null; } }
        public event System.EventHandler PageConnected { add { } remove { } }
        public event System.EventHandler PageDisconnected { add { } remove { } }
        protected sealed override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected void Dispose() { }
        protected object GetService(System.Type serviceType) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected sealed override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnDpiChanged(System.Windows.DpiScale oldDpiScaleInfo, System.Windows.DpiScale newDpiScaleInfo) { }
        void System.IDisposable.Dispose() { }
        object System.IServiceProvider.GetService(System.Type serviceType) { throw null; }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Document")]
    public abstract partial class DocumentViewerBase : System.Windows.Controls.Control, System.IServiceProvider, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty CanGoToNextPageProperty;
        protected static readonly System.Windows.DependencyPropertyKey CanGoToNextPagePropertyKey;
        public static readonly System.Windows.DependencyProperty CanGoToPreviousPageProperty;
        protected static readonly System.Windows.DependencyPropertyKey CanGoToPreviousPagePropertyKey;
        public static readonly System.Windows.DependencyProperty DocumentProperty;
        public static readonly System.Windows.DependencyProperty IsMasterPageProperty;
        public static readonly System.Windows.DependencyProperty MasterPageNumberProperty;
        protected static readonly System.Windows.DependencyPropertyKey MasterPageNumberPropertyKey;
        public static readonly System.Windows.DependencyProperty PageCountProperty;
        protected static readonly System.Windows.DependencyPropertyKey PageCountPropertyKey;
        protected DocumentViewerBase() { }
        public virtual bool CanGoToNextPage { get { throw null; } }
        public virtual bool CanGoToPreviousPage { get { throw null; } }
        public System.Windows.Documents.IDocumentPaginatorSource Document { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public virtual int MasterPageNumber { get { throw null; } }
        public int PageCount { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Controls.Primitives.DocumentPageView> PageViews { get { throw null; } }
        public event System.EventHandler PageViewsChanged { add { } remove { } }
        public void CancelPrint() { }
        public virtual bool CanGoToPage(int pageNumber) { throw null; }
        public void FirstPage() { }
        public static bool GetIsMasterPage(System.Windows.DependencyObject element) { throw null; }
        protected System.Windows.Controls.Primitives.DocumentPageView GetMasterPageView() { throw null; }
        protected virtual System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Controls.Primitives.DocumentPageView> GetPageViewsCollection(out bool changed) { throw null; }
        public void GoToPage(int pageNumber) { }
        protected void InvalidatePageViews() { }
        public void LastPage() { }
        public void NextPage() { }
        public override void OnApplyTemplate() { }
        protected virtual void OnBringIntoView(System.Windows.DependencyObject element, System.Windows.Rect rect, int pageNumber) { }
        protected virtual void OnCancelPrintCommand() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDocumentChanged() { }
        protected override void OnDpiChanged(System.Windows.DpiScale oldDpiScaleInfo, System.Windows.DpiScale newDpiScaleInfo) { }
        protected virtual void OnFirstPageCommand() { }
        protected virtual void OnGoToPageCommand(int pageNumber) { }
        protected virtual void OnLastPageCommand() { }
        protected virtual void OnMasterPageNumberChanged() { }
        protected virtual void OnNextPageCommand() { }
        protected virtual void OnPageViewsChanged() { }
        protected virtual void OnPreviousPageCommand() { }
        protected virtual void OnPrintCommand() { }
        public void PreviousPage() { }
        public void Print() { }
        public static void SetIsMasterPage(System.Windows.DependencyObject element, bool value) { }
        object System.IServiceProvider.GetService(System.Type serviceType) { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class DragCompletedEventArgs : System.Windows.RoutedEventArgs
    {
        public DragCompletedEventArgs(double horizontalChange, double verticalChange, bool canceled) { }
        public bool Canceled { get { throw null; } }
        public double HorizontalChange { get { throw null; } }
        public double VerticalChange { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void DragCompletedEventHandler(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e);
    public partial class DragDeltaEventArgs : System.Windows.RoutedEventArgs
    {
        public DragDeltaEventArgs(double horizontalChange, double verticalChange) { }
        public double HorizontalChange { get { throw null; } }
        public double VerticalChange { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void DragDeltaEventHandler(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e);
    public partial class DragStartedEventArgs : System.Windows.RoutedEventArgs
    {
        public DragStartedEventArgs(double horizontalOffset, double verticalOffset) { }
        public double HorizontalOffset { get { throw null; } }
        public double VerticalOffset { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void DragStartedEventHandler(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e);
    public enum GeneratorDirection
    {
        Forward = 0,
        Backward = 1,
    }
    public partial struct GeneratorPosition
    {
        public GeneratorPosition(int index, int offset) { throw null; }
        public int Index { get { throw null; } set { } }
        public int Offset { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Controls.Primitives.GeneratorPosition gp1, System.Windows.Controls.Primitives.GeneratorPosition gp2) { throw null; }
        public static bool operator !=(System.Windows.Controls.Primitives.GeneratorPosition gp1, System.Windows.Controls.Primitives.GeneratorPosition gp2) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum GeneratorStatus
    {
        NotStarted = 0,
        GeneratingContainers = 1,
        ContainersGenerated = 2,
        Error = 3,
    }
    public abstract partial class GridViewRowPresenterBase : System.Windows.FrameworkElement, System.Windows.IWeakEventListener
    {
        public static readonly System.Windows.DependencyProperty ColumnsProperty;
        protected GridViewRowPresenterBase() { }
        public System.Windows.Controls.GridViewColumnCollection Columns { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        bool System.Windows.IWeakEventListener.ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs args) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial interface IContainItemStorage
    {
        void Clear();
        void ClearItemValue(object item, System.Windows.DependencyProperty dp);
        void ClearValue(System.Windows.DependencyProperty dp);
        object ReadItemValue(object item, System.Windows.DependencyProperty dp);
        void StoreItemValue(object item, System.Windows.DependencyProperty dp, object value);
    }
    public partial interface IHierarchicalVirtualizationAndScrollInfo
    {
        System.Windows.Controls.HierarchicalVirtualizationConstraints Constraints { get; set; }
        System.Windows.Controls.HierarchicalVirtualizationHeaderDesiredSizes HeaderDesiredSizes { get; }
        bool InBackgroundLayout { get; set; }
        System.Windows.Controls.HierarchicalVirtualizationItemDesiredSizes ItemDesiredSizes { get; set; }
        System.Windows.Controls.Panel ItemsHost { get; }
        bool MustDisableVirtualization { get; set; }
    }
    public partial interface IItemContainerGenerator
    {
        System.Windows.DependencyObject GenerateNext();
        System.Windows.DependencyObject GenerateNext(out bool isNewlyRealized);
        System.Windows.Controls.Primitives.GeneratorPosition GeneratorPositionFromIndex(int itemIndex);
        System.Windows.Controls.ItemContainerGenerator GetItemContainerGeneratorForPanel(System.Windows.Controls.Panel panel);
        int IndexFromGeneratorPosition(System.Windows.Controls.Primitives.GeneratorPosition position);
        void PrepareItemContainer(System.Windows.DependencyObject container);
        void Remove(System.Windows.Controls.Primitives.GeneratorPosition position, int count);
        void RemoveAll();
        System.IDisposable StartAt(System.Windows.Controls.Primitives.GeneratorPosition position, System.Windows.Controls.Primitives.GeneratorDirection direction);
        System.IDisposable StartAt(System.Windows.Controls.Primitives.GeneratorPosition position, System.Windows.Controls.Primitives.GeneratorDirection direction, bool allowStartAtRealizedItem);
    }
    public partial interface IRecyclingItemContainerGenerator : System.Windows.Controls.Primitives.IItemContainerGenerator
    {
        void Recycle(System.Windows.Controls.Primitives.GeneratorPosition position, int count);
    }
    public partial interface IScrollInfo
    {
        bool CanHorizontallyScroll { get; set; }
        bool CanVerticallyScroll { get; set; }
        double ExtentHeight { get; }
        double ExtentWidth { get; }
        double HorizontalOffset { get; }
        System.Windows.Controls.ScrollViewer ScrollOwner { get; set; }
        double VerticalOffset { get; }
        double ViewportHeight { get; }
        double ViewportWidth { get; }
        void LineDown();
        void LineLeft();
        void LineRight();
        void LineUp();
        System.Windows.Rect MakeVisible(System.Windows.Media.Visual visual, System.Windows.Rect rectangle);
        void MouseWheelDown();
        void MouseWheelLeft();
        void MouseWheelRight();
        void MouseWheelUp();
        void PageDown();
        void PageLeft();
        void PageRight();
        void PageUp();
        void SetHorizontalOffset(double offset);
        void SetVerticalOffset(double offset);
    }
    public partial class ItemsChangedEventArgs : System.EventArgs
    {
        internal ItemsChangedEventArgs() { }
        public System.Collections.Specialized.NotifyCollectionChangedAction Action { get { throw null; } }
        public int ItemCount { get { throw null; } }
        public int ItemUICount { get { throw null; } }
        public System.Windows.Controls.Primitives.GeneratorPosition OldPosition { get { throw null; } }
        public System.Windows.Controls.Primitives.GeneratorPosition Position { get { throw null; } }
    }
    public delegate void ItemsChangedEventHandler(object sender, System.Windows.Controls.Primitives.ItemsChangedEventArgs e);
    public static partial class LayoutInformation
    {
        public static System.Windows.Media.Geometry GetLayoutClip(System.Windows.FrameworkElement element) { throw null; }
        public static System.Windows.UIElement GetLayoutExceptionElement(System.Windows.Threading.Dispatcher dispatcher) { throw null; }
        public static System.Windows.Rect GetLayoutSlot(System.Windows.FrameworkElement element) { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Menu)]
    [System.Windows.StyleTypedPropertyAttribute(Property="ItemContainerStyle", StyleTargetType=typeof(System.Windows.Controls.MenuItem))]
    public abstract partial class MenuBase : System.Windows.Controls.ItemsControl
    {
        public static readonly System.Windows.DependencyProperty ItemContainerTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty UsesItemContainerTemplateProperty;
        protected MenuBase() { }
        public System.Windows.Controls.ItemContainerTemplateSelector ItemContainerTemplateSelector { get { throw null; } set { } }
        public bool UsesItemContainerTemplate { get { throw null; } set { } }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        protected virtual void HandleMouseButton(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        protected override void OnIsKeyboardFocusWithinChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e) { }
    }
    public abstract partial class MultiSelector : System.Windows.Controls.Primitives.Selector
    {
        protected MultiSelector() { }
        protected bool CanSelectMultipleItems { get { throw null; } set { } }
        protected bool IsUpdatingSelectedItems { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Collections.IList SelectedItems { get { throw null; } }
        protected void BeginUpdateSelectedItems() { }
        protected void EndUpdateSelectedItems() { }
        public void SelectAll() { }
        public void UnselectAll() { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public enum PlacementMode
    {
        Absolute = 0,
        Relative = 1,
        Bottom = 2,
        Center = 3,
        Right = 4,
        AbsolutePoint = 5,
        RelativePoint = 6,
        Mouse = 7,
        MousePoint = 8,
        Left = 9,
        Top = 10,
        Custom = 11,
    }
    [System.ComponentModel.DefaultEventAttribute("Opened")]
    [System.ComponentModel.DefaultPropertyAttribute("Child")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None)]
    [System.Windows.Markup.ContentPropertyAttribute("Child")]
    public partial class Popup : System.Windows.FrameworkElement, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty AllowsTransparencyProperty;
        public static readonly System.Windows.DependencyProperty ChildProperty;
        public static readonly System.Windows.DependencyProperty CustomPopupPlacementCallbackProperty;
        public static readonly System.Windows.DependencyProperty HasDropShadowProperty;
        public static readonly System.Windows.DependencyProperty HorizontalOffsetProperty;
        public static readonly System.Windows.DependencyProperty IsOpenProperty;
        public static readonly System.Windows.DependencyProperty PlacementProperty;
        public static readonly System.Windows.DependencyProperty PlacementRectangleProperty;
        public static readonly System.Windows.DependencyProperty PlacementTargetProperty;
        public static readonly System.Windows.DependencyProperty PopupAnimationProperty;
        public static readonly System.Windows.DependencyProperty StaysOpenProperty;
        public static readonly System.Windows.DependencyProperty VerticalOffsetProperty;
        public Popup() { }
        public bool AllowsTransparency { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        public System.Windows.UIElement Child { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.Controls.Primitives.CustomPopupPlacementCallback CustomPopupPlacementCallback { get { throw null; } set { } }
        public bool HasDropShadow { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double HorizontalOffset { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsOpen { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.Controls.Primitives.PlacementMode Placement { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        public System.Windows.Rect PlacementRectangle { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.UIElement PlacementTarget { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Controls.Primitives.PopupAnimation PopupAnimation { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public bool StaysOpen { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Layout")]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double VerticalOffset { get { throw null; } set { } }
        public event System.EventHandler Closed { add { } remove { } }
        public event System.EventHandler Opened { add { } remove { } }
        public static void CreateRootPopup(System.Windows.Controls.Primitives.Popup popup, System.Windows.UIElement child) { }
        protected internal override System.Windows.DependencyObject GetUIParentCore() { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        protected virtual void OnClosed(System.EventArgs e) { }
        protected virtual void OnOpened(System.EventArgs e) { }
        protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnPreviewMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnPreviewMouseRightButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnPreviewMouseRightButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public enum PopupAnimation
    {
        None = 0,
        Fade = 1,
        Slide = 2,
        Scroll = 3,
    }
    public enum PopupPrimaryAxis
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
    }
    [System.ComponentModel.DefaultEventAttribute("ValueChanged")]
    [System.ComponentModel.DefaultPropertyAttribute("Value")]
    public abstract partial class RangeBase : System.Windows.Controls.Control
    {
        public static readonly System.Windows.DependencyProperty LargeChangeProperty;
        public static readonly System.Windows.DependencyProperty MaximumProperty;
        public static readonly System.Windows.DependencyProperty MinimumProperty;
        public static readonly System.Windows.DependencyProperty SmallChangeProperty;
        public static readonly System.Windows.RoutedEvent ValueChangedEvent;
        public static readonly System.Windows.DependencyProperty ValueProperty;
        protected RangeBase() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public double LargeChange { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public double Maximum { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public double Minimum { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public double SmallChange { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public double Value { get { throw null; } set { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedPropertyChangedEventHandler<double> ValueChanged { add { } remove { } }
        protected virtual void OnMaximumChanged(double oldMaximum, double newMaximum) { }
        protected virtual void OnMinimumChanged(double oldMinimum, double newMinimum) { }
        protected virtual void OnValueChanged(double oldValue, double newValue) { }
        public override string ToString() { throw null; }
    }
    public partial class RepeatButton : System.Windows.Controls.Primitives.ButtonBase
    {
        public static readonly System.Windows.DependencyProperty DelayProperty;
        public static readonly System.Windows.DependencyProperty IntervalProperty;
        public RepeatButton() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public int Delay { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public int Interval { get { throw null; } set { } }
        protected override void OnClick() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnKeyUp(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnLostMouseCapture(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
    }
    public partial class ResizeGrip : System.Windows.Controls.Control
    {
        public ResizeGrip() { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
    [System.Windows.TemplatePartAttribute(Name="PART_Track", Type=typeof(System.Windows.Controls.Primitives.Track))]
    public partial class ScrollBar : System.Windows.Controls.Primitives.RangeBase
    {
        public static readonly System.Windows.Input.RoutedCommand DeferScrollToHorizontalOffsetCommand;
        public static readonly System.Windows.Input.RoutedCommand DeferScrollToVerticalOffsetCommand;
        public static readonly System.Windows.Input.RoutedCommand LineDownCommand;
        public static readonly System.Windows.Input.RoutedCommand LineLeftCommand;
        public static readonly System.Windows.Input.RoutedCommand LineRightCommand;
        public static readonly System.Windows.Input.RoutedCommand LineUpCommand;
        public static readonly System.Windows.DependencyProperty OrientationProperty;
        public static readonly System.Windows.Input.RoutedCommand PageDownCommand;
        public static readonly System.Windows.Input.RoutedCommand PageLeftCommand;
        public static readonly System.Windows.Input.RoutedCommand PageRightCommand;
        public static readonly System.Windows.Input.RoutedCommand PageUpCommand;
        public static readonly System.Windows.RoutedEvent ScrollEvent;
        public static readonly System.Windows.Input.RoutedCommand ScrollHereCommand;
        public static readonly System.Windows.Input.RoutedCommand ScrollToBottomCommand;
        public static readonly System.Windows.Input.RoutedCommand ScrollToEndCommand;
        public static readonly System.Windows.Input.RoutedCommand ScrollToHomeCommand;
        public static readonly System.Windows.Input.RoutedCommand ScrollToHorizontalOffsetCommand;
        public static readonly System.Windows.Input.RoutedCommand ScrollToLeftEndCommand;
        public static readonly System.Windows.Input.RoutedCommand ScrollToRightEndCommand;
        public static readonly System.Windows.Input.RoutedCommand ScrollToTopCommand;
        public static readonly System.Windows.Input.RoutedCommand ScrollToVerticalOffsetCommand;
        public static readonly System.Windows.DependencyProperty ViewportSizeProperty;
        public ScrollBar() { }
        protected override bool IsEnabledCore { get { throw null; } }
        public System.Windows.Controls.Orientation Orientation { get { throw null; } set { } }
        public System.Windows.Controls.Primitives.Track Track { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public double ViewportSize { get { throw null; } set { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.Controls.Primitives.ScrollEventHandler Scroll { add { } remove { } }
        public override void OnApplyTemplate() { }
        protected override void OnContextMenuClosing(System.Windows.Controls.ContextMenuEventArgs e) { }
        protected override void OnContextMenuOpening(System.Windows.Controls.ContextMenuEventArgs e) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnPreviewMouseRightButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
    }
    public partial class ScrollEventArgs : System.Windows.RoutedEventArgs
    {
        public ScrollEventArgs(System.Windows.Controls.Primitives.ScrollEventType scrollEventType, double newValue) { }
        public double NewValue { get { throw null; } }
        public System.Windows.Controls.Primitives.ScrollEventType ScrollEventType { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void ScrollEventHandler(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e);
    public enum ScrollEventType
    {
        EndScroll = 0,
        First = 1,
        LargeDecrement = 2,
        LargeIncrement = 3,
        Last = 4,
        SmallDecrement = 5,
        SmallIncrement = 6,
        ThumbPosition = 7,
        ThumbTrack = 8,
    }
    public partial class SelectiveScrollingGrid : System.Windows.Controls.Grid
    {
        public static readonly System.Windows.DependencyProperty SelectiveScrollingOrientationProperty;
        public SelectiveScrollingGrid() { }
        public static System.Windows.Controls.SelectiveScrollingOrientation GetSelectiveScrollingOrientation(System.Windows.DependencyObject obj) { throw null; }
        public static void SetSelectiveScrollingOrientation(System.Windows.DependencyObject obj, System.Windows.Controls.SelectiveScrollingOrientation value) { }
    }
    [System.ComponentModel.DefaultEventAttribute("SelectionChanged")]
    [System.ComponentModel.DefaultPropertyAttribute("SelectedIndex")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public abstract partial class Selector : System.Windows.Controls.ItemsControl
    {
        public static readonly System.Windows.DependencyProperty IsSelectedProperty;
        public static readonly System.Windows.DependencyProperty IsSelectionActiveProperty;
        public static readonly System.Windows.DependencyProperty IsSynchronizedWithCurrentItemProperty;
        public static readonly System.Windows.RoutedEvent SelectedEvent;
        public static readonly System.Windows.DependencyProperty SelectedIndexProperty;
        public static readonly System.Windows.DependencyProperty SelectedItemProperty;
        public static readonly System.Windows.DependencyProperty SelectedValuePathProperty;
        public static readonly System.Windows.DependencyProperty SelectedValueProperty;
        public static readonly System.Windows.RoutedEvent SelectionChangedEvent;
        public static readonly System.Windows.RoutedEvent UnselectedEvent;
        protected Selector() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        [System.ComponentModel.TypeConverterAttribute("System.Windows.NullableBoolConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public bool? IsSynchronizedWithCurrentItem { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public int SelectedIndex { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public object SelectedItem { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public object SelectedValue { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public string SelectedValuePath { get { throw null; } set { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.Controls.SelectionChangedEventHandler SelectionChanged { add { } remove { } }
        public static void AddSelectedHandler(System.Windows.DependencyObject element, System.Windows.RoutedEventHandler handler) { }
        public static void AddUnselectedHandler(System.Windows.DependencyObject element, System.Windows.RoutedEventHandler handler) { }
        protected override void ClearContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static bool GetIsSelected(System.Windows.DependencyObject element) { throw null; }
        public static bool GetIsSelectionActive(System.Windows.DependencyObject element) { throw null; }
        protected override void OnInitialized(System.EventArgs e) { }
        protected override void OnIsKeyboardFocusWithinChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected override void OnItemsSourceChanged(System.Collections.IEnumerable oldValue, System.Collections.IEnumerable newValue) { }
        protected virtual void OnSelectionChanged(System.Windows.Controls.SelectionChangedEventArgs e) { }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        public static void RemoveSelectedHandler(System.Windows.DependencyObject element, System.Windows.RoutedEventHandler handler) { }
        public static void RemoveUnselectedHandler(System.Windows.DependencyObject element, System.Windows.RoutedEventHandler handler) { }
        public static void SetIsSelected(System.Windows.DependencyObject element, bool isSelected) { }
    }
    [System.Windows.StyleTypedPropertyAttribute(Property="ItemContainerStyle", StyleTargetType=typeof(System.Windows.Controls.Primitives.StatusBarItem))]
    public partial class StatusBar : System.Windows.Controls.ItemsControl
    {
        public static readonly System.Windows.DependencyProperty ItemContainerTemplateSelectorProperty;
        public static readonly System.Windows.DependencyProperty UsesItemContainerTemplateProperty;
        public StatusBar() { }
        public System.Windows.Controls.ItemContainerTemplateSelector ItemContainerTemplateSelector { get { throw null; } set { } }
        public static System.Windows.ResourceKey SeparatorStyleKey { get { throw null; } }
        public bool UsesItemContainerTemplate { get { throw null; } set { } }
        protected override System.Windows.DependencyObject GetContainerForItemOverride() { throw null; }
        protected override bool IsItemItsOwnContainerOverride(object item) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item) { }
        protected override bool ShouldApplyItemContainerStyle(System.Windows.DependencyObject container, object item) { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Inherit)]
    public partial class StatusBarItem : System.Windows.Controls.ContentControl
    {
        public StatusBarItem() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
    }
    public partial class TabPanel : System.Windows.Controls.Panel
    {
        public TabPanel() { }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Media.Geometry GetLayoutClip(System.Windows.Size layoutSlotSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Text)]
    [System.Windows.TemplatePartAttribute(Name="PART_ContentHost", Type=typeof(System.Windows.FrameworkElement))]
    public abstract partial class TextBoxBase : System.Windows.Controls.Control
    {
        internal TextBoxBase() { }
        public static readonly System.Windows.DependencyProperty AcceptsReturnProperty;
        public static readonly System.Windows.DependencyProperty AcceptsTabProperty;
        public static readonly System.Windows.DependencyProperty AutoWordSelectionProperty;
        public static readonly System.Windows.DependencyProperty CaretBrushProperty;
        public static readonly System.Windows.DependencyProperty HorizontalScrollBarVisibilityProperty;
        public static readonly System.Windows.DependencyProperty IsInactiveSelectionHighlightEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsReadOnlyCaretVisibleProperty;
        public static readonly System.Windows.DependencyProperty IsReadOnlyProperty;
        public static readonly System.Windows.DependencyProperty IsSelectionActiveProperty;
        public static readonly System.Windows.DependencyProperty IsUndoEnabledProperty;
        public static readonly System.Windows.DependencyProperty SelectionBrushProperty;
        public static readonly System.Windows.RoutedEvent SelectionChangedEvent;
        public static readonly System.Windows.DependencyProperty SelectionOpacityProperty;
        public static readonly System.Windows.DependencyProperty SelectionTextBrushProperty;
        public static readonly System.Windows.RoutedEvent TextChangedEvent;
        public static readonly System.Windows.DependencyProperty UndoLimitProperty;
        public static readonly System.Windows.DependencyProperty VerticalScrollBarVisibilityProperty;
        public bool AcceptsReturn { get { throw null; } set { } }
        public bool AcceptsTab { get { throw null; } set { } }
        public bool AutoWordSelection { get { throw null; } set { } }
        public bool CanRedo { get { throw null; } }
        public bool CanUndo { get { throw null; } }
        public System.Windows.Media.Brush CaretBrush { get { throw null; } set { } }
        public double ExtentHeight { get { throw null; } }
        public double ExtentWidth { get { throw null; } }
        public double HorizontalOffset { get { throw null; } }
        public System.Windows.Controls.ScrollBarVisibility HorizontalScrollBarVisibility { get { throw null; } set { } }
        public bool IsInactiveSelectionHighlightEnabled { get { throw null; } set { } }
        public bool IsReadOnly { get { throw null; } set { } }
        public bool IsReadOnlyCaretVisible { get { throw null; } set { } }
        public bool IsSelectionActive { get { throw null; } }
        public bool IsUndoEnabled { get { throw null; } set { } }
        public System.Windows.Media.Brush SelectionBrush { get { throw null; } set { } }
        public double SelectionOpacity { get { throw null; } set { } }
        public System.Windows.Media.Brush SelectionTextBrush { get { throw null; } set { } }
        public System.Windows.Controls.SpellCheck SpellCheck { get { throw null; } }
        public int UndoLimit { get { throw null; } set { } }
        public double VerticalOffset { get { throw null; } }
        public System.Windows.Controls.ScrollBarVisibility VerticalScrollBarVisibility { get { throw null; } set { } }
        public double ViewportHeight { get { throw null; } }
        public double ViewportWidth { get { throw null; } }
        public event System.Windows.RoutedEventHandler SelectionChanged { add { } remove { } }
        public event System.Windows.Controls.TextChangedEventHandler TextChanged { add { } remove { } }
        public void AppendText(string textData) { }
        public void BeginChange() { }
        public void Copy() { }
        public void Cut() { }
        public System.IDisposable DeclareChangeBlock() { throw null; }
        public void EndChange() { }
        public void LineDown() { }
        public void LineLeft() { }
        public void LineRight() { }
        public void LineUp() { }
        public void LockCurrentUndoUnit() { }
        public override void OnApplyTemplate() { }
        protected override void OnContextMenuOpening(System.Windows.Controls.ContextMenuEventArgs e) { }
        protected override void OnDragEnter(System.Windows.DragEventArgs e) { }
        protected override void OnDragLeave(System.Windows.DragEventArgs e) { }
        protected override void OnDragOver(System.Windows.DragEventArgs e) { }
        protected override void OnDrop(System.Windows.DragEventArgs e) { }
        protected override void OnGiveFeedback(System.Windows.GiveFeedbackEventArgs e) { }
        protected override void OnGotKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnKeyUp(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnLostFocus(System.Windows.RoutedEventArgs e) { }
        protected override void OnLostKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e) { }
        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) { }
        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e) { }
        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnQueryContinueDrag(System.Windows.QueryContinueDragEventArgs e) { }
        protected override void OnQueryCursor(System.Windows.Input.QueryCursorEventArgs e) { }
        protected virtual void OnSelectionChanged(System.Windows.RoutedEventArgs e) { }
        protected override void OnTemplateChanged(System.Windows.Controls.ControlTemplate oldTemplate, System.Windows.Controls.ControlTemplate newTemplate) { }
        protected virtual void OnTextChanged(System.Windows.Controls.TextChangedEventArgs e) { }
        protected override void OnTextInput(System.Windows.Input.TextCompositionEventArgs e) { }
        public void PageDown() { }
        public void PageLeft() { }
        public void PageRight() { }
        public void PageUp() { }
        public void Paste() { }
        public bool Redo() { throw null; }
        public void ScrollToEnd() { }
        public void ScrollToHome() { }
        public void ScrollToHorizontalOffset(double offset) { }
        public void ScrollToVerticalOffset(double offset) { }
        public void SelectAll() { }
        public bool Undo() { throw null; }
    }
    [System.ComponentModel.DefaultEventAttribute("DragDelta")]
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
    public partial class Thumb : System.Windows.Controls.Control
    {
        public static readonly System.Windows.RoutedEvent DragCompletedEvent;
        public static readonly System.Windows.RoutedEvent DragDeltaEvent;
        public static readonly System.Windows.RoutedEvent DragStartedEvent;
        public static readonly System.Windows.DependencyProperty IsDraggingProperty;
        public Thumb() { }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsDragging { get { throw null; } protected set { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.Controls.Primitives.DragCompletedEventHandler DragCompleted { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.Controls.Primitives.DragDeltaEventHandler DragDelta { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.Controls.Primitives.DragStartedEventHandler DragStarted { add { } remove { } }
        public void CancelDrag() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnDraggingChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public partial class TickBar : System.Windows.FrameworkElement
    {
        public static readonly System.Windows.DependencyProperty FillProperty;
        public static readonly System.Windows.DependencyProperty IsDirectionReversedProperty;
        public static readonly System.Windows.DependencyProperty IsSelectionRangeEnabledProperty;
        public static readonly System.Windows.DependencyProperty MaximumProperty;
        public static readonly System.Windows.DependencyProperty MinimumProperty;
        public static readonly System.Windows.DependencyProperty PlacementProperty;
        public static readonly System.Windows.DependencyProperty ReservedSpaceProperty;
        public static readonly System.Windows.DependencyProperty SelectionEndProperty;
        public static readonly System.Windows.DependencyProperty SelectionStartProperty;
        public static readonly System.Windows.DependencyProperty TickFrequencyProperty;
        public static readonly System.Windows.DependencyProperty TicksProperty;
        public TickBar() { }
        public System.Windows.Media.Brush Fill { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsDirectionReversed { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public bool IsSelectionRangeEnabled { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public double Maximum { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public double Minimum { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Controls.Primitives.TickBarPlacement Placement { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public double ReservedSpace { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public double SelectionEnd { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public double SelectionStart { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public double TickFrequency { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Appearance")]
        public System.Windows.Media.DoubleCollection Ticks { get { throw null; } set { } }
        protected override void OnRender(System.Windows.Media.DrawingContext dc) { }
    }
    public enum TickBarPlacement
    {
        Left = 0,
        Top = 1,
        Right = 2,
        Bottom = 3,
    }
    public enum TickPlacement
    {
        None = 0,
        TopLeft = 1,
        BottomRight = 2,
        Both = 3,
    }
    [System.ComponentModel.DefaultEventAttribute("Checked")]
    public partial class ToggleButton : System.Windows.Controls.Primitives.ButtonBase
    {
        public static readonly System.Windows.RoutedEvent CheckedEvent;
        public static readonly System.Windows.RoutedEvent IndeterminateEvent;
        public static readonly System.Windows.DependencyProperty IsCheckedProperty;
        public static readonly System.Windows.DependencyProperty IsThreeStateProperty;
        public static readonly System.Windows.RoutedEvent UncheckedEvent;
        public ToggleButton() { }
        [System.ComponentModel.CategoryAttribute("Appearance")]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.NullableBoolConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        public bool? IsChecked { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public bool IsThreeState { get { throw null; } set { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Checked { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Indeterminate { add { } remove { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Unchecked { add { } remove { } }
        protected virtual void OnChecked(System.Windows.RoutedEventArgs e) { }
        protected override void OnClick() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected virtual void OnIndeterminate(System.Windows.RoutedEventArgs e) { }
        protected internal virtual void OnToggle() { }
        protected virtual void OnUnchecked(System.Windows.RoutedEventArgs e) { }
        public override string ToString() { throw null; }
    }
    public partial class ToolBarOverflowPanel : System.Windows.Controls.Panel
    {
        public static readonly System.Windows.DependencyProperty WrapWidthProperty;
        public ToolBarOverflowPanel() { }
        public double WrapWidth { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeBounds) { throw null; }
        protected override System.Windows.Controls.UIElementCollection CreateUIElementCollection(System.Windows.FrameworkElement logicalParent) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
    }
    public partial class ToolBarPanel : System.Windows.Controls.StackPanel
    {
        public ToolBarPanel() { }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public partial class Track : System.Windows.FrameworkElement
    {
        public static readonly System.Windows.DependencyProperty IsDirectionReversedProperty;
        public static readonly System.Windows.DependencyProperty MaximumProperty;
        public static readonly System.Windows.DependencyProperty MinimumProperty;
        public static readonly System.Windows.DependencyProperty OrientationProperty;
        public static readonly System.Windows.DependencyProperty ValueProperty;
        public static readonly System.Windows.DependencyProperty ViewportSizeProperty;
        public Track() { }
        public System.Windows.Controls.Primitives.RepeatButton DecreaseRepeatButton { get { throw null; } set { } }
        public System.Windows.Controls.Primitives.RepeatButton IncreaseRepeatButton { get { throw null; } set { } }
        public bool IsDirectionReversed { get { throw null; } set { } }
        public double Maximum { get { throw null; } set { } }
        public double Minimum { get { throw null; } set { } }
        public System.Windows.Controls.Orientation Orientation { get { throw null; } set { } }
        public System.Windows.Controls.Primitives.Thumb Thumb { get { throw null; } set { } }
        public double Value { get { throw null; } set { } }
        public double ViewportSize { get { throw null; } set { } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        public virtual double ValueFromDistance(double horizontal, double vertical) { throw null; }
        public virtual double ValueFromPoint(System.Windows.Point pt) { throw null; }
    }
    public partial class UniformGrid : System.Windows.Controls.Panel
    {
        public static readonly System.Windows.DependencyProperty ColumnsProperty;
        public static readonly System.Windows.DependencyProperty FirstColumnProperty;
        public static readonly System.Windows.DependencyProperty RowsProperty;
        public UniformGrid() { }
        public int Columns { get { throw null; } set { } }
        public int FirstColumn { get { throw null; } set { } }
        public int Rows { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
    }
}
namespace System.Windows.Data
{
    public partial class Binding : System.Windows.Data.BindingBase
    {
        public static readonly object DoNothing;
        public const string IndexerName = "Item[]";
        public static readonly System.Windows.RoutedEvent SourceUpdatedEvent;
        public static readonly System.Windows.RoutedEvent TargetUpdatedEvent;
        public static readonly System.Windows.DependencyProperty XmlNamespaceManagerProperty;
        public Binding() { }
        public Binding(string path) { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public object AsyncState { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool BindsDirectlyToSource { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Data.IValueConverter Converter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.CultureInfoIetfLanguageTagConverter))]
        public System.Globalization.CultureInfo ConverterCulture { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public object ConverterParameter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string ElementName { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool IsAsync { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Windows.Data.BindingMode.Default)]
        public System.Windows.Data.BindingMode Mode { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool NotifyOnSourceUpdated { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool NotifyOnTargetUpdated { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool NotifyOnValidationError { get { throw null; } set { } }
        public System.Windows.PropertyPath Path { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Data.RelativeSource RelativeSource { get { throw null; } set { } }
        public object Source { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Data.UpdateSourceExceptionFilterCallback UpdateSourceExceptionFilter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Windows.Data.UpdateSourceTrigger.Default)]
        public System.Windows.Data.UpdateSourceTrigger UpdateSourceTrigger { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool ValidatesOnDataErrors { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool ValidatesOnExceptions { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool ValidatesOnNotifyDataErrors { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<System.Windows.Controls.ValidationRule> ValidationRules { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string XPath { get { throw null; } set { } }
        public static void AddSourceUpdatedHandler(System.Windows.DependencyObject element, System.EventHandler<System.Windows.Data.DataTransferEventArgs> handler) { }
        public static void AddTargetUpdatedHandler(System.Windows.DependencyObject element, System.EventHandler<System.Windows.Data.DataTransferEventArgs> handler) { }
        public static System.Xml.XmlNamespaceManager GetXmlNamespaceManager(System.Windows.DependencyObject target) { throw null; }
        public static void RemoveSourceUpdatedHandler(System.Windows.DependencyObject element, System.EventHandler<System.Windows.Data.DataTransferEventArgs> handler) { }
        public static void RemoveTargetUpdatedHandler(System.Windows.DependencyObject element, System.EventHandler<System.Windows.Data.DataTransferEventArgs> handler) { }
        public static void SetXmlNamespaceManager(System.Windows.DependencyObject target, System.Xml.XmlNamespaceManager value) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializePath() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeSource() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeValidationRules() { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Modifiability=System.Windows.Modifiability.Unmodifiable, Readability=System.Windows.Readability.Unreadable)]
    [System.Windows.Markup.MarkupExtensionReturnTypeAttribute(typeof(object))]
    public abstract partial class BindingBase : System.Windows.Markup.MarkupExtension
    {
        internal BindingBase() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string BindingGroupName { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int Delay { get { throw null; } set { } }
        public object FallbackValue { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string StringFormat { get { throw null; } set { } }
        public object TargetNullValue { get { throw null; } set { } }
        public sealed override object ProvideValue(System.IServiceProvider serviceProvider) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeFallbackValue() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeTargetNullValue() { throw null; }
    }
    public sealed partial class BindingExpression : System.Windows.Data.BindingExpressionBase, System.Windows.IWeakEventListener
    {
        internal BindingExpression() { }
        public object DataItem { get { throw null; } }
        public System.Windows.Data.Binding ParentBinding { get { throw null; } }
        public object ResolvedSource { get { throw null; } }
        public string ResolvedSourcePropertyName { get { throw null; } }
        bool System.Windows.IWeakEventListener.ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e) { throw null; }
        public override void UpdateSource() { }
        public override void UpdateTarget() { }
    }
    public abstract partial class BindingExpressionBase : System.Windows.Expression, System.Windows.IWeakEventListener
    {
        internal BindingExpressionBase() { }
        public System.Windows.Data.BindingGroup BindingGroup { get { throw null; } }
        public virtual bool HasError { get { throw null; } }
        public virtual bool HasValidationError { get { throw null; } }
        public bool IsDirty { get { throw null; } }
        public System.Windows.Data.BindingBase ParentBindingBase { get { throw null; } }
        public System.Windows.Data.BindingStatus Status { get { throw null; } }
        public System.Windows.DependencyObject Target { get { throw null; } }
        public System.Windows.DependencyProperty TargetProperty { get { throw null; } }
        public virtual System.Windows.Controls.ValidationError ValidationError { get { throw null; } }
        public virtual System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Controls.ValidationError> ValidationErrors { get { throw null; } }
        bool System.Windows.IWeakEventListener.ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e) { throw null; }
        public virtual void UpdateSource() { }
        public virtual void UpdateTarget() { }
        public bool ValidateWithoutUpdate() { throw null; }
    }
    public partial class BindingGroup : System.Windows.DependencyObject
    {
        public BindingGroup() { }
        public System.Collections.ObjectModel.Collection<System.Windows.Data.BindingExpressionBase> BindingExpressions { get { throw null; } }
        public bool CanRestoreValues { get { throw null; } }
        public bool HasValidationError { get { throw null; } }
        public bool IsDirty { get { throw null; } }
        public System.Collections.IList Items { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public bool NotifyOnValidationError { get { throw null; } set { } }
        public System.Windows.DependencyObject Owner { get { throw null; } }
        public bool SharesProposedValues { get { throw null; } set { } }
        public bool ValidatesOnNotifyDataError { get { throw null; } set { } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Controls.ValidationError> ValidationErrors { get { throw null; } }
        public System.Collections.ObjectModel.Collection<System.Windows.Controls.ValidationRule> ValidationRules { get { throw null; } }
        public void BeginEdit() { }
        public void CancelEdit() { }
        public bool CommitEdit() { throw null; }
        public object GetValue(object item, string propertyName) { throw null; }
        public bool TryGetValue(object item, string propertyName, out object value) { throw null; }
        public bool UpdateSources() { throw null; }
        public bool ValidateWithoutUpdate() { throw null; }
    }
    public sealed partial class BindingListCollectionView : System.Windows.Data.CollectionView, System.Collections.IComparer, System.ComponentModel.ICollectionViewLiveShaping, System.ComponentModel.IEditableCollectionView, System.ComponentModel.IItemProperties
    {
        public BindingListCollectionView(System.ComponentModel.IBindingList list) : base (default(System.Collections.IEnumerable)) { }
        public bool CanAddNew { get { throw null; } }
        public bool CanCancelEdit { get { throw null; } }
        public bool CanChangeLiveFiltering { get { throw null; } }
        public bool CanChangeLiveGrouping { get { throw null; } }
        public bool CanChangeLiveSorting { get { throw null; } }
        public bool CanCustomFilter { get { throw null; } }
        public override bool CanFilter { get { throw null; } }
        public override bool CanGroup { get { throw null; } }
        public bool CanRemove { get { throw null; } }
        public override bool CanSort { get { throw null; } }
        public override int Count { get { throw null; } }
        public object CurrentAddItem { get { throw null; } }
        public object CurrentEditItem { get { throw null; } }
        public string CustomFilter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Data.GroupDescriptionSelectorCallback GroupBySelector { get { throw null; } set { } }
        public override System.Collections.ObjectModel.ObservableCollection<System.ComponentModel.GroupDescription> GroupDescriptions { get { throw null; } }
        public override System.Collections.ObjectModel.ReadOnlyObservableCollection<object> Groups { get { throw null; } }
        public bool IsAddingNew { get { throw null; } }
        public bool IsDataInGroupOrder { get { throw null; } set { } }
        public bool IsEditingItem { get { throw null; } }
        public override bool IsEmpty { get { throw null; } }
        public bool? IsLiveFiltering { get { throw null; } set { } }
        public bool? IsLiveGrouping { get { throw null; } set { } }
        public bool? IsLiveSorting { get { throw null; } set { } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.ComponentModel.ItemPropertyInfo> ItemProperties { get { throw null; } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveFilteringProperties { get { throw null; } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveGroupingProperties { get { throw null; } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveSortingProperties { get { throw null; } }
        public System.ComponentModel.NewItemPlaceholderPosition NewItemPlaceholderPosition { get { throw null; } set { } }
        public override System.ComponentModel.SortDescriptionCollection SortDescriptions { get { throw null; } }
        public object AddNew() { throw null; }
        public void CancelEdit() { }
        public void CancelNew() { }
        public void CommitEdit() { }
        public void CommitNew() { }
        public override bool Contains(object item) { throw null; }
        public override void DetachFromSourceCollection() { }
        public void EditItem(object item) { }
        protected override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public override object GetItemAt(int index) { throw null; }
        public override int IndexOf(object item) { throw null; }
        public override bool MoveCurrentToPosition(int position) { throw null; }
        protected override void OnAllowsCrossThreadChangesChanged() { }
        [System.ObsoleteAttribute("Replaced by OnAllowsCrossThreadChangesChanged")]
        protected override void OnBeginChangeLogging(System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { }
        public override bool PassesFilter(object item) { throw null; }
        protected override void ProcessCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { }
        protected override void RefreshOverride() { }
        public void Remove(object item) { }
        public void RemoveAt(int index) { }
        int System.Collections.IComparer.Compare(object o1, object o2) { throw null; }
    }
    public enum BindingMode
    {
        TwoWay = 0,
        OneWay = 1,
        OneTime = 2,
        OneWayToSource = 3,
        Default = 4,
    }
    public static partial class BindingOperations
    {
        public static object DisconnectedSource { get { throw null; } }
        public static event System.EventHandler<System.Windows.Data.CollectionRegisteringEventArgs> CollectionRegistering { add { } remove { } }
        public static event System.EventHandler<System.Windows.Data.CollectionViewRegisteringEventArgs> CollectionViewRegistering { add { } remove { } }
        public static void AccessCollection(System.Collections.IEnumerable collection, System.Action accessMethod, bool writeAccess) { }
        public static void ClearAllBindings(System.Windows.DependencyObject target) { }
        public static void ClearBinding(System.Windows.DependencyObject target, System.Windows.DependencyProperty dp) { }
        public static void DisableCollectionSynchronization(System.Collections.IEnumerable collection) { }
        public static void EnableCollectionSynchronization(System.Collections.IEnumerable collection, object lockObject) { }
        public static void EnableCollectionSynchronization(System.Collections.IEnumerable collection, object context, System.Windows.Data.CollectionSynchronizationCallback synchronizationCallback) { }
        public static System.Windows.Data.Binding GetBinding(System.Windows.DependencyObject target, System.Windows.DependencyProperty dp) { throw null; }
        public static System.Windows.Data.BindingBase GetBindingBase(System.Windows.DependencyObject target, System.Windows.DependencyProperty dp) { throw null; }
        public static System.Windows.Data.BindingExpression GetBindingExpression(System.Windows.DependencyObject target, System.Windows.DependencyProperty dp) { throw null; }
        public static System.Windows.Data.BindingExpressionBase GetBindingExpressionBase(System.Windows.DependencyObject target, System.Windows.DependencyProperty dp) { throw null; }
        public static System.Windows.Data.MultiBinding GetMultiBinding(System.Windows.DependencyObject target, System.Windows.DependencyProperty dp) { throw null; }
        public static System.Windows.Data.MultiBindingExpression GetMultiBindingExpression(System.Windows.DependencyObject target, System.Windows.DependencyProperty dp) { throw null; }
        public static System.Windows.Data.PriorityBinding GetPriorityBinding(System.Windows.DependencyObject target, System.Windows.DependencyProperty dp) { throw null; }
        public static System.Windows.Data.PriorityBindingExpression GetPriorityBindingExpression(System.Windows.DependencyObject target, System.Windows.DependencyProperty dp) { throw null; }
        public static System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Data.BindingGroup> GetSourceUpdatingBindingGroups(System.Windows.DependencyObject root) { throw null; }
        public static System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Data.BindingExpressionBase> GetSourceUpdatingBindings(System.Windows.DependencyObject root) { throw null; }
        public static bool IsDataBound(System.Windows.DependencyObject target, System.Windows.DependencyProperty dp) { throw null; }
        public static System.Windows.Data.BindingExpressionBase SetBinding(System.Windows.DependencyObject target, System.Windows.DependencyProperty dp, System.Windows.Data.BindingBase binding) { throw null; }
    }
    public enum BindingStatus
    {
        Unattached = 0,
        Inactive = 1,
        Active = 2,
        Detached = 3,
        AsyncRequestPending = 4,
        PathError = 5,
        UpdateTargetError = 6,
        UpdateSourceError = 7,
    }
    public partial class CollectionContainer : System.Windows.DependencyObject, System.Collections.Specialized.INotifyCollectionChanged, System.Windows.IWeakEventListener
    {
        public static readonly System.Windows.DependencyProperty CollectionProperty;
        public CollectionContainer() { }
        public System.Collections.IEnumerable Collection { get { throw null; } set { } }
        protected virtual event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged { add { } remove { } }
        event System.Collections.Specialized.NotifyCollectionChangedEventHandler System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged { add { } remove { } }
        protected virtual void OnContainedCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { }
        protected virtual bool ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeCollection() { throw null; }
        bool System.Windows.IWeakEventListener.ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e) { throw null; }
    }
    public partial class CollectionRegisteringEventArgs : System.EventArgs
    {
        internal CollectionRegisteringEventArgs() { }
        public System.Collections.IEnumerable Collection { get { throw null; } }
        public object Parent { get { throw null; } }
    }
    public delegate void CollectionSynchronizationCallback(System.Collections.IEnumerable collection, object context, System.Action accessMethod, bool writeAccess);
    public partial class CollectionView : System.Windows.Threading.DispatcherObject, System.Collections.IEnumerable, System.Collections.Specialized.INotifyCollectionChanged, System.ComponentModel.ICollectionView, System.ComponentModel.INotifyPropertyChanged
    {
        public CollectionView(System.Collections.IEnumerable collection) { }
        protected bool AllowsCrossThreadChanges { get { throw null; } }
        public virtual bool CanFilter { get { throw null; } }
        public virtual bool CanGroup { get { throw null; } }
        public virtual bool CanSort { get { throw null; } }
        public virtual System.Collections.IComparer Comparer { get { throw null; } }
        public virtual int Count { get { throw null; } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.CultureInfoIetfLanguageTagConverter))]
        public virtual System.Globalization.CultureInfo Culture { get { throw null; } set { } }
        public virtual object CurrentItem { get { throw null; } }
        public virtual int CurrentPosition { get { throw null; } }
        public virtual System.Predicate<object> Filter { get { throw null; } set { } }
        public virtual System.Collections.ObjectModel.ObservableCollection<System.ComponentModel.GroupDescription> GroupDescriptions { get { throw null; } }
        public virtual System.Collections.ObjectModel.ReadOnlyObservableCollection<object> Groups { get { throw null; } }
        public virtual bool IsCurrentAfterLast { get { throw null; } }
        public virtual bool IsCurrentBeforeFirst { get { throw null; } }
        protected bool IsCurrentInSync { get { throw null; } }
        protected bool IsDynamic { get { throw null; } }
        public virtual bool IsEmpty { get { throw null; } }
        public virtual bool IsInUse { get { throw null; } }
        protected bool IsRefreshDeferred { get { throw null; } }
        public virtual bool NeedsRefresh { get { throw null; } }
        public static object NewItemPlaceholder { get { throw null; } }
        public virtual System.ComponentModel.SortDescriptionCollection SortDescriptions { get { throw null; } }
        public virtual System.Collections.IEnumerable SourceCollection { get { throw null; } }
        protected bool UpdatedOutsideDispatcher { get { throw null; } }
        protected virtual event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged { add { } remove { } }
        public virtual event System.EventHandler CurrentChanged { add { } remove { } }
        public virtual event System.ComponentModel.CurrentChangingEventHandler CurrentChanging { add { } remove { } }
        protected virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }
        event System.Collections.Specialized.NotifyCollectionChangedEventHandler System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged { add { } remove { } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        [System.ObsoleteAttribute("Replaced by ClearPendingChanges")]
        protected void ClearChangeLog() { }
        protected void ClearPendingChanges() { }
        public virtual bool Contains(object item) { throw null; }
        public virtual System.IDisposable DeferRefresh() { throw null; }
        public virtual void DetachFromSourceCollection() { }
        protected virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        public virtual object GetItemAt(int index) { throw null; }
        public virtual int IndexOf(object item) { throw null; }
        public virtual bool MoveCurrentTo(object item) { throw null; }
        public virtual bool MoveCurrentToFirst() { throw null; }
        public virtual bool MoveCurrentToLast() { throw null; }
        public virtual bool MoveCurrentToNext() { throw null; }
        public virtual bool MoveCurrentToPosition(int position) { throw null; }
        public virtual bool MoveCurrentToPrevious() { throw null; }
        protected bool OKToChangeCurrent() { throw null; }
        protected virtual void OnAllowsCrossThreadChangesChanged() { }
        [System.ObsoleteAttribute("Replaced by OnAllowsCrossThreadChangesChanged")]
        protected virtual void OnBeginChangeLogging(System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { }
        protected virtual void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { }
        protected void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { }
        protected virtual void OnCurrentChanged() { }
        protected void OnCurrentChanging() { }
        protected virtual void OnCurrentChanging(System.ComponentModel.CurrentChangingEventArgs args) { }
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e) { }
        public virtual bool PassesFilter(object item) { throw null; }
        protected virtual void ProcessCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { }
        protected void ProcessPendingChanges() { }
        public virtual void Refresh() { }
        protected void RefreshOrDefer() { }
        protected virtual void RefreshOverride() { }
        protected void SetCurrent(object newItem, int newPosition) { }
        protected void SetCurrent(object newItem, int newPosition, int count) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public abstract partial class CollectionViewGroup : System.ComponentModel.INotifyPropertyChanged
    {
        protected CollectionViewGroup(object name) { }
        public abstract bool IsBottomLevel { get; }
        public int ItemCount { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyObservableCollection<object> Items { get { throw null; } }
        public object Name { get { throw null; } }
        protected int ProtectedItemCount { get { throw null; } set { } }
        protected System.Collections.ObjectModel.ObservableCollection<object> ProtectedItems { get { throw null; } }
        protected virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e) { }
    }
    public partial class CollectionViewRegisteringEventArgs : System.EventArgs
    {
        internal CollectionViewRegisteringEventArgs() { }
        public System.Windows.Data.CollectionView CollectionView { get { throw null; } }
    }
    public partial class CollectionViewSource : System.Windows.DependencyObject, System.ComponentModel.ISupportInitialize, System.Windows.IWeakEventListener
    {
        public static readonly System.Windows.DependencyProperty CanChangeLiveFilteringProperty;
        public static readonly System.Windows.DependencyProperty CanChangeLiveGroupingProperty;
        public static readonly System.Windows.DependencyProperty CanChangeLiveSortingProperty;
        public static readonly System.Windows.DependencyProperty CollectionViewTypeProperty;
        public static readonly System.Windows.DependencyProperty IsLiveFilteringProperty;
        public static readonly System.Windows.DependencyProperty IsLiveFilteringRequestedProperty;
        public static readonly System.Windows.DependencyProperty IsLiveGroupingProperty;
        public static readonly System.Windows.DependencyProperty IsLiveGroupingRequestedProperty;
        public static readonly System.Windows.DependencyProperty IsLiveSortingProperty;
        public static readonly System.Windows.DependencyProperty IsLiveSortingRequestedProperty;
        public static readonly System.Windows.DependencyProperty SourceProperty;
        public static readonly System.Windows.DependencyProperty ViewProperty;
        public CollectionViewSource() { }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public bool CanChangeLiveFiltering { get { throw null; } }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public bool CanChangeLiveGrouping { get { throw null; } }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public bool CanChangeLiveSorting { get { throw null; } }
        public System.Type CollectionViewType { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.CultureInfoIetfLanguageTagConverter))]
        public System.Globalization.CultureInfo Culture { get { throw null; } set { } }
        public System.Collections.ObjectModel.ObservableCollection<System.ComponentModel.GroupDescription> GroupDescriptions { get { throw null; } }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public bool? IsLiveFiltering { get { throw null; } }
        public bool IsLiveFilteringRequested { get { throw null; } set { } }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public bool? IsLiveGrouping { get { throw null; } }
        public bool IsLiveGroupingRequested { get { throw null; } set { } }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public bool? IsLiveSorting { get { throw null; } }
        public bool IsLiveSortingRequested { get { throw null; } set { } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveFilteringProperties { get { throw null; } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveGroupingProperties { get { throw null; } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveSortingProperties { get { throw null; } }
        public System.ComponentModel.SortDescriptionCollection SortDescriptions { get { throw null; } }
        public object Source { get { throw null; } set { } }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public System.ComponentModel.ICollectionView View { get { throw null; } }
        public event System.Windows.Data.FilterEventHandler Filter { add { } remove { } }
        public System.IDisposable DeferRefresh() { throw null; }
        public static System.ComponentModel.ICollectionView GetDefaultView(object source) { throw null; }
        public static bool IsDefaultView(System.ComponentModel.ICollectionView view) { throw null; }
        protected virtual void OnCollectionViewTypeChanged(System.Type oldCollectionViewType, System.Type newCollectionViewType) { }
        protected virtual void OnSourceChanged(object oldSource, object newSource) { }
        protected virtual bool ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e) { throw null; }
        void System.ComponentModel.ISupportInitialize.BeginInit() { }
        void System.ComponentModel.ISupportInitialize.EndInit() { }
        bool System.Windows.IWeakEventListener.ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e) { throw null; }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Ignore)]
    public partial class CompositeCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Collections.Specialized.INotifyCollectionChanged, System.ComponentModel.ICollectionViewFactory, System.Windows.IWeakEventListener
    {
        public CompositeCollection() { }
        public CompositeCollection(int capacity) { }
        public int Count { get { throw null; } }
        public object this[int itemIndex] { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        protected event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged { add { } remove { } }
        event System.Collections.Specialized.NotifyCollectionChangedEventHandler System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged { add { } remove { } }
        public int Add(object newItem) { throw null; }
        public void Clear() { }
        public bool Contains(object containItem) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public int IndexOf(object indexItem) { throw null; }
        public void Insert(int insertIndex, object insertItem) { }
        protected virtual bool ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e) { throw null; }
        public void Remove(object removeItem) { }
        public void RemoveAt(int removeIndex) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        System.ComponentModel.ICollectionView System.ComponentModel.ICollectionViewFactory.CreateView() { throw null; }
        bool System.Windows.IWeakEventListener.ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e) { throw null; }
    }
    public partial class DataChangedEventManager : System.Windows.WeakEventManager
    {
        internal DataChangedEventManager() { }
        public static void AddHandler(System.Windows.Data.DataSourceProvider source, System.EventHandler<System.EventArgs> handler) { }
        public static void AddListener(System.Windows.Data.DataSourceProvider source, System.Windows.IWeakEventListener listener) { }
        protected override System.Windows.WeakEventManager.ListenerList NewListenerList() { throw null; }
        public static void RemoveHandler(System.Windows.Data.DataSourceProvider source, System.EventHandler<System.EventArgs> handler) { }
        public static void RemoveListener(System.Windows.Data.DataSourceProvider source, System.Windows.IWeakEventListener listener) { }
        protected override void StartListening(object source) { }
        protected override void StopListening(object source) { }
    }
    public partial class DataTransferEventArgs : System.Windows.RoutedEventArgs
    {
        internal DataTransferEventArgs() { }
        public System.Windows.DependencyProperty Property { get { throw null; } }
        public System.Windows.DependencyObject TargetObject { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public partial class FilterEventArgs : System.EventArgs
    {
        internal FilterEventArgs() { }
        public bool Accepted { get { throw null; } set { } }
        public object Item { get { throw null; } }
    }
    public delegate void FilterEventHandler(object sender, System.Windows.Data.FilterEventArgs e);
    public delegate System.ComponentModel.GroupDescription GroupDescriptionSelectorCallback(System.Windows.Data.CollectionViewGroup group, int level);
    public partial interface IMultiValueConverter
    {
        object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture);
        object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture);
    }
    public partial interface IValueConverter
    {
        object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture);
        object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture);
    }
    public partial class ListCollectionView : System.Windows.Data.CollectionView, System.Collections.IComparer, System.ComponentModel.ICollectionViewLiveShaping, System.ComponentModel.IEditableCollectionView, System.ComponentModel.IEditableCollectionViewAddNewItem, System.ComponentModel.IItemProperties
    {
        public ListCollectionView(System.Collections.IList list) : base (default(System.Collections.IEnumerable)) { }
        protected System.Collections.IComparer ActiveComparer { get { throw null; } set { } }
        protected System.Predicate<object> ActiveFilter { get { throw null; } set { } }
        public bool CanAddNew { get { throw null; } }
        public bool CanAddNewItem { get { throw null; } }
        public bool CanCancelEdit { get { throw null; } }
        public bool CanChangeLiveFiltering { get { throw null; } }
        public bool CanChangeLiveGrouping { get { throw null; } }
        public bool CanChangeLiveSorting { get { throw null; } }
        public override bool CanFilter { get { throw null; } }
        public override bool CanGroup { get { throw null; } }
        public bool CanRemove { get { throw null; } }
        public override bool CanSort { get { throw null; } }
        public override int Count { get { throw null; } }
        public object CurrentAddItem { get { throw null; } }
        public object CurrentEditItem { get { throw null; } }
        public System.Collections.IComparer CustomSort { get { throw null; } set { } }
        public override System.Predicate<object> Filter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public virtual System.Windows.Data.GroupDescriptionSelectorCallback GroupBySelector { get { throw null; } set { } }
        public override System.Collections.ObjectModel.ObservableCollection<System.ComponentModel.GroupDescription> GroupDescriptions { get { throw null; } }
        public override System.Collections.ObjectModel.ReadOnlyObservableCollection<object> Groups { get { throw null; } }
        protected int InternalCount { get { throw null; } }
        protected System.Collections.IList InternalList { get { throw null; } }
        public bool IsAddingNew { get { throw null; } }
        public bool IsDataInGroupOrder { get { throw null; } set { } }
        public bool IsEditingItem { get { throw null; } }
        public override bool IsEmpty { get { throw null; } }
        protected bool IsGrouping { get { throw null; } }
        public bool? IsLiveFiltering { get { throw null; } set { } }
        public bool? IsLiveGrouping { get { throw null; } set { } }
        public bool? IsLiveSorting { get { throw null; } set { } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.ComponentModel.ItemPropertyInfo> ItemProperties { get { throw null; } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveFilteringProperties { get { throw null; } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveGroupingProperties { get { throw null; } }
        public System.Collections.ObjectModel.ObservableCollection<string> LiveSortingProperties { get { throw null; } }
        public System.ComponentModel.NewItemPlaceholderPosition NewItemPlaceholderPosition { get { throw null; } set { } }
        public override System.ComponentModel.SortDescriptionCollection SortDescriptions { get { throw null; } }
        protected bool UsesLocalArray { get { throw null; } }
        public object AddNew() { throw null; }
        public object AddNewItem(object newItem) { throw null; }
        public void CancelEdit() { }
        public void CancelNew() { }
        public void CommitEdit() { }
        public void CommitNew() { }
        protected virtual int Compare(object o1, object o2) { throw null; }
        public override bool Contains(object item) { throw null; }
        public void EditItem(object item) { }
        protected override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public override object GetItemAt(int index) { throw null; }
        public override int IndexOf(object item) { throw null; }
        protected bool InternalContains(object item) { throw null; }
        protected System.Collections.IEnumerator InternalGetEnumerator() { throw null; }
        protected int InternalIndexOf(object item) { throw null; }
        protected object InternalItemAt(int index) { throw null; }
        public override bool MoveCurrentToPosition(int position) { throw null; }
        protected override void OnAllowsCrossThreadChangesChanged() { }
        [System.ObsoleteAttribute("Replaced by OnAllowsCrossThreadChangesChanged")]
        protected override void OnBeginChangeLogging(System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { }
        public override bool PassesFilter(object item) { throw null; }
        protected override void ProcessCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { }
        protected override void RefreshOverride() { }
        public void Remove(object item) { }
        public void RemoveAt(int index) { }
        int System.Collections.IComparer.Compare(object o1, object o2) { throw null; }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Bindings")]
    public partial class MultiBinding : System.Windows.Data.BindingBase, System.Windows.Markup.IAddChild
    {
        public MultiBinding() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Collections.ObjectModel.Collection<System.Windows.Data.BindingBase> Bindings { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Data.IMultiValueConverter Converter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.CultureInfoIetfLanguageTagConverter))]
        public System.Globalization.CultureInfo ConverterCulture { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public object ConverterParameter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Windows.Data.BindingMode.Default)]
        public System.Windows.Data.BindingMode Mode { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool NotifyOnSourceUpdated { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool NotifyOnTargetUpdated { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool NotifyOnValidationError { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Windows.Data.UpdateSourceExceptionFilterCallback UpdateSourceExceptionFilter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Windows.Data.UpdateSourceTrigger.PropertyChanged)]
        public System.Windows.Data.UpdateSourceTrigger UpdateSourceTrigger { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool ValidatesOnDataErrors { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool ValidatesOnExceptions { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool ValidatesOnNotifyDataErrors { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<System.Windows.Controls.ValidationRule> ValidationRules { get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeBindings() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeValidationRules() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public sealed partial class MultiBindingExpression : System.Windows.Data.BindingExpressionBase
    {
        internal MultiBindingExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Data.BindingExpressionBase> BindingExpressions { get { throw null; } }
        public override bool HasError { get { throw null; } }
        public override bool HasValidationError { get { throw null; } }
        public System.Windows.Data.MultiBinding ParentMultiBinding { get { throw null; } }
        public override System.Windows.Controls.ValidationError ValidationError { get { throw null; } }
        public override void UpdateSource() { }
        public override void UpdateTarget() { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
    public partial class ObjectDataProvider : System.Windows.Data.DataSourceProvider
    {
        public ObjectDataProvider() { }
        public System.Collections.IList ConstructorParameters { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool IsAsynchronous { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string MethodName { get { throw null; } set { } }
        public System.Collections.IList MethodParameters { get { throw null; } }
        public object ObjectInstance { get { throw null; } set { } }
        public System.Type ObjectType { get { throw null; } set { } }
        protected override void BeginQuery() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeConstructorParameters() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeMethodParameters() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeObjectInstance() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeObjectType() { throw null; }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Bindings")]
    public partial class PriorityBinding : System.Windows.Data.BindingBase, System.Windows.Markup.IAddChild
    {
        public PriorityBinding() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Collections.ObjectModel.Collection<System.Windows.Data.BindingBase> Bindings { get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeBindings() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public sealed partial class PriorityBindingExpression : System.Windows.Data.BindingExpressionBase
    {
        internal PriorityBindingExpression() { }
        public System.Windows.Data.BindingExpressionBase ActiveBindingExpression { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Data.BindingExpressionBase> BindingExpressions { get { throw null; } }
        public override bool HasValidationError { get { throw null; } }
        public System.Windows.Data.PriorityBinding ParentPriorityBinding { get { throw null; } }
        public override void UpdateSource() { }
        public override void UpdateTarget() { }
    }
    public partial class PropertyGroupDescription : System.ComponentModel.GroupDescription
    {
        public PropertyGroupDescription() { }
        public PropertyGroupDescription(string propertyName) { }
        public PropertyGroupDescription(string propertyName, System.Windows.Data.IValueConverter converter) { }
        public PropertyGroupDescription(string propertyName, System.Windows.Data.IValueConverter converter, System.StringComparison stringComparison) { }
        public static System.Collections.IComparer CompareNameAscending { get { throw null; } }
        public static System.Collections.IComparer CompareNameDescending { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Data.IValueConverter Converter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string PropertyName { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.StringComparison.Ordinal)]
        public System.StringComparison StringComparison { get { throw null; } set { } }
        public override object GroupNameFromItem(object item, int level, System.Globalization.CultureInfo culture) { throw null; }
        public override bool NamesMatch(object groupName, object itemName) { throw null; }
    }
    [System.Windows.Markup.MarkupExtensionReturnTypeAttribute(typeof(System.Windows.Data.RelativeSource))]
    public partial class RelativeSource : System.Windows.Markup.MarkupExtension, System.ComponentModel.ISupportInitialize
    {
        public RelativeSource() { }
        public RelativeSource(System.Windows.Data.RelativeSourceMode mode) { }
        public RelativeSource(System.Windows.Data.RelativeSourceMode mode, System.Type ancestorType, int ancestorLevel) { }
        public int AncestorLevel { get { throw null; } set { } }
        public System.Type AncestorType { get { throw null; } set { } }
        [System.Windows.Markup.ConstructorArgumentAttribute("mode")]
        public System.Windows.Data.RelativeSourceMode Mode { get { throw null; } set { } }
        public static System.Windows.Data.RelativeSource PreviousData { get { throw null; } }
        public static System.Windows.Data.RelativeSource Self { get { throw null; } }
        public static System.Windows.Data.RelativeSource TemplatedParent { get { throw null; } }
        public override object ProvideValue(System.IServiceProvider serviceProvider) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeAncestorLevel() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeAncestorType() { throw null; }
        void System.ComponentModel.ISupportInitialize.BeginInit() { }
        void System.ComponentModel.ISupportInitialize.EndInit() { }
    }
    public enum RelativeSourceMode
    {
        PreviousData = 0,
        TemplatedParent = 1,
        Self = 2,
        FindAncestor = 3,
    }
    public delegate object UpdateSourceExceptionFilterCallback(object bindExpression, System.Exception exception);
    public enum UpdateSourceTrigger
    {
        Default = 0,
        PropertyChanged = 1,
        LostFocus = 2,
        Explicit = 3,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true)]
    public sealed partial class ValueConversionAttribute : System.Attribute
    {
        public ValueConversionAttribute(System.Type sourceType, System.Type targetType) { }
        public System.Type ParameterType { get { throw null; } set { } }
        public System.Type SourceType { get { throw null; } }
        public System.Type TargetType { get { throw null; } }
        public override object TypeId { get { throw null; } }
        public override int GetHashCode() { throw null; }
    }
    public partial class ValueUnavailableException : System.SystemException
    {
        public ValueUnavailableException() { }
        protected ValueUnavailableException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ValueUnavailableException(string message) { }
        public ValueUnavailableException(string message, System.Exception innerException) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    [System.Windows.Markup.ContentPropertyAttribute("XmlSerializer")]
    public partial class XmlDataProvider : System.Windows.Data.DataSourceProvider, System.Windows.Markup.IUriContext
    {
        public XmlDataProvider() { }
        protected virtual System.Uri BaseUri { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Xml.XmlDocument Document { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool IsAsynchronous { get { throw null; } set { } }
        public System.Uri Source { get { throw null; } set { } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Xml.XmlNamespaceManager XmlNamespaceManager { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public System.Xml.Serialization.IXmlSerializable XmlSerializer { get { throw null; } }
        [System.Windows.Markup.DesignerSerializationOptionsAttribute(System.Windows.Markup.DesignerSerializationOptions.SerializeAsAttribute)]
        public string XPath { get { throw null; } set { } }
        protected override void BeginQuery() { }
        protected override void EndInit() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeSource() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeXmlSerializer() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeXPath() { throw null; }
    }
    public partial class XmlNamespaceMapping : System.ComponentModel.ISupportInitialize
    {
        public XmlNamespaceMapping() { }
        public XmlNamespaceMapping(string prefix, System.Uri uri) { }
        public string Prefix { get { throw null; } set { } }
        public System.Uri Uri { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Data.XmlNamespaceMapping mappingA, System.Windows.Data.XmlNamespaceMapping mappingB) { throw null; }
        public static bool operator !=(System.Windows.Data.XmlNamespaceMapping mappingA, System.Windows.Data.XmlNamespaceMapping mappingB) { throw null; }
        void System.ComponentModel.ISupportInitialize.BeginInit() { }
        void System.ComponentModel.ISupportInitialize.EndInit() { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
    public partial class XmlNamespaceMappingCollection : System.Xml.XmlNamespaceManager, System.Collections.Generic.ICollection<System.Windows.Data.XmlNamespaceMapping>, System.Collections.Generic.IEnumerable<System.Windows.Data.XmlNamespaceMapping>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild, System.Windows.Markup.IAddChildInternal
    {
        public XmlNamespaceMappingCollection() : base (default(System.Xml.XmlNameTable)) { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public void Add(System.Windows.Data.XmlNamespaceMapping mapping) { }
        protected virtual void AddChild(object value) { }
        protected virtual void AddText(string text) { }
        public void Clear() { }
        public bool Contains(System.Windows.Data.XmlNamespaceMapping mapping) { throw null; }
        public void CopyTo(System.Windows.Data.XmlNamespaceMapping[] array, int arrayIndex) { }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        protected System.Collections.Generic.IEnumerator<System.Windows.Data.XmlNamespaceMapping> ProtectedGetEnumerator() { throw null; }
        public bool Remove(System.Windows.Data.XmlNamespaceMapping mapping) { throw null; }
        System.Collections.Generic.IEnumerator<System.Windows.Data.XmlNamespaceMapping> System.Collections.Generic.IEnumerable<System.Windows.Data.XmlNamespaceMapping>.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
}
namespace System.Windows.Diagnostics
{
    public static partial class BindingDiagnostics
    {
        public static event System.EventHandler<BindingFailedEventArgs> BindingFailed { add { } remove { } }
    }
    public partial class BindingFailedEventArgs
    {
        public System.Diagnostics.TraceEventType EventType { get { throw null; } }
        public int Code { get { throw null; } }
        public string Message { get { throw null; } }
        public System.Windows.Data.BindingExpressionBase Binding { get { throw null; } }
        public object[] Parameters { get { throw null; } }
    }
    public static partial class ResourceDictionaryDiagnostics
    {
        public static System.Collections.Generic.IEnumerable<System.Windows.Diagnostics.ResourceDictionaryInfo> GenericResourceDictionaries { get { throw null; } }
        public static System.Collections.Generic.IEnumerable<System.Windows.Diagnostics.ResourceDictionaryInfo> ThemedResourceDictionaries { get { throw null; } }
        public static event System.EventHandler<System.Windows.Diagnostics.ResourceDictionaryLoadedEventArgs> GenericResourceDictionaryLoaded { add { } remove { } }
        public static event System.EventHandler<System.Windows.Diagnostics.StaticResourceResolvedEventArgs> StaticResourceResolved { add { } remove { } }
        public static event System.EventHandler<System.Windows.Diagnostics.ResourceDictionaryLoadedEventArgs> ThemedResourceDictionaryLoaded { add { } remove { } }
        public static event System.EventHandler<System.Windows.Diagnostics.ResourceDictionaryUnloadedEventArgs> ThemedResourceDictionaryUnloaded { add { } remove { } }
        public static System.Collections.Generic.IEnumerable<System.Windows.Application> GetApplicationOwners(System.Windows.ResourceDictionary dictionary) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Windows.FrameworkContentElement> GetFrameworkContentElementOwners(System.Windows.ResourceDictionary dictionary) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Windows.FrameworkElement> GetFrameworkElementOwners(System.Windows.ResourceDictionary dictionary) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Windows.ResourceDictionary> GetResourceDictionariesForSource(System.Uri uri) { throw null; }
    }
    public partial class ResourceDictionaryInfo
    {
        internal ResourceDictionaryInfo() { }
        public System.Reflection.Assembly Assembly { get { throw null; } }
        public System.Windows.ResourceDictionary ResourceDictionary { get { throw null; } }
        public System.Reflection.Assembly ResourceDictionaryAssembly { get { throw null; } }
        public System.Uri SourceUri { get { throw null; } }
    }
    public partial class ResourceDictionaryLoadedEventArgs : System.EventArgs
    {
        internal ResourceDictionaryLoadedEventArgs() { }
        public System.Windows.Diagnostics.ResourceDictionaryInfo ResourceDictionaryInfo { get { throw null; } }
    }
    public partial class ResourceDictionaryUnloadedEventArgs : System.EventArgs
    {
        internal ResourceDictionaryUnloadedEventArgs() { }
        public System.Windows.Diagnostics.ResourceDictionaryInfo ResourceDictionaryInfo { get { throw null; } }
    }
    public partial class StaticResourceResolvedEventArgs : System.EventArgs
    {
        internal StaticResourceResolvedEventArgs() { }
        public System.Windows.ResourceDictionary ResourceDictionary { get { throw null; } }
        public object ResourceKey { get { throw null; } }
        public object TargetObject { get { throw null; } }
        public object TargetProperty { get { throw null; } }
    }
}
namespace System.Windows.Documents
{
    public abstract partial class Adorner : System.Windows.FrameworkElement
    {
        protected Adorner(System.Windows.UIElement adornedElement) { }
        public System.Windows.UIElement AdornedElement { get { throw null; } }
        public bool IsClipEnabled { get { throw null; } set { } }
        public virtual System.Windows.Media.GeneralTransform GetDesiredTransform(System.Windows.Media.GeneralTransform transform) { throw null; }
        protected override System.Windows.Media.Geometry GetLayoutClip(System.Windows.Size layoutSlotSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
    }
    public partial class AdornerDecorator : System.Windows.Controls.Decorator
    {
        public AdornerDecorator() { }
        public System.Windows.Documents.AdornerLayer AdornerLayer { get { throw null; } }
        public override System.Windows.UIElement Child { get { throw null; } set { } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
    }
    public partial class AdornerLayer : System.Windows.FrameworkElement
    {
        internal AdornerLayer() { }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        protected override int VisualChildrenCount { get { throw null; } }
        public void Add(System.Windows.Documents.Adorner adorner) { }
        public System.Windows.Media.AdornerHitTestResult AdornerHitTest(System.Windows.Point point) { throw null; }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        public static System.Windows.Documents.AdornerLayer GetAdornerLayer(System.Windows.Media.Visual visual) { throw null; }
        public System.Windows.Documents.Adorner[] GetAdorners(System.Windows.UIElement element) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        public void Remove(System.Windows.Documents.Adorner adorner) { }
        public void Update() { }
        public void Update(System.Windows.UIElement element) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Blocks")]
    public abstract partial class AnchoredBlock : System.Windows.Documents.Inline
    {
        public static readonly System.Windows.DependencyProperty BorderBrushProperty;
        public static readonly System.Windows.DependencyProperty BorderThicknessProperty;
        public static readonly System.Windows.DependencyProperty LineHeightProperty;
        public static readonly System.Windows.DependencyProperty LineStackingStrategyProperty;
        public static readonly System.Windows.DependencyProperty MarginProperty;
        public static readonly System.Windows.DependencyProperty PaddingProperty;
        public static readonly System.Windows.DependencyProperty TextAlignmentProperty;
        protected AnchoredBlock(System.Windows.Documents.Block block, System.Windows.Documents.TextPointer insertionPosition) { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.BlockCollection Blocks { get { throw null; } }
        public System.Windows.Media.Brush BorderBrush { get { throw null; } set { } }
        public System.Windows.Thickness BorderThickness { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double LineHeight { get { throw null; } set { } }
        public System.Windows.LineStackingStrategy LineStackingStrategy { get { throw null; } set { } }
        public System.Windows.Thickness Margin { get { throw null; } set { } }
        public System.Windows.Thickness Padding { get { throw null; } set { } }
        public System.Windows.TextAlignment TextAlignment { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeBlocks(System.Windows.Markup.XamlDesignerSerializationManager manager) { throw null; }
    }
    public abstract partial class Block : System.Windows.Documents.TextElement
    {
        public static readonly System.Windows.DependencyProperty BorderBrushProperty;
        public static readonly System.Windows.DependencyProperty BorderThicknessProperty;
        public static readonly System.Windows.DependencyProperty BreakColumnBeforeProperty;
        public static readonly System.Windows.DependencyProperty BreakPageBeforeProperty;
        public static readonly System.Windows.DependencyProperty ClearFloatersProperty;
        public static readonly System.Windows.DependencyProperty FlowDirectionProperty;
        public static readonly System.Windows.DependencyProperty IsHyphenationEnabledProperty;
        public static readonly System.Windows.DependencyProperty LineHeightProperty;
        public static readonly System.Windows.DependencyProperty LineStackingStrategyProperty;
        public static readonly System.Windows.DependencyProperty MarginProperty;
        public static readonly System.Windows.DependencyProperty PaddingProperty;
        public static readonly System.Windows.DependencyProperty TextAlignmentProperty;
        protected Block() { }
        public System.Windows.Media.Brush BorderBrush { get { throw null; } set { } }
        public System.Windows.Thickness BorderThickness { get { throw null; } set { } }
        public bool BreakColumnBefore { get { throw null; } set { } }
        public bool BreakPageBefore { get { throw null; } set { } }
        public System.Windows.WrapDirection ClearFloaters { get { throw null; } set { } }
        public System.Windows.FlowDirection FlowDirection { get { throw null; } set { } }
        public bool IsHyphenationEnabled { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double LineHeight { get { throw null; } set { } }
        public System.Windows.LineStackingStrategy LineStackingStrategy { get { throw null; } set { } }
        public System.Windows.Thickness Margin { get { throw null; } set { } }
        public System.Windows.Documents.Block NextBlock { get { throw null; } }
        public System.Windows.Thickness Padding { get { throw null; } set { } }
        public System.Windows.Documents.Block PreviousBlock { get { throw null; } }
        public System.Windows.Documents.BlockCollection SiblingBlocks { get { throw null; } }
        public System.Windows.TextAlignment TextAlignment { get { throw null; } set { } }
        public static bool GetIsHyphenationEnabled(System.Windows.DependencyObject element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public static double GetLineHeight(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.LineStackingStrategy GetLineStackingStrategy(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.TextAlignment GetTextAlignment(System.Windows.DependencyObject element) { throw null; }
        public static void SetIsHyphenationEnabled(System.Windows.DependencyObject element, bool value) { }
        public static void SetLineHeight(System.Windows.DependencyObject element, double value) { }
        public static void SetLineStackingStrategy(System.Windows.DependencyObject element, System.Windows.LineStackingStrategy value) { }
        public static void SetTextAlignment(System.Windows.DependencyObject element, System.Windows.TextAlignment value) { }
    }
    public partial class BlockCollection : System.Windows.Documents.TextElementCollection<System.Windows.Documents.Block>
    {
        internal BlockCollection() { }
        public System.Windows.Documents.Block FirstBlock { get { throw null; } }
        public System.Windows.Documents.Block LastBlock { get { throw null; } }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Child")]
    public partial class BlockUIContainer : System.Windows.Documents.Block
    {
        public BlockUIContainer() { }
        public BlockUIContainer(System.Windows.UIElement uiElement) { }
        public System.Windows.UIElement Child { get { throw null; } set { } }
    }
    public partial class Bold : System.Windows.Documents.Span
    {
        public Bold() { }
        public Bold(System.Windows.Documents.Inline childInline) { }
        public Bold(System.Windows.Documents.Inline childInline, System.Windows.Documents.TextPointer insertionPosition) { }
        public Bold(System.Windows.Documents.TextPointer start, System.Windows.Documents.TextPointer end) { }
    }
    [System.Windows.Markup.UsableDuringInitializationAttribute(false)]
    public sealed partial class DocumentReference : System.Windows.FrameworkElement, System.Windows.Markup.IUriContext
    {
        public static readonly System.Windows.DependencyProperty SourceProperty;
        public DocumentReference() { }
        public System.Uri Source { get { throw null; } set { } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        public System.Windows.Documents.FixedDocument GetDocument(bool forceReload) { throw null; }
        public void SetDocument(System.Windows.Documents.FixedDocument doc) { }
    }
    [System.CLSCompliantAttribute(false)]
    public sealed partial class DocumentReferenceCollection : System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentReference>, System.Collections.IEnumerable, System.Collections.Specialized.INotifyCollectionChanged
    {
        internal DocumentReferenceCollection() { }
        public int Count { get { throw null; } }
        public System.Windows.Documents.DocumentReference this[int index] { get { throw null; } }
        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged { add { } remove { } }
        public void Add(System.Windows.Documents.DocumentReference item) { }
        public void CopyTo(System.Windows.Documents.DocumentReference[] array, int arrayIndex) { }
        public System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentReference> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public static partial class EditingCommands
    {
        public static System.Windows.Input.RoutedUICommand AlignCenter { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand AlignJustify { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand AlignLeft { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand AlignRight { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand Backspace { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand CorrectSpellingError { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand DecreaseFontSize { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand DecreaseIndentation { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand Delete { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand DeleteNextWord { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand DeletePreviousWord { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand EnterLineBreak { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand EnterParagraphBreak { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand IgnoreSpellingError { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand IncreaseFontSize { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand IncreaseIndentation { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveDownByLine { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveDownByPage { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveDownByParagraph { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveLeftByCharacter { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveLeftByWord { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveRightByCharacter { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveRightByWord { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveToDocumentEnd { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveToDocumentStart { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveToLineEnd { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveToLineStart { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveUpByLine { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveUpByPage { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand MoveUpByParagraph { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectDownByLine { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectDownByPage { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectDownByParagraph { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectLeftByCharacter { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectLeftByWord { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectRightByCharacter { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectRightByWord { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectToDocumentEnd { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectToDocumentStart { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectToLineEnd { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectToLineStart { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectUpByLine { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectUpByPage { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand SelectUpByParagraph { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand TabBackward { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand TabForward { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ToggleBold { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ToggleBullets { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ToggleInsert { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ToggleItalic { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ToggleNumbering { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ToggleSubscript { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ToggleSuperscript { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ToggleUnderline { get { throw null; } }
    }
    public partial class Figure : System.Windows.Documents.AnchoredBlock
    {
        public static readonly System.Windows.DependencyProperty CanDelayPlacementProperty;
        public static readonly System.Windows.DependencyProperty HeightProperty;
        public static readonly System.Windows.DependencyProperty HorizontalAnchorProperty;
        public static readonly System.Windows.DependencyProperty HorizontalOffsetProperty;
        public static readonly System.Windows.DependencyProperty VerticalAnchorProperty;
        public static readonly System.Windows.DependencyProperty VerticalOffsetProperty;
        public static readonly System.Windows.DependencyProperty WidthProperty;
        public static readonly System.Windows.DependencyProperty WrapDirectionProperty;
        public Figure() : base (default(System.Windows.Documents.Block), default(System.Windows.Documents.TextPointer)) { }
        public Figure(System.Windows.Documents.Block childBlock) : base (default(System.Windows.Documents.Block), default(System.Windows.Documents.TextPointer)) { }
        public Figure(System.Windows.Documents.Block childBlock, System.Windows.Documents.TextPointer insertionPosition) : base (default(System.Windows.Documents.Block), default(System.Windows.Documents.TextPointer)) { }
        public bool CanDelayPlacement { get { throw null; } set { } }
        public System.Windows.FigureLength Height { get { throw null; } set { } }
        public System.Windows.FigureHorizontalAnchor HorizontalAnchor { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double HorizontalOffset { get { throw null; } set { } }
        public System.Windows.FigureVerticalAnchor VerticalAnchor { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double VerticalOffset { get { throw null; } set { } }
        public System.Windows.FigureLength Width { get { throw null; } set { } }
        public System.Windows.WrapDirection WrapDirection { get { throw null; } set { } }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Pages")]
    public partial class FixedDocument : System.Windows.FrameworkContentElement, System.IServiceProvider, System.Windows.Documents.IDocumentPaginatorSource, System.Windows.Markup.IAddChild, System.Windows.Markup.IUriContext, System.Windows.Markup.IAddChildInternal
    {
        public static readonly System.Windows.DependencyProperty PrintTicketProperty;
        public FixedDocument() { }
        public System.Windows.Documents.DocumentPaginator DocumentPaginator { get { throw null; } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.PageContentCollection Pages { get { throw null; } }
        public object PrintTicket { get { throw null; } set { } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        object System.IServiceProvider.GetService(System.Type serviceType) { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("References")]
    public partial class FixedDocumentSequence : System.Windows.FrameworkContentElement, System.IServiceProvider, System.Windows.Documents.IDocumentPaginatorSource, System.Windows.Markup.IAddChild, System.Windows.Markup.IUriContext, System.Windows.Markup.IAddChildInternal
    {
        public static readonly System.Windows.DependencyProperty PrintTicketProperty;
        public FixedDocumentSequence() { }
        public System.Windows.Documents.DocumentPaginator DocumentPaginator { get { throw null; } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public object PrintTicket { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.DocumentReferenceCollection References { get { throw null; } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        object System.IServiceProvider.GetService(System.Type serviceType) { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Children")]
    public sealed partial class FixedPage : System.Windows.FrameworkElement, System.Windows.Markup.IAddChild, System.Windows.Markup.IUriContext, System.Windows.Markup.IAddChildInternal
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty BleedBoxProperty;
        public static readonly System.Windows.DependencyProperty BottomProperty;
        public static readonly System.Windows.DependencyProperty ContentBoxProperty;
        public static readonly System.Windows.DependencyProperty LeftProperty;
        public static readonly System.Windows.DependencyProperty NavigateUriProperty;
        public static readonly System.Windows.DependencyProperty PrintTicketProperty;
        public static readonly System.Windows.DependencyProperty RightProperty;
        public static readonly System.Windows.DependencyProperty TopProperty;
        public FixedPage() { }
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        public System.Windows.Rect BleedBox { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Controls.UIElementCollection Children { get { throw null; } }
        public System.Windows.Rect ContentBox { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public object PrintTicket { get { throw null; } set { } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        protected override int VisualChildrenCount { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetBottom(System.Windows.UIElement element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetLeft(System.Windows.UIElement element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static System.Uri GetNavigateUri(System.Windows.UIElement element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetRight(System.Windows.UIElement element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        [System.Windows.AttachedPropertyBrowsableForChildrenAttribute]
        public static double GetTop(System.Windows.UIElement element) { throw null; }
        protected override System.Windows.Media.Visual GetVisualChild(int index) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnPreviewMouseWheel(System.Windows.Input.MouseWheelEventArgs e) { }
        protected override void OnRender(System.Windows.Media.DrawingContext dc) { }
        protected internal override void OnVisualParentChanged(System.Windows.DependencyObject oldParent) { }
        public static void SetBottom(System.Windows.UIElement element, double length) { }
        public static void SetLeft(System.Windows.UIElement element, double length) { }
        public static void SetNavigateUri(System.Windows.UIElement element, System.Uri uri) { }
        public static void SetRight(System.Windows.UIElement element, double length) { }
        public static void SetTop(System.Windows.UIElement element, double length) { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class Floater : System.Windows.Documents.AnchoredBlock
    {
        public static readonly System.Windows.DependencyProperty HorizontalAlignmentProperty;
        public static readonly System.Windows.DependencyProperty WidthProperty;
        public Floater() : base (default(System.Windows.Documents.Block), default(System.Windows.Documents.TextPointer)) { }
        public Floater(System.Windows.Documents.Block childBlock) : base (default(System.Windows.Documents.Block), default(System.Windows.Documents.TextPointer)) { }
        public Floater(System.Windows.Documents.Block childBlock, System.Windows.Documents.TextPointer insertionPosition) : base (default(System.Windows.Documents.Block), default(System.Windows.Documents.TextPointer)) { }
        public System.Windows.HorizontalAlignment HorizontalAlignment { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double Width { get { throw null; } set { } }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Inherit, Readability=System.Windows.Readability.Unreadable)]
    [System.Windows.Markup.ContentPropertyAttribute("Blocks")]
    public partial class FlowDocument : System.Windows.FrameworkContentElement, System.IServiceProvider, System.Windows.Documents.IDocumentPaginatorSource, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty ColumnGapProperty;
        public static readonly System.Windows.DependencyProperty ColumnRuleBrushProperty;
        public static readonly System.Windows.DependencyProperty ColumnRuleWidthProperty;
        public static readonly System.Windows.DependencyProperty ColumnWidthProperty;
        public static readonly System.Windows.DependencyProperty FlowDirectionProperty;
        public static readonly System.Windows.DependencyProperty FontFamilyProperty;
        public static readonly System.Windows.DependencyProperty FontSizeProperty;
        public static readonly System.Windows.DependencyProperty FontStretchProperty;
        public static readonly System.Windows.DependencyProperty FontStyleProperty;
        public static readonly System.Windows.DependencyProperty FontWeightProperty;
        public static readonly System.Windows.DependencyProperty ForegroundProperty;
        public static readonly System.Windows.DependencyProperty IsColumnWidthFlexibleProperty;
        public static readonly System.Windows.DependencyProperty IsHyphenationEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsOptimalParagraphEnabledProperty;
        public static readonly System.Windows.DependencyProperty LineHeightProperty;
        public static readonly System.Windows.DependencyProperty LineStackingStrategyProperty;
        public static readonly System.Windows.DependencyProperty MaxPageHeightProperty;
        public static readonly System.Windows.DependencyProperty MaxPageWidthProperty;
        public static readonly System.Windows.DependencyProperty MinPageHeightProperty;
        public static readonly System.Windows.DependencyProperty MinPageWidthProperty;
        public static readonly System.Windows.DependencyProperty PageHeightProperty;
        public static readonly System.Windows.DependencyProperty PagePaddingProperty;
        public static readonly System.Windows.DependencyProperty PageWidthProperty;
        public static readonly System.Windows.DependencyProperty TextAlignmentProperty;
        public static readonly System.Windows.DependencyProperty TextEffectsProperty;
        public FlowDocument() { }
        public FlowDocument(System.Windows.Documents.Block block) { }
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.BlockCollection Blocks { get { throw null; } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        public double ColumnGap { get { throw null; } set { } }
        public System.Windows.Media.Brush ColumnRuleBrush { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        public double ColumnRuleWidth { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
        public double ColumnWidth { get { throw null; } set { } }
        public System.Windows.Documents.TextPointer ContentEnd { get { throw null; } }
        public System.Windows.Documents.TextPointer ContentStart { get { throw null; } }
        public System.Windows.FlowDirection FlowDirection { get { throw null; } set { } }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Font, Modifiability=System.Windows.Modifiability.Unmodifiable)]
        public System.Windows.Media.FontFamily FontFamily { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.FontSizeConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None)]
        public double FontSize { get { throw null; } set { } }
        public System.Windows.FontStretch FontStretch { get { throw null; } set { } }
        public System.Windows.FontStyle FontStyle { get { throw null; } set { } }
        public System.Windows.FontWeight FontWeight { get { throw null; } set { } }
        public System.Windows.Media.Brush Foreground { get { throw null; } set { } }
        public bool IsColumnWidthFlexible { get { throw null; } set { } }
        protected override bool IsEnabledCore { get { throw null; } }
        public bool IsHyphenationEnabled { get { throw null; } set { } }
        public bool IsOptimalParagraphEnabled { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double LineHeight { get { throw null; } set { } }
        public System.Windows.LineStackingStrategy LineStackingStrategy { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double MaxPageHeight { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double MaxPageWidth { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double MinPageHeight { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double MinPageWidth { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double PageHeight { get { throw null; } set { } }
        public System.Windows.Thickness PagePadding { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double PageWidth { get { throw null; } set { } }
        System.Windows.Documents.DocumentPaginator System.Windows.Documents.IDocumentPaginatorSource.DocumentPaginator { get { throw null; } }
        public System.Windows.TextAlignment TextAlignment { get { throw null; } set { } }
        public System.Windows.Media.TextEffectCollection TextEffects { get { throw null; } set { } }
        public System.Windows.Documents.Typography Typography { get { throw null; } }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected sealed override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        public void SetDpi(System.Windows.DpiScale dpiInfo) { }
        object System.IServiceProvider.GetService(System.Type serviceType) { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public sealed partial class FrameworkRichTextComposition : System.Windows.Documents.FrameworkTextComposition
    {
        internal FrameworkRichTextComposition() { }
        public System.Windows.Documents.TextPointer CompositionEnd { get { throw null; } }
        public System.Windows.Documents.TextPointer CompositionStart { get { throw null; } }
        public System.Windows.Documents.TextPointer ResultEnd { get { throw null; } }
        public System.Windows.Documents.TextPointer ResultStart { get { throw null; } }
    }
    public partial class FrameworkTextComposition : System.Windows.Input.TextComposition
    {
        internal FrameworkTextComposition() : base (default(System.Windows.Input.InputManager), default(System.Windows.IInputElement), default(string)) { }
        public int CompositionLength { get { throw null; } }
        public int CompositionOffset { get { throw null; } }
        public int ResultLength { get { throw null; } }
        public int ResultOffset { get { throw null; } }
        public override void Complete() { }
    }
    public sealed partial class GetPageRootCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        internal GetPageRootCompletedEventArgs() : base (default(System.Exception), default(bool), default(object)) { }
        public System.Windows.Documents.FixedPage Result { get { throw null; } }
    }
    public delegate void GetPageRootCompletedEventHandler(object sender, System.Windows.Documents.GetPageRootCompletedEventArgs e);
    public sealed partial class Glyphs : System.Windows.FrameworkElement, System.Windows.Markup.IUriContext
    {
        public static readonly System.Windows.DependencyProperty BidiLevelProperty;
        public static readonly System.Windows.DependencyProperty CaretStopsProperty;
        public static readonly System.Windows.DependencyProperty DeviceFontNameProperty;
        public static readonly System.Windows.DependencyProperty FillProperty;
        public static readonly System.Windows.DependencyProperty FontRenderingEmSizeProperty;
        public static readonly System.Windows.DependencyProperty FontUriProperty;
        public static readonly System.Windows.DependencyProperty IndicesProperty;
        public static readonly System.Windows.DependencyProperty IsSidewaysProperty;
        public static readonly System.Windows.DependencyProperty OriginXProperty;
        public static readonly System.Windows.DependencyProperty OriginYProperty;
        public static readonly System.Windows.DependencyProperty StyleSimulationsProperty;
        public static readonly System.Windows.DependencyProperty UnicodeStringProperty;
        public Glyphs() { }
        public int BidiLevel { get { throw null; } set { } }
        public string CaretStops { get { throw null; } set { } }
        public string DeviceFontName { get { throw null; } set { } }
        public System.Windows.Media.Brush Fill { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.FontSizeConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        public double FontRenderingEmSize { get { throw null; } set { } }
        public System.Uri FontUri { get { throw null; } set { } }
        public string Indices { get { throw null; } set { } }
        public bool IsSideways { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        public double OriginX { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute("System.Windows.LengthConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
        public double OriginY { get { throw null; } set { } }
        public System.Windows.Media.StyleSimulations StyleSimulations { get { throw null; } set { } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        public string UnicodeString { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext context) { }
        public System.Windows.Media.GlyphRun ToGlyphRun() { throw null; }
    }
    [System.Windows.Documents.TextElementEditingBehaviorAttribute(IsMergeable=false, IsTypographicOnly=false)]
    public partial class Hyperlink : System.Windows.Documents.Span, System.Windows.Input.ICommandSource, System.Windows.Markup.IUriContext
    {
        public static readonly System.Windows.RoutedEvent ClickEvent;
        public static readonly System.Windows.DependencyProperty CommandParameterProperty;
        public static readonly System.Windows.DependencyProperty CommandProperty;
        public static readonly System.Windows.DependencyProperty CommandTargetProperty;
        public static readonly System.Windows.DependencyProperty NavigateUriProperty;
        public static readonly System.Windows.RoutedEvent RequestNavigateEvent;
        public static readonly System.Windows.DependencyProperty TargetNameProperty;
        public Hyperlink() { }
        public Hyperlink(System.Windows.Documents.Inline childInline) { }
        public Hyperlink(System.Windows.Documents.Inline childInline, System.Windows.Documents.TextPointer insertionPosition) { }
        public Hyperlink(System.Windows.Documents.TextPointer start, System.Windows.Documents.TextPointer end) { }
        protected virtual System.Uri BaseUri { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Action")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public System.Windows.Input.ICommand Command { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Action")]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public object CommandParameter { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.ComponentModel.CategoryAttribute("Action")]
        public System.Windows.IInputElement CommandTarget { get { throw null; } set { } }
        protected override bool IsEnabledCore { get { throw null; } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Hyperlink)]
        public System.Uri NavigateUri { get { throw null; } set { } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        [System.ComponentModel.BindableAttribute(true)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Modifiability=System.Windows.Modifiability.Unmodifiable)]
        public string TargetName { get { throw null; } set { } }
        [System.ComponentModel.CategoryAttribute("Behavior")]
        public event System.Windows.RoutedEventHandler Click { add { } remove { } }
        public event System.Windows.Navigation.RequestNavigateEventHandler RequestNavigate { add { } remove { } }
        public void DoClick() { }
        protected virtual void OnClick() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected internal override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected internal override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
        protected internal override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
    }
    [System.Windows.Documents.TextElementEditingBehaviorAttribute(IsMergeable=true, IsTypographicOnly=true)]
    public abstract partial class Inline : System.Windows.Documents.TextElement
    {
        public static readonly System.Windows.DependencyProperty BaselineAlignmentProperty;
        public static readonly System.Windows.DependencyProperty FlowDirectionProperty;
        public static readonly System.Windows.DependencyProperty TextDecorationsProperty;
        protected Inline() { }
        public System.Windows.BaselineAlignment BaselineAlignment { get { throw null; } set { } }
        public System.Windows.FlowDirection FlowDirection { get { throw null; } set { } }
        public System.Windows.Documents.Inline NextInline { get { throw null; } }
        public System.Windows.Documents.Inline PreviousInline { get { throw null; } }
        public System.Windows.Documents.InlineCollection SiblingInlines { get { throw null; } }
        public System.Windows.TextDecorationCollection TextDecorations { get { throw null; } set { } }
    }
    [System.Windows.Markup.ContentWrapperAttribute(typeof(System.Windows.Documents.InlineUIContainer))]
    [System.Windows.Markup.ContentWrapperAttribute(typeof(System.Windows.Documents.Run))]
    [System.Windows.Markup.WhitespaceSignificantCollectionAttribute]
    public partial class InlineCollection : System.Windows.Documents.TextElementCollection<System.Windows.Documents.Inline>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal InlineCollection() { }
        public System.Windows.Documents.Inline FirstInline { get { throw null; } }
        public System.Windows.Documents.Inline LastInline { get { throw null; } }
        public void Add(string text) { }
        public void Add(System.Windows.UIElement uiElement) { }
    }
    [System.Windows.Documents.TextElementEditingBehaviorAttribute(IsMergeable=false)]
    [System.Windows.Markup.ContentPropertyAttribute("Child")]
    public partial class InlineUIContainer : System.Windows.Documents.Inline
    {
        public InlineUIContainer() { }
        public InlineUIContainer(System.Windows.UIElement childUIElement) { }
        public InlineUIContainer(System.Windows.UIElement childUIElement, System.Windows.Documents.TextPointer insertionPosition) { }
        public System.Windows.UIElement Child { get { throw null; } set { } }
    }
    public partial class Italic : System.Windows.Documents.Span
    {
        public Italic() { }
        public Italic(System.Windows.Documents.Inline childInline) { }
        public Italic(System.Windows.Documents.Inline childInline, System.Windows.Documents.TextPointer insertionPosition) { }
        public Italic(System.Windows.Documents.TextPointer start, System.Windows.Documents.TextPointer end) { }
    }
    [System.Windows.Markup.TrimSurroundingWhitespaceAttribute]
    public partial class LineBreak : System.Windows.Documents.Inline
    {
        public LineBreak() { }
        public LineBreak(System.Windows.Documents.TextPointer insertionPosition) { }
    }
    public sealed partial class LinkTarget
    {
        public LinkTarget() { }
        public string Name { get { throw null; } set { } }
    }
    public sealed partial class LinkTargetCollection : System.Collections.CollectionBase
    {
        public LinkTargetCollection() { }
        public System.Windows.Documents.LinkTarget this[int index] { get { throw null; } set { } }
        public int Add(System.Windows.Documents.LinkTarget value) { throw null; }
        public bool Contains(System.Windows.Documents.LinkTarget value) { throw null; }
        public void CopyTo(System.Windows.Documents.LinkTarget[] array, int index) { }
        public int IndexOf(System.Windows.Documents.LinkTarget value) { throw null; }
        public void Insert(int index, System.Windows.Documents.LinkTarget value) { }
        public void Remove(System.Windows.Documents.LinkTarget value) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("ListItems")]
    public partial class List : System.Windows.Documents.Block
    {
        public static readonly System.Windows.DependencyProperty MarkerOffsetProperty;
        public static readonly System.Windows.DependencyProperty MarkerStyleProperty;
        public static readonly System.Windows.DependencyProperty StartIndexProperty;
        public List() { }
        public List(System.Windows.Documents.ListItem listItem) { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.ListItemCollection ListItems { get { throw null; } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double MarkerOffset { get { throw null; } set { } }
        public System.Windows.TextMarkerStyle MarkerStyle { get { throw null; } set { } }
        public int StartIndex { get { throw null; } set { } }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Blocks")]
    public partial class ListItem : System.Windows.Documents.TextElement
    {
        public static readonly System.Windows.DependencyProperty BorderBrushProperty;
        public static readonly System.Windows.DependencyProperty BorderThicknessProperty;
        public static readonly System.Windows.DependencyProperty FlowDirectionProperty;
        public static readonly System.Windows.DependencyProperty LineHeightProperty;
        public static readonly System.Windows.DependencyProperty LineStackingStrategyProperty;
        public static readonly System.Windows.DependencyProperty MarginProperty;
        public static readonly System.Windows.DependencyProperty PaddingProperty;
        public static readonly System.Windows.DependencyProperty TextAlignmentProperty;
        public ListItem() { }
        public ListItem(System.Windows.Documents.Paragraph paragraph) { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.BlockCollection Blocks { get { throw null; } }
        public System.Windows.Media.Brush BorderBrush { get { throw null; } set { } }
        public System.Windows.Thickness BorderThickness { get { throw null; } set { } }
        public System.Windows.FlowDirection FlowDirection { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double LineHeight { get { throw null; } set { } }
        public System.Windows.LineStackingStrategy LineStackingStrategy { get { throw null; } set { } }
        public System.Windows.Documents.List List { get { throw null; } }
        public System.Windows.Thickness Margin { get { throw null; } set { } }
        public System.Windows.Documents.ListItem NextListItem { get { throw null; } }
        public System.Windows.Thickness Padding { get { throw null; } set { } }
        public System.Windows.Documents.ListItem PreviousListItem { get { throw null; } }
        public System.Windows.Documents.ListItemCollection SiblingListItems { get { throw null; } }
        public System.Windows.TextAlignment TextAlignment { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeBlocks(System.Windows.Markup.XamlDesignerSerializationManager manager) { throw null; }
    }
    public partial class ListItemCollection : System.Windows.Documents.TextElementCollection<System.Windows.Documents.ListItem>
    {
        internal ListItemCollection() { }
        public System.Windows.Documents.ListItem FirstListItem { get { throw null; } }
        public System.Windows.Documents.ListItem LastListItem { get { throw null; } }
    }
    public enum LogicalDirection
    {
        Backward = 0,
        Forward = 1,
    }
    [System.Windows.Markup.ContentPropertyAttribute("Child")]
    public sealed partial class PageContent : System.Windows.FrameworkElement, System.Windows.Markup.IAddChild, System.Windows.Markup.IUriContext, System.Windows.Markup.IAddChildInternal
    {
        public static readonly System.Windows.DependencyProperty SourceProperty;
        public PageContent() { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.FixedPage Child { get { throw null; } set { } }
        public System.Windows.Documents.LinkTargetCollection LinkTargets { get { throw null; } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public System.Uri Source { get { throw null; } set { } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        public event System.Windows.Documents.GetPageRootCompletedEventHandler GetPageRootCompleted { add { } remove { } }
        public System.Windows.Documents.FixedPage GetPageRoot(bool forceReload) { throw null; }
        public void GetPageRootAsync(bool forceReload) { }
        public void GetPageRootAsyncCancel() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeChild(System.Windows.Markup.XamlDesignerSerializationManager manager) { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public sealed partial class PageContentCollection : System.Collections.Generic.IEnumerable<System.Windows.Documents.PageContent>, System.Collections.IEnumerable
    {
        internal PageContentCollection() { }
        public int Count { get { throw null; } }
        public System.Windows.Documents.PageContent this[int pageIndex] { get { throw null; } }
        public int Add(System.Windows.Documents.PageContent newPageContent) { throw null; }
        public System.Collections.Generic.IEnumerator<System.Windows.Documents.PageContent> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Inlines")]
    public partial class Paragraph : System.Windows.Documents.Block
    {
        public static readonly System.Windows.DependencyProperty KeepTogetherProperty;
        public static readonly System.Windows.DependencyProperty KeepWithNextProperty;
        public static readonly System.Windows.DependencyProperty MinOrphanLinesProperty;
        public static readonly System.Windows.DependencyProperty MinWidowLinesProperty;
        public static readonly System.Windows.DependencyProperty TextDecorationsProperty;
        public static readonly System.Windows.DependencyProperty TextIndentProperty;
        public Paragraph() { }
        public Paragraph(System.Windows.Documents.Inline inline) { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.InlineCollection Inlines { get { throw null; } }
        public bool KeepTogether { get { throw null; } set { } }
        public bool KeepWithNext { get { throw null; } set { } }
        public int MinOrphanLines { get { throw null; } set { } }
        public int MinWidowLines { get { throw null; } set { } }
        public System.Windows.TextDecorationCollection TextDecorations { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double TextIndent { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeInlines(System.Windows.Markup.XamlDesignerSerializationManager manager) { throw null; }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Text")]
    public partial class Run : System.Windows.Documents.Inline
    {
        public static readonly System.Windows.DependencyProperty TextProperty;
        public Run() { }
        public Run(string text) { }
        public Run(string text, System.Windows.Documents.TextPointer insertionPosition) { }
        public string Text { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeText(System.Windows.Markup.XamlDesignerSerializationManager manager) { throw null; }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Blocks")]
    public partial class Section : System.Windows.Documents.Block
    {
        public Section() { }
        public Section(System.Windows.Documents.Block block) { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.BlockCollection Blocks { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool HasTrailingParagraphBreakOnPaste { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeBlocks(System.Windows.Markup.XamlDesignerSerializationManager manager) { throw null; }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Inlines")]
    public partial class Span : System.Windows.Documents.Inline
    {
        public Span() { }
        public Span(System.Windows.Documents.Inline childInline) { }
        public Span(System.Windows.Documents.Inline childInline, System.Windows.Documents.TextPointer insertionPosition) { }
        public Span(System.Windows.Documents.TextPointer start, System.Windows.Documents.TextPointer end) { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.InlineCollection Inlines { get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeInlines(System.Windows.Markup.XamlDesignerSerializationManager manager) { throw null; }
    }
    [System.Windows.Markup.ContentPropertyAttribute("RowGroups")]
    public partial class Table : System.Windows.Documents.Block, System.Windows.Markup.IAddChild
    {
        public static readonly System.Windows.DependencyProperty CellSpacingProperty;
        public Table() { }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double CellSpacing { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.TableColumnCollection Columns { get { throw null; } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.TableRowGroupCollection RowGroups { get { throw null; } }
        public override void BeginInit() { }
        public override void EndInit() { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeColumns() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Blocks")]
    public partial class TableCell : System.Windows.Documents.TextElement
    {
        public static readonly System.Windows.DependencyProperty BorderBrushProperty;
        public static readonly System.Windows.DependencyProperty BorderThicknessProperty;
        public static readonly System.Windows.DependencyProperty ColumnSpanProperty;
        public static readonly System.Windows.DependencyProperty FlowDirectionProperty;
        public static readonly System.Windows.DependencyProperty LineHeightProperty;
        public static readonly System.Windows.DependencyProperty LineStackingStrategyProperty;
        public static readonly System.Windows.DependencyProperty PaddingProperty;
        public static readonly System.Windows.DependencyProperty RowSpanProperty;
        public static readonly System.Windows.DependencyProperty TextAlignmentProperty;
        public TableCell() { }
        public TableCell(System.Windows.Documents.Block blockItem) { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.BlockCollection Blocks { get { throw null; } }
        public System.Windows.Media.Brush BorderBrush { get { throw null; } set { } }
        public System.Windows.Thickness BorderThickness { get { throw null; } set { } }
        public int ColumnSpan { get { throw null; } set { } }
        public System.Windows.FlowDirection FlowDirection { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double LineHeight { get { throw null; } set { } }
        public System.Windows.LineStackingStrategy LineStackingStrategy { get { throw null; } set { } }
        public System.Windows.Thickness Padding { get { throw null; } set { } }
        public int RowSpan { get { throw null; } set { } }
        public System.Windows.TextAlignment TextAlignment { get { throw null; } set { } }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
    }
    public sealed partial class TableCellCollection : System.Collections.Generic.ICollection<System.Windows.Documents.TableCell>, System.Collections.Generic.IEnumerable<System.Windows.Documents.TableCell>, System.Collections.Generic.IList<System.Windows.Documents.TableCell>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal TableCellCollection() { }
        public int Capacity { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Windows.Documents.TableCell this[int index] { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(System.Windows.Documents.TableCell item) { }
        public void Clear() { }
        public bool Contains(System.Windows.Documents.TableCell item) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Windows.Documents.TableCell[] array, int index) { }
        public int IndexOf(System.Windows.Documents.TableCell item) { throw null; }
        public void Insert(int index, System.Windows.Documents.TableCell item) { }
        public bool Remove(System.Windows.Documents.TableCell item) { throw null; }
        public void RemoveAt(int index) { }
        public void RemoveRange(int index, int count) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.TableCell> System.Collections.Generic.IEnumerable<System.Windows.Documents.TableCell>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        public void TrimToSize() { }
    }
    public partial class TableColumn : System.Windows.FrameworkContentElement
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty WidthProperty;
        public TableColumn() { }
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        public System.Windows.GridLength Width { get { throw null; } set { } }
    }
    public sealed partial class TableColumnCollection : System.Collections.Generic.ICollection<System.Windows.Documents.TableColumn>, System.Collections.Generic.IEnumerable<System.Windows.Documents.TableColumn>, System.Collections.Generic.IList<System.Windows.Documents.TableColumn>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal TableColumnCollection() { }
        public int Capacity { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Windows.Documents.TableColumn this[int index] { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(System.Windows.Documents.TableColumn item) { }
        public void Clear() { }
        public bool Contains(System.Windows.Documents.TableColumn item) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Windows.Documents.TableColumn[] array, int index) { }
        public int IndexOf(System.Windows.Documents.TableColumn item) { throw null; }
        public void Insert(int index, System.Windows.Documents.TableColumn item) { }
        public bool Remove(System.Windows.Documents.TableColumn item) { throw null; }
        public void RemoveAt(int index) { }
        public void RemoveRange(int index, int count) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.TableColumn> System.Collections.Generic.IEnumerable<System.Windows.Documents.TableColumn>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        public void TrimToSize() { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Cells")]
    public partial class TableRow : System.Windows.Documents.TextElement, System.Windows.Markup.IAddChild
    {
        public TableRow() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.TableCellCollection Cells { get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeCells() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public sealed partial class TableRowCollection : System.Collections.Generic.ICollection<System.Windows.Documents.TableRow>, System.Collections.Generic.IEnumerable<System.Windows.Documents.TableRow>, System.Collections.Generic.IList<System.Windows.Documents.TableRow>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal TableRowCollection() { }
        public int Capacity { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Windows.Documents.TableRow this[int index] { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(System.Windows.Documents.TableRow item) { }
        public void Clear() { }
        public bool Contains(System.Windows.Documents.TableRow item) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Windows.Documents.TableRow[] array, int index) { }
        public int IndexOf(System.Windows.Documents.TableRow item) { throw null; }
        public void Insert(int index, System.Windows.Documents.TableRow item) { }
        public bool Remove(System.Windows.Documents.TableRow item) { throw null; }
        public void RemoveAt(int index) { }
        public void RemoveRange(int index, int count) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.TableRow> System.Collections.Generic.IEnumerable<System.Windows.Documents.TableRow>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        public void TrimToSize() { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("Rows")]
    public partial class TableRowGroup : System.Windows.Documents.TextElement, System.Windows.Markup.IAddChild
    {
        public TableRowGroup() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Windows.Documents.TableRowCollection Rows { get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeRows() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public sealed partial class TableRowGroupCollection : System.Collections.Generic.ICollection<System.Windows.Documents.TableRowGroup>, System.Collections.Generic.IEnumerable<System.Windows.Documents.TableRowGroup>, System.Collections.Generic.IList<System.Windows.Documents.TableRowGroup>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal TableRowGroupCollection() { }
        public int Capacity { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Windows.Documents.TableRowGroup this[int index] { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(System.Windows.Documents.TableRowGroup item) { }
        public void Clear() { }
        public bool Contains(System.Windows.Documents.TableRowGroup item) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Windows.Documents.TableRowGroup[] array, int index) { }
        public int IndexOf(System.Windows.Documents.TableRowGroup item) { throw null; }
        public void Insert(int index, System.Windows.Documents.TableRowGroup item) { }
        public bool Remove(System.Windows.Documents.TableRowGroup item) { throw null; }
        public void RemoveAt(int index) { }
        public void RemoveRange(int index, int count) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.TableRowGroup> System.Collections.Generic.IEnumerable<System.Windows.Documents.TableRowGroup>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        public void TrimToSize() { }
    }
    public static partial class TextEffectResolver
    {
        public static System.Windows.Documents.TextEffectTarget[] Resolve(System.Windows.Documents.TextPointer startPosition, System.Windows.Documents.TextPointer endPosition, System.Windows.Media.TextEffect effect) { throw null; }
    }
    public partial class TextEffectTarget
    {
        internal TextEffectTarget() { }
        public System.Windows.DependencyObject Element { get { throw null; } }
        public bool IsEnabled { get { throw null; } }
        public System.Windows.Media.TextEffect TextEffect { get { throw null; } }
        public void Disable() { }
        public void Enable() { }
    }
    public abstract partial class TextElement : System.Windows.FrameworkContentElement, System.Windows.Markup.IAddChild
    {
        internal TextElement() { }
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty FontFamilyProperty;
        public static readonly System.Windows.DependencyProperty FontSizeProperty;
        public static readonly System.Windows.DependencyProperty FontStretchProperty;
        public static readonly System.Windows.DependencyProperty FontStyleProperty;
        public static readonly System.Windows.DependencyProperty FontWeightProperty;
        public static readonly System.Windows.DependencyProperty ForegroundProperty;
        public static readonly System.Windows.DependencyProperty TextEffectsProperty;
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        public System.Windows.Documents.TextPointer ContentEnd { get { throw null; } }
        public System.Windows.Documents.TextPointer ContentStart { get { throw null; } }
        public System.Windows.Documents.TextPointer ElementEnd { get { throw null; } }
        public System.Windows.Documents.TextPointer ElementStart { get { throw null; } }
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.Font, Modifiability=System.Windows.Modifiability.Unmodifiable)]
        public System.Windows.Media.FontFamily FontFamily { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.FontSizeConverter))]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None)]
        public double FontSize { get { throw null; } set { } }
        public System.Windows.FontStretch FontStretch { get { throw null; } set { } }
        public System.Windows.FontStyle FontStyle { get { throw null; } set { } }
        public System.Windows.FontWeight FontWeight { get { throw null; } set { } }
        public System.Windows.Media.Brush Foreground { get { throw null; } set { } }
        protected internal override System.Collections.IEnumerator LogicalChildren { get { throw null; } }
        public System.Windows.Media.TextEffectCollection TextEffects { get { throw null; } set { } }
        public System.Windows.Documents.Typography Typography { get { throw null; } }
        public static System.Windows.Media.FontFamily GetFontFamily(System.Windows.DependencyObject element) { throw null; }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.FontSizeConverter))]
        public static double GetFontSize(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.FontStretch GetFontStretch(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.FontStyle GetFontStyle(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.FontWeight GetFontWeight(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.Media.Brush GetForeground(System.Windows.DependencyObject element) { throw null; }
        protected override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        public static void SetFontFamily(System.Windows.DependencyObject element, System.Windows.Media.FontFamily value) { }
        public static void SetFontSize(System.Windows.DependencyObject element, double value) { }
        public static void SetFontStretch(System.Windows.DependencyObject element, System.Windows.FontStretch value) { }
        public static void SetFontStyle(System.Windows.DependencyObject element, System.Windows.FontStyle value) { }
        public static void SetFontWeight(System.Windows.DependencyObject element, System.Windows.FontWeight value) { }
        public static void SetForeground(System.Windows.DependencyObject element, System.Windows.Media.Brush value) { }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class TextElementCollection<TextElementType> : System.Collections.Generic.ICollection<TextElementType>, System.Collections.Generic.IEnumerable<TextElementType>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList where TextElementType : System.Windows.Documents.TextElement
    {
        internal TextElementCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(TextElementType item) { }
        public void AddRange(System.Collections.IEnumerable range) { }
        public void Clear() { }
        public bool Contains(TextElementType item) { throw null; }
        public void CopyTo(TextElementType[] array, int arrayIndex) { }
        public System.Collections.Generic.IEnumerator<TextElementType> GetEnumerator() { throw null; }
        public void InsertAfter(TextElementType previousSibling, TextElementType newItem) { }
        public void InsertBefore(TextElementType nextSibling, TextElementType newItem) { }
        public bool Remove(TextElementType item) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
    public sealed partial class TextElementEditingBehaviorAttribute : System.Attribute
    {
        public TextElementEditingBehaviorAttribute() { }
        public bool IsMergeable { get { throw null; } set { } }
        public bool IsTypographicOnly { get { throw null; } set { } }
    }
    public partial class TextPointer : System.Windows.Documents.ContentPosition
    {
        internal TextPointer() { }
        public System.Windows.Documents.TextPointer DocumentEnd { get { throw null; } }
        public System.Windows.Documents.TextPointer DocumentStart { get { throw null; } }
        public bool HasValidLayout { get { throw null; } }
        public bool IsAtInsertionPosition { get { throw null; } }
        public bool IsAtLineStartPosition { get { throw null; } }
        public System.Windows.Documents.LogicalDirection LogicalDirection { get { throw null; } }
        public System.Windows.Documents.Paragraph Paragraph { get { throw null; } }
        public System.Windows.DependencyObject Parent { get { throw null; } }
        public int CompareTo(System.Windows.Documents.TextPointer position) { throw null; }
        public int DeleteTextInRun(int count) { throw null; }
        public System.Windows.DependencyObject GetAdjacentElement(System.Windows.Documents.LogicalDirection direction) { throw null; }
        public System.Windows.Rect GetCharacterRect(System.Windows.Documents.LogicalDirection direction) { throw null; }
        public System.Windows.Documents.TextPointer GetInsertionPosition(System.Windows.Documents.LogicalDirection direction) { throw null; }
        public System.Windows.Documents.TextPointer GetLineStartPosition(int count) { throw null; }
        public System.Windows.Documents.TextPointer GetLineStartPosition(int count, out int actualCount) { throw null; }
        public System.Windows.Documents.TextPointer GetNextContextPosition(System.Windows.Documents.LogicalDirection direction) { throw null; }
        public System.Windows.Documents.TextPointer GetNextInsertionPosition(System.Windows.Documents.LogicalDirection direction) { throw null; }
        public int GetOffsetToPosition(System.Windows.Documents.TextPointer position) { throw null; }
        public System.Windows.Documents.TextPointerContext GetPointerContext(System.Windows.Documents.LogicalDirection direction) { throw null; }
        public System.Windows.Documents.TextPointer GetPositionAtOffset(int offset) { throw null; }
        public System.Windows.Documents.TextPointer GetPositionAtOffset(int offset, System.Windows.Documents.LogicalDirection direction) { throw null; }
        public string GetTextInRun(System.Windows.Documents.LogicalDirection direction) { throw null; }
        public int GetTextInRun(System.Windows.Documents.LogicalDirection direction, char[] textBuffer, int startIndex, int count) { throw null; }
        public int GetTextRunLength(System.Windows.Documents.LogicalDirection direction) { throw null; }
        public System.Windows.Documents.TextPointer InsertLineBreak() { throw null; }
        public System.Windows.Documents.TextPointer InsertParagraphBreak() { throw null; }
        public void InsertTextInRun(string textData) { }
        public bool IsInSameDocument(System.Windows.Documents.TextPointer textPosition) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum TextPointerContext
    {
        None = 0,
        Text = 1,
        EmbeddedElement = 2,
        ElementStart = 3,
        ElementEnd = 4,
    }
    public partial class TextRange
    {
        public TextRange(System.Windows.Documents.TextPointer position1, System.Windows.Documents.TextPointer position2) { }
        public System.Windows.Documents.TextPointer End { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public System.Windows.Documents.TextPointer Start { get { throw null; } }
        public string Text { get { throw null; } set { } }
        public event System.EventHandler Changed { add { } remove { } }
        public void ApplyPropertyValue(System.Windows.DependencyProperty formattingProperty, object value) { }
        public bool CanLoad(string dataFormat) { throw null; }
        public bool CanSave(string dataFormat) { throw null; }
        public void ClearAllProperties() { }
        public bool Contains(System.Windows.Documents.TextPointer textPointer) { throw null; }
        public object GetPropertyValue(System.Windows.DependencyProperty formattingProperty) { throw null; }
        public void Load(System.IO.Stream stream, string dataFormat) { }
        public void Save(System.IO.Stream stream, string dataFormat) { }
        public void Save(System.IO.Stream stream, string dataFormat, bool preserveTextElements) { }
        public void Select(System.Windows.Documents.TextPointer position1, System.Windows.Documents.TextPointer position2) { }
    }
    public sealed partial class TextSelection : System.Windows.Documents.TextRange
    {
        internal TextSelection() : base (default(System.Windows.Documents.TextPointer), default(System.Windows.Documents.TextPointer)) { }
    }
    public sealed partial class Typography
    {
        internal Typography() { }
        public static readonly System.Windows.DependencyProperty AnnotationAlternatesProperty;
        public static readonly System.Windows.DependencyProperty CapitalSpacingProperty;
        public static readonly System.Windows.DependencyProperty CapitalsProperty;
        public static readonly System.Windows.DependencyProperty CaseSensitiveFormsProperty;
        public static readonly System.Windows.DependencyProperty ContextualAlternatesProperty;
        public static readonly System.Windows.DependencyProperty ContextualLigaturesProperty;
        public static readonly System.Windows.DependencyProperty ContextualSwashesProperty;
        public static readonly System.Windows.DependencyProperty DiscretionaryLigaturesProperty;
        public static readonly System.Windows.DependencyProperty EastAsianExpertFormsProperty;
        public static readonly System.Windows.DependencyProperty EastAsianLanguageProperty;
        public static readonly System.Windows.DependencyProperty EastAsianWidthsProperty;
        public static readonly System.Windows.DependencyProperty FractionProperty;
        public static readonly System.Windows.DependencyProperty HistoricalFormsProperty;
        public static readonly System.Windows.DependencyProperty HistoricalLigaturesProperty;
        public static readonly System.Windows.DependencyProperty KerningProperty;
        public static readonly System.Windows.DependencyProperty MathematicalGreekProperty;
        public static readonly System.Windows.DependencyProperty NumeralAlignmentProperty;
        public static readonly System.Windows.DependencyProperty NumeralStyleProperty;
        public static readonly System.Windows.DependencyProperty SlashedZeroProperty;
        public static readonly System.Windows.DependencyProperty StandardLigaturesProperty;
        public static readonly System.Windows.DependencyProperty StandardSwashesProperty;
        public static readonly System.Windows.DependencyProperty StylisticAlternatesProperty;
        public static readonly System.Windows.DependencyProperty StylisticSet10Property;
        public static readonly System.Windows.DependencyProperty StylisticSet11Property;
        public static readonly System.Windows.DependencyProperty StylisticSet12Property;
        public static readonly System.Windows.DependencyProperty StylisticSet13Property;
        public static readonly System.Windows.DependencyProperty StylisticSet14Property;
        public static readonly System.Windows.DependencyProperty StylisticSet15Property;
        public static readonly System.Windows.DependencyProperty StylisticSet16Property;
        public static readonly System.Windows.DependencyProperty StylisticSet17Property;
        public static readonly System.Windows.DependencyProperty StylisticSet18Property;
        public static readonly System.Windows.DependencyProperty StylisticSet19Property;
        public static readonly System.Windows.DependencyProperty StylisticSet1Property;
        public static readonly System.Windows.DependencyProperty StylisticSet20Property;
        public static readonly System.Windows.DependencyProperty StylisticSet2Property;
        public static readonly System.Windows.DependencyProperty StylisticSet3Property;
        public static readonly System.Windows.DependencyProperty StylisticSet4Property;
        public static readonly System.Windows.DependencyProperty StylisticSet5Property;
        public static readonly System.Windows.DependencyProperty StylisticSet6Property;
        public static readonly System.Windows.DependencyProperty StylisticSet7Property;
        public static readonly System.Windows.DependencyProperty StylisticSet8Property;
        public static readonly System.Windows.DependencyProperty StylisticSet9Property;
        public static readonly System.Windows.DependencyProperty VariantsProperty;
        public int AnnotationAlternates { get { throw null; } set { } }
        public System.Windows.FontCapitals Capitals { get { throw null; } set { } }
        public bool CapitalSpacing { get { throw null; } set { } }
        public bool CaseSensitiveForms { get { throw null; } set { } }
        public bool ContextualAlternates { get { throw null; } set { } }
        public bool ContextualLigatures { get { throw null; } set { } }
        public int ContextualSwashes { get { throw null; } set { } }
        public bool DiscretionaryLigatures { get { throw null; } set { } }
        public bool EastAsianExpertForms { get { throw null; } set { } }
        public System.Windows.FontEastAsianLanguage EastAsianLanguage { get { throw null; } set { } }
        public System.Windows.FontEastAsianWidths EastAsianWidths { get { throw null; } set { } }
        public System.Windows.FontFraction Fraction { get { throw null; } set { } }
        public bool HistoricalForms { get { throw null; } set { } }
        public bool HistoricalLigatures { get { throw null; } set { } }
        public bool Kerning { get { throw null; } set { } }
        public bool MathematicalGreek { get { throw null; } set { } }
        public System.Windows.FontNumeralAlignment NumeralAlignment { get { throw null; } set { } }
        public System.Windows.FontNumeralStyle NumeralStyle { get { throw null; } set { } }
        public bool SlashedZero { get { throw null; } set { } }
        public bool StandardLigatures { get { throw null; } set { } }
        public int StandardSwashes { get { throw null; } set { } }
        public int StylisticAlternates { get { throw null; } set { } }
        public bool StylisticSet1 { get { throw null; } set { } }
        public bool StylisticSet10 { get { throw null; } set { } }
        public bool StylisticSet11 { get { throw null; } set { } }
        public bool StylisticSet12 { get { throw null; } set { } }
        public bool StylisticSet13 { get { throw null; } set { } }
        public bool StylisticSet14 { get { throw null; } set { } }
        public bool StylisticSet15 { get { throw null; } set { } }
        public bool StylisticSet16 { get { throw null; } set { } }
        public bool StylisticSet17 { get { throw null; } set { } }
        public bool StylisticSet18 { get { throw null; } set { } }
        public bool StylisticSet19 { get { throw null; } set { } }
        public bool StylisticSet2 { get { throw null; } set { } }
        public bool StylisticSet20 { get { throw null; } set { } }
        public bool StylisticSet3 { get { throw null; } set { } }
        public bool StylisticSet4 { get { throw null; } set { } }
        public bool StylisticSet5 { get { throw null; } set { } }
        public bool StylisticSet6 { get { throw null; } set { } }
        public bool StylisticSet7 { get { throw null; } set { } }
        public bool StylisticSet8 { get { throw null; } set { } }
        public bool StylisticSet9 { get { throw null; } set { } }
        public System.Windows.FontVariants Variants { get { throw null; } set { } }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static int GetAnnotationAlternates(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.FontCapitals GetCapitals(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetCapitalSpacing(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetCaseSensitiveForms(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetContextualAlternates(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetContextualLigatures(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static int GetContextualSwashes(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetDiscretionaryLigatures(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetEastAsianExpertForms(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.FontEastAsianLanguage GetEastAsianLanguage(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.FontEastAsianWidths GetEastAsianWidths(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.FontFraction GetFraction(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetHistoricalForms(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetHistoricalLigatures(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetKerning(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetMathematicalGreek(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.FontNumeralAlignment GetNumeralAlignment(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.FontNumeralStyle GetNumeralStyle(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetSlashedZero(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStandardLigatures(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static int GetStandardSwashes(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static int GetStylisticAlternates(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet1(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet10(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet11(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet12(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet13(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet14(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet15(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet16(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet17(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet18(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet19(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet2(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet20(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet3(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet4(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet5(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet6(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet7(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet8(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetStylisticSet9(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.FontVariants GetVariants(System.Windows.DependencyObject element) { throw null; }
        public static void SetAnnotationAlternates(System.Windows.DependencyObject element, int value) { }
        public static void SetCapitals(System.Windows.DependencyObject element, System.Windows.FontCapitals value) { }
        public static void SetCapitalSpacing(System.Windows.DependencyObject element, bool value) { }
        public static void SetCaseSensitiveForms(System.Windows.DependencyObject element, bool value) { }
        public static void SetContextualAlternates(System.Windows.DependencyObject element, bool value) { }
        public static void SetContextualLigatures(System.Windows.DependencyObject element, bool value) { }
        public static void SetContextualSwashes(System.Windows.DependencyObject element, int value) { }
        public static void SetDiscretionaryLigatures(System.Windows.DependencyObject element, bool value) { }
        public static void SetEastAsianExpertForms(System.Windows.DependencyObject element, bool value) { }
        public static void SetEastAsianLanguage(System.Windows.DependencyObject element, System.Windows.FontEastAsianLanguage value) { }
        public static void SetEastAsianWidths(System.Windows.DependencyObject element, System.Windows.FontEastAsianWidths value) { }
        public static void SetFraction(System.Windows.DependencyObject element, System.Windows.FontFraction value) { }
        public static void SetHistoricalForms(System.Windows.DependencyObject element, bool value) { }
        public static void SetHistoricalLigatures(System.Windows.DependencyObject element, bool value) { }
        public static void SetKerning(System.Windows.DependencyObject element, bool value) { }
        public static void SetMathematicalGreek(System.Windows.DependencyObject element, bool value) { }
        public static void SetNumeralAlignment(System.Windows.DependencyObject element, System.Windows.FontNumeralAlignment value) { }
        public static void SetNumeralStyle(System.Windows.DependencyObject element, System.Windows.FontNumeralStyle value) { }
        public static void SetSlashedZero(System.Windows.DependencyObject element, bool value) { }
        public static void SetStandardLigatures(System.Windows.DependencyObject element, bool value) { }
        public static void SetStandardSwashes(System.Windows.DependencyObject element, int value) { }
        public static void SetStylisticAlternates(System.Windows.DependencyObject element, int value) { }
        public static void SetStylisticSet1(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet10(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet11(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet12(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet13(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet14(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet15(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet16(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet17(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet18(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet19(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet2(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet20(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet3(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet4(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet5(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet6(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet7(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet8(System.Windows.DependencyObject element, bool value) { }
        public static void SetStylisticSet9(System.Windows.DependencyObject element, bool value) { }
        public static void SetVariants(System.Windows.DependencyObject element, System.Windows.FontVariants value) { }
    }
    public partial class Underline : System.Windows.Documents.Span
    {
        public Underline() { }
        public Underline(System.Windows.Documents.Inline childInline) { }
        public Underline(System.Windows.Documents.Inline childInline, System.Windows.Documents.TextPointer insertionPosition) { }
        public Underline(System.Windows.Documents.TextPointer start, System.Windows.Documents.TextPointer end) { }
    }
    public sealed partial class ZoomPercentageConverter : System.Windows.Data.IValueConverter
    {
        public ZoomPercentageConverter() { }
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
    }
}
namespace System.Windows.Documents.DocumentStructures
{
    public partial class BlockElement
    {
        public BlockElement() { }
    }
    public partial class FigureStructure : System.Windows.Documents.DocumentStructures.SemanticBasicElement, System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.NamedElement>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild
    {
        public FigureStructure() { }
        public void Add(System.Windows.Documents.DocumentStructures.NamedElement element) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentStructures.NamedElement> System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.NamedElement>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class ListItemStructure : System.Windows.Documents.DocumentStructures.SemanticBasicElement, System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.BlockElement>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild
    {
        public ListItemStructure() { }
        public string Marker { get { throw null; } set { } }
        public void Add(System.Windows.Documents.DocumentStructures.BlockElement element) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentStructures.BlockElement> System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.BlockElement>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class ListStructure : System.Windows.Documents.DocumentStructures.SemanticBasicElement, System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.ListItemStructure>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild
    {
        public ListStructure() { }
        public void Add(System.Windows.Documents.DocumentStructures.ListItemStructure listItem) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentStructures.ListItemStructure> System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.ListItemStructure>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class NamedElement : System.Windows.Documents.DocumentStructures.BlockElement
    {
        public NamedElement() { }
        public string NameReference { get { throw null; } set { } }
    }
    public partial class ParagraphStructure : System.Windows.Documents.DocumentStructures.SemanticBasicElement, System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.NamedElement>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild
    {
        public ParagraphStructure() { }
        public void Add(System.Windows.Documents.DocumentStructures.NamedElement element) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentStructures.NamedElement> System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.NamedElement>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class SectionStructure : System.Windows.Documents.DocumentStructures.SemanticBasicElement, System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.BlockElement>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild
    {
        public SectionStructure() { }
        public void Add(System.Windows.Documents.DocumentStructures.BlockElement element) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentStructures.BlockElement> System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.BlockElement>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class SemanticBasicElement : System.Windows.Documents.DocumentStructures.BlockElement
    {
        internal SemanticBasicElement() { }
    }
    public partial class StoryBreak : System.Windows.Documents.DocumentStructures.BlockElement
    {
        public StoryBreak() { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("BlockElementList")]
    public partial class StoryFragment : System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.BlockElement>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild
    {
        public StoryFragment() { }
        public string FragmentName { get { throw null; } set { } }
        public string FragmentType { get { throw null; } set { } }
        public string StoryName { get { throw null; } set { } }
        public void Add(System.Windows.Documents.DocumentStructures.BlockElement element) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentStructures.BlockElement> System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.BlockElement>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    [System.Windows.Markup.ContentPropertyAttribute("StoryFragmentList")]
    public partial class StoryFragments : System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.StoryFragment>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild
    {
        public StoryFragments() { }
        public void Add(System.Windows.Documents.DocumentStructures.StoryFragment storyFragment) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentStructures.StoryFragment> System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.StoryFragment>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class TableCellStructure : System.Windows.Documents.DocumentStructures.SemanticBasicElement, System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.BlockElement>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild
    {
        public TableCellStructure() { }
        public int ColumnSpan { get { throw null; } set { } }
        public int RowSpan { get { throw null; } set { } }
        public void Add(System.Windows.Documents.DocumentStructures.BlockElement element) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentStructures.BlockElement> System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.BlockElement>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class TableRowGroupStructure : System.Windows.Documents.DocumentStructures.SemanticBasicElement, System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.TableRowStructure>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild
    {
        public TableRowGroupStructure() { }
        public void Add(System.Windows.Documents.DocumentStructures.TableRowStructure tableRow) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentStructures.TableRowStructure> System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.TableRowStructure>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class TableRowStructure : System.Windows.Documents.DocumentStructures.SemanticBasicElement, System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.TableCellStructure>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild
    {
        public TableRowStructure() { }
        public void Add(System.Windows.Documents.DocumentStructures.TableCellStructure tableCell) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentStructures.TableCellStructure> System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.TableCellStructure>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
    public partial class TableStructure : System.Windows.Documents.DocumentStructures.SemanticBasicElement, System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.TableRowGroupStructure>, System.Collections.IEnumerable, System.Windows.Markup.IAddChild
    {
        public TableStructure() { }
        public void Add(System.Windows.Documents.DocumentStructures.TableRowGroupStructure tableRowGroup) { }
        System.Collections.Generic.IEnumerator<System.Windows.Documents.DocumentStructures.TableRowGroupStructure> System.Collections.Generic.IEnumerable<System.Windows.Documents.DocumentStructures.TableRowGroupStructure>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object value) { }
        void System.Windows.Markup.IAddChild.AddText(string text) { }
    }
}
namespace System.Windows.Documents.Serialization
{
    public partial interface ISerializerFactory
    {
        string DefaultFileExtension { get; }
        string DisplayName { get; }
        string ManufacturerName { get; }
        System.Uri ManufacturerWebsite { get; }
        System.Windows.Documents.Serialization.SerializerWriter CreateSerializerWriter(System.IO.Stream stream);
    }
    public sealed partial class SerializerDescriptor
    {
        internal SerializerDescriptor() { }
        public string AssemblyName { get { throw null; } }
        public string AssemblyPath { get { throw null; } }
        public System.Version AssemblyVersion { get { throw null; } }
        public string DefaultFileExtension { get { throw null; } }
        public string DisplayName { get { throw null; } }
        public string FactoryInterfaceName { get { throw null; } }
        public bool IsLoadable { get { throw null; } }
        public string ManufacturerName { get { throw null; } }
        public System.Uri ManufacturerWebsite { get { throw null; } }
        public System.Version WinFXVersion { get { throw null; } }
        public static System.Windows.Documents.Serialization.SerializerDescriptor CreateFromFactoryInstance(System.Windows.Documents.Serialization.ISerializerFactory factoryInstance) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public sealed partial class SerializerProvider
    {
        public SerializerProvider() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Documents.Serialization.SerializerDescriptor> InstalledSerializers { get { throw null; } }
        public System.Windows.Documents.Serialization.SerializerWriter CreateSerializerWriter(System.Windows.Documents.Serialization.SerializerDescriptor serializerDescriptor, System.IO.Stream stream) { throw null; }
        public static void RegisterSerializer(System.Windows.Documents.Serialization.SerializerDescriptor serializerDescriptor, bool overwrite) { }
        public static void UnregisterSerializer(System.Windows.Documents.Serialization.SerializerDescriptor serializerDescriptor) { }
    }
    public abstract partial class SerializerWriter
    {
        protected SerializerWriter() { }
        public abstract event System.Windows.Documents.Serialization.WritingCancelledEventHandler WritingCancelled;
        public abstract event System.Windows.Documents.Serialization.WritingCompletedEventHandler WritingCompleted;
        public abstract event System.Windows.Documents.Serialization.WritingPrintTicketRequiredEventHandler WritingPrintTicketRequired;
        public abstract event System.Windows.Documents.Serialization.WritingProgressChangedEventHandler WritingProgressChanged;
        public abstract void CancelAsync();
        public abstract System.Windows.Documents.Serialization.SerializerWriterCollator CreateVisualsCollator();
        public abstract System.Windows.Documents.Serialization.SerializerWriterCollator CreateVisualsCollator(System.Printing.PrintTicket documentSequencePT, System.Printing.PrintTicket documentPT);
        public abstract void Write(System.Windows.Documents.DocumentPaginator documentPaginator);
        public abstract void Write(System.Windows.Documents.DocumentPaginator documentPaginator, System.Printing.PrintTicket printTicket);
        public abstract void Write(System.Windows.Documents.FixedDocument fixedDocument);
        public abstract void Write(System.Windows.Documents.FixedDocument fixedDocument, System.Printing.PrintTicket printTicket);
        public abstract void Write(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence);
        public abstract void Write(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, System.Printing.PrintTicket printTicket);
        public abstract void Write(System.Windows.Documents.FixedPage fixedPage);
        public abstract void Write(System.Windows.Documents.FixedPage fixedPage, System.Printing.PrintTicket printTicket);
        public abstract void Write(System.Windows.Media.Visual visual);
        public abstract void Write(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator);
        public abstract void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator, object userState);
        public abstract void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator, System.Printing.PrintTicket printTicket, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument, System.Printing.PrintTicket printTicket, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, System.Printing.PrintTicket printTicket, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedPage fixedPage);
        public abstract void WriteAsync(System.Windows.Documents.FixedPage fixedPage, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedPage fixedPage, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Documents.FixedPage fixedPage, System.Printing.PrintTicket printTicket, object userState);
        public abstract void WriteAsync(System.Windows.Media.Visual visual);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, object userState);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket, object userState);
    }
    public abstract partial class SerializerWriterCollator
    {
        protected SerializerWriterCollator() { }
        public abstract void BeginBatchWrite();
        public abstract void Cancel();
        public abstract void CancelAsync();
        public abstract void EndBatchWrite();
        public abstract void Write(System.Windows.Media.Visual visual);
        public abstract void Write(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Media.Visual visual);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, object userState);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket, object userState);
    }
    public partial class WritingCancelledEventArgs : System.EventArgs
    {
        public WritingCancelledEventArgs(System.Exception exception) { }
        public System.Exception Error { get { throw null; } }
    }
    public delegate void WritingCancelledEventHandler(object sender, System.Windows.Documents.Serialization.WritingCancelledEventArgs e);
    public partial class WritingCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        public WritingCompletedEventArgs(bool cancelled, object state, System.Exception exception) : base (default(System.Exception), default(bool), default(object)) { }
    }
    public delegate void WritingCompletedEventHandler(object sender, System.Windows.Documents.Serialization.WritingCompletedEventArgs e);
    public partial class WritingPrintTicketRequiredEventArgs : System.EventArgs
    {
        public WritingPrintTicketRequiredEventArgs(System.Windows.Xps.Serialization.PrintTicketLevel printTicketLevel, int sequence) { }
        public System.Printing.PrintTicket CurrentPrintTicket { get { throw null; } set { } }
        public System.Windows.Xps.Serialization.PrintTicketLevel CurrentPrintTicketLevel { get { throw null; } }
        public int Sequence { get { throw null; } }
    }
    public delegate void WritingPrintTicketRequiredEventHandler(object sender, System.Windows.Documents.Serialization.WritingPrintTicketRequiredEventArgs e);
    public partial class WritingProgressChangedEventArgs : System.ComponentModel.ProgressChangedEventArgs
    {
        public WritingProgressChangedEventArgs(System.Windows.Documents.Serialization.WritingProgressChangeLevel writingLevel, int number, int progressPercentage, object state) : base (default(int), default(object)) { }
        public int Number { get { throw null; } }
        public System.Windows.Documents.Serialization.WritingProgressChangeLevel WritingLevel { get { throw null; } }
    }
    public delegate void WritingProgressChangedEventHandler(object sender, System.Windows.Documents.Serialization.WritingProgressChangedEventArgs e);
    public enum WritingProgressChangeLevel
    {
        None = 0,
        FixedDocumentSequenceWritingProgress = 1,
        FixedDocumentWritingProgress = 2,
        FixedPageWritingProgress = 3,
    }
}
namespace System.Windows.Input
{
    public sealed partial class CommandConverter : System.ComponentModel.TypeConverter
    {
        public CommandConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public sealed partial class KeyboardNavigation
    {
        internal KeyboardNavigation() { }
        public static readonly System.Windows.DependencyProperty AcceptsReturnProperty;
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public static readonly System.Windows.DependencyProperty ControlTabNavigationProperty;
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public static readonly System.Windows.DependencyProperty DirectionalNavigationProperty;
        public static readonly System.Windows.DependencyProperty IsTabStopProperty;
        public static readonly System.Windows.DependencyProperty TabIndexProperty;
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public static readonly System.Windows.DependencyProperty TabNavigationProperty;
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetAcceptsReturn(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.Input.KeyboardNavigationMode GetControlTabNavigation(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.Input.KeyboardNavigationMode GetDirectionalNavigation(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static bool GetIsTabStop(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static int GetTabIndex(System.Windows.DependencyObject element) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.Input.KeyboardNavigationMode GetTabNavigation(System.Windows.DependencyObject element) { throw null; }
        public static void SetAcceptsReturn(System.Windows.DependencyObject element, bool enabled) { }
        public static void SetControlTabNavigation(System.Windows.DependencyObject element, System.Windows.Input.KeyboardNavigationMode mode) { }
        public static void SetDirectionalNavigation(System.Windows.DependencyObject element, System.Windows.Input.KeyboardNavigationMode mode) { }
        public static void SetIsTabStop(System.Windows.DependencyObject element, bool isTabStop) { }
        public static void SetTabIndex(System.Windows.DependencyObject element, int index) { }
        public static void SetTabNavigation(System.Windows.DependencyObject element, System.Windows.Input.KeyboardNavigationMode mode) { }
    }
    public enum KeyboardNavigationMode
    {
        Continue = 0,
        Once = 1,
        Cycle = 2,
        None = 3,
        Contained = 4,
        Local = 5,
    }
}
namespace System.Windows.Interop
{
    public partial class ActiveXHost : System.Windows.Interop.HwndHost
    {
        internal ActiveXHost() { }
        protected bool IsDisposed { get { throw null; } }
        protected override System.Runtime.InteropServices.HandleRef BuildWindowCore(System.Runtime.InteropServices.HandleRef hwndParent) { throw null; }
        protected override void DestroyWindowCore(System.Runtime.InteropServices.HandleRef hwnd) { }
        protected override void Dispose(bool disposing) { }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size swConstraint) { throw null; }
        protected override void OnAccessKey(System.Windows.Input.AccessKeyEventArgs args) { }
        protected override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected override void OnWindowPositionChanged(System.Windows.Rect bounds) { }
    }
    public static partial class BrowserInteropHelper
    {
        public static object ClientSite { get { throw null; } }
        public static dynamic HostScript { get { throw null; } }
        public static bool IsBrowserHosted { get { throw null; } }
        public static System.Uri Source { get { throw null; } }
    }
    public sealed partial class DynamicScriptObject : System.Dynamic.DynamicObject
    {
        internal DynamicScriptObject() { }
        public override string ToString() { throw null; }
        public override bool TryGetIndex(System.Dynamic.GetIndexBinder binder, object[] indexes, out object result) { throw null; }
        public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result) { throw null; }
        public override bool TryInvoke(System.Dynamic.InvokeBinder binder, object[] args, out object result) { throw null; }
        public override bool TryInvokeMember(System.Dynamic.InvokeMemberBinder binder, object[] args, out object result) { throw null; }
        public override bool TrySetIndex(System.Dynamic.SetIndexBinder binder, object[] indexes, object value) { throw null; }
        public override bool TrySetMember(System.Dynamic.SetMemberBinder binder, object value) { throw null; }
    }
    public abstract partial class HwndHost : System.Windows.FrameworkElement, System.IDisposable, System.Windows.Interop.IKeyboardInputSink, System.Windows.Interop.IWin32Window
    {
        public static readonly System.Windows.RoutedEvent DpiChangedEvent;
        protected HwndHost() { }
        public System.IntPtr Handle { get { throw null; } }
        System.Windows.Interop.IKeyboardInputSite System.Windows.Interop.IKeyboardInputSink.KeyboardInputSite { get { throw null; } set { } }
        public event System.Windows.DpiChangedEventHandler DpiChanged { add { } remove { } }
        public event System.Windows.Interop.HwndSourceHook MessageHook { add { } remove { } }
        protected abstract System.Runtime.InteropServices.HandleRef BuildWindowCore(System.Runtime.InteropServices.HandleRef hwndParent);
        protected abstract void DestroyWindowCore(System.Runtime.InteropServices.HandleRef hwnd);
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~HwndHost() { }
        protected virtual bool HasFocusWithinCore() { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        protected override void OnDpiChanged(System.Windows.DpiScale oldDpi, System.Windows.DpiScale newDpi) { }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) { }
        protected override void OnKeyUp(System.Windows.Input.KeyEventArgs e) { }
        protected virtual bool OnMnemonicCore(ref System.Windows.Interop.MSG msg, System.Windows.Input.ModifierKeys modifiers) { throw null; }
        protected virtual void OnWindowPositionChanged(System.Windows.Rect rcBoundingBox) { }
        protected virtual System.Windows.Interop.IKeyboardInputSite RegisterKeyboardInputSinkCore(System.Windows.Interop.IKeyboardInputSink sink) { throw null; }
        bool System.Windows.Interop.IKeyboardInputSink.HasFocusWithin() { throw null; }
        bool System.Windows.Interop.IKeyboardInputSink.OnMnemonic(ref System.Windows.Interop.MSG msg, System.Windows.Input.ModifierKeys modifiers) { throw null; }
        System.Windows.Interop.IKeyboardInputSite System.Windows.Interop.IKeyboardInputSink.RegisterKeyboardInputSink(System.Windows.Interop.IKeyboardInputSink sink) { throw null; }
        bool System.Windows.Interop.IKeyboardInputSink.TabInto(System.Windows.Input.TraversalRequest request) { throw null; }
        bool System.Windows.Interop.IKeyboardInputSink.TranslateAccelerator(ref System.Windows.Interop.MSG msg, System.Windows.Input.ModifierKeys modifiers) { throw null; }
        bool System.Windows.Interop.IKeyboardInputSink.TranslateChar(ref System.Windows.Interop.MSG msg, System.Windows.Input.ModifierKeys modifiers) { throw null; }
        protected virtual bool TabIntoCore(System.Windows.Input.TraversalRequest request) { throw null; }
        protected virtual bool TranslateAcceleratorCore(ref System.Windows.Interop.MSG msg, System.Windows.Input.ModifierKeys modifiers) { throw null; }
        protected virtual bool TranslateCharCore(ref System.Windows.Interop.MSG msg, System.Windows.Input.ModifierKeys modifiers) { throw null; }
        public void UpdateWindowPos() { }
        protected virtual System.IntPtr WndProc(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam, ref bool handled) { throw null; }
    }
    public partial interface IErrorPage
    {
        System.Uri DeploymentPath { get; set; }
        bool ErrorFlag { get; set; }
        string ErrorText { get; set; }
        string ErrorTitle { get; set; }
        System.Windows.Threading.DispatcherOperationCallback GetWinFxCallback { get; set; }
        string LogFilePath { get; set; }
        System.Windows.Threading.DispatcherOperationCallback RefreshCallback { get; set; }
        System.Uri SupportUri { get; set; }
    }
    public partial interface IProgressPage
    {
        string ApplicationName { get; set; }
        System.Uri DeploymentPath { get; set; }
        string PublisherName { get; set; }
        System.Windows.Threading.DispatcherOperationCallback RefreshCallback { get; set; }
        System.Windows.Threading.DispatcherOperationCallback StopCallback { get; set; }
        void UpdateProgress(long bytesDownloaded, long bytesTotal);
    }
    public sealed partial class WindowInteropHelper
    {
        public WindowInteropHelper(System.Windows.Window window) { }
        public System.IntPtr Handle { get { throw null; } }
        public System.IntPtr Owner { get { throw null; } set { } }
        public System.IntPtr EnsureHandle() { throw null; }
    }
}
namespace System.Windows.Markup
{
    public partial class ComponentResourceKeyConverter : System.Windows.ExpressionConverter
    {
        public ComponentResourceKeyConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public sealed partial class DependencyPropertyConverter : System.ComponentModel.TypeConverter
    {
        public DependencyPropertyConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public sealed partial class EventSetterHandlerConverter : System.ComponentModel.TypeConverter
    {
        public EventSetterHandlerConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object value, System.Type destinationType) { throw null; }
    }
    public partial interface IStyleConnector
    {
        void Connect(int connectionId, object target);
    }
    public partial class NamespaceMapEntry
    {
        public NamespaceMapEntry() { }
        public NamespaceMapEntry(string xmlNamespace, string assemblyName, string clrNamespace) { }
        public string AssemblyName { get { throw null; } set { } }
        public string ClrNamespace { get { throw null; } set { } }
        public string XmlNamespace { get { throw null; } set { } }
    }
    public partial class ParserContext : System.Windows.Markup.IUriContext
    {
        public ParserContext() { }
        public ParserContext(System.Xml.XmlParserContext xmlParserContext) { }
        public System.Uri BaseUri { get { throw null; } set { } }
        public System.Windows.Markup.XamlTypeMapper XamlTypeMapper { get { throw null; } set { } }
        public string XmlLang { get { throw null; } set { } }
        public System.Windows.Markup.XmlnsDictionary XmlnsDictionary { get { throw null; } }
        public string XmlSpace { get { throw null; } set { } }
        public static implicit operator System.Xml.XmlParserContext (System.Windows.Markup.ParserContext parserContext) { throw null; }
        public static System.Xml.XmlParserContext ToXmlParserContext(System.Windows.Markup.ParserContext parserContext) { throw null; }
    }
    public partial class ResourceReferenceExpressionConverter : System.Windows.ExpressionConverter
    {
        public ResourceReferenceExpressionConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public sealed partial class RoutedEventConverter : System.ComponentModel.TypeConverter
    {
        public RoutedEventConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext typeDescriptorContext, System.Globalization.CultureInfo cultureInfo, object value, System.Type destinationType) { throw null; }
    }
    public sealed partial class SetterTriggerConditionValueConverter : System.ComponentModel.TypeConverter
    {
        public SetterTriggerConditionValueConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public sealed partial class TemplateKeyConverter : System.ComponentModel.TypeConverter
    {
        public TemplateKeyConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public partial class XamlDesignerSerializationManager : System.Windows.Markup.ServiceProviders
    {
        public XamlDesignerSerializationManager(System.Xml.XmlWriter xmlWriter) { }
        public System.Windows.Markup.XamlWriterMode XamlWriterMode { get { throw null; } set { } }
    }
    public abstract partial class XamlInstanceCreator
    {
        protected XamlInstanceCreator() { }
        public abstract object CreateObject();
    }
    public partial class XamlParseException : System.SystemException
    {
        public XamlParseException() { }
        protected XamlParseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XamlParseException(string message) { }
        public XamlParseException(string message, System.Exception innerException) { }
        public XamlParseException(string message, int lineNumber, int linePosition) { }
        public XamlParseException(string message, int lineNumber, int linePosition, System.Exception innerException) { }
        public System.Uri BaseUri { get { throw null; } }
        public object KeyContext { get { throw null; } }
        public int LineNumber { get { throw null; } }
        public int LinePosition { get { throw null; } }
        public string NameContext { get { throw null; } }
        public string UidContext { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class XamlReader
    {
        public XamlReader() { }
        public event System.ComponentModel.AsyncCompletedEventHandler LoadCompleted { add { } remove { } }
        public void CancelAsync() { }
        public static System.Xaml.XamlSchemaContext GetWpfSchemaContext() { throw null; }
        public static object Load(System.IO.Stream stream) { throw null; }
        public static object Load(System.IO.Stream stream, bool useRestrictiveXamlReader) { throw null; }
        public static object Load(System.IO.Stream stream, System.Windows.Markup.ParserContext parserContext) { throw null; }
        public static object Load(System.IO.Stream stream, System.Windows.Markup.ParserContext parserContext, bool useRestrictiveXamlReader) { throw null; }
        public static object Load(System.Xaml.XamlReader reader) { throw null; }
        public static object Load(System.Xml.XmlReader reader) { throw null; }
        public static object Load(System.Xml.XmlReader reader, bool useRestrictiveXamlReader) { throw null; }
        public object LoadAsync(System.IO.Stream stream) { throw null; }
        public object LoadAsync(System.IO.Stream stream, bool useRestrictiveXamlReader) { throw null; }
        public object LoadAsync(System.IO.Stream stream, System.Windows.Markup.ParserContext parserContext) { throw null; }
        public object LoadAsync(System.IO.Stream stream, System.Windows.Markup.ParserContext parserContext, bool useRestrictiveXamlReader) { throw null; }
        public object LoadAsync(System.Xml.XmlReader reader) { throw null; }
        public object LoadAsync(System.Xml.XmlReader reader, bool useRestrictiveXamlReader) { throw null; }
        public static object Parse(string xamlText) { throw null; }
        public static object Parse(string xamlText, bool useRestrictiveXamlReader) { throw null; }
        public static object Parse(string xamlText, System.Windows.Markup.ParserContext parserContext) { throw null; }
        public static object Parse(string xamlText, System.Windows.Markup.ParserContext parserContext, bool useRestrictiveXamlReader) { throw null; }
    }
    public partial class XamlTypeMapper
    {
        public XamlTypeMapper(string[] assemblyNames) { }
        public XamlTypeMapper(string[] assemblyNames, System.Windows.Markup.NamespaceMapEntry[] namespaceMaps) { }
        public static System.Windows.Markup.XamlTypeMapper DefaultMapper { get { throw null; } }
        public void AddMappingProcessingInstruction(string xmlNamespace, string clrNamespace, string assemblyName) { }
        protected virtual bool AllowInternalType(System.Type type) { throw null; }
        public System.Type GetType(string xmlNamespace, string localName) { throw null; }
        public void SetAssemblyPath(string assemblyName, string assemblyPath) { }
    }
    public static partial class XamlWriter
    {
        public static string Save(object obj) { throw null; }
        public static void Save(object obj, System.IO.Stream stream) { }
        public static void Save(object obj, System.IO.TextWriter writer) { }
        public static void Save(object obj, System.Windows.Markup.XamlDesignerSerializationManager manager) { }
        public static void Save(object obj, System.Xml.XmlWriter xmlWriter) { }
    }
    public enum XamlWriterMode
    {
        Expression = 0,
        Value = 1,
    }
    public enum XamlWriterState
    {
        Starting = 0,
        Finished = 1,
    }
    public sealed partial class XmlAttributeProperties
    {
        internal XmlAttributeProperties() { }
        [System.ComponentModel.BrowsableAttribute(false)]
        public static readonly System.Windows.DependencyProperty XmlNamespaceMapsProperty;
        [System.ComponentModel.BrowsableAttribute(false)]
        public static readonly System.Windows.DependencyProperty XmlnsDefinitionProperty;
        [System.ComponentModel.BrowsableAttribute(false)]
        public static readonly System.Windows.DependencyProperty XmlnsDictionaryProperty;
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.NeverLocalize)]
        public static readonly System.Windows.DependencyProperty XmlSpaceProperty;
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static string GetXmlNamespaceMaps(System.Windows.DependencyObject dependencyObject) { throw null; }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        [System.Windows.Markup.DesignerSerializationOptionsAttribute(System.Windows.Markup.DesignerSerializationOptions.SerializeAsAttribute)]
        public static string GetXmlnsDefinition(System.Windows.DependencyObject dependencyObject) { throw null; }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        public static System.Windows.Markup.XmlnsDictionary GetXmlnsDictionary(System.Windows.DependencyObject dependencyObject) { throw null; }
        [System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(System.Windows.DependencyObject))]
        [System.Windows.Markup.DesignerSerializationOptionsAttribute(System.Windows.Markup.DesignerSerializationOptions.SerializeAsAttribute)]
        public static string GetXmlSpace(System.Windows.DependencyObject dependencyObject) { throw null; }
        public static void SetXmlNamespaceMaps(System.Windows.DependencyObject dependencyObject, string value) { }
        public static void SetXmlnsDefinition(System.Windows.DependencyObject dependencyObject, string value) { }
        public static void SetXmlnsDictionary(System.Windows.DependencyObject dependencyObject, System.Windows.Markup.XmlnsDictionary value) { }
        public static void SetXmlSpace(System.Windows.DependencyObject dependencyObject, string value) { }
    }
    public partial class XmlnsDictionary : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable, System.Xaml.IXamlNamespaceResolver
    {
        public XmlnsDictionary() { }
        public XmlnsDictionary(System.Windows.Markup.XmlnsDictionary xmlnsDictionary) { }
        public int Count { get { throw null; } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public object this[object prefix] { get { throw null; } set { } }
        public string this[string prefix] { get { throw null; } set { } }
        public System.Collections.ICollection Keys { get { throw null; } }
        public bool Sealed { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public void Add(object prefix, object xmlNamespace) { }
        public void Add(string prefix, string xmlNamespace) { }
        public void Clear() { }
        public bool Contains(object key) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Collections.DictionaryEntry[] array, int index) { }
        public string DefaultNamespace() { throw null; }
        protected System.Collections.IDictionaryEnumerator GetDictionaryEnumerator() { throw null; }
        protected System.Collections.IEnumerator GetEnumerator() { throw null; }
        public string GetNamespace(string prefix) { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xaml.NamespaceDeclaration> GetNamespacePrefixes() { throw null; }
        public string LookupNamespace(string prefix) { throw null; }
        public string LookupPrefix(string xmlNamespace) { throw null; }
        public void PopScope() { }
        public void PushScope() { }
        public void Remove(object prefix) { }
        public void Remove(string prefix) { }
        public void Seal() { }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
}
namespace System.Windows.Markup.Localizer
{
    public abstract partial class BamlLocalizabilityResolver
    {
        protected BamlLocalizabilityResolver() { }
        public abstract System.Windows.Markup.Localizer.ElementLocalizability GetElementLocalizability(string assembly, string className);
        public abstract System.Windows.LocalizabilityAttribute GetPropertyLocalizability(string assembly, string className, string property);
        public abstract string ResolveAssemblyFromClass(string className);
        public abstract string ResolveFormattingTagToClass(string formattingTag);
    }
    public partial class BamlLocalizableResource
    {
        public BamlLocalizableResource() { }
        public BamlLocalizableResource(string content, string comments, System.Windows.LocalizationCategory category, bool modifiable, bool readable) { }
        public System.Windows.LocalizationCategory Category { get { throw null; } set { } }
        public string Comments { get { throw null; } set { } }
        public string Content { get { throw null; } set { } }
        public bool Modifiable { get { throw null; } set { } }
        public bool Readable { get { throw null; } set { } }
        public override bool Equals(object other) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public partial class BamlLocalizableResourceKey
    {
        public BamlLocalizableResourceKey(string uid, string className, string propertyName) { }
        public string AssemblyName { get { throw null; } }
        public string ClassName { get { throw null; } }
        public string PropertyName { get { throw null; } }
        public string Uid { get { throw null; } }
        public override bool Equals(object other) { throw null; }
        public bool Equals(System.Windows.Markup.Localizer.BamlLocalizableResourceKey other) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public sealed partial class BamlLocalizationDictionary : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public BamlLocalizationDictionary() { }
        public int Count { get { throw null; } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public System.Windows.Markup.Localizer.BamlLocalizableResource this[System.Windows.Markup.Localizer.BamlLocalizableResourceKey key] { get { throw null; } set { } }
        public System.Collections.ICollection Keys { get { throw null; } }
        public System.Windows.Markup.Localizer.BamlLocalizableResourceKey RootElementKey { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        object System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
        public System.Collections.ICollection Values { get { throw null; } }
        public void Add(System.Windows.Markup.Localizer.BamlLocalizableResourceKey key, System.Windows.Markup.Localizer.BamlLocalizableResource value) { }
        public void Clear() { }
        public bool Contains(System.Windows.Markup.Localizer.BamlLocalizableResourceKey key) { throw null; }
        public void CopyTo(System.Collections.DictionaryEntry[] array, int arrayIndex) { }
        public System.Windows.Markup.Localizer.BamlLocalizationDictionaryEnumerator GetEnumerator() { throw null; }
        public void Remove(System.Windows.Markup.Localizer.BamlLocalizableResourceKey key) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object key, object value) { }
        bool System.Collections.IDictionary.Contains(object key) { throw null; }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class BamlLocalizationDictionaryEnumerator : System.Collections.IDictionaryEnumerator, System.Collections.IEnumerator
    {
        internal BamlLocalizationDictionaryEnumerator() { }
        public System.Collections.DictionaryEntry Current { get { throw null; } }
        public System.Collections.DictionaryEntry Entry { get { throw null; } }
        public System.Windows.Markup.Localizer.BamlLocalizableResourceKey Key { get { throw null; } }
        object System.Collections.IDictionaryEnumerator.Key { get { throw null; } }
        object System.Collections.IDictionaryEnumerator.Value { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public System.Windows.Markup.Localizer.BamlLocalizableResource Value { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public partial class BamlLocalizer
    {
        public BamlLocalizer(System.IO.Stream source) { }
        public BamlLocalizer(System.IO.Stream source, System.Windows.Markup.Localizer.BamlLocalizabilityResolver resolver) { }
        public BamlLocalizer(System.IO.Stream source, System.Windows.Markup.Localizer.BamlLocalizabilityResolver resolver, System.IO.TextReader comments) { }
        public event System.Windows.Markup.Localizer.BamlLocalizerErrorNotifyEventHandler ErrorNotify { add { } remove { } }
        public System.Windows.Markup.Localizer.BamlLocalizationDictionary ExtractResources() { throw null; }
        protected virtual void OnErrorNotify(System.Windows.Markup.Localizer.BamlLocalizerErrorNotifyEventArgs e) { }
        public void UpdateBaml(System.IO.Stream target, System.Windows.Markup.Localizer.BamlLocalizationDictionary updates) { }
    }
    public enum BamlLocalizerError
    {
        DuplicateUid = 0,
        DuplicateElement = 1,
        IncompleteElementPlaceholder = 2,
        InvalidCommentingXml = 3,
        InvalidLocalizationAttributes = 4,
        InvalidLocalizationComments = 5,
        InvalidUid = 6,
        MismatchedElements = 7,
        SubstitutionAsPlaintext = 8,
        UidMissingOnChildElement = 9,
        UnknownFormattingTag = 10,
    }
    public partial class BamlLocalizerErrorNotifyEventArgs : System.EventArgs
    {
        internal BamlLocalizerErrorNotifyEventArgs() { }
        public System.Windows.Markup.Localizer.BamlLocalizerError Error { get { throw null; } }
        public System.Windows.Markup.Localizer.BamlLocalizableResourceKey Key { get { throw null; } }
    }
    public delegate void BamlLocalizerErrorNotifyEventHandler(object sender, System.Windows.Markup.Localizer.BamlLocalizerErrorNotifyEventArgs e);
    public partial class ElementLocalizability
    {
        public ElementLocalizability() { }
        public ElementLocalizability(string formattingTag, System.Windows.LocalizabilityAttribute attribute) { }
        public System.Windows.LocalizabilityAttribute Attribute { get { throw null; } set { } }
        public string FormattingTag { get { throw null; } set { } }
    }
}
namespace System.Windows.Markup.Primitives
{
    public sealed partial class MarkupWriter : System.IDisposable
    {
        internal MarkupWriter() { }
        public void Dispose() { }
        public static System.Windows.Markup.Primitives.MarkupObject GetMarkupObjectFor(object instance) { throw null; }
        public static System.Windows.Markup.Primitives.MarkupObject GetMarkupObjectFor(object instance, System.Windows.Markup.XamlDesignerSerializationManager manager) { throw null; }
    }
}
namespace System.Windows.Media
{
    public partial class AdornerHitTestResult : System.Windows.Media.PointHitTestResult
    {
        internal AdornerHitTestResult() : base (default(System.Windows.Media.Visual), default(System.Windows.Point)) { }
        public System.Windows.Documents.Adorner Adorner { get { throw null; } }
    }
    public static partial class TextOptions
    {
        public static readonly System.Windows.DependencyProperty TextFormattingModeProperty;
        public static readonly System.Windows.DependencyProperty TextHintingModeProperty;
        public static readonly System.Windows.DependencyProperty TextRenderingModeProperty;
        public static System.Windows.Media.TextFormattingMode GetTextFormattingMode(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.Media.TextHintingMode GetTextHintingMode(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.Media.TextRenderingMode GetTextRenderingMode(System.Windows.DependencyObject element) { throw null; }
        public static void SetTextFormattingMode(System.Windows.DependencyObject element, System.Windows.Media.TextFormattingMode value) { }
        public static void SetTextHintingMode(System.Windows.DependencyObject element, System.Windows.Media.TextHintingMode value) { }
        public static void SetTextRenderingMode(System.Windows.DependencyObject element, System.Windows.Media.TextRenderingMode value) { }
    }
}
namespace System.Windows.Media.Animation
{
    [System.Windows.Markup.ContentPropertyAttribute("Storyboard")]
    [System.Windows.Markup.RuntimeNamePropertyAttribute("Name")]
    public sealed partial class BeginStoryboard : System.Windows.TriggerAction
    {
        public static readonly System.Windows.DependencyProperty StoryboardProperty;
        public BeginStoryboard() { }
        [System.ComponentModel.DefaultValueAttribute(System.Windows.Media.Animation.HandoffBehavior.SnapshotAndReplace)]
        public System.Windows.Media.Animation.HandoffBehavior HandoffBehavior { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string Name { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Windows.Media.Animation.Storyboard Storyboard { get { throw null; } set { } }
    }
    public abstract partial class ControllableStoryboardAction : System.Windows.TriggerAction
    {
        internal ControllableStoryboardAction() { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string BeginStoryboardName { get { throw null; } set { } }
    }
    public partial class DiscreteThicknessKeyFrame : System.Windows.Media.Animation.ThicknessKeyFrame
    {
        public DiscreteThicknessKeyFrame() { }
        public DiscreteThicknessKeyFrame(System.Windows.Thickness value) { }
        public DiscreteThicknessKeyFrame(System.Windows.Thickness value, System.Windows.Media.Animation.KeyTime keyTime) { }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
        protected override System.Windows.Thickness InterpolateValueCore(System.Windows.Thickness baseValue, double keyFrameProgress) { throw null; }
    }
    public partial class EasingThicknessKeyFrame : System.Windows.Media.Animation.ThicknessKeyFrame
    {
        public static readonly System.Windows.DependencyProperty EasingFunctionProperty;
        public EasingThicknessKeyFrame() { }
        public EasingThicknessKeyFrame(System.Windows.Thickness value) { }
        public EasingThicknessKeyFrame(System.Windows.Thickness value, System.Windows.Media.Animation.KeyTime keyTime) { }
        public EasingThicknessKeyFrame(System.Windows.Thickness value, System.Windows.Media.Animation.KeyTime keyTime, System.Windows.Media.Animation.IEasingFunction easingFunction) { }
        public System.Windows.Media.Animation.IEasingFunction EasingFunction { get { throw null; } set { } }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
        protected override System.Windows.Thickness InterpolateValueCore(System.Windows.Thickness baseValue, double keyFrameProgress) { throw null; }
    }
    public partial class LinearThicknessKeyFrame : System.Windows.Media.Animation.ThicknessKeyFrame
    {
        public LinearThicknessKeyFrame() { }
        public LinearThicknessKeyFrame(System.Windows.Thickness value) { }
        public LinearThicknessKeyFrame(System.Windows.Thickness value, System.Windows.Media.Animation.KeyTime keyTime) { }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
        protected override System.Windows.Thickness InterpolateValueCore(System.Windows.Thickness baseValue, double keyFrameProgress) { throw null; }
    }
    public sealed partial class PauseStoryboard : System.Windows.Media.Animation.ControllableStoryboardAction
    {
        public PauseStoryboard() { }
    }
    public sealed partial class RemoveStoryboard : System.Windows.Media.Animation.ControllableStoryboardAction
    {
        public RemoveStoryboard() { }
    }
    public sealed partial class ResumeStoryboard : System.Windows.Media.Animation.ControllableStoryboardAction
    {
        public ResumeStoryboard() { }
    }
    public sealed partial class SeekStoryboard : System.Windows.Media.Animation.ControllableStoryboardAction
    {
        public SeekStoryboard() { }
        public System.TimeSpan Offset { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Windows.Media.Animation.TimeSeekOrigin.BeginTime)]
        public System.Windows.Media.Animation.TimeSeekOrigin Origin { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeOffset() { throw null; }
    }
    public sealed partial class SetStoryboardSpeedRatio : System.Windows.Media.Animation.ControllableStoryboardAction
    {
        public SetStoryboardSpeedRatio() { }
        [System.ComponentModel.DefaultValueAttribute(1)]
        public double SpeedRatio { get { throw null; } set { } }
    }
    public sealed partial class SkipStoryboardToFill : System.Windows.Media.Animation.ControllableStoryboardAction
    {
        public SkipStoryboardToFill() { }
    }
    public partial class SplineThicknessKeyFrame : System.Windows.Media.Animation.ThicknessKeyFrame
    {
        public static readonly System.Windows.DependencyProperty KeySplineProperty;
        public SplineThicknessKeyFrame() { }
        public SplineThicknessKeyFrame(System.Windows.Thickness value) { }
        public SplineThicknessKeyFrame(System.Windows.Thickness value, System.Windows.Media.Animation.KeyTime keyTime) { }
        public SplineThicknessKeyFrame(System.Windows.Thickness value, System.Windows.Media.Animation.KeyTime keyTime, System.Windows.Media.Animation.KeySpline keySpline) { }
        public System.Windows.Media.Animation.KeySpline KeySpline { get { throw null; } set { } }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
        protected override System.Windows.Thickness InterpolateValueCore(System.Windows.Thickness baseValue, double keyFrameProgress) { throw null; }
    }
    public sealed partial class StopStoryboard : System.Windows.Media.Animation.ControllableStoryboardAction
    {
        public StopStoryboard() { }
    }
    public partial class Storyboard : System.Windows.Media.Animation.ParallelTimeline
    {
        public static readonly System.Windows.DependencyProperty TargetNameProperty;
        public static readonly System.Windows.DependencyProperty TargetProperty;
        public static readonly System.Windows.DependencyProperty TargetPropertyProperty;
        public Storyboard() { }
        public void Begin() { }
        public void Begin(System.Windows.FrameworkContentElement containingObject) { }
        public void Begin(System.Windows.FrameworkContentElement containingObject, bool isControllable) { }
        public void Begin(System.Windows.FrameworkContentElement containingObject, System.Windows.Media.Animation.HandoffBehavior handoffBehavior) { }
        public void Begin(System.Windows.FrameworkContentElement containingObject, System.Windows.Media.Animation.HandoffBehavior handoffBehavior, bool isControllable) { }
        public void Begin(System.Windows.FrameworkElement containingObject) { }
        public void Begin(System.Windows.FrameworkElement containingObject, bool isControllable) { }
        public void Begin(System.Windows.FrameworkElement containingObject, System.Windows.FrameworkTemplate frameworkTemplate) { }
        public void Begin(System.Windows.FrameworkElement containingObject, System.Windows.FrameworkTemplate frameworkTemplate, bool isControllable) { }
        public void Begin(System.Windows.FrameworkElement containingObject, System.Windows.FrameworkTemplate frameworkTemplate, System.Windows.Media.Animation.HandoffBehavior handoffBehavior) { }
        public void Begin(System.Windows.FrameworkElement containingObject, System.Windows.FrameworkTemplate frameworkTemplate, System.Windows.Media.Animation.HandoffBehavior handoffBehavior, bool isControllable) { }
        public void Begin(System.Windows.FrameworkElement containingObject, System.Windows.Media.Animation.HandoffBehavior handoffBehavior) { }
        public void Begin(System.Windows.FrameworkElement containingObject, System.Windows.Media.Animation.HandoffBehavior handoffBehavior, bool isControllable) { }
        public new System.Windows.Media.Animation.Storyboard Clone() { throw null; }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
        public double GetCurrentGlobalSpeed() { throw null; }
        public double? GetCurrentGlobalSpeed(System.Windows.FrameworkContentElement containingObject) { throw null; }
        public double? GetCurrentGlobalSpeed(System.Windows.FrameworkElement containingObject) { throw null; }
        public int GetCurrentIteration() { throw null; }
        public int? GetCurrentIteration(System.Windows.FrameworkContentElement containingObject) { throw null; }
        public int? GetCurrentIteration(System.Windows.FrameworkElement containingObject) { throw null; }
        public double GetCurrentProgress() { throw null; }
        public double? GetCurrentProgress(System.Windows.FrameworkContentElement containingObject) { throw null; }
        public double? GetCurrentProgress(System.Windows.FrameworkElement containingObject) { throw null; }
        public System.Windows.Media.Animation.ClockState GetCurrentState() { throw null; }
        public System.Windows.Media.Animation.ClockState GetCurrentState(System.Windows.FrameworkContentElement containingObject) { throw null; }
        public System.Windows.Media.Animation.ClockState GetCurrentState(System.Windows.FrameworkElement containingObject) { throw null; }
        public System.TimeSpan GetCurrentTime() { throw null; }
        public System.TimeSpan? GetCurrentTime(System.Windows.FrameworkContentElement containingObject) { throw null; }
        public System.TimeSpan? GetCurrentTime(System.Windows.FrameworkElement containingObject) { throw null; }
        public bool GetIsPaused() { throw null; }
        public bool GetIsPaused(System.Windows.FrameworkContentElement containingObject) { throw null; }
        public bool GetIsPaused(System.Windows.FrameworkElement containingObject) { throw null; }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public static System.Windows.DependencyObject GetTarget(System.Windows.DependencyObject element) { throw null; }
        public static string GetTargetName(System.Windows.DependencyObject element) { throw null; }
        public static System.Windows.PropertyPath GetTargetProperty(System.Windows.DependencyObject element) { throw null; }
        public void Pause() { }
        public void Pause(System.Windows.FrameworkContentElement containingObject) { }
        public void Pause(System.Windows.FrameworkElement containingObject) { }
        public void Remove() { }
        public void Remove(System.Windows.FrameworkContentElement containingObject) { }
        public void Remove(System.Windows.FrameworkElement containingObject) { }
        public void Resume() { }
        public void Resume(System.Windows.FrameworkContentElement containingObject) { }
        public void Resume(System.Windows.FrameworkElement containingObject) { }
        public void Seek(System.TimeSpan offset) { }
        public void Seek(System.TimeSpan offset, System.Windows.Media.Animation.TimeSeekOrigin origin) { }
        public void Seek(System.Windows.FrameworkContentElement containingObject, System.TimeSpan offset, System.Windows.Media.Animation.TimeSeekOrigin origin) { }
        public void Seek(System.Windows.FrameworkElement containingObject, System.TimeSpan offset, System.Windows.Media.Animation.TimeSeekOrigin origin) { }
        public void SeekAlignedToLastTick(System.TimeSpan offset) { }
        public void SeekAlignedToLastTick(System.TimeSpan offset, System.Windows.Media.Animation.TimeSeekOrigin origin) { }
        public void SeekAlignedToLastTick(System.Windows.FrameworkContentElement containingObject, System.TimeSpan offset, System.Windows.Media.Animation.TimeSeekOrigin origin) { }
        public void SeekAlignedToLastTick(System.Windows.FrameworkElement containingObject, System.TimeSpan offset, System.Windows.Media.Animation.TimeSeekOrigin origin) { }
        public void SetSpeedRatio(double speedRatio) { }
        public void SetSpeedRatio(System.Windows.FrameworkContentElement containingObject, double speedRatio) { }
        public void SetSpeedRatio(System.Windows.FrameworkElement containingObject, double speedRatio) { }
        public static void SetTarget(System.Windows.DependencyObject element, System.Windows.DependencyObject value) { }
        public static void SetTargetName(System.Windows.DependencyObject element, string name) { }
        public static void SetTargetProperty(System.Windows.DependencyObject element, System.Windows.PropertyPath path) { }
        public void SkipToFill() { }
        public void SkipToFill(System.Windows.FrameworkContentElement containingObject) { }
        public void SkipToFill(System.Windows.FrameworkElement containingObject) { }
        public void Stop() { }
        public void Stop(System.Windows.FrameworkContentElement containingObject) { }
        public void Stop(System.Windows.FrameworkElement containingObject) { }
    }
    public partial class ThicknessAnimation : System.Windows.Media.Animation.ThicknessAnimationBase
    {
        public static readonly System.Windows.DependencyProperty ByProperty;
        public static readonly System.Windows.DependencyProperty EasingFunctionProperty;
        public static readonly System.Windows.DependencyProperty FromProperty;
        public static readonly System.Windows.DependencyProperty ToProperty;
        public ThicknessAnimation() { }
        public ThicknessAnimation(System.Windows.Thickness toValue, System.Windows.Duration duration) { }
        public ThicknessAnimation(System.Windows.Thickness toValue, System.Windows.Duration duration, System.Windows.Media.Animation.FillBehavior fillBehavior) { }
        public ThicknessAnimation(System.Windows.Thickness fromValue, System.Windows.Thickness toValue, System.Windows.Duration duration) { }
        public ThicknessAnimation(System.Windows.Thickness fromValue, System.Windows.Thickness toValue, System.Windows.Duration duration, System.Windows.Media.Animation.FillBehavior fillBehavior) { }
        public System.Windows.Thickness? By { get { throw null; } set { } }
        public System.Windows.Media.Animation.IEasingFunction EasingFunction { get { throw null; } set { } }
        public System.Windows.Thickness? From { get { throw null; } set { } }
        public bool IsAdditive { get { throw null; } set { } }
        public bool IsCumulative { get { throw null; } set { } }
        public System.Windows.Thickness? To { get { throw null; } set { } }
        public new System.Windows.Media.Animation.ThicknessAnimation Clone() { throw null; }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
        protected override System.Windows.Thickness GetCurrentValueCore(System.Windows.Thickness defaultOriginValue, System.Windows.Thickness defaultDestinationValue, System.Windows.Media.Animation.AnimationClock animationClock) { throw null; }
    }
    public abstract partial class ThicknessAnimationBase : System.Windows.Media.Animation.AnimationTimeline
    {
        protected ThicknessAnimationBase() { }
        public sealed override System.Type TargetPropertyType { get { throw null; } }
        public new System.Windows.Media.Animation.ThicknessAnimationBase Clone() { throw null; }
        public sealed override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, System.Windows.Media.Animation.AnimationClock animationClock) { throw null; }
        public System.Windows.Thickness GetCurrentValue(System.Windows.Thickness defaultOriginValue, System.Windows.Thickness defaultDestinationValue, System.Windows.Media.Animation.AnimationClock animationClock) { throw null; }
        protected abstract System.Windows.Thickness GetCurrentValueCore(System.Windows.Thickness defaultOriginValue, System.Windows.Thickness defaultDestinationValue, System.Windows.Media.Animation.AnimationClock animationClock);
    }
    [System.Windows.Markup.ContentPropertyAttribute("KeyFrames")]
    public partial class ThicknessAnimationUsingKeyFrames : System.Windows.Media.Animation.ThicknessAnimationBase, System.Windows.Markup.IAddChild, System.Windows.Media.Animation.IKeyFrameAnimation
    {
        public ThicknessAnimationUsingKeyFrames() { }
        public bool IsAdditive { get { throw null; } set { } }
        public bool IsCumulative { get { throw null; } set { } }
        public System.Windows.Media.Animation.ThicknessKeyFrameCollection KeyFrames { get { throw null; } set { } }
        System.Collections.IList System.Windows.Media.Animation.IKeyFrameAnimation.KeyFrames { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        protected virtual void AddChild(object child) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        protected virtual void AddText(string childText) { }
        public new System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames Clone() { throw null; }
        protected override void CloneCore(System.Windows.Freezable sourceFreezable) { }
        public new System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames CloneCurrentValue() { throw null; }
        protected override void CloneCurrentValueCore(System.Windows.Freezable sourceFreezable) { }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
        protected override bool FreezeCore(bool isChecking) { throw null; }
        protected override void GetAsFrozenCore(System.Windows.Freezable source) { }
        protected override void GetCurrentValueAsFrozenCore(System.Windows.Freezable source) { }
        protected sealed override System.Windows.Thickness GetCurrentValueCore(System.Windows.Thickness defaultOriginValue, System.Windows.Thickness defaultDestinationValue, System.Windows.Media.Animation.AnimationClock animationClock) { throw null; }
        protected sealed override System.Windows.Duration GetNaturalDurationCore(System.Windows.Media.Animation.Clock clock) { throw null; }
        protected override void OnChanged() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeKeyFrames() { throw null; }
        void System.Windows.Markup.IAddChild.AddChild(object child) { }
        void System.Windows.Markup.IAddChild.AddText(string childText) { }
    }
    public abstract partial class ThicknessKeyFrame : System.Windows.Freezable, System.Windows.Media.Animation.IKeyFrame
    {
        public static readonly System.Windows.DependencyProperty KeyTimeProperty;
        public static readonly System.Windows.DependencyProperty ValueProperty;
        protected ThicknessKeyFrame() { }
        protected ThicknessKeyFrame(System.Windows.Thickness value) { }
        protected ThicknessKeyFrame(System.Windows.Thickness value, System.Windows.Media.Animation.KeyTime keyTime) { }
        public System.Windows.Media.Animation.KeyTime KeyTime { get { throw null; } set { } }
        object System.Windows.Media.Animation.IKeyFrame.Value { get { throw null; } set { } }
        public System.Windows.Thickness Value { get { throw null; } set { } }
        public System.Windows.Thickness InterpolateValue(System.Windows.Thickness baseValue, double keyFrameProgress) { throw null; }
        protected abstract System.Windows.Thickness InterpolateValueCore(System.Windows.Thickness baseValue, double keyFrameProgress);
    }
    public partial class ThicknessKeyFrameCollection : System.Windows.Freezable, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public ThicknessKeyFrameCollection() { }
        public int Count { get { throw null; } }
        public static System.Windows.Media.Animation.ThicknessKeyFrameCollection Empty { get { throw null; } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Windows.Media.Animation.ThicknessKeyFrame this[int index] { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public int Add(System.Windows.Media.Animation.ThicknessKeyFrame keyFrame) { throw null; }
        public void Clear() { }
        public new System.Windows.Media.Animation.ThicknessKeyFrameCollection Clone() { throw null; }
        protected override void CloneCore(System.Windows.Freezable sourceFreezable) { }
        protected override void CloneCurrentValueCore(System.Windows.Freezable sourceFreezable) { }
        public bool Contains(System.Windows.Media.Animation.ThicknessKeyFrame keyFrame) { throw null; }
        public void CopyTo(System.Windows.Media.Animation.ThicknessKeyFrame[] array, int index) { }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
        protected override bool FreezeCore(bool isChecking) { throw null; }
        protected override void GetAsFrozenCore(System.Windows.Freezable sourceFreezable) { }
        protected override void GetCurrentValueAsFrozenCore(System.Windows.Freezable sourceFreezable) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public int IndexOf(System.Windows.Media.Animation.ThicknessKeyFrame keyFrame) { throw null; }
        public void Insert(int index, System.Windows.Media.Animation.ThicknessKeyFrame keyFrame) { }
        public void Remove(System.Windows.Media.Animation.ThicknessKeyFrame keyFrame) { }
        public void RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        int System.Collections.IList.Add(object keyFrame) { throw null; }
        bool System.Collections.IList.Contains(object keyFrame) { throw null; }
        int System.Collections.IList.IndexOf(object keyFrame) { throw null; }
        void System.Collections.IList.Insert(int index, object keyFrame) { }
        void System.Collections.IList.Remove(object keyFrame) { }
    }
}
namespace System.Windows.Navigation
{
    public abstract partial class CustomContentState
    {
        protected CustomContentState() { }
        public virtual string JournalEntryName { get { throw null; } }
        public abstract void Replay(System.Windows.Navigation.NavigationService navigationService, System.Windows.Navigation.NavigationMode mode);
    }
    public partial class FragmentNavigationEventArgs : System.EventArgs
    {
        internal FragmentNavigationEventArgs() { }
        public string Fragment { get { throw null; } }
        public bool Handled { get { throw null; } set { } }
        public object Navigator { get { throw null; } }
    }
    public delegate void FragmentNavigationEventHandler(object sender, System.Windows.Navigation.FragmentNavigationEventArgs e);
    public partial interface IProvideCustomContentState
    {
        System.Windows.Navigation.CustomContentState GetContentState();
    }
    public partial class JournalEntry : System.Windows.DependencyObject, System.Runtime.Serialization.ISerializable
    {
        public static readonly System.Windows.DependencyProperty KeepAliveProperty;
        public static readonly System.Windows.DependencyProperty NameProperty;
        protected JournalEntry(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.Windows.Navigation.CustomContentState CustomContentState { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public System.Uri Source { get { throw null; } set { } }
        public static bool GetKeepAlive(System.Windows.DependencyObject dependencyObject) { throw null; }
        public static string GetName(System.Windows.DependencyObject dependencyObject) { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static void SetKeepAlive(System.Windows.DependencyObject dependencyObject, bool keepAlive) { }
        public static void SetName(System.Windows.DependencyObject dependencyObject, string name) { }
    }
    public sealed partial class JournalEntryListConverter : System.Windows.Data.IValueConverter
    {
        public JournalEntryListConverter() { }
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
    }
    public enum JournalEntryPosition
    {
        Back = 0,
        Current = 1,
        Forward = 2,
    }
    public sealed partial class JournalEntryUnifiedViewConverter : System.Windows.Data.IMultiValueConverter
    {
        public static readonly System.Windows.DependencyProperty JournalEntryPositionProperty;
        public JournalEntryUnifiedViewConverter() { }
        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
        public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) { throw null; }
        public static System.Windows.Navigation.JournalEntryPosition GetJournalEntryPosition(System.Windows.DependencyObject element) { throw null; }
        public static void SetJournalEntryPosition(System.Windows.DependencyObject element, System.Windows.Navigation.JournalEntryPosition position) { }
    }
    public enum JournalOwnership
    {
        Automatic = 0,
        OwnsJournal = 1,
        UsesParentJournal = 2,
    }
    public delegate void LoadCompletedEventHandler(object sender, System.Windows.Navigation.NavigationEventArgs e);
    public delegate void NavigatedEventHandler(object sender, System.Windows.Navigation.NavigationEventArgs e);
    public partial class NavigatingCancelEventArgs : System.ComponentModel.CancelEventArgs
    {
        internal NavigatingCancelEventArgs() { }
        public object Content { get { throw null; } }
        public System.Windows.Navigation.CustomContentState ContentStateToSave { get { throw null; } set { } }
        public object ExtraData { get { throw null; } }
        public bool IsNavigationInitiator { get { throw null; } }
        public System.Windows.Navigation.NavigationMode NavigationMode { get { throw null; } }
        public object Navigator { get { throw null; } }
        public System.Windows.Navigation.CustomContentState TargetContentState { get { throw null; } }
        public System.Uri Uri { get { throw null; } }
        public System.Net.WebRequest WebRequest { get { throw null; } }
    }
    public delegate void NavigatingCancelEventHandler(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e);
    public partial class NavigationEventArgs : System.EventArgs
    {
        internal NavigationEventArgs() { }
        public object Content { get { throw null; } }
        public object ExtraData { get { throw null; } }
        public bool IsNavigationInitiator { get { throw null; } }
        public object Navigator { get { throw null; } }
        public System.Uri Uri { get { throw null; } }
        public System.Net.WebResponse WebResponse { get { throw null; } }
    }
    public partial class NavigationFailedEventArgs : System.EventArgs
    {
        internal NavigationFailedEventArgs() { }
        public System.Exception Exception { get { throw null; } }
        public object ExtraData { get { throw null; } }
        public bool Handled { get { throw null; } set { } }
        public object Navigator { get { throw null; } }
        public System.Uri Uri { get { throw null; } }
        public System.Net.WebRequest WebRequest { get { throw null; } }
        public System.Net.WebResponse WebResponse { get { throw null; } }
    }
    public delegate void NavigationFailedEventHandler(object sender, System.Windows.Navigation.NavigationFailedEventArgs e);
    public enum NavigationMode : byte
    {
        New = (byte)0,
        Back = (byte)1,
        Forward = (byte)2,
        Refresh = (byte)3,
    }
    public partial class NavigationProgressEventArgs : System.EventArgs
    {
        internal NavigationProgressEventArgs() { }
        public long BytesRead { get { throw null; } }
        public long MaxBytes { get { throw null; } }
        public object Navigator { get { throw null; } }
        public System.Uri Uri { get { throw null; } }
    }
    public delegate void NavigationProgressEventHandler(object sender, System.Windows.Navigation.NavigationProgressEventArgs e);
    public sealed partial class NavigationService
    {
        internal NavigationService() { }
        public bool CanGoBack { get { throw null; } }
        public bool CanGoForward { get { throw null; } }
        public object Content { get { throw null; } set { } }
        public System.Uri CurrentSource { get { throw null; } }
        public System.Uri Source { get { throw null; } set { } }
        public event System.Windows.Navigation.FragmentNavigationEventHandler FragmentNavigation { add { } remove { } }
        public event System.Windows.Navigation.LoadCompletedEventHandler LoadCompleted { add { } remove { } }
        public event System.Windows.Navigation.NavigatedEventHandler Navigated { add { } remove { } }
        public event System.Windows.Navigation.NavigatingCancelEventHandler Navigating { add { } remove { } }
        public event System.Windows.Navigation.NavigationFailedEventHandler NavigationFailed { add { } remove { } }
        public event System.Windows.Navigation.NavigationProgressEventHandler NavigationProgress { add { } remove { } }
        public event System.Windows.Navigation.NavigationStoppedEventHandler NavigationStopped { add { } remove { } }
        public void AddBackEntry(System.Windows.Navigation.CustomContentState state) { }
        public static System.Windows.Navigation.NavigationService GetNavigationService(System.Windows.DependencyObject dependencyObject) { throw null; }
        public void GoBack() { }
        public void GoForward() { }
        public bool Navigate(object root) { throw null; }
        public bool Navigate(object root, object navigationState) { throw null; }
        public bool Navigate(System.Uri source) { throw null; }
        public bool Navigate(System.Uri source, object navigationState) { throw null; }
        public bool Navigate(System.Uri source, object navigationState, bool sandboxExternalContent) { throw null; }
        public void Refresh() { }
        public System.Windows.Navigation.JournalEntry RemoveBackEntry() { throw null; }
        public void StopLoading() { }
    }
    public delegate void NavigationStoppedEventHandler(object sender, System.Windows.Navigation.NavigationEventArgs e);
    public enum NavigationUIVisibility
    {
        Automatic = 0,
        Visible = 1,
        Hidden = 2,
    }
    [System.Windows.Markup.ContentPropertyAttribute]
    [System.Windows.TemplatePartAttribute(Name="PART_NavWinCP", Type=typeof(System.Windows.Controls.ContentPresenter))]
    public partial class NavigationWindow : System.Windows.Window, System.Windows.Markup.IUriContext
    {
        public static readonly System.Windows.DependencyProperty BackStackProperty;
        public static readonly System.Windows.DependencyProperty CanGoBackProperty;
        public static readonly System.Windows.DependencyProperty CanGoForwardProperty;
        public static readonly System.Windows.DependencyProperty ForwardStackProperty;
        public static readonly System.Windows.DependencyProperty SandboxExternalContentProperty;
        public static readonly System.Windows.DependencyProperty ShowsNavigationUIProperty;
        public static readonly System.Windows.DependencyProperty SourceProperty;
        public NavigationWindow() { }
        public System.Collections.IEnumerable BackStack { get { throw null; } }
        public bool CanGoBack { get { throw null; } }
        public bool CanGoForward { get { throw null; } }
        public System.Uri CurrentSource { get { throw null; } }
        public System.Collections.IEnumerable ForwardStack { get { throw null; } }
        public System.Windows.Navigation.NavigationService NavigationService { get { throw null; } }
        public bool SandboxExternalContent { get { throw null; } set { } }
        public bool ShowsNavigationUI { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Uri Source { get { throw null; } set { } }
        System.Uri System.Windows.Markup.IUriContext.BaseUri { get { throw null; } set { } }
        public event System.Windows.Navigation.FragmentNavigationEventHandler FragmentNavigation { add { } remove { } }
        public event System.Windows.Navigation.LoadCompletedEventHandler LoadCompleted { add { } remove { } }
        public event System.Windows.Navigation.NavigatedEventHandler Navigated { add { } remove { } }
        public event System.Windows.Navigation.NavigatingCancelEventHandler Navigating { add { } remove { } }
        public event System.Windows.Navigation.NavigationFailedEventHandler NavigationFailed { add { } remove { } }
        public event System.Windows.Navigation.NavigationProgressEventHandler NavigationProgress { add { } remove { } }
        public event System.Windows.Navigation.NavigationStoppedEventHandler NavigationStopped { add { } remove { } }
        public void AddBackEntry(System.Windows.Navigation.CustomContentState state) { }
        protected override void AddChild(object value) { }
        protected override void AddText(string text) { }
        public void GoBack() { }
        public void GoForward() { }
        public bool Navigate(object content) { throw null; }
        public bool Navigate(object content, object extraData) { throw null; }
        public bool Navigate(System.Uri source) { throw null; }
        public bool Navigate(System.Uri source, object extraData) { throw null; }
        public override void OnApplyTemplate() { }
        protected override void OnClosed(System.EventArgs args) { }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() { throw null; }
        public void Refresh() { }
        public System.Windows.Navigation.JournalEntry RemoveBackEntry() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool ShouldSerializeContent() { throw null; }
        public void StopLoading() { }
    }
    public abstract partial class PageFunctionBase : System.Windows.Controls.Page
    {
        protected PageFunctionBase() { }
        public bool RemoveFromJournal { get { throw null; } set { } }
        protected virtual void Start() { }
    }
    public partial class PageFunction<T> : System.Windows.Navigation.PageFunctionBase
    {
        public PageFunction() { }
        public event System.Windows.Navigation.ReturnEventHandler<T> Return { add { } remove { } }
        protected virtual void OnReturn(System.Windows.Navigation.ReturnEventArgs<T> e) { }
    }
    public partial class RequestNavigateEventArgs : System.Windows.RoutedEventArgs
    {
        protected RequestNavigateEventArgs() { }
        public RequestNavigateEventArgs(System.Uri uri, string target) { }
        public string Target { get { throw null; } }
        public System.Uri Uri { get { throw null; } }
        protected override void InvokeEventHandler(System.Delegate genericHandler, object genericTarget) { }
    }
    public delegate void RequestNavigateEventHandler(object sender, System.Windows.Navigation.RequestNavigateEventArgs e);
    public partial class ReturnEventArgs<T> : System.EventArgs
    {
        public ReturnEventArgs() { }
        public ReturnEventArgs(T result) { }
        public T Result { get { throw null; } set { } }
    }
    public delegate void ReturnEventHandler<T>(object sender, System.Windows.Navigation.ReturnEventArgs<T> e);
}
namespace System.Windows.Resources
{
    public sealed partial class ContentTypes
    {
        public const string XamlContentType = "applicaton/xaml+xml";
        public ContentTypes() { }
    }
    public partial class StreamResourceInfo
    {
        public StreamResourceInfo() { }
        public StreamResourceInfo(System.IO.Stream stream, string contentType) { }
        public string ContentType { get { throw null; } }
        public System.IO.Stream Stream { get { throw null; } }
    }
}
namespace System.Windows.Shapes
{
    public sealed partial class Ellipse : System.Windows.Shapes.Shape
    {
        public Ellipse() { }
        protected override System.Windows.Media.Geometry DefiningGeometry { get { throw null; } }
        public override System.Windows.Media.Transform GeometryTransform { get { throw null; } }
        public override System.Windows.Media.Geometry RenderedGeometry { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) { }
    }
    public sealed partial class Line : System.Windows.Shapes.Shape
    {
        public static readonly System.Windows.DependencyProperty X1Property;
        public static readonly System.Windows.DependencyProperty X2Property;
        public static readonly System.Windows.DependencyProperty Y1Property;
        public static readonly System.Windows.DependencyProperty Y2Property;
        public Line() { }
        protected override System.Windows.Media.Geometry DefiningGeometry { get { throw null; } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double X1 { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double X2 { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double Y1 { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double Y2 { get { throw null; } set { } }
    }
    public sealed partial class Path : System.Windows.Shapes.Shape
    {
        public static readonly System.Windows.DependencyProperty DataProperty;
        public Path() { }
        public System.Windows.Media.Geometry Data { get { throw null; } set { } }
        protected override System.Windows.Media.Geometry DefiningGeometry { get { throw null; } }
    }
    public sealed partial class Polygon : System.Windows.Shapes.Shape
    {
        public static readonly System.Windows.DependencyProperty FillRuleProperty;
        public static readonly System.Windows.DependencyProperty PointsProperty;
        public Polygon() { }
        protected override System.Windows.Media.Geometry DefiningGeometry { get { throw null; } }
        public System.Windows.Media.FillRule FillRule { get { throw null; } set { } }
        public System.Windows.Media.PointCollection Points { get { throw null; } set { } }
    }
    public sealed partial class Polyline : System.Windows.Shapes.Shape
    {
        public static readonly System.Windows.DependencyProperty FillRuleProperty;
        public static readonly System.Windows.DependencyProperty PointsProperty;
        public Polyline() { }
        protected override System.Windows.Media.Geometry DefiningGeometry { get { throw null; } }
        public System.Windows.Media.FillRule FillRule { get { throw null; } set { } }
        public System.Windows.Media.PointCollection Points { get { throw null; } set { } }
    }
    public sealed partial class Rectangle : System.Windows.Shapes.Shape
    {
        public static readonly System.Windows.DependencyProperty RadiusXProperty;
        public static readonly System.Windows.DependencyProperty RadiusYProperty;
        public Rectangle() { }
        protected override System.Windows.Media.Geometry DefiningGeometry { get { throw null; } }
        public override System.Windows.Media.Transform GeometryTransform { get { throw null; } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double RadiusX { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double RadiusY { get { throw null; } set { } }
        public override System.Windows.Media.Geometry RenderedGeometry { get { throw null; } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) { }
    }
    [System.Windows.LocalizabilityAttribute(System.Windows.LocalizationCategory.None, Readability=System.Windows.Readability.Unreadable)]
    public abstract partial class Shape : System.Windows.FrameworkElement
    {
        public static readonly System.Windows.DependencyProperty FillProperty;
        public static readonly System.Windows.DependencyProperty StretchProperty;
        public static readonly System.Windows.DependencyProperty StrokeDashArrayProperty;
        public static readonly System.Windows.DependencyProperty StrokeDashCapProperty;
        public static readonly System.Windows.DependencyProperty StrokeDashOffsetProperty;
        public static readonly System.Windows.DependencyProperty StrokeEndLineCapProperty;
        public static readonly System.Windows.DependencyProperty StrokeLineJoinProperty;
        public static readonly System.Windows.DependencyProperty StrokeMiterLimitProperty;
        public static readonly System.Windows.DependencyProperty StrokeProperty;
        public static readonly System.Windows.DependencyProperty StrokeStartLineCapProperty;
        public static readonly System.Windows.DependencyProperty StrokeThicknessProperty;
        protected Shape() { }
        protected abstract System.Windows.Media.Geometry DefiningGeometry { get; }
        public System.Windows.Media.Brush Fill { get { throw null; } set { } }
        public virtual System.Windows.Media.Transform GeometryTransform { get { throw null; } }
        public virtual System.Windows.Media.Geometry RenderedGeometry { get { throw null; } }
        public System.Windows.Media.Stretch Stretch { get { throw null; } set { } }
        public System.Windows.Media.Brush Stroke { get { throw null; } set { } }
        public System.Windows.Media.DoubleCollection StrokeDashArray { get { throw null; } set { } }
        public System.Windows.Media.PenLineCap StrokeDashCap { get { throw null; } set { } }
        public double StrokeDashOffset { get { throw null; } set { } }
        public System.Windows.Media.PenLineCap StrokeEndLineCap { get { throw null; } set { } }
        public System.Windows.Media.PenLineJoin StrokeLineJoin { get { throw null; } set { } }
        public double StrokeMiterLimit { get { throw null; } set { } }
        public System.Windows.Media.PenLineCap StrokeStartLineCap { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.LengthConverter))]
        public double StrokeThickness { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) { }
    }
}
namespace System.Windows.Shell
{
    public abstract partial class JumpItem
    {
        internal JumpItem() { }
        public string CustomCategory { get { throw null; } set { } }
    }
    public enum JumpItemRejectionReason
    {
        None = 0,
        InvalidItem = 1,
        NoRegisteredHandler = 2,
        RemovedByUser = 3,
    }
    public sealed partial class JumpItemsRejectedEventArgs : System.EventArgs
    {
        public JumpItemsRejectedEventArgs() { }
        public JumpItemsRejectedEventArgs(System.Collections.Generic.IList<System.Windows.Shell.JumpItem> rejectedItems, System.Collections.Generic.IList<System.Windows.Shell.JumpItemRejectionReason> reasons) { }
        public System.Collections.Generic.IList<System.Windows.Shell.JumpItem> RejectedItems { get { throw null; } }
        public System.Collections.Generic.IList<System.Windows.Shell.JumpItemRejectionReason> RejectionReasons { get { throw null; } }
    }
    public sealed partial class JumpItemsRemovedEventArgs : System.EventArgs
    {
        public JumpItemsRemovedEventArgs() { }
        public JumpItemsRemovedEventArgs(System.Collections.Generic.IList<System.Windows.Shell.JumpItem> removedItems) { }
        public System.Collections.Generic.IList<System.Windows.Shell.JumpItem> RemovedItems { get { throw null; } }
    }
    [System.Windows.Markup.ContentPropertyAttribute("JumpItems")]
    public sealed partial class JumpList : System.ComponentModel.ISupportInitialize
    {
        public JumpList() { }
        public JumpList(System.Collections.Generic.IEnumerable<System.Windows.Shell.JumpItem> items, bool showFrequent, bool showRecent) { }
        public System.Collections.Generic.List<System.Windows.Shell.JumpItem> JumpItems { get { throw null; } }
        public bool ShowFrequentCategory { get { throw null; } set { } }
        public bool ShowRecentCategory { get { throw null; } set { } }
        public event System.EventHandler<System.Windows.Shell.JumpItemsRejectedEventArgs> JumpItemsRejected { add { } remove { } }
        public event System.EventHandler<System.Windows.Shell.JumpItemsRemovedEventArgs> JumpItemsRemovedByUser { add { } remove { } }
        public static void AddToRecentCategory(string itemPath) { }
        public static void AddToRecentCategory(System.Windows.Shell.JumpPath jumpPath) { }
        public static void AddToRecentCategory(System.Windows.Shell.JumpTask jumpTask) { }
        public void Apply() { }
        public void BeginInit() { }
        public void EndInit() { }
        public static System.Windows.Shell.JumpList GetJumpList(System.Windows.Application application) { throw null; }
        public static void SetJumpList(System.Windows.Application application, System.Windows.Shell.JumpList value) { }
    }
    public partial class JumpPath : System.Windows.Shell.JumpItem
    {
        public JumpPath() { }
        public string Path { get { throw null; } set { } }
    }
    public partial class JumpTask : System.Windows.Shell.JumpItem
    {
        public JumpTask() { }
        public string ApplicationPath { get { throw null; } set { } }
        public string Arguments { get { throw null; } set { } }
        public string Description { get { throw null; } set { } }
        public int IconResourceIndex { get { throw null; } set { } }
        public string IconResourcePath { get { throw null; } set { } }
        public string Title { get { throw null; } set { } }
        public string WorkingDirectory { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum NonClientFrameEdges
    {
        None = 0,
        Left = 1,
        Top = 2,
        Right = 4,
        Bottom = 8,
    }
    public enum ResizeGripDirection
    {
        None = 0,
        TopLeft = 1,
        Top = 2,
        TopRight = 3,
        Right = 4,
        BottomRight = 5,
        Bottom = 6,
        BottomLeft = 7,
        Left = 8,
    }
    public sealed partial class TaskbarItemInfo : System.Windows.Freezable
    {
        public static readonly System.Windows.DependencyProperty DescriptionProperty;
        public static readonly System.Windows.DependencyProperty OverlayProperty;
        public static readonly System.Windows.DependencyProperty ProgressStateProperty;
        public static readonly System.Windows.DependencyProperty ProgressValueProperty;
        public static readonly System.Windows.DependencyProperty ThumbButtonInfosProperty;
        public static readonly System.Windows.DependencyProperty ThumbnailClipMarginProperty;
        public TaskbarItemInfo() { }
        public string Description { get { throw null; } set { } }
        public System.Windows.Media.ImageSource Overlay { get { throw null; } set { } }
        public System.Windows.Shell.TaskbarItemProgressState ProgressState { get { throw null; } set { } }
        public double ProgressValue { get { throw null; } set { } }
        public System.Windows.Shell.ThumbButtonInfoCollection ThumbButtonInfos { get { throw null; } set { } }
        public System.Windows.Thickness ThumbnailClipMargin { get { throw null; } set { } }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
    }
    public enum TaskbarItemProgressState
    {
        None = 0,
        Indeterminate = 1,
        Normal = 2,
        Error = 3,
        Paused = 4,
    }
    public sealed partial class ThumbButtonInfo : System.Windows.Freezable, System.Windows.Input.ICommandSource
    {
        public static readonly System.Windows.DependencyProperty CommandParameterProperty;
        public static readonly System.Windows.DependencyProperty CommandProperty;
        public static readonly System.Windows.DependencyProperty CommandTargetProperty;
        public static readonly System.Windows.DependencyProperty DescriptionProperty;
        public static readonly System.Windows.DependencyProperty DismissWhenClickedProperty;
        public static readonly System.Windows.DependencyProperty ImageSourceProperty;
        public static readonly System.Windows.DependencyProperty IsBackgroundVisibleProperty;
        public static readonly System.Windows.DependencyProperty IsEnabledProperty;
        public static readonly System.Windows.DependencyProperty IsInteractiveProperty;
        public static readonly System.Windows.DependencyProperty VisibilityProperty;
        public ThumbButtonInfo() { }
        public System.Windows.Input.ICommand Command { get { throw null; } set { } }
        public object CommandParameter { get { throw null; } set { } }
        public System.Windows.IInputElement CommandTarget { get { throw null; } set { } }
        public string Description { get { throw null; } set { } }
        public bool DismissWhenClicked { get { throw null; } set { } }
        public System.Windows.Media.ImageSource ImageSource { get { throw null; } set { } }
        public bool IsBackgroundVisible { get { throw null; } set { } }
        public bool IsEnabled { get { throw null; } set { } }
        public bool IsInteractive { get { throw null; } set { } }
        public System.Windows.Visibility Visibility { get { throw null; } set { } }
        public event System.EventHandler Click { add { } remove { } }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
    }
    public partial class ThumbButtonInfoCollection : System.Windows.FreezableCollection<System.Windows.Shell.ThumbButtonInfo>
    {
        public ThumbButtonInfoCollection() { }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
    }
    public partial class WindowChrome : System.Windows.Freezable
    {
        public static readonly System.Windows.DependencyProperty CaptionHeightProperty;
        public static readonly System.Windows.DependencyProperty CornerRadiusProperty;
        public static readonly System.Windows.DependencyProperty GlassFrameThicknessProperty;
        public static readonly System.Windows.DependencyProperty IsHitTestVisibleInChromeProperty;
        public static readonly System.Windows.DependencyProperty NonClientFrameEdgesProperty;
        public static readonly System.Windows.DependencyProperty ResizeBorderThicknessProperty;
        public static readonly System.Windows.DependencyProperty ResizeGripDirectionProperty;
        public static readonly System.Windows.DependencyProperty UseAeroCaptionButtonsProperty;
        public static readonly System.Windows.DependencyProperty WindowChromeProperty;
        public WindowChrome() { }
        public double CaptionHeight { get { throw null; } set { } }
        public System.Windows.CornerRadius CornerRadius { get { throw null; } set { } }
        public static System.Windows.Thickness GlassFrameCompleteThickness { get { throw null; } }
        public System.Windows.Thickness GlassFrameThickness { get { throw null; } set { } }
        public System.Windows.Shell.NonClientFrameEdges NonClientFrameEdges { get { throw null; } set { } }
        public System.Windows.Thickness ResizeBorderThickness { get { throw null; } set { } }
        public bool UseAeroCaptionButtons { get { throw null; } set { } }
        protected override System.Windows.Freezable CreateInstanceCore() { throw null; }
        public static bool GetIsHitTestVisibleInChrome(System.Windows.IInputElement inputElement) { throw null; }
        public static System.Windows.Shell.ResizeGripDirection GetResizeGripDirection(System.Windows.IInputElement inputElement) { throw null; }
        public static System.Windows.Shell.WindowChrome GetWindowChrome(System.Windows.Window window) { throw null; }
        public static void SetIsHitTestVisibleInChrome(System.Windows.IInputElement inputElement, bool hitTestVisible) { }
        public static void SetResizeGripDirection(System.Windows.IInputElement inputElement, System.Windows.Shell.ResizeGripDirection direction) { }
        public static void SetWindowChrome(System.Windows.Window window, System.Windows.Shell.WindowChrome chrome) { }
    }
}

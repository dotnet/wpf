// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Definition of MS.Internal.IO.Packaging.XamlFilter.InitElementDictionary.
//
// Note:
//  THIS FILE HAS BEEN AUTOMATICALLY GENERATED. DO NOT UPDATE MANUALLY.
//

using System.Collections;               // For Hashtable

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// The class that supports content extraction from XAML files for indexing purposes.
    /// </summary>
    internal partial class XamlFilter : IManagedFilter
    {
        // Helper function to reduce code size
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void AddPresentationDescriptor(string Key)
        {
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Key),
                new ContentDescriptor(true, false, null, null));
        }

        // Helper function to reduce code size
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void AddPresentationDescriptor(string Key, string Value)
        {
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", Key),
                new ContentDescriptor(true, false, Value, null));
        }

        private void InitElementDictionary()
        {
            // Making this initialization idempotent is useful in so far as
            // a XAML filter gets reinitialized every time a new input gets loaded.
            if (_xamlElementContentDescriptorDictionary != null)
            {
                return;
            }
            _xamlElementContentDescriptorDictionary = new Hashtable(300);
            AddPresentationDescriptor("TextBox", "Text");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Italic"),
                new ContentDescriptor(true, true, null, null));
            AddPresentationDescriptor("GridViewColumnHeader", "Content");
            AddPresentationDescriptor("Canvas");
            AddPresentationDescriptor("ListBox");
            AddPresentationDescriptor("ItemsControl");
            AddPresentationDescriptor("AdornerDecorator");
            AddPresentationDescriptor("ComponentResourceKey");
            AddPresentationDescriptor("Button", "Content");
            AddPresentationDescriptor("FrameworkRichTextComposition", "Text");
            AddPresentationDescriptor("LinkTarget");
            AddPresentationDescriptor("TextBlock", "Text");
            AddPresentationDescriptor("DataTemplateSelector");
            AddPresentationDescriptor("MediaElement");
            AddPresentationDescriptor("PrintDialogException");
            AddPresentationDescriptor("DialogResultConverter");
            AddPresentationDescriptor("ComboBoxItem", "Content");
            AddPresentationDescriptor("AttachedPropertyBrowsableForChildrenAttribute");
            AddPresentationDescriptor("RowDefinition");
            AddPresentationDescriptor("TextSearch");
            AddPresentationDescriptor("DocumentReference");
            AddPresentationDescriptor("GridViewColumn");
            AddPresentationDescriptor("ValidationError");
            AddPresentationDescriptor("PasswordBox");
            AddPresentationDescriptor("InkCanvas");
            AddPresentationDescriptor("DataTrigger");
            AddPresentationDescriptor("TemplatePartAttribute");
            AddPresentationDescriptor("BlockUIContainer");
            AddPresentationDescriptor("LengthConverter");
            AddPresentationDescriptor("TextChange");
            AddPresentationDescriptor("Decorator");
            AddPresentationDescriptor("ToolTip", "Content");
            AddPresentationDescriptor("FigureLengthConverter");
            AddPresentationDescriptor("ValidationResult");
            AddPresentationDescriptor("ContentControl", "Content");
            AddPresentationDescriptor("CornerRadiusConverter");
            AddPresentationDescriptor("JournalEntryListConverter");
            AddPresentationDescriptor("ToggleButton", "Content");
            AddPresentationDescriptor("Paragraph");
            AddPresentationDescriptor("HeaderedContentControl", "Content");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "LineBreak"),
                new ContentDescriptor(true, true, null, null));
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Window"),
                new ContentDescriptor(true, false, "Content", "Title"));
            AddPresentationDescriptor("StyleSelector");
            AddPresentationDescriptor("FixedPage");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/xps/2005/06", "Path"),
                new ContentDescriptor(false, false, null, null));
            AddPresentationDescriptor("GroupStyleSelector");
            AddPresentationDescriptor("GroupStyle");
            AddPresentationDescriptor("BorderGapMaskConverter");
            AddPresentationDescriptor("Slider");
            AddPresentationDescriptor("GroupItem", "Content");
            AddPresentationDescriptor("ResourceDictionary");
            AddPresentationDescriptor("StackPanel");
            AddPresentationDescriptor("DockPanel");
            AddPresentationDescriptor("Image");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/xps/2005/06", "Ellipse"),
                new ContentDescriptor(false, false, null, null));
            AddPresentationDescriptor("HeaderedItemsControl");
            AddPresentationDescriptor("ColumnDefinition");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/xps/2005/06", "Polygon"),
                new ContentDescriptor(false, false, null, null));
            AddPresentationDescriptor("PropertyPathConverter");
            AddPresentationDescriptor("Menu");
            AddPresentationDescriptor("Condition");
            AddPresentationDescriptor("TemplateBindingExtension");
            AddPresentationDescriptor("TextElementEditingBehaviorAttribute");
            AddPresentationDescriptor("RepeatButton", "Content");
            AddPresentationDescriptor("AdornedElementPlaceholder");
            AddPresentationDescriptor("JournalEntry");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Figure"),
                new ContentDescriptor(true, true, null, null));
            AddPresentationDescriptor("BulletDecorator");
            AddPresentationDescriptor("SpellingError");
            AddPresentationDescriptor("InkPresenter");
            AddPresentationDescriptor("DataTemplateKey");
            AddPresentationDescriptor("ItemsPanelTemplate");
            AddPresentationDescriptor("FlowDocumentPageViewer");
            AddPresentationDescriptor("GridViewRowPresenter", "Content");
            AddPresentationDescriptor("ThicknessConverter");
            AddPresentationDescriptor("FixedDocumentSequence");
            AddPresentationDescriptor("MenuScrollingVisibilityConverter");
            AddPresentationDescriptor("TemplateBindingExpressionConverter");
            AddPresentationDescriptor("GridViewHeaderRowPresenter");
            AddPresentationDescriptor("TreeViewItem");
            AddPresentationDescriptor("TemplateBindingExtensionConverter");
            AddPresentationDescriptor("MultiTrigger");
            AddPresentationDescriptor("ComboBox", "Text");
            AddPresentationDescriptor("UniformGrid");
            AddPresentationDescriptor("ListBoxItem", "Content");
            AddPresentationDescriptor("Grid");
            AddPresentationDescriptor("Trigger");
            AddPresentationDescriptor("RichTextBox");
            AddPresentationDescriptor("GroupBox", "Content");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "InlineUIContainer"),
                new ContentDescriptor(true, true, null, null));
            AddPresentationDescriptor("CheckBox", "Content");
            AddPresentationDescriptor("ToolBarPanel");
            AddPresentationDescriptor("DynamicResourceExtension");
            AddPresentationDescriptor("FontSizeConverter");
            AddPresentationDescriptor("Separator");
            AddPresentationDescriptor("Table");
            AddPresentationDescriptor("VirtualizingStackPanel");
            AddPresentationDescriptor("DocumentViewer");
            AddPresentationDescriptor("TableRow");
            AddPresentationDescriptor("RadioButton", "Content");
            AddPresentationDescriptor("StaticResourceExtension");
            AddPresentationDescriptor("TableColumn");
            AddPresentationDescriptor("Track");
            AddPresentationDescriptor("ProgressBar");
            AddPresentationDescriptor("ListViewItem", "Content");
            AddPresentationDescriptor("ZoomPercentageConverter");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Floater"),
                new ContentDescriptor(true, true, null, null));
            AddPresentationDescriptor("TabItem", "Content");
            AddPresentationDescriptor("FlowDocument");
            AddPresentationDescriptor("Label", "Content");
            AddPresentationDescriptor("WrapPanel");
            AddPresentationDescriptor("ListItem");
            AddPresentationDescriptor("FrameworkPropertyMetadata");
            AddPresentationDescriptor("NameScope");
            AddPresentationDescriptor("TreeView");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/xps/2005/06", "Rectangle"),
                new ContentDescriptor(false, false, null, null));
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Hyperlink"),
                new ContentDescriptor(true, true, null, null));
            AddPresentationDescriptor("TableRowGroup");
            AddPresentationDescriptor("Application");
            AddPresentationDescriptor("TickBar");
            AddPresentationDescriptor("ResizeGrip");
            AddPresentationDescriptor("FrameworkElement");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Run"),
                new ContentDescriptor(true, true, "Text", null));
            AddPresentationDescriptor("FrameworkContentElement");
            AddPresentationDescriptor("ItemContainerGenerator");
            AddPresentationDescriptor("ThemeDictionaryExtension");
            AddPresentationDescriptor("AccessText", "Text");
            AddPresentationDescriptor("Frame", "Content");
            AddPresentationDescriptor("LostFocusEventManager");
            AddPresentationDescriptor("EventTrigger");
            AddPresentationDescriptor("DataErrorValidationRule");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Page"),
                new ContentDescriptor(true, false, "Content", "WindowTitle"));
            AddPresentationDescriptor("GridLengthConverter");
            AddPresentationDescriptor("TextSelection", "Text");
            AddPresentationDescriptor("FixedDocument");
            AddPresentationDescriptor("HierarchicalDataTemplate");
            AddPresentationDescriptor("MessageBox");
            AddPresentationDescriptor("Style");
            AddPresentationDescriptor("ScrollContentPresenter", "Content");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Span"),
                new ContentDescriptor(true, true, null, null));
            AddPresentationDescriptor("TextPointer");
            AddPresentationDescriptor("FrameworkElementFactory", "Text");
            AddPresentationDescriptor("ExceptionValidationRule");
            AddPresentationDescriptor("DocumentPageView");
            AddPresentationDescriptor("ToolBar");
            AddPresentationDescriptor("ListView");
            AddPresentationDescriptor("StyleTypedPropertyAttribute");
            AddPresentationDescriptor("ToolBarOverflowPanel");
            AddPresentationDescriptor("BooleanToVisibilityConverter");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/xps/2005/06", "Line"),
                new ContentDescriptor(false, false, null, null));
            AddPresentationDescriptor("MenuItem");
            AddPresentationDescriptor("Section");
            AddPresentationDescriptor("DynamicResourceExtensionConverter");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Underline"),
                new ContentDescriptor(true, true, null, null));
            AddPresentationDescriptor("TemplateBindingExpression");
            AddPresentationDescriptor("Viewport3D");
            AddPresentationDescriptor("PrintDialog");
            AddPresentationDescriptor("ItemsPresenter");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/xps/2005/06", "Polyline"),
                new ContentDescriptor(false, false, null, null));
            AddPresentationDescriptor("FrameworkTextComposition", "Text");
            AddPresentationDescriptor("TextRange", "Text");
            AddPresentationDescriptor("StatusBarItem", "Content");
            AddPresentationDescriptor("FlowDocumentReader");
            AddPresentationDescriptor("TextEffectTarget");
            AddPresentationDescriptor("ColorConvertedBitmapExtension");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "NavigationWindow"),
                new ContentDescriptor(true, false, "Content", "Title"));
            AddPresentationDescriptor("AdornerLayer");
            AddPresentationDescriptor("GridView");
            AddPresentationDescriptor("CustomPopupPlacementCallback");
            AddPresentationDescriptor("MultiDataTrigger");
            AddPresentationDescriptor("NavigationService", "Content");
            AddPresentationDescriptor("PropertyPath");
            _xamlElementContentDescriptorDictionary.Add(
                new ElementTableKey("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Bold"),
                new ContentDescriptor(true, true, null, null));
            AddPresentationDescriptor("ResourceReferenceKeyNotFoundException");
            AddPresentationDescriptor("StatusBar");
            AddPresentationDescriptor("Border");
            AddPresentationDescriptor("SpellCheck");
            AddPresentationDescriptor("SoundPlayerAction");
            AddPresentationDescriptor("ContentPresenter", "Content");
            AddPresentationDescriptor("EventSetter");
            AddPresentationDescriptor("StickyNoteControl");
            AddPresentationDescriptor("UserControl", "Content");
            AddPresentationDescriptor("FlowDocumentScrollViewer");
            AddPresentationDescriptor("ThemeInfoAttribute");
            AddPresentationDescriptor("List");
            AddPresentationDescriptor("DataTemplate");
            AddPresentationDescriptor("GridSplitter");
            AddPresentationDescriptor("TableCell");
            AddPresentationDescriptor("Thumb");
            AddPresentationDescriptor("Glyphs");
            AddPresentationDescriptor("ScrollViewer", "Content");
            AddPresentationDescriptor("TabPanel");
            AddPresentationDescriptor("Setter");
            AddPresentationDescriptor("PageContent");
            AddPresentationDescriptor("TabControl");
            AddPresentationDescriptor("Typography");
            AddPresentationDescriptor("ScrollBar");
            AddPresentationDescriptor("NullableBoolConverter");
            AddPresentationDescriptor("ControlTemplate");
            AddPresentationDescriptor("ContextMenu");
            AddPresentationDescriptor("Popup");
            AddPresentationDescriptor("Control");
            AddPresentationDescriptor("ToolBarTray");
            AddPresentationDescriptor("Expander", "Content");
            AddPresentationDescriptor("JournalEntryUnifiedViewConverter");
            AddPresentationDescriptor("Viewbox");
        }
    }
}

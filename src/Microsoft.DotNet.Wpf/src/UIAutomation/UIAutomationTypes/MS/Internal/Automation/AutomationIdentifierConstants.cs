// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Set of GUIDs for Automation Idenfitiers (Property, Event, etc.)
//


using System;
using System.Security;
using MS.Internal.UIAutomationTypes.Interop;

namespace MS.Internal.Automation
{
    internal static class AutomationIdentifierConstants
    {
        internal const Properties      FirstProperty = Properties.RuntimeId;
        internal const Events          FirstEvent = Events.ToolTipOpened;
        internal const Patterns        FirstPattern = Patterns.Invoke;
        internal const TextAttributes  FirstTextAttribute = TextAttributes.AnimationStyle;
        internal const ControlTypes    FirstControlType = ControlTypes.Button;
        
        internal static Properties     LastSupportedProperty;
        internal static Events         LastSupportedEvent;
        internal static Patterns       LastSupportedPattern;
        internal static TextAttributes LastSupportedTextAttribute;
        internal static ControlTypes   LastSupportedControlType;

        static AutomationIdentifierConstants()
        {
            if (OSVersionHelper.IsOsWindows10RS2OrGreater)
            {
                LastSupportedProperty = Properties.Size;
                LastSupportedEvent = Events.Changes;
                LastSupportedPattern = Patterns.CustomNavigation;
                LastSupportedTextAttribute = TextAttributes.SayAsInterpretAs;
                LastSupportedControlType = ControlTypes.AppBar;
            }
            else if (OSVersionHelper.IsOsWindows10RS1OrGreater)
            {
                LastSupportedProperty = Properties.FullDescription;
                LastSupportedEvent = Events.Changes;
                LastSupportedPattern = Patterns.CustomNavigation;
                LastSupportedTextAttribute = TextAttributes.CaretBidiMode;
                LastSupportedControlType = ControlTypes.AppBar;
            }
            else if (OSVersionHelper.IsOsWindows10TH2OrGreater)
            {
                LastSupportedProperty = Properties.LocalizedLandmarkType;
                LastSupportedEvent = Events.TextEdit_ConversionTargetChanged;
                LastSupportedPattern = Patterns.CustomNavigation;
                LastSupportedTextAttribute = TextAttributes.CaretBidiMode;
                LastSupportedControlType = ControlTypes.AppBar;
            }
            else if (OSVersionHelper.IsOsWindows10OrGreater)
            {
                LastSupportedProperty = Properties.AnnotationObjects;
                LastSupportedEvent = Events.TextEdit_ConversionTargetChanged;
                LastSupportedPattern = Patterns.CustomNavigation;
                LastSupportedTextAttribute = TextAttributes.CaretBidiMode;
                LastSupportedControlType = ControlTypes.AppBar;
            }
            else if (OSVersionHelper.IsOsWindows8Point1OrGreater)
            {
                LastSupportedProperty = Properties.IsPeripheral;
                LastSupportedEvent = Events.TextEdit_ConversionTargetChanged;
                LastSupportedPattern = Patterns.TextEdit;
                LastSupportedTextAttribute = TextAttributes.CaretBidiMode;
                LastSupportedControlType = ControlTypes.AppBar;
            }
            else if (OSVersionHelper.IsOsWindows8OrGreater)
            {
                LastSupportedProperty = Properties.FlowsFrom;
                LastSupportedEvent = Events.DropTarget_Dropped;
                LastSupportedPattern = Patterns.DropTarget;
                LastSupportedTextAttribute = TextAttributes.CaretBidiMode;
                LastSupportedControlType = ControlTypes.SemanticZoom;
            }
            else if (OSVersionHelper.IsOsWindows7OrGreater ||
                    (OSVersionHelper.IsOsWindowsVistaOrGreater && UiaCoreTypesApi.SupportsWin7Identifiers()))
            {
                LastSupportedProperty = Properties.IsSynchronizedInputPatternAvailable;
                LastSupportedEvent = Events.InputDiscarded;
                LastSupportedPattern = Patterns.SynchronizedInput;
                LastSupportedTextAttribute = TextAttributes.UnderlineStyle;
                LastSupportedControlType = ControlTypes.Separator;
            }
            else
            {
                LastSupportedProperty = Properties.TransformCanRotate;
                LastSupportedEvent = Events.Window_WindowClosed;
                LastSupportedPattern = Patterns.ScrollItem;
                LastSupportedTextAttribute = TextAttributes.UnderlineStyle;
                LastSupportedControlType = ControlTypes.Separator;
            }
        }

        
        // Enum values were taken from UIAutomationCore's sources Schema.h
        // They are also publicly available here: https://msdn.microsoft.com/en-us/library/windows/desktop/ee671207(v=vs.85).aspx
        internal enum Properties
        {
            RuntimeId = 30000,
            BoundingRectangle,
            ProcessId,
            ControlType,
            LocalizedControlType,
            Name,
            AcceleratorKey,
            AccessKey,
            HasKeyboardFocus,
            IsKeyboardFocusable,
            IsEnabled,
            AutomationId,
            ClassName,
            HelpText,
            ClickablePoint,
            Culture,
            IsControlElement,
            IsContentElement,
            LabeledBy,
            IsPassword,
            NativeWindowHandle,
            ItemType,
            IsOffscreen,
            Orientation,
            FrameworkId,
            IsRequiredForForm,
            ItemStatus,
            IsDockPatternAvailable,
            IsExpandCollapsePatternAvailable,
            IsGridItemPatternAvailable,
            IsGridPatternAvailable,
            IsInvokePatternAvailable,
            IsMultipleViewPatternAvailable,
            IsRangeValuePatternAvailable,
            IsScrollPatternAvailable,
            IsScrollItemPatternAvailable,
            IsSelectionItemPatternAvailable,
            IsSelectionPatternAvailable,
            IsTablePatternAvailable,
            IsTableItemPatternAvailable,
            IsTextPatternAvailable,
            IsTogglePatternAvailable,
            IsTransformPatternAvailable,
            IsValuePatternAvailable,
            IsWindowPatternAvailable,
            ValueValue,
            ValueIsReadOnly,
            RangeValueValue,
            RangeValueIsReadOnly,
            RangeValueMinimum,
            RangeValueMaximum,
            RangeValueLargeChange,
            RangeValueSmallChange,
            ScrollHorizontalScrollPercent,
            ScrollHorizontalViewSize,
            ScrollVerticalScrollPercent,
            ScrollVerticalViewSize,
            ScrollHorizontallyScrollable,
            ScrollVerticallyScrollable,
            SelectionSelection,
            SelectionCanSelectMultiple,
            SelectionIsSelectionRequired,
            GridRowCount,
            GridColumnCount,
            GridItemRow,
            GridItemColumn,
            GridItemRowSpan,
            GridItemColumnSpan,
            GridItemContainingGrid,
            DockDockPosition,
            ExpandCollapseExpandCollapseState,
            MultipleViewCurrentView,
            MultipleViewSupportedViews,
            WindowCanMaximize,
            WindowCanMinimize,
            WindowWindowVisualState,
            WindowWindowInteractionState,
            WindowIsModal,
            WindowIsTopmost,
            SelectionItemIsSelected,
            SelectionItemSelectionContainer,
            TableRowHeaders,
            TableColumnHeaders,
            TableRowOrColumnMajor,
            TableItemRowHeaderItems,
            TableItemColumnHeaderItems,
            ToggleToggleState,
            TransformCanMove,
            TransformCanResize,
            TransformCanRotate,
            IsLegacyIAccessiblePatternAvailable,
            LegacyIAccessibleChildId,
            LegacyIAccessibleName,
            LegacyIAccessibleValue,
            LegacyIAccessibleDescription,
            LegacyIAccessibleRole,
            LegacyIAccessibleState,
            LegacyIAccessibleHelp,
            LegacyIAccessibleKeyboardShortcut,
            LegacyIAccessibleSelection,
            LegacyIAccessibleDefaultAction,
            AriaRole,
            AriaProperties,
            IsDataValidForForm,
            ControllerFor,
            DescribedBy,
            FlowsTo,
            ProviderDescription,
            IsItemContainerPatternAvailable,
            IsVirtualizedItemPatternAvailable,
            IsSynchronizedInputPatternAvailable,
            OptimizeForVisualContent,
            IsObjectModelPatternAvailable,
            AnnotationAnnotationTypeId,
            AnnotationAnnotationTypeName,
            AnnotationAuthor,
            AnnotationDateTime,
            AnnotationTarget,
            IsAnnotationPatternAvailable,
            IsTextPattern2Available,
            StylesStyleId,
            StylesStyleName,
            StylesFillColor,
            StylesFillPatternStyle,
            StylesShape,
            StylesFillPatternColor,
            StylesExtendedProperties,
            IsStylesPatternAvailable,
            IsSpreadsheetPatternAvailable,
            SpreadsheetItemFormula,
            SpreadsheetItemAnnotationObjects,
            SpreadsheetItemAnnotationTypes,
            IsSpreadsheetItemPatternAvailable,
            Transform2CanZoom,
            IsTransformPattern2Available,
            LiveSetting,
            IsTextChildPatternAvailable,
            IsDragPatternAvailable,
            DragIsGrabbed,
            DragDropEffect,
            DragDropEffects,
            IsDropTargetPatternAvailable,
            DropTargetDropTargetEffect,
            DropTargetDropTargetEffects,
            DragGrabbedItems,
            Transform2ZoomLevel,
            Transform2ZoomMinimum,
            Transform2ZoomMaximum,
            FlowsFrom,
            IsTextEditPatternAvailable,
            IsPeripheral,
            IsCustomNavigationPatternAvailable,
            PositionInSet,
            SizeOfSet,
            Level,
            AnnotationTypes,
            AnnotationObjects,
            LandmarkType,
            LocalizedLandmarkType,
            FullDescription,
            FillColor,
            OutlineColor,
            FillType,
            VisualEffects,
            OutlineThickness,
            CenterPoint,
            Rotatation,
            Size
        };
        
        internal enum Events
        {
            ToolTipOpened = 20000,
            ToolTipClosed,
            StructureChanged,
            MenuOpened,
            AutomationPropertyChanged,
            AutomationFocusChanged,
            AsyncContentLoaded,
            MenuClosed,
            LayoutInvalidated,
            Invoke_Invoked,
            SelectionItem_ElementAddedToSelection,
            SelectionItem_ElementRemovedFromSelection,
            SelectionItem_ElementSelected,
            Selection_Invalidated,
            Text_TextSelectionChanged,
            Text_TextChanged,
            Window_WindowOpened,
            Window_WindowClosed,
            MenuModeStart,
            MenuModeEnd,
            InputReachedTarget,
            InputReachedOtherElement,
            InputDiscarded,
            SystemAlert,
            LiveRegionChanged,
            HostedFragmentRootsInvalidated,
            Drag_DragStart,
            Drag_DragCancel,
            Drag_DragComplete,
            DropTarget_DragEnter,
            DropTarget_DragLeave,
            DropTarget_Dropped,
            TextEdit_TextChanged,
            TextEdit_ConversionTargetChanged,
            Changes
        };

 
        
        internal enum Patterns
        {
            Invoke = 10000,
            Selection,
            Value,
            RangeValue,
            Scroll,
            ExpandCollapse,
            Grid,
            GridItem,
            MultipleView,
            Window,
            SelectionItem,
            Dock,
            Table,
            TableItem,
            Text,
            Toggle,
            Transform,
            ScrollItem,
            LegacyIAccessible,
            ItemContainer,
            VirtualizedItem,
            SynchronizedInput,
            ObjectModel,
            Annotation,
            Text2,
            Styles,
            Spreadsheet,
            SpreadsheetItem,
            Transform2,
            TextChild,
            Drag,
            DropTarget,
            TextEdit,
            CustomNavigation
        };

        
        internal enum TextAttributes
        {
            AnimationStyle = 40000,
            BackgroundColor,
            BulletStyle,
            CapStyle,
            Culture,
            FontName,
            FontSize,
            FontWeight,
            ForegroundColor,
            HorizontalTextAlignment,
            IndentationFirstLine,
            IndentationLeading,
            IndentationTrailing,
            IsHidden,
            IsItalic,
            IsReadOnly,
            IsSubscript,
            IsSuperscript,
            MarginBottom,
            MarginLeading,
            MarginTop,
            MarginTrailing,
            OutlineStyles,
            OverlineColor,
            OverlineStyle,
            StrikethroughColor,
            StrikethroughStyle,
            Tabs,
            TextFlowDirections,
            UnderlineColor,
            UnderlineStyle,
            AnnotationTypes,
            AnnotationObjects,
            StyleName,
            StyleId,
            Link,
            IsActive,
            SelectionActiveEnd,
            CaretPosition,
            CaretBidiMode,
            LineSpacing,
            BeforeParagraphSpacing,
            AfterParagraphSpacing,
            SayAsInterpretAs,
        };

        
        internal enum ControlTypes
        {
            Button = 50000,
            Calendar,
            CheckBox,
            ComboBox,
            Edit,
            Hyperlink,
            Image,
            ListItem,
            List,
            Menu,
            MenuBar,
            MenuItem,
            ProgressBar,
            RadioButton,
            ScrollBar,
            Slider,
            Spinner,
            StatusBar,
            Tab,
            TabItem,
            Text,
            ToolBar,
            ToolTip,
            Tree,
            TreeItem,
            Custom,
            Group,
            Thumb,
            DataGrid,
            DataItem,
            Document,
            SplitButton,
            Window,
            Pane,
            Header,
            HeaderItem,
            Table,
            TitleBar,
            Separator,
            SemanticZoom,
            AppBar
        };
        
    }

}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Definition of known types in PresentationFramework.dll and
//      PresentationCore.dll and WindowsBase.dll
//
//  THIS FILE HAS BEEN AUTOMATICALLY GENERATED.
//    (See generator code in wcp\tools\KnownTypes\KnownTypesInitializer.cs)
//
//  If you are REMOVING or RENAMING an EXISTING TYPE, then a build error has sent
//  you here because this file no longer compiles.
//  The MINIMAL REQUIRED steps are:
//       you have renamed or removed in Framework, Core or Base
//    2) Update BamlWriterVersion in BamlVersionHeader.cs, incrementing the second number
//    3) Build the WCP compiler.   To do that; (on FRE) build from wcp\build.
//       When it tells you the WCP compiler has changed, install it in the root
//       To do that: cd \nt\tools and run UpdateWcpCompiler.
//       (Don't forget to check in this compiler updates with your WCP changes)
//    4) Build from wcp.  (FRE or CHK) Make certain that wcp builds cleanly.
//    5) Don't forget to check in compiler updates in the root Tools directory
//    Note: There is no need to regenerate from the tool in this case.
//
//  IF you are ADDING NEW TYPES, or you want the new name of a RENAMED TYPE to showup:
//  The OPTIONAL or ADDITIONAL steps (to use perf optimization for your type) are:
//    1) Build the dll(s) that define the new types
//    2) Update BamlWriterVersion in BamlVersionHeader.cs, incrementing the second number
//    3) (On FRE build) Run wcp\tools\buildscripts\UpdateKnownTypes.cmd
//       Or more directly you can run: KnownTypesInitializer.exe
//      Note: You may see other new types that have been have added since the last time
//            this file was generated.
//    4) Build the WCP compiler.   To do that; (on FRE) build from wcp\build.
//       When it tells you the WCP compiler has changed, install it in the root
//       To do that: cd \nt\tools and run UpdateWcpCompiler.
//       (Don't forget to check in this compiler updates with your WCP changes)
//    5) (FRE or CHK) Rebuild everything under wcp to pick up the KnownTypes changes
//    6) Don't forget to check in compiler updates in the root Tools directory
//
//   This file is shared by PresentationFramework.dll and PresentaionBuildTasks.dll.
//
//   The code marked with #if PBTCOMPILER is compiled into PresentationBuildTasks.dll.
//   The code marked with #if !PBTCOMPILER is compiled into PresenationFramework.dll
//   The code without #if flag will be compiled into both dlls.
//

using System;
using System.Collections;
using System.ComponentModel; // TypeConverters
using System.Diagnostics;
using System.Globalization;  // CultureInfo KnownType
using System.Reflection;
using MS.Utility;

// Disabling 1634 and 1691:
// In order to avoid generating warnings about unknown message numbers and
// unknown pragmas when compiling C# source code with the C# compiler,
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    // This enum specifies the TypeIds we use for know types in BAML
    // The baml files contains the negative of these values
    internal enum KnownElements : short
    {
        UnknownElement = 0,
        AccessText,
        AdornedElementPlaceholder,
        Adorner,
        AdornerDecorator,
        AdornerLayer,
        AffineTransform3D,
        AmbientLight,
        AnchoredBlock,
        Animatable,
        AnimationClock,
        AnimationTimeline,
        Application,
        ArcSegment,
        ArrayExtension,
        AxisAngleRotation3D,
        BaseIListConverter,
        BeginStoryboard,
        BevelBitmapEffect,
        BezierSegment,
        Binding,
        BindingBase,
        BindingExpression,
        BindingExpressionBase,
        BindingListCollectionView,
        BitmapDecoder,
        BitmapEffect,
        BitmapEffectCollection,
        BitmapEffectGroup,
        BitmapEffectInput,
        BitmapEncoder,
        BitmapFrame,
        BitmapImage,
        BitmapMetadata,
        BitmapPalette,
        BitmapSource,
        Block,
        BlockUIContainer,
        BlurBitmapEffect,
        BmpBitmapDecoder,
        BmpBitmapEncoder,
        Bold,
        BoolIListConverter,
        Boolean,
        BooleanAnimationBase,
        BooleanAnimationUsingKeyFrames,
        BooleanConverter,
        BooleanKeyFrame,
        BooleanKeyFrameCollection,
        BooleanToVisibilityConverter,
        Border,
        BorderGapMaskConverter,
        Brush,
        BrushConverter,
        BulletDecorator,
        Button,
        ButtonBase,
        Byte,
        ByteAnimation,
        ByteAnimationBase,
        ByteAnimationUsingKeyFrames,
        ByteConverter,
        ByteKeyFrame,
        ByteKeyFrameCollection,
        CachedBitmap,
        Camera,
        Canvas,
        Char,
        CharAnimationBase,
        CharAnimationUsingKeyFrames,
        CharConverter,
        CharIListConverter,
        CharKeyFrame,
        CharKeyFrameCollection,
        CheckBox,
        Clock,
        ClockController,
        ClockGroup,
        CollectionContainer,
        CollectionView,
        CollectionViewSource,
        Color,
        ColorAnimation,
        ColorAnimationBase,
        ColorAnimationUsingKeyFrames,
        ColorConvertedBitmap,
        ColorConvertedBitmapExtension,
        ColorConverter,
        ColorKeyFrame,
        ColorKeyFrameCollection,
        ColumnDefinition,
        CombinedGeometry,
        ComboBox,
        ComboBoxItem,
        CommandConverter,
        ComponentResourceKey,
        ComponentResourceKeyConverter,
        CompositionTarget,
        Condition,
        ContainerVisual,
        ContentControl,
        ContentElement,
        ContentPresenter,
        ContentPropertyAttribute,
        ContentWrapperAttribute,
        ContextMenu,
        ContextMenuService,
        Control,
        ControlTemplate,
        ControllableStoryboardAction,
        CornerRadius,
        CornerRadiusConverter,
        CroppedBitmap,
        CultureInfo,
        CultureInfoConverter,
        CultureInfoIetfLanguageTagConverter,
        Cursor,
        CursorConverter,
        DashStyle,
        DataChangedEventManager,
        DataTemplate,
        DataTemplateKey,
        DataTrigger,
        DateTime,
        DateTimeConverter,
        DateTimeConverter2,
        Decimal,
        DecimalAnimation,
        DecimalAnimationBase,
        DecimalAnimationUsingKeyFrames,
        DecimalConverter,
        DecimalKeyFrame,
        DecimalKeyFrameCollection,
        Decorator,
        DefinitionBase,
        DependencyObject,
        DependencyProperty,
        DependencyPropertyConverter,
        DialogResultConverter,
        DiffuseMaterial,
        DirectionalLight,
        DiscreteBooleanKeyFrame,
        DiscreteByteKeyFrame,
        DiscreteCharKeyFrame,
        DiscreteColorKeyFrame,
        DiscreteDecimalKeyFrame,
        DiscreteDoubleKeyFrame,
        DiscreteInt16KeyFrame,
        DiscreteInt32KeyFrame,
        DiscreteInt64KeyFrame,
        DiscreteMatrixKeyFrame,
        DiscreteObjectKeyFrame,
        DiscretePoint3DKeyFrame,
        DiscretePointKeyFrame,
        DiscreteQuaternionKeyFrame,
        DiscreteRectKeyFrame,
        DiscreteRotation3DKeyFrame,
        DiscreteSingleKeyFrame,
        DiscreteSizeKeyFrame,
        DiscreteStringKeyFrame,
        DiscreteThicknessKeyFrame,
        DiscreteVector3DKeyFrame,
        DiscreteVectorKeyFrame,
        DockPanel,
        DocumentPageView,
        DocumentReference,
        DocumentViewer,
        DocumentViewerBase,
        Double,
        DoubleAnimation,
        DoubleAnimationBase,
        DoubleAnimationUsingKeyFrames,
        DoubleAnimationUsingPath,
        DoubleCollection,
        DoubleCollectionConverter,
        DoubleConverter,
        DoubleIListConverter,
        DoubleKeyFrame,
        DoubleKeyFrameCollection,
        Drawing,
        DrawingBrush,
        DrawingCollection,
        DrawingContext,
        DrawingGroup,
        DrawingImage,
        DrawingVisual,
        DropShadowBitmapEffect,
        Duration,
        DurationConverter,
        DynamicResourceExtension,
        DynamicResourceExtensionConverter,
        Ellipse,
        EllipseGeometry,
        EmbossBitmapEffect,
        EmissiveMaterial,
        EnumConverter,
        EventManager,
        EventSetter,
        EventTrigger,
        Expander,
        Expression,
        ExpressionConverter,
        Figure,
        FigureLength,
        FigureLengthConverter,
        FixedDocument,
        FixedDocumentSequence,
        FixedPage,
        Floater,
        FlowDocument,
        FlowDocumentPageViewer,
        FlowDocumentReader,
        FlowDocumentScrollViewer,
        FocusManager,
        FontFamily,
        FontFamilyConverter,
        FontSizeConverter,
        FontStretch,
        FontStretchConverter,
        FontStyle,
        FontStyleConverter,
        FontWeight,
        FontWeightConverter,
        FormatConvertedBitmap,
        Frame,
        FrameworkContentElement,
        FrameworkElement,
        FrameworkElementFactory,
        FrameworkPropertyMetadata,
        FrameworkPropertyMetadataOptions,
        FrameworkRichTextComposition,
        FrameworkTemplate,
        FrameworkTextComposition,
        Freezable,
        GeneralTransform,
        GeneralTransformCollection,
        GeneralTransformGroup,
        Geometry,
        Geometry3D,
        GeometryCollection,
        GeometryConverter,
        GeometryDrawing,
        GeometryGroup,
        GeometryModel3D,
        GestureRecognizer,
        GifBitmapDecoder,
        GifBitmapEncoder,
        GlyphRun,
        GlyphRunDrawing,
        GlyphTypeface,
        Glyphs,
        GradientBrush,
        GradientStop,
        GradientStopCollection,
        Grid,
        GridLength,
        GridLengthConverter,
        GridSplitter,
        GridView,
        GridViewColumn,
        GridViewColumnHeader,
        GridViewHeaderRowPresenter,
        GridViewRowPresenter,
        GridViewRowPresenterBase,
        GroupBox,
        GroupItem,
        Guid,
        GuidConverter,
        GuidelineSet,
        HeaderedContentControl,
        HeaderedItemsControl,
        HierarchicalDataTemplate,
        HostVisual,
        Hyperlink,
        IAddChild,
        IAddChildInternal,
        ICommand,
        IComponentConnector,
        INameScope,
        IStyleConnector,
        IconBitmapDecoder,
        Image,
        ImageBrush,
        ImageDrawing,
        ImageMetadata,
        ImageSource,
        ImageSourceConverter,
        InPlaceBitmapMetadataWriter,
        InkCanvas,
        InkPresenter,
        Inline,
        InlineCollection,
        InlineUIContainer,
        InputBinding,
        InputDevice,
        InputLanguageManager,
        InputManager,
        InputMethod,
        InputScope,
        InputScopeConverter,
        InputScopeName,
        InputScopeNameConverter,
        Int16,
        Int16Animation,
        Int16AnimationBase,
        Int16AnimationUsingKeyFrames,
        Int16Converter,
        Int16KeyFrame,
        Int16KeyFrameCollection,
        Int32,
        Int32Animation,
        Int32AnimationBase,
        Int32AnimationUsingKeyFrames,
        Int32Collection,
        Int32CollectionConverter,
        Int32Converter,
        Int32KeyFrame,
        Int32KeyFrameCollection,
        Int32Rect,
        Int32RectConverter,
        Int64,
        Int64Animation,
        Int64AnimationBase,
        Int64AnimationUsingKeyFrames,
        Int64Converter,
        Int64KeyFrame,
        Int64KeyFrameCollection,
        Italic,
        ItemCollection,
        ItemsControl,
        ItemsPanelTemplate,
        ItemsPresenter,
        JournalEntry,
        JournalEntryListConverter,
        JournalEntryUnifiedViewConverter,
        JpegBitmapDecoder,
        JpegBitmapEncoder,
        KeyBinding,
        KeyConverter,
        KeyGesture,
        KeyGestureConverter,
        KeySpline,
        KeySplineConverter,
        KeyTime,
        KeyTimeConverter,
        KeyboardDevice,
        Label,
        LateBoundBitmapDecoder,
        LengthConverter,
        Light,
        Line,
        LineBreak,
        LineGeometry,
        LineSegment,
        LinearByteKeyFrame,
        LinearColorKeyFrame,
        LinearDecimalKeyFrame,
        LinearDoubleKeyFrame,
        LinearGradientBrush,
        LinearInt16KeyFrame,
        LinearInt32KeyFrame,
        LinearInt64KeyFrame,
        LinearPoint3DKeyFrame,
        LinearPointKeyFrame,
        LinearQuaternionKeyFrame,
        LinearRectKeyFrame,
        LinearRotation3DKeyFrame,
        LinearSingleKeyFrame,
        LinearSizeKeyFrame,
        LinearThicknessKeyFrame,
        LinearVector3DKeyFrame,
        LinearVectorKeyFrame,
        List,
        ListBox,
        ListBoxItem,
        ListCollectionView,
        ListItem,
        ListView,
        ListViewItem,
        Localization,
        LostFocusEventManager,
        MarkupExtension,
        Material,
        MaterialCollection,
        MaterialGroup,
        Matrix,
        Matrix3D,
        Matrix3DConverter,
        MatrixAnimationBase,
        MatrixAnimationUsingKeyFrames,
        MatrixAnimationUsingPath,
        MatrixCamera,
        MatrixConverter,
        MatrixKeyFrame,
        MatrixKeyFrameCollection,
        MatrixTransform,
        MatrixTransform3D,
        MediaClock,
        MediaElement,
        MediaPlayer,
        MediaTimeline,
        Menu,
        MenuBase,
        MenuItem,
        MenuScrollingVisibilityConverter,
        MeshGeometry3D,
        Model3D,
        Model3DCollection,
        Model3DGroup,
        ModelVisual3D,
        ModifierKeysConverter,
        MouseActionConverter,
        MouseBinding,
        MouseDevice,
        MouseGesture,
        MouseGestureConverter,
        MultiBinding,
        MultiBindingExpression,
        MultiDataTrigger,
        MultiTrigger,
        NameScope,
        NavigationWindow,
        NullExtension,
        NullableBoolConverter,
        NullableConverter,
        NumberSubstitution,
        Object,
        ObjectAnimationBase,
        ObjectAnimationUsingKeyFrames,
        ObjectDataProvider,
        ObjectKeyFrame,
        ObjectKeyFrameCollection,
        OrthographicCamera,
        OuterGlowBitmapEffect,
        Page,
        PageContent,
        PageFunctionBase,
        Panel,
        Paragraph,
        ParallelTimeline,
        ParserContext,
        PasswordBox,
        Path,
        PathFigure,
        PathFigureCollection,
        PathFigureCollectionConverter,
        PathGeometry,
        PathSegment,
        PathSegmentCollection,
        PauseStoryboard,
        Pen,
        PerspectiveCamera,
        PixelFormat,
        PixelFormatConverter,
        PngBitmapDecoder,
        PngBitmapEncoder,
        Point,
        Point3D,
        Point3DAnimation,
        Point3DAnimationBase,
        Point3DAnimationUsingKeyFrames,
        Point3DCollection,
        Point3DCollectionConverter,
        Point3DConverter,
        Point3DKeyFrame,
        Point3DKeyFrameCollection,
        Point4D,
        Point4DConverter,
        PointAnimation,
        PointAnimationBase,
        PointAnimationUsingKeyFrames,
        PointAnimationUsingPath,
        PointCollection,
        PointCollectionConverter,
        PointConverter,
        PointIListConverter,
        PointKeyFrame,
        PointKeyFrameCollection,
        PointLight,
        PointLightBase,
        PolyBezierSegment,
        PolyLineSegment,
        PolyQuadraticBezierSegment,
        Polygon,
        Polyline,
        Popup,
        PresentationSource,
        PriorityBinding,
        PriorityBindingExpression,
        ProgressBar,
        ProjectionCamera,
        PropertyPath,
        PropertyPathConverter,
        QuadraticBezierSegment,
        Quaternion,
        QuaternionAnimation,
        QuaternionAnimationBase,
        QuaternionAnimationUsingKeyFrames,
        QuaternionConverter,
        QuaternionKeyFrame,
        QuaternionKeyFrameCollection,
        QuaternionRotation3D,
        RadialGradientBrush,
        RadioButton,
        RangeBase,
        Rect,
        Rect3D,
        Rect3DConverter,
        RectAnimation,
        RectAnimationBase,
        RectAnimationUsingKeyFrames,
        RectConverter,
        RectKeyFrame,
        RectKeyFrameCollection,
        Rectangle,
        RectangleGeometry,
        RelativeSource,
        RemoveStoryboard,
        RenderOptions,
        RenderTargetBitmap,
        RepeatBehavior,
        RepeatBehaviorConverter,
        RepeatButton,
        ResizeGrip,
        ResourceDictionary,
        ResourceKey,
        ResumeStoryboard,
        RichTextBox,
        RotateTransform,
        RotateTransform3D,
        Rotation3D,
        Rotation3DAnimation,
        Rotation3DAnimationBase,
        Rotation3DAnimationUsingKeyFrames,
        Rotation3DKeyFrame,
        Rotation3DKeyFrameCollection,
        RoutedCommand,
        RoutedEvent,
        RoutedEventConverter,
        RoutedUICommand,
        RoutingStrategy,
        RowDefinition,
        Run,
        RuntimeNamePropertyAttribute,
        SByte,
        SByteConverter,
        ScaleTransform,
        ScaleTransform3D,
        ScrollBar,
        ScrollContentPresenter,
        ScrollViewer,
        Section,
        SeekStoryboard,
        Selector,
        Separator,
        SetStoryboardSpeedRatio,
        Setter,
        SetterBase,
        Shape,
        Single,
        SingleAnimation,
        SingleAnimationBase,
        SingleAnimationUsingKeyFrames,
        SingleConverter,
        SingleKeyFrame,
        SingleKeyFrameCollection,
        Size,
        Size3D,
        Size3DConverter,
        SizeAnimation,
        SizeAnimationBase,
        SizeAnimationUsingKeyFrames,
        SizeConverter,
        SizeKeyFrame,
        SizeKeyFrameCollection,
        SkewTransform,
        SkipStoryboardToFill,
        Slider,
        SolidColorBrush,
        SoundPlayerAction,
        Span,
        SpecularMaterial,
        SpellCheck,
        SplineByteKeyFrame,
        SplineColorKeyFrame,
        SplineDecimalKeyFrame,
        SplineDoubleKeyFrame,
        SplineInt16KeyFrame,
        SplineInt32KeyFrame,
        SplineInt64KeyFrame,
        SplinePoint3DKeyFrame,
        SplinePointKeyFrame,
        SplineQuaternionKeyFrame,
        SplineRectKeyFrame,
        SplineRotation3DKeyFrame,
        SplineSingleKeyFrame,
        SplineSizeKeyFrame,
        SplineThicknessKeyFrame,
        SplineVector3DKeyFrame,
        SplineVectorKeyFrame,
        SpotLight,
        StackPanel,
        StaticExtension,
        StaticResourceExtension,
        StatusBar,
        StatusBarItem,
        StickyNoteControl,
        StopStoryboard,
        Storyboard,
        StreamGeometry,
        StreamGeometryContext,
        StreamResourceInfo,
        String,
        StringAnimationBase,
        StringAnimationUsingKeyFrames,
        StringConverter,
        StringKeyFrame,
        StringKeyFrameCollection,
        StrokeCollection,
        StrokeCollectionConverter,
        Style,
        Stylus,
        StylusDevice,
        TabControl,
        TabItem,
        TabPanel,
        Table,
        TableCell,
        TableColumn,
        TableRow,
        TableRowGroup,
        TabletDevice,
        TemplateBindingExpression,
        TemplateBindingExpressionConverter,
        TemplateBindingExtension,
        TemplateBindingExtensionConverter,
        TemplateKey,
        TemplateKeyConverter,
        TextBlock,
        TextBox,
        TextBoxBase,
        TextComposition,
        TextCompositionManager,
        TextDecoration,
        TextDecorationCollection,
        TextDecorationCollectionConverter,
        TextEffect,
        TextEffectCollection,
        TextElement,
        TextSearch,
        ThemeDictionaryExtension,
        Thickness,
        ThicknessAnimation,
        ThicknessAnimationBase,
        ThicknessAnimationUsingKeyFrames,
        ThicknessConverter,
        ThicknessKeyFrame,
        ThicknessKeyFrameCollection,
        Thumb,
        TickBar,
        TiffBitmapDecoder,
        TiffBitmapEncoder,
        TileBrush,
        TimeSpan,
        TimeSpanConverter,
        Timeline,
        TimelineCollection,
        TimelineGroup,
        ToggleButton,
        ToolBar,
        ToolBarOverflowPanel,
        ToolBarPanel,
        ToolBarTray,
        ToolTip,
        ToolTipService,
        Track,
        Transform,
        Transform3D,
        Transform3DCollection,
        Transform3DGroup,
        TransformCollection,
        TransformConverter,
        TransformGroup,
        TransformedBitmap,
        TranslateTransform,
        TranslateTransform3D,
        TreeView,
        TreeViewItem,
        Trigger,
        TriggerAction,
        TriggerBase,
        TypeExtension,
        TypeTypeConverter,
        Typography,
        UIElement,
        UInt16,
        UInt16Converter,
        UInt32,
        UInt32Converter,
        UInt64,
        UInt64Converter,
        UShortIListConverter,
        Underline,
        UniformGrid,
        Uri,
        UriTypeConverter,
        UserControl,
        Validation,
        Vector,
        Vector3D,
        Vector3DAnimation,
        Vector3DAnimationBase,
        Vector3DAnimationUsingKeyFrames,
        Vector3DCollection,
        Vector3DCollectionConverter,
        Vector3DConverter,
        Vector3DKeyFrame,
        Vector3DKeyFrameCollection,
        VectorAnimation,
        VectorAnimationBase,
        VectorAnimationUsingKeyFrames,
        VectorCollection,
        VectorCollectionConverter,
        VectorConverter,
        VectorKeyFrame,
        VectorKeyFrameCollection,
        VideoDrawing,
        ViewBase,
        Viewbox,
        Viewport3D,
        Viewport3DVisual,
        VirtualizingPanel,
        VirtualizingStackPanel,
        Visual,
        Visual3D,
        VisualBrush,
        VisualTarget,
        WeakEventManager,
        WhitespaceSignificantCollectionAttribute,
        Window,
        WmpBitmapDecoder,
        WmpBitmapEncoder,
        WrapPanel,
        WriteableBitmap,
        XamlBrushSerializer,
        XamlInt32CollectionSerializer,
        XamlPathDataSerializer,
        XamlPoint3DCollectionSerializer,
        XamlPointCollectionSerializer,
        XamlReader,
        XamlStyleSerializer,
        XamlTemplateSerializer,
        XamlVector3DCollectionSerializer,
        XamlWriter,
        XmlDataProvider,
        XmlLangPropertyAttribute,
        XmlLanguage,
        XmlLanguageConverter,
        XmlNamespaceMapping,
        ZoomPercentageConverter,
        MaxElement
    }

    // This enum specifies the IDs we use for known CLR and DP Properties in BAML.
    // The baml files contains the negative of these values.
    internal enum KnownProperties : short
    {
        UnknownProperty = 0,
        AccessText_Text,
        BeginStoryboard_Storyboard,
        BitmapEffectGroup_Children,
        Border_Background,
        Border_BorderBrush,
        Border_BorderThickness,
        ButtonBase_Command,
        ButtonBase_CommandParameter,
        ButtonBase_CommandTarget,
        ButtonBase_IsPressed,
        ColumnDefinition_MaxWidth,
        ColumnDefinition_MinWidth,
        ColumnDefinition_Width,
        ContentControl_Content,
        ContentControl_ContentTemplate,
        ContentControl_ContentTemplateSelector,
        ContentControl_HasContent,
        ContentElement_Focusable,
        ContentPresenter_Content,
        ContentPresenter_ContentSource,
        ContentPresenter_ContentTemplate,
        ContentPresenter_ContentTemplateSelector,
        ContentPresenter_RecognizesAccessKey,
        Control_Background,
        Control_BorderBrush,
        Control_BorderThickness,
        Control_FontFamily,
        Control_FontSize,
        Control_FontStretch,
        Control_FontStyle,
        Control_FontWeight,
        Control_Foreground,
        Control_HorizontalContentAlignment,
        Control_IsTabStop,
        Control_Padding,
        Control_TabIndex,
        Control_Template,
        Control_VerticalContentAlignment,
        DockPanel_Dock,
        DockPanel_LastChildFill,
        DocumentViewerBase_Document,
        DrawingGroup_Children,
        FlowDocumentReader_Document,
        FlowDocumentScrollViewer_Document,
        FrameworkContentElement_Style,
        FrameworkElement_FlowDirection,
        FrameworkElement_Height,
        FrameworkElement_HorizontalAlignment,
        FrameworkElement_Margin,
        FrameworkElement_MaxHeight,
        FrameworkElement_MaxWidth,
        FrameworkElement_MinHeight,
        FrameworkElement_MinWidth,
        FrameworkElement_Name,
        FrameworkElement_Style,
        FrameworkElement_VerticalAlignment,
        FrameworkElement_Width,
        GeneralTransformGroup_Children,
        GeometryGroup_Children,
        GradientBrush_GradientStops,
        Grid_Column,
        Grid_ColumnSpan,
        Grid_Row,
        Grid_RowSpan,
        GridViewColumn_Header,
        HeaderedContentControl_HasHeader,
        HeaderedContentControl_Header,
        HeaderedContentControl_HeaderTemplate,
        HeaderedContentControl_HeaderTemplateSelector,
        HeaderedItemsControl_HasHeader,
        HeaderedItemsControl_Header,
        HeaderedItemsControl_HeaderTemplate,
        HeaderedItemsControl_HeaderTemplateSelector,
        Hyperlink_NavigateUri,
        Image_Source,
        Image_Stretch,
        ItemsControl_ItemContainerStyle,
        ItemsControl_ItemContainerStyleSelector,
        ItemsControl_ItemTemplate,
        ItemsControl_ItemTemplateSelector,
        ItemsControl_ItemsPanel,
        ItemsControl_ItemsSource,
        MaterialGroup_Children,
        Model3DGroup_Children,
        Page_Content,
        Panel_Background,
        Path_Data,
        PathFigure_Segments,
        PathGeometry_Figures,
        Popup_Child,
        Popup_IsOpen,
        Popup_Placement,
        Popup_PopupAnimation,
        RowDefinition_Height,
        RowDefinition_MaxHeight,
        RowDefinition_MinHeight,
        ScrollViewer_CanContentScroll,
        ScrollViewer_HorizontalScrollBarVisibility,
        ScrollViewer_VerticalScrollBarVisibility,
        Shape_Fill,
        Shape_Stroke,
        Shape_StrokeThickness,
        TextBlock_Background,
        TextBlock_FontFamily,
        TextBlock_FontSize,
        TextBlock_FontStretch,
        TextBlock_FontStyle,
        TextBlock_FontWeight,
        TextBlock_Foreground,
        TextBlock_Text,
        TextBlock_TextDecorations,
        TextBlock_TextTrimming,
        TextBlock_TextWrapping,
        TextBox_Text,
        TextElement_Background,
        TextElement_FontFamily,
        TextElement_FontSize,
        TextElement_FontStretch,
        TextElement_FontStyle,
        TextElement_FontWeight,
        TextElement_Foreground,
        TimelineGroup_Children,
        Track_IsDirectionReversed,
        Track_Maximum,
        Track_Minimum,
        Track_Orientation,
        Track_Value,
        Track_ViewportSize,
        Transform3DGroup_Children,
        TransformGroup_Children,
        UIElement_ClipToBounds,
        UIElement_Focusable,
        UIElement_IsEnabled,
        UIElement_RenderTransform,
        UIElement_Visibility,
        Viewport3D_Children,
        MaxDependencyProperty,
        AdornedElementPlaceholder_Child,
        AdornerDecorator_Child,
        AnchoredBlock_Blocks,
        ArrayExtension_Items,
        BlockUIContainer_Child,
        Bold_Inlines,
        BooleanAnimationUsingKeyFrames_KeyFrames,
        Border_Child,
        BulletDecorator_Child,
        Button_Content,
        ButtonBase_Content,
        ByteAnimationUsingKeyFrames_KeyFrames,
        Canvas_Children,
        CharAnimationUsingKeyFrames_KeyFrames,
        CheckBox_Content,
        ColorAnimationUsingKeyFrames_KeyFrames,
        ComboBox_Items,
        ComboBoxItem_Content,
        ContextMenu_Items,
        ControlTemplate_VisualTree,
        DataTemplate_VisualTree,
        DataTrigger_Setters,
        DecimalAnimationUsingKeyFrames_KeyFrames,
        Decorator_Child,
        DockPanel_Children,
        DocumentViewer_Document,
        DoubleAnimationUsingKeyFrames_KeyFrames,
        EventTrigger_Actions,
        Expander_Content,
        Figure_Blocks,
        FixedDocument_Pages,
        FixedDocumentSequence_References,
        FixedPage_Children,
        Floater_Blocks,
        FlowDocument_Blocks,
        FlowDocumentPageViewer_Document,
        FrameworkTemplate_VisualTree,
        Grid_Children,
        GridView_Columns,
        GridViewColumnHeader_Content,
        GroupBox_Content,
        GroupItem_Content,
        HeaderedContentControl_Content,
        HeaderedItemsControl_Items,
        HierarchicalDataTemplate_VisualTree,
        Hyperlink_Inlines,
        InkCanvas_Children,
        InkPresenter_Child,
        InlineUIContainer_Child,
        InputScopeName_NameValue,
        Int16AnimationUsingKeyFrames_KeyFrames,
        Int32AnimationUsingKeyFrames_KeyFrames,
        Int64AnimationUsingKeyFrames_KeyFrames,
        Italic_Inlines,
        ItemsControl_Items,
        ItemsPanelTemplate_VisualTree,
        Label_Content,
        LinearGradientBrush_GradientStops,
        List_ListItems,
        ListBox_Items,
        ListBoxItem_Content,
        ListItem_Blocks,
        ListView_Items,
        ListViewItem_Content,
        MatrixAnimationUsingKeyFrames_KeyFrames,
        Menu_Items,
        MenuBase_Items,
        MenuItem_Items,
        ModelVisual3D_Children,
        MultiBinding_Bindings,
        MultiDataTrigger_Setters,
        MultiTrigger_Setters,
        ObjectAnimationUsingKeyFrames_KeyFrames,
        PageContent_Child,
        PageFunctionBase_Content,
        Panel_Children,
        Paragraph_Inlines,
        ParallelTimeline_Children,
        Point3DAnimationUsingKeyFrames_KeyFrames,
        PointAnimationUsingKeyFrames_KeyFrames,
        PriorityBinding_Bindings,
        QuaternionAnimationUsingKeyFrames_KeyFrames,
        RadialGradientBrush_GradientStops,
        RadioButton_Content,
        RectAnimationUsingKeyFrames_KeyFrames,
        RepeatButton_Content,
        RichTextBox_Document,
        Rotation3DAnimationUsingKeyFrames_KeyFrames,
        Run_Text,
        ScrollViewer_Content,
        Section_Blocks,
        Selector_Items,
        SingleAnimationUsingKeyFrames_KeyFrames,
        SizeAnimationUsingKeyFrames_KeyFrames,
        Span_Inlines,
        StackPanel_Children,
        StatusBar_Items,
        StatusBarItem_Content,
        Storyboard_Children,
        StringAnimationUsingKeyFrames_KeyFrames,
        Style_Setters,
        TabControl_Items,
        TabItem_Content,
        TabPanel_Children,
        Table_RowGroups,
        TableCell_Blocks,
        TableRow_Cells,
        TableRowGroup_Rows,
        TextBlock_Inlines,
        ThicknessAnimationUsingKeyFrames_KeyFrames,
        ToggleButton_Content,
        ToolBar_Items,
        ToolBarOverflowPanel_Children,
        ToolBarPanel_Children,
        ToolBarTray_ToolBars,
        ToolTip_Content,
        TreeView_Items,
        TreeViewItem_Items,
        Trigger_Setters,
        Underline_Inlines,
        UniformGrid_Children,
        UserControl_Content,
        Vector3DAnimationUsingKeyFrames_KeyFrames,
        VectorAnimationUsingKeyFrames_KeyFrames,
        Viewbox_Child,
        Viewport3DVisual_Children,
        VirtualizingPanel_Children,
        VirtualizingStackPanel_Children,
        Window_Content,
        WrapPanel_Children,
        XmlDataProvider_XmlSerializer,
        MaxProperty,
    }

#if !BAMLDASM
    internal static partial class KnownTypes
    {
#if !PBTCOMPILER
        // Code compiled into PresentationFramework.dll

        // Initialize known object types
        internal static object CreateKnownElement(KnownElements knownElement)
        {
            object o = null;
            switch (knownElement)
            {
                case KnownElements.AccessText: o = new System.Windows.Controls.AccessText();   break;
                case KnownElements.AdornedElementPlaceholder: o = new System.Windows.Controls.AdornedElementPlaceholder();   break;
                case KnownElements.AdornerDecorator: o = new System.Windows.Documents.AdornerDecorator();   break;
                case KnownElements.AmbientLight: o = new System.Windows.Media.Media3D.AmbientLight();   break;
                case KnownElements.Application: o = new System.Windows.Application();   break;
                case KnownElements.ArcSegment: o = new System.Windows.Media.ArcSegment();   break;
                case KnownElements.ArrayExtension: o = new System.Windows.Markup.ArrayExtension();   break;
                case KnownElements.AxisAngleRotation3D: o = new System.Windows.Media.Media3D.AxisAngleRotation3D();   break;
                case KnownElements.BeginStoryboard: o = new System.Windows.Media.Animation.BeginStoryboard();   break;
                case KnownElements.BevelBitmapEffect: o = new System.Windows.Media.Effects.BevelBitmapEffect();   break;
                case KnownElements.BezierSegment: o = new System.Windows.Media.BezierSegment();   break;
                case KnownElements.Binding: o = new System.Windows.Data.Binding();   break;
                case KnownElements.BitmapEffectCollection: o = new System.Windows.Media.Effects.BitmapEffectCollection();   break;
                case KnownElements.BitmapEffectGroup: o = new System.Windows.Media.Effects.BitmapEffectGroup();   break;
                case KnownElements.BitmapEffectInput: o = new System.Windows.Media.Effects.BitmapEffectInput();   break;
                case KnownElements.BitmapImage: o = new System.Windows.Media.Imaging.BitmapImage();   break;
                case KnownElements.BlockUIContainer: o = new System.Windows.Documents.BlockUIContainer();   break;
                case KnownElements.BlurBitmapEffect: o = new System.Windows.Media.Effects.BlurBitmapEffect();   break;
                case KnownElements.BmpBitmapEncoder: o = new System.Windows.Media.Imaging.BmpBitmapEncoder();   break;
                case KnownElements.Bold: o = new System.Windows.Documents.Bold();   break;
                case KnownElements.BoolIListConverter: o = new System.Windows.Media.Converters.BoolIListConverter();   break;
                case KnownElements.BooleanAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.BooleanAnimationUsingKeyFrames();   break;
                case KnownElements.BooleanConverter: o = new System.ComponentModel.BooleanConverter();   break;
                case KnownElements.BooleanKeyFrameCollection: o = new System.Windows.Media.Animation.BooleanKeyFrameCollection();   break;
                case KnownElements.BooleanToVisibilityConverter: o = new System.Windows.Controls.BooleanToVisibilityConverter();   break;
                case KnownElements.Border: o = new System.Windows.Controls.Border();   break;
                case KnownElements.BorderGapMaskConverter: o = new System.Windows.Controls.BorderGapMaskConverter();   break;
                case KnownElements.BrushConverter: o = new System.Windows.Media.BrushConverter();   break;
                case KnownElements.BulletDecorator: o = new System.Windows.Controls.Primitives.BulletDecorator();   break;
                case KnownElements.Button: o = new System.Windows.Controls.Button();   break;
                case KnownElements.ByteAnimation: o = new System.Windows.Media.Animation.ByteAnimation();   break;
                case KnownElements.ByteAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.ByteAnimationUsingKeyFrames();   break;
                case KnownElements.ByteConverter: o = new System.ComponentModel.ByteConverter();   break;
                case KnownElements.ByteKeyFrameCollection: o = new System.Windows.Media.Animation.ByteKeyFrameCollection();   break;
                case KnownElements.Canvas: o = new System.Windows.Controls.Canvas();   break;
                case KnownElements.CharAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.CharAnimationUsingKeyFrames();   break;
                case KnownElements.CharConverter: o = new System.ComponentModel.CharConverter();   break;
                case KnownElements.CharIListConverter: o = new System.Windows.Media.Converters.CharIListConverter();   break;
                case KnownElements.CharKeyFrameCollection: o = new System.Windows.Media.Animation.CharKeyFrameCollection();   break;
                case KnownElements.CheckBox: o = new System.Windows.Controls.CheckBox();   break;
                case KnownElements.CollectionContainer: o = new System.Windows.Data.CollectionContainer();   break;
                case KnownElements.CollectionViewSource: o = new System.Windows.Data.CollectionViewSource();   break;
                case KnownElements.Color: o = new System.Windows.Media.Color();   break;
                case KnownElements.ColorAnimation: o = new System.Windows.Media.Animation.ColorAnimation();   break;
                case KnownElements.ColorAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.ColorAnimationUsingKeyFrames();   break;
                case KnownElements.ColorConvertedBitmap: o = new System.Windows.Media.Imaging.ColorConvertedBitmap();   break;
                case KnownElements.ColorConvertedBitmapExtension: o = new System.Windows.ColorConvertedBitmapExtension();   break;
                case KnownElements.ColorConverter: o = new System.Windows.Media.ColorConverter();   break;
                case KnownElements.ColorKeyFrameCollection: o = new System.Windows.Media.Animation.ColorKeyFrameCollection();   break;
                case KnownElements.ColumnDefinition: o = new System.Windows.Controls.ColumnDefinition();   break;
                case KnownElements.CombinedGeometry: o = new System.Windows.Media.CombinedGeometry();   break;
                case KnownElements.ComboBox: o = new System.Windows.Controls.ComboBox();   break;
                case KnownElements.ComboBoxItem: o = new System.Windows.Controls.ComboBoxItem();   break;
                case KnownElements.CommandConverter: o = new System.Windows.Input.CommandConverter();   break;
                case KnownElements.ComponentResourceKey: o = new System.Windows.ComponentResourceKey();   break;
                case KnownElements.ComponentResourceKeyConverter: o = new System.Windows.Markup.ComponentResourceKeyConverter();   break;
                case KnownElements.Condition: o = new System.Windows.Condition();   break;
                case KnownElements.ContainerVisual: o = new System.Windows.Media.ContainerVisual();   break;
                case KnownElements.ContentControl: o = new System.Windows.Controls.ContentControl();   break;
                case KnownElements.ContentElement: o = new System.Windows.ContentElement();   break;
                case KnownElements.ContentPresenter: o = new System.Windows.Controls.ContentPresenter();   break;
                case KnownElements.ContextMenu: o = new System.Windows.Controls.ContextMenu();   break;
                case KnownElements.Control: o = new System.Windows.Controls.Control();   break;
                case KnownElements.ControlTemplate: o = new System.Windows.Controls.ControlTemplate();   break;
                case KnownElements.CornerRadius: o = new System.Windows.CornerRadius();   break;
                case KnownElements.CornerRadiusConverter: o = new System.Windows.CornerRadiusConverter();   break;
                case KnownElements.CroppedBitmap: o = new System.Windows.Media.Imaging.CroppedBitmap();   break;
                case KnownElements.CultureInfoConverter: o = new System.ComponentModel.CultureInfoConverter();   break;
                case KnownElements.CultureInfoIetfLanguageTagConverter: o = new System.Windows.CultureInfoIetfLanguageTagConverter();   break;
                case KnownElements.CursorConverter: o = new System.Windows.Input.CursorConverter();   break;
                case KnownElements.DashStyle: o = new System.Windows.Media.DashStyle();   break;
                case KnownElements.DataTemplate: o = new System.Windows.DataTemplate();   break;
                case KnownElements.DataTemplateKey: o = new System.Windows.DataTemplateKey();   break;
                case KnownElements.DataTrigger: o = new System.Windows.DataTrigger();   break;
                case KnownElements.DateTimeConverter: o = new System.ComponentModel.DateTimeConverter();   break;
                case KnownElements.DateTimeConverter2: o = new System.Windows.Markup.DateTimeConverter2();   break;
                case KnownElements.DecimalAnimation: o = new System.Windows.Media.Animation.DecimalAnimation();   break;
                case KnownElements.DecimalAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.DecimalAnimationUsingKeyFrames();   break;
                case KnownElements.DecimalConverter: o = new System.ComponentModel.DecimalConverter();   break;
                case KnownElements.DecimalKeyFrameCollection: o = new System.Windows.Media.Animation.DecimalKeyFrameCollection();   break;
                case KnownElements.Decorator: o = new System.Windows.Controls.Decorator();   break;
                case KnownElements.DependencyObject: o = new System.Windows.DependencyObject();   break;
                case KnownElements.DependencyPropertyConverter: o = new System.Windows.Markup.DependencyPropertyConverter();   break;
                case KnownElements.DialogResultConverter: o = new System.Windows.DialogResultConverter();   break;
                case KnownElements.DiffuseMaterial: o = new System.Windows.Media.Media3D.DiffuseMaterial();   break;
                case KnownElements.DirectionalLight: o = new System.Windows.Media.Media3D.DirectionalLight();   break;
                case KnownElements.DiscreteBooleanKeyFrame: o = new System.Windows.Media.Animation.DiscreteBooleanKeyFrame();   break;
                case KnownElements.DiscreteByteKeyFrame: o = new System.Windows.Media.Animation.DiscreteByteKeyFrame();   break;
                case KnownElements.DiscreteCharKeyFrame: o = new System.Windows.Media.Animation.DiscreteCharKeyFrame();   break;
                case KnownElements.DiscreteColorKeyFrame: o = new System.Windows.Media.Animation.DiscreteColorKeyFrame();   break;
                case KnownElements.DiscreteDecimalKeyFrame: o = new System.Windows.Media.Animation.DiscreteDecimalKeyFrame();   break;
                case KnownElements.DiscreteDoubleKeyFrame: o = new System.Windows.Media.Animation.DiscreteDoubleKeyFrame();   break;
                case KnownElements.DiscreteInt16KeyFrame: o = new System.Windows.Media.Animation.DiscreteInt16KeyFrame();   break;
                case KnownElements.DiscreteInt32KeyFrame: o = new System.Windows.Media.Animation.DiscreteInt32KeyFrame();   break;
                case KnownElements.DiscreteInt64KeyFrame: o = new System.Windows.Media.Animation.DiscreteInt64KeyFrame();   break;
                case KnownElements.DiscreteMatrixKeyFrame: o = new System.Windows.Media.Animation.DiscreteMatrixKeyFrame();   break;
                case KnownElements.DiscreteObjectKeyFrame: o = new System.Windows.Media.Animation.DiscreteObjectKeyFrame();   break;
                case KnownElements.DiscretePoint3DKeyFrame: o = new System.Windows.Media.Animation.DiscretePoint3DKeyFrame();   break;
                case KnownElements.DiscretePointKeyFrame: o = new System.Windows.Media.Animation.DiscretePointKeyFrame();   break;
                case KnownElements.DiscreteQuaternionKeyFrame: o = new System.Windows.Media.Animation.DiscreteQuaternionKeyFrame();   break;
                case KnownElements.DiscreteRectKeyFrame: o = new System.Windows.Media.Animation.DiscreteRectKeyFrame();   break;
                case KnownElements.DiscreteRotation3DKeyFrame: o = new System.Windows.Media.Animation.DiscreteRotation3DKeyFrame();   break;
                case KnownElements.DiscreteSingleKeyFrame: o = new System.Windows.Media.Animation.DiscreteSingleKeyFrame();   break;
                case KnownElements.DiscreteSizeKeyFrame: o = new System.Windows.Media.Animation.DiscreteSizeKeyFrame();   break;
                case KnownElements.DiscreteStringKeyFrame: o = new System.Windows.Media.Animation.DiscreteStringKeyFrame();   break;
                case KnownElements.DiscreteThicknessKeyFrame: o = new System.Windows.Media.Animation.DiscreteThicknessKeyFrame();   break;
                case KnownElements.DiscreteVector3DKeyFrame: o = new System.Windows.Media.Animation.DiscreteVector3DKeyFrame();   break;
                case KnownElements.DiscreteVectorKeyFrame: o = new System.Windows.Media.Animation.DiscreteVectorKeyFrame();   break;
                case KnownElements.DockPanel: o = new System.Windows.Controls.DockPanel();   break;
                case KnownElements.DocumentPageView: o = new System.Windows.Controls.Primitives.DocumentPageView();   break;
                case KnownElements.DocumentReference: o = new System.Windows.Documents.DocumentReference();   break;
                case KnownElements.DocumentViewer: o = new System.Windows.Controls.DocumentViewer();   break;
                case KnownElements.DoubleAnimation: o = new System.Windows.Media.Animation.DoubleAnimation();   break;
                case KnownElements.DoubleAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames();   break;
                case KnownElements.DoubleAnimationUsingPath: o = new System.Windows.Media.Animation.DoubleAnimationUsingPath();   break;
                case KnownElements.DoubleCollection: o = new System.Windows.Media.DoubleCollection();   break;
                case KnownElements.DoubleCollectionConverter: o = new System.Windows.Media.DoubleCollectionConverter();   break;
                case KnownElements.DoubleConverter: o = new System.ComponentModel.DoubleConverter();   break;
                case KnownElements.DoubleIListConverter: o = new System.Windows.Media.Converters.DoubleIListConverter();   break;
                case KnownElements.DoubleKeyFrameCollection: o = new System.Windows.Media.Animation.DoubleKeyFrameCollection();   break;
                case KnownElements.DrawingBrush: o = new System.Windows.Media.DrawingBrush();   break;
                case KnownElements.DrawingCollection: o = new System.Windows.Media.DrawingCollection();   break;
                case KnownElements.DrawingGroup: o = new System.Windows.Media.DrawingGroup();   break;
                case KnownElements.DrawingImage: o = new System.Windows.Media.DrawingImage();   break;
                case KnownElements.DrawingVisual: o = new System.Windows.Media.DrawingVisual();   break;
                case KnownElements.DropShadowBitmapEffect: o = new System.Windows.Media.Effects.DropShadowBitmapEffect();   break;
                case KnownElements.Duration: o = new System.Windows.Duration();   break;
                case KnownElements.DurationConverter: o = new System.Windows.DurationConverter();   break;
                case KnownElements.DynamicResourceExtension: o = new System.Windows.DynamicResourceExtension();   break;
                case KnownElements.DynamicResourceExtensionConverter: o = new System.Windows.DynamicResourceExtensionConverter();   break;
                case KnownElements.Ellipse: o = new System.Windows.Shapes.Ellipse();   break;
                case KnownElements.EllipseGeometry: o = new System.Windows.Media.EllipseGeometry();   break;
                case KnownElements.EmbossBitmapEffect: o = new System.Windows.Media.Effects.EmbossBitmapEffect();   break;
                case KnownElements.EmissiveMaterial: o = new System.Windows.Media.Media3D.EmissiveMaterial();   break;
                case KnownElements.EventSetter: o = new System.Windows.EventSetter();   break;
                case KnownElements.EventTrigger: o = new System.Windows.EventTrigger();   break;
                case KnownElements.Expander: o = new System.Windows.Controls.Expander();   break;
                case KnownElements.ExpressionConverter: o = new System.Windows.ExpressionConverter();   break;
                case KnownElements.Figure: o = new System.Windows.Documents.Figure();   break;
                case KnownElements.FigureLength: o = new System.Windows.FigureLength();   break;
                case KnownElements.FigureLengthConverter: o = new System.Windows.FigureLengthConverter();   break;
                case KnownElements.FixedDocument: o = new System.Windows.Documents.FixedDocument();   break;
                case KnownElements.FixedDocumentSequence: o = new System.Windows.Documents.FixedDocumentSequence();   break;
                case KnownElements.FixedPage: o = new System.Windows.Documents.FixedPage();   break;
                case KnownElements.Floater: o = new System.Windows.Documents.Floater();   break;
                case KnownElements.FlowDocument: o = new System.Windows.Documents.FlowDocument();   break;
                case KnownElements.FlowDocumentPageViewer: o = new System.Windows.Controls.FlowDocumentPageViewer();   break;
                case KnownElements.FlowDocumentReader: o = new System.Windows.Controls.FlowDocumentReader();   break;
                case KnownElements.FlowDocumentScrollViewer: o = new System.Windows.Controls.FlowDocumentScrollViewer();   break;
                case KnownElements.FontFamily: o = new System.Windows.Media.FontFamily();   break;
                case KnownElements.FontFamilyConverter: o = new System.Windows.Media.FontFamilyConverter();   break;
                case KnownElements.FontSizeConverter: o = new System.Windows.FontSizeConverter();   break;
                case KnownElements.FontStretch: o = new System.Windows.FontStretch();   break;
                case KnownElements.FontStretchConverter: o = new System.Windows.FontStretchConverter();   break;
                case KnownElements.FontStyle: o = new System.Windows.FontStyle();   break;
                case KnownElements.FontStyleConverter: o = new System.Windows.FontStyleConverter();   break;
                case KnownElements.FontWeight: o = new System.Windows.FontWeight();   break;
                case KnownElements.FontWeightConverter: o = new System.Windows.FontWeightConverter();   break;
                case KnownElements.FormatConvertedBitmap: o = new System.Windows.Media.Imaging.FormatConvertedBitmap();   break;
                case KnownElements.Frame: o = new System.Windows.Controls.Frame();   break;
                case KnownElements.FrameworkContentElement: o = new System.Windows.FrameworkContentElement();   break;
                case KnownElements.FrameworkElement: o = new System.Windows.FrameworkElement();   break;
                case KnownElements.FrameworkElementFactory: o = new System.Windows.FrameworkElementFactory();   break;
                case KnownElements.FrameworkPropertyMetadata: o = new System.Windows.FrameworkPropertyMetadata();   break;
                case KnownElements.GeneralTransformCollection: o = new System.Windows.Media.GeneralTransformCollection();   break;
                case KnownElements.GeneralTransformGroup: o = new System.Windows.Media.GeneralTransformGroup();   break;
                case KnownElements.GeometryCollection: o = new System.Windows.Media.GeometryCollection();   break;
                case KnownElements.GeometryConverter: o = new System.Windows.Media.GeometryConverter();   break;
                case KnownElements.GeometryDrawing: o = new System.Windows.Media.GeometryDrawing();   break;
                case KnownElements.GeometryGroup: o = new System.Windows.Media.GeometryGroup();   break;
                case KnownElements.GeometryModel3D: o = new System.Windows.Media.Media3D.GeometryModel3D();   break;
                case KnownElements.GestureRecognizer: o = new System.Windows.Ink.GestureRecognizer();   break;
                case KnownElements.GifBitmapEncoder: o = new System.Windows.Media.Imaging.GifBitmapEncoder();   break;
                case KnownElements.GlyphRun: o = new System.Windows.Media.GlyphRun();   break;
                case KnownElements.GlyphRunDrawing: o = new System.Windows.Media.GlyphRunDrawing();   break;
                case KnownElements.GlyphTypeface: o = new System.Windows.Media.GlyphTypeface();   break;
                case KnownElements.Glyphs: o = new System.Windows.Documents.Glyphs();   break;
                case KnownElements.GradientStop: o = new System.Windows.Media.GradientStop();   break;
                case KnownElements.GradientStopCollection: o = new System.Windows.Media.GradientStopCollection();   break;
                case KnownElements.Grid: o = new System.Windows.Controls.Grid();   break;
                case KnownElements.GridLength: o = new System.Windows.GridLength();   break;
                case KnownElements.GridLengthConverter: o = new System.Windows.GridLengthConverter();   break;
                case KnownElements.GridSplitter: o = new System.Windows.Controls.GridSplitter();   break;
                case KnownElements.GridView: o = new System.Windows.Controls.GridView();   break;
                case KnownElements.GridViewColumn: o = new System.Windows.Controls.GridViewColumn();   break;
                case KnownElements.GridViewColumnHeader: o = new System.Windows.Controls.GridViewColumnHeader();   break;
                case KnownElements.GridViewHeaderRowPresenter: o = new System.Windows.Controls.GridViewHeaderRowPresenter();   break;
                case KnownElements.GridViewRowPresenter: o = new System.Windows.Controls.GridViewRowPresenter();   break;
                case KnownElements.GroupBox: o = new System.Windows.Controls.GroupBox();   break;
                case KnownElements.GroupItem: o = new System.Windows.Controls.GroupItem();   break;
                case KnownElements.GuidConverter: o = new System.ComponentModel.GuidConverter();   break;
                case KnownElements.GuidelineSet: o = new System.Windows.Media.GuidelineSet();   break;
                case KnownElements.HeaderedContentControl: o = new System.Windows.Controls.HeaderedContentControl();   break;
                case KnownElements.HeaderedItemsControl: o = new System.Windows.Controls.HeaderedItemsControl();   break;
                case KnownElements.HierarchicalDataTemplate: o = new System.Windows.HierarchicalDataTemplate();   break;
                case KnownElements.HostVisual: o = new System.Windows.Media.HostVisual();   break;
                case KnownElements.Hyperlink: o = new System.Windows.Documents.Hyperlink();   break;
                case KnownElements.Image: o = new System.Windows.Controls.Image();   break;
                case KnownElements.ImageBrush: o = new System.Windows.Media.ImageBrush();   break;
                case KnownElements.ImageDrawing: o = new System.Windows.Media.ImageDrawing();   break;
                case KnownElements.ImageSourceConverter: o = new System.Windows.Media.ImageSourceConverter();   break;
                case KnownElements.InkCanvas: o = new System.Windows.Controls.InkCanvas();   break;
                case KnownElements.InkPresenter: o = new System.Windows.Controls.InkPresenter();   break;
                case KnownElements.InlineUIContainer: o = new System.Windows.Documents.InlineUIContainer();   break;
                case KnownElements.InputScope: o = new System.Windows.Input.InputScope();   break;
                case KnownElements.InputScopeConverter: o = new System.Windows.Input.InputScopeConverter();   break;
                case KnownElements.InputScopeName: o = new System.Windows.Input.InputScopeName();   break;
                case KnownElements.InputScopeNameConverter: o = new System.Windows.Input.InputScopeNameConverter();   break;
                case KnownElements.Int16Animation: o = new System.Windows.Media.Animation.Int16Animation();   break;
                case KnownElements.Int16AnimationUsingKeyFrames: o = new System.Windows.Media.Animation.Int16AnimationUsingKeyFrames();   break;
                case KnownElements.Int16Converter: o = new System.ComponentModel.Int16Converter();   break;
                case KnownElements.Int16KeyFrameCollection: o = new System.Windows.Media.Animation.Int16KeyFrameCollection();   break;
                case KnownElements.Int32Animation: o = new System.Windows.Media.Animation.Int32Animation();   break;
                case KnownElements.Int32AnimationUsingKeyFrames: o = new System.Windows.Media.Animation.Int32AnimationUsingKeyFrames();   break;
                case KnownElements.Int32Collection: o = new System.Windows.Media.Int32Collection();   break;
                case KnownElements.Int32CollectionConverter: o = new System.Windows.Media.Int32CollectionConverter();   break;
                case KnownElements.Int32Converter: o = new System.ComponentModel.Int32Converter();   break;
                case KnownElements.Int32KeyFrameCollection: o = new System.Windows.Media.Animation.Int32KeyFrameCollection();   break;
                case KnownElements.Int32Rect: o = new System.Windows.Int32Rect();   break;
                case KnownElements.Int32RectConverter: o = new System.Windows.Int32RectConverter();   break;
                case KnownElements.Int64Animation: o = new System.Windows.Media.Animation.Int64Animation();   break;
                case KnownElements.Int64AnimationUsingKeyFrames: o = new System.Windows.Media.Animation.Int64AnimationUsingKeyFrames();   break;
                case KnownElements.Int64Converter: o = new System.ComponentModel.Int64Converter();   break;
                case KnownElements.Int64KeyFrameCollection: o = new System.Windows.Media.Animation.Int64KeyFrameCollection();   break;
                case KnownElements.Italic: o = new System.Windows.Documents.Italic();   break;
                case KnownElements.ItemsControl: o = new System.Windows.Controls.ItemsControl();   break;
                case KnownElements.ItemsPanelTemplate: o = new System.Windows.Controls.ItemsPanelTemplate();   break;
                case KnownElements.ItemsPresenter: o = new System.Windows.Controls.ItemsPresenter();   break;
                case KnownElements.JournalEntryListConverter: o = new System.Windows.Navigation.JournalEntryListConverter();   break;
                case KnownElements.JournalEntryUnifiedViewConverter: o = new System.Windows.Navigation.JournalEntryUnifiedViewConverter();   break;
                case KnownElements.JpegBitmapEncoder: o = new System.Windows.Media.Imaging.JpegBitmapEncoder();   break;
                case KnownElements.KeyBinding: o = new System.Windows.Input.KeyBinding();   break;
                case KnownElements.KeyConverter: o = new System.Windows.Input.KeyConverter();   break;
                case KnownElements.KeyGestureConverter: o = new System.Windows.Input.KeyGestureConverter();   break;
                case KnownElements.KeySpline: o = new System.Windows.Media.Animation.KeySpline();   break;
                case KnownElements.KeySplineConverter: o = new System.Windows.KeySplineConverter();   break;
                case KnownElements.KeyTime: o = new System.Windows.Media.Animation.KeyTime();   break;
                case KnownElements.KeyTimeConverter: o = new System.Windows.KeyTimeConverter();   break;
                case KnownElements.Label: o = new System.Windows.Controls.Label();   break;
                case KnownElements.LengthConverter: o = new System.Windows.LengthConverter();   break;
                case KnownElements.Line: o = new System.Windows.Shapes.Line();   break;
                case KnownElements.LineBreak: o = new System.Windows.Documents.LineBreak();   break;
                case KnownElements.LineGeometry: o = new System.Windows.Media.LineGeometry();   break;
                case KnownElements.LineSegment: o = new System.Windows.Media.LineSegment();   break;
                case KnownElements.LinearByteKeyFrame: o = new System.Windows.Media.Animation.LinearByteKeyFrame();   break;
                case KnownElements.LinearColorKeyFrame: o = new System.Windows.Media.Animation.LinearColorKeyFrame();   break;
                case KnownElements.LinearDecimalKeyFrame: o = new System.Windows.Media.Animation.LinearDecimalKeyFrame();   break;
                case KnownElements.LinearDoubleKeyFrame: o = new System.Windows.Media.Animation.LinearDoubleKeyFrame();   break;
                case KnownElements.LinearGradientBrush: o = new System.Windows.Media.LinearGradientBrush();   break;
                case KnownElements.LinearInt16KeyFrame: o = new System.Windows.Media.Animation.LinearInt16KeyFrame();   break;
                case KnownElements.LinearInt32KeyFrame: o = new System.Windows.Media.Animation.LinearInt32KeyFrame();   break;
                case KnownElements.LinearInt64KeyFrame: o = new System.Windows.Media.Animation.LinearInt64KeyFrame();   break;
                case KnownElements.LinearPoint3DKeyFrame: o = new System.Windows.Media.Animation.LinearPoint3DKeyFrame();   break;
                case KnownElements.LinearPointKeyFrame: o = new System.Windows.Media.Animation.LinearPointKeyFrame();   break;
                case KnownElements.LinearQuaternionKeyFrame: o = new System.Windows.Media.Animation.LinearQuaternionKeyFrame();   break;
                case KnownElements.LinearRectKeyFrame: o = new System.Windows.Media.Animation.LinearRectKeyFrame();   break;
                case KnownElements.LinearRotation3DKeyFrame: o = new System.Windows.Media.Animation.LinearRotation3DKeyFrame();   break;
                case KnownElements.LinearSingleKeyFrame: o = new System.Windows.Media.Animation.LinearSingleKeyFrame();   break;
                case KnownElements.LinearSizeKeyFrame: o = new System.Windows.Media.Animation.LinearSizeKeyFrame();   break;
                case KnownElements.LinearThicknessKeyFrame: o = new System.Windows.Media.Animation.LinearThicknessKeyFrame();   break;
                case KnownElements.LinearVector3DKeyFrame: o = new System.Windows.Media.Animation.LinearVector3DKeyFrame();   break;
                case KnownElements.LinearVectorKeyFrame: o = new System.Windows.Media.Animation.LinearVectorKeyFrame();   break;
                case KnownElements.List: o = new System.Windows.Documents.List();   break;
                case KnownElements.ListBox: o = new System.Windows.Controls.ListBox();   break;
                case KnownElements.ListBoxItem: o = new System.Windows.Controls.ListBoxItem();   break;
                case KnownElements.ListItem: o = new System.Windows.Documents.ListItem();   break;
                case KnownElements.ListView: o = new System.Windows.Controls.ListView();   break;
                case KnownElements.ListViewItem: o = new System.Windows.Controls.ListViewItem();   break;
                case KnownElements.MaterialCollection: o = new System.Windows.Media.Media3D.MaterialCollection();   break;
                case KnownElements.MaterialGroup: o = new System.Windows.Media.Media3D.MaterialGroup();   break;
                case KnownElements.Matrix: o = new System.Windows.Media.Matrix();   break;
                case KnownElements.Matrix3D: o = new System.Windows.Media.Media3D.Matrix3D();   break;
                case KnownElements.Matrix3DConverter: o = new System.Windows.Media.Media3D.Matrix3DConverter();   break;
                case KnownElements.MatrixAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.MatrixAnimationUsingKeyFrames();   break;
                case KnownElements.MatrixAnimationUsingPath: o = new System.Windows.Media.Animation.MatrixAnimationUsingPath();   break;
                case KnownElements.MatrixCamera: o = new System.Windows.Media.Media3D.MatrixCamera();   break;
                case KnownElements.MatrixConverter: o = new System.Windows.Media.MatrixConverter();   break;
                case KnownElements.MatrixKeyFrameCollection: o = new System.Windows.Media.Animation.MatrixKeyFrameCollection();   break;
                case KnownElements.MatrixTransform: o = new System.Windows.Media.MatrixTransform();   break;
                case KnownElements.MatrixTransform3D: o = new System.Windows.Media.Media3D.MatrixTransform3D();   break;
                case KnownElements.MediaElement: o = new System.Windows.Controls.MediaElement();   break;
                case KnownElements.MediaPlayer: o = new System.Windows.Media.MediaPlayer();   break;
                case KnownElements.MediaTimeline: o = new System.Windows.Media.MediaTimeline();   break;
                case KnownElements.Menu: o = new System.Windows.Controls.Menu();   break;
                case KnownElements.MenuItem: o = new System.Windows.Controls.MenuItem();   break;
                case KnownElements.MenuScrollingVisibilityConverter: o = new System.Windows.Controls.MenuScrollingVisibilityConverter();   break;
                case KnownElements.MeshGeometry3D: o = new System.Windows.Media.Media3D.MeshGeometry3D();   break;
                case KnownElements.Model3DCollection: o = new System.Windows.Media.Media3D.Model3DCollection();   break;
                case KnownElements.Model3DGroup: o = new System.Windows.Media.Media3D.Model3DGroup();   break;
                case KnownElements.ModelVisual3D: o = new System.Windows.Media.Media3D.ModelVisual3D();   break;
                case KnownElements.ModifierKeysConverter: o = new System.Windows.Input.ModifierKeysConverter();   break;
                case KnownElements.MouseActionConverter: o = new System.Windows.Input.MouseActionConverter();   break;
                case KnownElements.MouseBinding: o = new System.Windows.Input.MouseBinding();   break;
                case KnownElements.MouseGesture: o = new System.Windows.Input.MouseGesture();   break;
                case KnownElements.MouseGestureConverter: o = new System.Windows.Input.MouseGestureConverter();   break;
                case KnownElements.MultiBinding: o = new System.Windows.Data.MultiBinding();   break;
                case KnownElements.MultiDataTrigger: o = new System.Windows.MultiDataTrigger();   break;
                case KnownElements.MultiTrigger: o = new System.Windows.MultiTrigger();   break;
                case KnownElements.NameScope: o = new System.Windows.NameScope();   break;
                case KnownElements.NavigationWindow: o = new System.Windows.Navigation.NavigationWindow();   break;
                case KnownElements.NullExtension: o = new System.Windows.Markup.NullExtension();   break;
                case KnownElements.NullableBoolConverter: o = new System.Windows.NullableBoolConverter();   break;
                case KnownElements.NumberSubstitution: o = new System.Windows.Media.NumberSubstitution();   break;
                case KnownElements.Object: o = new System.Object();   break;
                case KnownElements.ObjectAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.ObjectAnimationUsingKeyFrames();   break;
                case KnownElements.ObjectDataProvider: o = new System.Windows.Data.ObjectDataProvider();   break;
                case KnownElements.ObjectKeyFrameCollection: o = new System.Windows.Media.Animation.ObjectKeyFrameCollection();   break;
                case KnownElements.OrthographicCamera: o = new System.Windows.Media.Media3D.OrthographicCamera();   break;
                case KnownElements.OuterGlowBitmapEffect: o = new System.Windows.Media.Effects.OuterGlowBitmapEffect();   break;
                case KnownElements.Page: o = new System.Windows.Controls.Page();   break;
                case KnownElements.PageContent: o = new System.Windows.Documents.PageContent();   break;
                case KnownElements.Paragraph: o = new System.Windows.Documents.Paragraph();   break;
                case KnownElements.ParallelTimeline: o = new System.Windows.Media.Animation.ParallelTimeline();   break;
                case KnownElements.ParserContext: o = new System.Windows.Markup.ParserContext();   break;
                case KnownElements.PasswordBox: o = new System.Windows.Controls.PasswordBox();   break;
                case KnownElements.Path: o = new System.Windows.Shapes.Path();   break;
                case KnownElements.PathFigure: o = new System.Windows.Media.PathFigure();   break;
                case KnownElements.PathFigureCollection: o = new System.Windows.Media.PathFigureCollection();   break;
                case KnownElements.PathFigureCollectionConverter: o = new System.Windows.Media.PathFigureCollectionConverter();   break;
                case KnownElements.PathGeometry: o = new System.Windows.Media.PathGeometry();   break;
                case KnownElements.PathSegmentCollection: o = new System.Windows.Media.PathSegmentCollection();   break;
                case KnownElements.PauseStoryboard: o = new System.Windows.Media.Animation.PauseStoryboard();   break;
                case KnownElements.Pen: o = new System.Windows.Media.Pen();   break;
                case KnownElements.PerspectiveCamera: o = new System.Windows.Media.Media3D.PerspectiveCamera();   break;
                case KnownElements.PixelFormat: o = new System.Windows.Media.PixelFormat();   break;
                case KnownElements.PixelFormatConverter: o = new System.Windows.Media.PixelFormatConverter();   break;
                case KnownElements.PngBitmapEncoder: o = new System.Windows.Media.Imaging.PngBitmapEncoder();   break;
                case KnownElements.Point: o = new System.Windows.Point();   break;
                case KnownElements.Point3D: o = new System.Windows.Media.Media3D.Point3D();   break;
                case KnownElements.Point3DAnimation: o = new System.Windows.Media.Animation.Point3DAnimation();   break;
                case KnownElements.Point3DAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.Point3DAnimationUsingKeyFrames();   break;
                case KnownElements.Point3DCollection: o = new System.Windows.Media.Media3D.Point3DCollection();   break;
                case KnownElements.Point3DCollectionConverter: o = new System.Windows.Media.Media3D.Point3DCollectionConverter();   break;
                case KnownElements.Point3DConverter: o = new System.Windows.Media.Media3D.Point3DConverter();   break;
                case KnownElements.Point3DKeyFrameCollection: o = new System.Windows.Media.Animation.Point3DKeyFrameCollection();   break;
                case KnownElements.Point4D: o = new System.Windows.Media.Media3D.Point4D();   break;
                case KnownElements.Point4DConverter: o = new System.Windows.Media.Media3D.Point4DConverter();   break;
                case KnownElements.PointAnimation: o = new System.Windows.Media.Animation.PointAnimation();   break;
                case KnownElements.PointAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.PointAnimationUsingKeyFrames();   break;
                case KnownElements.PointAnimationUsingPath: o = new System.Windows.Media.Animation.PointAnimationUsingPath();   break;
                case KnownElements.PointCollection: o = new System.Windows.Media.PointCollection();   break;
                case KnownElements.PointCollectionConverter: o = new System.Windows.Media.PointCollectionConverter();   break;
                case KnownElements.PointConverter: o = new System.Windows.PointConverter();   break;
                case KnownElements.PointIListConverter: o = new System.Windows.Media.Converters.PointIListConverter();   break;
                case KnownElements.PointKeyFrameCollection: o = new System.Windows.Media.Animation.PointKeyFrameCollection();   break;
                case KnownElements.PointLight: o = new System.Windows.Media.Media3D.PointLight();   break;
                case KnownElements.PolyBezierSegment: o = new System.Windows.Media.PolyBezierSegment();   break;
                case KnownElements.PolyLineSegment: o = new System.Windows.Media.PolyLineSegment();   break;
                case KnownElements.PolyQuadraticBezierSegment: o = new System.Windows.Media.PolyQuadraticBezierSegment();   break;
                case KnownElements.Polygon: o = new System.Windows.Shapes.Polygon();   break;
                case KnownElements.Polyline: o = new System.Windows.Shapes.Polyline();   break;
                case KnownElements.Popup: o = new System.Windows.Controls.Primitives.Popup();   break;
                case KnownElements.PriorityBinding: o = new System.Windows.Data.PriorityBinding();   break;
                case KnownElements.ProgressBar: o = new System.Windows.Controls.ProgressBar();   break;
                case KnownElements.PropertyPathConverter: o = new System.Windows.PropertyPathConverter();   break;
                case KnownElements.QuadraticBezierSegment: o = new System.Windows.Media.QuadraticBezierSegment();   break;
                case KnownElements.Quaternion: o = new System.Windows.Media.Media3D.Quaternion();   break;
                case KnownElements.QuaternionAnimation: o = new System.Windows.Media.Animation.QuaternionAnimation();   break;
                case KnownElements.QuaternionAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.QuaternionAnimationUsingKeyFrames();   break;
                case KnownElements.QuaternionConverter: o = new System.Windows.Media.Media3D.QuaternionConverter();   break;
                case KnownElements.QuaternionKeyFrameCollection: o = new System.Windows.Media.Animation.QuaternionKeyFrameCollection();   break;
                case KnownElements.QuaternionRotation3D: o = new System.Windows.Media.Media3D.QuaternionRotation3D();   break;
                case KnownElements.RadialGradientBrush: o = new System.Windows.Media.RadialGradientBrush();   break;
                case KnownElements.RadioButton: o = new System.Windows.Controls.RadioButton();   break;
                case KnownElements.Rect: o = new System.Windows.Rect();   break;
                case KnownElements.Rect3D: o = new System.Windows.Media.Media3D.Rect3D();   break;
                case KnownElements.Rect3DConverter: o = new System.Windows.Media.Media3D.Rect3DConverter();   break;
                case KnownElements.RectAnimation: o = new System.Windows.Media.Animation.RectAnimation();   break;
                case KnownElements.RectAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.RectAnimationUsingKeyFrames();   break;
                case KnownElements.RectConverter: o = new System.Windows.RectConverter();   break;
                case KnownElements.RectKeyFrameCollection: o = new System.Windows.Media.Animation.RectKeyFrameCollection();   break;
                case KnownElements.Rectangle: o = new System.Windows.Shapes.Rectangle();   break;
                case KnownElements.RectangleGeometry: o = new System.Windows.Media.RectangleGeometry();   break;
                case KnownElements.RelativeSource: o = new System.Windows.Data.RelativeSource();   break;
                case KnownElements.RemoveStoryboard: o = new System.Windows.Media.Animation.RemoveStoryboard();   break;
                case KnownElements.RepeatBehavior: o = new System.Windows.Media.Animation.RepeatBehavior();   break;
                case KnownElements.RepeatBehaviorConverter: o = new System.Windows.Media.Animation.RepeatBehaviorConverter();   break;
                case KnownElements.RepeatButton: o = new System.Windows.Controls.Primitives.RepeatButton();   break;
                case KnownElements.ResizeGrip: o = new System.Windows.Controls.Primitives.ResizeGrip();   break;
                case KnownElements.ResourceDictionary: o = new System.Windows.ResourceDictionary();   break;
                case KnownElements.ResumeStoryboard: o = new System.Windows.Media.Animation.ResumeStoryboard();   break;
                case KnownElements.RichTextBox: o = new System.Windows.Controls.RichTextBox();   break;
                case KnownElements.RotateTransform: o = new System.Windows.Media.RotateTransform();   break;
                case KnownElements.RotateTransform3D: o = new System.Windows.Media.Media3D.RotateTransform3D();   break;
                case KnownElements.Rotation3DAnimation: o = new System.Windows.Media.Animation.Rotation3DAnimation();   break;
                case KnownElements.Rotation3DAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.Rotation3DAnimationUsingKeyFrames();   break;
                case KnownElements.Rotation3DKeyFrameCollection: o = new System.Windows.Media.Animation.Rotation3DKeyFrameCollection();   break;
                case KnownElements.RoutedCommand: o = new System.Windows.Input.RoutedCommand();   break;
                case KnownElements.RoutedEventConverter: o = new System.Windows.Markup.RoutedEventConverter();   break;
                case KnownElements.RoutedUICommand: o = new System.Windows.Input.RoutedUICommand();   break;
                case KnownElements.RowDefinition: o = new System.Windows.Controls.RowDefinition();   break;
                case KnownElements.Run: o = new System.Windows.Documents.Run();   break;
                case KnownElements.SByteConverter: o = new System.ComponentModel.SByteConverter();   break;
                case KnownElements.ScaleTransform: o = new System.Windows.Media.ScaleTransform();   break;
                case KnownElements.ScaleTransform3D: o = new System.Windows.Media.Media3D.ScaleTransform3D();   break;
                case KnownElements.ScrollBar: o = new System.Windows.Controls.Primitives.ScrollBar();   break;
                case KnownElements.ScrollContentPresenter: o = new System.Windows.Controls.ScrollContentPresenter();   break;
                case KnownElements.ScrollViewer: o = new System.Windows.Controls.ScrollViewer();   break;
                case KnownElements.Section: o = new System.Windows.Documents.Section();   break;
                case KnownElements.SeekStoryboard: o = new System.Windows.Media.Animation.SeekStoryboard();   break;
                case KnownElements.Separator: o = new System.Windows.Controls.Separator();   break;
                case KnownElements.SetStoryboardSpeedRatio: o = new System.Windows.Media.Animation.SetStoryboardSpeedRatio();   break;
                case KnownElements.Setter: o = new System.Windows.Setter();   break;
                case KnownElements.SingleAnimation: o = new System.Windows.Media.Animation.SingleAnimation();   break;
                case KnownElements.SingleAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.SingleAnimationUsingKeyFrames();   break;
                case KnownElements.SingleConverter: o = new System.ComponentModel.SingleConverter();   break;
                case KnownElements.SingleKeyFrameCollection: o = new System.Windows.Media.Animation.SingleKeyFrameCollection();   break;
                case KnownElements.Size: o = new System.Windows.Size();   break;
                case KnownElements.Size3D: o = new System.Windows.Media.Media3D.Size3D();   break;
                case KnownElements.Size3DConverter: o = new System.Windows.Media.Media3D.Size3DConverter();   break;
                case KnownElements.SizeAnimation: o = new System.Windows.Media.Animation.SizeAnimation();   break;
                case KnownElements.SizeAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.SizeAnimationUsingKeyFrames();   break;
                case KnownElements.SizeConverter: o = new System.Windows.SizeConverter();   break;
                case KnownElements.SizeKeyFrameCollection: o = new System.Windows.Media.Animation.SizeKeyFrameCollection();   break;
                case KnownElements.SkewTransform: o = new System.Windows.Media.SkewTransform();   break;
                case KnownElements.SkipStoryboardToFill: o = new System.Windows.Media.Animation.SkipStoryboardToFill();   break;
                case KnownElements.Slider: o = new System.Windows.Controls.Slider();   break;
                case KnownElements.SolidColorBrush: o = new System.Windows.Media.SolidColorBrush();   break;
                case KnownElements.SoundPlayerAction: o = new System.Windows.Controls.SoundPlayerAction();   break;
                case KnownElements.Span: o = new System.Windows.Documents.Span();   break;
                case KnownElements.SpecularMaterial: o = new System.Windows.Media.Media3D.SpecularMaterial();   break;
                case KnownElements.SplineByteKeyFrame: o = new System.Windows.Media.Animation.SplineByteKeyFrame();   break;
                case KnownElements.SplineColorKeyFrame: o = new System.Windows.Media.Animation.SplineColorKeyFrame();   break;
                case KnownElements.SplineDecimalKeyFrame: o = new System.Windows.Media.Animation.SplineDecimalKeyFrame();   break;
                case KnownElements.SplineDoubleKeyFrame: o = new System.Windows.Media.Animation.SplineDoubleKeyFrame();   break;
                case KnownElements.SplineInt16KeyFrame: o = new System.Windows.Media.Animation.SplineInt16KeyFrame();   break;
                case KnownElements.SplineInt32KeyFrame: o = new System.Windows.Media.Animation.SplineInt32KeyFrame();   break;
                case KnownElements.SplineInt64KeyFrame: o = new System.Windows.Media.Animation.SplineInt64KeyFrame();   break;
                case KnownElements.SplinePoint3DKeyFrame: o = new System.Windows.Media.Animation.SplinePoint3DKeyFrame();   break;
                case KnownElements.SplinePointKeyFrame: o = new System.Windows.Media.Animation.SplinePointKeyFrame();   break;
                case KnownElements.SplineQuaternionKeyFrame: o = new System.Windows.Media.Animation.SplineQuaternionKeyFrame();   break;
                case KnownElements.SplineRectKeyFrame: o = new System.Windows.Media.Animation.SplineRectKeyFrame();   break;
                case KnownElements.SplineRotation3DKeyFrame: o = new System.Windows.Media.Animation.SplineRotation3DKeyFrame();   break;
                case KnownElements.SplineSingleKeyFrame: o = new System.Windows.Media.Animation.SplineSingleKeyFrame();   break;
                case KnownElements.SplineSizeKeyFrame: o = new System.Windows.Media.Animation.SplineSizeKeyFrame();   break;
                case KnownElements.SplineThicknessKeyFrame: o = new System.Windows.Media.Animation.SplineThicknessKeyFrame();   break;
                case KnownElements.SplineVector3DKeyFrame: o = new System.Windows.Media.Animation.SplineVector3DKeyFrame();   break;
                case KnownElements.SplineVectorKeyFrame: o = new System.Windows.Media.Animation.SplineVectorKeyFrame();   break;
                case KnownElements.SpotLight: o = new System.Windows.Media.Media3D.SpotLight();   break;
                case KnownElements.StackPanel: o = new System.Windows.Controls.StackPanel();   break;
                case KnownElements.StaticExtension: o = new System.Windows.Markup.StaticExtension();   break;
                case KnownElements.StaticResourceExtension: o = new System.Windows.StaticResourceExtension();   break;
                case KnownElements.StatusBar: o = new System.Windows.Controls.Primitives.StatusBar();   break;
                case KnownElements.StatusBarItem: o = new System.Windows.Controls.Primitives.StatusBarItem();   break;
                case KnownElements.StopStoryboard: o = new System.Windows.Media.Animation.StopStoryboard();   break;
                case KnownElements.Storyboard: o = new System.Windows.Media.Animation.Storyboard();   break;
                case KnownElements.StreamGeometry: o = new System.Windows.Media.StreamGeometry();   break;
                case KnownElements.StreamResourceInfo: o = new System.Windows.Resources.StreamResourceInfo();   break;
                case KnownElements.StringAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.StringAnimationUsingKeyFrames();   break;
                case KnownElements.StringConverter: o = new System.ComponentModel.StringConverter();   break;
                case KnownElements.StringKeyFrameCollection: o = new System.Windows.Media.Animation.StringKeyFrameCollection();   break;
                case KnownElements.StrokeCollection: o = new System.Windows.Ink.StrokeCollection();   break;
                case KnownElements.StrokeCollectionConverter: o = new System.Windows.StrokeCollectionConverter();   break;
                case KnownElements.Style: o = new System.Windows.Style();   break;
                case KnownElements.TabControl: o = new System.Windows.Controls.TabControl();   break;
                case KnownElements.TabItem: o = new System.Windows.Controls.TabItem();   break;
                case KnownElements.TabPanel: o = new System.Windows.Controls.Primitives.TabPanel();   break;
                case KnownElements.Table: o = new System.Windows.Documents.Table();   break;
                case KnownElements.TableCell: o = new System.Windows.Documents.TableCell();   break;
                case KnownElements.TableColumn: o = new System.Windows.Documents.TableColumn();   break;
                case KnownElements.TableRow: o = new System.Windows.Documents.TableRow();   break;
                case KnownElements.TableRowGroup: o = new System.Windows.Documents.TableRowGroup();   break;
                case KnownElements.TemplateBindingExpressionConverter: o = new System.Windows.TemplateBindingExpressionConverter();   break;
                case KnownElements.TemplateBindingExtension: o = new System.Windows.TemplateBindingExtension();   break;
                case KnownElements.TemplateBindingExtensionConverter: o = new System.Windows.TemplateBindingExtensionConverter();   break;
                case KnownElements.TemplateKeyConverter: o = new System.Windows.Markup.TemplateKeyConverter();   break;
                case KnownElements.TextBlock: o = new System.Windows.Controls.TextBlock();   break;
                case KnownElements.TextBox: o = new System.Windows.Controls.TextBox();   break;
                case KnownElements.TextDecoration: o = new System.Windows.TextDecoration();   break;
                case KnownElements.TextDecorationCollection: o = new System.Windows.TextDecorationCollection();   break;
                case KnownElements.TextDecorationCollectionConverter: o = new System.Windows.TextDecorationCollectionConverter();   break;
                case KnownElements.TextEffect: o = new System.Windows.Media.TextEffect();   break;
                case KnownElements.TextEffectCollection: o = new System.Windows.Media.TextEffectCollection();   break;
                case KnownElements.ThemeDictionaryExtension: o = new System.Windows.ThemeDictionaryExtension();   break;
                case KnownElements.Thickness: o = new System.Windows.Thickness();   break;
                case KnownElements.ThicknessAnimation: o = new System.Windows.Media.Animation.ThicknessAnimation();   break;
                case KnownElements.ThicknessAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames();   break;
                case KnownElements.ThicknessConverter: o = new System.Windows.ThicknessConverter();   break;
                case KnownElements.ThicknessKeyFrameCollection: o = new System.Windows.Media.Animation.ThicknessKeyFrameCollection();   break;
                case KnownElements.Thumb: o = new System.Windows.Controls.Primitives.Thumb();   break;
                case KnownElements.TickBar: o = new System.Windows.Controls.Primitives.TickBar();   break;
                case KnownElements.TiffBitmapEncoder: o = new System.Windows.Media.Imaging.TiffBitmapEncoder();   break;
                case KnownElements.TimeSpanConverter: o = new System.ComponentModel.TimeSpanConverter();   break;
                case KnownElements.TimelineCollection: o = new System.Windows.Media.Animation.TimelineCollection();   break;
                case KnownElements.ToggleButton: o = new System.Windows.Controls.Primitives.ToggleButton();   break;
                case KnownElements.ToolBar: o = new System.Windows.Controls.ToolBar();   break;
                case KnownElements.ToolBarOverflowPanel: o = new System.Windows.Controls.Primitives.ToolBarOverflowPanel();   break;
                case KnownElements.ToolBarPanel: o = new System.Windows.Controls.Primitives.ToolBarPanel();   break;
                case KnownElements.ToolBarTray: o = new System.Windows.Controls.ToolBarTray();   break;
                case KnownElements.ToolTip: o = new System.Windows.Controls.ToolTip();   break;
                case KnownElements.Track: o = new System.Windows.Controls.Primitives.Track();   break;
                case KnownElements.Transform3DCollection: o = new System.Windows.Media.Media3D.Transform3DCollection();   break;
                case KnownElements.Transform3DGroup: o = new System.Windows.Media.Media3D.Transform3DGroup();   break;
                case KnownElements.TransformCollection: o = new System.Windows.Media.TransformCollection();   break;
                case KnownElements.TransformConverter: o = new System.Windows.Media.TransformConverter();   break;
                case KnownElements.TransformGroup: o = new System.Windows.Media.TransformGroup();   break;
                case KnownElements.TransformedBitmap: o = new System.Windows.Media.Imaging.TransformedBitmap();   break;
                case KnownElements.TranslateTransform: o = new System.Windows.Media.TranslateTransform();   break;
                case KnownElements.TranslateTransform3D: o = new System.Windows.Media.Media3D.TranslateTransform3D();   break;
                case KnownElements.TreeView: o = new System.Windows.Controls.TreeView();   break;
                case KnownElements.TreeViewItem: o = new System.Windows.Controls.TreeViewItem();   break;
                case KnownElements.Trigger: o = new System.Windows.Trigger();   break;
                case KnownElements.TypeExtension: o = new System.Windows.Markup.TypeExtension();   break;
                case KnownElements.TypeTypeConverter: o = new System.Windows.Markup.TypeTypeConverter();   break;
                case KnownElements.UIElement: o = new System.Windows.UIElement();   break;
                case KnownElements.UInt16Converter: o = new System.ComponentModel.UInt16Converter();   break;
                case KnownElements.UInt32Converter: o = new System.ComponentModel.UInt32Converter();   break;
                case KnownElements.UInt64Converter: o = new System.ComponentModel.UInt64Converter();   break;
                case KnownElements.UShortIListConverter: o = new System.Windows.Media.Converters.UShortIListConverter();   break;
                case KnownElements.Underline: o = new System.Windows.Documents.Underline();   break;
                case KnownElements.UniformGrid: o = new System.Windows.Controls.Primitives.UniformGrid();   break;
                case KnownElements.UriTypeConverter: o = new System.UriTypeConverter();   break;
                case KnownElements.UserControl: o = new System.Windows.Controls.UserControl();   break;
                case KnownElements.Vector: o = new System.Windows.Vector();   break;
                case KnownElements.Vector3D: o = new System.Windows.Media.Media3D.Vector3D();   break;
                case KnownElements.Vector3DAnimation: o = new System.Windows.Media.Animation.Vector3DAnimation();   break;
                case KnownElements.Vector3DAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.Vector3DAnimationUsingKeyFrames();   break;
                case KnownElements.Vector3DCollection: o = new System.Windows.Media.Media3D.Vector3DCollection();   break;
                case KnownElements.Vector3DCollectionConverter: o = new System.Windows.Media.Media3D.Vector3DCollectionConverter();   break;
                case KnownElements.Vector3DConverter: o = new System.Windows.Media.Media3D.Vector3DConverter();   break;
                case KnownElements.Vector3DKeyFrameCollection: o = new System.Windows.Media.Animation.Vector3DKeyFrameCollection();   break;
                case KnownElements.VectorAnimation: o = new System.Windows.Media.Animation.VectorAnimation();   break;
                case KnownElements.VectorAnimationUsingKeyFrames: o = new System.Windows.Media.Animation.VectorAnimationUsingKeyFrames();   break;
                case KnownElements.VectorCollection: o = new System.Windows.Media.VectorCollection();   break;
                case KnownElements.VectorCollectionConverter: o = new System.Windows.Media.VectorCollectionConverter();   break;
                case KnownElements.VectorConverter: o = new System.Windows.VectorConverter();   break;
                case KnownElements.VectorKeyFrameCollection: o = new System.Windows.Media.Animation.VectorKeyFrameCollection();   break;
                case KnownElements.VideoDrawing: o = new System.Windows.Media.VideoDrawing();   break;
                case KnownElements.Viewbox: o = new System.Windows.Controls.Viewbox();   break;
                case KnownElements.Viewport3D: o = new System.Windows.Controls.Viewport3D();   break;
                case KnownElements.Viewport3DVisual: o = new System.Windows.Media.Media3D.Viewport3DVisual();   break;
                case KnownElements.VirtualizingStackPanel: o = new System.Windows.Controls.VirtualizingStackPanel();   break;
                case KnownElements.VisualBrush: o = new System.Windows.Media.VisualBrush();   break;
                case KnownElements.Window: o = new System.Windows.Window();   break;
                case KnownElements.WmpBitmapEncoder: o = new System.Windows.Media.Imaging.WmpBitmapEncoder();   break;
                case KnownElements.WrapPanel: o = new System.Windows.Controls.WrapPanel();   break;
                case KnownElements.XamlBrushSerializer: o = new System.Windows.Markup.XamlBrushSerializer();   break;
                case KnownElements.XamlInt32CollectionSerializer: o = new System.Windows.Markup.XamlInt32CollectionSerializer();   break;
                case KnownElements.XamlPathDataSerializer: o = new System.Windows.Markup.XamlPathDataSerializer();   break;
                case KnownElements.XamlPoint3DCollectionSerializer: o = new System.Windows.Markup.XamlPoint3DCollectionSerializer();   break;
                case KnownElements.XamlPointCollectionSerializer: o = new System.Windows.Markup.XamlPointCollectionSerializer();   break;
                case KnownElements.XamlStyleSerializer: o = new System.Windows.Markup.XamlStyleSerializer();   break;
                case KnownElements.XamlTemplateSerializer: o = new System.Windows.Markup.XamlTemplateSerializer();   break;
                case KnownElements.XamlVector3DCollectionSerializer: o = new System.Windows.Markup.XamlVector3DCollectionSerializer();   break;
                case KnownElements.XmlDataProvider: o = new System.Windows.Data.XmlDataProvider();   break;
                case KnownElements.XmlLanguageConverter: o = new System.Windows.Markup.XmlLanguageConverter();   break;
                case KnownElements.XmlNamespaceMapping: o = new System.Windows.Data.XmlNamespaceMapping();   break;
                case KnownElements.ZoomPercentageConverter: o = new System.Windows.Documents.ZoomPercentageConverter();   break;
            }
            return o;
        }

        internal static DependencyProperty GetKnownDependencyPropertyFromId(KnownProperties knownProperty)
        {
            switch (knownProperty)
            {
                case KnownProperties.AccessText_Text:
                    return System.Windows.Controls.AccessText.TextProperty;
                case KnownProperties.BeginStoryboard_Storyboard:
                    return System.Windows.Media.Animation.BeginStoryboard.StoryboardProperty;
                case KnownProperties.BitmapEffectGroup_Children:
                    return System.Windows.Media.Effects.BitmapEffectGroup.ChildrenProperty;
                case KnownProperties.Border_Background:
                    return System.Windows.Controls.Border.BackgroundProperty;
                case KnownProperties.Border_BorderBrush:
                    return System.Windows.Controls.Border.BorderBrushProperty;
                case KnownProperties.Border_BorderThickness:
                    return System.Windows.Controls.Border.BorderThicknessProperty;
                case KnownProperties.ButtonBase_Command:
                    return System.Windows.Controls.Primitives.ButtonBase.CommandProperty;
                case KnownProperties.ButtonBase_CommandParameter:
                    return System.Windows.Controls.Primitives.ButtonBase.CommandParameterProperty;
                case KnownProperties.ButtonBase_CommandTarget:
                    return System.Windows.Controls.Primitives.ButtonBase.CommandTargetProperty;
                case KnownProperties.ButtonBase_IsPressed:
                    return System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty;
                case KnownProperties.ColumnDefinition_MaxWidth:
                    return System.Windows.Controls.ColumnDefinition.MaxWidthProperty;
                case KnownProperties.ColumnDefinition_MinWidth:
                    return System.Windows.Controls.ColumnDefinition.MinWidthProperty;
                case KnownProperties.ColumnDefinition_Width:
                    return System.Windows.Controls.ColumnDefinition.WidthProperty;
                case KnownProperties.ContentControl_Content:
                    return System.Windows.Controls.ContentControl.ContentProperty;
                case KnownProperties.ContentControl_ContentTemplate:
                    return System.Windows.Controls.ContentControl.ContentTemplateProperty;
                case KnownProperties.ContentControl_ContentTemplateSelector:
                    return System.Windows.Controls.ContentControl.ContentTemplateSelectorProperty;
                case KnownProperties.ContentControl_HasContent:
                    return System.Windows.Controls.ContentControl.HasContentProperty;
                case KnownProperties.ContentElement_Focusable:
                    return System.Windows.ContentElement.FocusableProperty;
                case KnownProperties.ContentPresenter_Content:
                    return System.Windows.Controls.ContentPresenter.ContentProperty;
                case KnownProperties.ContentPresenter_ContentSource:
                    return System.Windows.Controls.ContentPresenter.ContentSourceProperty;
                case KnownProperties.ContentPresenter_ContentTemplate:
                    return System.Windows.Controls.ContentPresenter.ContentTemplateProperty;
                case KnownProperties.ContentPresenter_ContentTemplateSelector:
                    return System.Windows.Controls.ContentPresenter.ContentTemplateSelectorProperty;
                case KnownProperties.ContentPresenter_RecognizesAccessKey:
                    return System.Windows.Controls.ContentPresenter.RecognizesAccessKeyProperty;
                case KnownProperties.Control_Background:
                    return System.Windows.Controls.Control.BackgroundProperty;
                case KnownProperties.Control_BorderBrush:
                    return System.Windows.Controls.Control.BorderBrushProperty;
                case KnownProperties.Control_BorderThickness:
                    return System.Windows.Controls.Control.BorderThicknessProperty;
                case KnownProperties.Control_FontFamily:
                    return System.Windows.Controls.Control.FontFamilyProperty;
                case KnownProperties.Control_FontSize:
                    return System.Windows.Controls.Control.FontSizeProperty;
                case KnownProperties.Control_FontStretch:
                    return System.Windows.Controls.Control.FontStretchProperty;
                case KnownProperties.Control_FontStyle:
                    return System.Windows.Controls.Control.FontStyleProperty;
                case KnownProperties.Control_FontWeight:
                    return System.Windows.Controls.Control.FontWeightProperty;
                case KnownProperties.Control_Foreground:
                    return System.Windows.Controls.Control.ForegroundProperty;
                case KnownProperties.Control_HorizontalContentAlignment:
                    return System.Windows.Controls.Control.HorizontalContentAlignmentProperty;
                case KnownProperties.Control_IsTabStop:
                    return System.Windows.Controls.Control.IsTabStopProperty;
                case KnownProperties.Control_Padding:
                    return System.Windows.Controls.Control.PaddingProperty;
                case KnownProperties.Control_TabIndex:
                    return System.Windows.Controls.Control.TabIndexProperty;
                case KnownProperties.Control_Template:
                    return System.Windows.Controls.Control.TemplateProperty;
                case KnownProperties.Control_VerticalContentAlignment:
                    return System.Windows.Controls.Control.VerticalContentAlignmentProperty;
                case KnownProperties.DockPanel_Dock:
                    return System.Windows.Controls.DockPanel.DockProperty;
                case KnownProperties.DockPanel_LastChildFill:
                    return System.Windows.Controls.DockPanel.LastChildFillProperty;
                case KnownProperties.DocumentViewerBase_Document:
                    return System.Windows.Controls.Primitives.DocumentViewerBase.DocumentProperty;
                case KnownProperties.DrawingGroup_Children:
                    return System.Windows.Media.DrawingGroup.ChildrenProperty;
                case KnownProperties.FlowDocumentReader_Document:
                    return System.Windows.Controls.FlowDocumentReader.DocumentProperty;
                case KnownProperties.FlowDocumentScrollViewer_Document:
                    return System.Windows.Controls.FlowDocumentScrollViewer.DocumentProperty;
                case KnownProperties.FrameworkContentElement_Style:
                    return System.Windows.FrameworkContentElement.StyleProperty;
                case KnownProperties.FrameworkElement_FlowDirection:
                    return System.Windows.FrameworkElement.FlowDirectionProperty;
                case KnownProperties.FrameworkElement_Height:
                    return System.Windows.FrameworkElement.HeightProperty;
                case KnownProperties.FrameworkElement_HorizontalAlignment:
                    return System.Windows.FrameworkElement.HorizontalAlignmentProperty;
                case KnownProperties.FrameworkElement_Margin:
                    return System.Windows.FrameworkElement.MarginProperty;
                case KnownProperties.FrameworkElement_MaxHeight:
                    return System.Windows.FrameworkElement.MaxHeightProperty;
                case KnownProperties.FrameworkElement_MaxWidth:
                    return System.Windows.FrameworkElement.MaxWidthProperty;
                case KnownProperties.FrameworkElement_MinHeight:
                    return System.Windows.FrameworkElement.MinHeightProperty;
                case KnownProperties.FrameworkElement_MinWidth:
                    return System.Windows.FrameworkElement.MinWidthProperty;
                case KnownProperties.FrameworkElement_Name:
                    return System.Windows.FrameworkElement.NameProperty;
                case KnownProperties.FrameworkElement_Style:
                    return System.Windows.FrameworkElement.StyleProperty;
                case KnownProperties.FrameworkElement_VerticalAlignment:
                    return System.Windows.FrameworkElement.VerticalAlignmentProperty;
                case KnownProperties.FrameworkElement_Width:
                    return System.Windows.FrameworkElement.WidthProperty;
                case KnownProperties.GeneralTransformGroup_Children:
                    return System.Windows.Media.GeneralTransformGroup.ChildrenProperty;
                case KnownProperties.GeometryGroup_Children:
                    return System.Windows.Media.GeometryGroup.ChildrenProperty;
                case KnownProperties.GradientBrush_GradientStops:
                    return System.Windows.Media.GradientBrush.GradientStopsProperty;
                case KnownProperties.Grid_Column:
                    return System.Windows.Controls.Grid.ColumnProperty;
                case KnownProperties.Grid_ColumnSpan:
                    return System.Windows.Controls.Grid.ColumnSpanProperty;
                case KnownProperties.Grid_Row:
                    return System.Windows.Controls.Grid.RowProperty;
                case KnownProperties.Grid_RowSpan:
                    return System.Windows.Controls.Grid.RowSpanProperty;
                case KnownProperties.GridViewColumn_Header:
                    return System.Windows.Controls.GridViewColumn.HeaderProperty;
                case KnownProperties.HeaderedContentControl_HasHeader:
                    return System.Windows.Controls.HeaderedContentControl.HasHeaderProperty;
                case KnownProperties.HeaderedContentControl_Header:
                    return System.Windows.Controls.HeaderedContentControl.HeaderProperty;
                case KnownProperties.HeaderedContentControl_HeaderTemplate:
                    return System.Windows.Controls.HeaderedContentControl.HeaderTemplateProperty;
                case KnownProperties.HeaderedContentControl_HeaderTemplateSelector:
                    return System.Windows.Controls.HeaderedContentControl.HeaderTemplateSelectorProperty;
                case KnownProperties.HeaderedItemsControl_HasHeader:
                    return System.Windows.Controls.HeaderedItemsControl.HasHeaderProperty;
                case KnownProperties.HeaderedItemsControl_Header:
                    return System.Windows.Controls.HeaderedItemsControl.HeaderProperty;
                case KnownProperties.HeaderedItemsControl_HeaderTemplate:
                    return System.Windows.Controls.HeaderedItemsControl.HeaderTemplateProperty;
                case KnownProperties.HeaderedItemsControl_HeaderTemplateSelector:
                    return System.Windows.Controls.HeaderedItemsControl.HeaderTemplateSelectorProperty;
                case KnownProperties.Hyperlink_NavigateUri:
                    return System.Windows.Documents.Hyperlink.NavigateUriProperty;
                case KnownProperties.Image_Source:
                    return System.Windows.Controls.Image.SourceProperty;
                case KnownProperties.Image_Stretch:
                    return System.Windows.Controls.Image.StretchProperty;
                case KnownProperties.ItemsControl_ItemContainerStyle:
                    return System.Windows.Controls.ItemsControl.ItemContainerStyleProperty;
                case KnownProperties.ItemsControl_ItemContainerStyleSelector:
                    return System.Windows.Controls.ItemsControl.ItemContainerStyleSelectorProperty;
                case KnownProperties.ItemsControl_ItemTemplate:
                    return System.Windows.Controls.ItemsControl.ItemTemplateProperty;
                case KnownProperties.ItemsControl_ItemTemplateSelector:
                    return System.Windows.Controls.ItemsControl.ItemTemplateSelectorProperty;
                case KnownProperties.ItemsControl_ItemsPanel:
                    return System.Windows.Controls.ItemsControl.ItemsPanelProperty;
                case KnownProperties.ItemsControl_ItemsSource:
                    return System.Windows.Controls.ItemsControl.ItemsSourceProperty;
                case KnownProperties.MaterialGroup_Children:
                    return System.Windows.Media.Media3D.MaterialGroup.ChildrenProperty;
                case KnownProperties.Model3DGroup_Children:
                    return System.Windows.Media.Media3D.Model3DGroup.ChildrenProperty;
                case KnownProperties.Page_Content:
                    return System.Windows.Controls.Page.ContentProperty;
                case KnownProperties.Panel_Background:
                    return System.Windows.Controls.Panel.BackgroundProperty;
                case KnownProperties.Path_Data:
                    return System.Windows.Shapes.Path.DataProperty;
                case KnownProperties.PathFigure_Segments:
                    return System.Windows.Media.PathFigure.SegmentsProperty;
                case KnownProperties.PathGeometry_Figures:
                    return System.Windows.Media.PathGeometry.FiguresProperty;
                case KnownProperties.Popup_Child:
                    return System.Windows.Controls.Primitives.Popup.ChildProperty;
                case KnownProperties.Popup_IsOpen:
                    return System.Windows.Controls.Primitives.Popup.IsOpenProperty;
                case KnownProperties.Popup_Placement:
                    return System.Windows.Controls.Primitives.Popup.PlacementProperty;
                case KnownProperties.Popup_PopupAnimation:
                    return System.Windows.Controls.Primitives.Popup.PopupAnimationProperty;
                case KnownProperties.RowDefinition_Height:
                    return System.Windows.Controls.RowDefinition.HeightProperty;
                case KnownProperties.RowDefinition_MaxHeight:
                    return System.Windows.Controls.RowDefinition.MaxHeightProperty;
                case KnownProperties.RowDefinition_MinHeight:
                    return System.Windows.Controls.RowDefinition.MinHeightProperty;
                case KnownProperties.Run_Text:
                    return System.Windows.Documents.Run.TextProperty;
                case KnownProperties.ScrollViewer_CanContentScroll:
                    return System.Windows.Controls.ScrollViewer.CanContentScrollProperty;
                case KnownProperties.ScrollViewer_HorizontalScrollBarVisibility:
                    return System.Windows.Controls.ScrollViewer.HorizontalScrollBarVisibilityProperty;
                case KnownProperties.ScrollViewer_VerticalScrollBarVisibility:
                    return System.Windows.Controls.ScrollViewer.VerticalScrollBarVisibilityProperty;
                case KnownProperties.Shape_Fill:
                    return System.Windows.Shapes.Shape.FillProperty;
                case KnownProperties.Shape_Stroke:
                    return System.Windows.Shapes.Shape.StrokeProperty;
                case KnownProperties.Shape_StrokeThickness:
                    return System.Windows.Shapes.Shape.StrokeThicknessProperty;
                case KnownProperties.TextBlock_Background:
                    return System.Windows.Controls.TextBlock.BackgroundProperty;
                case KnownProperties.TextBlock_FontFamily:
                    return System.Windows.Controls.TextBlock.FontFamilyProperty;
                case KnownProperties.TextBlock_FontSize:
                    return System.Windows.Controls.TextBlock.FontSizeProperty;
                case KnownProperties.TextBlock_FontStretch:
                    return System.Windows.Controls.TextBlock.FontStretchProperty;
                case KnownProperties.TextBlock_FontStyle:
                    return System.Windows.Controls.TextBlock.FontStyleProperty;
                case KnownProperties.TextBlock_FontWeight:
                    return System.Windows.Controls.TextBlock.FontWeightProperty;
                case KnownProperties.TextBlock_Foreground:
                    return System.Windows.Controls.TextBlock.ForegroundProperty;
                case KnownProperties.TextBlock_Text:
                    return System.Windows.Controls.TextBlock.TextProperty;
                case KnownProperties.TextBlock_TextDecorations:
                    return System.Windows.Controls.TextBlock.TextDecorationsProperty;
                case KnownProperties.TextBlock_TextTrimming:
                    return System.Windows.Controls.TextBlock.TextTrimmingProperty;
                case KnownProperties.TextBlock_TextWrapping:
                    return System.Windows.Controls.TextBlock.TextWrappingProperty;
                case KnownProperties.TextBox_Text:
                    return System.Windows.Controls.TextBox.TextProperty;
                case KnownProperties.TextElement_Background:
                    return System.Windows.Documents.TextElement.BackgroundProperty;
                case KnownProperties.TextElement_FontFamily:
                    return System.Windows.Documents.TextElement.FontFamilyProperty;
                case KnownProperties.TextElement_FontSize:
                    return System.Windows.Documents.TextElement.FontSizeProperty;
                case KnownProperties.TextElement_FontStretch:
                    return System.Windows.Documents.TextElement.FontStretchProperty;
                case KnownProperties.TextElement_FontStyle:
                    return System.Windows.Documents.TextElement.FontStyleProperty;
                case KnownProperties.TextElement_FontWeight:
                    return System.Windows.Documents.TextElement.FontWeightProperty;
                case KnownProperties.TextElement_Foreground:
                    return System.Windows.Documents.TextElement.ForegroundProperty;
                case KnownProperties.TimelineGroup_Children:
                    return System.Windows.Media.Animation.TimelineGroup.ChildrenProperty;
                case KnownProperties.Track_IsDirectionReversed:
                    return System.Windows.Controls.Primitives.Track.IsDirectionReversedProperty;
                case KnownProperties.Track_Maximum:
                    return System.Windows.Controls.Primitives.Track.MaximumProperty;
                case KnownProperties.Track_Minimum:
                    return System.Windows.Controls.Primitives.Track.MinimumProperty;
                case KnownProperties.Track_Orientation:
                    return System.Windows.Controls.Primitives.Track.OrientationProperty;
                case KnownProperties.Track_Value:
                    return System.Windows.Controls.Primitives.Track.ValueProperty;
                case KnownProperties.Track_ViewportSize:
                    return System.Windows.Controls.Primitives.Track.ViewportSizeProperty;
                case KnownProperties.Transform3DGroup_Children:
                    return System.Windows.Media.Media3D.Transform3DGroup.ChildrenProperty;
                case KnownProperties.TransformGroup_Children:
                    return System.Windows.Media.TransformGroup.ChildrenProperty;
                case KnownProperties.UIElement_ClipToBounds:
                    return System.Windows.UIElement.ClipToBoundsProperty;
                case KnownProperties.UIElement_Focusable:
                    return System.Windows.UIElement.FocusableProperty;
                case KnownProperties.UIElement_IsEnabled:
                    return System.Windows.UIElement.IsEnabledProperty;
                case KnownProperties.UIElement_RenderTransform:
                    return System.Windows.UIElement.RenderTransformProperty;
                case KnownProperties.UIElement_Visibility:
                    return System.Windows.UIElement.VisibilityProperty;
                case KnownProperties.Viewport3D_Children:
                    return System.Windows.Controls.Viewport3D.ChildrenProperty;
            }
            return null;
        }

        internal static KnownElements GetKnownElementFromKnownCommonProperty(KnownProperties knownProperty)
        {
            switch (knownProperty)
            {
                case KnownProperties.AccessText_Text:
                    return KnownElements.AccessText;
                case KnownProperties.AdornedElementPlaceholder_Child:
                    return KnownElements.AdornedElementPlaceholder;
                case KnownProperties.AdornerDecorator_Child:
                    return KnownElements.AdornerDecorator;
                case KnownProperties.AnchoredBlock_Blocks:
                    return KnownElements.AnchoredBlock;
                case KnownProperties.ArrayExtension_Items:
                    return KnownElements.ArrayExtension;
                case KnownProperties.BeginStoryboard_Storyboard:
                    return KnownElements.BeginStoryboard;
                case KnownProperties.BitmapEffectGroup_Children:
                    return KnownElements.BitmapEffectGroup;
                case KnownProperties.BlockUIContainer_Child:
                    return KnownElements.BlockUIContainer;
                case KnownProperties.Bold_Inlines:
                    return KnownElements.Bold;
                case KnownProperties.BooleanAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.BooleanAnimationUsingKeyFrames;
                case KnownProperties.Border_Background:
                case KnownProperties.Border_BorderBrush:
                case KnownProperties.Border_BorderThickness:
                case KnownProperties.Border_Child:
                    return KnownElements.Border;
                case KnownProperties.BulletDecorator_Child:
                    return KnownElements.BulletDecorator;
                case KnownProperties.Button_Content:
                    return KnownElements.Button;
                case KnownProperties.ButtonBase_Command:
                case KnownProperties.ButtonBase_CommandParameter:
                case KnownProperties.ButtonBase_CommandTarget:
                case KnownProperties.ButtonBase_Content:
                case KnownProperties.ButtonBase_IsPressed:
                    return KnownElements.ButtonBase;
                case KnownProperties.ByteAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.ByteAnimationUsingKeyFrames;
                case KnownProperties.Canvas_Children:
                    return KnownElements.Canvas;
                case KnownProperties.CharAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.CharAnimationUsingKeyFrames;
                case KnownProperties.CheckBox_Content:
                    return KnownElements.CheckBox;
                case KnownProperties.ColorAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.ColorAnimationUsingKeyFrames;
                case KnownProperties.ColumnDefinition_MaxWidth:
                case KnownProperties.ColumnDefinition_MinWidth:
                case KnownProperties.ColumnDefinition_Width:
                    return KnownElements.ColumnDefinition;
                case KnownProperties.ComboBox_Items:
                    return KnownElements.ComboBox;
                case KnownProperties.ComboBoxItem_Content:
                    return KnownElements.ComboBoxItem;
                case KnownProperties.ContentControl_Content:
                case KnownProperties.ContentControl_ContentTemplate:
                case KnownProperties.ContentControl_ContentTemplateSelector:
                case KnownProperties.ContentControl_HasContent:
                    return KnownElements.ContentControl;
                case KnownProperties.ContentElement_Focusable:
                    return KnownElements.ContentElement;
                case KnownProperties.ContentPresenter_Content:
                case KnownProperties.ContentPresenter_ContentSource:
                case KnownProperties.ContentPresenter_ContentTemplate:
                case KnownProperties.ContentPresenter_ContentTemplateSelector:
                case KnownProperties.ContentPresenter_RecognizesAccessKey:
                    return KnownElements.ContentPresenter;
                case KnownProperties.ContextMenu_Items:
                    return KnownElements.ContextMenu;
                case KnownProperties.Control_Background:
                case KnownProperties.Control_BorderBrush:
                case KnownProperties.Control_BorderThickness:
                case KnownProperties.Control_FontFamily:
                case KnownProperties.Control_FontSize:
                case KnownProperties.Control_FontStretch:
                case KnownProperties.Control_FontStyle:
                case KnownProperties.Control_FontWeight:
                case KnownProperties.Control_Foreground:
                case KnownProperties.Control_HorizontalContentAlignment:
                case KnownProperties.Control_IsTabStop:
                case KnownProperties.Control_Padding:
                case KnownProperties.Control_TabIndex:
                case KnownProperties.Control_Template:
                case KnownProperties.Control_VerticalContentAlignment:
                    return KnownElements.Control;
                case KnownProperties.ControlTemplate_VisualTree:
                    return KnownElements.ControlTemplate;
                case KnownProperties.DataTemplate_VisualTree:
                    return KnownElements.DataTemplate;
                case KnownProperties.DataTrigger_Setters:
                    return KnownElements.DataTrigger;
                case KnownProperties.DecimalAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.DecimalAnimationUsingKeyFrames;
                case KnownProperties.Decorator_Child:
                    return KnownElements.Decorator;
                case KnownProperties.DockPanel_Children:
                case KnownProperties.DockPanel_Dock:
                case KnownProperties.DockPanel_LastChildFill:
                    return KnownElements.DockPanel;
                case KnownProperties.DocumentViewer_Document:
                    return KnownElements.DocumentViewer;
                case KnownProperties.DocumentViewerBase_Document:
                    return KnownElements.DocumentViewerBase;
                case KnownProperties.DoubleAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.DoubleAnimationUsingKeyFrames;
                case KnownProperties.DrawingGroup_Children:
                    return KnownElements.DrawingGroup;
                case KnownProperties.EventTrigger_Actions:
                    return KnownElements.EventTrigger;
                case KnownProperties.Expander_Content:
                    return KnownElements.Expander;
                case KnownProperties.Figure_Blocks:
                    return KnownElements.Figure;
                case KnownProperties.FixedDocument_Pages:
                    return KnownElements.FixedDocument;
                case KnownProperties.FixedDocumentSequence_References:
                    return KnownElements.FixedDocumentSequence;
                case KnownProperties.FixedPage_Children:
                    return KnownElements.FixedPage;
                case KnownProperties.Floater_Blocks:
                    return KnownElements.Floater;
                case KnownProperties.FlowDocument_Blocks:
                    return KnownElements.FlowDocument;
                case KnownProperties.FlowDocumentPageViewer_Document:
                    return KnownElements.FlowDocumentPageViewer;
                case KnownProperties.FlowDocumentReader_Document:
                    return KnownElements.FlowDocumentReader;
                case KnownProperties.FlowDocumentScrollViewer_Document:
                    return KnownElements.FlowDocumentScrollViewer;
                case KnownProperties.FrameworkContentElement_Style:
                    return KnownElements.FrameworkContentElement;
                case KnownProperties.FrameworkElement_FlowDirection:
                case KnownProperties.FrameworkElement_Height:
                case KnownProperties.FrameworkElement_HorizontalAlignment:
                case KnownProperties.FrameworkElement_Margin:
                case KnownProperties.FrameworkElement_MaxHeight:
                case KnownProperties.FrameworkElement_MaxWidth:
                case KnownProperties.FrameworkElement_MinHeight:
                case KnownProperties.FrameworkElement_MinWidth:
                case KnownProperties.FrameworkElement_Name:
                case KnownProperties.FrameworkElement_Style:
                case KnownProperties.FrameworkElement_VerticalAlignment:
                case KnownProperties.FrameworkElement_Width:
                    return KnownElements.FrameworkElement;
                case KnownProperties.FrameworkTemplate_VisualTree:
                    return KnownElements.FrameworkTemplate;
                case KnownProperties.GeneralTransformGroup_Children:
                    return KnownElements.GeneralTransformGroup;
                case KnownProperties.GeometryGroup_Children:
                    return KnownElements.GeometryGroup;
                case KnownProperties.GradientBrush_GradientStops:
                    return KnownElements.GradientBrush;
                case KnownProperties.Grid_Children:
                case KnownProperties.Grid_Column:
                case KnownProperties.Grid_ColumnSpan:
                case KnownProperties.Grid_Row:
                case KnownProperties.Grid_RowSpan:
                    return KnownElements.Grid;
                case KnownProperties.GridView_Columns:
                    return KnownElements.GridView;
                case KnownProperties.GridViewColumn_Header:
                    return KnownElements.GridViewColumn;
                case KnownProperties.GridViewColumnHeader_Content:
                    return KnownElements.GridViewColumnHeader;
                case KnownProperties.GroupBox_Content:
                    return KnownElements.GroupBox;
                case KnownProperties.GroupItem_Content:
                    return KnownElements.GroupItem;
                case KnownProperties.HeaderedContentControl_Content:
                case KnownProperties.HeaderedContentControl_HasHeader:
                case KnownProperties.HeaderedContentControl_Header:
                case KnownProperties.HeaderedContentControl_HeaderTemplate:
                case KnownProperties.HeaderedContentControl_HeaderTemplateSelector:
                    return KnownElements.HeaderedContentControl;
                case KnownProperties.HeaderedItemsControl_HasHeader:
                case KnownProperties.HeaderedItemsControl_Header:
                case KnownProperties.HeaderedItemsControl_HeaderTemplate:
                case KnownProperties.HeaderedItemsControl_HeaderTemplateSelector:
                case KnownProperties.HeaderedItemsControl_Items:
                    return KnownElements.HeaderedItemsControl;
                case KnownProperties.HierarchicalDataTemplate_VisualTree:
                    return KnownElements.HierarchicalDataTemplate;
                case KnownProperties.Hyperlink_Inlines:
                case KnownProperties.Hyperlink_NavigateUri:
                    return KnownElements.Hyperlink;
                case KnownProperties.Image_Source:
                case KnownProperties.Image_Stretch:
                    return KnownElements.Image;
                case KnownProperties.InkCanvas_Children:
                    return KnownElements.InkCanvas;
                case KnownProperties.InkPresenter_Child:
                    return KnownElements.InkPresenter;
                case KnownProperties.InlineUIContainer_Child:
                    return KnownElements.InlineUIContainer;
                case KnownProperties.InputScopeName_NameValue:
                    return KnownElements.InputScopeName;
                case KnownProperties.Int16AnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.Int16AnimationUsingKeyFrames;
                case KnownProperties.Int32AnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.Int32AnimationUsingKeyFrames;
                case KnownProperties.Int64AnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.Int64AnimationUsingKeyFrames;
                case KnownProperties.Italic_Inlines:
                    return KnownElements.Italic;
                case KnownProperties.ItemsControl_ItemContainerStyle:
                case KnownProperties.ItemsControl_ItemContainerStyleSelector:
                case KnownProperties.ItemsControl_ItemTemplate:
                case KnownProperties.ItemsControl_ItemTemplateSelector:
                case KnownProperties.ItemsControl_Items:
                case KnownProperties.ItemsControl_ItemsPanel:
                case KnownProperties.ItemsControl_ItemsSource:
                    return KnownElements.ItemsControl;
                case KnownProperties.ItemsPanelTemplate_VisualTree:
                    return KnownElements.ItemsPanelTemplate;
                case KnownProperties.Label_Content:
                    return KnownElements.Label;
                case KnownProperties.LinearGradientBrush_GradientStops:
                    return KnownElements.LinearGradientBrush;
                case KnownProperties.List_ListItems:
                    return KnownElements.List;
                case KnownProperties.ListBox_Items:
                    return KnownElements.ListBox;
                case KnownProperties.ListBoxItem_Content:
                    return KnownElements.ListBoxItem;
                case KnownProperties.ListItem_Blocks:
                    return KnownElements.ListItem;
                case KnownProperties.ListView_Items:
                    return KnownElements.ListView;
                case KnownProperties.ListViewItem_Content:
                    return KnownElements.ListViewItem;
                case KnownProperties.MaterialGroup_Children:
                    return KnownElements.MaterialGroup;
                case KnownProperties.MatrixAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.MatrixAnimationUsingKeyFrames;
                case KnownProperties.Menu_Items:
                    return KnownElements.Menu;
                case KnownProperties.MenuBase_Items:
                    return KnownElements.MenuBase;
                case KnownProperties.MenuItem_Items:
                    return KnownElements.MenuItem;
                case KnownProperties.Model3DGroup_Children:
                    return KnownElements.Model3DGroup;
                case KnownProperties.ModelVisual3D_Children:
                    return KnownElements.ModelVisual3D;
                case KnownProperties.MultiBinding_Bindings:
                    return KnownElements.MultiBinding;
                case KnownProperties.MultiDataTrigger_Setters:
                    return KnownElements.MultiDataTrigger;
                case KnownProperties.MultiTrigger_Setters:
                    return KnownElements.MultiTrigger;
                case KnownProperties.ObjectAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.ObjectAnimationUsingKeyFrames;
                case KnownProperties.Page_Content:
                    return KnownElements.Page;
                case KnownProperties.PageContent_Child:
                    return KnownElements.PageContent;
                case KnownProperties.PageFunctionBase_Content:
                    return KnownElements.PageFunctionBase;
                case KnownProperties.Panel_Background:
                case KnownProperties.Panel_Children:
                    return KnownElements.Panel;
                case KnownProperties.Paragraph_Inlines:
                    return KnownElements.Paragraph;
                case KnownProperties.ParallelTimeline_Children:
                    return KnownElements.ParallelTimeline;
                case KnownProperties.Path_Data:
                    return KnownElements.Path;
                case KnownProperties.PathFigure_Segments:
                    return KnownElements.PathFigure;
                case KnownProperties.PathGeometry_Figures:
                    return KnownElements.PathGeometry;
                case KnownProperties.Point3DAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.Point3DAnimationUsingKeyFrames;
                case KnownProperties.PointAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.PointAnimationUsingKeyFrames;
                case KnownProperties.Popup_Child:
                case KnownProperties.Popup_IsOpen:
                case KnownProperties.Popup_Placement:
                case KnownProperties.Popup_PopupAnimation:
                    return KnownElements.Popup;
                case KnownProperties.PriorityBinding_Bindings:
                    return KnownElements.PriorityBinding;
                case KnownProperties.QuaternionAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.QuaternionAnimationUsingKeyFrames;
                case KnownProperties.RadialGradientBrush_GradientStops:
                    return KnownElements.RadialGradientBrush;
                case KnownProperties.RadioButton_Content:
                    return KnownElements.RadioButton;
                case KnownProperties.RectAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.RectAnimationUsingKeyFrames;
                case KnownProperties.RepeatButton_Content:
                    return KnownElements.RepeatButton;
                case KnownProperties.RichTextBox_Document:
                    return KnownElements.RichTextBox;
                case KnownProperties.Rotation3DAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.Rotation3DAnimationUsingKeyFrames;
                case KnownProperties.RowDefinition_Height:
                case KnownProperties.RowDefinition_MaxHeight:
                case KnownProperties.RowDefinition_MinHeight:
                    return KnownElements.RowDefinition;
                case KnownProperties.Run_Text:
                    return KnownElements.Run;
                case KnownProperties.ScrollViewer_CanContentScroll:
                case KnownProperties.ScrollViewer_Content:
                case KnownProperties.ScrollViewer_HorizontalScrollBarVisibility:
                case KnownProperties.ScrollViewer_VerticalScrollBarVisibility:
                    return KnownElements.ScrollViewer;
                case KnownProperties.Section_Blocks:
                    return KnownElements.Section;
                case KnownProperties.Selector_Items:
                    return KnownElements.Selector;
                case KnownProperties.Shape_Fill:
                case KnownProperties.Shape_Stroke:
                case KnownProperties.Shape_StrokeThickness:
                    return KnownElements.Shape;
                case KnownProperties.SingleAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.SingleAnimationUsingKeyFrames;
                case KnownProperties.SizeAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.SizeAnimationUsingKeyFrames;
                case KnownProperties.Span_Inlines:
                    return KnownElements.Span;
                case KnownProperties.StackPanel_Children:
                    return KnownElements.StackPanel;
                case KnownProperties.StatusBar_Items:
                    return KnownElements.StatusBar;
                case KnownProperties.StatusBarItem_Content:
                    return KnownElements.StatusBarItem;
                case KnownProperties.Storyboard_Children:
                    return KnownElements.Storyboard;
                case KnownProperties.StringAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.StringAnimationUsingKeyFrames;
                case KnownProperties.Style_Setters:
                    return KnownElements.Style;
                case KnownProperties.TabControl_Items:
                    return KnownElements.TabControl;
                case KnownProperties.TabItem_Content:
                    return KnownElements.TabItem;
                case KnownProperties.TabPanel_Children:
                    return KnownElements.TabPanel;
                case KnownProperties.Table_RowGroups:
                    return KnownElements.Table;
                case KnownProperties.TableCell_Blocks:
                    return KnownElements.TableCell;
                case KnownProperties.TableRow_Cells:
                    return KnownElements.TableRow;
                case KnownProperties.TableRowGroup_Rows:
                    return KnownElements.TableRowGroup;
                case KnownProperties.TextBlock_Background:
                case KnownProperties.TextBlock_FontFamily:
                case KnownProperties.TextBlock_FontSize:
                case KnownProperties.TextBlock_FontStretch:
                case KnownProperties.TextBlock_FontStyle:
                case KnownProperties.TextBlock_FontWeight:
                case KnownProperties.TextBlock_Foreground:
                case KnownProperties.TextBlock_Inlines:
                case KnownProperties.TextBlock_Text:
                case KnownProperties.TextBlock_TextDecorations:
                case KnownProperties.TextBlock_TextTrimming:
                case KnownProperties.TextBlock_TextWrapping:
                    return KnownElements.TextBlock;
                case KnownProperties.TextBox_Text:
                    return KnownElements.TextBox;
                case KnownProperties.TextElement_Background:
                case KnownProperties.TextElement_FontFamily:
                case KnownProperties.TextElement_FontSize:
                case KnownProperties.TextElement_FontStretch:
                case KnownProperties.TextElement_FontStyle:
                case KnownProperties.TextElement_FontWeight:
                case KnownProperties.TextElement_Foreground:
                    return KnownElements.TextElement;
                case KnownProperties.ThicknessAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.ThicknessAnimationUsingKeyFrames;
                case KnownProperties.TimelineGroup_Children:
                    return KnownElements.TimelineGroup;
                case KnownProperties.ToggleButton_Content:
                    return KnownElements.ToggleButton;
                case KnownProperties.ToolBar_Items:
                    return KnownElements.ToolBar;
                case KnownProperties.ToolBarOverflowPanel_Children:
                    return KnownElements.ToolBarOverflowPanel;
                case KnownProperties.ToolBarPanel_Children:
                    return KnownElements.ToolBarPanel;
                case KnownProperties.ToolBarTray_ToolBars:
                    return KnownElements.ToolBarTray;
                case KnownProperties.ToolTip_Content:
                    return KnownElements.ToolTip;
                case KnownProperties.Track_IsDirectionReversed:
                case KnownProperties.Track_Maximum:
                case KnownProperties.Track_Minimum:
                case KnownProperties.Track_Orientation:
                case KnownProperties.Track_Value:
                case KnownProperties.Track_ViewportSize:
                    return KnownElements.Track;
                case KnownProperties.Transform3DGroup_Children:
                    return KnownElements.Transform3DGroup;
                case KnownProperties.TransformGroup_Children:
                    return KnownElements.TransformGroup;
                case KnownProperties.TreeView_Items:
                    return KnownElements.TreeView;
                case KnownProperties.TreeViewItem_Items:
                    return KnownElements.TreeViewItem;
                case KnownProperties.Trigger_Setters:
                    return KnownElements.Trigger;
                case KnownProperties.UIElement_ClipToBounds:
                case KnownProperties.UIElement_Focusable:
                case KnownProperties.UIElement_IsEnabled:
                case KnownProperties.UIElement_RenderTransform:
                case KnownProperties.UIElement_Visibility:
                    return KnownElements.UIElement;
                case KnownProperties.Underline_Inlines:
                    return KnownElements.Underline;
                case KnownProperties.UniformGrid_Children:
                    return KnownElements.UniformGrid;
                case KnownProperties.UserControl_Content:
                    return KnownElements.UserControl;
                case KnownProperties.Vector3DAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.Vector3DAnimationUsingKeyFrames;
                case KnownProperties.VectorAnimationUsingKeyFrames_KeyFrames:
                    return KnownElements.VectorAnimationUsingKeyFrames;
                case KnownProperties.Viewbox_Child:
                    return KnownElements.Viewbox;
                case KnownProperties.Viewport3D_Children:
                    return KnownElements.Viewport3D;
                case KnownProperties.Viewport3DVisual_Children:
                    return KnownElements.Viewport3DVisual;
                case KnownProperties.VirtualizingPanel_Children:
                    return KnownElements.VirtualizingPanel;
                case KnownProperties.VirtualizingStackPanel_Children:
                    return KnownElements.VirtualizingStackPanel;
                case KnownProperties.Window_Content:
                    return KnownElements.Window;
                case KnownProperties.WrapPanel_Children:
                    return KnownElements.WrapPanel;
                case KnownProperties.XmlDataProvider_XmlSerializer:
                    return KnownElements.XmlDataProvider;
            }
            return KnownElements.UnknownElement;
        }

        // This code 'knows' that all non-DP (clr) KnownProperties are
        // also Content Properties.  As long as that is true there is no
        // need for a second string table, and we can just cross reference.
        internal static string GetKnownClrPropertyNameFromId(KnownProperties knownProperty)
        {
            KnownElements knownElement = GetKnownElementFromKnownCommonProperty(knownProperty);
            string name = GetContentPropertyName(knownElement);
            return name;
        }

        // Returns IList interface of the Content Property for the given Element.
        // WARNING can return null if no CPA is defined on the Element, or the CPA does implement IList.
        internal static IList GetCollectionForCPA(object o, KnownElements knownElement)
        {
            // We don't cache because we return the IList of the given object.
            switch(knownElement)
            {
            // Panel.Children
            case KnownElements.Canvas:
            case KnownElements.DockPanel:
            case KnownElements.Grid:
            case KnownElements.Panel:
            case KnownElements.StackPanel:
            case KnownElements.TabPanel:
            case KnownElements.ToolBarOverflowPanel:
            case KnownElements.ToolBarPanel:
            case KnownElements.UniformGrid:
            case KnownElements.VirtualizingPanel:
            case KnownElements.VirtualizingStackPanel:
            case KnownElements.WrapPanel:
                return (o as System.Windows.Controls.Panel).Children;

            // ItemsControl.Items
            case KnownElements.ComboBox:
            case KnownElements.ContextMenu:
            case KnownElements.HeaderedItemsControl:
            case KnownElements.ItemsControl:
            case KnownElements.ListBox:
            case KnownElements.ListView:
            case KnownElements.Menu:
            case KnownElements.MenuBase:
            case KnownElements.MenuItem:
            case KnownElements.Selector:
            case KnownElements.StatusBar:
            case KnownElements.TabControl:
            case KnownElements.ToolBar:
            case KnownElements.TreeView:
            case KnownElements.TreeViewItem:
                return (o as System.Windows.Controls.ItemsControl).Items;

            // Span.Inlines
            case KnownElements.Bold:
            case KnownElements.Hyperlink:
            case KnownElements.Italic:
            case KnownElements.Span:
            case KnownElements.Underline:
                return (o as System.Windows.Documents.Span).Inlines;

            // AnchoredBlock.Blocks
            case KnownElements.AnchoredBlock:
            case KnownElements.Figure:
            case KnownElements.Floater:
                return (o as System.Windows.Documents.AnchoredBlock).Blocks;

            // GradientBrush.GradientStops
            case KnownElements.GradientBrush:
            case KnownElements.LinearGradientBrush:
            case KnownElements.RadialGradientBrush:
                return (o as System.Windows.Media.GradientBrush).GradientStops;

            // TimelineGroup.Children
            case KnownElements.ParallelTimeline:
            case KnownElements.Storyboard:
            case KnownElements.TimelineGroup:
                return (o as System.Windows.Media.Animation.TimelineGroup).Children;

            // Other
            case KnownElements.BitmapEffectGroup: return (o as System.Windows.Media.Effects.BitmapEffectGroup).Children;
            case KnownElements.BooleanAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.BooleanAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.ByteAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.ByteAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.CharAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.CharAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.ColorAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.ColorAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.DataTrigger: return (o as System.Windows.DataTrigger).Setters;
            case KnownElements.DecimalAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.DecimalAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.DoubleAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.DrawingGroup: return (o as System.Windows.Media.DrawingGroup).Children;
            case KnownElements.EventTrigger: return (o as System.Windows.EventTrigger).Actions;
            case KnownElements.FixedPage: return (o as System.Windows.Documents.FixedPage).Children;
            case KnownElements.FlowDocument: return (o as System.Windows.Documents.FlowDocument).Blocks;
            case KnownElements.GeneralTransformGroup: return (o as System.Windows.Media.GeneralTransformGroup).Children;
            case KnownElements.GeometryGroup: return (o as System.Windows.Media.GeometryGroup).Children;
            case KnownElements.GridView: return (o as System.Windows.Controls.GridView).Columns;
            case KnownElements.InkCanvas: return (o as System.Windows.Controls.InkCanvas).Children;
            case KnownElements.Int16AnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.Int16AnimationUsingKeyFrames).KeyFrames;
            case KnownElements.Int32AnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.Int32AnimationUsingKeyFrames).KeyFrames;
            case KnownElements.Int64AnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.Int64AnimationUsingKeyFrames).KeyFrames;
            case KnownElements.List: return (o as System.Windows.Documents.List).ListItems;
            case KnownElements.ListItem: return (o as System.Windows.Documents.ListItem).Blocks;
            case KnownElements.MaterialGroup: return (o as System.Windows.Media.Media3D.MaterialGroup).Children;
            case KnownElements.MatrixAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.MatrixAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.Model3DGroup: return (o as System.Windows.Media.Media3D.Model3DGroup).Children;
            case KnownElements.ModelVisual3D: return (o as System.Windows.Media.Media3D.ModelVisual3D).Children;
            case KnownElements.MultiBinding: return (o as System.Windows.Data.MultiBinding).Bindings;
            case KnownElements.MultiDataTrigger: return (o as System.Windows.MultiDataTrigger).Setters;
            case KnownElements.MultiTrigger: return (o as System.Windows.MultiTrigger).Setters;
            case KnownElements.ObjectAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.ObjectAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.Paragraph: return (o as System.Windows.Documents.Paragraph).Inlines;
            case KnownElements.PathFigure: return (o as System.Windows.Media.PathFigure).Segments;
            case KnownElements.PathGeometry: return (o as System.Windows.Media.PathGeometry).Figures;
            case KnownElements.Point3DAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.Point3DAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.PointAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.PointAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.PriorityBinding: return (o as System.Windows.Data.PriorityBinding).Bindings;
            case KnownElements.QuaternionAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.QuaternionAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.RectAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.RectAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.Rotation3DAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.Rotation3DAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.Section: return (o as System.Windows.Documents.Section).Blocks;
            case KnownElements.SingleAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.SingleAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.SizeAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.SizeAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.StringAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.StringAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.Style: return (o as System.Windows.Style).Setters;
            case KnownElements.Table: return (o as System.Windows.Documents.Table).RowGroups;
            case KnownElements.TableCell: return (o as System.Windows.Documents.TableCell).Blocks;
            case KnownElements.TableRow: return (o as System.Windows.Documents.TableRow).Cells;
            case KnownElements.TableRowGroup: return (o as System.Windows.Documents.TableRowGroup).Rows;
            case KnownElements.TextBlock: return (o as System.Windows.Controls.TextBlock).Inlines;
            case KnownElements.ThicknessAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.ToolBarTray: return (o as System.Windows.Controls.ToolBarTray).ToolBars;
            case KnownElements.Transform3DGroup: return (o as System.Windows.Media.Media3D.Transform3DGroup).Children;
            case KnownElements.TransformGroup: return (o as System.Windows.Media.TransformGroup).Children;
            case KnownElements.Trigger: return (o as System.Windows.Trigger).Setters;
            case KnownElements.Vector3DAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.Vector3DAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.VectorAnimationUsingKeyFrames: return (o as System.Windows.Media.Animation.VectorAnimationUsingKeyFrames).KeyFrames;
            case KnownElements.Viewport3D: return (o as System.Windows.Controls.Viewport3D).Children;
            case KnownElements.Viewport3DVisual: return (o as System.Windows.Media.Media3D.Viewport3DVisual).Children;
            }
            return null;
        }
#endif // #if !PBTCOMPILER

        // Indicate if a collection type can accept strings.  E.g. DoubleCollection cannot
        // accept strings, because it is an ICollection<Double>.  But UIElementCollection does
        // accept strings, because it is just IList.

        internal static bool CanCollectionTypeAcceptStrings(KnownElements knownElement)
        {
            switch(knownElement)
            {
                case KnownElements.BitmapEffectCollection:
                case KnownElements.DoubleCollection:
                case KnownElements.DrawingCollection:
                case KnownElements.GeneralTransformCollection:
                case KnownElements.GeometryCollection:
                case KnownElements.GradientStopCollection:
                case KnownElements.Int32Collection:
                case KnownElements.MaterialCollection:
                case KnownElements.Model3DCollection:
                case KnownElements.PathFigureCollection:
                case KnownElements.PathSegmentCollection:
                case KnownElements.Point3DCollection:
                case KnownElements.PointCollection:
                case KnownElements.StrokeCollection:
                case KnownElements.TextDecorationCollection:
                case KnownElements.TextEffectCollection:
                case KnownElements.TimelineCollection:
                case KnownElements.Transform3DCollection:
                case KnownElements.TransformCollection:
                case KnownElements.Vector3DCollection:
                case KnownElements.VectorCollection:

                        return false;
            }
            return true;
        }

        internal static string GetContentPropertyName(KnownElements knownElement)
        {
            string name=null;

            switch(knownElement)
            {
                case KnownElements.EventTrigger:
                    name = "Actions";
                    break;
                case KnownElements.MultiBinding:
                case KnownElements.PriorityBinding:
                    name = "Bindings";
                    break;
                case KnownElements.AnchoredBlock:
                case KnownElements.Figure:
                case KnownElements.Floater:
                case KnownElements.FlowDocument:
                case KnownElements.ListItem:
                case KnownElements.Section:
                case KnownElements.TableCell:
                    name = "Blocks";
                    break;
                case KnownElements.TableRow:
                    name = "Cells";
                    break;
                case KnownElements.AdornedElementPlaceholder:
                case KnownElements.AdornerDecorator:
                case KnownElements.BlockUIContainer:
                case KnownElements.Border:
                case KnownElements.BulletDecorator:
                case KnownElements.Decorator:
                case KnownElements.InkPresenter:
                case KnownElements.InlineUIContainer:
                case KnownElements.PageContent:
                case KnownElements.Popup:
                case KnownElements.Viewbox:
                    name = "Child";
                    break;
                case KnownElements.BitmapEffectGroup:
                case KnownElements.Canvas:
                case KnownElements.DockPanel:
                case KnownElements.DrawingGroup:
                case KnownElements.FixedPage:
                case KnownElements.GeneralTransformGroup:
                case KnownElements.GeometryGroup:
                case KnownElements.Grid:
                case KnownElements.InkCanvas:
                case KnownElements.MaterialGroup:
                case KnownElements.Model3DGroup:
                case KnownElements.ModelVisual3D:
                case KnownElements.Panel:
                case KnownElements.ParallelTimeline:
                case KnownElements.StackPanel:
                case KnownElements.Storyboard:
                case KnownElements.TabPanel:
                case KnownElements.TimelineGroup:
                case KnownElements.ToolBarOverflowPanel:
                case KnownElements.ToolBarPanel:
                case KnownElements.Transform3DGroup:
                case KnownElements.TransformGroup:
                case KnownElements.UniformGrid:
                case KnownElements.Viewport3D:
                case KnownElements.Viewport3DVisual:
                case KnownElements.VirtualizingPanel:
                case KnownElements.VirtualizingStackPanel:
                case KnownElements.WrapPanel:
                    name = "Children";
                    break;
                case KnownElements.GridView:
                    name = "Columns";
                    break;
                case KnownElements.Button:
                case KnownElements.ButtonBase:
                case KnownElements.CheckBox:
                case KnownElements.ComboBoxItem:
                case KnownElements.ContentControl:
                case KnownElements.Expander:
                case KnownElements.GridViewColumnHeader:
                case KnownElements.GroupBox:
                case KnownElements.GroupItem:
                case KnownElements.HeaderedContentControl:
                case KnownElements.Label:
                case KnownElements.ListBoxItem:
                case KnownElements.ListViewItem:
                case KnownElements.Page:
                case KnownElements.PageFunctionBase:
                case KnownElements.RadioButton:
                case KnownElements.RepeatButton:
                case KnownElements.ScrollViewer:
                case KnownElements.StatusBarItem:
                case KnownElements.TabItem:
                case KnownElements.ToggleButton:
                case KnownElements.ToolTip:
                case KnownElements.UserControl:
                case KnownElements.Window:
                    name = "Content";
                    break;
                case KnownElements.DocumentViewer:
                case KnownElements.DocumentViewerBase:
                case KnownElements.FlowDocumentPageViewer:
                case KnownElements.FlowDocumentReader:
                case KnownElements.FlowDocumentScrollViewer:
                case KnownElements.RichTextBox:
                    name = "Document";
                    break;
                case KnownElements.PathGeometry:
                    name = "Figures";
                    break;
                case KnownElements.GradientBrush:
                case KnownElements.LinearGradientBrush:
                case KnownElements.RadialGradientBrush:
                    name = "GradientStops";
                    break;
                case KnownElements.GridViewColumn:
                    name = "Header";
                    break;
                case KnownElements.Bold:
                case KnownElements.Hyperlink:
                case KnownElements.Italic:
                case KnownElements.Paragraph:
                case KnownElements.Span:
                case KnownElements.TextBlock:
                case KnownElements.Underline:
                    name = "Inlines";
                    break;
                case KnownElements.ArrayExtension:
                case KnownElements.ComboBox:
                case KnownElements.ContextMenu:
                case KnownElements.HeaderedItemsControl:
                case KnownElements.ItemsControl:
                case KnownElements.ListBox:
                case KnownElements.ListView:
                case KnownElements.Menu:
                case KnownElements.MenuBase:
                case KnownElements.MenuItem:
                case KnownElements.Selector:
                case KnownElements.StatusBar:
                case KnownElements.TabControl:
                case KnownElements.ToolBar:
                case KnownElements.TreeView:
                case KnownElements.TreeViewItem:
                    name = "Items";
                    break;
                case KnownElements.BooleanAnimationUsingKeyFrames:
                case KnownElements.ByteAnimationUsingKeyFrames:
                case KnownElements.CharAnimationUsingKeyFrames:
                case KnownElements.ColorAnimationUsingKeyFrames:
                case KnownElements.DecimalAnimationUsingKeyFrames:
                case KnownElements.DoubleAnimationUsingKeyFrames:
                case KnownElements.Int16AnimationUsingKeyFrames:
                case KnownElements.Int32AnimationUsingKeyFrames:
                case KnownElements.Int64AnimationUsingKeyFrames:
                case KnownElements.MatrixAnimationUsingKeyFrames:
                case KnownElements.ObjectAnimationUsingKeyFrames:
                case KnownElements.Point3DAnimationUsingKeyFrames:
                case KnownElements.PointAnimationUsingKeyFrames:
                case KnownElements.QuaternionAnimationUsingKeyFrames:
                case KnownElements.RectAnimationUsingKeyFrames:
                case KnownElements.Rotation3DAnimationUsingKeyFrames:
                case KnownElements.SingleAnimationUsingKeyFrames:
                case KnownElements.SizeAnimationUsingKeyFrames:
                case KnownElements.StringAnimationUsingKeyFrames:
                case KnownElements.ThicknessAnimationUsingKeyFrames:
                case KnownElements.Vector3DAnimationUsingKeyFrames:
                case KnownElements.VectorAnimationUsingKeyFrames:
                    name = "KeyFrames";
                    break;
                case KnownElements.List:
                    name = "ListItems";
                    break;
                case KnownElements.InputScopeName:
                    name = "NameValue";
                    break;
                case KnownElements.FixedDocument:
                    name = "Pages";
                    break;
                case KnownElements.FixedDocumentSequence:
                    name = "References";
                    break;
                case KnownElements.Table:
                    name = "RowGroups";
                    break;
                case KnownElements.TableRowGroup:
                    name = "Rows";
                    break;
                case KnownElements.PathFigure:
                    name = "Segments";
                    break;
                case KnownElements.DataTrigger:
                case KnownElements.MultiDataTrigger:
                case KnownElements.MultiTrigger:
                case KnownElements.Style:
                case KnownElements.Trigger:
                    name = "Setters";
                    break;
                case KnownElements.BeginStoryboard:
                    name = "Storyboard";
                    break;
                case KnownElements.AccessText:
                case KnownElements.Run:
                case KnownElements.TextBox:
                    name = "Text";
                    break;
                case KnownElements.ToolBarTray:
                    name = "ToolBars";
                    break;
                case KnownElements.ControlTemplate:
                case KnownElements.DataTemplate:
                case KnownElements.FrameworkTemplate:
                case KnownElements.HierarchicalDataTemplate:
                case KnownElements.ItemsPanelTemplate:
                    name = "VisualTree";
                    break;
                case KnownElements.XmlDataProvider:
                    name = "XmlSerializer";
                    break;
            }
            return name;
        }

        internal static short GetKnownPropertyAttributeId(KnownElements typeID, string fieldName)
        {
            switch (typeID)
            {
                case KnownElements.AccessText:
                    if (String.CompareOrdinal(fieldName, "Text") == 0)
                        return (short)KnownProperties.AccessText_Text;
                    break;
                case KnownElements.AdornedElementPlaceholder:
                    if (String.CompareOrdinal(fieldName, "Child") == 0)
                        return (short)KnownProperties.AdornedElementPlaceholder_Child;
                    break;
                case KnownElements.AdornerDecorator:
                    if (String.CompareOrdinal(fieldName, "Child") == 0)
                        return (short)KnownProperties.AdornerDecorator_Child;
                    break;
                case KnownElements.AnchoredBlock:
                    if (String.CompareOrdinal(fieldName, "Blocks") == 0)
                        return (short)KnownProperties.AnchoredBlock_Blocks;
                    break;
                case KnownElements.ArrayExtension:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.ArrayExtension_Items;
                    break;
                case KnownElements.BeginStoryboard:
                    if (String.CompareOrdinal(fieldName, "Storyboard") == 0)
                        return (short)KnownProperties.BeginStoryboard_Storyboard;
                    break;
                case KnownElements.BitmapEffectGroup:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.BitmapEffectGroup_Children;
                    break;
                case KnownElements.BlockUIContainer:
                    if (String.CompareOrdinal(fieldName, "Child") == 0)
                        return (short)KnownProperties.BlockUIContainer_Child;
                    break;
                case KnownElements.Bold:
                    if (String.CompareOrdinal(fieldName, "Inlines") == 0)
                        return (short)KnownProperties.Bold_Inlines;
                    break;
                case KnownElements.BooleanAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.BooleanAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Border:
                    if (String.CompareOrdinal(fieldName, "Background") == 0)
                        return (short)KnownProperties.Border_Background;
                    if (String.CompareOrdinal(fieldName, "BorderBrush") == 0)
                        return (short)KnownProperties.Border_BorderBrush;
                    if (String.CompareOrdinal(fieldName, "BorderThickness") == 0)
                        return (short)KnownProperties.Border_BorderThickness;
                    if (String.CompareOrdinal(fieldName, "Child") == 0)
                        return (short)KnownProperties.Border_Child;
                    break;
                case KnownElements.BulletDecorator:
                    if (String.CompareOrdinal(fieldName, "Child") == 0)
                        return (short)KnownProperties.BulletDecorator_Child;
                    break;
                case KnownElements.Button:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.Button_Content;
                    break;
                case KnownElements.ButtonBase:
                    if (String.CompareOrdinal(fieldName, "Command") == 0)
                        return (short)KnownProperties.ButtonBase_Command;
                    if (String.CompareOrdinal(fieldName, "CommandParameter") == 0)
                        return (short)KnownProperties.ButtonBase_CommandParameter;
                    if (String.CompareOrdinal(fieldName, "CommandTarget") == 0)
                        return (short)KnownProperties.ButtonBase_CommandTarget;
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.ButtonBase_Content;
                    if (String.CompareOrdinal(fieldName, "IsPressed") == 0)
                        return (short)KnownProperties.ButtonBase_IsPressed;
                    break;
                case KnownElements.ByteAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.ByteAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Canvas:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.Canvas_Children;
                    break;
                case KnownElements.CharAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.CharAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.CheckBox:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.CheckBox_Content;
                    break;
                case KnownElements.ColorAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.ColorAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.ColumnDefinition:
                    if (String.CompareOrdinal(fieldName, "MaxWidth") == 0)
                        return (short)KnownProperties.ColumnDefinition_MaxWidth;
                    if (String.CompareOrdinal(fieldName, "MinWidth") == 0)
                        return (short)KnownProperties.ColumnDefinition_MinWidth;
                    if (String.CompareOrdinal(fieldName, "Width") == 0)
                        return (short)KnownProperties.ColumnDefinition_Width;
                    break;
                case KnownElements.ComboBox:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.ComboBox_Items;
                    break;
                case KnownElements.ComboBoxItem:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.ComboBoxItem_Content;
                    break;
                case KnownElements.ContentControl:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.ContentControl_Content;
                    if (String.CompareOrdinal(fieldName, "ContentTemplate") == 0)
                        return (short)KnownProperties.ContentControl_ContentTemplate;
                    if (String.CompareOrdinal(fieldName, "ContentTemplateSelector") == 0)
                        return (short)KnownProperties.ContentControl_ContentTemplateSelector;
                    if (String.CompareOrdinal(fieldName, "HasContent") == 0)
                        return (short)KnownProperties.ContentControl_HasContent;
                    break;
                case KnownElements.ContentElement:
                    if (String.CompareOrdinal(fieldName, "Focusable") == 0)
                        return (short)KnownProperties.ContentElement_Focusable;
                    break;
                case KnownElements.ContentPresenter:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.ContentPresenter_Content;
                    if (String.CompareOrdinal(fieldName, "ContentSource") == 0)
                        return (short)KnownProperties.ContentPresenter_ContentSource;
                    if (String.CompareOrdinal(fieldName, "ContentTemplate") == 0)
                        return (short)KnownProperties.ContentPresenter_ContentTemplate;
                    if (String.CompareOrdinal(fieldName, "ContentTemplateSelector") == 0)
                        return (short)KnownProperties.ContentPresenter_ContentTemplateSelector;
                    if (String.CompareOrdinal(fieldName, "RecognizesAccessKey") == 0)
                        return (short)KnownProperties.ContentPresenter_RecognizesAccessKey;
                    break;
                case KnownElements.ContextMenu:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.ContextMenu_Items;
                    break;
                case KnownElements.Control:
                    if (String.CompareOrdinal(fieldName, "Background") == 0)
                        return (short)KnownProperties.Control_Background;
                    if (String.CompareOrdinal(fieldName, "BorderBrush") == 0)
                        return (short)KnownProperties.Control_BorderBrush;
                    if (String.CompareOrdinal(fieldName, "BorderThickness") == 0)
                        return (short)KnownProperties.Control_BorderThickness;
                    if (String.CompareOrdinal(fieldName, "FontFamily") == 0)
                        return (short)KnownProperties.Control_FontFamily;
                    if (String.CompareOrdinal(fieldName, "FontSize") == 0)
                        return (short)KnownProperties.Control_FontSize;
                    if (String.CompareOrdinal(fieldName, "FontStretch") == 0)
                        return (short)KnownProperties.Control_FontStretch;
                    if (String.CompareOrdinal(fieldName, "FontStyle") == 0)
                        return (short)KnownProperties.Control_FontStyle;
                    if (String.CompareOrdinal(fieldName, "FontWeight") == 0)
                        return (short)KnownProperties.Control_FontWeight;
                    if (String.CompareOrdinal(fieldName, "Foreground") == 0)
                        return (short)KnownProperties.Control_Foreground;
                    if (String.CompareOrdinal(fieldName, "HorizontalContentAlignment") == 0)
                        return (short)KnownProperties.Control_HorizontalContentAlignment;
                    if (String.CompareOrdinal(fieldName, "IsTabStop") == 0)
                        return (short)KnownProperties.Control_IsTabStop;
                    if (String.CompareOrdinal(fieldName, "Padding") == 0)
                        return (short)KnownProperties.Control_Padding;
                    if (String.CompareOrdinal(fieldName, "TabIndex") == 0)
                        return (short)KnownProperties.Control_TabIndex;
                    if (String.CompareOrdinal(fieldName, "Template") == 0)
                        return (short)KnownProperties.Control_Template;
                    if (String.CompareOrdinal(fieldName, "VerticalContentAlignment") == 0)
                        return (short)KnownProperties.Control_VerticalContentAlignment;
                    break;
                case KnownElements.ControlTemplate:
                    if (String.CompareOrdinal(fieldName, "VisualTree") == 0)
                        return (short)KnownProperties.ControlTemplate_VisualTree;
                    break;
                case KnownElements.DataTemplate:
                    if (String.CompareOrdinal(fieldName, "VisualTree") == 0)
                        return (short)KnownProperties.DataTemplate_VisualTree;
                    break;
                case KnownElements.DataTrigger:
                    if (String.CompareOrdinal(fieldName, "Setters") == 0)
                        return (short)KnownProperties.DataTrigger_Setters;
                    break;
                case KnownElements.DecimalAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.DecimalAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Decorator:
                    if (String.CompareOrdinal(fieldName, "Child") == 0)
                        return (short)KnownProperties.Decorator_Child;
                    break;
                case KnownElements.DockPanel:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.DockPanel_Children;
                    if (String.CompareOrdinal(fieldName, "Dock") == 0)
                        return (short)KnownProperties.DockPanel_Dock;
                    if (String.CompareOrdinal(fieldName, "LastChildFill") == 0)
                        return (short)KnownProperties.DockPanel_LastChildFill;
                    break;
                case KnownElements.DocumentViewer:
                    if (String.CompareOrdinal(fieldName, "Document") == 0)
                        return (short)KnownProperties.DocumentViewer_Document;
                    break;
                case KnownElements.DocumentViewerBase:
                    if (String.CompareOrdinal(fieldName, "Document") == 0)
                        return (short)KnownProperties.DocumentViewerBase_Document;
                    break;
                case KnownElements.DoubleAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.DoubleAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.DrawingGroup:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.DrawingGroup_Children;
                    break;
                case KnownElements.EventTrigger:
                    if (String.CompareOrdinal(fieldName, "Actions") == 0)
                        return (short)KnownProperties.EventTrigger_Actions;
                    break;
                case KnownElements.Expander:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.Expander_Content;
                    break;
                case KnownElements.Figure:
                    if (String.CompareOrdinal(fieldName, "Blocks") == 0)
                        return (short)KnownProperties.Figure_Blocks;
                    break;
                case KnownElements.FixedDocument:
                    if (String.CompareOrdinal(fieldName, "Pages") == 0)
                        return (short)KnownProperties.FixedDocument_Pages;
                    break;
                case KnownElements.FixedDocumentSequence:
                    if (String.CompareOrdinal(fieldName, "References") == 0)
                        return (short)KnownProperties.FixedDocumentSequence_References;
                    break;
                case KnownElements.FixedPage:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.FixedPage_Children;
                    break;
                case KnownElements.Floater:
                    if (String.CompareOrdinal(fieldName, "Blocks") == 0)
                        return (short)KnownProperties.Floater_Blocks;
                    break;
                case KnownElements.FlowDocument:
                    if (String.CompareOrdinal(fieldName, "Blocks") == 0)
                        return (short)KnownProperties.FlowDocument_Blocks;
                    break;
                case KnownElements.FlowDocumentPageViewer:
                    if (String.CompareOrdinal(fieldName, "Document") == 0)
                        return (short)KnownProperties.FlowDocumentPageViewer_Document;
                    break;
                case KnownElements.FlowDocumentReader:
                    if (String.CompareOrdinal(fieldName, "Document") == 0)
                        return (short)KnownProperties.FlowDocumentReader_Document;
                    break;
                case KnownElements.FlowDocumentScrollViewer:
                    if (String.CompareOrdinal(fieldName, "Document") == 0)
                        return (short)KnownProperties.FlowDocumentScrollViewer_Document;
                    break;
                case KnownElements.FrameworkContentElement:
                    if (String.CompareOrdinal(fieldName, "Style") == 0)
                        return (short)KnownProperties.FrameworkContentElement_Style;
                    break;
                case KnownElements.FrameworkElement:
                    if (String.CompareOrdinal(fieldName, "FlowDirection") == 0)
                        return (short)KnownProperties.FrameworkElement_FlowDirection;
                    if (String.CompareOrdinal(fieldName, "Height") == 0)
                        return (short)KnownProperties.FrameworkElement_Height;
                    if (String.CompareOrdinal(fieldName, "HorizontalAlignment") == 0)
                        return (short)KnownProperties.FrameworkElement_HorizontalAlignment;
                    if (String.CompareOrdinal(fieldName, "Margin") == 0)
                        return (short)KnownProperties.FrameworkElement_Margin;
                    if (String.CompareOrdinal(fieldName, "MaxHeight") == 0)
                        return (short)KnownProperties.FrameworkElement_MaxHeight;
                    if (String.CompareOrdinal(fieldName, "MaxWidth") == 0)
                        return (short)KnownProperties.FrameworkElement_MaxWidth;
                    if (String.CompareOrdinal(fieldName, "MinHeight") == 0)
                        return (short)KnownProperties.FrameworkElement_MinHeight;
                    if (String.CompareOrdinal(fieldName, "MinWidth") == 0)
                        return (short)KnownProperties.FrameworkElement_MinWidth;
                    if (String.CompareOrdinal(fieldName, "Name") == 0)
                        return (short)KnownProperties.FrameworkElement_Name;
                    if (String.CompareOrdinal(fieldName, "Style") == 0)
                        return (short)KnownProperties.FrameworkElement_Style;
                    if (String.CompareOrdinal(fieldName, "VerticalAlignment") == 0)
                        return (short)KnownProperties.FrameworkElement_VerticalAlignment;
                    if (String.CompareOrdinal(fieldName, "Width") == 0)
                        return (short)KnownProperties.FrameworkElement_Width;
                    break;
                case KnownElements.FrameworkTemplate:
                    if (String.CompareOrdinal(fieldName, "VisualTree") == 0)
                        return (short)KnownProperties.FrameworkTemplate_VisualTree;
                    break;
                case KnownElements.GeneralTransformGroup:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.GeneralTransformGroup_Children;
                    break;
                case KnownElements.GeometryGroup:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.GeometryGroup_Children;
                    break;
                case KnownElements.GradientBrush:
                    if (String.CompareOrdinal(fieldName, "GradientStops") == 0)
                        return (short)KnownProperties.GradientBrush_GradientStops;
                    break;
                case KnownElements.Grid:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.Grid_Children;
                    if (String.CompareOrdinal(fieldName, "Column") == 0)
                        return (short)KnownProperties.Grid_Column;
                    if (String.CompareOrdinal(fieldName, "ColumnSpan") == 0)
                        return (short)KnownProperties.Grid_ColumnSpan;
                    if (String.CompareOrdinal(fieldName, "Row") == 0)
                        return (short)KnownProperties.Grid_Row;
                    if (String.CompareOrdinal(fieldName, "RowSpan") == 0)
                        return (short)KnownProperties.Grid_RowSpan;
                    break;
                case KnownElements.GridView:
                    if (String.CompareOrdinal(fieldName, "Columns") == 0)
                        return (short)KnownProperties.GridView_Columns;
                    break;
                case KnownElements.GridViewColumn:
                    if (String.CompareOrdinal(fieldName, "Header") == 0)
                        return (short)KnownProperties.GridViewColumn_Header;
                    break;
                case KnownElements.GridViewColumnHeader:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.GridViewColumnHeader_Content;
                    break;
                case KnownElements.GroupBox:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.GroupBox_Content;
                    break;
                case KnownElements.GroupItem:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.GroupItem_Content;
                    break;
                case KnownElements.HeaderedContentControl:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.HeaderedContentControl_Content;
                    if (String.CompareOrdinal(fieldName, "HasHeader") == 0)
                        return (short)KnownProperties.HeaderedContentControl_HasHeader;
                    if (String.CompareOrdinal(fieldName, "Header") == 0)
                        return (short)KnownProperties.HeaderedContentControl_Header;
                    if (String.CompareOrdinal(fieldName, "HeaderTemplate") == 0)
                        return (short)KnownProperties.HeaderedContentControl_HeaderTemplate;
                    if (String.CompareOrdinal(fieldName, "HeaderTemplateSelector") == 0)
                        return (short)KnownProperties.HeaderedContentControl_HeaderTemplateSelector;
                    break;
                case KnownElements.HeaderedItemsControl:
                    if (String.CompareOrdinal(fieldName, "HasHeader") == 0)
                        return (short)KnownProperties.HeaderedItemsControl_HasHeader;
                    if (String.CompareOrdinal(fieldName, "Header") == 0)
                        return (short)KnownProperties.HeaderedItemsControl_Header;
                    if (String.CompareOrdinal(fieldName, "HeaderTemplate") == 0)
                        return (short)KnownProperties.HeaderedItemsControl_HeaderTemplate;
                    if (String.CompareOrdinal(fieldName, "HeaderTemplateSelector") == 0)
                        return (short)KnownProperties.HeaderedItemsControl_HeaderTemplateSelector;
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.HeaderedItemsControl_Items;
                    break;
                case KnownElements.HierarchicalDataTemplate:
                    if (String.CompareOrdinal(fieldName, "VisualTree") == 0)
                        return (short)KnownProperties.HierarchicalDataTemplate_VisualTree;
                    break;
                case KnownElements.Hyperlink:
                    if (String.CompareOrdinal(fieldName, "Inlines") == 0)
                        return (short)KnownProperties.Hyperlink_Inlines;
                    if (String.CompareOrdinal(fieldName, "NavigateUri") == 0)
                        return (short)KnownProperties.Hyperlink_NavigateUri;
                    break;
                case KnownElements.Image:
                    if (String.CompareOrdinal(fieldName, "Source") == 0)
                        return (short)KnownProperties.Image_Source;
                    if (String.CompareOrdinal(fieldName, "Stretch") == 0)
                        return (short)KnownProperties.Image_Stretch;
                    break;
                case KnownElements.InkCanvas:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.InkCanvas_Children;
                    break;
                case KnownElements.InkPresenter:
                    if (String.CompareOrdinal(fieldName, "Child") == 0)
                        return (short)KnownProperties.InkPresenter_Child;
                    break;
                case KnownElements.InlineUIContainer:
                    if (String.CompareOrdinal(fieldName, "Child") == 0)
                        return (short)KnownProperties.InlineUIContainer_Child;
                    break;
                case KnownElements.InputScopeName:
                    if (String.CompareOrdinal(fieldName, "NameValue") == 0)
                        return (short)KnownProperties.InputScopeName_NameValue;
                    break;
                case KnownElements.Int16AnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.Int16AnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Int32AnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.Int32AnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Int64AnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.Int64AnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Italic:
                    if (String.CompareOrdinal(fieldName, "Inlines") == 0)
                        return (short)KnownProperties.Italic_Inlines;
                    break;
                case KnownElements.ItemsControl:
                    if (String.CompareOrdinal(fieldName, "ItemContainerStyle") == 0)
                        return (short)KnownProperties.ItemsControl_ItemContainerStyle;
                    if (String.CompareOrdinal(fieldName, "ItemContainerStyleSelector") == 0)
                        return (short)KnownProperties.ItemsControl_ItemContainerStyleSelector;
                    if (String.CompareOrdinal(fieldName, "ItemTemplate") == 0)
                        return (short)KnownProperties.ItemsControl_ItemTemplate;
                    if (String.CompareOrdinal(fieldName, "ItemTemplateSelector") == 0)
                        return (short)KnownProperties.ItemsControl_ItemTemplateSelector;
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.ItemsControl_Items;
                    if (String.CompareOrdinal(fieldName, "ItemsPanel") == 0)
                        return (short)KnownProperties.ItemsControl_ItemsPanel;
                    if (String.CompareOrdinal(fieldName, "ItemsSource") == 0)
                        return (short)KnownProperties.ItemsControl_ItemsSource;
                    break;
                case KnownElements.ItemsPanelTemplate:
                    if (String.CompareOrdinal(fieldName, "VisualTree") == 0)
                        return (short)KnownProperties.ItemsPanelTemplate_VisualTree;
                    break;
                case KnownElements.Label:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.Label_Content;
                    break;
                case KnownElements.LinearGradientBrush:
                    if (String.CompareOrdinal(fieldName, "GradientStops") == 0)
                        return (short)KnownProperties.LinearGradientBrush_GradientStops;
                    break;
                case KnownElements.List:
                    if (String.CompareOrdinal(fieldName, "ListItems") == 0)
                        return (short)KnownProperties.List_ListItems;
                    break;
                case KnownElements.ListBox:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.ListBox_Items;
                    break;
                case KnownElements.ListBoxItem:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.ListBoxItem_Content;
                    break;
                case KnownElements.ListItem:
                    if (String.CompareOrdinal(fieldName, "Blocks") == 0)
                        return (short)KnownProperties.ListItem_Blocks;
                    break;
                case KnownElements.ListView:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.ListView_Items;
                    break;
                case KnownElements.ListViewItem:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.ListViewItem_Content;
                    break;
                case KnownElements.MaterialGroup:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.MaterialGroup_Children;
                    break;
                case KnownElements.MatrixAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.MatrixAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Menu:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.Menu_Items;
                    break;
                case KnownElements.MenuBase:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.MenuBase_Items;
                    break;
                case KnownElements.MenuItem:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.MenuItem_Items;
                    break;
                case KnownElements.Model3DGroup:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.Model3DGroup_Children;
                    break;
                case KnownElements.ModelVisual3D:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.ModelVisual3D_Children;
                    break;
                case KnownElements.MultiBinding:
                    if (String.CompareOrdinal(fieldName, "Bindings") == 0)
                        return (short)KnownProperties.MultiBinding_Bindings;
                    break;
                case KnownElements.MultiDataTrigger:
                    if (String.CompareOrdinal(fieldName, "Setters") == 0)
                        return (short)KnownProperties.MultiDataTrigger_Setters;
                    break;
                case KnownElements.MultiTrigger:
                    if (String.CompareOrdinal(fieldName, "Setters") == 0)
                        return (short)KnownProperties.MultiTrigger_Setters;
                    break;
                case KnownElements.ObjectAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.ObjectAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Page:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.Page_Content;
                    break;
                case KnownElements.PageContent:
                    if (String.CompareOrdinal(fieldName, "Child") == 0)
                        return (short)KnownProperties.PageContent_Child;
                    break;
                case KnownElements.PageFunctionBase:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.PageFunctionBase_Content;
                    break;
                case KnownElements.Panel:
                    if (String.CompareOrdinal(fieldName, "Background") == 0)
                        return (short)KnownProperties.Panel_Background;
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.Panel_Children;
                    break;
                case KnownElements.Paragraph:
                    if (String.CompareOrdinal(fieldName, "Inlines") == 0)
                        return (short)KnownProperties.Paragraph_Inlines;
                    break;
                case KnownElements.ParallelTimeline:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.ParallelTimeline_Children;
                    break;
                case KnownElements.Path:
                    if (String.CompareOrdinal(fieldName, "Data") == 0)
                        return (short)KnownProperties.Path_Data;
                    break;
                case KnownElements.PathFigure:
                    if (String.CompareOrdinal(fieldName, "Segments") == 0)
                        return (short)KnownProperties.PathFigure_Segments;
                    break;
                case KnownElements.PathGeometry:
                    if (String.CompareOrdinal(fieldName, "Figures") == 0)
                        return (short)KnownProperties.PathGeometry_Figures;
                    break;
                case KnownElements.Point3DAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.Point3DAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.PointAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.PointAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Popup:
                    if (String.CompareOrdinal(fieldName, "Child") == 0)
                        return (short)KnownProperties.Popup_Child;
                    if (String.CompareOrdinal(fieldName, "IsOpen") == 0)
                        return (short)KnownProperties.Popup_IsOpen;
                    if (String.CompareOrdinal(fieldName, "Placement") == 0)
                        return (short)KnownProperties.Popup_Placement;
                    if (String.CompareOrdinal(fieldName, "PopupAnimation") == 0)
                        return (short)KnownProperties.Popup_PopupAnimation;
                    break;
                case KnownElements.PriorityBinding:
                    if (String.CompareOrdinal(fieldName, "Bindings") == 0)
                        return (short)KnownProperties.PriorityBinding_Bindings;
                    break;
                case KnownElements.QuaternionAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.QuaternionAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.RadialGradientBrush:
                    if (String.CompareOrdinal(fieldName, "GradientStops") == 0)
                        return (short)KnownProperties.RadialGradientBrush_GradientStops;
                    break;
                case KnownElements.RadioButton:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.RadioButton_Content;
                    break;
                case KnownElements.RectAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.RectAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.RepeatButton:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.RepeatButton_Content;
                    break;
                case KnownElements.RichTextBox:
                    if (String.CompareOrdinal(fieldName, "Document") == 0)
                        return (short)KnownProperties.RichTextBox_Document;
                    break;
                case KnownElements.Rotation3DAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.Rotation3DAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.RowDefinition:
                    if (String.CompareOrdinal(fieldName, "Height") == 0)
                        return (short)KnownProperties.RowDefinition_Height;
                    if (String.CompareOrdinal(fieldName, "MaxHeight") == 0)
                        return (short)KnownProperties.RowDefinition_MaxHeight;
                    if (String.CompareOrdinal(fieldName, "MinHeight") == 0)
                        return (short)KnownProperties.RowDefinition_MinHeight;
                    break;
                case KnownElements.Run:
                    if (String.CompareOrdinal(fieldName, "Text") == 0)
                        return (short)KnownProperties.Run_Text;
                    break;
                case KnownElements.ScrollViewer:
                    if (String.CompareOrdinal(fieldName, "CanContentScroll") == 0)
                        return (short)KnownProperties.ScrollViewer_CanContentScroll;
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.ScrollViewer_Content;
                    if (String.CompareOrdinal(fieldName, "HorizontalScrollBarVisibility") == 0)
                        return (short)KnownProperties.ScrollViewer_HorizontalScrollBarVisibility;
                    if (String.CompareOrdinal(fieldName, "VerticalScrollBarVisibility") == 0)
                        return (short)KnownProperties.ScrollViewer_VerticalScrollBarVisibility;
                    break;
                case KnownElements.Section:
                    if (String.CompareOrdinal(fieldName, "Blocks") == 0)
                        return (short)KnownProperties.Section_Blocks;
                    break;
                case KnownElements.Selector:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.Selector_Items;
                    break;
                case KnownElements.Shape:
                    if (String.CompareOrdinal(fieldName, "Fill") == 0)
                        return (short)KnownProperties.Shape_Fill;
                    if (String.CompareOrdinal(fieldName, "Stroke") == 0)
                        return (short)KnownProperties.Shape_Stroke;
                    if (String.CompareOrdinal(fieldName, "StrokeThickness") == 0)
                        return (short)KnownProperties.Shape_StrokeThickness;
                    break;
                case KnownElements.SingleAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.SingleAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.SizeAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.SizeAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Span:
                    if (String.CompareOrdinal(fieldName, "Inlines") == 0)
                        return (short)KnownProperties.Span_Inlines;
                    break;
                case KnownElements.StackPanel:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.StackPanel_Children;
                    break;
                case KnownElements.StatusBar:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.StatusBar_Items;
                    break;
                case KnownElements.StatusBarItem:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.StatusBarItem_Content;
                    break;
                case KnownElements.Storyboard:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.Storyboard_Children;
                    break;
                case KnownElements.StringAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.StringAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Style:
                    if (String.CompareOrdinal(fieldName, "Setters") == 0)
                        return (short)KnownProperties.Style_Setters;
                    break;
                case KnownElements.TabControl:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.TabControl_Items;
                    break;
                case KnownElements.TabItem:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.TabItem_Content;
                    break;
                case KnownElements.TabPanel:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.TabPanel_Children;
                    break;
                case KnownElements.Table:
                    if (String.CompareOrdinal(fieldName, "RowGroups") == 0)
                        return (short)KnownProperties.Table_RowGroups;
                    break;
                case KnownElements.TableCell:
                    if (String.CompareOrdinal(fieldName, "Blocks") == 0)
                        return (short)KnownProperties.TableCell_Blocks;
                    break;
                case KnownElements.TableRow:
                    if (String.CompareOrdinal(fieldName, "Cells") == 0)
                        return (short)KnownProperties.TableRow_Cells;
                    break;
                case KnownElements.TableRowGroup:
                    if (String.CompareOrdinal(fieldName, "Rows") == 0)
                        return (short)KnownProperties.TableRowGroup_Rows;
                    break;
                case KnownElements.TextBlock:
                    if (String.CompareOrdinal(fieldName, "Background") == 0)
                        return (short)KnownProperties.TextBlock_Background;
                    if (String.CompareOrdinal(fieldName, "FontFamily") == 0)
                        return (short)KnownProperties.TextBlock_FontFamily;
                    if (String.CompareOrdinal(fieldName, "FontSize") == 0)
                        return (short)KnownProperties.TextBlock_FontSize;
                    if (String.CompareOrdinal(fieldName, "FontStretch") == 0)
                        return (short)KnownProperties.TextBlock_FontStretch;
                    if (String.CompareOrdinal(fieldName, "FontStyle") == 0)
                        return (short)KnownProperties.TextBlock_FontStyle;
                    if (String.CompareOrdinal(fieldName, "FontWeight") == 0)
                        return (short)KnownProperties.TextBlock_FontWeight;
                    if (String.CompareOrdinal(fieldName, "Foreground") == 0)
                        return (short)KnownProperties.TextBlock_Foreground;
                    if (String.CompareOrdinal(fieldName, "Inlines") == 0)
                        return (short)KnownProperties.TextBlock_Inlines;
                    if (String.CompareOrdinal(fieldName, "Text") == 0)
                        return (short)KnownProperties.TextBlock_Text;
                    if (String.CompareOrdinal(fieldName, "TextDecorations") == 0)
                        return (short)KnownProperties.TextBlock_TextDecorations;
                    if (String.CompareOrdinal(fieldName, "TextTrimming") == 0)
                        return (short)KnownProperties.TextBlock_TextTrimming;
                    if (String.CompareOrdinal(fieldName, "TextWrapping") == 0)
                        return (short)KnownProperties.TextBlock_TextWrapping;
                    break;
                case KnownElements.TextBox:
                    if (String.CompareOrdinal(fieldName, "Text") == 0)
                        return (short)KnownProperties.TextBox_Text;
                    break;
                case KnownElements.TextElement:
                    if (String.CompareOrdinal(fieldName, "Background") == 0)
                        return (short)KnownProperties.TextElement_Background;
                    if (String.CompareOrdinal(fieldName, "FontFamily") == 0)
                        return (short)KnownProperties.TextElement_FontFamily;
                    if (String.CompareOrdinal(fieldName, "FontSize") == 0)
                        return (short)KnownProperties.TextElement_FontSize;
                    if (String.CompareOrdinal(fieldName, "FontStretch") == 0)
                        return (short)KnownProperties.TextElement_FontStretch;
                    if (String.CompareOrdinal(fieldName, "FontStyle") == 0)
                        return (short)KnownProperties.TextElement_FontStyle;
                    if (String.CompareOrdinal(fieldName, "FontWeight") == 0)
                        return (short)KnownProperties.TextElement_FontWeight;
                    if (String.CompareOrdinal(fieldName, "Foreground") == 0)
                        return (short)KnownProperties.TextElement_Foreground;
                    break;
                case KnownElements.ThicknessAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.ThicknessAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.TimelineGroup:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.TimelineGroup_Children;
                    break;
                case KnownElements.ToggleButton:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.ToggleButton_Content;
                    break;
                case KnownElements.ToolBar:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.ToolBar_Items;
                    break;
                case KnownElements.ToolBarOverflowPanel:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.ToolBarOverflowPanel_Children;
                    break;
                case KnownElements.ToolBarPanel:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.ToolBarPanel_Children;
                    break;
                case KnownElements.ToolBarTray:
                    if (String.CompareOrdinal(fieldName, "ToolBars") == 0)
                        return (short)KnownProperties.ToolBarTray_ToolBars;
                    break;
                case KnownElements.ToolTip:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.ToolTip_Content;
                    break;
                case KnownElements.Track:
                    if (String.CompareOrdinal(fieldName, "IsDirectionReversed") == 0)
                        return (short)KnownProperties.Track_IsDirectionReversed;
                    if (String.CompareOrdinal(fieldName, "Maximum") == 0)
                        return (short)KnownProperties.Track_Maximum;
                    if (String.CompareOrdinal(fieldName, "Minimum") == 0)
                        return (short)KnownProperties.Track_Minimum;
                    if (String.CompareOrdinal(fieldName, "Orientation") == 0)
                        return (short)KnownProperties.Track_Orientation;
                    if (String.CompareOrdinal(fieldName, "Value") == 0)
                        return (short)KnownProperties.Track_Value;
                    if (String.CompareOrdinal(fieldName, "ViewportSize") == 0)
                        return (short)KnownProperties.Track_ViewportSize;
                    break;
                case KnownElements.Transform3DGroup:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.Transform3DGroup_Children;
                    break;
                case KnownElements.TransformGroup:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.TransformGroup_Children;
                    break;
                case KnownElements.TreeView:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.TreeView_Items;
                    break;
                case KnownElements.TreeViewItem:
                    if (String.CompareOrdinal(fieldName, "Items") == 0)
                        return (short)KnownProperties.TreeViewItem_Items;
                    break;
                case KnownElements.Trigger:
                    if (String.CompareOrdinal(fieldName, "Setters") == 0)
                        return (short)KnownProperties.Trigger_Setters;
                    break;
                case KnownElements.UIElement:
                    if (String.CompareOrdinal(fieldName, "ClipToBounds") == 0)
                        return (short)KnownProperties.UIElement_ClipToBounds;
                    if (String.CompareOrdinal(fieldName, "Focusable") == 0)
                        return (short)KnownProperties.UIElement_Focusable;
                    if (String.CompareOrdinal(fieldName, "IsEnabled") == 0)
                        return (short)KnownProperties.UIElement_IsEnabled;
                    if (String.CompareOrdinal(fieldName, "RenderTransform") == 0)
                        return (short)KnownProperties.UIElement_RenderTransform;
                    if (String.CompareOrdinal(fieldName, "Visibility") == 0)
                        return (short)KnownProperties.UIElement_Visibility;
                    break;
                case KnownElements.Underline:
                    if (String.CompareOrdinal(fieldName, "Inlines") == 0)
                        return (short)KnownProperties.Underline_Inlines;
                    break;
                case KnownElements.UniformGrid:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.UniformGrid_Children;
                    break;
                case KnownElements.UserControl:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.UserControl_Content;
                    break;
                case KnownElements.Vector3DAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.Vector3DAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.VectorAnimationUsingKeyFrames:
                    if (String.CompareOrdinal(fieldName, "KeyFrames") == 0)
                        return (short)KnownProperties.VectorAnimationUsingKeyFrames_KeyFrames;
                    break;
                case KnownElements.Viewbox:
                    if (String.CompareOrdinal(fieldName, "Child") == 0)
                        return (short)KnownProperties.Viewbox_Child;
                    break;
                case KnownElements.Viewport3D:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.Viewport3D_Children;
                    break;
                case KnownElements.Viewport3DVisual:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.Viewport3DVisual_Children;
                    break;
                case KnownElements.VirtualizingPanel:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.VirtualizingPanel_Children;
                    break;
                case KnownElements.VirtualizingStackPanel:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.VirtualizingStackPanel_Children;
                    break;
                case KnownElements.Window:
                    if (String.CompareOrdinal(fieldName, "Content") == 0)
                        return (short)KnownProperties.Window_Content;
                    break;
                case KnownElements.WrapPanel:
                    if (String.CompareOrdinal(fieldName, "Children") == 0)
                        return (short)KnownProperties.WrapPanel_Children;
                    break;
                case KnownElements.XmlDataProvider:
                    if (String.CompareOrdinal(fieldName, "XmlSerializer") == 0)
                        return (short)KnownProperties.XmlDataProvider_XmlSerializer;
                    break;
            }
            return 0;
        }

        private static bool IsStandardLengthProp(string propName)
        {
            return (String.CompareOrdinal(propName, "Width") == 0 ||
                String.CompareOrdinal(propName, "MinWidth") == 0 ||
                String.CompareOrdinal(propName, "MaxWidth") == 0 ||
                String.CompareOrdinal(propName, "Height") == 0 ||
                String.CompareOrdinal(propName, "MinHeight") == 0 ||
                String.CompareOrdinal(propName, "MaxHeight") == 0);
        }

        // Look for a converter type that is associated with a known type.
        // Return KnownElements.UnknownElements if not found.
        internal static KnownElements GetKnownTypeConverterId(KnownElements knownElement)
        {
            KnownElements converterId = KnownElements.UnknownElement;
            switch (knownElement)
            {
                case KnownElements.ComponentResourceKey: converterId = KnownElements.ComponentResourceKeyConverter; break;
                case KnownElements.CornerRadius: converterId = KnownElements.CornerRadiusConverter; break;
                case KnownElements.BindingExpressionBase: converterId = KnownElements.ExpressionConverter; break;
                case KnownElements.BindingExpression: converterId = KnownElements.ExpressionConverter; break;
                case KnownElements.MultiBindingExpression: converterId = KnownElements.ExpressionConverter; break;
                case KnownElements.PriorityBindingExpression: converterId = KnownElements.ExpressionConverter; break;
                case KnownElements.TemplateKey: converterId = KnownElements.TemplateKeyConverter; break;
                case KnownElements.DataTemplateKey: converterId = KnownElements.TemplateKeyConverter; break;
                case KnownElements.DynamicResourceExtension: converterId = KnownElements.DynamicResourceExtensionConverter; break;
                case KnownElements.FigureLength: converterId = KnownElements.FigureLengthConverter; break;
                case KnownElements.GridLength: converterId = KnownElements.GridLengthConverter; break;
                case KnownElements.PropertyPath: converterId = KnownElements.PropertyPathConverter; break;
                case KnownElements.TemplateBindingExpression: converterId = KnownElements.TemplateBindingExpressionConverter; break;
                case KnownElements.TemplateBindingExtension: converterId = KnownElements.TemplateBindingExtensionConverter; break;
                case KnownElements.Thickness: converterId = KnownElements.ThicknessConverter; break;
                case KnownElements.Duration: converterId = KnownElements.DurationConverter; break;
                case KnownElements.FontStyle: converterId = KnownElements.FontStyleConverter; break;
                case KnownElements.FontStretch: converterId = KnownElements.FontStretchConverter; break;
                case KnownElements.FontWeight: converterId = KnownElements.FontWeightConverter; break;
                case KnownElements.RoutedEvent: converterId = KnownElements.RoutedEventConverter; break;
                case KnownElements.TextDecorationCollection: converterId = KnownElements.TextDecorationCollectionConverter; break;
                case KnownElements.StrokeCollection: converterId = KnownElements.StrokeCollectionConverter; break;
                case KnownElements.ICommand: converterId = KnownElements.CommandConverter; break;
                case KnownElements.KeyGesture: converterId = KnownElements.KeyGestureConverter; break;
                case KnownElements.MouseGesture: converterId = KnownElements.MouseGestureConverter; break;
                case KnownElements.RoutedCommand: converterId = KnownElements.CommandConverter; break;
                case KnownElements.RoutedUICommand: converterId = KnownElements.CommandConverter; break;
                case KnownElements.Cursor: converterId = KnownElements.CursorConverter; break;
                case KnownElements.InputScope: converterId = KnownElements.InputScopeConverter; break;
                case KnownElements.InputScopeName: converterId = KnownElements.InputScopeNameConverter; break;
                case KnownElements.KeySpline: converterId = KnownElements.KeySplineConverter; break;
                case KnownElements.KeyTime: converterId = KnownElements.KeyTimeConverter; break;
                case KnownElements.RepeatBehavior: converterId = KnownElements.RepeatBehaviorConverter; break;
                case KnownElements.Brush: converterId = KnownElements.BrushConverter; break;
                case KnownElements.Color: converterId = KnownElements.ColorConverter; break;
                case KnownElements.Geometry: converterId = KnownElements.GeometryConverter; break;
                case KnownElements.CombinedGeometry: converterId = KnownElements.GeometryConverter; break;
                case KnownElements.TileBrush: converterId = KnownElements.BrushConverter; break;
                case KnownElements.DrawingBrush: converterId = KnownElements.BrushConverter; break;
                case KnownElements.ImageSource: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.DrawingImage: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.EllipseGeometry: converterId = KnownElements.GeometryConverter; break;
                case KnownElements.FontFamily: converterId = KnownElements.FontFamilyConverter; break;
                case KnownElements.DoubleCollection: converterId = KnownElements.DoubleCollectionConverter; break;
                case KnownElements.GeometryGroup: converterId = KnownElements.GeometryConverter; break;
                case KnownElements.GradientBrush: converterId = KnownElements.BrushConverter; break;
                case KnownElements.ImageBrush: converterId = KnownElements.BrushConverter; break;
                case KnownElements.Int32Collection: converterId = KnownElements.Int32CollectionConverter; break;
                case KnownElements.LinearGradientBrush: converterId = KnownElements.BrushConverter; break;
                case KnownElements.LineGeometry: converterId = KnownElements.GeometryConverter; break;
                case KnownElements.Transform: converterId = KnownElements.TransformConverter; break;
                case KnownElements.MatrixTransform: converterId = KnownElements.TransformConverter; break;
                case KnownElements.PathFigureCollection: converterId = KnownElements.PathFigureCollectionConverter; break;
                case KnownElements.PathGeometry: converterId = KnownElements.GeometryConverter; break;
                case KnownElements.PointCollection: converterId = KnownElements.PointCollectionConverter; break;
                case KnownElements.RadialGradientBrush: converterId = KnownElements.BrushConverter; break;
                case KnownElements.RectangleGeometry: converterId = KnownElements.GeometryConverter; break;
                case KnownElements.RotateTransform: converterId = KnownElements.TransformConverter; break;
                case KnownElements.ScaleTransform: converterId = KnownElements.TransformConverter; break;
                case KnownElements.SkewTransform: converterId = KnownElements.TransformConverter; break;
                case KnownElements.SolidColorBrush: converterId = KnownElements.BrushConverter; break;
                case KnownElements.StreamGeometry: converterId = KnownElements.GeometryConverter; break;
                case KnownElements.TransformGroup: converterId = KnownElements.TransformConverter; break;
                case KnownElements.TranslateTransform: converterId = KnownElements.TransformConverter; break;
                case KnownElements.VectorCollection: converterId = KnownElements.VectorCollectionConverter; break;
                case KnownElements.VisualBrush: converterId = KnownElements.BrushConverter; break;
                case KnownElements.BitmapSource: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.BitmapFrame: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.BitmapImage: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.CachedBitmap: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.ColorConvertedBitmap: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.CroppedBitmap: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.FormatConvertedBitmap: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.RenderTargetBitmap: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.TransformedBitmap: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.WriteableBitmap: converterId = KnownElements.ImageSourceConverter; break;
                case KnownElements.PixelFormat: converterId = KnownElements.PixelFormatConverter; break;
                case KnownElements.Matrix3D: converterId = KnownElements.Matrix3DConverter; break;
                case KnownElements.Point3D: converterId = KnownElements.Point3DConverter; break;
                case KnownElements.Point3DCollection: converterId = KnownElements.Point3DCollectionConverter; break;
                case KnownElements.Vector3DCollection: converterId = KnownElements.Vector3DCollectionConverter; break;
                case KnownElements.Point4D: converterId = KnownElements.Point4DConverter; break;
                case KnownElements.Quaternion: converterId = KnownElements.QuaternionConverter; break;
                case KnownElements.Rect3D: converterId = KnownElements.Rect3DConverter; break;
                case KnownElements.Size3D: converterId = KnownElements.Size3DConverter; break;
                case KnownElements.Vector3D: converterId = KnownElements.Vector3DConverter; break;
                case KnownElements.XmlLanguage: converterId = KnownElements.XmlLanguageConverter; break;
                case KnownElements.Point: converterId = KnownElements.PointConverter; break;
                case KnownElements.Size: converterId = KnownElements.SizeConverter; break;
                case KnownElements.Vector: converterId = KnownElements.VectorConverter; break;
                case KnownElements.Rect: converterId = KnownElements.RectConverter; break;
                case KnownElements.Matrix: converterId = KnownElements.MatrixConverter; break;
                case KnownElements.DependencyProperty: converterId = KnownElements.DependencyPropertyConverter; break;
                case KnownElements.Expression: converterId = KnownElements.ExpressionConverter; break;
                case KnownElements.Int32Rect: converterId = KnownElements.Int32RectConverter; break;
                case KnownElements.Boolean: converterId = KnownElements.BooleanConverter; break;
                case KnownElements.Int16: converterId = KnownElements.Int16Converter; break;
                case KnownElements.Int32: converterId = KnownElements.Int32Converter; break;
                case KnownElements.Int64: converterId = KnownElements.Int64Converter; break;
                case KnownElements.UInt16: converterId = KnownElements.UInt16Converter; break;
                case KnownElements.UInt32: converterId = KnownElements.UInt32Converter; break;
                case KnownElements.UInt64: converterId = KnownElements.UInt64Converter; break;
                case KnownElements.Single: converterId = KnownElements.SingleConverter; break;
                case KnownElements.Double: converterId = KnownElements.DoubleConverter; break;
                case KnownElements.Object: converterId = KnownElements.StringConverter; break;
                case KnownElements.String: converterId = KnownElements.StringConverter; break;
                case KnownElements.Byte: converterId = KnownElements.ByteConverter; break;
                case KnownElements.SByte: converterId = KnownElements.SByteConverter; break;
                case KnownElements.Char: converterId = KnownElements.CharConverter; break;
                case KnownElements.Decimal: converterId = KnownElements.DecimalConverter; break;
                case KnownElements.TimeSpan: converterId = KnownElements.TimeSpanConverter; break;
                case KnownElements.Guid: converterId = KnownElements.GuidConverter; break;
                case KnownElements.DateTime: converterId = KnownElements.DateTimeConverter2; break;
                case KnownElements.Uri: converterId = KnownElements.UriTypeConverter; break;
                case KnownElements.CultureInfo: converterId = KnownElements.CultureInfoConverter; break;
            }
            return converterId;
        }

        // Look for a converter type that is associated with a known type.
        // Return KnownElements.UnknownElements if not found.
        internal static KnownElements GetKnownTypeConverterIdForProperty(
                KnownElements id,
                string        propName)
        {
            KnownElements converterId = KnownElements.UnknownElement;
            switch (id)
            {
                case KnownElements.ColumnDefinition:
                    if (String.CompareOrdinal(propName, "MinWidth") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "MaxWidth") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.RowDefinition:
                    if (String.CompareOrdinal(propName, "MinHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "MaxHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.FrameworkElement:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Adorner:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Shape:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "StrokeThickness") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Panel:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Canvas:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Left") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Top") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Right") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Bottom") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Control:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ContentControl:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Window:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Top") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Left") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "DialogResult") == 0)
                        converterId = KnownElements.DialogResultConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.NavigationWindow:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Top") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Left") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "DialogResult") == 0)
                        converterId = KnownElements.DialogResultConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.CollectionView:
                    if (String.CompareOrdinal(propName, "Culture") == 0)
                        converterId = KnownElements.CultureInfoIetfLanguageTagConverter;
                    break;
                case KnownElements.StickyNoteControl:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ItemsControl:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.MenuBase:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ContextMenu:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "HorizontalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "VerticalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.HeaderedItemsControl:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.MenuItem:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.FlowDocumentScrollViewer:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.DocumentViewerBase:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.FlowDocumentPageViewer:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.AccessText:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    else if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.AdornedElementPlaceholder:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Decorator:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Border:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.ButtonBase:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Button:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ToggleButton:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "IsChecked") == 0)
                        converterId = KnownElements.NullableBoolConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.CheckBox:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "IsChecked") == 0)
                        converterId = KnownElements.NullableBoolConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Selector:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "IsSynchronizedWithCurrentItem") == 0)
                        converterId = KnownElements.NullableBoolConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ComboBox:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "MaxDropDownHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "IsSynchronizedWithCurrentItem") == 0)
                        converterId = KnownElements.NullableBoolConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ListBoxItem:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ComboBoxItem:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ContentPresenter:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.ContextMenuService:
                    if (String.CompareOrdinal(propName, "HorizontalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "VerticalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.DockPanel:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.DocumentViewer:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.HeaderedContentControl:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Expander:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.FlowDocumentReader:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Frame:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Grid:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.GridViewColumn:
                    if (String.CompareOrdinal(propName, "Width") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.GridViewColumnHeader:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.GridViewRowPresenterBase:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.GridViewHeaderRowPresenter:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.GridViewRowPresenter:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Thumb:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.GridSplitter:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.GroupBox:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.GroupItem:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Image:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.InkCanvas:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Top") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Bottom") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Left") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Right") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.InkPresenter:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.ItemCollection:
                    if (String.CompareOrdinal(propName, "Culture") == 0)
                        converterId = KnownElements.CultureInfoIetfLanguageTagConverter;
                    break;
                case KnownElements.ItemsPresenter:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Label:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ListBox:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "IsSynchronizedWithCurrentItem") == 0)
                        converterId = KnownElements.NullableBoolConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ListView:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "IsSynchronizedWithCurrentItem") == 0)
                        converterId = KnownElements.NullableBoolConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ListViewItem:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.MediaElement:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Menu:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Page:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.PasswordBox:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.BulletDecorator:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.DocumentPageView:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Popup:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "HorizontalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "VerticalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.RangeBase:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.RepeatButton:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ResizeGrip:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ScrollBar:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ScrollContentPresenter:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.StatusBar:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.StatusBarItem:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.TabPanel:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.TextBoxBase:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.TickBar:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.ToolBarOverflowPanel:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.StackPanel:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.ToolBarPanel:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Track:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.UniformGrid:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.ProgressBar:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.RadioButton:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "IsChecked") == 0)
                        converterId = KnownElements.NullableBoolConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.RichTextBox:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ScrollViewer:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Separator:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Slider:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.TabControl:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "IsSynchronizedWithCurrentItem") == 0)
                        converterId = KnownElements.NullableBoolConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.TabItem:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.TextBlock:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    else if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.TextBox:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ToolBar:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ToolBarTray:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.ToolTip:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "HorizontalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "VerticalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ToolTipService:
                    if (String.CompareOrdinal(propName, "HorizontalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "VerticalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.TreeView:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.TreeViewItem:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.UserControl:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Viewbox:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Viewport3D:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.VirtualizingPanel:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.VirtualizingStackPanel:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.WrapPanel:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "ItemWidth") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "ItemHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Binding:
                    if (String.CompareOrdinal(propName, "ConverterCulture") == 0)
                        converterId = KnownElements.CultureInfoIetfLanguageTagConverter;
                    break;
                case KnownElements.BindingListCollectionView:
                    if (String.CompareOrdinal(propName, "Culture") == 0)
                        converterId = KnownElements.CultureInfoIetfLanguageTagConverter;
                    break;
                case KnownElements.CollectionViewSource:
                    if (String.CompareOrdinal(propName, "Culture") == 0)
                        converterId = KnownElements.CultureInfoIetfLanguageTagConverter;
                    break;
                case KnownElements.ListCollectionView:
                    if (String.CompareOrdinal(propName, "Culture") == 0)
                        converterId = KnownElements.CultureInfoIetfLanguageTagConverter;
                    break;
                case KnownElements.MultiBinding:
                    if (String.CompareOrdinal(propName, "ConverterCulture") == 0)
                        converterId = KnownElements.CultureInfoIetfLanguageTagConverter;
                    break;
                case KnownElements.AdornerDecorator:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.AdornerLayer:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.TextElement:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Inline:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.AnchoredBlock:
                    if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Block:
                    if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.BlockUIContainer:
                    if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Span:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Bold:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.DocumentReference:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Figure:
                    if (String.CompareOrdinal(propName, "HorizontalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "VerticalOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.FixedPage:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Left") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Top") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Right") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Bottom") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Floater:
                    if (String.CompareOrdinal(propName, "Width") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.FlowDocument:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    else if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "ColumnWidth") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "ColumnGap") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "ColumnRuleWidth") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "PageWidth") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "MinPageWidth") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "MaxPageWidth") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "PageHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "MinPageHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "MaxPageHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Glyphs:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontRenderingEmSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    else if (String.CompareOrdinal(propName, "OriginX") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "OriginY") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Hyperlink:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.InlineUIContainer:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Italic:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.LineBreak:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.List:
                    if (String.CompareOrdinal(propName, "MarkerOffset") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.ListItem:
                    if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.PageContent:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Paragraph:
                    if (String.CompareOrdinal(propName, "TextIndent") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Run:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Section:
                    if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Table:
                    if (String.CompareOrdinal(propName, "CellSpacing") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.TableCell:
                    if (String.CompareOrdinal(propName, "LineHeight") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.TableRow:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.TableRowGroup:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Underline:
                    if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.PageFunctionBase:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "FontSize") == 0)
                        converterId = KnownElements.FontSizeConverter;
                    break;
                case KnownElements.Ellipse:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "StrokeThickness") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Line:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "X1") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Y1") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "X2") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "Y2") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "StrokeThickness") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Path:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "StrokeThickness") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Polygon:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "StrokeThickness") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Polyline:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "StrokeThickness") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.Rectangle:
                    if (IsStandardLengthProp(propName))
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "RadiusX") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "RadiusY") == 0)
                        converterId = KnownElements.LengthConverter;
                    else if (String.CompareOrdinal(propName, "StrokeThickness") == 0)
                        converterId = KnownElements.LengthConverter;
                    break;
                case KnownElements.InputBinding:
                    if (String.CompareOrdinal(propName, "Command") == 0)
                        converterId = KnownElements.CommandConverter;
                    break;
                case KnownElements.KeyBinding:
                    if (String.CompareOrdinal(propName, "Gesture") == 0)
                        converterId = KnownElements.KeyGestureConverter;
                    else if (String.CompareOrdinal(propName, "Command") == 0)
                        converterId = KnownElements.CommandConverter;
                    break;
                case KnownElements.MouseBinding:
                    if (String.CompareOrdinal(propName, "Gesture") == 0)
                        converterId = KnownElements.MouseGestureConverter;
                    else if (String.CompareOrdinal(propName, "Command") == 0)
                        converterId = KnownElements.CommandConverter;
                    break;
                case KnownElements.InputLanguageManager:
                    if (String.CompareOrdinal(propName, "CurrentInputLanguage") == 0)
                        converterId = KnownElements.CultureInfoIetfLanguageTagConverter;
                    else if (String.CompareOrdinal(propName, "InputLanguage") == 0)
                        converterId = KnownElements.CultureInfoIetfLanguageTagConverter;
                    break;
                case KnownElements.GlyphRun:
                    if (String.CompareOrdinal(propName, "CaretStops") == 0)
                        converterId = KnownElements.BoolIListConverter;
                    else if (String.CompareOrdinal(propName, "ClusterMap") == 0)
                        converterId = KnownElements.UShortIListConverter;
                    else if (String.CompareOrdinal(propName, "Characters") == 0)
                        converterId = KnownElements.CharIListConverter;
                    else if (String.CompareOrdinal(propName, "GlyphIndices") == 0)
                        converterId = KnownElements.UShortIListConverter;
                    else if (String.CompareOrdinal(propName, "AdvanceWidths") == 0)
                        converterId = KnownElements.DoubleIListConverter;
                    else if (String.CompareOrdinal(propName, "GlyphOffsets") == 0)
                        converterId = KnownElements.PointIListConverter;
                    break;
                case KnownElements.NumberSubstitution:
                    if (String.CompareOrdinal(propName, "CultureOverride") == 0)
                        converterId = KnownElements.CultureInfoIetfLanguageTagConverter;
                    break;
            }
            return converterId;
        }
    }

    // Index class for lazy initialization of KnownTypes on the Compiler path.
    internal partial class TypeIndexer
    {
#if PBTCOMPILER
        private static bool _initialized = false;
        private static Assembly _asmFramework;
        private static Assembly _asmCore;
        private static Assembly _asmBase;

        public void Initialize(Assembly asmFramework, Assembly asmCore, Assembly asmBase)
        {
            // Paramater validation

            Debug.Assert(asmFramework != null, "asmFramework must not be null");
            Debug.Assert(asmCore != null, "asmCore must not be null");
            Debug.Assert(asmBase != null, "asmBase must not be null");

            if (!_initialized)
            {
                _asmFramework = asmFramework;
                _asmCore = asmCore;
                _asmBase = asmBase;
                _initialized = true;
            }
        }

        //  Initialize the Known WCP types from basic WCP assemblies
        private Type InitializeOneType(KnownElements knownElement)
        {
            Type t = null;
            switch(knownElement)
            {
            case KnownElements.FrameworkContentElement: t = _asmFramework.GetType("System.Windows.FrameworkContentElement"); break;
            case KnownElements.DefinitionBase: t = _asmFramework.GetType("System.Windows.Controls.DefinitionBase"); break;
            case KnownElements.ColumnDefinition: t = _asmFramework.GetType("System.Windows.Controls.ColumnDefinition"); break;
            case KnownElements.RowDefinition: t = _asmFramework.GetType("System.Windows.Controls.RowDefinition"); break;
            case KnownElements.FrameworkElement: t = _asmFramework.GetType("System.Windows.FrameworkElement"); break;
            case KnownElements.Adorner: t = _asmFramework.GetType("System.Windows.Documents.Adorner"); break;
            case KnownElements.Shape: t = _asmFramework.GetType("System.Windows.Shapes.Shape"); break;
            case KnownElements.Panel: t = _asmFramework.GetType("System.Windows.Controls.Panel"); break;
            case KnownElements.Canvas: t = _asmFramework.GetType("System.Windows.Controls.Canvas"); break;
            case KnownElements.JournalEntry: t = _asmFramework.GetType("System.Windows.Navigation.JournalEntry"); break;
            case KnownElements.Control: t = _asmFramework.GetType("System.Windows.Controls.Control"); break;
            case KnownElements.ContentControl: t = _asmFramework.GetType("System.Windows.Controls.ContentControl"); break;
            case KnownElements.Window: t = _asmFramework.GetType("System.Windows.Window"); break;
            case KnownElements.NavigationWindow: t = _asmFramework.GetType("System.Windows.Navigation.NavigationWindow"); break;
            case KnownElements.Application: t = _asmFramework.GetType("System.Windows.Application"); break;
            case KnownElements.CollectionView: t = _asmFramework.GetType("System.Windows.Data.CollectionView"); break;
            case KnownElements.StickyNoteControl: t = _asmFramework.GetType("System.Windows.Controls.StickyNoteControl"); break;
            case KnownElements.ItemsControl: t = _asmFramework.GetType("System.Windows.Controls.ItemsControl"); break;
            case KnownElements.MenuBase: t = _asmFramework.GetType("System.Windows.Controls.Primitives.MenuBase"); break;
            case KnownElements.ContextMenu: t = _asmFramework.GetType("System.Windows.Controls.ContextMenu"); break;
            case KnownElements.HeaderedItemsControl: t = _asmFramework.GetType("System.Windows.Controls.HeaderedItemsControl"); break;
            case KnownElements.MenuItem: t = _asmFramework.GetType("System.Windows.Controls.MenuItem"); break;
            case KnownElements.FlowDocumentScrollViewer: t = _asmFramework.GetType("System.Windows.Controls.FlowDocumentScrollViewer"); break;
            case KnownElements.DocumentViewerBase: t = _asmFramework.GetType("System.Windows.Controls.Primitives.DocumentViewerBase"); break;
            case KnownElements.FlowDocumentPageViewer: t = _asmFramework.GetType("System.Windows.Controls.FlowDocumentPageViewer"); break;
            case KnownElements.ResourceKey: t = _asmFramework.GetType("System.Windows.ResourceKey"); break;
            case KnownElements.ComponentResourceKey: t = _asmFramework.GetType("System.Windows.ComponentResourceKey"); break;
            case KnownElements.FrameworkTemplate: t = _asmFramework.GetType("System.Windows.FrameworkTemplate"); break;
            case KnownElements.ControlTemplate: t = _asmFramework.GetType("System.Windows.Controls.ControlTemplate"); break;
            case KnownElements.AccessText: t = _asmFramework.GetType("System.Windows.Controls.AccessText"); break;
            case KnownElements.AdornedElementPlaceholder: t = _asmFramework.GetType("System.Windows.Controls.AdornedElementPlaceholder"); break;
            case KnownElements.BooleanToVisibilityConverter: t = _asmFramework.GetType("System.Windows.Controls.BooleanToVisibilityConverter"); break;
            case KnownElements.Decorator: t = _asmFramework.GetType("System.Windows.Controls.Decorator"); break;
            case KnownElements.Border: t = _asmFramework.GetType("System.Windows.Controls.Border"); break;
            case KnownElements.BorderGapMaskConverter: t = _asmFramework.GetType("System.Windows.Controls.BorderGapMaskConverter"); break;
            case KnownElements.ButtonBase: t = _asmFramework.GetType("System.Windows.Controls.Primitives.ButtonBase"); break;
            case KnownElements.Button: t = _asmFramework.GetType("System.Windows.Controls.Button"); break;
            case KnownElements.ToggleButton: t = _asmFramework.GetType("System.Windows.Controls.Primitives.ToggleButton"); break;
            case KnownElements.CheckBox: t = _asmFramework.GetType("System.Windows.Controls.CheckBox"); break;
            case KnownElements.Selector: t = _asmFramework.GetType("System.Windows.Controls.Primitives.Selector"); break;
            case KnownElements.ComboBox: t = _asmFramework.GetType("System.Windows.Controls.ComboBox"); break;
            case KnownElements.ListBoxItem: t = _asmFramework.GetType("System.Windows.Controls.ListBoxItem"); break;
            case KnownElements.ComboBoxItem: t = _asmFramework.GetType("System.Windows.Controls.ComboBoxItem"); break;
            case KnownElements.ContentPresenter: t = _asmFramework.GetType("System.Windows.Controls.ContentPresenter"); break;
            case KnownElements.DataTemplate: t = _asmFramework.GetType("System.Windows.DataTemplate"); break;
            case KnownElements.ContextMenuService: t = _asmFramework.GetType("System.Windows.Controls.ContextMenuService"); break;
            case KnownElements.DockPanel: t = _asmFramework.GetType("System.Windows.Controls.DockPanel"); break;
            case KnownElements.DocumentViewer: t = _asmFramework.GetType("System.Windows.Controls.DocumentViewer"); break;
            case KnownElements.HeaderedContentControl: t = _asmFramework.GetType("System.Windows.Controls.HeaderedContentControl"); break;
            case KnownElements.Expander: t = _asmFramework.GetType("System.Windows.Controls.Expander"); break;
            case KnownElements.FlowDocumentReader: t = _asmFramework.GetType("System.Windows.Controls.FlowDocumentReader"); break;
            case KnownElements.Frame: t = _asmFramework.GetType("System.Windows.Controls.Frame"); break;
            case KnownElements.Grid: t = _asmFramework.GetType("System.Windows.Controls.Grid"); break;
            case KnownElements.ViewBase: t = _asmFramework.GetType("System.Windows.Controls.ViewBase"); break;
            case KnownElements.GridView: t = _asmFramework.GetType("System.Windows.Controls.GridView"); break;
            case KnownElements.GridViewColumn: t = _asmFramework.GetType("System.Windows.Controls.GridViewColumn"); break;
            case KnownElements.GridViewColumnHeader: t = _asmFramework.GetType("System.Windows.Controls.GridViewColumnHeader"); break;
            case KnownElements.GridViewRowPresenterBase: t = _asmFramework.GetType("System.Windows.Controls.Primitives.GridViewRowPresenterBase"); break;
            case KnownElements.GridViewHeaderRowPresenter: t = _asmFramework.GetType("System.Windows.Controls.GridViewHeaderRowPresenter"); break;
            case KnownElements.GridViewRowPresenter: t = _asmFramework.GetType("System.Windows.Controls.GridViewRowPresenter"); break;
            case KnownElements.Thumb: t = _asmFramework.GetType("System.Windows.Controls.Primitives.Thumb"); break;
            case KnownElements.GridSplitter: t = _asmFramework.GetType("System.Windows.Controls.GridSplitter"); break;
            case KnownElements.GroupBox: t = _asmFramework.GetType("System.Windows.Controls.GroupBox"); break;
            case KnownElements.GroupItem: t = _asmFramework.GetType("System.Windows.Controls.GroupItem"); break;
            case KnownElements.Image: t = _asmFramework.GetType("System.Windows.Controls.Image"); break;
            case KnownElements.InkCanvas: t = _asmFramework.GetType("System.Windows.Controls.InkCanvas"); break;
            case KnownElements.InkPresenter: t = _asmFramework.GetType("System.Windows.Controls.InkPresenter"); break;
            case KnownElements.ItemCollection: t = _asmFramework.GetType("System.Windows.Controls.ItemCollection"); break;
            case KnownElements.ItemsPanelTemplate: t = _asmFramework.GetType("System.Windows.Controls.ItemsPanelTemplate"); break;
            case KnownElements.ItemsPresenter: t = _asmFramework.GetType("System.Windows.Controls.ItemsPresenter"); break;
            case KnownElements.Label: t = _asmFramework.GetType("System.Windows.Controls.Label"); break;
            case KnownElements.ListBox: t = _asmFramework.GetType("System.Windows.Controls.ListBox"); break;
            case KnownElements.ListView: t = _asmFramework.GetType("System.Windows.Controls.ListView"); break;
            case KnownElements.ListViewItem: t = _asmFramework.GetType("System.Windows.Controls.ListViewItem"); break;
            case KnownElements.MediaElement: t = _asmFramework.GetType("System.Windows.Controls.MediaElement"); break;
            case KnownElements.Menu: t = _asmFramework.GetType("System.Windows.Controls.Menu"); break;
            case KnownElements.MenuScrollingVisibilityConverter: t = _asmFramework.GetType("System.Windows.Controls.MenuScrollingVisibilityConverter"); break;
            case KnownElements.Page: t = _asmFramework.GetType("System.Windows.Controls.Page"); break;
            case KnownElements.PasswordBox: t = _asmFramework.GetType("System.Windows.Controls.PasswordBox"); break;
            case KnownElements.BulletDecorator: t = _asmFramework.GetType("System.Windows.Controls.Primitives.BulletDecorator"); break;
            case KnownElements.DocumentPageView: t = _asmFramework.GetType("System.Windows.Controls.Primitives.DocumentPageView"); break;
            case KnownElements.Popup: t = _asmFramework.GetType("System.Windows.Controls.Primitives.Popup"); break;
            case KnownElements.RangeBase: t = _asmFramework.GetType("System.Windows.Controls.Primitives.RangeBase"); break;
            case KnownElements.RepeatButton: t = _asmFramework.GetType("System.Windows.Controls.Primitives.RepeatButton"); break;
            case KnownElements.ResizeGrip: t = _asmFramework.GetType("System.Windows.Controls.Primitives.ResizeGrip"); break;
            case KnownElements.ScrollBar: t = _asmFramework.GetType("System.Windows.Controls.Primitives.ScrollBar"); break;
            case KnownElements.ScrollContentPresenter: t = _asmFramework.GetType("System.Windows.Controls.ScrollContentPresenter"); break;
            case KnownElements.StatusBar: t = _asmFramework.GetType("System.Windows.Controls.Primitives.StatusBar"); break;
            case KnownElements.StatusBarItem: t = _asmFramework.GetType("System.Windows.Controls.Primitives.StatusBarItem"); break;
            case KnownElements.TabPanel: t = _asmFramework.GetType("System.Windows.Controls.Primitives.TabPanel"); break;
            case KnownElements.TextBoxBase: t = _asmFramework.GetType("System.Windows.Controls.Primitives.TextBoxBase"); break;
            case KnownElements.TickBar: t = _asmFramework.GetType("System.Windows.Controls.Primitives.TickBar"); break;
            case KnownElements.ToolBarOverflowPanel: t = _asmFramework.GetType("System.Windows.Controls.Primitives.ToolBarOverflowPanel"); break;
            case KnownElements.StackPanel: t = _asmFramework.GetType("System.Windows.Controls.StackPanel"); break;
            case KnownElements.ToolBarPanel: t = _asmFramework.GetType("System.Windows.Controls.Primitives.ToolBarPanel"); break;
            case KnownElements.Track: t = _asmFramework.GetType("System.Windows.Controls.Primitives.Track"); break;
            case KnownElements.UniformGrid: t = _asmFramework.GetType("System.Windows.Controls.Primitives.UniformGrid"); break;
            case KnownElements.ProgressBar: t = _asmFramework.GetType("System.Windows.Controls.ProgressBar"); break;
            case KnownElements.RadioButton: t = _asmFramework.GetType("System.Windows.Controls.RadioButton"); break;
            case KnownElements.RichTextBox: t = _asmFramework.GetType("System.Windows.Controls.RichTextBox"); break;
            case KnownElements.ScrollViewer: t = _asmFramework.GetType("System.Windows.Controls.ScrollViewer"); break;
            case KnownElements.Separator: t = _asmFramework.GetType("System.Windows.Controls.Separator"); break;
            case KnownElements.Slider: t = _asmFramework.GetType("System.Windows.Controls.Slider"); break;
            case KnownElements.TriggerAction: t = _asmFramework.GetType("System.Windows.TriggerAction"); break;
            case KnownElements.SoundPlayerAction: t = _asmFramework.GetType("System.Windows.Controls.SoundPlayerAction"); break;
            case KnownElements.SpellCheck: t = _asmFramework.GetType("System.Windows.Controls.SpellCheck"); break;
            case KnownElements.TabControl: t = _asmFramework.GetType("System.Windows.Controls.TabControl"); break;
            case KnownElements.TabItem: t = _asmFramework.GetType("System.Windows.Controls.TabItem"); break;
            case KnownElements.TextBlock: t = _asmFramework.GetType("System.Windows.Controls.TextBlock"); break;
            case KnownElements.TextBox: t = _asmFramework.GetType("System.Windows.Controls.TextBox"); break;
            case KnownElements.TextSearch: t = _asmFramework.GetType("System.Windows.Controls.TextSearch"); break;
            case KnownElements.ToolBar: t = _asmFramework.GetType("System.Windows.Controls.ToolBar"); break;
            case KnownElements.ToolBarTray: t = _asmFramework.GetType("System.Windows.Controls.ToolBarTray"); break;
            case KnownElements.ToolTip: t = _asmFramework.GetType("System.Windows.Controls.ToolTip"); break;
            case KnownElements.ToolTipService: t = _asmFramework.GetType("System.Windows.Controls.ToolTipService"); break;
            case KnownElements.TreeView: t = _asmFramework.GetType("System.Windows.Controls.TreeView"); break;
            case KnownElements.TreeViewItem: t = _asmFramework.GetType("System.Windows.Controls.TreeViewItem"); break;
            case KnownElements.UserControl: t = _asmFramework.GetType("System.Windows.Controls.UserControl"); break;
            case KnownElements.Validation: t = _asmFramework.GetType("System.Windows.Controls.Validation"); break;
            case KnownElements.Viewbox: t = _asmFramework.GetType("System.Windows.Controls.Viewbox"); break;
            case KnownElements.Viewport3D: t = _asmFramework.GetType("System.Windows.Controls.Viewport3D"); break;
            case KnownElements.VirtualizingPanel: t = _asmFramework.GetType("System.Windows.Controls.VirtualizingPanel"); break;
            case KnownElements.VirtualizingStackPanel: t = _asmFramework.GetType("System.Windows.Controls.VirtualizingStackPanel"); break;
            case KnownElements.WrapPanel: t = _asmFramework.GetType("System.Windows.Controls.WrapPanel"); break;
            case KnownElements.CornerRadius: t = _asmFramework.GetType("System.Windows.CornerRadius"); break;
            case KnownElements.CornerRadiusConverter: t = _asmFramework.GetType("System.Windows.CornerRadiusConverter"); break;
            case KnownElements.BindingBase: t = _asmFramework.GetType("System.Windows.Data.BindingBase"); break;
            case KnownElements.Binding: t = _asmFramework.GetType("System.Windows.Data.Binding"); break;
            case KnownElements.BindingExpressionBase: t = _asmFramework.GetType("System.Windows.Data.BindingExpressionBase"); break;
            case KnownElements.BindingExpression: t = _asmFramework.GetType("System.Windows.Data.BindingExpression"); break;
            case KnownElements.BindingListCollectionView: t = _asmFramework.GetType("System.Windows.Data.BindingListCollectionView"); break;
            case KnownElements.CollectionContainer: t = _asmFramework.GetType("System.Windows.Data.CollectionContainer"); break;
            case KnownElements.CollectionViewSource: t = _asmFramework.GetType("System.Windows.Data.CollectionViewSource"); break;
            case KnownElements.DataChangedEventManager: t = _asmFramework.GetType("System.Windows.Data.DataChangedEventManager"); break;
            case KnownElements.ListCollectionView: t = _asmFramework.GetType("System.Windows.Data.ListCollectionView"); break;
            case KnownElements.MultiBinding: t = _asmFramework.GetType("System.Windows.Data.MultiBinding"); break;
            case KnownElements.MultiBindingExpression: t = _asmFramework.GetType("System.Windows.Data.MultiBindingExpression"); break;
            case KnownElements.ObjectDataProvider: t = _asmFramework.GetType("System.Windows.Data.ObjectDataProvider"); break;
            case KnownElements.PriorityBinding: t = _asmFramework.GetType("System.Windows.Data.PriorityBinding"); break;
            case KnownElements.PriorityBindingExpression: t = _asmFramework.GetType("System.Windows.Data.PriorityBindingExpression"); break;
            case KnownElements.RelativeSource: t = _asmFramework.GetType("System.Windows.Data.RelativeSource"); break;
            case KnownElements.XmlDataProvider: t = _asmFramework.GetType("System.Windows.Data.XmlDataProvider"); break;
            case KnownElements.XmlNamespaceMapping: t = _asmFramework.GetType("System.Windows.Data.XmlNamespaceMapping"); break;
            case KnownElements.TemplateKey: t = _asmFramework.GetType("System.Windows.TemplateKey"); break;
            case KnownElements.DataTemplateKey: t = _asmFramework.GetType("System.Windows.DataTemplateKey"); break;
            case KnownElements.TriggerBase: t = _asmFramework.GetType("System.Windows.TriggerBase"); break;
            case KnownElements.DataTrigger: t = _asmFramework.GetType("System.Windows.DataTrigger"); break;
            case KnownElements.DialogResultConverter: t = _asmFramework.GetType("System.Windows.DialogResultConverter"); break;
            case KnownElements.AdornerDecorator: t = _asmFramework.GetType("System.Windows.Documents.AdornerDecorator"); break;
            case KnownElements.AdornerLayer: t = _asmFramework.GetType("System.Windows.Documents.AdornerLayer"); break;
            case KnownElements.TextElement: t = _asmFramework.GetType("System.Windows.Documents.TextElement"); break;
            case KnownElements.Inline: t = _asmFramework.GetType("System.Windows.Documents.Inline"); break;
            case KnownElements.AnchoredBlock: t = _asmFramework.GetType("System.Windows.Documents.AnchoredBlock"); break;
            case KnownElements.Block: t = _asmFramework.GetType("System.Windows.Documents.Block"); break;
            case KnownElements.BlockUIContainer: t = _asmFramework.GetType("System.Windows.Documents.BlockUIContainer"); break;
            case KnownElements.Span: t = _asmFramework.GetType("System.Windows.Documents.Span"); break;
            case KnownElements.Bold: t = _asmFramework.GetType("System.Windows.Documents.Bold"); break;
            case KnownElements.DocumentReference: t = _asmFramework.GetType("System.Windows.Documents.DocumentReference"); break;
            case KnownElements.FixedDocumentSequence: t = _asmFramework.GetType("System.Windows.Documents.FixedDocumentSequence"); break;
            case KnownElements.Figure: t = _asmFramework.GetType("System.Windows.Documents.Figure"); break;
            case KnownElements.FixedDocument: t = _asmFramework.GetType("System.Windows.Documents.FixedDocument"); break;
            case KnownElements.FixedPage: t = _asmFramework.GetType("System.Windows.Documents.FixedPage"); break;
            case KnownElements.Floater: t = _asmFramework.GetType("System.Windows.Documents.Floater"); break;
            case KnownElements.FlowDocument: t = _asmFramework.GetType("System.Windows.Documents.FlowDocument"); break;
            case KnownElements.FrameworkTextComposition: t = _asmFramework.GetType("System.Windows.Documents.FrameworkTextComposition"); break;
            case KnownElements.FrameworkRichTextComposition: t = _asmFramework.GetType("System.Windows.Documents.FrameworkRichTextComposition"); break;
            case KnownElements.Glyphs: t = _asmFramework.GetType("System.Windows.Documents.Glyphs"); break;
            case KnownElements.Hyperlink: t = _asmFramework.GetType("System.Windows.Documents.Hyperlink"); break;
            case KnownElements.InlineUIContainer: t = _asmFramework.GetType("System.Windows.Documents.InlineUIContainer"); break;
            case KnownElements.Italic: t = _asmFramework.GetType("System.Windows.Documents.Italic"); break;
            case KnownElements.LineBreak: t = _asmFramework.GetType("System.Windows.Documents.LineBreak"); break;
            case KnownElements.List: t = _asmFramework.GetType("System.Windows.Documents.List"); break;
            case KnownElements.ListItem: t = _asmFramework.GetType("System.Windows.Documents.ListItem"); break;
            case KnownElements.PageContent: t = _asmFramework.GetType("System.Windows.Documents.PageContent"); break;
            case KnownElements.Paragraph: t = _asmFramework.GetType("System.Windows.Documents.Paragraph"); break;
            case KnownElements.Run: t = _asmFramework.GetType("System.Windows.Documents.Run"); break;
            case KnownElements.Section: t = _asmFramework.GetType("System.Windows.Documents.Section"); break;
            case KnownElements.Table: t = _asmFramework.GetType("System.Windows.Documents.Table"); break;
            case KnownElements.TableCell: t = _asmFramework.GetType("System.Windows.Documents.TableCell"); break;
            case KnownElements.TableColumn: t = _asmFramework.GetType("System.Windows.Documents.TableColumn"); break;
            case KnownElements.TableRow: t = _asmFramework.GetType("System.Windows.Documents.TableRow"); break;
            case KnownElements.TableRowGroup: t = _asmFramework.GetType("System.Windows.Documents.TableRowGroup"); break;
            case KnownElements.Typography: t = _asmFramework.GetType("System.Windows.Documents.Typography"); break;
            case KnownElements.Underline: t = _asmFramework.GetType("System.Windows.Documents.Underline"); break;
            case KnownElements.ZoomPercentageConverter: t = _asmFramework.GetType("System.Windows.Documents.ZoomPercentageConverter"); break;
            case KnownElements.DynamicResourceExtension: t = _asmFramework.GetType("System.Windows.DynamicResourceExtension"); break;
            case KnownElements.DynamicResourceExtensionConverter: t = _asmFramework.GetType("System.Windows.DynamicResourceExtensionConverter"); break;
            case KnownElements.SetterBase: t = _asmFramework.GetType("System.Windows.SetterBase"); break;
            case KnownElements.EventSetter: t = _asmFramework.GetType("System.Windows.EventSetter"); break;
            case KnownElements.EventTrigger: t = _asmFramework.GetType("System.Windows.EventTrigger"); break;
            case KnownElements.FigureLength: t = _asmFramework.GetType("System.Windows.FigureLength"); break;
            case KnownElements.FigureLengthConverter: t = _asmFramework.GetType("System.Windows.FigureLengthConverter"); break;
            case KnownElements.FontSizeConverter: t = _asmFramework.GetType("System.Windows.FontSizeConverter"); break;
            case KnownElements.GridLength: t = _asmFramework.GetType("System.Windows.GridLength"); break;
            case KnownElements.GridLengthConverter: t = _asmFramework.GetType("System.Windows.GridLengthConverter"); break;
            case KnownElements.HierarchicalDataTemplate: t = _asmFramework.GetType("System.Windows.HierarchicalDataTemplate"); break;
            case KnownElements.LengthConverter: t = _asmFramework.GetType("System.Windows.LengthConverter"); break;
            case KnownElements.Localization: t = _asmFramework.GetType("System.Windows.Localization"); break;
            case KnownElements.LostFocusEventManager: t = _asmFramework.GetType("System.Windows.LostFocusEventManager"); break;
            case KnownElements.BeginStoryboard: t = _asmFramework.GetType("System.Windows.Media.Animation.BeginStoryboard"); break;
            case KnownElements.ControllableStoryboardAction: t = _asmFramework.GetType("System.Windows.Media.Animation.ControllableStoryboardAction"); break;
            case KnownElements.PauseStoryboard: t = _asmFramework.GetType("System.Windows.Media.Animation.PauseStoryboard"); break;
            case KnownElements.RemoveStoryboard: t = _asmFramework.GetType("System.Windows.Media.Animation.RemoveStoryboard"); break;
            case KnownElements.ResumeStoryboard: t = _asmFramework.GetType("System.Windows.Media.Animation.ResumeStoryboard"); break;
            case KnownElements.SeekStoryboard: t = _asmFramework.GetType("System.Windows.Media.Animation.SeekStoryboard"); break;
            case KnownElements.SetStoryboardSpeedRatio: t = _asmFramework.GetType("System.Windows.Media.Animation.SetStoryboardSpeedRatio"); break;
            case KnownElements.SkipStoryboardToFill: t = _asmFramework.GetType("System.Windows.Media.Animation.SkipStoryboardToFill"); break;
            case KnownElements.StopStoryboard: t = _asmFramework.GetType("System.Windows.Media.Animation.StopStoryboard"); break;
            case KnownElements.Storyboard: t = _asmFramework.GetType("System.Windows.Media.Animation.Storyboard"); break;
            case KnownElements.ThicknessKeyFrame: t = _asmFramework.GetType("System.Windows.Media.Animation.ThicknessKeyFrame"); break;
            case KnownElements.DiscreteThicknessKeyFrame: t = _asmFramework.GetType("System.Windows.Media.Animation.DiscreteThicknessKeyFrame"); break;
            case KnownElements.LinearThicknessKeyFrame: t = _asmFramework.GetType("System.Windows.Media.Animation.LinearThicknessKeyFrame"); break;
            case KnownElements.SplineThicknessKeyFrame: t = _asmFramework.GetType("System.Windows.Media.Animation.SplineThicknessKeyFrame"); break;
            case KnownElements.ThicknessAnimationBase: t = _asmFramework.GetType("System.Windows.Media.Animation.ThicknessAnimationBase"); break;
            case KnownElements.ThicknessAnimation: t = _asmFramework.GetType("System.Windows.Media.Animation.ThicknessAnimation"); break;
            case KnownElements.ThicknessAnimationUsingKeyFrames: t = _asmFramework.GetType("System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames"); break;
            case KnownElements.ThicknessKeyFrameCollection: t = _asmFramework.GetType("System.Windows.Media.Animation.ThicknessKeyFrameCollection"); break;
            case KnownElements.MultiDataTrigger: t = _asmFramework.GetType("System.Windows.MultiDataTrigger"); break;
            case KnownElements.MultiTrigger: t = _asmFramework.GetType("System.Windows.MultiTrigger"); break;
            case KnownElements.NameScope: t = _asmFramework.GetType("System.Windows.NameScope"); break;
            case KnownElements.JournalEntryListConverter: t = _asmFramework.GetType("System.Windows.Navigation.JournalEntryListConverter"); break;
            case KnownElements.JournalEntryUnifiedViewConverter: t = _asmFramework.GetType("System.Windows.Navigation.JournalEntryUnifiedViewConverter"); break;
            case KnownElements.PageFunctionBase: t = _asmFramework.GetType("System.Windows.Navigation.PageFunctionBase"); break;
            case KnownElements.NullableBoolConverter: t = _asmFramework.GetType("System.Windows.NullableBoolConverter"); break;
            case KnownElements.PropertyPath: t = _asmFramework.GetType("System.Windows.PropertyPath"); break;
            case KnownElements.PropertyPathConverter: t = _asmFramework.GetType("System.Windows.PropertyPathConverter"); break;
            case KnownElements.ResourceDictionary: t = _asmFramework.GetType("System.Windows.ResourceDictionary"); break;
            case KnownElements.ColorConvertedBitmapExtension: t = _asmFramework.GetType("System.Windows.ColorConvertedBitmapExtension"); break;
            case KnownElements.StaticResourceExtension: t = _asmFramework.GetType("System.Windows.StaticResourceExtension"); break;
            case KnownElements.Setter: t = _asmFramework.GetType("System.Windows.Setter"); break;
            case KnownElements.Ellipse: t = _asmFramework.GetType("System.Windows.Shapes.Ellipse"); break;
            case KnownElements.Line: t = _asmFramework.GetType("System.Windows.Shapes.Line"); break;
            case KnownElements.Path: t = _asmFramework.GetType("System.Windows.Shapes.Path"); break;
            case KnownElements.Polygon: t = _asmFramework.GetType("System.Windows.Shapes.Polygon"); break;
            case KnownElements.Polyline: t = _asmFramework.GetType("System.Windows.Shapes.Polyline"); break;
            case KnownElements.Rectangle: t = _asmFramework.GetType("System.Windows.Shapes.Rectangle"); break;
            case KnownElements.Style: t = _asmFramework.GetType("System.Windows.Style"); break;
            case KnownElements.TemplateBindingExpression: t = _asmFramework.GetType("System.Windows.TemplateBindingExpression"); break;
            case KnownElements.TemplateBindingExpressionConverter: t = _asmFramework.GetType("System.Windows.TemplateBindingExpressionConverter"); break;
            case KnownElements.TemplateBindingExtension: t = _asmFramework.GetType("System.Windows.TemplateBindingExtension"); break;
            case KnownElements.TemplateBindingExtensionConverter: t = _asmFramework.GetType("System.Windows.TemplateBindingExtensionConverter"); break;
            case KnownElements.ThemeDictionaryExtension: t = _asmFramework.GetType("System.Windows.ThemeDictionaryExtension"); break;
            case KnownElements.Thickness: t = _asmFramework.GetType("System.Windows.Thickness"); break;
            case KnownElements.ThicknessConverter: t = _asmFramework.GetType("System.Windows.ThicknessConverter"); break;
            case KnownElements.Trigger: t = _asmFramework.GetType("System.Windows.Trigger"); break;
            case KnownElements.BaseIListConverter: t = _asmCore.GetType("System.Windows.Media.Converters.BaseIListConverter"); break;
            case KnownElements.DoubleIListConverter: t = _asmCore.GetType("System.Windows.Media.Converters.DoubleIListConverter"); break;
            case KnownElements.UShortIListConverter: t = _asmCore.GetType("System.Windows.Media.Converters.UShortIListConverter"); break;
            case KnownElements.BoolIListConverter: t = _asmCore.GetType("System.Windows.Media.Converters.BoolIListConverter"); break;
            case KnownElements.PointIListConverter: t = _asmCore.GetType("System.Windows.Media.Converters.PointIListConverter"); break;
            case KnownElements.CharIListConverter: t = _asmCore.GetType("System.Windows.Media.Converters.CharIListConverter"); break;
            case KnownElements.Visual: t = _asmCore.GetType("System.Windows.Media.Visual"); break;
            case KnownElements.ContainerVisual: t = _asmCore.GetType("System.Windows.Media.ContainerVisual"); break;
            case KnownElements.DrawingVisual: t = _asmCore.GetType("System.Windows.Media.DrawingVisual"); break;
            case KnownElements.StreamGeometryContext: t = _asmCore.GetType("System.Windows.Media.StreamGeometryContext"); break;
            case KnownElements.Animatable: t = _asmCore.GetType("System.Windows.Media.Animation.Animatable"); break;
            case KnownElements.GeneralTransform: t = _asmCore.GetType("System.Windows.Media.GeneralTransform"); break;
            case KnownElements.ContentElement: t = _asmCore.GetType("System.Windows.ContentElement"); break;
            case KnownElements.CultureInfoIetfLanguageTagConverter: t = _asmCore.GetType("System.Windows.CultureInfoIetfLanguageTagConverter"); break;
            case KnownElements.Duration: t = _asmCore.GetType("System.Windows.Duration"); break;
            case KnownElements.DurationConverter: t = _asmCore.GetType("System.Windows.DurationConverter"); break;
            case KnownElements.FontStyle: t = _asmCore.GetType("System.Windows.FontStyle"); break;
            case KnownElements.FontStyleConverter: t = _asmCore.GetType("System.Windows.FontStyleConverter"); break;
            case KnownElements.FontStretch: t = _asmCore.GetType("System.Windows.FontStretch"); break;
            case KnownElements.FontStretchConverter: t = _asmCore.GetType("System.Windows.FontStretchConverter"); break;
            case KnownElements.FontWeight: t = _asmCore.GetType("System.Windows.FontWeight"); break;
            case KnownElements.FontWeightConverter: t = _asmCore.GetType("System.Windows.FontWeightConverter"); break;
            case KnownElements.UIElement: t = _asmCore.GetType("System.Windows.UIElement"); break;
            case KnownElements.Visual3D: t = _asmCore.GetType("System.Windows.Media.Media3D.Visual3D"); break;
            case KnownElements.RoutedEvent: t = _asmCore.GetType("System.Windows.RoutedEvent"); break;
            case KnownElements.TextDecoration: t = _asmCore.GetType("System.Windows.TextDecoration"); break;
            case KnownElements.TextDecorationCollection: t = _asmCore.GetType("System.Windows.TextDecorationCollection"); break;
            case KnownElements.TextDecorationCollectionConverter: t = _asmCore.GetType("System.Windows.TextDecorationCollectionConverter"); break;
            case KnownElements.GestureRecognizer: t = _asmCore.GetType("System.Windows.Ink.GestureRecognizer"); break;
            case KnownElements.StrokeCollection: t = _asmCore.GetType("System.Windows.Ink.StrokeCollection"); break;
            case KnownElements.StrokeCollectionConverter: t = _asmCore.GetType("System.Windows.StrokeCollectionConverter"); break;
            case KnownElements.InputDevice: t = _asmCore.GetType("System.Windows.Input.InputDevice"); break;
            case KnownElements.ICommand: t = _asmCore.GetType("System.Windows.Input.ICommand"); break;
            case KnownElements.InputBinding: t = _asmCore.GetType("System.Windows.Input.InputBinding"); break;
            case KnownElements.KeyBinding: t = _asmCore.GetType("System.Windows.Input.KeyBinding"); break;
            case KnownElements.KeyGesture: t = _asmCore.GetType("System.Windows.Input.KeyGesture"); break;
            case KnownElements.KeyGestureConverter: t = _asmCore.GetType("System.Windows.Input.KeyGestureConverter"); break;
            case KnownElements.MouseActionConverter: t = _asmCore.GetType("System.Windows.Input.MouseActionConverter"); break;
            case KnownElements.MouseBinding: t = _asmCore.GetType("System.Windows.Input.MouseBinding"); break;
            case KnownElements.MouseGesture: t = _asmCore.GetType("System.Windows.Input.MouseGesture"); break;
            case KnownElements.MouseGestureConverter: t = _asmCore.GetType("System.Windows.Input.MouseGestureConverter"); break;
            case KnownElements.RoutedCommand: t = _asmCore.GetType("System.Windows.Input.RoutedCommand"); break;
            case KnownElements.RoutedUICommand: t = _asmCore.GetType("System.Windows.Input.RoutedUICommand"); break;
            case KnownElements.Cursor: t = _asmCore.GetType("System.Windows.Input.Cursor"); break;
            case KnownElements.CursorConverter: t = _asmCore.GetType("System.Windows.Input.CursorConverter"); break;
            case KnownElements.TextComposition: t = _asmCore.GetType("System.Windows.Input.TextComposition"); break;
            case KnownElements.FocusManager: t = _asmCore.GetType("System.Windows.Input.FocusManager"); break;
            case KnownElements.InputLanguageManager: t = _asmCore.GetType("System.Windows.Input.InputLanguageManager"); break;
            case KnownElements.InputManager: t = _asmCore.GetType("System.Windows.Input.InputManager"); break;
            case KnownElements.InputMethod: t = _asmCore.GetType("System.Windows.Input.InputMethod"); break;
            case KnownElements.InputScope: t = _asmCore.GetType("System.Windows.Input.InputScope"); break;
            case KnownElements.InputScopeName: t = _asmCore.GetType("System.Windows.Input.InputScopeName"); break;
            case KnownElements.InputScopeConverter: t = _asmCore.GetType("System.Windows.Input.InputScopeConverter"); break;
            case KnownElements.InputScopeNameConverter: t = _asmCore.GetType("System.Windows.Input.InputScopeNameConverter"); break;
            case KnownElements.KeyboardDevice: t = _asmCore.GetType("System.Windows.Input.KeyboardDevice"); break;
            case KnownElements.MouseDevice: t = _asmCore.GetType("System.Windows.Input.MouseDevice"); break;
            case KnownElements.HostVisual: t = _asmCore.GetType("System.Windows.Media.HostVisual"); break;
            case KnownElements.Stylus: t = _asmCore.GetType("System.Windows.Input.Stylus"); break;
            case KnownElements.StylusDevice: t = _asmCore.GetType("System.Windows.Input.StylusDevice"); break;
            case KnownElements.TabletDevice: t = _asmCore.GetType("System.Windows.Input.TabletDevice"); break;
            case KnownElements.TextCompositionManager: t = _asmCore.GetType("System.Windows.Input.TextCompositionManager"); break;
            case KnownElements.CompositionTarget: t = _asmCore.GetType("System.Windows.Media.CompositionTarget"); break;
            case KnownElements.PresentationSource: t = _asmCore.GetType("System.Windows.PresentationSource"); break;
            case KnownElements.Clock: t = _asmCore.GetType("System.Windows.Media.Animation.Clock"); break;
            case KnownElements.AnimationClock: t = _asmCore.GetType("System.Windows.Media.Animation.AnimationClock"); break;
            case KnownElements.Timeline: t = _asmCore.GetType("System.Windows.Media.Animation.Timeline"); break;
            case KnownElements.AnimationTimeline: t = _asmCore.GetType("System.Windows.Media.Animation.AnimationTimeline"); break;
            case KnownElements.ClockController: t = _asmCore.GetType("System.Windows.Media.Animation.ClockController"); break;
            case KnownElements.ClockGroup: t = _asmCore.GetType("System.Windows.Media.Animation.ClockGroup"); break;
            case KnownElements.DoubleAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.DoubleAnimationBase"); break;
            case KnownElements.DoubleAnimationUsingPath: t = _asmCore.GetType("System.Windows.Media.Animation.DoubleAnimationUsingPath"); break;
            case KnownElements.BooleanAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.BooleanAnimationBase"); break;
            case KnownElements.BooleanAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.BooleanAnimationUsingKeyFrames"); break;
            case KnownElements.BooleanKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.BooleanKeyFrameCollection"); break;
            case KnownElements.ByteAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.ByteAnimationBase"); break;
            case KnownElements.ByteAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.ByteAnimation"); break;
            case KnownElements.ByteAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.ByteAnimationUsingKeyFrames"); break;
            case KnownElements.ByteKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.ByteKeyFrameCollection"); break;
            case KnownElements.CharAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.CharAnimationBase"); break;
            case KnownElements.CharAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.CharAnimationUsingKeyFrames"); break;
            case KnownElements.CharKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.CharKeyFrameCollection"); break;
            case KnownElements.ColorAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.ColorAnimationBase"); break;
            case KnownElements.ColorAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.ColorAnimation"); break;
            case KnownElements.ColorAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.ColorAnimationUsingKeyFrames"); break;
            case KnownElements.ColorKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.ColorKeyFrameCollection"); break;
            case KnownElements.DecimalAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.DecimalAnimationBase"); break;
            case KnownElements.DecimalAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.DecimalAnimation"); break;
            case KnownElements.DecimalAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.DecimalAnimationUsingKeyFrames"); break;
            case KnownElements.DecimalKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.DecimalKeyFrameCollection"); break;
            case KnownElements.BooleanKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.BooleanKeyFrame"); break;
            case KnownElements.DiscreteBooleanKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteBooleanKeyFrame"); break;
            case KnownElements.ByteKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.ByteKeyFrame"); break;
            case KnownElements.DiscreteByteKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteByteKeyFrame"); break;
            case KnownElements.CharKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.CharKeyFrame"); break;
            case KnownElements.DiscreteCharKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteCharKeyFrame"); break;
            case KnownElements.ColorKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.ColorKeyFrame"); break;
            case KnownElements.DiscreteColorKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteColorKeyFrame"); break;
            case KnownElements.DecimalKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DecimalKeyFrame"); break;
            case KnownElements.DiscreteDecimalKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteDecimalKeyFrame"); break;
            case KnownElements.DoubleKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DoubleKeyFrame"); break;
            case KnownElements.DiscreteDoubleKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteDoubleKeyFrame"); break;
            case KnownElements.Int16KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.Int16KeyFrame"); break;
            case KnownElements.DiscreteInt16KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteInt16KeyFrame"); break;
            case KnownElements.Int32KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.Int32KeyFrame"); break;
            case KnownElements.DiscreteInt32KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteInt32KeyFrame"); break;
            case KnownElements.Int64KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.Int64KeyFrame"); break;
            case KnownElements.DiscreteInt64KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteInt64KeyFrame"); break;
            case KnownElements.MatrixKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.MatrixKeyFrame"); break;
            case KnownElements.DiscreteMatrixKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteMatrixKeyFrame"); break;
            case KnownElements.ObjectKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.ObjectKeyFrame"); break;
            case KnownElements.DiscreteObjectKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteObjectKeyFrame"); break;
            case KnownElements.PointKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.PointKeyFrame"); break;
            case KnownElements.DiscretePointKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscretePointKeyFrame"); break;
            case KnownElements.Point3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.Point3DKeyFrame"); break;
            case KnownElements.DiscretePoint3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscretePoint3DKeyFrame"); break;
            case KnownElements.QuaternionKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.QuaternionKeyFrame"); break;
            case KnownElements.DiscreteQuaternionKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteQuaternionKeyFrame"); break;
            case KnownElements.Rotation3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.Rotation3DKeyFrame"); break;
            case KnownElements.DiscreteRotation3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteRotation3DKeyFrame"); break;
            case KnownElements.RectKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.RectKeyFrame"); break;
            case KnownElements.DiscreteRectKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteRectKeyFrame"); break;
            case KnownElements.SingleKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SingleKeyFrame"); break;
            case KnownElements.DiscreteSingleKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteSingleKeyFrame"); break;
            case KnownElements.SizeKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SizeKeyFrame"); break;
            case KnownElements.DiscreteSizeKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteSizeKeyFrame"); break;
            case KnownElements.StringKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.StringKeyFrame"); break;
            case KnownElements.DiscreteStringKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteStringKeyFrame"); break;
            case KnownElements.VectorKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.VectorKeyFrame"); break;
            case KnownElements.DiscreteVectorKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteVectorKeyFrame"); break;
            case KnownElements.Vector3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.Vector3DKeyFrame"); break;
            case KnownElements.DiscreteVector3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.DiscreteVector3DKeyFrame"); break;
            case KnownElements.DoubleAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.DoubleAnimation"); break;
            case KnownElements.DoubleAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames"); break;
            case KnownElements.DoubleKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.DoubleKeyFrameCollection"); break;
            case KnownElements.Int16AnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.Int16AnimationBase"); break;
            case KnownElements.Int16Animation: t = _asmCore.GetType("System.Windows.Media.Animation.Int16Animation"); break;
            case KnownElements.Int16AnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.Int16AnimationUsingKeyFrames"); break;
            case KnownElements.Int16KeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.Int16KeyFrameCollection"); break;
            case KnownElements.Int32AnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.Int32AnimationBase"); break;
            case KnownElements.Int32Animation: t = _asmCore.GetType("System.Windows.Media.Animation.Int32Animation"); break;
            case KnownElements.Int32AnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.Int32AnimationUsingKeyFrames"); break;
            case KnownElements.Int32KeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.Int32KeyFrameCollection"); break;
            case KnownElements.Int64AnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.Int64AnimationBase"); break;
            case KnownElements.Int64Animation: t = _asmCore.GetType("System.Windows.Media.Animation.Int64Animation"); break;
            case KnownElements.Int64AnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.Int64AnimationUsingKeyFrames"); break;
            case KnownElements.Int64KeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.Int64KeyFrameCollection"); break;
            case KnownElements.LinearByteKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearByteKeyFrame"); break;
            case KnownElements.LinearColorKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearColorKeyFrame"); break;
            case KnownElements.LinearDecimalKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearDecimalKeyFrame"); break;
            case KnownElements.LinearDoubleKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearDoubleKeyFrame"); break;
            case KnownElements.LinearInt16KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearInt16KeyFrame"); break;
            case KnownElements.LinearInt32KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearInt32KeyFrame"); break;
            case KnownElements.LinearInt64KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearInt64KeyFrame"); break;
            case KnownElements.LinearPointKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearPointKeyFrame"); break;
            case KnownElements.LinearPoint3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearPoint3DKeyFrame"); break;
            case KnownElements.LinearQuaternionKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearQuaternionKeyFrame"); break;
            case KnownElements.LinearRotation3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearRotation3DKeyFrame"); break;
            case KnownElements.LinearRectKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearRectKeyFrame"); break;
            case KnownElements.LinearSingleKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearSingleKeyFrame"); break;
            case KnownElements.LinearSizeKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearSizeKeyFrame"); break;
            case KnownElements.LinearVectorKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearVectorKeyFrame"); break;
            case KnownElements.LinearVector3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.LinearVector3DKeyFrame"); break;
            case KnownElements.MatrixAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.MatrixAnimationBase"); break;
            case KnownElements.MatrixAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.MatrixAnimationUsingKeyFrames"); break;
            case KnownElements.MatrixKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.MatrixKeyFrameCollection"); break;
            case KnownElements.ObjectAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.ObjectAnimationBase"); break;
            case KnownElements.ObjectAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.ObjectAnimationUsingKeyFrames"); break;
            case KnownElements.ObjectKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.ObjectKeyFrameCollection"); break;
            case KnownElements.TimelineGroup: t = _asmCore.GetType("System.Windows.Media.Animation.TimelineGroup"); break;
            case KnownElements.ParallelTimeline: t = _asmCore.GetType("System.Windows.Media.Animation.ParallelTimeline"); break;
            case KnownElements.Point3DAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.Point3DAnimationBase"); break;
            case KnownElements.Point3DAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.Point3DAnimation"); break;
            case KnownElements.Point3DAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.Point3DAnimationUsingKeyFrames"); break;
            case KnownElements.Point3DKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.Point3DKeyFrameCollection"); break;
            case KnownElements.PointAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.PointAnimationBase"); break;
            case KnownElements.PointAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.PointAnimation"); break;
            case KnownElements.PointAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.PointAnimationUsingKeyFrames"); break;
            case KnownElements.PointKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.PointKeyFrameCollection"); break;
            case KnownElements.QuaternionAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.QuaternionAnimationBase"); break;
            case KnownElements.QuaternionAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.QuaternionAnimation"); break;
            case KnownElements.QuaternionAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.QuaternionAnimationUsingKeyFrames"); break;
            case KnownElements.QuaternionKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.QuaternionKeyFrameCollection"); break;
            case KnownElements.RectAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.RectAnimationBase"); break;
            case KnownElements.RectAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.RectAnimation"); break;
            case KnownElements.RectAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.RectAnimationUsingKeyFrames"); break;
            case KnownElements.RectKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.RectKeyFrameCollection"); break;
            case KnownElements.Rotation3DAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.Rotation3DAnimationBase"); break;
            case KnownElements.Rotation3DAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.Rotation3DAnimation"); break;
            case KnownElements.Rotation3DAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.Rotation3DAnimationUsingKeyFrames"); break;
            case KnownElements.Rotation3DKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.Rotation3DKeyFrameCollection"); break;
            case KnownElements.SingleAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.SingleAnimationBase"); break;
            case KnownElements.SingleAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.SingleAnimation"); break;
            case KnownElements.SingleAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.SingleAnimationUsingKeyFrames"); break;
            case KnownElements.SingleKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.SingleKeyFrameCollection"); break;
            case KnownElements.SizeAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.SizeAnimationBase"); break;
            case KnownElements.SizeAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.SizeAnimation"); break;
            case KnownElements.SizeAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.SizeAnimationUsingKeyFrames"); break;
            case KnownElements.SizeKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.SizeKeyFrameCollection"); break;
            case KnownElements.SplineByteKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineByteKeyFrame"); break;
            case KnownElements.SplineColorKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineColorKeyFrame"); break;
            case KnownElements.SplineDecimalKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineDecimalKeyFrame"); break;
            case KnownElements.SplineDoubleKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineDoubleKeyFrame"); break;
            case KnownElements.SplineInt16KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineInt16KeyFrame"); break;
            case KnownElements.SplineInt32KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineInt32KeyFrame"); break;
            case KnownElements.SplineInt64KeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineInt64KeyFrame"); break;
            case KnownElements.SplinePointKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplinePointKeyFrame"); break;
            case KnownElements.SplinePoint3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplinePoint3DKeyFrame"); break;
            case KnownElements.SplineQuaternionKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineQuaternionKeyFrame"); break;
            case KnownElements.SplineRotation3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineRotation3DKeyFrame"); break;
            case KnownElements.SplineRectKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineRectKeyFrame"); break;
            case KnownElements.SplineSingleKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineSingleKeyFrame"); break;
            case KnownElements.SplineSizeKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineSizeKeyFrame"); break;
            case KnownElements.SplineVectorKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineVectorKeyFrame"); break;
            case KnownElements.SplineVector3DKeyFrame: t = _asmCore.GetType("System.Windows.Media.Animation.SplineVector3DKeyFrame"); break;
            case KnownElements.StringAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.StringAnimationBase"); break;
            case KnownElements.StringAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.StringAnimationUsingKeyFrames"); break;
            case KnownElements.StringKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.StringKeyFrameCollection"); break;
            case KnownElements.TimelineCollection: t = _asmCore.GetType("System.Windows.Media.Animation.TimelineCollection"); break;
            case KnownElements.Vector3DAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.Vector3DAnimationBase"); break;
            case KnownElements.Vector3DAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.Vector3DAnimation"); break;
            case KnownElements.Vector3DAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.Vector3DAnimationUsingKeyFrames"); break;
            case KnownElements.Vector3DKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.Vector3DKeyFrameCollection"); break;
            case KnownElements.VectorAnimationBase: t = _asmCore.GetType("System.Windows.Media.Animation.VectorAnimationBase"); break;
            case KnownElements.VectorAnimation: t = _asmCore.GetType("System.Windows.Media.Animation.VectorAnimation"); break;
            case KnownElements.VectorAnimationUsingKeyFrames: t = _asmCore.GetType("System.Windows.Media.Animation.VectorAnimationUsingKeyFrames"); break;
            case KnownElements.VectorKeyFrameCollection: t = _asmCore.GetType("System.Windows.Media.Animation.VectorKeyFrameCollection"); break;
            case KnownElements.KeySpline: t = _asmCore.GetType("System.Windows.Media.Animation.KeySpline"); break;
            case KnownElements.KeySplineConverter: t = _asmCore.GetType("System.Windows.KeySplineConverter"); break;
            case KnownElements.KeyTime: t = _asmCore.GetType("System.Windows.Media.Animation.KeyTime"); break;
            case KnownElements.KeyTimeConverter: t = _asmCore.GetType("System.Windows.KeyTimeConverter"); break;
            case KnownElements.MatrixAnimationUsingPath: t = _asmCore.GetType("System.Windows.Media.Animation.MatrixAnimationUsingPath"); break;
            case KnownElements.PointAnimationUsingPath: t = _asmCore.GetType("System.Windows.Media.Animation.PointAnimationUsingPath"); break;
            case KnownElements.RepeatBehavior: t = _asmCore.GetType("System.Windows.Media.Animation.RepeatBehavior"); break;
            case KnownElements.RepeatBehaviorConverter: t = _asmCore.GetType("System.Windows.Media.Animation.RepeatBehaviorConverter"); break;
            case KnownElements.PathSegment: t = _asmCore.GetType("System.Windows.Media.PathSegment"); break;
            case KnownElements.ArcSegment: t = _asmCore.GetType("System.Windows.Media.ArcSegment"); break;
            case KnownElements.BezierSegment: t = _asmCore.GetType("System.Windows.Media.BezierSegment"); break;
            case KnownElements.DrawingContext: t = _asmCore.GetType("System.Windows.Media.DrawingContext"); break;
            case KnownElements.Brush: t = _asmCore.GetType("System.Windows.Media.Brush"); break;
            case KnownElements.Color: t = _asmCore.GetType("System.Windows.Media.Color"); break;
            case KnownElements.ColorConverter: t = _asmCore.GetType("System.Windows.Media.ColorConverter"); break;
            case KnownElements.Geometry: t = _asmCore.GetType("System.Windows.Media.Geometry"); break;
            case KnownElements.CombinedGeometry: t = _asmCore.GetType("System.Windows.Media.CombinedGeometry"); break;
            case KnownElements.DashStyle: t = _asmCore.GetType("System.Windows.Media.DashStyle"); break;
            case KnownElements.Drawing: t = _asmCore.GetType("System.Windows.Media.Drawing"); break;
            case KnownElements.TileBrush: t = _asmCore.GetType("System.Windows.Media.TileBrush"); break;
            case KnownElements.DrawingBrush: t = _asmCore.GetType("System.Windows.Media.DrawingBrush"); break;
            case KnownElements.DrawingCollection: t = _asmCore.GetType("System.Windows.Media.DrawingCollection"); break;
            case KnownElements.DrawingGroup: t = _asmCore.GetType("System.Windows.Media.DrawingGroup"); break;
            case KnownElements.ImageSource: t = _asmCore.GetType("System.Windows.Media.ImageSource"); break;
            case KnownElements.DrawingImage: t = _asmCore.GetType("System.Windows.Media.DrawingImage"); break;
            case KnownElements.BitmapEffect: t = _asmCore.GetType("System.Windows.Media.Effects.BitmapEffect"); break;
            case KnownElements.BitmapEffectGroup: t = _asmCore.GetType("System.Windows.Media.Effects.BitmapEffectGroup"); break;
            case KnownElements.BitmapEffectInput: t = _asmCore.GetType("System.Windows.Media.Effects.BitmapEffectInput"); break;
            case KnownElements.BevelBitmapEffect: t = _asmCore.GetType("System.Windows.Media.Effects.BevelBitmapEffect"); break;
            case KnownElements.BlurBitmapEffect: t = _asmCore.GetType("System.Windows.Media.Effects.BlurBitmapEffect"); break;
            case KnownElements.DropShadowBitmapEffect: t = _asmCore.GetType("System.Windows.Media.Effects.DropShadowBitmapEffect"); break;
            case KnownElements.EmbossBitmapEffect: t = _asmCore.GetType("System.Windows.Media.Effects.EmbossBitmapEffect"); break;
            case KnownElements.OuterGlowBitmapEffect: t = _asmCore.GetType("System.Windows.Media.Effects.OuterGlowBitmapEffect"); break;
            case KnownElements.BitmapEffectCollection: t = _asmCore.GetType("System.Windows.Media.Effects.BitmapEffectCollection"); break;
            case KnownElements.EllipseGeometry: t = _asmCore.GetType("System.Windows.Media.EllipseGeometry"); break;
            case KnownElements.FontFamily: t = _asmCore.GetType("System.Windows.Media.FontFamily"); break;
            case KnownElements.FontFamilyConverter: t = _asmCore.GetType("System.Windows.Media.FontFamilyConverter"); break;
            case KnownElements.GeneralTransformGroup: t = _asmCore.GetType("System.Windows.Media.GeneralTransformGroup"); break;
            case KnownElements.BrushConverter: t = _asmCore.GetType("System.Windows.Media.BrushConverter"); break;
            case KnownElements.DoubleCollection: t = _asmCore.GetType("System.Windows.Media.DoubleCollection"); break;
            case KnownElements.DoubleCollectionConverter: t = _asmCore.GetType("System.Windows.Media.DoubleCollectionConverter"); break;
            case KnownElements.GeneralTransformCollection: t = _asmCore.GetType("System.Windows.Media.GeneralTransformCollection"); break;
            case KnownElements.GeometryCollection: t = _asmCore.GetType("System.Windows.Media.GeometryCollection"); break;
            case KnownElements.GeometryConverter: t = _asmCore.GetType("System.Windows.Media.GeometryConverter"); break;
            case KnownElements.GeometryDrawing: t = _asmCore.GetType("System.Windows.Media.GeometryDrawing"); break;
            case KnownElements.GeometryGroup: t = _asmCore.GetType("System.Windows.Media.GeometryGroup"); break;
            case KnownElements.GlyphRunDrawing: t = _asmCore.GetType("System.Windows.Media.GlyphRunDrawing"); break;
            case KnownElements.GradientBrush: t = _asmCore.GetType("System.Windows.Media.GradientBrush"); break;
            case KnownElements.GradientStop: t = _asmCore.GetType("System.Windows.Media.GradientStop"); break;
            case KnownElements.GradientStopCollection: t = _asmCore.GetType("System.Windows.Media.GradientStopCollection"); break;
            case KnownElements.ImageBrush: t = _asmCore.GetType("System.Windows.Media.ImageBrush"); break;
            case KnownElements.ImageDrawing: t = _asmCore.GetType("System.Windows.Media.ImageDrawing"); break;
            case KnownElements.Int32Collection: t = _asmCore.GetType("System.Windows.Media.Int32Collection"); break;
            case KnownElements.Int32CollectionConverter: t = _asmCore.GetType("System.Windows.Media.Int32CollectionConverter"); break;
            case KnownElements.LinearGradientBrush: t = _asmCore.GetType("System.Windows.Media.LinearGradientBrush"); break;
            case KnownElements.LineGeometry: t = _asmCore.GetType("System.Windows.Media.LineGeometry"); break;
            case KnownElements.LineSegment: t = _asmCore.GetType("System.Windows.Media.LineSegment"); break;
            case KnownElements.Transform: t = _asmCore.GetType("System.Windows.Media.Transform"); break;
            case KnownElements.MatrixTransform: t = _asmCore.GetType("System.Windows.Media.MatrixTransform"); break;
            case KnownElements.MediaTimeline: t = _asmCore.GetType("System.Windows.Media.MediaTimeline"); break;
            case KnownElements.PathFigure: t = _asmCore.GetType("System.Windows.Media.PathFigure"); break;
            case KnownElements.PathFigureCollection: t = _asmCore.GetType("System.Windows.Media.PathFigureCollection"); break;
            case KnownElements.PathFigureCollectionConverter: t = _asmCore.GetType("System.Windows.Media.PathFigureCollectionConverter"); break;
            case KnownElements.PathGeometry: t = _asmCore.GetType("System.Windows.Media.PathGeometry"); break;
            case KnownElements.PathSegmentCollection: t = _asmCore.GetType("System.Windows.Media.PathSegmentCollection"); break;
            case KnownElements.Pen: t = _asmCore.GetType("System.Windows.Media.Pen"); break;
            case KnownElements.PointCollection: t = _asmCore.GetType("System.Windows.Media.PointCollection"); break;
            case KnownElements.PointCollectionConverter: t = _asmCore.GetType("System.Windows.Media.PointCollectionConverter"); break;
            case KnownElements.PolyBezierSegment: t = _asmCore.GetType("System.Windows.Media.PolyBezierSegment"); break;
            case KnownElements.PolyLineSegment: t = _asmCore.GetType("System.Windows.Media.PolyLineSegment"); break;
            case KnownElements.PolyQuadraticBezierSegment: t = _asmCore.GetType("System.Windows.Media.PolyQuadraticBezierSegment"); break;
            case KnownElements.QuadraticBezierSegment: t = _asmCore.GetType("System.Windows.Media.QuadraticBezierSegment"); break;
            case KnownElements.RadialGradientBrush: t = _asmCore.GetType("System.Windows.Media.RadialGradientBrush"); break;
            case KnownElements.RectangleGeometry: t = _asmCore.GetType("System.Windows.Media.RectangleGeometry"); break;
            case KnownElements.RotateTransform: t = _asmCore.GetType("System.Windows.Media.RotateTransform"); break;
            case KnownElements.ScaleTransform: t = _asmCore.GetType("System.Windows.Media.ScaleTransform"); break;
            case KnownElements.SkewTransform: t = _asmCore.GetType("System.Windows.Media.SkewTransform"); break;
            case KnownElements.SolidColorBrush: t = _asmCore.GetType("System.Windows.Media.SolidColorBrush"); break;
            case KnownElements.StreamGeometry: t = _asmCore.GetType("System.Windows.Media.StreamGeometry"); break;
            case KnownElements.TextEffect: t = _asmCore.GetType("System.Windows.Media.TextEffect"); break;
            case KnownElements.TextEffectCollection: t = _asmCore.GetType("System.Windows.Media.TextEffectCollection"); break;
            case KnownElements.TransformCollection: t = _asmCore.GetType("System.Windows.Media.TransformCollection"); break;
            case KnownElements.TransformConverter: t = _asmCore.GetType("System.Windows.Media.TransformConverter"); break;
            case KnownElements.TransformGroup: t = _asmCore.GetType("System.Windows.Media.TransformGroup"); break;
            case KnownElements.TranslateTransform: t = _asmCore.GetType("System.Windows.Media.TranslateTransform"); break;
            case KnownElements.VectorCollection: t = _asmCore.GetType("System.Windows.Media.VectorCollection"); break;
            case KnownElements.VectorCollectionConverter: t = _asmCore.GetType("System.Windows.Media.VectorCollectionConverter"); break;
            case KnownElements.VisualBrush: t = _asmCore.GetType("System.Windows.Media.VisualBrush"); break;
            case KnownElements.VideoDrawing: t = _asmCore.GetType("System.Windows.Media.VideoDrawing"); break;
            case KnownElements.GuidelineSet: t = _asmCore.GetType("System.Windows.Media.GuidelineSet"); break;
            case KnownElements.GlyphRun: t = _asmCore.GetType("System.Windows.Media.GlyphRun"); break;
            case KnownElements.GlyphTypeface: t = _asmCore.GetType("System.Windows.Media.GlyphTypeface"); break;
            case KnownElements.ImageMetadata: t = _asmCore.GetType("System.Windows.Media.ImageMetadata"); break;
            case KnownElements.ImageSourceConverter: t = _asmCore.GetType("System.Windows.Media.ImageSourceConverter"); break;
            case KnownElements.BitmapDecoder: t = _asmCore.GetType("System.Windows.Media.Imaging.BitmapDecoder"); break;
            case KnownElements.BitmapEncoder: t = _asmCore.GetType("System.Windows.Media.Imaging.BitmapEncoder"); break;
            case KnownElements.BmpBitmapDecoder: t = _asmCore.GetType("System.Windows.Media.Imaging.BmpBitmapDecoder"); break;
            case KnownElements.BmpBitmapEncoder: t = _asmCore.GetType("System.Windows.Media.Imaging.BmpBitmapEncoder"); break;
            case KnownElements.BitmapSource: t = _asmCore.GetType("System.Windows.Media.Imaging.BitmapSource"); break;
            case KnownElements.BitmapFrame: t = _asmCore.GetType("System.Windows.Media.Imaging.BitmapFrame"); break;
            case KnownElements.BitmapMetadata: t = _asmCore.GetType("System.Windows.Media.Imaging.BitmapMetadata"); break;
            case KnownElements.BitmapPalette: t = _asmCore.GetType("System.Windows.Media.Imaging.BitmapPalette"); break;
            case KnownElements.BitmapImage: t = _asmCore.GetType("System.Windows.Media.Imaging.BitmapImage"); break;
            case KnownElements.CachedBitmap: t = _asmCore.GetType("System.Windows.Media.Imaging.CachedBitmap"); break;
            case KnownElements.ColorConvertedBitmap: t = _asmCore.GetType("System.Windows.Media.Imaging.ColorConvertedBitmap"); break;
            case KnownElements.CroppedBitmap: t = _asmCore.GetType("System.Windows.Media.Imaging.CroppedBitmap"); break;
            case KnownElements.FormatConvertedBitmap: t = _asmCore.GetType("System.Windows.Media.Imaging.FormatConvertedBitmap"); break;
            case KnownElements.GifBitmapDecoder: t = _asmCore.GetType("System.Windows.Media.Imaging.GifBitmapDecoder"); break;
            case KnownElements.GifBitmapEncoder: t = _asmCore.GetType("System.Windows.Media.Imaging.GifBitmapEncoder"); break;
            case KnownElements.IconBitmapDecoder: t = _asmCore.GetType("System.Windows.Media.Imaging.IconBitmapDecoder"); break;
            case KnownElements.InPlaceBitmapMetadataWriter: t = _asmCore.GetType("System.Windows.Media.Imaging.InPlaceBitmapMetadataWriter"); break;
            case KnownElements.LateBoundBitmapDecoder: t = _asmCore.GetType("System.Windows.Media.Imaging.LateBoundBitmapDecoder"); break;
            case KnownElements.JpegBitmapDecoder: t = _asmCore.GetType("System.Windows.Media.Imaging.JpegBitmapDecoder"); break;
            case KnownElements.JpegBitmapEncoder: t = _asmCore.GetType("System.Windows.Media.Imaging.JpegBitmapEncoder"); break;
            case KnownElements.PngBitmapDecoder: t = _asmCore.GetType("System.Windows.Media.Imaging.PngBitmapDecoder"); break;
            case KnownElements.PngBitmapEncoder: t = _asmCore.GetType("System.Windows.Media.Imaging.PngBitmapEncoder"); break;
            case KnownElements.RenderTargetBitmap: t = _asmCore.GetType("System.Windows.Media.Imaging.RenderTargetBitmap"); break;
            case KnownElements.TiffBitmapDecoder: t = _asmCore.GetType("System.Windows.Media.Imaging.TiffBitmapDecoder"); break;
            case KnownElements.TiffBitmapEncoder: t = _asmCore.GetType("System.Windows.Media.Imaging.TiffBitmapEncoder"); break;
            case KnownElements.WmpBitmapDecoder: t = _asmCore.GetType("System.Windows.Media.Imaging.WmpBitmapDecoder"); break;
            case KnownElements.WmpBitmapEncoder: t = _asmCore.GetType("System.Windows.Media.Imaging.WmpBitmapEncoder"); break;
            case KnownElements.TransformedBitmap: t = _asmCore.GetType("System.Windows.Media.Imaging.TransformedBitmap"); break;
            case KnownElements.WriteableBitmap: t = _asmCore.GetType("System.Windows.Media.Imaging.WriteableBitmap"); break;
            case KnownElements.MediaClock: t = _asmCore.GetType("System.Windows.Media.MediaClock"); break;
            case KnownElements.MediaPlayer: t = _asmCore.GetType("System.Windows.Media.MediaPlayer"); break;
            case KnownElements.PixelFormat: t = _asmCore.GetType("System.Windows.Media.PixelFormat"); break;
            case KnownElements.PixelFormatConverter: t = _asmCore.GetType("System.Windows.Media.PixelFormatConverter"); break;
            case KnownElements.RenderOptions: t = _asmCore.GetType("System.Windows.Media.RenderOptions"); break;
            case KnownElements.NumberSubstitution: t = _asmCore.GetType("System.Windows.Media.NumberSubstitution"); break;
            case KnownElements.VisualTarget: t = _asmCore.GetType("System.Windows.Media.VisualTarget"); break;
            case KnownElements.Transform3D: t = _asmCore.GetType("System.Windows.Media.Media3D.Transform3D"); break;
            case KnownElements.AffineTransform3D: t = _asmCore.GetType("System.Windows.Media.Media3D.AffineTransform3D"); break;
            case KnownElements.Model3D: t = _asmCore.GetType("System.Windows.Media.Media3D.Model3D"); break;
            case KnownElements.Light: t = _asmCore.GetType("System.Windows.Media.Media3D.Light"); break;
            case KnownElements.AmbientLight: t = _asmCore.GetType("System.Windows.Media.Media3D.AmbientLight"); break;
            case KnownElements.Rotation3D: t = _asmCore.GetType("System.Windows.Media.Media3D.Rotation3D"); break;
            case KnownElements.AxisAngleRotation3D: t = _asmCore.GetType("System.Windows.Media.Media3D.AxisAngleRotation3D"); break;
            case KnownElements.Camera: t = _asmCore.GetType("System.Windows.Media.Media3D.Camera"); break;
            case KnownElements.Material: t = _asmCore.GetType("System.Windows.Media.Media3D.Material"); break;
            case KnownElements.DiffuseMaterial: t = _asmCore.GetType("System.Windows.Media.Media3D.DiffuseMaterial"); break;
            case KnownElements.DirectionalLight: t = _asmCore.GetType("System.Windows.Media.Media3D.DirectionalLight"); break;
            case KnownElements.EmissiveMaterial: t = _asmCore.GetType("System.Windows.Media.Media3D.EmissiveMaterial"); break;
            case KnownElements.Geometry3D: t = _asmCore.GetType("System.Windows.Media.Media3D.Geometry3D"); break;
            case KnownElements.GeometryModel3D: t = _asmCore.GetType("System.Windows.Media.Media3D.GeometryModel3D"); break;
            case KnownElements.MaterialGroup: t = _asmCore.GetType("System.Windows.Media.Media3D.MaterialGroup"); break;
            case KnownElements.Matrix3D: t = _asmCore.GetType("System.Windows.Media.Media3D.Matrix3D"); break;
            case KnownElements.MatrixCamera: t = _asmCore.GetType("System.Windows.Media.Media3D.MatrixCamera"); break;
            case KnownElements.MatrixTransform3D: t = _asmCore.GetType("System.Windows.Media.Media3D.MatrixTransform3D"); break;
            case KnownElements.MeshGeometry3D: t = _asmCore.GetType("System.Windows.Media.Media3D.MeshGeometry3D"); break;
            case KnownElements.Model3DGroup: t = _asmCore.GetType("System.Windows.Media.Media3D.Model3DGroup"); break;
            case KnownElements.ModelVisual3D: t = _asmCore.GetType("System.Windows.Media.Media3D.ModelVisual3D"); break;
            case KnownElements.Point3D: t = _asmCore.GetType("System.Windows.Media.Media3D.Point3D"); break;
            case KnownElements.Point3DCollection: t = _asmCore.GetType("System.Windows.Media.Media3D.Point3DCollection"); break;
            case KnownElements.Vector3DCollection: t = _asmCore.GetType("System.Windows.Media.Media3D.Vector3DCollection"); break;
            case KnownElements.Point4D: t = _asmCore.GetType("System.Windows.Media.Media3D.Point4D"); break;
            case KnownElements.PointLightBase: t = _asmCore.GetType("System.Windows.Media.Media3D.PointLightBase"); break;
            case KnownElements.PointLight: t = _asmCore.GetType("System.Windows.Media.Media3D.PointLight"); break;
            case KnownElements.ProjectionCamera: t = _asmCore.GetType("System.Windows.Media.Media3D.ProjectionCamera"); break;
            case KnownElements.OrthographicCamera: t = _asmCore.GetType("System.Windows.Media.Media3D.OrthographicCamera"); break;
            case KnownElements.PerspectiveCamera: t = _asmCore.GetType("System.Windows.Media.Media3D.PerspectiveCamera"); break;
            case KnownElements.Quaternion: t = _asmCore.GetType("System.Windows.Media.Media3D.Quaternion"); break;
            case KnownElements.QuaternionRotation3D: t = _asmCore.GetType("System.Windows.Media.Media3D.QuaternionRotation3D"); break;
            case KnownElements.Rect3D: t = _asmCore.GetType("System.Windows.Media.Media3D.Rect3D"); break;
            case KnownElements.RotateTransform3D: t = _asmCore.GetType("System.Windows.Media.Media3D.RotateTransform3D"); break;
            case KnownElements.ScaleTransform3D: t = _asmCore.GetType("System.Windows.Media.Media3D.ScaleTransform3D"); break;
            case KnownElements.Size3D: t = _asmCore.GetType("System.Windows.Media.Media3D.Size3D"); break;
            case KnownElements.SpecularMaterial: t = _asmCore.GetType("System.Windows.Media.Media3D.SpecularMaterial"); break;
            case KnownElements.SpotLight: t = _asmCore.GetType("System.Windows.Media.Media3D.SpotLight"); break;
            case KnownElements.Transform3DGroup: t = _asmCore.GetType("System.Windows.Media.Media3D.Transform3DGroup"); break;
            case KnownElements.TranslateTransform3D: t = _asmCore.GetType("System.Windows.Media.Media3D.TranslateTransform3D"); break;
            case KnownElements.Vector3D: t = _asmCore.GetType("System.Windows.Media.Media3D.Vector3D"); break;
            case KnownElements.Viewport3DVisual: t = _asmCore.GetType("System.Windows.Media.Media3D.Viewport3DVisual"); break;
            case KnownElements.MaterialCollection: t = _asmCore.GetType("System.Windows.Media.Media3D.MaterialCollection"); break;
            case KnownElements.Matrix3DConverter: t = _asmCore.GetType("System.Windows.Media.Media3D.Matrix3DConverter"); break;
            case KnownElements.Model3DCollection: t = _asmCore.GetType("System.Windows.Media.Media3D.Model3DCollection"); break;
            case KnownElements.Point3DConverter: t = _asmCore.GetType("System.Windows.Media.Media3D.Point3DConverter"); break;
            case KnownElements.Point3DCollectionConverter: t = _asmCore.GetType("System.Windows.Media.Media3D.Point3DCollectionConverter"); break;
            case KnownElements.Point4DConverter: t = _asmCore.GetType("System.Windows.Media.Media3D.Point4DConverter"); break;
            case KnownElements.QuaternionConverter: t = _asmCore.GetType("System.Windows.Media.Media3D.QuaternionConverter"); break;
            case KnownElements.Rect3DConverter: t = _asmCore.GetType("System.Windows.Media.Media3D.Rect3DConverter"); break;
            case KnownElements.Size3DConverter: t = _asmCore.GetType("System.Windows.Media.Media3D.Size3DConverter"); break;
            case KnownElements.Transform3DCollection: t = _asmCore.GetType("System.Windows.Media.Media3D.Transform3DCollection"); break;
            case KnownElements.Vector3DConverter: t = _asmCore.GetType("System.Windows.Media.Media3D.Vector3DConverter"); break;
            case KnownElements.Vector3DCollectionConverter: t = _asmCore.GetType("System.Windows.Media.Media3D.Vector3DCollectionConverter"); break;
            case KnownElements.XmlLanguage: t = _asmCore.GetType("System.Windows.Markup.XmlLanguage"); break;
            case KnownElements.XmlLanguageConverter: t = _asmCore.GetType("System.Windows.Markup.XmlLanguageConverter"); break;
            case KnownElements.Point: t = _asmBase.GetType("System.Windows.Point"); break;
            case KnownElements.Size: t = _asmBase.GetType("System.Windows.Size"); break;
            case KnownElements.Vector: t = _asmBase.GetType("System.Windows.Vector"); break;
            case KnownElements.Rect: t = _asmBase.GetType("System.Windows.Rect"); break;
            case KnownElements.Matrix: t = _asmBase.GetType("System.Windows.Media.Matrix"); break;
            case KnownElements.DependencyProperty: t = _asmBase.GetType("System.Windows.DependencyProperty"); break;
            case KnownElements.DependencyObject: t = _asmBase.GetType("System.Windows.DependencyObject"); break;
            case KnownElements.Expression: t = _asmBase.GetType("System.Windows.Expression"); break;
            case KnownElements.Freezable: t = _asmBase.GetType("System.Windows.Freezable"); break;
            case KnownElements.WeakEventManager: t = _asmBase.GetType("System.Windows.WeakEventManager"); break;
            case KnownElements.Int32Rect: t = _asmBase.GetType("System.Windows.Int32Rect"); break;
            case KnownElements.ExpressionConverter: t = _asmBase.GetType("System.Windows.ExpressionConverter"); break;
            case KnownElements.Int32RectConverter: t = _asmBase.GetType("System.Windows.Int32RectConverter"); break;
            case KnownElements.PointConverter: t = _asmBase.GetType("System.Windows.PointConverter"); break;
            case KnownElements.RectConverter: t = _asmBase.GetType("System.Windows.RectConverter"); break;
            case KnownElements.SizeConverter: t = _asmBase.GetType("System.Windows.SizeConverter"); break;
            case KnownElements.VectorConverter: t = _asmBase.GetType("System.Windows.VectorConverter"); break;
            case KnownElements.KeyConverter: t = _asmBase.GetType("System.Windows.Input.KeyConverter"); break;
            case KnownElements.MatrixConverter: t = _asmBase.GetType("System.Windows.Media.MatrixConverter"); break;
            case KnownElements.MarkupExtension: t = _asmBase.GetType("System.Windows.Markup.MarkupExtension"); break;
            case KnownElements.ModifierKeysConverter: t = _asmBase.GetType("System.Windows.Input.ModifierKeysConverter"); break;
            case KnownElements.FrameworkPropertyMetadataOptions: t = _asmFramework.GetType("System.Windows.FrameworkPropertyMetadataOptions"); break;
            case KnownElements.NullExtension: t = _asmFramework.GetType("System.Windows.Markup.NullExtension"); break;
            case KnownElements.StaticExtension: t = _asmFramework.GetType("System.Windows.Markup.StaticExtension"); break;
            case KnownElements.ArrayExtension: t = _asmFramework.GetType("System.Windows.Markup.ArrayExtension"); break;
            case KnownElements.TypeExtension: t = _asmFramework.GetType("System.Windows.Markup.TypeExtension"); break;
            case KnownElements.IStyleConnector: t = _asmFramework.GetType("System.Windows.Markup.IStyleConnector"); break;
            case KnownElements.ParserContext: t = _asmFramework.GetType("System.Windows.Markup.ParserContext"); break;
            case KnownElements.XamlReader: t = _asmFramework.GetType("System.Windows.Markup.XamlReader"); break;
            case KnownElements.XamlWriter: t = _asmFramework.GetType("System.Windows.Markup.XamlWriter"); break;
            case KnownElements.StreamResourceInfo: t = _asmFramework.GetType("System.Windows.Resources.StreamResourceInfo"); break;
            case KnownElements.CommandConverter: t = _asmFramework.GetType("System.Windows.Input.CommandConverter"); break;
            case KnownElements.DependencyPropertyConverter: t = _asmFramework.GetType("System.Windows.Markup.DependencyPropertyConverter"); break;
            case KnownElements.ComponentResourceKeyConverter: t = _asmFramework.GetType("System.Windows.Markup.ComponentResourceKeyConverter"); break;
            case KnownElements.TemplateKeyConverter: t = _asmFramework.GetType("System.Windows.Markup.TemplateKeyConverter"); break;
            case KnownElements.RoutedEventConverter: t = _asmFramework.GetType("System.Windows.Markup.RoutedEventConverter"); break;
            case KnownElements.FrameworkPropertyMetadata: t = _asmFramework.GetType("System.Windows.FrameworkPropertyMetadata"); break;
            case KnownElements.Condition: t = _asmFramework.GetType("System.Windows.Condition"); break;
            case KnownElements.FrameworkElementFactory: t = _asmFramework.GetType("System.Windows.FrameworkElementFactory"); break;
            case KnownElements.IAddChild: t = _asmCore.GetType("System.Windows.Markup.IAddChild"); break;
            case KnownElements.IAddChildInternal: t = _asmCore.GetType("System.Windows.Markup.IAddChildInternal"); break;
            case KnownElements.RoutingStrategy: t = _asmCore.GetType("System.Windows.RoutingStrategy"); break;
            case KnownElements.EventManager: t = _asmCore.GetType("System.Windows.EventManager"); break;
            case KnownElements.XmlLangPropertyAttribute: t = _asmBase.GetType("System.Windows.Markup.XmlLangPropertyAttribute"); break;
            case KnownElements.INameScope: t = _asmBase.GetType("System.Windows.Markup.INameScope"); break;
            case KnownElements.IComponentConnector: t = _asmBase.GetType("System.Windows.Markup.IComponentConnector"); break;
            case KnownElements.RuntimeNamePropertyAttribute: t = _asmBase.GetType("System.Windows.Markup.RuntimeNamePropertyAttribute"); break;
            case KnownElements.ContentPropertyAttribute: t = _asmBase.GetType("System.Windows.Markup.ContentPropertyAttribute"); break;
            case KnownElements.WhitespaceSignificantCollectionAttribute: t = _asmBase.GetType("System.Windows.Markup.WhitespaceSignificantCollectionAttribute"); break;
            case KnownElements.ContentWrapperAttribute: t = _asmBase.GetType("System.Windows.Markup.ContentWrapperAttribute"); break;
            case KnownElements.InlineCollection: t = _asmFramework.GetType("System.Windows.Documents.InlineCollection"); break;
            case KnownElements.XamlStyleSerializer: t = typeof(XamlStyleSerializer); break;
            case KnownElements.XamlTemplateSerializer: t = typeof(XamlTemplateSerializer); break;
            case KnownElements.XamlBrushSerializer: t = typeof(XamlBrushSerializer); break;
            case KnownElements.XamlPoint3DCollectionSerializer: t = typeof(XamlPoint3DCollectionSerializer); break;
            case KnownElements.XamlVector3DCollectionSerializer: t = typeof(XamlVector3DCollectionSerializer); break;
            case KnownElements.XamlPointCollectionSerializer: t = typeof(XamlPointCollectionSerializer); break;
            case KnownElements.XamlInt32CollectionSerializer: t = typeof(XamlInt32CollectionSerializer); break;
            case KnownElements.XamlPathDataSerializer: t = typeof(XamlPathDataSerializer); break;
            case KnownElements.TypeTypeConverter: t = typeof(TypeTypeConverter); break;
            case KnownElements.Boolean: t = typeof(Boolean); break;
            case KnownElements.Int16: t = typeof(Int16); break;
            case KnownElements.Int32: t = typeof(Int32); break;
            case KnownElements.Int64: t = typeof(Int64); break;
            case KnownElements.UInt16: t = typeof(UInt16); break;
            case KnownElements.UInt32: t = typeof(UInt32); break;
            case KnownElements.UInt64: t = typeof(UInt64); break;
            case KnownElements.Single: t = typeof(Single); break;
            case KnownElements.Double: t = typeof(Double); break;
            case KnownElements.Object: t = typeof(Object); break;
            case KnownElements.String: t = typeof(String); break;
            case KnownElements.Byte: t = typeof(Byte); break;
            case KnownElements.SByte: t = typeof(SByte); break;
            case KnownElements.Char: t = typeof(Char); break;
            case KnownElements.Decimal: t = typeof(Decimal); break;
            case KnownElements.TimeSpan: t = typeof(TimeSpan); break;
            case KnownElements.Guid: t = typeof(Guid); break;
            case KnownElements.DateTime: t = typeof(DateTime); break;
            case KnownElements.Uri: t = typeof(Uri); break;
            case KnownElements.CultureInfo: t = typeof(CultureInfo); break;
            case KnownElements.EnumConverter: t = typeof(EnumConverter); break;
            case KnownElements.NullableConverter: t = typeof(NullableConverter); break;
            case KnownElements.BooleanConverter: t = typeof(BooleanConverter); break;
            case KnownElements.Int16Converter: t = typeof(Int16Converter); break;
            case KnownElements.Int32Converter: t = typeof(Int32Converter); break;
            case KnownElements.Int64Converter: t = typeof(Int64Converter); break;
            case KnownElements.UInt16Converter: t = typeof(UInt16Converter); break;
            case KnownElements.UInt32Converter: t = typeof(UInt32Converter); break;
            case KnownElements.UInt64Converter: t = typeof(UInt64Converter); break;
            case KnownElements.SingleConverter: t = typeof(SingleConverter); break;
            case KnownElements.DoubleConverter: t = typeof(DoubleConverter); break;
            case KnownElements.StringConverter: t = typeof(StringConverter); break;
            case KnownElements.ByteConverter: t = typeof(ByteConverter); break;
            case KnownElements.SByteConverter: t = typeof(SByteConverter); break;
            case KnownElements.CharConverter: t = typeof(CharConverter); break;
            case KnownElements.DecimalConverter: t = typeof(DecimalConverter); break;
            case KnownElements.TimeSpanConverter: t = typeof(TimeSpanConverter); break;
            case KnownElements.GuidConverter: t = typeof(GuidConverter); break;
            case KnownElements.CultureInfoConverter: t = typeof(CultureInfoConverter); break;
            case KnownElements.DateTimeConverter: t = typeof(DateTimeConverter); break;
            case KnownElements.DateTimeConverter2: t = typeof(DateTimeConverter2); break;
            case KnownElements.UriTypeConverter: t = typeof(UriTypeConverter); break;
            }

            if(t == null)
            {
                MarkupCompiler.ThrowCompilerException(SRID.ParserInvalidKnownType, ((int)knownElement).ToString(CultureInfo.InvariantCulture), knownElement.ToString());
            }
            return t;
        }
#else
        //  Initialize the Known WCP types from basic WCP assemblies
        private Type InitializeOneType(KnownElements knownElement)
        {
            Type t = null;
            switch(knownElement)
            {
            case KnownElements.AccessText: t = typeof(System.Windows.Controls.AccessText); break;
            case KnownElements.AdornedElementPlaceholder: t = typeof(System.Windows.Controls.AdornedElementPlaceholder); break;
            case KnownElements.Adorner: t = typeof(System.Windows.Documents.Adorner); break;
            case KnownElements.AdornerDecorator: t = typeof(System.Windows.Documents.AdornerDecorator); break;
            case KnownElements.AdornerLayer: t = typeof(System.Windows.Documents.AdornerLayer); break;
            case KnownElements.AffineTransform3D: t = typeof(System.Windows.Media.Media3D.AffineTransform3D); break;
            case KnownElements.AmbientLight: t = typeof(System.Windows.Media.Media3D.AmbientLight); break;
            case KnownElements.AnchoredBlock: t = typeof(System.Windows.Documents.AnchoredBlock); break;
            case KnownElements.Animatable: t = typeof(System.Windows.Media.Animation.Animatable); break;
            case KnownElements.AnimationClock: t = typeof(System.Windows.Media.Animation.AnimationClock); break;
            case KnownElements.AnimationTimeline: t = typeof(System.Windows.Media.Animation.AnimationTimeline); break;
            case KnownElements.Application: t = typeof(System.Windows.Application); break;
            case KnownElements.ArcSegment: t = typeof(System.Windows.Media.ArcSegment); break;
            case KnownElements.ArrayExtension: t = typeof(System.Windows.Markup.ArrayExtension); break;
            case KnownElements.AxisAngleRotation3D: t = typeof(System.Windows.Media.Media3D.AxisAngleRotation3D); break;
            case KnownElements.BaseIListConverter: t = typeof(System.Windows.Media.Converters.BaseIListConverter); break;
            case KnownElements.BeginStoryboard: t = typeof(System.Windows.Media.Animation.BeginStoryboard); break;
            case KnownElements.BevelBitmapEffect: t = typeof(System.Windows.Media.Effects.BevelBitmapEffect); break;
            case KnownElements.BezierSegment: t = typeof(System.Windows.Media.BezierSegment); break;
            case KnownElements.Binding: t = typeof(System.Windows.Data.Binding); break;
            case KnownElements.BindingBase: t = typeof(System.Windows.Data.BindingBase); break;
            case KnownElements.BindingExpression: t = typeof(System.Windows.Data.BindingExpression); break;
            case KnownElements.BindingExpressionBase: t = typeof(System.Windows.Data.BindingExpressionBase); break;
            case KnownElements.BindingListCollectionView: t = typeof(System.Windows.Data.BindingListCollectionView); break;
            case KnownElements.BitmapDecoder: t = typeof(System.Windows.Media.Imaging.BitmapDecoder); break;
            case KnownElements.BitmapEffect: t = typeof(System.Windows.Media.Effects.BitmapEffect); break;
            case KnownElements.BitmapEffectCollection: t = typeof(System.Windows.Media.Effects.BitmapEffectCollection); break;
            case KnownElements.BitmapEffectGroup: t = typeof(System.Windows.Media.Effects.BitmapEffectGroup); break;
            case KnownElements.BitmapEffectInput: t = typeof(System.Windows.Media.Effects.BitmapEffectInput); break;
            case KnownElements.BitmapEncoder: t = typeof(System.Windows.Media.Imaging.BitmapEncoder); break;
            case KnownElements.BitmapFrame: t = typeof(System.Windows.Media.Imaging.BitmapFrame); break;
            case KnownElements.BitmapImage: t = typeof(System.Windows.Media.Imaging.BitmapImage); break;
            case KnownElements.BitmapMetadata: t = typeof(System.Windows.Media.Imaging.BitmapMetadata); break;
            case KnownElements.BitmapPalette: t = typeof(System.Windows.Media.Imaging.BitmapPalette); break;
            case KnownElements.BitmapSource: t = typeof(System.Windows.Media.Imaging.BitmapSource); break;
            case KnownElements.Block: t = typeof(System.Windows.Documents.Block); break;
            case KnownElements.BlockUIContainer: t = typeof(System.Windows.Documents.BlockUIContainer); break;
            case KnownElements.BlurBitmapEffect: t = typeof(System.Windows.Media.Effects.BlurBitmapEffect); break;
            case KnownElements.BmpBitmapDecoder: t = typeof(System.Windows.Media.Imaging.BmpBitmapDecoder); break;
            case KnownElements.BmpBitmapEncoder: t = typeof(System.Windows.Media.Imaging.BmpBitmapEncoder); break;
            case KnownElements.Bold: t = typeof(System.Windows.Documents.Bold); break;
            case KnownElements.BoolIListConverter: t = typeof(System.Windows.Media.Converters.BoolIListConverter); break;
            case KnownElements.Boolean: t = typeof(System.Boolean); break;
            case KnownElements.BooleanAnimationBase: t = typeof(System.Windows.Media.Animation.BooleanAnimationBase); break;
            case KnownElements.BooleanAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.BooleanAnimationUsingKeyFrames); break;
            case KnownElements.BooleanConverter: t = typeof(System.ComponentModel.BooleanConverter); break;
            case KnownElements.BooleanKeyFrame: t = typeof(System.Windows.Media.Animation.BooleanKeyFrame); break;
            case KnownElements.BooleanKeyFrameCollection: t = typeof(System.Windows.Media.Animation.BooleanKeyFrameCollection); break;
            case KnownElements.BooleanToVisibilityConverter: t = typeof(System.Windows.Controls.BooleanToVisibilityConverter); break;
            case KnownElements.Border: t = typeof(System.Windows.Controls.Border); break;
            case KnownElements.BorderGapMaskConverter: t = typeof(System.Windows.Controls.BorderGapMaskConverter); break;
            case KnownElements.Brush: t = typeof(System.Windows.Media.Brush); break;
            case KnownElements.BrushConverter: t = typeof(System.Windows.Media.BrushConverter); break;
            case KnownElements.BulletDecorator: t = typeof(System.Windows.Controls.Primitives.BulletDecorator); break;
            case KnownElements.Button: t = typeof(System.Windows.Controls.Button); break;
            case KnownElements.ButtonBase: t = typeof(System.Windows.Controls.Primitives.ButtonBase); break;
            case KnownElements.Byte: t = typeof(System.Byte); break;
            case KnownElements.ByteAnimation: t = typeof(System.Windows.Media.Animation.ByteAnimation); break;
            case KnownElements.ByteAnimationBase: t = typeof(System.Windows.Media.Animation.ByteAnimationBase); break;
            case KnownElements.ByteAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.ByteAnimationUsingKeyFrames); break;
            case KnownElements.ByteConverter: t = typeof(System.ComponentModel.ByteConverter); break;
            case KnownElements.ByteKeyFrame: t = typeof(System.Windows.Media.Animation.ByteKeyFrame); break;
            case KnownElements.ByteKeyFrameCollection: t = typeof(System.Windows.Media.Animation.ByteKeyFrameCollection); break;
            case KnownElements.CachedBitmap: t = typeof(System.Windows.Media.Imaging.CachedBitmap); break;
            case KnownElements.Camera: t = typeof(System.Windows.Media.Media3D.Camera); break;
            case KnownElements.Canvas: t = typeof(System.Windows.Controls.Canvas); break;
            case KnownElements.Char: t = typeof(System.Char); break;
            case KnownElements.CharAnimationBase: t = typeof(System.Windows.Media.Animation.CharAnimationBase); break;
            case KnownElements.CharAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.CharAnimationUsingKeyFrames); break;
            case KnownElements.CharConverter: t = typeof(System.ComponentModel.CharConverter); break;
            case KnownElements.CharIListConverter: t = typeof(System.Windows.Media.Converters.CharIListConverter); break;
            case KnownElements.CharKeyFrame: t = typeof(System.Windows.Media.Animation.CharKeyFrame); break;
            case KnownElements.CharKeyFrameCollection: t = typeof(System.Windows.Media.Animation.CharKeyFrameCollection); break;
            case KnownElements.CheckBox: t = typeof(System.Windows.Controls.CheckBox); break;
            case KnownElements.Clock: t = typeof(System.Windows.Media.Animation.Clock); break;
            case KnownElements.ClockController: t = typeof(System.Windows.Media.Animation.ClockController); break;
            case KnownElements.ClockGroup: t = typeof(System.Windows.Media.Animation.ClockGroup); break;
            case KnownElements.CollectionContainer: t = typeof(System.Windows.Data.CollectionContainer); break;
            case KnownElements.CollectionView: t = typeof(System.Windows.Data.CollectionView); break;
            case KnownElements.CollectionViewSource: t = typeof(System.Windows.Data.CollectionViewSource); break;
            case KnownElements.Color: t = typeof(System.Windows.Media.Color); break;
            case KnownElements.ColorAnimation: t = typeof(System.Windows.Media.Animation.ColorAnimation); break;
            case KnownElements.ColorAnimationBase: t = typeof(System.Windows.Media.Animation.ColorAnimationBase); break;
            case KnownElements.ColorAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.ColorAnimationUsingKeyFrames); break;
            case KnownElements.ColorConvertedBitmap: t = typeof(System.Windows.Media.Imaging.ColorConvertedBitmap); break;
            case KnownElements.ColorConvertedBitmapExtension: t = typeof(System.Windows.ColorConvertedBitmapExtension); break;
            case KnownElements.ColorConverter: t = typeof(System.Windows.Media.ColorConverter); break;
            case KnownElements.ColorKeyFrame: t = typeof(System.Windows.Media.Animation.ColorKeyFrame); break;
            case KnownElements.ColorKeyFrameCollection: t = typeof(System.Windows.Media.Animation.ColorKeyFrameCollection); break;
            case KnownElements.ColumnDefinition: t = typeof(System.Windows.Controls.ColumnDefinition); break;
            case KnownElements.CombinedGeometry: t = typeof(System.Windows.Media.CombinedGeometry); break;
            case KnownElements.ComboBox: t = typeof(System.Windows.Controls.ComboBox); break;
            case KnownElements.ComboBoxItem: t = typeof(System.Windows.Controls.ComboBoxItem); break;
            case KnownElements.CommandConverter: t = typeof(System.Windows.Input.CommandConverter); break;
            case KnownElements.ComponentResourceKey: t = typeof(System.Windows.ComponentResourceKey); break;
            case KnownElements.ComponentResourceKeyConverter: t = typeof(System.Windows.Markup.ComponentResourceKeyConverter); break;
            case KnownElements.CompositionTarget: t = typeof(System.Windows.Media.CompositionTarget); break;
            case KnownElements.Condition: t = typeof(System.Windows.Condition); break;
            case KnownElements.ContainerVisual: t = typeof(System.Windows.Media.ContainerVisual); break;
            case KnownElements.ContentControl: t = typeof(System.Windows.Controls.ContentControl); break;
            case KnownElements.ContentElement: t = typeof(System.Windows.ContentElement); break;
            case KnownElements.ContentPresenter: t = typeof(System.Windows.Controls.ContentPresenter); break;
            case KnownElements.ContentPropertyAttribute: t = typeof(System.Windows.Markup.ContentPropertyAttribute); break;
            case KnownElements.ContentWrapperAttribute: t = typeof(System.Windows.Markup.ContentWrapperAttribute); break;
            case KnownElements.ContextMenu: t = typeof(System.Windows.Controls.ContextMenu); break;
            case KnownElements.ContextMenuService: t = typeof(System.Windows.Controls.ContextMenuService); break;
            case KnownElements.Control: t = typeof(System.Windows.Controls.Control); break;
            case KnownElements.ControlTemplate: t = typeof(System.Windows.Controls.ControlTemplate); break;
            case KnownElements.ControllableStoryboardAction: t = typeof(System.Windows.Media.Animation.ControllableStoryboardAction); break;
            case KnownElements.CornerRadius: t = typeof(System.Windows.CornerRadius); break;
            case KnownElements.CornerRadiusConverter: t = typeof(System.Windows.CornerRadiusConverter); break;
            case KnownElements.CroppedBitmap: t = typeof(System.Windows.Media.Imaging.CroppedBitmap); break;
            case KnownElements.CultureInfo: t = typeof(System.Globalization.CultureInfo); break;
            case KnownElements.CultureInfoConverter: t = typeof(System.ComponentModel.CultureInfoConverter); break;
            case KnownElements.CultureInfoIetfLanguageTagConverter: t = typeof(System.Windows.CultureInfoIetfLanguageTagConverter); break;
            case KnownElements.Cursor: t = typeof(System.Windows.Input.Cursor); break;
            case KnownElements.CursorConverter: t = typeof(System.Windows.Input.CursorConverter); break;
            case KnownElements.DashStyle: t = typeof(System.Windows.Media.DashStyle); break;
            case KnownElements.DataChangedEventManager: t = typeof(System.Windows.Data.DataChangedEventManager); break;
            case KnownElements.DataTemplate: t = typeof(System.Windows.DataTemplate); break;
            case KnownElements.DataTemplateKey: t = typeof(System.Windows.DataTemplateKey); break;
            case KnownElements.DataTrigger: t = typeof(System.Windows.DataTrigger); break;
            case KnownElements.DateTime: t = typeof(System.DateTime); break;
            case KnownElements.DateTimeConverter: t = typeof(System.ComponentModel.DateTimeConverter); break;
            case KnownElements.DateTimeConverter2: t = typeof(System.Windows.Markup.DateTimeConverter2); break;
            case KnownElements.Decimal: t = typeof(System.Decimal); break;
            case KnownElements.DecimalAnimation: t = typeof(System.Windows.Media.Animation.DecimalAnimation); break;
            case KnownElements.DecimalAnimationBase: t = typeof(System.Windows.Media.Animation.DecimalAnimationBase); break;
            case KnownElements.DecimalAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.DecimalAnimationUsingKeyFrames); break;
            case KnownElements.DecimalConverter: t = typeof(System.ComponentModel.DecimalConverter); break;
            case KnownElements.DecimalKeyFrame: t = typeof(System.Windows.Media.Animation.DecimalKeyFrame); break;
            case KnownElements.DecimalKeyFrameCollection: t = typeof(System.Windows.Media.Animation.DecimalKeyFrameCollection); break;
            case KnownElements.Decorator: t = typeof(System.Windows.Controls.Decorator); break;
            case KnownElements.DefinitionBase: t = typeof(System.Windows.Controls.DefinitionBase); break;
            case KnownElements.DependencyObject: t = typeof(System.Windows.DependencyObject); break;
            case KnownElements.DependencyProperty: t = typeof(System.Windows.DependencyProperty); break;
            case KnownElements.DependencyPropertyConverter: t = typeof(System.Windows.Markup.DependencyPropertyConverter); break;
            case KnownElements.DialogResultConverter: t = typeof(System.Windows.DialogResultConverter); break;
            case KnownElements.DiffuseMaterial: t = typeof(System.Windows.Media.Media3D.DiffuseMaterial); break;
            case KnownElements.DirectionalLight: t = typeof(System.Windows.Media.Media3D.DirectionalLight); break;
            case KnownElements.DiscreteBooleanKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteBooleanKeyFrame); break;
            case KnownElements.DiscreteByteKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteByteKeyFrame); break;
            case KnownElements.DiscreteCharKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteCharKeyFrame); break;
            case KnownElements.DiscreteColorKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteColorKeyFrame); break;
            case KnownElements.DiscreteDecimalKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteDecimalKeyFrame); break;
            case KnownElements.DiscreteDoubleKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteDoubleKeyFrame); break;
            case KnownElements.DiscreteInt16KeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteInt16KeyFrame); break;
            case KnownElements.DiscreteInt32KeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteInt32KeyFrame); break;
            case KnownElements.DiscreteInt64KeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteInt64KeyFrame); break;
            case KnownElements.DiscreteMatrixKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteMatrixKeyFrame); break;
            case KnownElements.DiscreteObjectKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteObjectKeyFrame); break;
            case KnownElements.DiscretePoint3DKeyFrame: t = typeof(System.Windows.Media.Animation.DiscretePoint3DKeyFrame); break;
            case KnownElements.DiscretePointKeyFrame: t = typeof(System.Windows.Media.Animation.DiscretePointKeyFrame); break;
            case KnownElements.DiscreteQuaternionKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteQuaternionKeyFrame); break;
            case KnownElements.DiscreteRectKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteRectKeyFrame); break;
            case KnownElements.DiscreteRotation3DKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteRotation3DKeyFrame); break;
            case KnownElements.DiscreteSingleKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteSingleKeyFrame); break;
            case KnownElements.DiscreteSizeKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteSizeKeyFrame); break;
            case KnownElements.DiscreteStringKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteStringKeyFrame); break;
            case KnownElements.DiscreteThicknessKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteThicknessKeyFrame); break;
            case KnownElements.DiscreteVector3DKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteVector3DKeyFrame); break;
            case KnownElements.DiscreteVectorKeyFrame: t = typeof(System.Windows.Media.Animation.DiscreteVectorKeyFrame); break;
            case KnownElements.DockPanel: t = typeof(System.Windows.Controls.DockPanel); break;
            case KnownElements.DocumentPageView: t = typeof(System.Windows.Controls.Primitives.DocumentPageView); break;
            case KnownElements.DocumentReference: t = typeof(System.Windows.Documents.DocumentReference); break;
            case KnownElements.DocumentViewer: t = typeof(System.Windows.Controls.DocumentViewer); break;
            case KnownElements.DocumentViewerBase: t = typeof(System.Windows.Controls.Primitives.DocumentViewerBase); break;
            case KnownElements.Double: t = typeof(System.Double); break;
            case KnownElements.DoubleAnimation: t = typeof(System.Windows.Media.Animation.DoubleAnimation); break;
            case KnownElements.DoubleAnimationBase: t = typeof(System.Windows.Media.Animation.DoubleAnimationBase); break;
            case KnownElements.DoubleAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames); break;
            case KnownElements.DoubleAnimationUsingPath: t = typeof(System.Windows.Media.Animation.DoubleAnimationUsingPath); break;
            case KnownElements.DoubleCollection: t = typeof(System.Windows.Media.DoubleCollection); break;
            case KnownElements.DoubleCollectionConverter: t = typeof(System.Windows.Media.DoubleCollectionConverter); break;
            case KnownElements.DoubleConverter: t = typeof(System.ComponentModel.DoubleConverter); break;
            case KnownElements.DoubleIListConverter: t = typeof(System.Windows.Media.Converters.DoubleIListConverter); break;
            case KnownElements.DoubleKeyFrame: t = typeof(System.Windows.Media.Animation.DoubleKeyFrame); break;
            case KnownElements.DoubleKeyFrameCollection: t = typeof(System.Windows.Media.Animation.DoubleKeyFrameCollection); break;
            case KnownElements.Drawing: t = typeof(System.Windows.Media.Drawing); break;
            case KnownElements.DrawingBrush: t = typeof(System.Windows.Media.DrawingBrush); break;
            case KnownElements.DrawingCollection: t = typeof(System.Windows.Media.DrawingCollection); break;
            case KnownElements.DrawingContext: t = typeof(System.Windows.Media.DrawingContext); break;
            case KnownElements.DrawingGroup: t = typeof(System.Windows.Media.DrawingGroup); break;
            case KnownElements.DrawingImage: t = typeof(System.Windows.Media.DrawingImage); break;
            case KnownElements.DrawingVisual: t = typeof(System.Windows.Media.DrawingVisual); break;
            case KnownElements.DropShadowBitmapEffect: t = typeof(System.Windows.Media.Effects.DropShadowBitmapEffect); break;
            case KnownElements.Duration: t = typeof(System.Windows.Duration); break;
            case KnownElements.DurationConverter: t = typeof(System.Windows.DurationConverter); break;
            case KnownElements.DynamicResourceExtension: t = typeof(System.Windows.DynamicResourceExtension); break;
            case KnownElements.DynamicResourceExtensionConverter: t = typeof(System.Windows.DynamicResourceExtensionConverter); break;
            case KnownElements.Ellipse: t = typeof(System.Windows.Shapes.Ellipse); break;
            case KnownElements.EllipseGeometry: t = typeof(System.Windows.Media.EllipseGeometry); break;
            case KnownElements.EmbossBitmapEffect: t = typeof(System.Windows.Media.Effects.EmbossBitmapEffect); break;
            case KnownElements.EmissiveMaterial: t = typeof(System.Windows.Media.Media3D.EmissiveMaterial); break;
            case KnownElements.EnumConverter: t = typeof(System.ComponentModel.EnumConverter); break;
            case KnownElements.EventManager: t = typeof(System.Windows.EventManager); break;
            case KnownElements.EventSetter: t = typeof(System.Windows.EventSetter); break;
            case KnownElements.EventTrigger: t = typeof(System.Windows.EventTrigger); break;
            case KnownElements.Expander: t = typeof(System.Windows.Controls.Expander); break;
            case KnownElements.Expression: t = typeof(System.Windows.Expression); break;
            case KnownElements.ExpressionConverter: t = typeof(System.Windows.ExpressionConverter); break;
            case KnownElements.Figure: t = typeof(System.Windows.Documents.Figure); break;
            case KnownElements.FigureLength: t = typeof(System.Windows.FigureLength); break;
            case KnownElements.FigureLengthConverter: t = typeof(System.Windows.FigureLengthConverter); break;
            case KnownElements.FixedDocument: t = typeof(System.Windows.Documents.FixedDocument); break;
            case KnownElements.FixedDocumentSequence: t = typeof(System.Windows.Documents.FixedDocumentSequence); break;
            case KnownElements.FixedPage: t = typeof(System.Windows.Documents.FixedPage); break;
            case KnownElements.Floater: t = typeof(System.Windows.Documents.Floater); break;
            case KnownElements.FlowDocument: t = typeof(System.Windows.Documents.FlowDocument); break;
            case KnownElements.FlowDocumentPageViewer: t = typeof(System.Windows.Controls.FlowDocumentPageViewer); break;
            case KnownElements.FlowDocumentReader: t = typeof(System.Windows.Controls.FlowDocumentReader); break;
            case KnownElements.FlowDocumentScrollViewer: t = typeof(System.Windows.Controls.FlowDocumentScrollViewer); break;
            case KnownElements.FocusManager: t = typeof(System.Windows.Input.FocusManager); break;
            case KnownElements.FontFamily: t = typeof(System.Windows.Media.FontFamily); break;
            case KnownElements.FontFamilyConverter: t = typeof(System.Windows.Media.FontFamilyConverter); break;
            case KnownElements.FontSizeConverter: t = typeof(System.Windows.FontSizeConverter); break;
            case KnownElements.FontStretch: t = typeof(System.Windows.FontStretch); break;
            case KnownElements.FontStretchConverter: t = typeof(System.Windows.FontStretchConverter); break;
            case KnownElements.FontStyle: t = typeof(System.Windows.FontStyle); break;
            case KnownElements.FontStyleConverter: t = typeof(System.Windows.FontStyleConverter); break;
            case KnownElements.FontWeight: t = typeof(System.Windows.FontWeight); break;
            case KnownElements.FontWeightConverter: t = typeof(System.Windows.FontWeightConverter); break;
            case KnownElements.FormatConvertedBitmap: t = typeof(System.Windows.Media.Imaging.FormatConvertedBitmap); break;
            case KnownElements.Frame: t = typeof(System.Windows.Controls.Frame); break;
            case KnownElements.FrameworkContentElement: t = typeof(System.Windows.FrameworkContentElement); break;
            case KnownElements.FrameworkElement: t = typeof(System.Windows.FrameworkElement); break;
            case KnownElements.FrameworkElementFactory: t = typeof(System.Windows.FrameworkElementFactory); break;
            case KnownElements.FrameworkPropertyMetadata: t = typeof(System.Windows.FrameworkPropertyMetadata); break;
            case KnownElements.FrameworkPropertyMetadataOptions: t = typeof(System.Windows.FrameworkPropertyMetadataOptions); break;
            case KnownElements.FrameworkRichTextComposition: t = typeof(System.Windows.Documents.FrameworkRichTextComposition); break;
            case KnownElements.FrameworkTemplate: t = typeof(System.Windows.FrameworkTemplate); break;
            case KnownElements.FrameworkTextComposition: t = typeof(System.Windows.Documents.FrameworkTextComposition); break;
            case KnownElements.Freezable: t = typeof(System.Windows.Freezable); break;
            case KnownElements.GeneralTransform: t = typeof(System.Windows.Media.GeneralTransform); break;
            case KnownElements.GeneralTransformCollection: t = typeof(System.Windows.Media.GeneralTransformCollection); break;
            case KnownElements.GeneralTransformGroup: t = typeof(System.Windows.Media.GeneralTransformGroup); break;
            case KnownElements.Geometry: t = typeof(System.Windows.Media.Geometry); break;
            case KnownElements.Geometry3D: t = typeof(System.Windows.Media.Media3D.Geometry3D); break;
            case KnownElements.GeometryCollection: t = typeof(System.Windows.Media.GeometryCollection); break;
            case KnownElements.GeometryConverter: t = typeof(System.Windows.Media.GeometryConverter); break;
            case KnownElements.GeometryDrawing: t = typeof(System.Windows.Media.GeometryDrawing); break;
            case KnownElements.GeometryGroup: t = typeof(System.Windows.Media.GeometryGroup); break;
            case KnownElements.GeometryModel3D: t = typeof(System.Windows.Media.Media3D.GeometryModel3D); break;
            case KnownElements.GestureRecognizer: t = typeof(System.Windows.Ink.GestureRecognizer); break;
            case KnownElements.GifBitmapDecoder: t = typeof(System.Windows.Media.Imaging.GifBitmapDecoder); break;
            case KnownElements.GifBitmapEncoder: t = typeof(System.Windows.Media.Imaging.GifBitmapEncoder); break;
            case KnownElements.GlyphRun: t = typeof(System.Windows.Media.GlyphRun); break;
            case KnownElements.GlyphRunDrawing: t = typeof(System.Windows.Media.GlyphRunDrawing); break;
            case KnownElements.GlyphTypeface: t = typeof(System.Windows.Media.GlyphTypeface); break;
            case KnownElements.Glyphs: t = typeof(System.Windows.Documents.Glyphs); break;
            case KnownElements.GradientBrush: t = typeof(System.Windows.Media.GradientBrush); break;
            case KnownElements.GradientStop: t = typeof(System.Windows.Media.GradientStop); break;
            case KnownElements.GradientStopCollection: t = typeof(System.Windows.Media.GradientStopCollection); break;
            case KnownElements.Grid: t = typeof(System.Windows.Controls.Grid); break;
            case KnownElements.GridLength: t = typeof(System.Windows.GridLength); break;
            case KnownElements.GridLengthConverter: t = typeof(System.Windows.GridLengthConverter); break;
            case KnownElements.GridSplitter: t = typeof(System.Windows.Controls.GridSplitter); break;
            case KnownElements.GridView: t = typeof(System.Windows.Controls.GridView); break;
            case KnownElements.GridViewColumn: t = typeof(System.Windows.Controls.GridViewColumn); break;
            case KnownElements.GridViewColumnHeader: t = typeof(System.Windows.Controls.GridViewColumnHeader); break;
            case KnownElements.GridViewHeaderRowPresenter: t = typeof(System.Windows.Controls.GridViewHeaderRowPresenter); break;
            case KnownElements.GridViewRowPresenter: t = typeof(System.Windows.Controls.GridViewRowPresenter); break;
            case KnownElements.GridViewRowPresenterBase: t = typeof(System.Windows.Controls.Primitives.GridViewRowPresenterBase); break;
            case KnownElements.GroupBox: t = typeof(System.Windows.Controls.GroupBox); break;
            case KnownElements.GroupItem: t = typeof(System.Windows.Controls.GroupItem); break;
            case KnownElements.Guid: t = typeof(System.Guid); break;
            case KnownElements.GuidConverter: t = typeof(System.ComponentModel.GuidConverter); break;
            case KnownElements.GuidelineSet: t = typeof(System.Windows.Media.GuidelineSet); break;
            case KnownElements.HeaderedContentControl: t = typeof(System.Windows.Controls.HeaderedContentControl); break;
            case KnownElements.HeaderedItemsControl: t = typeof(System.Windows.Controls.HeaderedItemsControl); break;
            case KnownElements.HierarchicalDataTemplate: t = typeof(System.Windows.HierarchicalDataTemplate); break;
            case KnownElements.HostVisual: t = typeof(System.Windows.Media.HostVisual); break;
            case KnownElements.Hyperlink: t = typeof(System.Windows.Documents.Hyperlink); break;
            case KnownElements.IAddChild: t = typeof(System.Windows.Markup.IAddChild); break;
            case KnownElements.IAddChildInternal: t = typeof(System.Windows.Markup.IAddChildInternal); break;
            case KnownElements.ICommand: t = typeof(System.Windows.Input.ICommand); break;
            case KnownElements.IComponentConnector: t = typeof(System.Windows.Markup.IComponentConnector); break;
            case KnownElements.INameScope: t = typeof(System.Windows.Markup.INameScope); break;
            case KnownElements.IStyleConnector: t = typeof(System.Windows.Markup.IStyleConnector); break;
            case KnownElements.IconBitmapDecoder: t = typeof(System.Windows.Media.Imaging.IconBitmapDecoder); break;
            case KnownElements.Image: t = typeof(System.Windows.Controls.Image); break;
            case KnownElements.ImageBrush: t = typeof(System.Windows.Media.ImageBrush); break;
            case KnownElements.ImageDrawing: t = typeof(System.Windows.Media.ImageDrawing); break;
            case KnownElements.ImageMetadata: t = typeof(System.Windows.Media.ImageMetadata); break;
            case KnownElements.ImageSource: t = typeof(System.Windows.Media.ImageSource); break;
            case KnownElements.ImageSourceConverter: t = typeof(System.Windows.Media.ImageSourceConverter); break;
            case KnownElements.InPlaceBitmapMetadataWriter: t = typeof(System.Windows.Media.Imaging.InPlaceBitmapMetadataWriter); break;
            case KnownElements.InkCanvas: t = typeof(System.Windows.Controls.InkCanvas); break;
            case KnownElements.InkPresenter: t = typeof(System.Windows.Controls.InkPresenter); break;
            case KnownElements.Inline: t = typeof(System.Windows.Documents.Inline); break;
            case KnownElements.InlineCollection: t = typeof(System.Windows.Documents.InlineCollection); break;
            case KnownElements.InlineUIContainer: t = typeof(System.Windows.Documents.InlineUIContainer); break;
            case KnownElements.InputBinding: t = typeof(System.Windows.Input.InputBinding); break;
            case KnownElements.InputDevice: t = typeof(System.Windows.Input.InputDevice); break;
            case KnownElements.InputLanguageManager: t = typeof(System.Windows.Input.InputLanguageManager); break;
            case KnownElements.InputManager: t = typeof(System.Windows.Input.InputManager); break;
            case KnownElements.InputMethod: t = typeof(System.Windows.Input.InputMethod); break;
            case KnownElements.InputScope: t = typeof(System.Windows.Input.InputScope); break;
            case KnownElements.InputScopeConverter: t = typeof(System.Windows.Input.InputScopeConverter); break;
            case KnownElements.InputScopeName: t = typeof(System.Windows.Input.InputScopeName); break;
            case KnownElements.InputScopeNameConverter: t = typeof(System.Windows.Input.InputScopeNameConverter); break;
            case KnownElements.Int16: t = typeof(System.Int16); break;
            case KnownElements.Int16Animation: t = typeof(System.Windows.Media.Animation.Int16Animation); break;
            case KnownElements.Int16AnimationBase: t = typeof(System.Windows.Media.Animation.Int16AnimationBase); break;
            case KnownElements.Int16AnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.Int16AnimationUsingKeyFrames); break;
            case KnownElements.Int16Converter: t = typeof(System.ComponentModel.Int16Converter); break;
            case KnownElements.Int16KeyFrame: t = typeof(System.Windows.Media.Animation.Int16KeyFrame); break;
            case KnownElements.Int16KeyFrameCollection: t = typeof(System.Windows.Media.Animation.Int16KeyFrameCollection); break;
            case KnownElements.Int32: t = typeof(System.Int32); break;
            case KnownElements.Int32Animation: t = typeof(System.Windows.Media.Animation.Int32Animation); break;
            case KnownElements.Int32AnimationBase: t = typeof(System.Windows.Media.Animation.Int32AnimationBase); break;
            case KnownElements.Int32AnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.Int32AnimationUsingKeyFrames); break;
            case KnownElements.Int32Collection: t = typeof(System.Windows.Media.Int32Collection); break;
            case KnownElements.Int32CollectionConverter: t = typeof(System.Windows.Media.Int32CollectionConverter); break;
            case KnownElements.Int32Converter: t = typeof(System.ComponentModel.Int32Converter); break;
            case KnownElements.Int32KeyFrame: t = typeof(System.Windows.Media.Animation.Int32KeyFrame); break;
            case KnownElements.Int32KeyFrameCollection: t = typeof(System.Windows.Media.Animation.Int32KeyFrameCollection); break;
            case KnownElements.Int32Rect: t = typeof(System.Windows.Int32Rect); break;
            case KnownElements.Int32RectConverter: t = typeof(System.Windows.Int32RectConverter); break;
            case KnownElements.Int64: t = typeof(System.Int64); break;
            case KnownElements.Int64Animation: t = typeof(System.Windows.Media.Animation.Int64Animation); break;
            case KnownElements.Int64AnimationBase: t = typeof(System.Windows.Media.Animation.Int64AnimationBase); break;
            case KnownElements.Int64AnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.Int64AnimationUsingKeyFrames); break;
            case KnownElements.Int64Converter: t = typeof(System.ComponentModel.Int64Converter); break;
            case KnownElements.Int64KeyFrame: t = typeof(System.Windows.Media.Animation.Int64KeyFrame); break;
            case KnownElements.Int64KeyFrameCollection: t = typeof(System.Windows.Media.Animation.Int64KeyFrameCollection); break;
            case KnownElements.Italic: t = typeof(System.Windows.Documents.Italic); break;
            case KnownElements.ItemCollection: t = typeof(System.Windows.Controls.ItemCollection); break;
            case KnownElements.ItemsControl: t = typeof(System.Windows.Controls.ItemsControl); break;
            case KnownElements.ItemsPanelTemplate: t = typeof(System.Windows.Controls.ItemsPanelTemplate); break;
            case KnownElements.ItemsPresenter: t = typeof(System.Windows.Controls.ItemsPresenter); break;
            case KnownElements.JournalEntry: t = typeof(System.Windows.Navigation.JournalEntry); break;
            case KnownElements.JournalEntryListConverter: t = typeof(System.Windows.Navigation.JournalEntryListConverter); break;
            case KnownElements.JournalEntryUnifiedViewConverter: t = typeof(System.Windows.Navigation.JournalEntryUnifiedViewConverter); break;
            case KnownElements.JpegBitmapDecoder: t = typeof(System.Windows.Media.Imaging.JpegBitmapDecoder); break;
            case KnownElements.JpegBitmapEncoder: t = typeof(System.Windows.Media.Imaging.JpegBitmapEncoder); break;
            case KnownElements.KeyBinding: t = typeof(System.Windows.Input.KeyBinding); break;
            case KnownElements.KeyConverter: t = typeof(System.Windows.Input.KeyConverter); break;
            case KnownElements.KeyGesture: t = typeof(System.Windows.Input.KeyGesture); break;
            case KnownElements.KeyGestureConverter: t = typeof(System.Windows.Input.KeyGestureConverter); break;
            case KnownElements.KeySpline: t = typeof(System.Windows.Media.Animation.KeySpline); break;
            case KnownElements.KeySplineConverter: t = typeof(System.Windows.KeySplineConverter); break;
            case KnownElements.KeyTime: t = typeof(System.Windows.Media.Animation.KeyTime); break;
            case KnownElements.KeyTimeConverter: t = typeof(System.Windows.KeyTimeConverter); break;
            case KnownElements.KeyboardDevice: t = typeof(System.Windows.Input.KeyboardDevice); break;
            case KnownElements.Label: t = typeof(System.Windows.Controls.Label); break;
            case KnownElements.LateBoundBitmapDecoder: t = typeof(System.Windows.Media.Imaging.LateBoundBitmapDecoder); break;
            case KnownElements.LengthConverter: t = typeof(System.Windows.LengthConverter); break;
            case KnownElements.Light: t = typeof(System.Windows.Media.Media3D.Light); break;
            case KnownElements.Line: t = typeof(System.Windows.Shapes.Line); break;
            case KnownElements.LineBreak: t = typeof(System.Windows.Documents.LineBreak); break;
            case KnownElements.LineGeometry: t = typeof(System.Windows.Media.LineGeometry); break;
            case KnownElements.LineSegment: t = typeof(System.Windows.Media.LineSegment); break;
            case KnownElements.LinearByteKeyFrame: t = typeof(System.Windows.Media.Animation.LinearByteKeyFrame); break;
            case KnownElements.LinearColorKeyFrame: t = typeof(System.Windows.Media.Animation.LinearColorKeyFrame); break;
            case KnownElements.LinearDecimalKeyFrame: t = typeof(System.Windows.Media.Animation.LinearDecimalKeyFrame); break;
            case KnownElements.LinearDoubleKeyFrame: t = typeof(System.Windows.Media.Animation.LinearDoubleKeyFrame); break;
            case KnownElements.LinearGradientBrush: t = typeof(System.Windows.Media.LinearGradientBrush); break;
            case KnownElements.LinearInt16KeyFrame: t = typeof(System.Windows.Media.Animation.LinearInt16KeyFrame); break;
            case KnownElements.LinearInt32KeyFrame: t = typeof(System.Windows.Media.Animation.LinearInt32KeyFrame); break;
            case KnownElements.LinearInt64KeyFrame: t = typeof(System.Windows.Media.Animation.LinearInt64KeyFrame); break;
            case KnownElements.LinearPoint3DKeyFrame: t = typeof(System.Windows.Media.Animation.LinearPoint3DKeyFrame); break;
            case KnownElements.LinearPointKeyFrame: t = typeof(System.Windows.Media.Animation.LinearPointKeyFrame); break;
            case KnownElements.LinearQuaternionKeyFrame: t = typeof(System.Windows.Media.Animation.LinearQuaternionKeyFrame); break;
            case KnownElements.LinearRectKeyFrame: t = typeof(System.Windows.Media.Animation.LinearRectKeyFrame); break;
            case KnownElements.LinearRotation3DKeyFrame: t = typeof(System.Windows.Media.Animation.LinearRotation3DKeyFrame); break;
            case KnownElements.LinearSingleKeyFrame: t = typeof(System.Windows.Media.Animation.LinearSingleKeyFrame); break;
            case KnownElements.LinearSizeKeyFrame: t = typeof(System.Windows.Media.Animation.LinearSizeKeyFrame); break;
            case KnownElements.LinearThicknessKeyFrame: t = typeof(System.Windows.Media.Animation.LinearThicknessKeyFrame); break;
            case KnownElements.LinearVector3DKeyFrame: t = typeof(System.Windows.Media.Animation.LinearVector3DKeyFrame); break;
            case KnownElements.LinearVectorKeyFrame: t = typeof(System.Windows.Media.Animation.LinearVectorKeyFrame); break;
            case KnownElements.List: t = typeof(System.Windows.Documents.List); break;
            case KnownElements.ListBox: t = typeof(System.Windows.Controls.ListBox); break;
            case KnownElements.ListBoxItem: t = typeof(System.Windows.Controls.ListBoxItem); break;
            case KnownElements.ListCollectionView: t = typeof(System.Windows.Data.ListCollectionView); break;
            case KnownElements.ListItem: t = typeof(System.Windows.Documents.ListItem); break;
            case KnownElements.ListView: t = typeof(System.Windows.Controls.ListView); break;
            case KnownElements.ListViewItem: t = typeof(System.Windows.Controls.ListViewItem); break;
            case KnownElements.Localization: t = typeof(System.Windows.Localization); break;
            case KnownElements.LostFocusEventManager: t = typeof(System.Windows.LostFocusEventManager); break;
            case KnownElements.MarkupExtension: t = typeof(System.Windows.Markup.MarkupExtension); break;
            case KnownElements.Material: t = typeof(System.Windows.Media.Media3D.Material); break;
            case KnownElements.MaterialCollection: t = typeof(System.Windows.Media.Media3D.MaterialCollection); break;
            case KnownElements.MaterialGroup: t = typeof(System.Windows.Media.Media3D.MaterialGroup); break;
            case KnownElements.Matrix: t = typeof(System.Windows.Media.Matrix); break;
            case KnownElements.Matrix3D: t = typeof(System.Windows.Media.Media3D.Matrix3D); break;
            case KnownElements.Matrix3DConverter: t = typeof(System.Windows.Media.Media3D.Matrix3DConverter); break;
            case KnownElements.MatrixAnimationBase: t = typeof(System.Windows.Media.Animation.MatrixAnimationBase); break;
            case KnownElements.MatrixAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.MatrixAnimationUsingKeyFrames); break;
            case KnownElements.MatrixAnimationUsingPath: t = typeof(System.Windows.Media.Animation.MatrixAnimationUsingPath); break;
            case KnownElements.MatrixCamera: t = typeof(System.Windows.Media.Media3D.MatrixCamera); break;
            case KnownElements.MatrixConverter: t = typeof(System.Windows.Media.MatrixConverter); break;
            case KnownElements.MatrixKeyFrame: t = typeof(System.Windows.Media.Animation.MatrixKeyFrame); break;
            case KnownElements.MatrixKeyFrameCollection: t = typeof(System.Windows.Media.Animation.MatrixKeyFrameCollection); break;
            case KnownElements.MatrixTransform: t = typeof(System.Windows.Media.MatrixTransform); break;
            case KnownElements.MatrixTransform3D: t = typeof(System.Windows.Media.Media3D.MatrixTransform3D); break;
            case KnownElements.MediaClock: t = typeof(System.Windows.Media.MediaClock); break;
            case KnownElements.MediaElement: t = typeof(System.Windows.Controls.MediaElement); break;
            case KnownElements.MediaPlayer: t = typeof(System.Windows.Media.MediaPlayer); break;
            case KnownElements.MediaTimeline: t = typeof(System.Windows.Media.MediaTimeline); break;
            case KnownElements.Menu: t = typeof(System.Windows.Controls.Menu); break;
            case KnownElements.MenuBase: t = typeof(System.Windows.Controls.Primitives.MenuBase); break;
            case KnownElements.MenuItem: t = typeof(System.Windows.Controls.MenuItem); break;
            case KnownElements.MenuScrollingVisibilityConverter: t = typeof(System.Windows.Controls.MenuScrollingVisibilityConverter); break;
            case KnownElements.MeshGeometry3D: t = typeof(System.Windows.Media.Media3D.MeshGeometry3D); break;
            case KnownElements.Model3D: t = typeof(System.Windows.Media.Media3D.Model3D); break;
            case KnownElements.Model3DCollection: t = typeof(System.Windows.Media.Media3D.Model3DCollection); break;
            case KnownElements.Model3DGroup: t = typeof(System.Windows.Media.Media3D.Model3DGroup); break;
            case KnownElements.ModelVisual3D: t = typeof(System.Windows.Media.Media3D.ModelVisual3D); break;
            case KnownElements.ModifierKeysConverter: t = typeof(System.Windows.Input.ModifierKeysConverter); break;
            case KnownElements.MouseActionConverter: t = typeof(System.Windows.Input.MouseActionConverter); break;
            case KnownElements.MouseBinding: t = typeof(System.Windows.Input.MouseBinding); break;
            case KnownElements.MouseDevice: t = typeof(System.Windows.Input.MouseDevice); break;
            case KnownElements.MouseGesture: t = typeof(System.Windows.Input.MouseGesture); break;
            case KnownElements.MouseGestureConverter: t = typeof(System.Windows.Input.MouseGestureConverter); break;
            case KnownElements.MultiBinding: t = typeof(System.Windows.Data.MultiBinding); break;
            case KnownElements.MultiBindingExpression: t = typeof(System.Windows.Data.MultiBindingExpression); break;
            case KnownElements.MultiDataTrigger: t = typeof(System.Windows.MultiDataTrigger); break;
            case KnownElements.MultiTrigger: t = typeof(System.Windows.MultiTrigger); break;
            case KnownElements.NameScope: t = typeof(System.Windows.NameScope); break;
            case KnownElements.NavigationWindow: t = typeof(System.Windows.Navigation.NavigationWindow); break;
            case KnownElements.NullExtension: t = typeof(System.Windows.Markup.NullExtension); break;
            case KnownElements.NullableBoolConverter: t = typeof(System.Windows.NullableBoolConverter); break;
            case KnownElements.NullableConverter: t = typeof(System.ComponentModel.NullableConverter); break;
            case KnownElements.NumberSubstitution: t = typeof(System.Windows.Media.NumberSubstitution); break;
            case KnownElements.Object: t = typeof(System.Object); break;
            case KnownElements.ObjectAnimationBase: t = typeof(System.Windows.Media.Animation.ObjectAnimationBase); break;
            case KnownElements.ObjectAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.ObjectAnimationUsingKeyFrames); break;
            case KnownElements.ObjectDataProvider: t = typeof(System.Windows.Data.ObjectDataProvider); break;
            case KnownElements.ObjectKeyFrame: t = typeof(System.Windows.Media.Animation.ObjectKeyFrame); break;
            case KnownElements.ObjectKeyFrameCollection: t = typeof(System.Windows.Media.Animation.ObjectKeyFrameCollection); break;
            case KnownElements.OrthographicCamera: t = typeof(System.Windows.Media.Media3D.OrthographicCamera); break;
            case KnownElements.OuterGlowBitmapEffect: t = typeof(System.Windows.Media.Effects.OuterGlowBitmapEffect); break;
            case KnownElements.Page: t = typeof(System.Windows.Controls.Page); break;
            case KnownElements.PageContent: t = typeof(System.Windows.Documents.PageContent); break;
            case KnownElements.PageFunctionBase: t = typeof(System.Windows.Navigation.PageFunctionBase); break;
            case KnownElements.Panel: t = typeof(System.Windows.Controls.Panel); break;
            case KnownElements.Paragraph: t = typeof(System.Windows.Documents.Paragraph); break;
            case KnownElements.ParallelTimeline: t = typeof(System.Windows.Media.Animation.ParallelTimeline); break;
            case KnownElements.ParserContext: t = typeof(System.Windows.Markup.ParserContext); break;
            case KnownElements.PasswordBox: t = typeof(System.Windows.Controls.PasswordBox); break;
            case KnownElements.Path: t = typeof(System.Windows.Shapes.Path); break;
            case KnownElements.PathFigure: t = typeof(System.Windows.Media.PathFigure); break;
            case KnownElements.PathFigureCollection: t = typeof(System.Windows.Media.PathFigureCollection); break;
            case KnownElements.PathFigureCollectionConverter: t = typeof(System.Windows.Media.PathFigureCollectionConverter); break;
            case KnownElements.PathGeometry: t = typeof(System.Windows.Media.PathGeometry); break;
            case KnownElements.PathSegment: t = typeof(System.Windows.Media.PathSegment); break;
            case KnownElements.PathSegmentCollection: t = typeof(System.Windows.Media.PathSegmentCollection); break;
            case KnownElements.PauseStoryboard: t = typeof(System.Windows.Media.Animation.PauseStoryboard); break;
            case KnownElements.Pen: t = typeof(System.Windows.Media.Pen); break;
            case KnownElements.PerspectiveCamera: t = typeof(System.Windows.Media.Media3D.PerspectiveCamera); break;
            case KnownElements.PixelFormat: t = typeof(System.Windows.Media.PixelFormat); break;
            case KnownElements.PixelFormatConverter: t = typeof(System.Windows.Media.PixelFormatConverter); break;
            case KnownElements.PngBitmapDecoder: t = typeof(System.Windows.Media.Imaging.PngBitmapDecoder); break;
            case KnownElements.PngBitmapEncoder: t = typeof(System.Windows.Media.Imaging.PngBitmapEncoder); break;
            case KnownElements.Point: t = typeof(System.Windows.Point); break;
            case KnownElements.Point3D: t = typeof(System.Windows.Media.Media3D.Point3D); break;
            case KnownElements.Point3DAnimation: t = typeof(System.Windows.Media.Animation.Point3DAnimation); break;
            case KnownElements.Point3DAnimationBase: t = typeof(System.Windows.Media.Animation.Point3DAnimationBase); break;
            case KnownElements.Point3DAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.Point3DAnimationUsingKeyFrames); break;
            case KnownElements.Point3DCollection: t = typeof(System.Windows.Media.Media3D.Point3DCollection); break;
            case KnownElements.Point3DCollectionConverter: t = typeof(System.Windows.Media.Media3D.Point3DCollectionConverter); break;
            case KnownElements.Point3DConverter: t = typeof(System.Windows.Media.Media3D.Point3DConverter); break;
            case KnownElements.Point3DKeyFrame: t = typeof(System.Windows.Media.Animation.Point3DKeyFrame); break;
            case KnownElements.Point3DKeyFrameCollection: t = typeof(System.Windows.Media.Animation.Point3DKeyFrameCollection); break;
            case KnownElements.Point4D: t = typeof(System.Windows.Media.Media3D.Point4D); break;
            case KnownElements.Point4DConverter: t = typeof(System.Windows.Media.Media3D.Point4DConverter); break;
            case KnownElements.PointAnimation: t = typeof(System.Windows.Media.Animation.PointAnimation); break;
            case KnownElements.PointAnimationBase: t = typeof(System.Windows.Media.Animation.PointAnimationBase); break;
            case KnownElements.PointAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.PointAnimationUsingKeyFrames); break;
            case KnownElements.PointAnimationUsingPath: t = typeof(System.Windows.Media.Animation.PointAnimationUsingPath); break;
            case KnownElements.PointCollection: t = typeof(System.Windows.Media.PointCollection); break;
            case KnownElements.PointCollectionConverter: t = typeof(System.Windows.Media.PointCollectionConverter); break;
            case KnownElements.PointConverter: t = typeof(System.Windows.PointConverter); break;
            case KnownElements.PointIListConverter: t = typeof(System.Windows.Media.Converters.PointIListConverter); break;
            case KnownElements.PointKeyFrame: t = typeof(System.Windows.Media.Animation.PointKeyFrame); break;
            case KnownElements.PointKeyFrameCollection: t = typeof(System.Windows.Media.Animation.PointKeyFrameCollection); break;
            case KnownElements.PointLight: t = typeof(System.Windows.Media.Media3D.PointLight); break;
            case KnownElements.PointLightBase: t = typeof(System.Windows.Media.Media3D.PointLightBase); break;
            case KnownElements.PolyBezierSegment: t = typeof(System.Windows.Media.PolyBezierSegment); break;
            case KnownElements.PolyLineSegment: t = typeof(System.Windows.Media.PolyLineSegment); break;
            case KnownElements.PolyQuadraticBezierSegment: t = typeof(System.Windows.Media.PolyQuadraticBezierSegment); break;
            case KnownElements.Polygon: t = typeof(System.Windows.Shapes.Polygon); break;
            case KnownElements.Polyline: t = typeof(System.Windows.Shapes.Polyline); break;
            case KnownElements.Popup: t = typeof(System.Windows.Controls.Primitives.Popup); break;
            case KnownElements.PresentationSource: t = typeof(System.Windows.PresentationSource); break;
            case KnownElements.PriorityBinding: t = typeof(System.Windows.Data.PriorityBinding); break;
            case KnownElements.PriorityBindingExpression: t = typeof(System.Windows.Data.PriorityBindingExpression); break;
            case KnownElements.ProgressBar: t = typeof(System.Windows.Controls.ProgressBar); break;
            case KnownElements.ProjectionCamera: t = typeof(System.Windows.Media.Media3D.ProjectionCamera); break;
            case KnownElements.PropertyPath: t = typeof(System.Windows.PropertyPath); break;
            case KnownElements.PropertyPathConverter: t = typeof(System.Windows.PropertyPathConverter); break;
            case KnownElements.QuadraticBezierSegment: t = typeof(System.Windows.Media.QuadraticBezierSegment); break;
            case KnownElements.Quaternion: t = typeof(System.Windows.Media.Media3D.Quaternion); break;
            case KnownElements.QuaternionAnimation: t = typeof(System.Windows.Media.Animation.QuaternionAnimation); break;
            case KnownElements.QuaternionAnimationBase: t = typeof(System.Windows.Media.Animation.QuaternionAnimationBase); break;
            case KnownElements.QuaternionAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.QuaternionAnimationUsingKeyFrames); break;
            case KnownElements.QuaternionConverter: t = typeof(System.Windows.Media.Media3D.QuaternionConverter); break;
            case KnownElements.QuaternionKeyFrame: t = typeof(System.Windows.Media.Animation.QuaternionKeyFrame); break;
            case KnownElements.QuaternionKeyFrameCollection: t = typeof(System.Windows.Media.Animation.QuaternionKeyFrameCollection); break;
            case KnownElements.QuaternionRotation3D: t = typeof(System.Windows.Media.Media3D.QuaternionRotation3D); break;
            case KnownElements.RadialGradientBrush: t = typeof(System.Windows.Media.RadialGradientBrush); break;
            case KnownElements.RadioButton: t = typeof(System.Windows.Controls.RadioButton); break;
            case KnownElements.RangeBase: t = typeof(System.Windows.Controls.Primitives.RangeBase); break;
            case KnownElements.Rect: t = typeof(System.Windows.Rect); break;
            case KnownElements.Rect3D: t = typeof(System.Windows.Media.Media3D.Rect3D); break;
            case KnownElements.Rect3DConverter: t = typeof(System.Windows.Media.Media3D.Rect3DConverter); break;
            case KnownElements.RectAnimation: t = typeof(System.Windows.Media.Animation.RectAnimation); break;
            case KnownElements.RectAnimationBase: t = typeof(System.Windows.Media.Animation.RectAnimationBase); break;
            case KnownElements.RectAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.RectAnimationUsingKeyFrames); break;
            case KnownElements.RectConverter: t = typeof(System.Windows.RectConverter); break;
            case KnownElements.RectKeyFrame: t = typeof(System.Windows.Media.Animation.RectKeyFrame); break;
            case KnownElements.RectKeyFrameCollection: t = typeof(System.Windows.Media.Animation.RectKeyFrameCollection); break;
            case KnownElements.Rectangle: t = typeof(System.Windows.Shapes.Rectangle); break;
            case KnownElements.RectangleGeometry: t = typeof(System.Windows.Media.RectangleGeometry); break;
            case KnownElements.RelativeSource: t = typeof(System.Windows.Data.RelativeSource); break;
            case KnownElements.RemoveStoryboard: t = typeof(System.Windows.Media.Animation.RemoveStoryboard); break;
            case KnownElements.RenderOptions: t = typeof(System.Windows.Media.RenderOptions); break;
            case KnownElements.RenderTargetBitmap: t = typeof(System.Windows.Media.Imaging.RenderTargetBitmap); break;
            case KnownElements.RepeatBehavior: t = typeof(System.Windows.Media.Animation.RepeatBehavior); break;
            case KnownElements.RepeatBehaviorConverter: t = typeof(System.Windows.Media.Animation.RepeatBehaviorConverter); break;
            case KnownElements.RepeatButton: t = typeof(System.Windows.Controls.Primitives.RepeatButton); break;
            case KnownElements.ResizeGrip: t = typeof(System.Windows.Controls.Primitives.ResizeGrip); break;
            case KnownElements.ResourceDictionary: t = typeof(System.Windows.ResourceDictionary); break;
            case KnownElements.ResourceKey: t = typeof(System.Windows.ResourceKey); break;
            case KnownElements.ResumeStoryboard: t = typeof(System.Windows.Media.Animation.ResumeStoryboard); break;
            case KnownElements.RichTextBox: t = typeof(System.Windows.Controls.RichTextBox); break;
            case KnownElements.RotateTransform: t = typeof(System.Windows.Media.RotateTransform); break;
            case KnownElements.RotateTransform3D: t = typeof(System.Windows.Media.Media3D.RotateTransform3D); break;
            case KnownElements.Rotation3D: t = typeof(System.Windows.Media.Media3D.Rotation3D); break;
            case KnownElements.Rotation3DAnimation: t = typeof(System.Windows.Media.Animation.Rotation3DAnimation); break;
            case KnownElements.Rotation3DAnimationBase: t = typeof(System.Windows.Media.Animation.Rotation3DAnimationBase); break;
            case KnownElements.Rotation3DAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.Rotation3DAnimationUsingKeyFrames); break;
            case KnownElements.Rotation3DKeyFrame: t = typeof(System.Windows.Media.Animation.Rotation3DKeyFrame); break;
            case KnownElements.Rotation3DKeyFrameCollection: t = typeof(System.Windows.Media.Animation.Rotation3DKeyFrameCollection); break;
            case KnownElements.RoutedCommand: t = typeof(System.Windows.Input.RoutedCommand); break;
            case KnownElements.RoutedEvent: t = typeof(System.Windows.RoutedEvent); break;
            case KnownElements.RoutedEventConverter: t = typeof(System.Windows.Markup.RoutedEventConverter); break;
            case KnownElements.RoutedUICommand: t = typeof(System.Windows.Input.RoutedUICommand); break;
            case KnownElements.RoutingStrategy: t = typeof(System.Windows.RoutingStrategy); break;
            case KnownElements.RowDefinition: t = typeof(System.Windows.Controls.RowDefinition); break;
            case KnownElements.Run: t = typeof(System.Windows.Documents.Run); break;
            case KnownElements.RuntimeNamePropertyAttribute: t = typeof(System.Windows.Markup.RuntimeNamePropertyAttribute); break;
            case KnownElements.SByte: t = typeof(System.SByte); break;
            case KnownElements.SByteConverter: t = typeof(System.ComponentModel.SByteConverter); break;
            case KnownElements.ScaleTransform: t = typeof(System.Windows.Media.ScaleTransform); break;
            case KnownElements.ScaleTransform3D: t = typeof(System.Windows.Media.Media3D.ScaleTransform3D); break;
            case KnownElements.ScrollBar: t = typeof(System.Windows.Controls.Primitives.ScrollBar); break;
            case KnownElements.ScrollContentPresenter: t = typeof(System.Windows.Controls.ScrollContentPresenter); break;
            case KnownElements.ScrollViewer: t = typeof(System.Windows.Controls.ScrollViewer); break;
            case KnownElements.Section: t = typeof(System.Windows.Documents.Section); break;
            case KnownElements.SeekStoryboard: t = typeof(System.Windows.Media.Animation.SeekStoryboard); break;
            case KnownElements.Selector: t = typeof(System.Windows.Controls.Primitives.Selector); break;
            case KnownElements.Separator: t = typeof(System.Windows.Controls.Separator); break;
            case KnownElements.SetStoryboardSpeedRatio: t = typeof(System.Windows.Media.Animation.SetStoryboardSpeedRatio); break;
            case KnownElements.Setter: t = typeof(System.Windows.Setter); break;
            case KnownElements.SetterBase: t = typeof(System.Windows.SetterBase); break;
            case KnownElements.Shape: t = typeof(System.Windows.Shapes.Shape); break;
            case KnownElements.Single: t = typeof(System.Single); break;
            case KnownElements.SingleAnimation: t = typeof(System.Windows.Media.Animation.SingleAnimation); break;
            case KnownElements.SingleAnimationBase: t = typeof(System.Windows.Media.Animation.SingleAnimationBase); break;
            case KnownElements.SingleAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.SingleAnimationUsingKeyFrames); break;
            case KnownElements.SingleConverter: t = typeof(System.ComponentModel.SingleConverter); break;
            case KnownElements.SingleKeyFrame: t = typeof(System.Windows.Media.Animation.SingleKeyFrame); break;
            case KnownElements.SingleKeyFrameCollection: t = typeof(System.Windows.Media.Animation.SingleKeyFrameCollection); break;
            case KnownElements.Size: t = typeof(System.Windows.Size); break;
            case KnownElements.Size3D: t = typeof(System.Windows.Media.Media3D.Size3D); break;
            case KnownElements.Size3DConverter: t = typeof(System.Windows.Media.Media3D.Size3DConverter); break;
            case KnownElements.SizeAnimation: t = typeof(System.Windows.Media.Animation.SizeAnimation); break;
            case KnownElements.SizeAnimationBase: t = typeof(System.Windows.Media.Animation.SizeAnimationBase); break;
            case KnownElements.SizeAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.SizeAnimationUsingKeyFrames); break;
            case KnownElements.SizeConverter: t = typeof(System.Windows.SizeConverter); break;
            case KnownElements.SizeKeyFrame: t = typeof(System.Windows.Media.Animation.SizeKeyFrame); break;
            case KnownElements.SizeKeyFrameCollection: t = typeof(System.Windows.Media.Animation.SizeKeyFrameCollection); break;
            case KnownElements.SkewTransform: t = typeof(System.Windows.Media.SkewTransform); break;
            case KnownElements.SkipStoryboardToFill: t = typeof(System.Windows.Media.Animation.SkipStoryboardToFill); break;
            case KnownElements.Slider: t = typeof(System.Windows.Controls.Slider); break;
            case KnownElements.SolidColorBrush: t = typeof(System.Windows.Media.SolidColorBrush); break;
            case KnownElements.SoundPlayerAction: t = typeof(System.Windows.Controls.SoundPlayerAction); break;
            case KnownElements.Span: t = typeof(System.Windows.Documents.Span); break;
            case KnownElements.SpecularMaterial: t = typeof(System.Windows.Media.Media3D.SpecularMaterial); break;
            case KnownElements.SpellCheck: t = typeof(System.Windows.Controls.SpellCheck); break;
            case KnownElements.SplineByteKeyFrame: t = typeof(System.Windows.Media.Animation.SplineByteKeyFrame); break;
            case KnownElements.SplineColorKeyFrame: t = typeof(System.Windows.Media.Animation.SplineColorKeyFrame); break;
            case KnownElements.SplineDecimalKeyFrame: t = typeof(System.Windows.Media.Animation.SplineDecimalKeyFrame); break;
            case KnownElements.SplineDoubleKeyFrame: t = typeof(System.Windows.Media.Animation.SplineDoubleKeyFrame); break;
            case KnownElements.SplineInt16KeyFrame: t = typeof(System.Windows.Media.Animation.SplineInt16KeyFrame); break;
            case KnownElements.SplineInt32KeyFrame: t = typeof(System.Windows.Media.Animation.SplineInt32KeyFrame); break;
            case KnownElements.SplineInt64KeyFrame: t = typeof(System.Windows.Media.Animation.SplineInt64KeyFrame); break;
            case KnownElements.SplinePoint3DKeyFrame: t = typeof(System.Windows.Media.Animation.SplinePoint3DKeyFrame); break;
            case KnownElements.SplinePointKeyFrame: t = typeof(System.Windows.Media.Animation.SplinePointKeyFrame); break;
            case KnownElements.SplineQuaternionKeyFrame: t = typeof(System.Windows.Media.Animation.SplineQuaternionKeyFrame); break;
            case KnownElements.SplineRectKeyFrame: t = typeof(System.Windows.Media.Animation.SplineRectKeyFrame); break;
            case KnownElements.SplineRotation3DKeyFrame: t = typeof(System.Windows.Media.Animation.SplineRotation3DKeyFrame); break;
            case KnownElements.SplineSingleKeyFrame: t = typeof(System.Windows.Media.Animation.SplineSingleKeyFrame); break;
            case KnownElements.SplineSizeKeyFrame: t = typeof(System.Windows.Media.Animation.SplineSizeKeyFrame); break;
            case KnownElements.SplineThicknessKeyFrame: t = typeof(System.Windows.Media.Animation.SplineThicknessKeyFrame); break;
            case KnownElements.SplineVector3DKeyFrame: t = typeof(System.Windows.Media.Animation.SplineVector3DKeyFrame); break;
            case KnownElements.SplineVectorKeyFrame: t = typeof(System.Windows.Media.Animation.SplineVectorKeyFrame); break;
            case KnownElements.SpotLight: t = typeof(System.Windows.Media.Media3D.SpotLight); break;
            case KnownElements.StackPanel: t = typeof(System.Windows.Controls.StackPanel); break;
            case KnownElements.StaticExtension: t = typeof(System.Windows.Markup.StaticExtension); break;
            case KnownElements.StaticResourceExtension: t = typeof(System.Windows.StaticResourceExtension); break;
            case KnownElements.StatusBar: t = typeof(System.Windows.Controls.Primitives.StatusBar); break;
            case KnownElements.StatusBarItem: t = typeof(System.Windows.Controls.Primitives.StatusBarItem); break;
            case KnownElements.StickyNoteControl: t = typeof(System.Windows.Controls.StickyNoteControl); break;
            case KnownElements.StopStoryboard: t = typeof(System.Windows.Media.Animation.StopStoryboard); break;
            case KnownElements.Storyboard: t = typeof(System.Windows.Media.Animation.Storyboard); break;
            case KnownElements.StreamGeometry: t = typeof(System.Windows.Media.StreamGeometry); break;
            case KnownElements.StreamGeometryContext: t = typeof(System.Windows.Media.StreamGeometryContext); break;
            case KnownElements.StreamResourceInfo: t = typeof(System.Windows.Resources.StreamResourceInfo); break;
            case KnownElements.String: t = typeof(System.String); break;
            case KnownElements.StringAnimationBase: t = typeof(System.Windows.Media.Animation.StringAnimationBase); break;
            case KnownElements.StringAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.StringAnimationUsingKeyFrames); break;
            case KnownElements.StringConverter: t = typeof(System.ComponentModel.StringConverter); break;
            case KnownElements.StringKeyFrame: t = typeof(System.Windows.Media.Animation.StringKeyFrame); break;
            case KnownElements.StringKeyFrameCollection: t = typeof(System.Windows.Media.Animation.StringKeyFrameCollection); break;
            case KnownElements.StrokeCollection: t = typeof(System.Windows.Ink.StrokeCollection); break;
            case KnownElements.StrokeCollectionConverter: t = typeof(System.Windows.StrokeCollectionConverter); break;
            case KnownElements.Style: t = typeof(System.Windows.Style); break;
            case KnownElements.Stylus: t = typeof(System.Windows.Input.Stylus); break;
            case KnownElements.StylusDevice: t = typeof(System.Windows.Input.StylusDevice); break;
            case KnownElements.TabControl: t = typeof(System.Windows.Controls.TabControl); break;
            case KnownElements.TabItem: t = typeof(System.Windows.Controls.TabItem); break;
            case KnownElements.TabPanel: t = typeof(System.Windows.Controls.Primitives.TabPanel); break;
            case KnownElements.Table: t = typeof(System.Windows.Documents.Table); break;
            case KnownElements.TableCell: t = typeof(System.Windows.Documents.TableCell); break;
            case KnownElements.TableColumn: t = typeof(System.Windows.Documents.TableColumn); break;
            case KnownElements.TableRow: t = typeof(System.Windows.Documents.TableRow); break;
            case KnownElements.TableRowGroup: t = typeof(System.Windows.Documents.TableRowGroup); break;
            case KnownElements.TabletDevice: t = typeof(System.Windows.Input.TabletDevice); break;
            case KnownElements.TemplateBindingExpression: t = typeof(System.Windows.TemplateBindingExpression); break;
            case KnownElements.TemplateBindingExpressionConverter: t = typeof(System.Windows.TemplateBindingExpressionConverter); break;
            case KnownElements.TemplateBindingExtension: t = typeof(System.Windows.TemplateBindingExtension); break;
            case KnownElements.TemplateBindingExtensionConverter: t = typeof(System.Windows.TemplateBindingExtensionConverter); break;
            case KnownElements.TemplateKey: t = typeof(System.Windows.TemplateKey); break;
            case KnownElements.TemplateKeyConverter: t = typeof(System.Windows.Markup.TemplateKeyConverter); break;
            case KnownElements.TextBlock: t = typeof(System.Windows.Controls.TextBlock); break;
            case KnownElements.TextBox: t = typeof(System.Windows.Controls.TextBox); break;
            case KnownElements.TextBoxBase: t = typeof(System.Windows.Controls.Primitives.TextBoxBase); break;
            case KnownElements.TextComposition: t = typeof(System.Windows.Input.TextComposition); break;
            case KnownElements.TextCompositionManager: t = typeof(System.Windows.Input.TextCompositionManager); break;
            case KnownElements.TextDecoration: t = typeof(System.Windows.TextDecoration); break;
            case KnownElements.TextDecorationCollection: t = typeof(System.Windows.TextDecorationCollection); break;
            case KnownElements.TextDecorationCollectionConverter: t = typeof(System.Windows.TextDecorationCollectionConverter); break;
            case KnownElements.TextEffect: t = typeof(System.Windows.Media.TextEffect); break;
            case KnownElements.TextEffectCollection: t = typeof(System.Windows.Media.TextEffectCollection); break;
            case KnownElements.TextElement: t = typeof(System.Windows.Documents.TextElement); break;
            case KnownElements.TextSearch: t = typeof(System.Windows.Controls.TextSearch); break;
            case KnownElements.ThemeDictionaryExtension: t = typeof(System.Windows.ThemeDictionaryExtension); break;
            case KnownElements.Thickness: t = typeof(System.Windows.Thickness); break;
            case KnownElements.ThicknessAnimation: t = typeof(System.Windows.Media.Animation.ThicknessAnimation); break;
            case KnownElements.ThicknessAnimationBase: t = typeof(System.Windows.Media.Animation.ThicknessAnimationBase); break;
            case KnownElements.ThicknessAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames); break;
            case KnownElements.ThicknessConverter: t = typeof(System.Windows.ThicknessConverter); break;
            case KnownElements.ThicknessKeyFrame: t = typeof(System.Windows.Media.Animation.ThicknessKeyFrame); break;
            case KnownElements.ThicknessKeyFrameCollection: t = typeof(System.Windows.Media.Animation.ThicknessKeyFrameCollection); break;
            case KnownElements.Thumb: t = typeof(System.Windows.Controls.Primitives.Thumb); break;
            case KnownElements.TickBar: t = typeof(System.Windows.Controls.Primitives.TickBar); break;
            case KnownElements.TiffBitmapDecoder: t = typeof(System.Windows.Media.Imaging.TiffBitmapDecoder); break;
            case KnownElements.TiffBitmapEncoder: t = typeof(System.Windows.Media.Imaging.TiffBitmapEncoder); break;
            case KnownElements.TileBrush: t = typeof(System.Windows.Media.TileBrush); break;
            case KnownElements.TimeSpan: t = typeof(System.TimeSpan); break;
            case KnownElements.TimeSpanConverter: t = typeof(System.ComponentModel.TimeSpanConverter); break;
            case KnownElements.Timeline: t = typeof(System.Windows.Media.Animation.Timeline); break;
            case KnownElements.TimelineCollection: t = typeof(System.Windows.Media.Animation.TimelineCollection); break;
            case KnownElements.TimelineGroup: t = typeof(System.Windows.Media.Animation.TimelineGroup); break;
            case KnownElements.ToggleButton: t = typeof(System.Windows.Controls.Primitives.ToggleButton); break;
            case KnownElements.ToolBar: t = typeof(System.Windows.Controls.ToolBar); break;
            case KnownElements.ToolBarOverflowPanel: t = typeof(System.Windows.Controls.Primitives.ToolBarOverflowPanel); break;
            case KnownElements.ToolBarPanel: t = typeof(System.Windows.Controls.Primitives.ToolBarPanel); break;
            case KnownElements.ToolBarTray: t = typeof(System.Windows.Controls.ToolBarTray); break;
            case KnownElements.ToolTip: t = typeof(System.Windows.Controls.ToolTip); break;
            case KnownElements.ToolTipService: t = typeof(System.Windows.Controls.ToolTipService); break;
            case KnownElements.Track: t = typeof(System.Windows.Controls.Primitives.Track); break;
            case KnownElements.Transform: t = typeof(System.Windows.Media.Transform); break;
            case KnownElements.Transform3D: t = typeof(System.Windows.Media.Media3D.Transform3D); break;
            case KnownElements.Transform3DCollection: t = typeof(System.Windows.Media.Media3D.Transform3DCollection); break;
            case KnownElements.Transform3DGroup: t = typeof(System.Windows.Media.Media3D.Transform3DGroup); break;
            case KnownElements.TransformCollection: t = typeof(System.Windows.Media.TransformCollection); break;
            case KnownElements.TransformConverter: t = typeof(System.Windows.Media.TransformConverter); break;
            case KnownElements.TransformGroup: t = typeof(System.Windows.Media.TransformGroup); break;
            case KnownElements.TransformedBitmap: t = typeof(System.Windows.Media.Imaging.TransformedBitmap); break;
            case KnownElements.TranslateTransform: t = typeof(System.Windows.Media.TranslateTransform); break;
            case KnownElements.TranslateTransform3D: t = typeof(System.Windows.Media.Media3D.TranslateTransform3D); break;
            case KnownElements.TreeView: t = typeof(System.Windows.Controls.TreeView); break;
            case KnownElements.TreeViewItem: t = typeof(System.Windows.Controls.TreeViewItem); break;
            case KnownElements.Trigger: t = typeof(System.Windows.Trigger); break;
            case KnownElements.TriggerAction: t = typeof(System.Windows.TriggerAction); break;
            case KnownElements.TriggerBase: t = typeof(System.Windows.TriggerBase); break;
            case KnownElements.TypeExtension: t = typeof(System.Windows.Markup.TypeExtension); break;
            case KnownElements.TypeTypeConverter: t = typeof(System.Windows.Markup.TypeTypeConverter); break;
            case KnownElements.Typography: t = typeof(System.Windows.Documents.Typography); break;
            case KnownElements.UIElement: t = typeof(System.Windows.UIElement); break;
            case KnownElements.UInt16: t = typeof(System.UInt16); break;
            case KnownElements.UInt16Converter: t = typeof(System.ComponentModel.UInt16Converter); break;
            case KnownElements.UInt32: t = typeof(System.UInt32); break;
            case KnownElements.UInt32Converter: t = typeof(System.ComponentModel.UInt32Converter); break;
            case KnownElements.UInt64: t = typeof(System.UInt64); break;
            case KnownElements.UInt64Converter: t = typeof(System.ComponentModel.UInt64Converter); break;
            case KnownElements.UShortIListConverter: t = typeof(System.Windows.Media.Converters.UShortIListConverter); break;
            case KnownElements.Underline: t = typeof(System.Windows.Documents.Underline); break;
            case KnownElements.UniformGrid: t = typeof(System.Windows.Controls.Primitives.UniformGrid); break;
            case KnownElements.Uri: t = typeof(System.Uri); break;
            case KnownElements.UriTypeConverter: t = typeof(System.UriTypeConverter); break;
            case KnownElements.UserControl: t = typeof(System.Windows.Controls.UserControl); break;
            case KnownElements.Validation: t = typeof(System.Windows.Controls.Validation); break;
            case KnownElements.Vector: t = typeof(System.Windows.Vector); break;
            case KnownElements.Vector3D: t = typeof(System.Windows.Media.Media3D.Vector3D); break;
            case KnownElements.Vector3DAnimation: t = typeof(System.Windows.Media.Animation.Vector3DAnimation); break;
            case KnownElements.Vector3DAnimationBase: t = typeof(System.Windows.Media.Animation.Vector3DAnimationBase); break;
            case KnownElements.Vector3DAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.Vector3DAnimationUsingKeyFrames); break;
            case KnownElements.Vector3DCollection: t = typeof(System.Windows.Media.Media3D.Vector3DCollection); break;
            case KnownElements.Vector3DCollectionConverter: t = typeof(System.Windows.Media.Media3D.Vector3DCollectionConverter); break;
            case KnownElements.Vector3DConverter: t = typeof(System.Windows.Media.Media3D.Vector3DConverter); break;
            case KnownElements.Vector3DKeyFrame: t = typeof(System.Windows.Media.Animation.Vector3DKeyFrame); break;
            case KnownElements.Vector3DKeyFrameCollection: t = typeof(System.Windows.Media.Animation.Vector3DKeyFrameCollection); break;
            case KnownElements.VectorAnimation: t = typeof(System.Windows.Media.Animation.VectorAnimation); break;
            case KnownElements.VectorAnimationBase: t = typeof(System.Windows.Media.Animation.VectorAnimationBase); break;
            case KnownElements.VectorAnimationUsingKeyFrames: t = typeof(System.Windows.Media.Animation.VectorAnimationUsingKeyFrames); break;
            case KnownElements.VectorCollection: t = typeof(System.Windows.Media.VectorCollection); break;
            case KnownElements.VectorCollectionConverter: t = typeof(System.Windows.Media.VectorCollectionConverter); break;
            case KnownElements.VectorConverter: t = typeof(System.Windows.VectorConverter); break;
            case KnownElements.VectorKeyFrame: t = typeof(System.Windows.Media.Animation.VectorKeyFrame); break;
            case KnownElements.VectorKeyFrameCollection: t = typeof(System.Windows.Media.Animation.VectorKeyFrameCollection); break;
            case KnownElements.VideoDrawing: t = typeof(System.Windows.Media.VideoDrawing); break;
            case KnownElements.ViewBase: t = typeof(System.Windows.Controls.ViewBase); break;
            case KnownElements.Viewbox: t = typeof(System.Windows.Controls.Viewbox); break;
            case KnownElements.Viewport3D: t = typeof(System.Windows.Controls.Viewport3D); break;
            case KnownElements.Viewport3DVisual: t = typeof(System.Windows.Media.Media3D.Viewport3DVisual); break;
            case KnownElements.VirtualizingPanel: t = typeof(System.Windows.Controls.VirtualizingPanel); break;
            case KnownElements.VirtualizingStackPanel: t = typeof(System.Windows.Controls.VirtualizingStackPanel); break;
            case KnownElements.Visual: t = typeof(System.Windows.Media.Visual); break;
            case KnownElements.Visual3D: t = typeof(System.Windows.Media.Media3D.Visual3D); break;
            case KnownElements.VisualBrush: t = typeof(System.Windows.Media.VisualBrush); break;
            case KnownElements.VisualTarget: t = typeof(System.Windows.Media.VisualTarget); break;
            case KnownElements.WeakEventManager: t = typeof(System.Windows.WeakEventManager); break;
            case KnownElements.WhitespaceSignificantCollectionAttribute: t = typeof(System.Windows.Markup.WhitespaceSignificantCollectionAttribute); break;
            case KnownElements.Window: t = typeof(System.Windows.Window); break;
            case KnownElements.WmpBitmapDecoder: t = typeof(System.Windows.Media.Imaging.WmpBitmapDecoder); break;
            case KnownElements.WmpBitmapEncoder: t = typeof(System.Windows.Media.Imaging.WmpBitmapEncoder); break;
            case KnownElements.WrapPanel: t = typeof(System.Windows.Controls.WrapPanel); break;
            case KnownElements.WriteableBitmap: t = typeof(System.Windows.Media.Imaging.WriteableBitmap); break;
            case KnownElements.XamlBrushSerializer: t = typeof(System.Windows.Markup.XamlBrushSerializer); break;
            case KnownElements.XamlInt32CollectionSerializer: t = typeof(System.Windows.Markup.XamlInt32CollectionSerializer); break;
            case KnownElements.XamlPathDataSerializer: t = typeof(System.Windows.Markup.XamlPathDataSerializer); break;
            case KnownElements.XamlPoint3DCollectionSerializer: t = typeof(System.Windows.Markup.XamlPoint3DCollectionSerializer); break;
            case KnownElements.XamlPointCollectionSerializer: t = typeof(System.Windows.Markup.XamlPointCollectionSerializer); break;
            case KnownElements.XamlReader: t = typeof(System.Windows.Markup.XamlReader); break;
            case KnownElements.XamlStyleSerializer: t = typeof(System.Windows.Markup.XamlStyleSerializer); break;
            case KnownElements.XamlTemplateSerializer: t = typeof(System.Windows.Markup.XamlTemplateSerializer); break;
            case KnownElements.XamlVector3DCollectionSerializer: t = typeof(System.Windows.Markup.XamlVector3DCollectionSerializer); break;
            case KnownElements.XamlWriter: t = typeof(System.Windows.Markup.XamlWriter); break;
            case KnownElements.XmlDataProvider: t = typeof(System.Windows.Data.XmlDataProvider); break;
            case KnownElements.XmlLangPropertyAttribute: t = typeof(System.Windows.Markup.XmlLangPropertyAttribute); break;
            case KnownElements.XmlLanguage: t = typeof(System.Windows.Markup.XmlLanguage); break;
            case KnownElements.XmlLanguageConverter: t = typeof(System.Windows.Markup.XmlLanguageConverter); break;
            case KnownElements.XmlNamespaceMapping: t = typeof(System.Windows.Data.XmlNamespaceMapping); break;
            case KnownElements.ZoomPercentageConverter: t = typeof(System.Windows.Documents.ZoomPercentageConverter); break;
            }

            return t;
        }

#endif  // PBTCOMPILER else
    }
#endif  // !BAMLDASM
}

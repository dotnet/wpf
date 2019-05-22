// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Hosts and formats text with advanced document features.
//

using System.Collections;
using System.ComponentModel;
using System.Windows.Automation.Peers;        // AutomationPeer
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Internal;                            // DoubleUtil
using MS.Internal.Documents;
using MS.Internal.PtsHost;
using MS.Internal.PtsHost.UnsafeNativeMethods; // PTS restrictions
using MS.Internal.Telemetry.PresentationFramework;
using MS.Internal.Text;

namespace System.Windows.Documents
{
    /// <summary>
    /// FlowDocument is the paginating container for text content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// FlowDocument does not derive from Visual, and therefore cannot
    /// be added as a direct child of UIElements such as Grid.
    /// In order to be displayed, FlowDocument must be viewed within a FlowDocumentPageViewer,
    /// DocumentViewerBase-derived class, or a custom paginated viewer that utilizes the
    /// IDocumentPaginatorSource API.
    /// It also can be viewed and edited within RichTextBox control,
    /// available as it child in xaml markup or via Document property in api.
    /// </para>
    /// <para>
    /// FlowDocument's content allows elements in some particular hierarchical structure
    /// specified by Flow Content Schema. The Flow Cotent Schema includes a variety
    /// of element classes that you can use to create rich formatted and structured text content.
    /// </para>
    /// <para>
    /// Classes in Flow Content Schema belong to several categories: "Block content",
    /// "Inline Content", "Embedded UIElements".
    /// </para>
    /// <para>
    /// Top-level children of Flow Document must be one of <see cref="Block"/>-derived classes:
    /// <see cref="Paragraph"/>, <see cref="Section"/>, <see cref="List"/>, <see cref="Table"/>.
    /// - Block-level of flow content schema.
    /// </para>
    /// <para>
    /// Each of block elements has specific schema, only <see cref="Paragraph"/> allowing
    /// inline content - elements deived from <see cref="Inline"/> class:
    /// <see cref="Run"/>, <see cref="Span"/>, <see cref="InlineUIContainer"/>, <see cref="Floater"/>, <see cref="Figure"/>.
    /// </para>
    /// <para>
    /// Only <see cref="Run"/> element can contain text directly. All other elements can only contain
    /// elements specified by Flow Schema.
    /// </para>
    /// </remarks>
    [Localizability(LocalizationCategory.Inherit, Readability = Readability.Unreadable)]
    [ContentProperty("Blocks")]
    public class FlowDocument : FrameworkContentElement, IDocumentPaginatorSource, IServiceProvider, IAddChild
    {
        static private readonly Type _typeofThis = typeof(FlowDocument);

        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static constructor. Registers metadata for its properties.
        /// </summary>
        static FlowDocument()
        {
            PropertyChangedCallback typographyChanged = new PropertyChangedCallback(OnTypographyChanged);

            // Registering typography properties metadata
            DependencyProperty[] typographyProperties = Typography.TypographyPropertiesList;
            for (int i = 0; i < typographyProperties.Length; i++)
            {
                typographyProperties[i].OverrideMetadata(_typeofThis, new FrameworkPropertyMetadata(typographyChanged));
            }

            DefaultStyleKeyProperty.OverrideMetadata(_typeofThis, new FrameworkPropertyMetadata(_typeofThis));
            FocusableProperty.OverrideMetadata(_typeofThis, new FrameworkPropertyMetadata(true));

            ControlsTraceLogger.AddControl(TelemetryControls.FlowDocument);
        }

        /// <summary>
        /// FlowDocument constructor.
        /// </summary>
        public FlowDocument()
            : base()
        {
            Initialize(null); // null means to create its own TextContainer
        }

        /// <summary>
        /// Initialized the new instance of a FlowDocument specifying a Block added
        /// as its first child.
        /// </summary>
        /// <param name="block">
        /// Block added as a first initial child of the FlowDocument.
        /// </param>
        public FlowDocument(Block block)
            : base()
        {
            Initialize(null); // null means to create its own TextContainer

            if (block == null)
            {
                throw new ArgumentNullException("block");
            }

            this.Blocks.Add(block);
        }

        /// <summary>
        /// FlowDocument constructor with TextContainer.
        /// </summary>
        internal FlowDocument(TextContainer textContainer)
            : base()
        {
            Initialize(textContainer);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <value>
        /// Collection of Blocks contained in this element
        /// </value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public BlockCollection Blocks
        {
            get
            {
                return new BlockCollection(this, /*isOwnerParent*/true);
            }
        }

        /// <summary>
        /// A TextRange spanning the content of this element.
        /// </summary>
        internal TextRange TextRange
        {
            get
            {
                return new TextRange(this.ContentStart, this.ContentEnd);
            }
        }

        /// <summary>
        /// TextPointer preceding all content.
        /// </summary>
        /// <remarks>
        /// The TextPointer returned always has its IsFrozen property set true
        /// and LogicalDirection property set to LogicalDirection.Backward.
        /// </remarks>
        public TextPointer ContentStart
        {
            get
            {
                return _structuralCache.TextContainer.Start;
            }
        }

        /// <summary>
        /// TextPointer following all content.
        /// </summary>
        /// <remarks>
        /// The TextPointer returned always has its IsFrozen property set true
        /// and LogicalDirection property set to LogicalDirection.Forward.
        /// </remarks>
        public TextPointer ContentEnd
        {
            get
            {
                return _structuralCache.TextContainer.End;
            }
        }

        #region Public Dynamic Properties

        /// <summary>
        /// DependencyProperty for <see cref="FontFamily" /> property.
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty =
                TextElement.FontFamilyProperty.AddOwner(_typeofThis);

        /// <summary>
        /// The FontFamily property specifies the font family.
        /// </summary>
        [Localizability(
            LocalizationCategory.Font,
            Modifiability = Modifiability.Unmodifiable
        )]
        public FontFamily FontFamily
        {
            get { return (FontFamily) GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontStyle" /> property.
        /// </summary>
        public static readonly DependencyProperty FontStyleProperty =
                TextElement.FontStyleProperty.AddOwner(_typeofThis);

        /// <summary>
        /// The FontStyle property requests normal, italic, and oblique faces within a font family.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return (FontStyle) GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontWeight" /> property.
        /// </summary>
        public static readonly DependencyProperty FontWeightProperty =
                TextElement.FontWeightProperty.AddOwner(_typeofThis);

        /// <summary>
        /// The FontWeight property specifies the weight of the font.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return (FontWeight) GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontStretch" /> property.
        /// </summary>
        public static readonly DependencyProperty FontStretchProperty =
                TextElement.FontStretchProperty.AddOwner(_typeofThis);

        /// <summary>
        /// The FontStretch property selects a normal, condensed, or extended face from a font family.
        /// </summary>
        public FontStretch FontStretch
        {
            get { return (FontStretch) GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontSize" /> property.
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty =
                TextElement.FontSizeProperty.AddOwner(
                        _typeofThis);

        /// <summary>
        /// The FontSize property specifies the size of the font.
        /// </summary>
        [TypeConverter(typeof(FontSizeConverter))]
        [Localizability(LocalizationCategory.None)]
        public double FontSize
        {
            get { return (double) GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Foreground" /> property.
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty =
                TextElement.ForegroundProperty.AddOwner(
                        _typeofThis);

        /// <summary>
        /// The Foreground property specifies the foreground brush of an element's text content.
        /// </summary>
        public Brush Foreground
        {
            get { return (Brush) GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty =
                TextElement.BackgroundProperty.AddOwner(
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                null,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The Background property defines the brush used to fill the content area.
        /// </summary>
        public Brush Background
        {
            get { return (Brush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextEffects" /> property.
        /// </summary>
        public static readonly DependencyProperty TextEffectsProperty =
                TextElement.TextEffectsProperty.AddOwner(
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                new FreezableDefaultValueFactory(TextEffectCollection.Empty),
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The TextEffects property specifies effects that are added to the text of an element.
        /// </summary>
        public TextEffectCollection TextEffects
        {
            get { return (TextEffectCollection) GetValue(TextEffectsProperty); }
            set { SetValue(TextEffectsProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextAlignment" /> property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty =
                Block.TextAlignmentProperty.AddOwner(_typeofThis);

        /// <summary>
        /// The TextAlignment property specifies the alignmnet of the element.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="FlowDirection" /> property.
        /// </summary>
        public static readonly DependencyProperty FlowDirectionProperty =
                Block.FlowDirectionProperty.AddOwner(_typeofThis);

        /// <summary>
        /// The FlowDirection property specifies the flow direction of the element.
        /// </summary>
        public FlowDirection FlowDirection
        {
            get { return (FlowDirection)GetValue(FlowDirectionProperty); }
            set { SetValue(FlowDirectionProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="LineHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty LineHeightProperty =
                Block.LineHeightProperty.AddOwner(_typeofThis);

        /// <summary>
        /// The LineHeight property specifies the height of each generated line box.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double LineHeight
        {
            get { return (double)GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="LineStackingStrategy" /> property.
        /// </summary>
        public static readonly DependencyProperty LineStackingStrategyProperty =
                Block.LineStackingStrategyProperty.AddOwner(_typeofThis);

        /// <summary>
        /// The LineStackingStrategy property specifies how lines are placed
        /// </summary>
        public LineStackingStrategy LineStackingStrategy
        {
            get { return (LineStackingStrategy)GetValue(LineStackingStrategyProperty); }
            set { SetValue(LineStackingStrategyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="ColumnWidth" /> property.
        /// </summary>
        public static readonly DependencyProperty ColumnWidthProperty =
                DependencyProperty.Register(
                        "ColumnWidth",
                        typeof(double),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                double.NaN,
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// The minimum width of each column.  If IsColumnWidthIsFlexible is True, then this
        /// value is clamped to be no larger than the width of the page (specified by
        /// PageWidth or PageSize) minus the PagePadding.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        public double ColumnWidth
        {
            get { return (double)GetValue(ColumnWidthProperty); }
            set { SetValue(ColumnWidthProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="ColumnGap" /> property.
        /// </summary>
        public static readonly DependencyProperty ColumnGapProperty =
                DependencyProperty.Register(
                        "ColumnGap",
                        typeof(double),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                double.NaN,
                                FrameworkPropertyMetadataOptions.AffectsMeasure),
                                new ValidateValueCallback(IsValidColumnGap));

        /// <summary>
        /// The distance between each column. This value is clamped to be no larger than
        /// the width of the page (specified by PageWidth or PageSize) minus the PagePadding.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        public double ColumnGap
        {
            get { return (double) GetValue(ColumnGapProperty); }
            set { SetValue(ColumnGapProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="IsColumnWidthFlexible" /> property.
        /// </summary>
        public static readonly DependencyProperty IsColumnWidthFlexibleProperty =
                DependencyProperty.Register(
                        "IsColumnWidthFlexible",
                        typeof(bool),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                true,
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Whether the width of columns is flexible. If this property is true, then columns
        /// will frequently be wider than ColumnWidth. If false, columns will always be exactly
        /// ColumnWidth (as long as the value is smaller than the width of the page minus padding).
        /// </summary>
        public bool IsColumnWidthFlexible
        {
            get { return (bool)GetValue(IsColumnWidthFlexibleProperty); }
            set { SetValue(IsColumnWidthFlexibleProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="ColumnRuleWidth" /> property.
        /// </summary>
        public static readonly DependencyProperty ColumnRuleWidthProperty =
                DependencyProperty.Register(
                        "ColumnRuleWidth",
                        typeof(double),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                0.0,
                                FrameworkPropertyMetadataOptions.AffectsMeasure),
                        new ValidateValueCallback(IsValidColumnRuleWidth));

        /// <summary>
        /// The width of the line drawn in between columns. This value is clamped
        /// to be no larger than the ColumnGap.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        public double ColumnRuleWidth
        {
            get { return (double)GetValue(ColumnRuleWidthProperty); }
            set { SetValue(ColumnRuleWidthProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="ColumnRuleBrush" /> property.
        /// </summary>
        public static readonly DependencyProperty ColumnRuleBrushProperty =
                DependencyProperty.Register(
                        "ColumnRuleBrush",
                        typeof(Brush),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                null,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The brush used to draw the line between columns.
        /// </summary>
        public Brush ColumnRuleBrush
        {
            get { return (Brush)GetValue(ColumnRuleBrushProperty); }
            set { SetValue(ColumnRuleBrushProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="IsOptimalParagraphEnabled" /> property.
        /// </summary>
        public static readonly DependencyProperty IsOptimalParagraphEnabledProperty =
                DependencyProperty.Register(
                        "IsOptimalParagraphEnabled",
                        typeof(bool),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Whether FlowDocument should attempt to construct optimal text paragraphs.
        /// </summary>
        public bool IsOptimalParagraphEnabled
        {
            get { return (bool)GetValue(IsOptimalParagraphEnabledProperty); }
            set { SetValue(IsOptimalParagraphEnabledProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="PageWidth" /> property.
        /// </summary>
        public static readonly DependencyProperty PageWidthProperty =
                DependencyProperty.Register(
                        "PageWidth",
                        typeof(double),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                double.NaN,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnPageMetricsChanged),
                                new CoerceValueCallback(CoercePageWidth)),
                        new ValidateValueCallback(IsValidPageSize));

        /// <summary>
        /// The width of pages returned by GetPage. This value takes precedence over
        /// PageSize.Width, MinPageWidth, and MaxPageWidth.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double PageWidth
        {
            get { return (double) GetValue(PageWidthProperty); }
            set { SetValue(PageWidthProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="MinPageWidth" /> property.
        /// </summary>
        public static readonly DependencyProperty MinPageWidthProperty =
                DependencyProperty.Register(
                        "MinPageWidth",
                        typeof(double),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                0.0,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnMinPageWidthChanged)),
                        new ValidateValueCallback(IsValidMinPageSize));

        /// <summary>
        /// The minimum width of pages returned by GetPage. This value takes
        /// precedence over PageSize.Width, but not PageWidth.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double MinPageWidth
        {
            get { return (double) GetValue(MinPageWidthProperty); }
            set { SetValue(MinPageWidthProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="MaxPageWidth" /> property.
        /// </summary>
        public static readonly DependencyProperty MaxPageWidthProperty =
                DependencyProperty.Register(
                        "MaxPageWidth",
                        typeof(double),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                double.PositiveInfinity,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnMaxPageWidthChanged),
                                new CoerceValueCallback(CoerceMaxPageWidth)),
                        new ValidateValueCallback(IsValidMaxPageSize));

        /// <summary>
        /// The maximum width of pages returned by GetPage. This value takes
        /// precedence over PageSize.Width, but not PageWidth.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double MaxPageWidth
        {
            get { return (double) GetValue(MaxPageWidthProperty); }
            set { SetValue(MaxPageWidthProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="PageHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty PageHeightProperty =
                DependencyProperty.Register(
                        "PageHeight",
                        typeof(double),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                double.NaN,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnPageMetricsChanged),
                                new CoerceValueCallback(CoercePageHeight)),
                        new ValidateValueCallback(IsValidPageSize));

        /// <summary>
        /// The height of pages returned by GetPage. This value takes precedence
        /// over PageSize.Height, MinPageHeight, and MaxPageHeight.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double PageHeight
        {
            get { return (double) GetValue(PageHeightProperty); }
            set { SetValue(PageHeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="MinPageHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty MinPageHeightProperty =
                DependencyProperty.Register(
                        "MinPageHeight",
                        typeof(double),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                0.0,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnMinPageHeightChanged)),
                        new ValidateValueCallback(IsValidMinPageSize));

        /// <summary>
        /// The minimum height of pages returned by GetPage. This value takes
        /// precedence over PageSize.Height, but not PageHeight.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double MinPageHeight
        {
            get { return (double) GetValue(MinPageHeightProperty); }
            set { SetValue(MinPageHeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="MaxPageHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty MaxPageHeightProperty =
                DependencyProperty.Register(
                        "MaxPageHeight",
                        typeof(double),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                double.PositiveInfinity,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnMaxPageHeightChanged),
                                new CoerceValueCallback(CoerceMaxPageHeight)),
                        new ValidateValueCallback(IsValidMaxPageSize));

        /// <summary>
        /// The maximum height of pages returned by GetPage. This value takes
        /// precedence over PageSize.Height, but not PageHeight.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double MaxPageHeight
        {
            get { return (double) GetValue(MaxPageHeightProperty); }
            set { SetValue(MaxPageHeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="PagePadding" /> property.
        /// </summary>
        public static readonly DependencyProperty PagePaddingProperty =
                DependencyProperty.Register(
                        "PagePadding",
                        typeof(Thickness),
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                new Thickness(Double.NaN),
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnPageMetricsChanged)),
                        new ValidateValueCallback(IsValidPagePadding));

        /// <summary>
        /// Padding applied between the page boundaries and the content of the page.
        /// If the sum of the specified padding in a dimension is greater than the
        /// corresponding page dimension, then the padding values in that dimension
        /// will be proportionally reduced such that the sum of the padding in that
        /// dimension is equal to the page dimension.
        /// For example, if the PageSize is (100, 100) and PagePadding is (0, 120, 0, 80),
        /// then the page will be formatted as if the PagePadding were actually (0, 60, 0, 40).
        /// </summary>
        public Thickness PagePadding
        {
            get { return (Thickness) GetValue(PagePaddingProperty); }
            set { SetValue(PagePaddingProperty, value); }
        }

        /// <summary>
        /// Class providing access to all text typography properties
        /// </summary>
        public Typography Typography
        {
            get
            {
                return new Typography(this);
            }
        }

        /// <summary>
        /// DependencyProperty for hyphenation property.
        /// </summary>
        public static readonly DependencyProperty IsHyphenationEnabledProperty =
                Block.IsHyphenationEnabledProperty.AddOwner(_typeofThis);

        /// <summary>
        /// CLR property for hyphenation
        /// </summary>
        public bool IsHyphenationEnabled
        {
            get { return (bool)GetValue(IsHyphenationEnabledProperty); }
            set { SetValue(IsHyphenationEnabledProperty, value); }
        }

        #endregion Public Dynamic Properties

        #endregion Public Properties

        /// <summary>
        /// Sets the DPI for the FlowDocument, causing it to be remeasured and re-rendered. Should be used in Per Monitor DPI scenarios. 
        /// </summary>
        public void SetDpi(DpiScale dpiInfo)
        {
            if (dpiInfo.PixelsPerDip != _pixelsPerDip)
            {
                _pixelsPerDip = dpiInfo.PixelsPerDip;
                if (StructuralCache.HasPtsContext())
                {
                    StructuralCache.TextFormatterHost.PixelsPerDip = _pixelsPerDip;
                }
                // Notify formatter about content invalidation.
                _formatter?.OnContentInvalidated(true);
            }
        }

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Notification that a specified property has been invalidated
        /// </summary>
        /// <param name="e">EventArgs that contains the property, metadata, old value, and new value for this change</param>
        protected sealed override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            // Always call base.OnPropertyChanged, otherwise Property Engine will not work.
            base.OnPropertyChanged(e);

            if (e.IsAValueChange || e.IsASubPropertyChange)
            {
                // Skip caches invalidation if content has not been formatted yet - non of caches are valid,
                // so they will be aquired during first formatting (full format).
                if (_structuralCache != null && _structuralCache.IsFormattedOnce)
                {
                    FrameworkPropertyMetadata fmetadata = e.Metadata as FrameworkPropertyMetadata;
                    if (fmetadata != null)
                    {
                        bool affectsRender = (fmetadata.AffectsRender &&
                            (e.IsAValueChange || !fmetadata.SubPropertiesDoNotAffectRender));
                        if (fmetadata.AffectsMeasure || fmetadata.AffectsArrange || affectsRender || fmetadata.AffectsParentMeasure || fmetadata.AffectsParentArrange)
                        {
                            // Detect invalid content change operations.
                            if (_structuralCache.IsFormattingInProgress)
                            {
                                _structuralCache.OnInvalidOperationDetected();
                                throw new InvalidOperationException(SR.Get(SRID.FlowDocumentInvalidContnetChange));
                            }

                            // None of FlowDocument properties can invalidate structural caches (the NameTable),
                            // but most likely it invalidates format caches. Invalidate all format caches
                            // accumulated in the NameTable.
                            _structuralCache.InvalidateFormatCache(!affectsRender);

                            // Notify formatter about content invalidation.
                            if (_formatter != null)
                            {
                                _formatter.OnContentInvalidated(!affectsRender);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="ContentElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DocumentAutomationPeer(this);
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Protected Properties
        //
        //-------------------------------------------------------------------

        #region Protected Properties

        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                return new RangeContentEnumerator(_structuralCache.TextContainer.Start, _structuralCache.TextContainer.End);
            }
        }

        /// <summary>
        ///     Fetches the value of the IsEnabled property
        /// </summary>
        /// <remarks>
        ///     We want to coerce ContentElements and UIElement children of FlowDocument to be disabled in _editable_ content (ie, RichTextBox).
        ///     In read-only mode, in say the FlowDocumentReader, we don't want any coercing.
        /// </remarks>
        protected override bool IsEnabledCore
        {
            get
            {
                if (!base.IsEnabledCore)
                {
                    return false;
                }

                RichTextBox parentRichTextBox = this.Parent as RichTextBox;

                return (parentRichTextBox == null) ? true : parentRichTextBox.IsDocumentEnabled;
            }
        }

        #endregion Protected Properties

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns a ContentPosition for an object.
        /// </summary>
        /// <param name="element">Object within this element's tree.</param>
        /// <returns>Returns a ContentPosition for an object.</returns>
        internal ContentPosition GetObjectPosition(Object element)
        {
            TextPointer flowContentPosition;
            ITextPointer textPointer = null;
            DependencyObject parentOfEmbeddedElement;

            // If element is 'this', return ContentPosition representing ContentStart.
            if (element == this)
            {
                textPointer = this.ContentStart;
            }
            // If element is a TextElement, return its ContentStart.
            else if (element is TextElement)
            {
                textPointer = ((TextElement)element).ContentStart;
            }
            // Otherwise we are dealing with embedded element. Find its position in the
            // TextContainer and return it.
            // It is possible that somebody asks for FrameworkElement that is not an immediate
            // child of FlowDocument's content. In such case walk up the parent chain and
            // get FrameworkElement that is directly embedded within FlowDocument's content.
            else if (element is FrameworkElement)
            {
                parentOfEmbeddedElement = null;
                while (element is FrameworkElement)
                {
                    parentOfEmbeddedElement = LogicalTreeHelper.GetParent((DependencyObject)element);
                    if (parentOfEmbeddedElement == null)
                    {
                        parentOfEmbeddedElement = VisualTreeHelper.GetParent((Visual)element);
                    }
                    if (!(parentOfEmbeddedElement is FrameworkElement))
                    {
                        break;
                    }
                    element = parentOfEmbeddedElement;
                }
                if (parentOfEmbeddedElement is BlockUIContainer || parentOfEmbeddedElement is InlineUIContainer)
                {
                    textPointer = TextContainerHelper.GetTextPointerForEmbeddedObject((FrameworkElement)element);
                }
            }
            // Check if the TextPointer belongs to our tree.
            if (textPointer != null && textPointer.TextContainer != _structuralCache.TextContainer)
            {
                textPointer = null;
            }
            flowContentPosition = textPointer as TextPointer;
            return (flowContentPosition != null) ? flowContentPosition : ContentPosition.Missing;
        }

        /// <summary>
        /// OnChildDesiredSizeChanged
        /// Called from FlowDocumentPage for IContentHost implementation
        /// This should be private but cannot be access from its
        /// FlowDocumentPage because of protection level. Find a workaround
        /// </summary>
        /// <param name="child"></param>
        internal void OnChildDesiredSizeChanged(UIElement child)
        {
            if (_structuralCache != null && _structuralCache.IsFormattedOnce && !_structuralCache.ForceReformat)
            {
                // If executed during formatting process, delay invalidation.
                // This may happen during formatting when text host notifies its about
                // baseline changes.
                if (_structuralCache.IsFormattingInProgress)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new DispatcherOperationCallback(OnChildDesiredSizeChangedAsync), child);
                    return;
                }

                // Get start and end positions
                int childStartIndex = TextContainerHelper.GetCPFromEmbeddedObject(child, ElementEdge.BeforeStart);
                if (childStartIndex < 0)
                {
                    return;
                }

                TextPointer childStart = new TextPointer(_structuralCache.TextContainer.Start);
                childStart.MoveByOffset(childStartIndex);
                TextPointer childEnd = new TextPointer(childStart);
                childEnd.MoveByOffset(TextContainerHelper.EmbeddedObjectLength);

                // Create new DTR for changing UIElement and add it to DRTList.
                DirtyTextRange dtr = new DirtyTextRange(childStartIndex, TextContainerHelper.EmbeddedObjectLength, TextContainerHelper.EmbeddedObjectLength);
                _structuralCache.AddDirtyTextRange(dtr);

                // Notify formatter about content invalidation.
                if (_formatter != null)
                {
                    _formatter.OnContentInvalidated(true, childStart, childEnd);
                }
            }
        }

        /// <summary>
        /// Do delayed initialization before first formatting.
        /// </summary>
        internal void InitializeForFirstFormatting()
        {
            _structuralCache.TextContainer.Changing += new EventHandler(OnTextContainerChanging);
            _structuralCache.TextContainer.Change += new TextContainerChangeEventHandler(OnTextContainerChange);
            _structuralCache.TextContainer.Highlights.Changed += new HighlightChangedEventHandler(OnHighlightChanged);
        }

        /// <summary>
        /// Clear the TextContainer and unregister events.  Called by TextBox on style change.
        /// </summary>
        internal void Uninitialize()
        {
            _structuralCache.TextContainer.Changing -= new EventHandler(OnTextContainerChanging);
            _structuralCache.TextContainer.Change -= new TextContainerChangeEventHandler(OnTextContainerChange);
            _structuralCache.TextContainer.Highlights.Changed -= new HighlightChangedEventHandler(OnHighlightChanged);
            _structuralCache.IsFormattedOnce = false;
        }

        /// <summary>
        /// Compute margin for a page.
        /// </summary>
        internal Thickness ComputePageMargin()
        {
            double lineHeight = DynamicPropertyReader.GetLineHeightValue(this);
            Thickness pageMargin = this.PagePadding;

            // If Padding value is 'Auto', treat it as 1*LineHeight.
            if (DoubleUtil.IsNaN(pageMargin.Left))
            {
                pageMargin.Left = lineHeight;
            }
            if (DoubleUtil.IsNaN(pageMargin.Top))
            {
                pageMargin.Top = lineHeight;
            }
            if (DoubleUtil.IsNaN(pageMargin.Right))
            {
                pageMargin.Right = lineHeight;
            }
            if (DoubleUtil.IsNaN(pageMargin.Bottom))
            {
                pageMargin.Bottom = lineHeight;
            }
            return pageMargin;
        }

        /// <summary>
        /// Called before the parent is changed to the new value.
        /// We listen to parent change notifications to enforce coercing FlowDocument's IsEnabled property to false
        /// (when it is parented by RichTextBox).
        /// This property coersion is done in 2 parts.
        ///     1. Implement IsEnabledCore for property coersion
        ///     2. Listen to changes to parent
        /// (2) needs to be done, since property system does not coerce default values.
        /// Listening to parent changes guarantees that every time FlowDocument is removed or connected to a RichTextBox parent,
        /// we explicitly coerce IsEnabled for the new tree.
        /// </summary>
        /// <param name="newParent"></param>
        internal override void OnNewParent(DependencyObject newParent)
        {
            DependencyObject oldParent = this.Parent;
            base.OnNewParent(newParent);

            if (newParent is RichTextBox || oldParent is RichTextBox)
            {
                CoerceValue(IsEnabledProperty);
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// An object which formats botomless content.
        /// </summary>
        internal FlowDocumentFormatter BottomlessFormatter
        {
            get
            {
                if (_formatter != null && !(_formatter is FlowDocumentFormatter))
                {
                    _formatter.Suspend();
                    _formatter = null;
                }
                if (_formatter == null)
                {
                    _formatter = new FlowDocumentFormatter(this);
                }
                return (FlowDocumentFormatter)_formatter;
            }
        }

        /// <summary>
        /// StructuralCache.
        /// </summary>
        internal StructuralCache StructuralCache
        {
            get
            {
                return _structuralCache;
            }
        }

        /// <summary>
        /// Typography properties group.
        /// </summary>
        internal TypographyProperties TypographyPropertiesGroup
        {
            get
            {
                if (_typographyPropertiesGroup == null)
                {
                    _typographyPropertiesGroup = TextElement.GetTypographyProperties(this);
                }
                return _typographyPropertiesGroup;
            }
        }

        /// <summary>
        /// TextWrapping property value (set by TextBox/RichTextBox)
        /// </summary>
        internal TextWrapping TextWrapping
        {
            get
            {
                return _textWrapping;
            }
            set
            {
                _textWrapping = value;
            }
        }

        /// <summary>
        /// Formatter value
        /// </summary>
        internal IFlowDocumentFormatter Formatter
        {
            get
            {
                return _formatter;
            }
        }

        //-------------------------------------------------------------------
        // Is layout data is in a valid state.
        //-------------------------------------------------------------------
        internal bool IsLayoutDataValid
        {
            get
            {
                if(_formatter != null)
                {
                    return _formatter.IsLayoutDataValid;
                }

                return false;
            }
        }

        //-------------------------------------------------------------------
        // TextContainer associated with this FlowDocument.
        //-------------------------------------------------------------------
        internal TextContainer TextContainer
        {
            get
            {
                return _structuralCache.TextContainer;
            }
        }

        //-------------------------------------------------------------------
        // DPI at which the FlowDocument needs to be rendered.
        //-------------------------------------------------------------------
        internal double PixelsPerDip
        {
            get { return _pixelsPerDip; }
            set { _pixelsPerDip = value; }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Internal Events
        //
        //-------------------------------------------------------------------

        #region Internal Events

        /// <summary>
        /// Fired when a PageSize property is changed
        /// </summary>
        internal event EventHandler PageSizeChanged;

        #endregion Internal Events

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// One of the properties which comprises TypographyProperties has changed -- reset cache.
        /// </summary>
        private static void OnTypographyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FlowDocument)d)._typographyPropertiesGroup = null;
        }

        /// <summary>
        /// OnChildDesiredSizeChanged delayed to avoid changes during page
        /// formatting.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private object OnChildDesiredSizeChangedAsync(object arg)
        {
            OnChildDesiredSizeChanged(arg as UIElement);
            return null;
        }

        /// <summary>
        /// Initialize FlowDocument.
        /// </summary>
        /// <param name="textContainer"></param>
        private void Initialize(TextContainer textContainer)
        {
            if (textContainer == null)
            {
                // Create text tree that contains content of the element.
                textContainer = new TextContainer(this, false /* plainTextOnly */);
            }

            // Create structural cache object
            _structuralCache = new StructuralCache(this, textContainer);

            // Get rid of the current formatter.
            if (_formatter != null)
            {
                _formatter.Suspend();
                _formatter = null;
            }
        }

        /// <summary>
        /// Respond to page metrics changes.
        /// </summary>
        private static void OnPageMetricsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FlowDocument fd = (FlowDocument)d;
            if (fd._structuralCache != null && fd._structuralCache.IsFormattedOnce)
            {
                // Notify formatter about content invalidation.
                if (fd._formatter != null)
                {
                    // Any change of page metrics invalidates the layout.
                    // Hence page metrics change is treated in the same way as ContentChanged
                    // spanning entire content.
                    fd._formatter.OnContentInvalidated(true);
                }

                // Fire notification about the PageSize change - needed in RichTextBox
                if (fd.PageSizeChanged != null)
                {
                    // NOTE: May execute external code, so it is possible to get
                    //       an exception here.
                    fd.PageSizeChanged(fd, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Respond to MinPageWidth change.
        /// </summary>
        private static void OnMinPageWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(MaxPageWidthProperty);
            d.CoerceValue(PageWidthProperty);
        }

        /// <summary>
        /// Respond to MinPageHeight change.
        /// </summary>
        private static void OnMinPageHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(MaxPageHeightProperty);
            d.CoerceValue(PageHeightProperty);
        }

        /// <summary>
        /// Respond to MaxPageWidth change.
        /// </summary>
        private static void OnMaxPageWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(PageWidthProperty);
        }

        /// <summary>
        /// Respond to MaxPageHeight change.
        /// </summary>
        private static void OnMaxPageHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(PageHeightProperty);
        }

        /// <summary>
        /// Coerce MaxPageWidth value.
        /// </summary>
        private static object CoerceMaxPageWidth(DependencyObject d, object value)
        {
            FlowDocument fd = (FlowDocument) d;
            double max = (double) value;
            double min = fd.MinPageWidth;
            if (max < min)
            {
                return min;
            }
            return value;
        }

        /// <summary>
        /// Coerce MaxPageHeight value.
        /// </summary>
        private static object CoerceMaxPageHeight(DependencyObject d, object value)
        {
            FlowDocument fd = (FlowDocument) d;
            double max = (double) value;
            double min = fd.MinPageHeight;
            if (max < min)
            {
                return min;
            }
            return value;
        }

        /// <summary>
        /// Coerce PageWidth value.
        /// </summary>
        private static object CoercePageWidth(DependencyObject d, object value)
        {
            FlowDocument fd = (FlowDocument) d;
            double width = (double) value;

            if (!DoubleUtil.IsNaN(width))
            {
                double max = fd.MaxPageWidth;
                if (width > max)
                {
                    width = max;
                }

                double min = fd.MinPageWidth;
                if (width < min)
                {
                    width = min;
                }
            }

            return value;
        }

        /// <summary>
        /// Coerce PageHeight value.
        /// </summary>
        private static object CoercePageHeight(DependencyObject d, object value)
        {
            FlowDocument fd = (FlowDocument) d;
            double height = (double) value;

            if (!DoubleUtil.IsNaN(height))
            {
                double max = fd.MaxPageHeight;
                if (height > max)
                {
                    height = max;
                }

                double min = fd.MinPageHeight;
                if (height < min)
                {
                    height = min;
                }
            }

            return value;
        }

        /// <summary>
        /// Invalidates a portion of text affected by a highlight change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnHighlightChanged(object sender, HighlightChangedEventArgs args)
        {
            TextSegment textSegment;
            int i;

            Invariant.Assert(args != null);
            Invariant.Assert(args.Ranges != null);
            Invariant.Assert(_structuralCache != null && _structuralCache.IsFormattedOnce, "Unexpected Highlights.Changed callback before first format!");

            // Detect invalid content change operations.
            if (_structuralCache.IsFormattingInProgress)
            {
                _structuralCache.OnInvalidOperationDetected();
                throw new InvalidOperationException(SR.Get(SRID.FlowDocumentInvalidContnetChange));
            }

            // The only supported highlight type for FlowDocument is SpellerHightlight.
            // TextSelection and HighlightComponent are ignored, because they are handled by
            // separate layer.
            if (args.OwnerType != typeof(SpellerHighlightLayer))
            {
                return;
            }

            if (args.Ranges.Count > 0)
            {
                // Invalidate affected pages and break records.
                // We DTR invalidate if we're using a formatter as well for incremental update.
                if (_formatter == null || !(_formatter is FlowDocumentFormatter))
                {
                    _structuralCache.InvalidateFormatCache(/*Clear structure*/ false);
                }

                // Notify formatter about content invalidation.
                if (_formatter != null)
                {
                    for (i = 0; i < args.Ranges.Count; i++)
                    {
                        textSegment = (TextSegment)args.Ranges[i];
                        _formatter.OnContentInvalidated(false, textSegment.Start, textSegment.End);

                        if (_formatter is FlowDocumentFormatter)
                        {
                            DirtyTextRange dtr = new DirtyTextRange(textSegment.Start.Offset,
                                                                    textSegment.Start.GetOffsetToPosition(textSegment.End),
                                                                    textSegment.Start.GetOffsetToPosition(textSegment.End)
                                                                    );
                            _structuralCache.AddDirtyTextRange(dtr);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handler for TextContainer changing notifications.
        /// </summary>
        private void OnTextContainerChanging(object sender, EventArgs args)
        {
            Invariant.Assert(sender == _structuralCache.TextContainer, "Received text change for foreign TextContainer.");
            Invariant.Assert(_structuralCache != null && _structuralCache.IsFormattedOnce, "Unexpected TextContainer.Changing callback before first format!");

            // Detect invalid content change operations.
            if (_structuralCache.IsFormattingInProgress)
            {
                _structuralCache.OnInvalidOperationDetected();
                throw new InvalidOperationException(SR.Get(SRID.FlowDocumentInvalidContnetChange));
            }

            // Remember the fact that content is changing.
            // OnTextContainerChange has to be received after this event.
            _structuralCache.IsContentChangeInProgress = true;
        }

        /// <summary>
        /// Handler for TextContainer change notifications.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnTextContainerChange(object sender, TextContainerChangeEventArgs args)
        {
            DirtyTextRange dtr;
            ITextPointer segmentEnd;

            Invariant.Assert(args != null);
            Invariant.Assert(sender == _structuralCache.TextContainer);
            Invariant.Assert(_structuralCache != null && _structuralCache.IsFormattedOnce, "Unexpected TextContainer.Change callback before first format!");

            if (args.Count == 0)
            {
                // A no-op for this control.  Happens when IMECharCount updates happen
                // without corresponding SymbolCount changes.
                return;
            }

            try
            {
                // Detect invalid content change operations.
                if (_structuralCache.IsFormattingInProgress)
                {
                    _structuralCache.OnInvalidOperationDetected();
                    throw new InvalidOperationException(SR.Get(SRID.FlowDocumentInvalidContnetChange));
                }

                // Since content is changeing, do partial invalidation of BreakRecordTable.
                if (args.TextChange != TextChangeType.ContentRemoved)
                {
                    segmentEnd = args.ITextPosition.CreatePointer(args.Count, LogicalDirection.Forward);
                }
                else
                {
                    segmentEnd = args.ITextPosition;
                }

                // Invalidate affected pages and break records.
                // We DTR invalidate if we're using a formatter as well for incremental update.
                if (!args.AffectsRenderOnly || (_formatter != null && _formatter is FlowDocumentFormatter))
                {
                    // Create new DTR for changing range and add it to DRTList.
                    dtr = new DirtyTextRange(args);
                    _structuralCache.AddDirtyTextRange(dtr);
                }
                else
                {
                    // Clear format caches.
                    _structuralCache.InvalidateFormatCache(/*Clear structure*/ false);
                }

                // Notify formatter about content invalidation.
                if (_formatter != null)
                {
                    _formatter.OnContentInvalidated(!args.AffectsRenderOnly, args.ITextPosition, segmentEnd);
                }
            }
            finally
            {
                // Content has been changed, so reset appropriate flag.
                _structuralCache.IsContentChangeInProgress = false;
            }
        }


        private static bool IsValidPageSize(object o)
        {
            double value = (double)o;
            double maxSize = Math.Min(1000000, PTS.MaxPageSize);
            if (Double.IsNaN(value))
            {
                return true;
            }
            if (value < 0 || value > maxSize)
            {
                return false;
            }
            return true;
        }

        private static bool IsValidMinPageSize(object o)
        {
            double value = (double)o;
            double maxSize = Math.Min(1000000, PTS.MaxPageSize);
            if (Double.IsNaN(value))
            {
                return false;
            }
            if (!Double.IsNegativeInfinity(value) && (value < 0 || value > maxSize))
            {
                return false;
            }
            return true;
        }

        private static bool IsValidMaxPageSize(object o)
        {
            double value = (double)o;
            double maxSize = Math.Min(1000000, PTS.MaxPageSize);
            if (Double.IsNaN(value))
            {
                return false;
            }
            if (!Double.IsPositiveInfinity(value) && (value < 0 || value > maxSize))
            {
                return false;
            }
            return true;
        }

        private static bool IsValidPagePadding(object o)
        {
            Thickness value = (Thickness)o;
            return Block.IsValidThickness(value, /*allow NaN*/true);
        }

        private static bool IsValidColumnRuleWidth(object o)
        {
            double ruleWidth = (double)o;
            double maxRuleWidth = Math.Min(1000000, PTS.MaxPageSize);
            if (Double.IsNaN(ruleWidth) || ruleWidth < 0 || ruleWidth > maxRuleWidth)
            {
                return false;
            }
            return true;
        }

        private static bool IsValidColumnGap(object o)
        {
            double gap = (double)o;
            double maxGap = Math.Min(1000000, PTS.MaxPageSize);
            if (Double.IsNaN(gap))
            {
                // Default value.
                return true;
            }
            if (gap < 0 || gap > maxGap)
            {
                return false;
            }
            return true;
        }

        #endregion Private methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private StructuralCache _structuralCache;                   // Structural cache for the content.
        private TypographyProperties _typographyPropertiesGroup;    // Cache for typography properties.
        private IFlowDocumentFormatter _formatter;                  // Current formatter asociated with FlowDocument.
        private TextWrapping _textWrapping = TextWrapping.Wrap;     // internal cache for TextBox/RichTextBox
        private double _pixelsPerDip = MS.Internal.FontCache.Util.PixelsPerDip;

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  IAddChild Members
        //
        //-------------------------------------------------------------------

        #region IAddChild Members

        ///<summary>
        /// Called to Add the object as a Child.
        ///</summary>
        ///<param name="value">
        /// Object to add as a child
        ///</param>
        void IAddChild.AddChild(Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (!TextSchema.IsValidChildOfContainer(/*parentType:*/_typeofThis, /*childType:*/value.GetType()))
            {
                throw new ArgumentException(SR.Get(SRID.TextSchema_ChildTypeIsInvalid, _typeofThis.Name, value.GetType().Name));
            }

            // Checking that the element inserted does not have a parent
            if (value is TextElement && ((TextElement)value).Parent != null)
            {
                throw new ArgumentException(SR.Get(SRID.TextSchema_TheChildElementBelongsToAnotherTreeAlready, value.GetType().Name));
            }

            if (value is Block)
            {
                TextContainer textContainer = _structuralCache.TextContainer;
                ((Block)value).RepositionWithContent(textContainer.End);
            }
            else
            {
                Invariant.Assert(false); // We do not expect anything except Blocks on top level of a FlowDocument
            }
        }

        ///<summary>
        /// Called when text appears under the tag in markup
        ///</summary>
        ///<param name="text">
        /// Text to Add to the Object
        ///</param>
        void IAddChild.AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        #endregion IAddChild Members

        //-------------------------------------------------------------------
        //
        //  IServiceProvider Members
        //
        //-------------------------------------------------------------------

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <remarks>
        /// FlowDocument supports only TextContainer.
        /// </remarks>
        /// <param name="serviceType">
        /// An object that specifies the type of service object to get.
        /// </param>
        /// <returns>
        /// A service object of type serviceType. A null reference if there is no
        /// service object of type serviceType.
        /// </returns>
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }
            if (serviceType == typeof(ITextContainer))
            {
                return _structuralCache.TextContainer;
            }
            else if (serviceType == typeof(TextContainer))
            {
                return _structuralCache.TextContainer as TextContainer;
            }
            return null;
        }

        #endregion IServiceProvider Members

        //-------------------------------------------------------------------
        //
        //  IDocumentPaginatorSource Members
        //
        //-------------------------------------------------------------------

        #region IDocumentPaginatorSource Members

        /// <summary>
        /// An object which paginates content.
        /// </summary>
        DocumentPaginator IDocumentPaginatorSource.DocumentPaginator
        {
            get
            {
                if (_formatter != null && !(_formatter is FlowDocumentPaginator))
                {
                    _formatter.Suspend();
                    _formatter = null;
                }
                if (_formatter == null)
                {
                    _formatter = new FlowDocumentPaginator(this);
                }
                return (FlowDocumentPaginator)_formatter;
            }
        }

        #endregion IDocumentPaginatorSource Members
    }
}


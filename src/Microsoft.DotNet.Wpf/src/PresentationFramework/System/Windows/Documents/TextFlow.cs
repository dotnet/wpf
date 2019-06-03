// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
// File: TextFlow.cs
//
// Description: TextFlow element displays text with advanced document features. 
//
//---------------------------------------------------------------------------

using System.Threading;

using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // TextBoxBase
using MS.Utility;
using MS.Internal;                  // Invariant.Assert
using MS.Internal.PtsHost;
using MS.Internal.Text;
using MS.Internal.Automation;       // TextAdaptor
using MS.Internal.Documents;
using System.ComponentModel;

using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Automation;
using System.Windows.Markup;
using System;
using System.Diagnostics;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace System.Windows.Documents
{
    /// <summary>
    /// TextFlow element displays text with advanced document features.
    /// </summary>
    [Localizability(LocalizationCategory.TextFlow)]
    [ContentProperty("Blocks")]
    internal class TextFlow : FrameworkElement, IContentHost, IAddChild, IServiceProvider
    {
        //-------------------------------------------------------------------
        //
        //  IContentHost Members
        //
        //-------------------------------------------------------------------

        #region IContentHost Members
        
        /// <summary>
        /// Hit tests to the correct ContentElement 
        /// within the ContentHost that the mouse 
        /// is over
        /// </summary>
        /// <param name="point">
        /// Mouse coordinates relative to 
        /// the ContentHost
        /// </param>
        IInputElement IContentHost.InputHitTest(Point point)
        {
            return this.InputHitTestCore(point);
        }
        
        /// <summary>
        /// Returns rectangles for element. First finds element by navigating in FlowDocumentPage. 
        /// If element is not found or if call to get rectangles from FlowDocumentPage returns null
        /// we return an empty collection. If the layout is not valid we return null.
        /// </summary>
        /// <param name="child">
        /// Content element for which rectangles are required
        /// </param>
        ReadOnlyCollection<Rect> IContentHost.GetRectangles(ContentElement child)
        {
            return this.GetRectanglesCore(child);
        }

        /// <summary>
        /// Returns elements hosted by the content host as an enumerator class
        /// </summary>
        IEnumerator<IInputElement> IContentHost.HostedElements
        {
            get
            {
                return this.HostedElementsCore;
            }
        }

        /// <summary>
        /// Called when a UIElement-derived class which is hosted by a IContentHost changes its DesiredSize
        /// NOTE: This method already exists for this class and is not specially implemented for IContentHost.
        /// If this method is called through IContentHost for this class it will fire an assert
        /// </summary>
        /// <param name="child">
        /// Child element whose DesiredSize has changed
        /// </param> 
        void IContentHost.OnChildDesiredSizeChanged(UIElement child)
        {
            this.OnChildDesiredSizeChangedCore(child);
        }

        #endregion IContentHost Members        

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
            ValidationHelper.ValidateChild(this, value);

            // We do not expect anything except Blocks on top level of a TextFlow. ValidationHelper is supposed to guarantee that.
            Invariant.Assert(value is Block);

            TextContainer textContainer = _structuralCache.TextContainer;
            ((Block)value).RepositionWithContent(textContainer.End);
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
        //  LogicalTree
        //
        //-------------------------------------------------------------------

        #region LogicalTree

        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                return _foreignTextContainer ? null : new RangeContentEnumerator(_structuralCache.TextContainer.Start, _structuralCache.TextContainer.End);
            }
        }

        #endregion LogicalTree


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
        /// TextFlow currently supports only TextView and TextContainer.
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
            else if (serviceType == typeof(ITextView))
            {
                return this.TextView;
            }
            return null;
        }

        #endregion IServiceProvider Members

        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static constructor. Registers metadata for its properties.
        /// </summary>
        static TextFlow()
        {
            Style ds = new Style(typeof(TextFlow));
            ds.Setters.Add(new Setter(TextAlignmentProperty, TextAlignment.Left));
            ds.Setters.Add(new Setter(FontFamilyProperty, new FontFamily("Georgia")));
            ds.Setters.Add(new Setter(FontSizeProperty, 16.0));
            ds.Setters.Add(new Setter(TextWrappingProperty, TextWrapping.Wrap));
            ds.Setters.Add(new Setter(TextTrimmingProperty, TextTrimming.None));
            ds.Seal();
            StyleProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(ds));

            // Registering typography properties metadata
            PropertyChangedCallback typographyChanged = new PropertyChangedCallback(OnTypographyChanged);

            Typography.StandardLigaturesProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.ContextualLigaturesProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.DiscretionaryLigaturesProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.HistoricalLigaturesProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.AnnotationAlternatesProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.ContextualAlternatesProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.HistoricalFormsProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.KerningProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.CapitalSpacingProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.CaseSensitiveFormsProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet1Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet2Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet3Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet4Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet5Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet6Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet7Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet8Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet9Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet10Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet11Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet12Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet13Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet14Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet15Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet16Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet17Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet18Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet19Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticSet20Property.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.FractionProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.SlashedZeroProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.MathematicalGreekProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.EastAsianExpertFormsProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.VariantsProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.CapitalsProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.NumeralStyleProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.NumeralAlignmentProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.EastAsianWidthsProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.EastAsianLanguageProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StandardSwashesProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.ContextualSwashesProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));
            Typography.StylisticAlternatesProperty.OverrideMetadata(typeof(TextFlow), new FrameworkPropertyMetadata(typographyChanged));

            EventManager.RegisterClassHandler(typeof(TextFlow), RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(OnRequestBringIntoView));
        }

        /// <summary>
        /// TextFlow constructor.
        /// </summary>
        public TextFlow() 
            : base()
        {
            Initialize();
        }

        /// <summary>
        /// TextFlow constructor.
        /// </summary>
        /// <param name="block">
        /// Block initially added to TextFlow's Blocks collection.
        /// </param>
        public TextFlow(Block block)
            : base()
        {
            if (block == null)
            {
                throw new ArgumentNullException("block");
            }

            Initialize();

            this.Blocks.Add(block);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Returns the TextPointer located closest to a supplied Point.
        /// </summary>
        /// <param name="point">
        /// Point to query, in the coordinate space of the TextFlow.
        /// </param>
        /// <param name="snapToText">
        /// If true, this method will always return a TextPointer --
        /// the closest position as calculated by the control's heuristics. 
        /// If false, this method will return a null position if the test 
        /// point does not fall within any character bounding box.
        /// </param>
        /// <returns>
        /// The closest TextPointer to the supplied Point, or null if
        /// snapToText is false and the supplied Point is not contained
        /// within any character bounding box.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if layout is dirty.
        /// </exception>
        public TextPointer GetPositionFromPoint(Point point, bool snapToText)
        {
            TextPointer position;

            // Change in progress is handled by TextDocumentView

            // Validate layout information on TextView.
            if (this.TextView.Validate(point))
            {
                position = (TextPointer)this.TextView.GetTextPositionFromPoint(point, snapToText);
            }
            else
            {
                position = snapToText ? new TextPointer(this.ContentStart) : null;
            }

            return position;
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <value>
        /// Collection of Blocks contained in this TextFlow.
        /// </value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public BlockCollection Blocks
        {
            get
            {
                return new BlockCollection(this);
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

        #region Public Dynamic Properties

        /// <summary>
        /// DependencyProperty for <see cref="FontFamily" /> property.
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty = 
                TextElement.FontFamilyProperty.AddOwner(typeof(TextFlow));

        /// <summary>
        /// The FontFamily property specifies the name of font family.
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
        /// DependencyProperty for <see cref="FontSize" /> property.
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty = 
                TextElement.FontSizeProperty.AddOwner(
                        typeof(TextFlow));

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
        /// DependencyProperty for <see cref="FontStyle" /> property.
        /// </summary>
        public static readonly DependencyProperty FontStyleProperty = 
                TextElement.FontStyleProperty.AddOwner(typeof(TextFlow));

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
                TextElement.FontWeightProperty.AddOwner(typeof(TextFlow));

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
                TextElement.FontStretchProperty.AddOwner(typeof(TextFlow));

        /// <summary>
        /// The FontStretch property selects a normal, condensed, or extended face from a font family.
        /// </summary>
        public FontStretch FontStretch
        {
            get { return (FontStretch) GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Foreground" /> property.
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty = 
                TextElement.ForegroundProperty.AddOwner(
                        typeof(TextFlow));

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
                        typeof(TextFlow),
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
        /// DependencyProperty for <see cref="LineHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty LineHeightProperty = 
                Block.LineHeightProperty.AddOwner(typeof(TextFlow));

        /// <summary>
        /// The LineHeight property specifies the height of each generated line box.
        /// </summary>
        public double LineHeight
        {
            get { return (double) GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Padding" /> property.
        /// </summary>
        public static readonly DependencyProperty PaddingProperty =
                Block.PaddingProperty.AddOwner(
                        typeof(TextFlow),
                        new FrameworkPropertyMetadata(
                                new Thickness(),
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// The Padding property specifies the padding of the element.
        /// </summary>
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextAlignment" /> property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty = 
                Block.TextAlignmentProperty.AddOwner(typeof(TextFlow));

        /// <summary>
        /// The TextAlignment property specifies horizontal alignment of the content.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment) GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextTrimming" /> property.
        /// </summary>
        public static readonly DependencyProperty TextTrimmingProperty =
                TextBlock.TextTrimmingProperty.AddOwner(
                        typeof(TextFlow),
                        new FrameworkPropertyMetadata(
                                TextTrimming.None,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The TextTrimming property specifies the trimming behavior situation 
        /// in case of clipping some textual content caused by overflowing the line's box.
        /// </summary>
        public TextTrimming TextTrimming
        {
            get { return (TextTrimming) GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextWrapping" /> property.
        /// </summary>
        public static readonly DependencyProperty TextWrappingProperty =
                TextBlock.TextWrappingProperty.AddOwner(
                        typeof(TextFlow),
                        new FrameworkPropertyMetadata(
                                TextWrapping.Wrap,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The TextWrapping property controls whether or not text wraps 
        /// when it reaches the flow edge of its containing block box.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get { return (TextWrapping) GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextEffects" /> property.
        /// </summary>
        public static readonly DependencyProperty TextEffectsProperty = 
                TextElement.TextEffectsProperty.AddOwner(
                        typeof(TextFlow),
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
                Block.IsHyphenationEnabledProperty.AddOwner(typeof(TextFlow));

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

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Content measurement.
        /// </summary>
        /// <param name="constraint">Constraint size.</param>
        /// <returns>Computed desired size.</returns>
        protected sealed override Size MeasureOverride(Size constraint)
        {

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.BeginScope("TextFlow.MeasureOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("TextFlow.MeasureOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif

            VerifyReentrancy();

            // Do first measure initialization
            if (!CheckFlags(Flags.FormattedOnce))
            {
                InitializeForFirstMeasure();
            }
            else
            {
                if(!CheckFlags(Flags.LastMeasureSuccessful))
                {
                    // We cannot resolve update info if last measure was unsuccessful.
                    _structuralCache.InvalidateFormatCache(true);
                }
            }

            AdjustForInfinity(ref constraint);

            if (CheckFlags(Flags.FormattedOnce) && !CheckFlags(Flags.ArrangedAfterMeasure) && 
                (!_structuralCache.ForceReformat || !_structuralCache.DestroyStructure))
            {
                // Need to clear update info by running arrange process.
                // This is necessary, because Measure may be called more than once
                // before Arrange is called. But PTS is not able to merge update info.
                // To protect against loosing incremental changes delta, need
                // to arrange the page and create all necessary visuals.
                ArrangeOverride(_documentPage.ContentSize);
            }

            SetFlags(true, Flags.FormattedOnce);
            SetFlags(false, Flags.ArrangedAfterMeasure);
            SetFlags(false, Flags.LastMeasureSuccessful);

            try
            {
                SetFlags(true, Flags.MeasureInProgress);
                _documentPage.FormatBottomless(constraint, Padding);
            }
            finally
            {
                SetFlags(false, Flags.MeasureInProgress);
            }

            SetFlags(true, Flags.LastMeasureSuccessful);

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("TextFlow.MeasureOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.EndScope(MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif
            // Add padding to content size
            return new Size(_documentPage.ContentSize.Width + Padding.Left + Padding.Right, 
                            _documentPage.ContentSize.Height + Padding.Top + Padding.Bottom);
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="arrangeSize">Size that element should use to arrange itself and its children.</param>
        protected sealed override Size ArrangeOverride(Size arrangeSize)
        {
            Invariant.Assert(_structuralCache.DtrList == null || _structuralCache.DtrList.Length == 0 ||
                             (_structuralCache.DtrList.Length == 1 && _structuralCache.BackgroundFormatInfo.DoesFinalDTRCoverRestOfText == true));

            VerifyReentrancy();

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.BeginScope("TextFlow.ArrangeOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("TextFlow.ArrangeOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif

            AdjustForInfinity(ref arrangeSize);


            try
            {
                SetFlags(true, Flags.ArrangeInProgress);
                _documentPage.Arrange(arrangeSize);
            }
            finally
            {
                SetFlags(false, Flags.ArrangeInProgress);
            }

            SetFlags(true, Flags.ArrangedAfterMeasure);

            // TextFlow has only one child visual. If the child visual is not
            // changing, do not re-add it.
            if(_child !=  _documentPage.Visual)
            {
                this.RemoveVisualChild(_child);
                _child =  _documentPage.Visual;
                this.AddVisualChild(_child);
            }           

            // Ensure we're syncced with viewport after arrange.
            _documentPage.UpdateViewport(ref _viewport, false);

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("TextFlow.ArrangeOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.EndScope(MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif
            return arrangeSize;
        }

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: 
        ///       Need to lock down Visual tree during the callbacks.
        ///       During this virtual call it is not valid to modify the Visual tree. 
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {                    
            if(index != 0)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }

            return _child;
        }
        
        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate 
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>        
        protected override int VisualChildrenCount
        {           
            get 
            {         
                return _child == null ? 0 : 1; 
            }
        }



        /// <summary>
        /// Notification that a specified property has been invalidated
        /// </summary>
        /// <param name="e">EventArgs that contains the property, metadata, old value, and new value for this change</param>
        protected sealed override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            // Always call base.OnPropertyChanged, otherwise Property Engine will not work.
            // NOTE: base.OnPropertyChanged will invalidata Measure/Arrange if 
            //       property is affecting it. But in DPS scenarios this is unnecessary, because
            //       it will trigger CPS calls.
            //       For now there is no solution for this problem.
            base.OnPropertyChanged(e);

            if (e.IsAValueChange || e.IsASubPropertyChange)
            {
                // Skip caches invalidation if content has not been formatted yet - non of caches are valid, 
                // so they will be aquired during first formatting (full format).
                if (CheckFlags(Flags.FormattedOnce))
                {
                    FrameworkPropertyMetadata fmetadata = e.Metadata as FrameworkPropertyMetadata;
                    if (fmetadata != null)
                    {
                        bool affectsRender = (fmetadata.AffectsRender &&
                            (e.IsAValueChange || !fmetadata.SubPropertiesDoNotAffectRender));
                        if (fmetadata.AffectsMeasure || fmetadata.AffectsArrange || affectsRender)
                        {
                            // Will throw an exception, if during measure/arrange process.
                            _structuralCache.VerifyTreeIsUnlocked();

#if TEXTPANELLAYOUTDEBUG
                            TextPanelDebug.Log("TextFlow.OnPropertyInvalidated, Name = " + dp.Name, TextPanelDebug.Category.ContentChange);
#endif
                            _structuralCache.InvalidateFormatCache(/*destroy structure*/ true);
                            // None of TextFlow properties can invalidate structural caches (the NameTable),
                            // but most likely it invalidates format caches. Invalidate all
                            // format caches accumulated in the NameTable.

                            // Due to incremental update issues (update info provided by PTS),
                            // arrange is done during measure. Hence there is a need to invalidate
                            // measure, if arrange is affected.
                            // All rendering is done in arrange code, so if rendering only
                            // property is changing there is a need to invalidate arrange.
                            if (fmetadata.AffectsArrange || affectsRender)
                            {
                                InvalidateMeasure();
                                InvalidateVisual(); //ensure re-rendering
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Notification that is called by Measure of a child when it ends up with 
        /// different desired size for the child. 
        /// </summary>
        protected sealed override void OnChildDesiredSizeChanged(UIElement child)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            OnChildUIElementChanged(child);
        }

        /// <summary>
        /// This method is called from during property invalidation time. If the FrameworkElement has a child on which
        /// some property was invalidated and the property was marked as AffectsParentMeasure or AffectsParentArrange
        /// during registration, this method is invoked to let a FrameworkElement know which particualr child must be
        /// remeasured if the FrameworkElement wants to do partial (incremental) update of layout.
        /// <para/>
        /// Olny advanced FrameworkElement, which implement incremental update should override this method. Since
        /// Panel always gets InvalidateMeasure or InvalidateArrange called in this situation, it ensures that
        /// the FrameworkElement will be re-measured and/or re-arranged. Only if the FrameworkElement wants to implement a performance
        /// optimization and avoid calling Measure/Arrange on all children, it should override this method and
        /// store the info about invalidated children, to use subsequently in the FrameworkElement's MeasureOverride/ArrangeOverride
        /// implementations.
        /// <para/>
        /// </summary>
        ///<param name="child">Reference to a child UIElement that had AffectsParentMeasure/AffectsParentArrange property invalidated.</param>
        protected internal sealed override void ParentLayoutInvalidated(UIElement child)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            OnChildUIElementChanged(child);
        }

        /// <summary>
        /// HitTestCore implements precise hit testing against render contents
        /// </summary>
        protected sealed override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (hitTestParameters == null)
            {
                throw new ArgumentNullException("hitTestParameters");
            }

            Rect r = new Rect(new Point(), RenderSize);

            if (r.Contains(hitTestParameters.HitPoint))
            {
                return new PointHitTestResult(this, hitTestParameters.HitPoint);
            }

            return null;
        }

        /// <summary>
        ///     Hit tests to the correct ContentElement 
        ///     within the ContentHost that the mouse 
        ///     is over
        /// </summary>
        /// <param name="point">
        ///     Mouse coordinates relative to 
        ///     the ContentHost
        /// </param>
        protected virtual IInputElement InputHitTestCore(Point point)
        {
            // If measure / arrange data is not valid return 'this'.
            IInputElement ie = null;
            if (IsLayoutDataValid)
            {
                ie = _documentPage.InputHitTestCore(point);
            }
            return (ie != null) ? ie : this;
        }

        /// <summary>
        /// Returns rectangles for element. First finds element by navigating in FlowDocumentPage. 
        /// If element is not found or if call to get rectangles from FlowDocumentPage returns null
        /// we return an empty collection. If the layout is not valid we return null.
        /// </summary>
        /// <param name="child">
        /// Content element for which rectangles are required
        /// </param>
        protected virtual ReadOnlyCollection<Rect> GetRectanglesCore(ContentElement child)
        {
            // Access and parameter validation
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            ReadOnlyCollection<Rect> rectangles = new ReadOnlyCollection<Rect>(new List<Rect>(0));
            if (IsLayoutDataValid)
            {
                // Here we must call the internal function on document page that tells it whether or not
                // to restrict search to only the text segments in the page. 
                rectangles = _documentPage.GetRectanglesCore(child, false);
            }

            Invariant.Assert(rectangles != null);
            return new ReadOnlyCollection<Rect>(rectangles);
        }

        /// <summary>
        /// Returns elements hosted by the content host as an enumerator class
        /// </summary>
        protected virtual IEnumerator<IInputElement> HostedElementsCore
        {
            get
            {
                // Access and parameter validation               
                IEnumerator<IInputElement> hostedElements;
                if (IsLayoutDataValid)
                {
                    // Return data from document page
                    hostedElements = _documentPage.HostedElementsCore as IEnumerator<IInputElement>;
                    Debug.Assert(hostedElements != null);
                }
                else
                {
                    // Return empty collection
                    hostedElements = new HostedElements(new ReadOnlyCollection<TextSegment>(new List<TextSegment>(0)));
                }
                return hostedElements;
            }
        }

        /// <summary>
        /// Called when a UIElement-derived class which is hosted by a IContentHost changes its DesiredSize
        /// </summary>
        /// <param name="child">
        /// Child element whose DesiredSize has changed
        /// </param> 
        protected virtual void OnChildDesiredSizeChangedCore(UIElement child)
        {
            this.OnChildDesiredSizeChanged(child);
        }

#if OLD_AUTOMATION
        /// <summary>
        /// Called to request an implementation for a UIAccess pattern interface
        /// </summary>
        /// <param name="target">Target element to get pattern for</param>
        /// <param name="pattern">Guid indicating which interface this request is for</param>
        /// <returns>null if the specified interface is not supported, otherwise
        /// returns an object that supports the specified interface</returns>
        protected virtual object GetPatternProviderCore(UIElement target, AutomationPattern pattern)
        {
            if (target == this)
            {
                if (pattern == TextPatternIdentifiers.Pattern)
                {
                    return new TextAdaptor(this);
                }
            }

            return null;
        }
#endif

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        internal override string GetPlainText()
        {
            return TextRangeBase.GetTextInternal(this.ContentStart, this.ContentEnd);
        }

        internal void SetTextContainer(TextContainer textContainer)
        {
            if (_structuralCache != null && textContainer == _structuralCache.TextContainer)
            {
                return;
            }

            if (textContainer == null)
            {
                // Create text tree that contains content of the element.
                textContainer = new TextContainer(this);
            }
            else
            {
                _foreignTextContainer = true;
            }

            if (_structuralCache == null)
            {
                // Create structural cache object
                _structuralCache = new StructuralCache(this, textContainer, false);
                _structuralCache.Section = new MS.Internal.PtsHost.Section(_structuralCache);

            }
            else
            {
                _structuralCache.TextContainer.Changing -= new EventHandler(OnTextContainerChanging);
                _structuralCache.TextContainer.Change -= new TextContainerChangeEventHandler(OnTextContainerChange);
                _structuralCache.TextContainer.Highlights.Changed -= new HighlightChangedEventHandler(OnHighlightChanged);
                _structuralCache.TextContainer = textContainer;
            }

            // Initialize default page. This page is used to answer UIElement type requests.
            _documentPage = new FlowDocumentPage(_structuralCache);
            _documentPage.UseSizingWorkaroundForTextBox = UseSizingWorkaroundForTextBox;

            SetFlags(false, Flags.FormattedOnce);

            // Hookup the TextContainer with the TextView, which depends on _documentPage.
            _structuralCache.TextContainer.TextView = this.TextView;

            InvalidateMeasure();
            InvalidateVisual(); //ensure re-rendering
        }


        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeBlocks(XamlDesignerSerializationManager manager) 
        {
            return manager != null && manager.XmlWriter == null;
        }


        #endregion Internal methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        // ------------------------------------------------------------------
        // Workaround to enable size-to-content for TextBox
        // ------------------------------------------------------------------
        internal bool UseSizingWorkaroundForTextBox
        {
            get
            {
                return _useSizingWorkaroundForTextBox;
            }
            set
            {
                Invariant.Assert(!CheckFlags(Flags.FormattedOnce), "Cannot set UseSizingWorkaroundForTextBox after formatting has already happened.");
                _useSizingWorkaroundForTextBox = value;
                _documentPage.UseSizingWorkaroundForTextBox = value;
            }
        }

        // ------------------------------------------------------------------
        // StructuralCache accessor.
        // ------------------------------------------------------------------
        internal StructuralCache StructuralCache { get { return _structuralCache; } }

        // ------------------------------------------------------------------
        // Returns "true" if the layout is clean. "false" otherwise.
        // ------------------------------------------------------------------
        internal bool IsLayoutDataValid
        { 
            get 
            {
                // Layout is clean only when the page is calculated and it
                // is in the clean state - there are no pending changes that affect layout.
                //
                // Hittest can be called with invalid arrange. This happens in
                // following situation:
                //   Something is causing to call InvalidateTree and eventually 
                //   InvalidateAllProperties will be called in such case. In responce
                //   to that following properties are invalidated:
                //   * ClipToBounds - invalidates arrange
                //   * IsEnabled - calls MouseDevice.Synchronize and it will eventually
                //     do hittesting.
                //
                // OR
                //   TextContainer sends Changing event, which invalidates measure,
                //   but we have not yet received a matching Changed event.
                //
                // So, it is possible to receive hittesting request on dirty layout.
                bool layoutValid = _documentPage != null &&  
                    CheckFlags(Flags.FormattedOnce) &&
                    !_structuralCache.ForceReformat &&
                    (_structuralCache.DtrList == null || _structuralCache.BackgroundFormatInfo.DoesFinalDTRCoverRestOfText) &&
                    IsMeasureValid && IsArrangeValid &&
                    !CheckFlags(Flags.ContentChangeInProgress) &&
                    !CheckFlags(Flags.MeasureInProgress) &&
                    !CheckFlags(Flags.ArrangeInProgress);
                return layoutValid;
            }
        }

        //-------------------------------------------------------------------
        // Number of lines formatted during page formatting.
        //-------------------------------------------------------------------
        internal int FormattedLinesCount { get { return (_documentPage != null) ? _documentPage.FormattedLinesCount : 0; } }

        //-------------------------------------------------------------------
        // Typography properties group
        //-------------------------------------------------------------------
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

        //-------------------------------------------------------------------
        // Viewport
        //-------------------------------------------------------------------
        internal Rect Viewport
        {
            get
            {
                return _viewport.FromTextDpi();
            }
        }
        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static void OnTypographyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TextFlow) d)._typographyPropertiesGroup = null;
        }        

        // ------------------------------------------------------------------
        // Initialize TextFlow.
        // ------------------------------------------------------------------
        private void Initialize()
        { 
            SetTextContainer(null);
            SetFlags(true, Flags.ArrangedAfterMeasure); // Initially set it to true.
        }

        // ------------------------------------------------------------------
        // Setup event handlers.
        // Deferred until the first measure.
        // ------------------------------------------------------------------
        private void InitializeForFirstMeasure()
        {
            _structuralCache.TextContainer.Changing += new EventHandler(OnTextContainerChanging);
            _structuralCache.TextContainer.Change += new TextContainerChangeEventHandler(OnTextContainerChange);
            _structuralCache.TextContainer.Highlights.Changed += new HighlightChangedEventHandler(OnHighlightChanged);

            LayoutUpdated += new System.EventHandler(OnLayoutUpdated);
        }

        // ------------------------------------------------------------------
        // Invalidates a portion of text affected by a highlight change.
        // ------------------------------------------------------------------
        private void OnHighlightChanged(object sender, HighlightChangedEventArgs args)
        {
            Invariant.Assert(args != null);
            Invariant.Assert(args.Ranges != null);
            Invariant.Assert(CheckFlags(Flags.FormattedOnce), "Unexpected Highlights.Changed callback before first format!");

            // The only supported highlight type for TextFlow is SpellerHightlight.
            // TextSelection and HighlightComponent are ignored, because they are handled by 
            // separate layer.
            if (args.OwnerType != typeof(SpellerHighlightLayer))
            {
                return;
            }

            // Skip caches/DTRs invalidation if one of following conditions is true:
            // a) has not been formatted yet - non of caches are valid, so they will be
            //    aquired during first formatting (full format).
            // b) there were some events forcing full reformat - all caches have been destroyed already,
            //    and they will be re-aquired during the next formatting request (full format).
            if (!_structuralCache.ForceReformat)
            {
                // Will throw an exception, if during measure/arrange process.
                _structuralCache.VerifyTreeIsUnlocked();

                IList ranges;
                TextSegment textSegment;
                int dtrStart;
                int dtrDelta;
                ITextPointer startPosition;
                int i;

                ranges = args.Ranges;
                startPosition = _structuralCache.TextContainer.Start;

                // Calculate DTRs for the change
                for (i = 0; i < ranges.Count; i++)
                {
                    textSegment = (TextSegment)ranges[i];

                    Debug.Assert(textSegment.Start.CompareTo(textSegment.End) < 0, "Unexpected empty or negative range!");

                    dtrStart = startPosition.GetOffsetToPosition(textSegment.Start);
                    dtrDelta = startPosition.GetOffsetToPosition(textSegment.End) - dtrStart;

                    _structuralCache.AddDirtyTextRange(new DirtyTextRange(dtrStart, dtrDelta, dtrDelta));
                }

                Debug.Assert(_structuralCache.DtrList != null, "Unexpected empty delta!");

                // Force remeasure process, because most likely desired size is going to change.
                InvalidateMeasure();
                InvalidateVisual(); //ensure re-rendering
            }
        }

        // ------------------------------------------------------------------
        // Handler for TextContainer changing notifications.
        // ------------------------------------------------------------------
        private void OnTextContainerChanging(object sender, EventArgs args)
        {
            Debug.Assert(sender == _structuralCache.TextContainer, "Received text change for foreign TextContainer.");
            Invariant.Assert(CheckFlags(Flags.FormattedOnce), "Unexpected TextContainer.Changing callback before first format!");

            if (!_structuralCache.ForceReformat)
            {
                // Will throw an exception, if during measure/arrange process.
                _structuralCache.VerifyTreeIsUnlocked();

                // Remember the fact that content is changing.
                // OnTextContainerChange has to be received after this event.
                SetFlags(true, Flags.ContentChangeInProgress);
            }
        }

        // ------------------------------------------------------------------
        // Handler for TextContainer changed notifications.
        // ------------------------------------------------------------------
        private void OnTextContainerChange(object sender, TextContainerChangeEventArgs args)
        {
            Invariant.Assert(args != null);
            Invariant.Assert(sender == _structuralCache.TextContainer);

            // Skip caches/DTRs invalidation if one of following conditions is true:
            // a) has not been formatted yet - non of caches are valid, so they will be
            //    aquired during first formatting (full format).
            // b) there were some events forcing full reformat - all caches have been destroed already,
            //    and they will be re-aquired during the next formatting request (full format).
            Invariant.Assert(CheckFlags(Flags.FormattedOnce), "Unexpected TextContainer.Changed callback before first format!");
            if (!_structuralCache.ForceReformat)
            {
                // Will throw an exception, if during measure/arrange process.
                _structuralCache.VerifyTreeIsUnlocked();

                // If change happens before we've been arranged, we need to do a full reformat
                if(!CheckFlags(Flags.ArrangedAfterMeasure))
                {
                    _structuralCache.InvalidateFormatCache(true);
                }
                else
                {
                    // Create new DTR for changing range and add it to DRTList.
                    DirtyTextRange dtr = new DirtyTextRange(args);
                    _structuralCache.AddDirtyTextRange(dtr);
                }
                // Invalidate measure in responce to invalidated content.
                InvalidateMeasure();
                InvalidateVisual(); //ensure re-rendering
            }
            // Content has been changed, so reset appropriate flag.
            SetFlags(false, Flags.ContentChangeInProgress);
        }

        // ------------------------------------------------------------------
        // Handler for UIElement children changes.
        // ------------------------------------------------------------------
        internal void OnChildUIElementChanged(UIElement child)
        {
            // During measure/arrange process child may be recalculated and TextFlow
            // can get notified about child DesiredSize change. In such case
            // Structural cache should not be updated, because we are recalculating
            // the child anyway.
            if (CheckFlags(Flags.MeasureInProgress) || CheckFlags(Flags.ArrangeInProgress))
            {
                return;
            }

            // Skip caches/DTRs invalidation if one of following conditions is true:
            // a) has not been formatted yet - non of caches are valid, so they will be
            //    aquired during first formatting (full format).
            // b) there were some events forcing full reformat - all caches have been destroyed already,
            //    and they will be re-aquired during the next formatting request (full format).
            if (CheckFlags(Flags.FormattedOnce) && !_structuralCache.ForceReformat)
            {
                // Will throw an exception, if during measure/arrange process.
                // Disable this check, because it fails when ITextEmbeddable notifies its host about
                // baseline changes.
                //_structuralCache.VerifyTreeIsUnlocked();

#if TEXTPANELLAYOUTDEBUG
                TextPanelDebug.Log("TextFlow.OnChildUIElementChanged, Child=" + child.GetType().Name, TextPanelDebug.Category.ContentChange);
#endif

                // There might be a case when 'child' is not actually a child of TextFlow. In such case
                // ignore this notification.
                int startIndex = TextContainerHelper.GetCPFromEmbeddedObject(child, ElementEdge.BeforeStart);
                if (startIndex < 0) { return; }

                if(!CheckFlags(Flags.ArrangedAfterMeasure))
                {
                    _structuralCache.InvalidateFormatCache(true);
                }
                else
                {
                    // Create new DTR for changing UIElement and add it to DRTList.
                    DirtyTextRange dtr = new DirtyTextRange(startIndex, TextContainerHelper.EmbeddedObjectLength, TextContainerHelper.EmbeddedObjectLength);
                    _structuralCache.AddDirtyTextRange(dtr);
                }

                // Invalidate measure.
                InvalidateMeasure();
                InvalidateVisual();
            }
        }

        // ------------------------------------------------------------------
        // OnRequestBringIntoView is called from the event handler TextFlow 
        // registers for the event. 
        // Handle the event for hosted ContentElements, and raise a new BringIntoView 
        // event with the following values:
        // * object: (this) 
        // * rect: A rect indicating the position of the ContentElement 
        //
        //      sender - The instance handling the event.
        //      args   - RequestBringIntoViewEventArgs indicates the element 
        //              and region to scroll into view.
        // ------------------------------------------------------------------
        private static void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs args)
        {
            TextFlow textFlow = sender as TextFlow;
            ContentElement child = args.TargetObject as ContentElement;

            if (textFlow != null && child != null)
            {
                // Handle original event.
                args.Handled = true;
                
                // Retrieve the first rectangle representing the child and 
                // raise a new BrightIntoView event with such rectangle.
                ReadOnlyCollection<Rect> rects = textFlow.GetRectanglesCore(child);
                Invariant.Assert(rects != null, "Rect collection cannot be null.");
                if (rects.Count > 0)
                {
                    textFlow.BringIntoView(rects[0]);
                }
                else
                {
                    textFlow.BringIntoView();
                }
            }
        }

        // ------------------------------------------------------------------
        // SetFlags is used to set or unset one or multiple flags.
        // ------------------------------------------------------------------
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        // ------------------------------------------------------------------
        // CheckFlags returns true if all of passed flags in the bitmask are set.
        // ------------------------------------------------------------------
        private bool CheckFlags(Flags flags)
        {
            return ((_flags & flags) == flags);
        }

        /// <summary>
        /// Called when the child's BaselineOffset value changes.
        /// </summary>
        internal void OnChildBaselineOffsetChanged(DependencyObject source)
        {
            // One of UIElement children has been changed. Need to 
            // notify NameTable about the change and remeasure content.
            // Treat this notification as OnChildUIElementChanged.
            UIElement uiChild = source as UIElement;
            if (uiChild != null)
            {
                OnChildUIElementChanged(uiChild);
            }
        }

        // ------------------------------------------------------------------
        // Ensures none of our methods can be called during measure/arrange/content change.
        // ------------------------------------------------------------------
        private void VerifyReentrancy()
        {
            if(CheckFlags(Flags.MeasureInProgress))
            {
                throw new InvalidOperationException(SR.Get(SRID.MeasureReentrancyInvalid));
            }

            if(CheckFlags(Flags.ArrangeInProgress))
            {
                throw new InvalidOperationException(SR.Get(SRID.ArrangeReentrancyInvalid));
            }

            if(CheckFlags(Flags.ContentChangeInProgress))
            {
                throw new InvalidOperationException(SR.Get(SRID.TextContainerChangingReentrancyInvalid));
            }
        }

        // ------------------------------------------------------------------
        /// CalculateViewportRect - Called to calculate the visible rect for this element
        // ------------------------------------------------------------------
        private Rect CalculateViewportRect()
        {
            Rect visibleRect = new Rect(0, 0, RenderSize.Width, RenderSize.Height);

            Visual visual = VisualTreeHelper.GetParent(this) as Visual;

            while(visual != null && visibleRect != Rect.Empty)
            {                
                if(VisualTreeHelper.GetClip(visual) != null)
                {
                    GeneralTransform transform = this.TransformToAncestor(visual).Inverse;
                    
                    // Safer version of transform to descendent (doing the inverse ourself), 
                    // we want the rect inside of our space. (Which is always rectangular and much nicer to work with)
                    if(transform != null)
                    {                       
                        Rect rectBounds = VisualTreeHelper.GetClip(visual).Bounds;
                        rectBounds = transform.TransformBounds(rectBounds);
                        
                        visibleRect.Intersect(rectBounds);
                    }
                    else
                    {
                        // No visibility if non-invertable transform exists.
                        return new Rect(0, 0, 0, 0);
                    }
                }

                visual = VisualTreeHelper.GetParent(visual) as Visual;
            }

            return visibleRect;
        }

        // ------------------------------------------------------------------
        /// OnLayoutUpdated - Handler for layout updated event
        // ------------------------------------------------------------------
        private void OnLayoutUpdated(object sender, EventArgs args)
        {
            if(IsMeasureValid && IsArrangeValid && CheckFlags(Flags.FormattedOnce))
            {
                Rect visibleRect = CalculateViewportRect();
                PTS.FSRECT viewportRect = new PTS.FSRECT(visibleRect);

                if(viewportRect != _viewport)
                {
                    _viewport = viewportRect;

                    if (!CheckFlags(Flags.ArrangedAfterMeasure))
                    {
                        // Need to clear update info by running arrange process.
                        // This is necessary, because Measure may be called more than once
                        // before Arrange is called. But PTS is not able to merge update info.
                        // To protect against loosing incremental changes delta, need
                        // to arrange the page and create all necessary visuals.

                        // Arrange process will validate our visuals to be within appropriate viewport as needed, no
                        // separate update required.
                        ArrangeOverride(_documentPage.ContentSize);
                    }
                    else
                    {
                        _documentPage.UpdateViewport(ref _viewport, false);
                    }
                }
            }
        }

        // Change Double.PositiveInfinity to some reasonable value.  Used for unconstrained widths.
        private void AdjustForInfinity(ref Size sizeToAdjust)
        {
            if (sizeToAdjust.Width == Double.PositiveInfinity)
            {
                if (UseSizingWorkaroundForTextBox)
                {
                    sizeToAdjust.Width = _MaximumMilRenderingWidth;  // Very large rects don't get rendered by MIL.
                }
                else
                {
                    sizeToAdjust.Width = _defaultWidth;
                }
            }
        }

        #endregion Private methods

        //-------------------------------------------------------------------
        //
        //  Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        // This TextFlow's TextView.
        private ITextView TextView
        {
            get
            {
                return (ITextView)((IServiceProvider)_documentPage).GetService(typeof(ITextView));
            }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields
        private Visual _child;

        //-------------------------------------------------------------------
        // Structural cache 
        //-------------------------------------------------------------------
        private StructuralCache _structuralCache;

        // True indicates that TextContainer does not belong logically to this TextFlow
        private bool _foreignTextContainer;

        //-------------------------------------------------------------------
        // DocumentPage create for the content.
        //-------------------------------------------------------------------
        private FlowDocumentPage _documentPage;

        //-------------------------------------------------------------------
        // Typography Group Property
        //-------------------------------------------------------------------
        private TypographyProperties _typographyPropertiesGroup;

        //-------------------------------------------------------------------
        // Viewport rect
        //-------------------------------------------------------------------
        private PTS.FSRECT _viewport = new PTS.FSRECT();

        //-------------------------------------------------------------------
        // Flags reflecting various aspects of TextFlow's state.
        //-------------------------------------------------------------------
        private Flags _flags;
        [System.Flags]
        private enum Flags
        {
            FormattedOnce = 0x1,            // Element has been formatted at least once 
            ArrangedAfterMeasure = 0x2,     // Element has been arranged after last Measure
            LastMeasureSuccessful = 0x4,    // Last measure successfully completed
            ContentChangeInProgress = 0x10, // Content change is in progress 
            MeasureInProgress = 0x20,       // Measure in progress
            ArrangeInProgress = 0x40,       // Arrange in progress
        }

        //-------------------------------------------------------------------
        // Width used when no width is specified
        //-------------------------------------------------------------------
        private const double _defaultWidth = 500.0;

        // ------------------------------------------------------------------
        // Workaround to enable size-to-content for TextBox
        // ------------------------------------------------------------------
        private bool _useSizingWorkaroundForTextBox;
        private int _MaximumMilRenderingWidth = 512000; // MIL won't render "really large" values.  Testing showed 512K was about the limit.

        #endregion Private Fields
    }
}



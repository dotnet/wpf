// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: TextBlock element displays text. It is meant for UI scenarios.
//              Most text scenarios should use the FlowDocumentScrollViewer.
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using System.Windows.Automation;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Markup;

using MS.Utility;
using MS.Internal;                  // Invariant.Assert
using MS.Internal.Automation;       // TextAdaptor
using System.Windows.Automation.Peers;
using MS.Internal.Text;
using MS.Internal.Documents;
using MS.Internal.Controls;
using MS.Internal.PresentationFramework;
using MS.Internal.Telemetry.PresentationFramework;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Controls
{
    /// <summary>
    /// TextBlockCache caches the properties and Line which can be
    /// reused during Measure, Arrange and Render phase.
    /// </summary>
    ///
    class TextBlockCache
    {
        public LineProperties _lineProperties;
        public TextRunCache _textRunCache;
    }

    /// <summary>
    /// TextBlock element displays text. It is meant for UI scenarios.
    /// Most text scenarios should use the FlowDocumentScrollViewer.
    /// </summary>
    [ContentProperty("Inlines")]
    [Localizability(LocalizationCategory.Text)]
    public class TextBlock : FrameworkElement, IContentHost, IAddChildInternal, IServiceProvider
    {
        //-------------------------------------------------------------------
        //
        //  IContentHost Members
        //
        //-------------------------------------------------------------------

        #region IContentHost Members

        /// <summary>
        /// Hit tests to the correct ContentElement within the ContentHost
        /// that the mouse is over.
        /// </summary>
        /// <param name="point">Mouse coordinates relative to the ContentHost.</param>
        IInputElement IContentHost.InputHitTest(Point point)
        {
            return this.InputHitTestCore(point);
        }

        /// <summary>
        /// Returns an ICollection of bounding rectangles for the given ContentElement
        /// </summary>
        /// <param name="child">
        /// Content element for which rectangles are required
        /// </param>
        /// <remarks>
        /// Looks at the ContentElement e line by line and gets rectangle bounds for each line
        /// </remarks>
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
        //  IContentHost Members
        //
        //-------------------------------------------------------------------

        #region IAddChild members

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

            EnsureComplexContent();

            if (!(_complexContent.TextContainer is TextContainer))
            {
                throw new ArgumentException(SR.Get(SRID.TextPanelIllegalParaTypeForIAddChild, "value", value.GetType()));
            }

            // Get parent of the text container. Note that it can be not a "this" TextBlock - in case
            // when a TextBlock is used as a storage for plain TextBox - as an owner of a text container.
            Type parentType = _complexContent.TextContainer.Parent.GetType();

            Type valueType = value.GetType();

            // Do implicit conversion to allowed inline type - if possible
            if (!TextSchema.IsValidChildOfContainer(parentType, /*childType*/valueType))
            {
                if (value is UIElement)
                {
                    value = new InlineUIContainer((UIElement)value);
                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.TextSchema_ChildTypeIsInvalid, parentType.Name, valueType.Name));
                }
            }

            // Insert inline element into the text container
            Invariant.Assert(value is Inline, "Schema validation helper must guarantee that invalid element is not passed here");

            TextContainer textContainer = (TextContainer)_complexContent.TextContainer;

            textContainer.BeginChange();
            try
            {
                TextPointer endPosition = textContainer.End;
                textContainer.InsertElementInternal(endPosition, endPosition, (Inline)value);
            }
            finally
            {
                textContainer.EndChange();
            }
        }

        ///<summary>
        /// Called when text appears under the tag in markup.
        ///</summary>
        ///<param name="text">
        /// Text to Add to the Object
        ///</param>
        void IAddChild.AddText(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (_complexContent == null)
            {
                Text = Text + text;
            }
            else
            {
                TextContainer textContainer = (TextContainer)_complexContent.TextContainer;

                textContainer.BeginChange();
                try
                {
                    TextPointer endPosition = textContainer.End;

                    Run implicitRun = Inline.CreateImplicitRun(this);

                    textContainer.InsertElementInternal(endPosition, endPosition, implicitRun);

                    implicitRun.Text = text;
                }
                finally
                {
                    textContainer.EndChange();
                }
            }
        }

        #endregion IAddChild members

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
                if (IsContentPresenterContainer)
                {
                    // We are hosting content that belongs to a ContentPresenter
                    return EmptyEnumerator.Instance;
                }
                else if (_complexContent == null)
                {
                    return new SimpleContentEnumerator(Text);
                }
                else
                {
                    if (!_complexContent.ForeignTextContainer)
                    {
                        return new RangeContentEnumerator(this.ContentStart, this.ContentEnd);
                    }
                    else
                    {
                        return EmptyEnumerator.Instance; // When we do not own the text tree we may not report its children as ours.
                    }
                }
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
        /// TextBlock control currently supports only TextView and TextContainer.
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
            //             VerifyAccess();

            if (serviceType == typeof(ITextView))
            {
                EnsureComplexContent();
                return _complexContent.TextView;
            }
            else if (serviceType == typeof(ITextContainer))
            {
                EnsureComplexContent();
                return _complexContent.TextContainer;
            }
            else if (serviceType == typeof(TextContainer))
            {
                EnsureComplexContent();
                return _complexContent.TextContainer as TextContainer;
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
        /// TextBlock static constructor. Registers metadata for its properties.
        /// </summary>
        static TextBlock()
        {
            // Required here, just for this type - Don't want anyone else to pick up this GetValueOverride
            BaselineOffsetProperty.OverrideMetadata(
                    typeof(TextBlock),
                    new FrameworkPropertyMetadata(
                            null,
                            new CoerceValueCallback(CoerceBaselineOffset)));

            // Registering typography properties metadata
            PropertyChangedCallback onTypographyChanged = new PropertyChangedCallback(OnTypographyChanged);
            DependencyProperty[] typographyProperties = Typography.TypographyPropertiesList;
            for (int i = 0; i < typographyProperties.Length; i++)
            {
                typographyProperties[i].OverrideMetadata(typeof(TextBlock), new FrameworkPropertyMetadata(onTypographyChanged));
            }

            EventManager.RegisterClassHandler(typeof(TextBlock), RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(OnRequestBringIntoView));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBlock), new FrameworkPropertyMetadata(typeof(TextBlock)));

            ControlsTraceLogger.AddControl(TelemetryControls.TextBlock);
        }

        /// <summary>
        /// Initializes a new instance of TextBlock class.
        /// </summary>
        public TextBlock() : base()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new inslace of TextBlock class and adds a first Inline to its Inline collection.
        /// </summary>
        public TextBlock(Inline inline)
            : base()
        {
            Initialize();
            if (inline == null)
            {
                throw new ArgumentNullException("inline");
            }

            this.Inlines.Add(inline);
        }

        private void Initialize()
        {
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
        /// Point to query, in the coordinate space of the Text.
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
        /// <remarks>
        /// The TextPointer returned always has its IsFrozen property set true
        /// and LogicalDirection property set towards a character hit by the point,
        /// or closest to this point.
        /// </remarks>
        public TextPointer GetPositionFromPoint(Point point, bool snapToText)
        {
            TextPointer position;

            if(CheckFlags(Flags.ContentChangeInProgress))
            {
                throw new InvalidOperationException(SR.Get(SRID.TextContainerChangingReentrancyInvalid));
            }

            EnsureComplexContent();

            // Validate layout information on TextView.
            if (((ITextView)_complexContent.TextView).Validate(point))
            {
                position = (TextPointer)_complexContent.TextView.GetTextPositionFromPoint(point, snapToText);
            }
            else
            {
                position = snapToText ? new TextPointer((TextPointer)_complexContent.TextContainer.Start) : null;
            }

            // BUG: We must freeze a pointer before returning.

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
        /// Collection of Inline items contained in this TextBlock.
        /// </value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public InlineCollection Inlines
        {
            get
            {
                return new InlineCollection(this, /*isOwnerParent*/true);
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
                EnsureComplexContent();

                return (TextPointer)_complexContent.TextContainer.Start;
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
                EnsureComplexContent();

                return (TextPointer)_complexContent.TextContainer.End;
            }
        }

        /// <value>
        /// A TextRange spanning the content of this element.
        /// </value>
        internal TextRange TextRange
        {
            get
            {
                // NOTE: We are creating a new instance of a TextRange on each request.
                // We cannot cache the instance, because it may become incorrect
                // after collapsing TextBlock's content and insertion a new one:
                // the cached range would remain empty, which is incorrect.
                return new TextRange(this.ContentStart, this.ContentEnd);
            }
        }

        /// <summary>
        /// Breaking condition before the Element.
        /// </summary>
        public LineBreakCondition BreakBefore { get { return LineBreakCondition.BreakDesired; } }

        /// <summary>
        /// Breaking condition after the Element.
        /// </summary>
        public LineBreakCondition BreakAfter { get { return LineBreakCondition.BreakDesired; } }

        /// <summary>
        /// Access to all text typography properties.
        /// </summary>
        public Typography Typography
        {
            get
            {
                return new Typography(this);
            }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Public Dynamic Properties
        //
        //-------------------------------------------------------------------

        #region Public Dynamic Properties

        /// <summary>
        /// DependencyProperty for <see cref="BaselineOffset" /> property.
        /// </summary>
        public static readonly DependencyProperty BaselineOffsetProperty =
                DependencyProperty.RegisterAttached(
                        "BaselineOffset",
                        typeof(double),
                        typeof(TextBlock),
                        new FrameworkPropertyMetadata(
                                double.NaN,
                                new PropertyChangedCallback(OnBaselineOffsetChanged)));

        /// <summary>
        /// The BaselineOffset property provides an adjustment to baseline offset
        /// </summary>
        public double BaselineOffset
        {
            get { return (double)GetValue(BaselineOffsetProperty); }
            set { SetValue(BaselineOffsetProperty, value); }
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetBaselineOffset(DependencyObject element, double value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(BaselineOffsetProperty, value);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="element">The element to which to read the attached property.</param>
        public static double GetBaselineOffset(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (double)element.GetValue(BaselineOffsetProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="Text" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty TextProperty =
                DependencyProperty.Register(
                        "Text",
                        typeof(string),
                        typeof(TextBlock),
                        new FrameworkPropertyMetadata(
                                string.Empty,
                                FrameworkPropertyMetadataOptions.AffectsMeasure |
                                FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnTextChanged),
                                new CoerceValueCallback(CoerceText)));

        /// <summary>
        /// The Text property defines the content (text) to be displayed.
        /// </summary>
        [Localizability(LocalizationCategory.Text)]
        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Coersion callback for the Text property.
        /// </summary>
        /// <remarks>
        /// We cannot assume value is a string here -- it may be a DeferredTextReference.
        /// </remarks>
        private static object CoerceText(DependencyObject d, object value)
        {
            TextBlock textblock = (TextBlock)d;

            if (value == null)
            {
                value = String.Empty;
            }

            if (textblock._complexContent != null &&
                !textblock.CheckFlags(Flags.TextContentChanging) &&
                (string)value == (string)textblock.GetValue(TextProperty))
            {
                // If the new value equals the old value, then the property
                // system will optimize out the call to OnTextChanged.  We can't
                // skip this call because there's ambiguity between the TextProperty
                // view of content and actual content -- we might have a new
                // value even if strings match.
                //
                // E.g.: content = <Image/>, TextProperty == " "
                // Now setting TextProperty = " " really changes content, replacing
                // the Image with a space char.
                OnTextChanged(d, (string)value);
            }

            return value;
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontFamily" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontFamilyProperty =
                TextElement.FontFamilyProperty.AddOwner(typeof(TextBlock));

        /// <summary>
        /// The FontFamily property specifies the name of font family.
        /// </summary>
        [Localizability(LocalizationCategory.Font)]
        public FontFamily FontFamily
        {
            get { return (FontFamily) GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="FontFamily" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontFamily(DependencyObject element, FontFamily value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="FontFamily" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static FontFamily GetFontFamily(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontFamily)element.GetValue(FontFamilyProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontStyle" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontStyleProperty =
                TextElement.FontStyleProperty.AddOwner(typeof(TextBlock));

        /// <summary>
        /// The FontStyle property requests normal, italic, and oblique faces within a font family.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return (FontStyle) GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="FontStyle" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontStyle(DependencyObject element, FontStyle value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="FontStyle" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static FontStyle GetFontStyle(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontStyle)element.GetValue(FontStyleProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontWeight" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontWeightProperty =
                TextElement.FontWeightProperty.AddOwner(typeof(TextBlock));

        /// <summary>
        /// The FontWeight property specifies the weight of the font.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return (FontWeight) GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="FontWeight" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontWeight(DependencyObject element, FontWeight value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="FontWeight" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static FontWeight GetFontWeight(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontWeight)element.GetValue(FontWeightProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontStretch" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontStretchProperty =
                TextElement.FontStretchProperty.AddOwner(typeof(TextBlock));

        /// <summary>
        /// The FontStretch property selects a normal, condensed, or extended face from a font family.
        /// </summary>
        public FontStretch FontStretch
        {
            get { return (FontStretch) GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="FontStretch" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontStretch(DependencyObject element, FontStretch value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FontStretchProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="FontStretch" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static FontStretch GetFontStretch(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontStretch)element.GetValue(FontStretchProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontSize" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontSizeProperty =
                TextElement.FontSizeProperty.AddOwner(
                        typeof(TextBlock));

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
        /// DependencyProperty setter for <see cref="FontSize" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontSize(DependencyObject element, double value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="FontSize" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        [TypeConverter(typeof(FontSizeConverter))]
        public static double GetFontSize(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (double)element.GetValue(FontSizeProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="Foreground" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ForegroundProperty =
                TextElement.ForegroundProperty.AddOwner(
                        typeof(TextBlock));

        /// <summary>
        /// The Foreground property specifies the foreground brush of an element's text content.
        /// </summary>
        public Brush Foreground
        {
            get { return (Brush) GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="Foreground" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetForeground(DependencyObject element, Brush value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="Foreground" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static Brush GetForeground(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (Brush)element.GetValue(ForegroundProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty BackgroundProperty =
                TextElement.BackgroundProperty.AddOwner(
                        typeof(TextBlock),
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
        /// DependencyProperty for <see cref="TextDecorations" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty TextDecorationsProperty =
                Inline.TextDecorationsProperty.AddOwner(
                        typeof(TextBlock),
                        new FrameworkPropertyMetadata(
                                new FreezableDefaultValueFactory(TextDecorationCollection.Empty),
                                FrameworkPropertyMetadataOptions.AffectsRender
                                ));

        /// <summary>
        /// The TextDecorations property specifies decorations that are added to the text of an element.
        /// </summary>
        public TextDecorationCollection TextDecorations
        {
            get { return (TextDecorationCollection) GetValue(TextDecorationsProperty); }
            set { SetValue(TextDecorationsProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextEffects" /> property.
        /// </summary>
        public static readonly DependencyProperty TextEffectsProperty =
                TextElement.TextEffectsProperty.AddOwner(
                        typeof(TextBlock),
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
        /// DependencyProperty for <see cref="LineHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty LineHeightProperty =
                Block.LineHeightProperty.AddOwner(typeof(TextBlock));

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
        /// DependencyProperty setter for <see cref="LineHeight" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetLineHeight(DependencyObject element, double value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(LineHeightProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="LineHeight" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        [TypeConverter(typeof(LengthConverter))]
        public static double GetLineHeight(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (double)element.GetValue(LineHeightProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="LineStackingStrategy" /> property.
        /// </summary>
        public static readonly DependencyProperty LineStackingStrategyProperty =
                Block.LineStackingStrategyProperty.AddOwner(typeof(TextBlock));

        /// <summary>
        /// The LineStackingStrategy property specifies how lines are placed
        /// </summary>
        public LineStackingStrategy LineStackingStrategy
        {
            get { return (LineStackingStrategy)GetValue(LineStackingStrategyProperty); }
            set { SetValue(LineStackingStrategyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="LineStackingStrategy" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetLineStackingStrategy(DependencyObject element, LineStackingStrategy value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(LineStackingStrategyProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="LineStackingStrategy" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static LineStackingStrategy GetLineStackingStrategy(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (LineStackingStrategy)element.GetValue(LineStackingStrategyProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="Padding" /> property.
        /// </summary>
        public static readonly DependencyProperty PaddingProperty =
                Block.PaddingProperty.AddOwner(
                        typeof(TextBlock),
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
                Block.TextAlignmentProperty.AddOwner(typeof(TextBlock));

        /// <summary>
        /// The TextAlignment property specifies horizontal alignment of the content.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="TextAlignment" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetTextAlignment(DependencyObject element, TextAlignment value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="TextAlignment" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static TextAlignment GetTextAlignment(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (TextAlignment)element.GetValue(TextAlignmentProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextTrimming" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty TextTrimmingProperty =
                DependencyProperty.Register(
                        "TextTrimming",
                        typeof(TextTrimming),
                        typeof(TextBlock),
                        new FrameworkPropertyMetadata(
                                TextTrimming.None,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                        new ValidateValueCallback(IsValidTextTrimming));

        /// <summary>
        /// The TextTrimming property specifies the trimming behavior situation
        /// in case of clipping some textual content caused by overflowing the line's box.
        /// </summary>
        public TextTrimming TextTrimming
        {
            get { return (TextTrimming)GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextWrapping" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty TextWrappingProperty =
                DependencyProperty.Register(
                        "TextWrapping",
                        typeof(TextWrapping),
                        typeof(TextBlock),
                        new FrameworkPropertyMetadata(
                                TextWrapping.NoWrap,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                        new ValidateValueCallback(IsValidTextWrap));

        /// <summary>
        /// The TextWrapping property controls whether or not text wraps
        /// when it reaches the flow edge of its containing block box.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for hyphenation property.
        /// </summary>
        public static readonly DependencyProperty IsHyphenationEnabledProperty =
                Block.IsHyphenationEnabledProperty.AddOwner(typeof(TextBlock));

        /// <summary>
        /// CLR property for hyphenation
        /// </summary>
        public bool IsHyphenationEnabled
        {
            get { return (bool)GetValue(IsHyphenationEnabledProperty); }
            set { SetValue(IsHyphenationEnabledProperty, value); }
        }

        #endregion Public Dynamic Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark:
        ///      During this virtual method the Visual tree must not be modified.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return _complexContent == null ? 0 : _complexContent.VisualChildren.Count; }
        }

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark:
        ///       During this virtual call it is not valid to modify the Visual tree.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (_complexContent == null)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return _complexContent.VisualChildren[index];
        }

        /// <summary>
        /// Content measurement.
        /// </summary>
        /// <param name="constraint">Constraint size.</param>
        /// <returns>Computed desired size.</returns>
        protected sealed override Size MeasureOverride(Size constraint)
        {
            VerifyReentrancy();

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.BeginScope("TextBlock.MeasureOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("TextBlock.MeasureOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif

            // Clear and repopulate our text block cache. (Handles multiple measure before arrange)
            _textBlockCache = null;

            EnsureTextBlockCache();
            LineProperties lineProperties = _textBlockCache._lineProperties;

            // Hook up our TextContainer event listeners if we haven't yet.
            if (CheckFlags(Flags.PendingTextContainerEventInit))
            {
                Invariant.Assert(_complexContent != null);
                InitializeTextContainerListeners();
                SetFlags(false, Flags.PendingTextContainerEventInit);
            }

            // Find out if we can skip measure process. Measure cannot be skipped in following situations:
            // a) content is dirty (properties or content)
            // b) there are inline objects (they may be dynamically sized)
            int lineCount = LineCount;
            if ((lineCount > 0) && IsMeasureValid && InlineObjects == null)
            {
                // Assuming that all of above conditions are true, Measure can be
                // skipped in following situations:
                // 1) TextTrimming == None and:
                //      a) Width is the same, or
                //      b) TextWrapping == NoWrap
                // 2) Width is the same and TextWrapping == NoWrap
                bool bypassMeasure;
                if (lineProperties.TextTrimming == TextTrimming.None)
                {
                    bypassMeasure = DoubleUtil.AreClose(constraint.Width, _referenceSize.Width) || (lineProperties.TextWrapping == TextWrapping.NoWrap);
                }
                else
                {
                    bypassMeasure =
                        DoubleUtil.AreClose(constraint.Width, _referenceSize.Width) &&
                        (lineProperties.TextWrapping == TextWrapping.NoWrap) &&
                        (DoubleUtil.AreClose(constraint.Height, _referenceSize.Height) || lineCount == 1);
                }
                if (bypassMeasure)
                {
                    _referenceSize = constraint;
#if TEXTPANELLAYOUTDEBUG
                    MS.Internal.PtsHost.TextPanelDebug.Log("MeasureOverride bypassed.", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
                    MS.Internal.PtsHost.TextPanelDebug.StopTimer("TextBlock.MeasureOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
                    MS.Internal.PtsHost.TextPanelDebug.EndScope(MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif
                    return _previousDesiredSize;
                }
            }

            // Store constraint size, it is used when measuring inline objects.
            _referenceSize = constraint;

            // Store previous ITextEmbeddable values
            bool formattedOnce = CheckFlags(Flags.FormattedOnce);
            double baselineOffsetPrevious = _baselineOffset;

            // Reset inline objects cache and line metrics cache.
            // They will be fully updated during lines formatting.
            InlineObjects = null;

            // before erasing the line metrics, keep track of how big it was last time
            // so that we can initialize the metrics array to that size this time
            int subsequentLinesInitialSize = (_subsequentLines == null) ? 1 : _subsequentLines.Count;

            ClearLineMetrics();

            if (_complexContent != null)
            {
                _complexContent.TextView.Invalidate();
            }

            // To determine natural size of the text TextAlignment has to be ignored.
            // Since for rendering/hittesting lines are recreated, it can be done without
            // any problems.
            lineProperties.IgnoreTextAlignment = true;
            SetFlags(true, Flags.RequiresAlignment); // Need to update LineMetrics.Start when FinalSize is known.
            SetFlags(true, Flags.FormattedOnce);
            SetFlags(false, Flags.HasParagraphEllipses);
            SetFlags(true, Flags.MeasureInProgress | Flags.TreeInReadOnlyMode);
            Size desiredSize = new Size();
            bool exceptionThrown = true;
            try
            {
                // Create and format lines until end of paragraph is reached.
                // Since we are disposing line object, it can be reused to format following lines.
                Line line = CreateLine(lineProperties);
                bool endOfParagraph = false;
                int dcp = 0;
                TextLineBreak textLineBreakIn = null;

                Thickness padding = this.Padding;
                Size contentSize = new Size(Math.Max(0.0, constraint.Width - (padding.Left + padding.Right)),
                                            Math.Max(0.0, constraint.Height - (padding.Top + padding.Bottom)));
                // Make sure that TextFormatter limitations are not exceeded.
                // Remove it when bug MIL Text API starts allowing Double.PositiveInfinity as ParagraphWidth
                TextDpi.EnsureValidLineWidth(ref contentSize);

                while (!endOfParagraph)
                {
                    using(line)
                    {
                        // Format line. Set showParagraphEllipsis flag to false because we do not know whether or not the line will have
                        // paragraph ellipsis at this time. Since TextBlock is auto-sized we do not know the RenderSize until we finish Measure
                        line.Format(dcp, contentSize.Width, GetLineProperties(dcp == 0, lineProperties), textLineBreakIn, _textBlockCache._textRunCache, /*Show paragraph ellipsis*/ false);

                        double lineHeight = CalcLineAdvance(line.Height, lineProperties);

    #if DEBUG
                        LineMetrics metrics = new LineMetrics(contentSize.Width, line.Length, line.Width, lineHeight, line.BaselineOffset, line.HasInlineObjects(), textLineBreakIn);
    #else
                        LineMetrics metrics = new LineMetrics(line.Length, line.Width, lineHeight, line.BaselineOffset, line.HasInlineObjects(), textLineBreakIn);
    #endif

                        if (!CheckFlags(Flags.HasFirstLine))
                        {
                            SetFlags(true, Flags.HasFirstLine);
                            _firstLine = metrics;
                        }
                        else
                        {
                            if (_subsequentLines == null)
                            {
                                _subsequentLines = new List<LineMetrics>(subsequentLinesInitialSize);
                            }
                            _subsequentLines.Add(metrics);
                        }


                        // Desired width is always max of calculated line widths.
                        // Desired height is sum of all line heights. But if TextTrimming is on
                        // do not overflow the requested height with the exception for the first line.
                        desiredSize.Width = Math.Max(desiredSize.Width, line.GetCollapsedWidth());
                        if ((lineProperties.TextTrimming == TextTrimming.None) ||
                            (contentSize.Height >= (desiredSize.Height + lineHeight)) ||
                            (dcp == 0))
                        {
                            // BaselineOffset is always distance from the Text's top
                            // to the baseline offset of the last line.
                            _baselineOffset = desiredSize.Height + line.BaselineOffset;

                            desiredSize.Height += lineHeight;
                        }
                        else
                        {
                            // Note the fact that there are paragraph ellipses
                            SetFlags(true, Flags.HasParagraphEllipses);
                        }

                        textLineBreakIn = line.GetTextLineBreak();

                        endOfParagraph = line.EndOfParagraph;
                        dcp += line.Length;

                        // don't wrap a line that was artificially broken because of excessive length
                        if (!endOfParagraph &&
                            lineProperties.TextWrapping == TextWrapping.NoWrap &&
                            line.Length == MS.Internal.TextFormatting.TextStore.MaxCharactersPerLine)
                        {
                            endOfParagraph = true;
                        }
                    }
                }

                desiredSize.Width += (padding.Left + padding.Right);
                desiredSize.Height += (padding.Top + padding.Bottom);

                Invariant.Assert(textLineBreakIn == null); // End of paragraph should have no line break record

                exceptionThrown = false;
            }
            finally
            {
                // Restore original line properties
                lineProperties.IgnoreTextAlignment = false;
                SetFlags(false, Flags.MeasureInProgress | Flags.TreeInReadOnlyMode);

                if(exceptionThrown)
                {
                    _textBlockCache._textRunCache = null;
                    ClearLineMetrics();
                }
            }

            // Notify ITextHost that ITextEmbeddable values have been changed, if necessary.
            if (!DoubleUtil.AreClose(baselineOffsetPrevious, _baselineOffset))
            {
                CoerceValue(BaselineOffsetProperty);
            }

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("TextBlock.MeasureOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.EndScope(MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif
            _previousDesiredSize = desiredSize;

            return desiredSize;
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="arrangeSize">Size that element should use to arrange itself and its children.</param>
        protected sealed override Size ArrangeOverride(Size arrangeSize)
        {
            VerifyReentrancy();

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.BeginScope("TextBlock.ArrangeOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("TextBlock.ArrangeOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif
            // Remove all existing visuals. If there are inline objects, they will be added below.
            if (_complexContent != null)
            {
                _complexContent.VisualChildren.Clear();
            }

            ArrayList inlineObjects = InlineObjects;
            int lineCount = LineCount;
            if (inlineObjects != null && lineCount > 0)
            {
                bool exceptionThrown = true;

                SetFlags(true, Flags.TreeInReadOnlyMode);
                SetFlags(true, Flags.ArrangeInProgress);

                try
                {
                    EnsureTextBlockCache();
                    LineProperties lineProperties = _textBlockCache._lineProperties;

                    double wrappingWidth = CalcWrappingWidth(arrangeSize.Width);
                    Vector contentOffset = CalcContentOffset(arrangeSize, wrappingWidth);

                    // Position all inline objects. Recreate only lines that have inline objects
                    // and call arrange on it. Line.Arrange enumerates all inline objects and
                    // sets appropriate transform on them.
                    Line line = CreateLine(lineProperties);
                    int dcp = 0;
                    Vector lineOffset = contentOffset;

                    for (int i = 0; i < lineCount; i++)
                    {
Debug.Assert(lineCount == LineCount);
                        LineMetrics lineMetrics = GetLine(i);

                        if (lineMetrics.HasInlineObjects)
                        {
                            using (line)
                            {
                                // Check if paragraph ellipsis are added to this line
                                bool ellipsis = ParagraphEllipsisShownOnLine(i, lineOffset.Y - contentOffset.Y);
                                line.Format(dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), lineMetrics.TextLineBreak, _textBlockCache._textRunCache, ellipsis);

                                // Check that lineMetrics length and line length are in sync
                                // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                                // MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");

                                // We shut off text alignment for measure, ensure we treat same here.
                                // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                                //if(lineProperties.TextAlignment != TextAlignment.Justify)
                                //{
                                //    Debug.Assert(DoubleUtil.AreClose(CalcLineAdvance(line.Height, lineProperties), lineMetrics.Height), "Line formatting is not consistent.");
                                //}
                                // Calculated line width might be different from measure width in following cases:
                                // a) dynamically sized children, when FinalSize != AvailableSize
                                // b) non-default horizontal alignment, when FinalSize != AvailableSize
                                // Hence do not assert about matching line width with cached line metrics.

                                // Add inline objects to visual children of the TextBlock visual and
                                // set appropriate transforms.
                                line.Arrange(_complexContent.VisualChildren, lineOffset);
                            }
                        }

                        lineOffset.Y += lineMetrics.Height;
                        dcp += lineMetrics.Length;
                    }

                    exceptionThrown = false;
                }
                finally
                {
                    SetFlags(false, Flags.TreeInReadOnlyMode);
                    SetFlags(false, Flags.ArrangeInProgress);
                    if(exceptionThrown)
                    {
                       _textBlockCache._textRunCache = null;
                       ClearLineMetrics();
                    }
                }
            }

            if (_complexContent != null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new DispatcherOperationCallback(OnValidateTextView), EventArgs.Empty);
            }

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("TextBlock.ArrangeOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.EndScope(MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif
            InvalidateVisual();
            return arrangeSize;
        }

        /// <summary>
        /// Render control's content.
        /// </summary>
        /// <param name="ctx">Drawing context.</param>
        protected sealed override void OnRender(DrawingContext ctx)
        {
            VerifyReentrancy();

            if (ctx == null)
            {
                throw new ArgumentNullException("ctx");
            }

            // If layout data is not updated do not render the content.
            if (!IsLayoutDataValid) { return; }

            // Draw background in rectangle.
            Brush background = this.Background;
            if (background != null)
            {
                ctx.DrawRectangle(background, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            }

            SetFlags(false, Flags.RequiresAlignment);
            SetFlags(true, Flags.TreeInReadOnlyMode);
            try
            {
                // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
                EnsureTextBlockCache();
                LineProperties lineProperties = _textBlockCache._lineProperties;


                double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
                Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);
                Point lineOffset = new Point(contentOffset.X, contentOffset.Y);

                // NOTE: All inline objects are UIElements and all of them are direct children of
                // the TextBlock. Hence visuals for those inline objects are already attached.
                // The only responsibility of OnRender is to render text, since transforms for inline
                // objects are set during OnArrange.

                // Create / format / render all lines.
                // Since we are disposing line object, it can be reused to format following lines.
                Line line = CreateLine(lineProperties);
                int dcp = 0;
                bool showParagraphEllipsis = false;
                SetFlags(CheckFlags(Flags.HasParagraphEllipses), Flags.RequiresAlignment);


                int lineCount = LineCount;
                for (int i = 0; i < lineCount; i++)
                {
Debug.Assert(lineCount == LineCount);
                    LineMetrics lineMetrics = GetLine(i);
                    double contentBottom = Math.Max(0.0, RenderSize.Height - Padding.Bottom);

                    // Find out if this is the last rendered line
                    if (CheckFlags(Flags.HasParagraphEllipses))
                    {
                        if (i + 1 < lineCount)
                        {
                            // Calculate bottom offset for next line
                            double nextLineBottomOffset = GetLine(i + 1).Height + lineMetrics.Height + lineOffset.Y;

                            // If the next line will exceed render height by a large margin, we cannot render
                            // it at all and so we should show ellipsis on this one. However if the next line
                            // almost fits, we will render it and so there should be no ellipsis
                            showParagraphEllipsis = DoubleUtil.GreaterThan(nextLineBottomOffset, contentBottom) && !DoubleUtil.AreClose(nextLineBottomOffset, contentBottom);
                        }
                    }

                    // If paragraph ellipsis are enabled, do not render lines that
                    // extend computed layout size. But if the first line does not fit completely,
                    // render it anyway.
                    if (!CheckFlags(Flags.HasParagraphEllipses) ||
                        (DoubleUtil.LessThanOrClose(lineMetrics.Height + lineOffset.Y, contentBottom) || i == 0))
                    {
                        using (line)
                        {
                            line.Format(dcp, wrappingWidth, GetLineProperties(dcp == 0, showParagraphEllipsis, lineProperties), lineMetrics.TextLineBreak, _textBlockCache._textRunCache, showParagraphEllipsis);

                            // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                            //if (!showParagraphEllipsis)
                            //{
                            //    // Check consistency of line formatting
                            //    Debug.Assert(line.Length == lineMetrics.Length, "Line length is out of sync");
                            //    // We shut off text alignment for measure, ensure we treat same here.
                            //    if(lineProperties.TextAlignment != TextAlignment.Justify)
                            //    {
                            //        Debug.Assert(DoubleUtil.AreClose(CalcLineAdvance(line.Height, lineProperties), lineMetrics.Height), "Line height is out of sync.");
                            //    }
                            //    // Calculated line width might be different from measure width in following cases:
                            //    // a) dynamically sized children, when FinalSize != AvailableSize
                            //    // b) non-default horizontal alignment, when FinalSize != AvailableSize
                            //    // Hence do not assert about matching line width with cached line metrics.
                            //}
                            if (!CheckFlags(Flags.HasParagraphEllipses))
                            {
                                lineMetrics = UpdateLine(i, lineMetrics, line.Start, line.Width);
                            }

                            line.Render(ctx, lineOffset);

                            lineOffset.Y += lineMetrics.Height;
                            dcp += lineMetrics.Length;
                        }
                    }
                }
            }
            finally
            {
                SetFlags(false, Flags.TreeInReadOnlyMode);
                _textBlockCache = null;
            }
        }

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
                if (CheckFlags(Flags.FormattedOnce))
                {
                    FrameworkPropertyMetadata fmetadata = e.Metadata as FrameworkPropertyMetadata;
                    if (fmetadata != null)
                    {
                        bool affectsRender = (fmetadata.AffectsRender &&
                            (e.IsAValueChange || !fmetadata.SubPropertiesDoNotAffectRender));

                        if (fmetadata.AffectsMeasure || fmetadata.AffectsArrange || affectsRender)
                        {
                            // Will throw an exception, if during measure/arrange/render process.
                            VerifyTreeIsUnlocked();

                            // TextRunCache stores properties for every single run fetched so far.
                            // If there are any property changes, which affect measure, arrange or
                            // render, invalidate TextRunCache. It will force TextFormatter to refetch
                            // runs and properties.
                           // _lineProperties = null;
                            _textBlockCache = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// HitTestCore implements precise hit testing against render contents
        /// </summary>
        protected sealed override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            VerifyReentrancy();

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
        /// Hit tests to the correct ContentElement within the ContentHost
        /// that the mouse is over.
        /// </summary>
        /// <param name="point">Mouse coordinates relative to the ContentHost.</param>
        protected virtual IInputElement InputHitTestCore(Point point)
        {
            // If layout data is not updated return 'this'.
            if (!IsLayoutDataValid) { return this; }

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();

            // If there is only one line and it is already cached, use it to do hit-testing.
            // Otherwise, do following:
            // a) use cached line information to find which line has been hit,
            // b) re-create the line that has been hit,
            // c) hit-test the line.
            IInputElement ie = null;
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);
            point -= contentOffset; // // Take into account content offset.

            if (point.X < 0 || point.Y < 0) return this;

            ie = null;
            int dcp = 0;
            double lineOffset = 0;

            TextRunCache textRunCache = new TextRunCache();

            int lineCount = LineCount;
            for (int i = 0; i < lineCount; i++)
            {
Debug.Assert(lineCount == LineCount);
                LineMetrics lineMetrics = GetLine(i);

                if (lineOffset + lineMetrics.Height > point.Y)
                {
                    // The current line has been hit. Format the line and
                    // retrieve IInputElement from the hit position.
                    Line line = CreateLine(lineProperties);
                    using (line)
                    {
                        // Check if paragraph ellipsis are rendered
                        bool ellipsis = ParagraphEllipsisShownOnLine(i, lineOffset);
                        line.Format(dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), lineMetrics.TextLineBreak, textRunCache, ellipsis);

                        // Verify consistency of line formatting
                        // Check that lineMetrics.Length is in sync with line.Length
                        // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                        //MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync:");

                        // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                        //Debug.Assert(DoubleUtil.AreClose(CalcLineAdvance(line.Height, lineProperties), lineMetrics.Height), "Line height is out of sync.");
                        // Calculated line width might be different from measure width in following cases:
                        // a) dynamically sized children, when FinalSize != AvailableSize
                        // b) non-default horizontal alignment, when FinalSize != AvailableSize
                        // Hence do not assert about matching line width with cached line metrics.

                        if ((line.Start <= point.X) && (line.Start + line.Width >= point.X))
                        {
                            ie = line.InputHitTest(point.X);
                        }
                    }
                    break; // Line covering the point has been found; no need to continue.
                }

                dcp += lineMetrics.Length;
                lineOffset += lineMetrics.Height;
            }

            // If nothing has been hit, assume that element itself has been hit.
            return (ie != null) ? ie : this;
        }

        /// <summary>
        /// Returns an ICollection of bounding rectangles for the given ContentElement
        /// </summary>
        /// <param name="child">
        /// Content element for which rectangles are required
        /// </param>
        /// <remarks>
        /// Looks at the ContentElement e line by line and gets rectangle bounds for each line
        /// </remarks>
        protected virtual ReadOnlyCollection<Rect> GetRectanglesCore(ContentElement child)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            // If layout data is not updated we assume that we will not be able to find the element we need and throw excception
            if (!IsLayoutDataValid)
            {
                // return empty collection
                return new ReadOnlyCollection<Rect>(new List<Rect>(0));
            }

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();

            // Check for complex content
            if (_complexContent == null || !(_complexContent.TextContainer is TextContainer))
            {
                // return empty collection
                return new ReadOnlyCollection<Rect>(new List<Rect>(0));
            }

            // First find the element start and end position
            TextPointer start = FindElementPosition((IInputElement)child);
            if (start == null)
            {
                return new ReadOnlyCollection<Rect>(new List<Rect>(0));
            }

            TextPointer end = null;
            if (child is TextElement)
            {
                end = new TextPointer(((TextElement)child).ElementEnd);
            }
            else if (child is FrameworkContentElement)
            {
                end = new TextPointer(start);
                end.MoveByOffset(+1);
            }

            if (end == null)
            {
                return new ReadOnlyCollection<Rect>(new List<Rect>(0));
            }

            int startOffset = _complexContent.TextContainer.Start.GetOffsetToPosition(start);
            int endOffset = _complexContent.TextContainer.Start.GetOffsetToPosition(end);

            int lineIndex = 0;
            int lineOffset = 0;
            double lineHeightOffset = 0;
            int lineCount = LineCount;
            while (startOffset >= (lineOffset + GetLine(lineIndex).Length) && lineIndex < lineCount)
            {
Debug.Assert(lineCount == LineCount);
                lineOffset += GetLine(lineIndex).Length;
                lineIndex++;
                lineHeightOffset += GetLine(lineIndex).Height;
            }
            Debug.Assert(lineIndex < lineCount);

            int lineStart = lineOffset;
            List<Rect> rectangles = new List<Rect>();
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);

            TextRunCache textRunCache = new TextRunCache();

            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);
            do
            {
Debug.Assert(lineCount == LineCount);
                // Check that line index never exceeds line count
                Debug.Assert(lineIndex < lineCount);

                // Create lines as long as they are spanned by the element
                LineMetrics lineMetrics = GetLine(lineIndex);

                Line line = CreateLine(lineProperties);

                using (line)
                {
                    // Check if paragraph ellipsis are rendered
                    bool ellipsis = ParagraphEllipsisShownOnLine(lineIndex, lineOffset);
                    line.Format(lineStart, wrappingWidth, GetLineProperties(lineIndex == 0, lineProperties), lineMetrics.TextLineBreak, textRunCache, ellipsis);

                    // Verify consistency of line formatting
                    // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                    if (lineMetrics.Length == line.Length)
                    {
                        //MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");
                        //Debug.Assert(DoubleUtil.AreClose(CalcLineAdvance(line.Height, lineProperties), lineMetrics.Height), "Line height is out of sync.");

                        int boundStart = (startOffset >= lineStart) ? startOffset : lineStart;
                        int boundEnd = (endOffset < lineStart + lineMetrics.Length) ? endOffset : lineStart + lineMetrics.Length;

                        double xOffset = contentOffset.X;
                        double yOffset = contentOffset.Y + lineHeightOffset;
                        List<Rect> lineBounds = line.GetRangeBounds(boundStart, boundEnd - boundStart, xOffset, yOffset);
                        Debug.Assert(lineBounds.Count > 0);
                        rectangles.AddRange(lineBounds);
                    }
                }

                lineStart += lineMetrics.Length;
                lineHeightOffset += lineMetrics.Height;
                lineIndex++;
            }
            while (endOffset > lineStart);

            // Rectangles collection must be non-null
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
                if(CheckFlags(Flags.ContentChangeInProgress))
                {
                    #pragma warning suppress 6503 // IEnumerator.Current is documented to throw this exception
                    throw new InvalidOperationException(SR.Get(SRID.TextContainerChangingReentrancyInvalid));
                }

                if (_complexContent == null || !(_complexContent.TextContainer is TextContainer))
                {
                    // Return empty collection
                    return new HostedElements(new ReadOnlyCollection<TextSegment>(new List<TextSegment>(0)));
                }

                // Create a TextSegment from TextContainer, use it to return enumerator
                System.Collections.Generic.List<TextSegment> textSegmentsList = new System.Collections.Generic.List<TextSegment>(1);
                TextSegment textSegment = new TextSegment(_complexContent.TextContainer.Start, _complexContent.TextContainer.End);
                textSegmentsList.Insert(0, textSegment);
                ReadOnlyCollection<TextSegment> textSegments = new ReadOnlyCollection<TextSegment>(textSegmentsList);

                // Return enumerator created from textSegments
                return new HostedElements(textSegments);
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

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextBlockAutomationPeer(this);
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// remove a child from TextBlock's collection (only Visually)
        /// Used by ComplexLine.cs, etc
        /// </summary>
        internal void RemoveChild(Visual child)
        {
            if (_complexContent != null)
            {
                _complexContent.VisualChildren.Remove(child);
            }
        }

        /// <summary>
        /// Sets external text container as a content for this TextBlock element.
        /// </summary>
        /// <param name="textContainer"></param>
        /// <remarks>
        /// External text container will remain owned by its current owner.
        /// This TextBlock element is not supposed to own it,
        /// which means that this TextBlock will have empty Children collection,
        /// and all top level elements in passed TextContainer will
        /// remain parented in their respective owner.
        /// </remarks>
        internal void SetTextContainer(ITextContainer textContainer)
        {
            // Detach from a previous container
            if (_complexContent != null)
            {
                _complexContent.Detach(this);
                _complexContent = null;
                SetFlags(false, Flags.PendingTextContainerEventInit);
            }
            // Attach new container
            if (textContainer != null)
            {
                _complexContent = null;
                EnsureComplexContent(textContainer);
            }
            SetFlags(false, Flags.ContentChangeInProgress);
            // Invalidate measure in responce to invalidated content.
            InvalidateMeasure();
            InvalidateVisual(); //invlaidate rendering in addition to sizing info
        }

        //-------------------------------------------------------------------
        // Measure child UIElement.
        //
        //      inlineObject - hosted inline object to measure.
        //
        // Returns: Size of the inline object.
        //-------------------------------------------------------------------
        internal Size MeasureChild(InlineObject inlineObject)
        {
            Debug.Assert(_complexContent != null, "Inline objects are supported only in complex content.");

            Size desiredSize;
            // Measure child only during measure pass. If not during measuring
            // use RenderSize.
            if (CheckFlags(Flags.MeasureInProgress))
            {
                // Measure inline objects. Original size constraint is passed,
                // because inline object size should not be dependent on position
                // inside a text line. It should not be also bigger than Text itself.
                Thickness padding = this.Padding;
                Size contentSize = new Size(Math.Max(0.0, _referenceSize.Width - (padding.Left + padding.Right)),
                                            Math.Max(0.0, _referenceSize.Height - (padding.Top + padding.Bottom)));
                inlineObject.Element.Measure(contentSize);
                desiredSize = inlineObject.Element.DesiredSize;

                // Store inline object in the cache.
                ArrayList inlineObjects = InlineObjects;
                bool alreadyCached = false;
                if (inlineObjects == null)
                {
                    InlineObjects = inlineObjects = new ArrayList(1);
                }
                else
                {
                    // Find out if inline object is already cached.
                    for (int index = 0; index < inlineObjects.Count; index++)
                    {
                        if (((InlineObject)inlineObjects[index]).Dcp == inlineObject.Dcp)
                        {
                            Debug.Assert(((InlineObject)inlineObjects[index]).Element == inlineObject.Element, "InlineObject cache is out of sync.");
                            alreadyCached = true;
                            break;
                        }
                    }
                }
                if (!alreadyCached)
                {
                    inlineObjects.Add(inlineObject);
                }
            }
            else
            {
                desiredSize = inlineObject.Element.DesiredSize;
            }
            return desiredSize;
        }

        /// <summary>
        ///     Gives a string representation of this object.
        /// </summary>
        internal override string GetPlainText()
        {
            if (_complexContent != null)
            {
                return TextRangeBase.GetTextInternal(_complexContent.TextContainer.Start, _complexContent.TextContainer.End);
            }
            else
            {
                if (_contentCache != null)
                {
                    return _contentCache;
                }
            }

            return String.Empty;
        }

        /// <summary>
        /// Returns a new array of LineResults for the paragraph's lines.
        /// </summary>
        internal ReadOnlyCollection<LineResult> GetLineResults()
        {
#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.IncrementCounter("TextBlock.GetLines", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            Invariant.Assert(IsLayoutDataValid);

            // Proper line alignment has to be done bofore LineResults are created.
            // Owherwise Line.Start may have wrong value.
            if (CheckFlags(Flags.RequiresAlignment))
            {
                AlignContent();
            }

            // Calculate content offset.
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);

            // Create line results
            int lineCount = LineCount;
            List<LineResult> lines = new List<LineResult>(lineCount);
            int dcp = 0;
            double lineOffset = 0;
            for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
            {
Debug.Assert(lineCount == LineCount);
                LineMetrics lineMetrics = GetLine(lineIndex);

                Rect layoutBox = new Rect(contentOffset.X + lineMetrics.Start, contentOffset.Y + lineOffset, lineMetrics.Width, lineMetrics.Height);
                lines.Add(new TextLineResult(this, dcp, lineMetrics.Length, layoutBox, lineMetrics.Baseline, lineIndex));

                lineOffset += lineMetrics.Height;
                dcp += lineMetrics.Length;
            }
            return new ReadOnlyCollection<LineResult>(lines);
        }

        /// <summary>
        /// Retrieves detailed information about a line of text.
        /// </summary>
        /// <param name="dcp">Index of the first character in the line.</param>
        /// <param name="index"> Index of the line</param>
        /// <param name="lineVOffset"> Vertical offset of the line</param>
        /// <param name="cchContent">Number of content characters in the line.</param>
        /// <param name="cchEllipses">Number of content characters hidden by ellipses.</param>
        internal void GetLineDetails(int dcp, int index, double lineVOffset, out int cchContent, out int cchEllipses)
        {
            Invariant.Assert(IsLayoutDataValid);
            Invariant.Assert(index >= 0 && index < LineCount);

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);

            TextRunCache textRunCache = new TextRunCache();

            // Retrieve details from the line.
            using(Line line = CreateLine(lineProperties))
            {
                // Format line. Set showParagraphEllipsis flag to false
                TextLineBreak textLineBreak = GetLine(index).TextLineBreak;
                bool ellipsis = ParagraphEllipsisShownOnLine(index, lineVOffset);
                line.Format(dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), textLineBreak, textRunCache, ellipsis);

                MS.Internal.Invariant.Assert(GetLine(index).Length == line.Length, "Line length is out of sync");

                cchContent = line.ContentLength;
                cchEllipses = line.GetEllipsesLength();
            }
        }

        /// <summary>
        /// Retrieve text position from the distance (relative to the beginning
        /// of specified line).
        /// </summary>
        /// <param name="dcp">Index of the first character in the line.</param>
        /// <param name="distance">Distance relative to the beginning of the line.</param>
        /// <param name="lineVOffset">
        /// Vertical offset of the line in which the position lies,
        /// </param>
        /// <param name="index">
        /// Index of the line
        /// </param>
        /// <returns>
        /// A text position and its orientation matching or closest to the distance.
        /// </returns>
        internal ITextPointer GetTextPositionFromDistance(int dcp, double distance, double lineVOffset, int index)
        {
#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("TextBlock.GetTextPositionFromDistance", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            Invariant.Assert(IsLayoutDataValid);

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent(); // TextOM access requires complex content.

            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);
            distance -= contentOffset.X;
            lineVOffset -= contentOffset.Y;

            TextRunCache textRunCache = new TextRunCache();
            ITextPointer pos;
            using(Line line = CreateLine(lineProperties))
            {
                MS.Internal.Invariant.Assert(index >= 0 && index < LineCount);
                TextLineBreak textLineBreak = GetLine(index).TextLineBreak;
                bool ellipsis = ParagraphEllipsisShownOnLine(index, lineVOffset);
                line.Format(dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), textLineBreak, textRunCache, ellipsis);

                MS.Internal.Invariant.Assert(GetLine(index).Length == line.Length, "Line length is out of sync");

                CharacterHit charIndex = line.GetTextPositionFromDistance(distance);
                LogicalDirection logicalDirection;

                logicalDirection = (charIndex.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
                pos = _complexContent.TextContainer.Start.CreatePointer(charIndex.FirstCharacterIndex + charIndex.TrailingLength, logicalDirection);
            }

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("TextBlock.GetTextPositionFromDistance", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            return pos;
        }

        /// <summary>
        /// Retrieves bounds of an object/character at the specified TextPointer.
        /// Throws IndexOutOfRangeException if position is out of range.
        /// </summary>
        /// <param name="orientedPosition">Position of an object/character.</param>
        /// <returns>Bounds of an object/character.</returns>
        internal Rect GetRectangleFromTextPosition(ITextPointer orientedPosition)
        {
#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("TextBlock.GetRectangleFromTextPosition", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            Invariant.Assert(IsLayoutDataValid);
            Invariant.Assert(orientedPosition != null);

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent();

            // From TextFormatter get rectangle of a single character.
            // If orientation is Backward, get the length of th previous character.
            int characterIndex = _complexContent.TextContainer.Start.GetOffsetToPosition(orientedPosition);
            int originalCharacterIndex = characterIndex;
            if (orientedPosition.LogicalDirection == LogicalDirection.Backward && characterIndex > 0)
            {
                --characterIndex;
            }

            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);

            double lineOffset = 0;
            int dcp = 0;

            TextRunCache textRunCache = new TextRunCache();

            Rect rect = Rect.Empty;
            FlowDirection flowDirection = FlowDirection.LeftToRight;

            int lineCount = LineCount;
            for (int i = 0; i < lineCount; i++)
            {
Debug.Assert(lineCount == LineCount);
                LineMetrics lineMetrics = GetLine(i);

                // characterIndex needs to be within line range. If position points to
                // dcp + line.Length, it means that the next line starts from such position,
                // hence go to the next line.
                // But if this is the last line (EOP character), get rectangle form the last
                // character of the line.
                if (dcp + lineMetrics.Length > characterIndex ||
                    ((dcp + lineMetrics.Length == characterIndex) && (i == lineCount - 1)))
                {
                    using(Line line = CreateLine(lineProperties))
                    {
                        bool ellipsis = ParagraphEllipsisShownOnLine(i, lineOffset);
                        line.Format(dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), lineMetrics.TextLineBreak, textRunCache, ellipsis);

                        // Check consistency of line length
                        MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");

                        rect = line.GetBoundsFromTextPosition(characterIndex, out flowDirection);
                    }

                    break;
                }

                dcp += lineMetrics.Length;
                lineOffset += lineMetrics.Height;
            }

            if (!rect.IsEmpty) // Empty rects can't be modified
            {
                rect.X += contentOffset.X;
                rect.Y += contentOffset.Y + lineOffset;

                // Return only TopLeft and Height.
                // Adjust rect.Left by taking into account flow direction of the
                // content and orientation of input position.
                if (lineProperties.FlowDirection != flowDirection)
                {
                    if (orientedPosition.LogicalDirection == LogicalDirection.Forward || originalCharacterIndex == 0)
                    {
                        rect.X = rect.Right;
                    }
                }
                else
                {
                    // NOTE: check for 'originalCharacterIndex > 0' is only required for position at the beginning
                    //       content with Backward orientation. This should not be a valid position.
                    //       Remove it later
                    if (orientedPosition.LogicalDirection == LogicalDirection.Backward && originalCharacterIndex > 0)
                    {
                        rect.X = rect.Right;
                    }
                }
                rect.Width = 0;
            }
#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("TextBlock.GetRectangleFromTextPosition", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif

            return rect;
        }

        /// <summary>
        /// Implementation of TextParagraphView.GetTightBoundingGeometryFromTextPositions.
        /// <seealso cref="TextParagraphView.GetTightBoundingGeometryFromTextPositions"/>
        /// </summary>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
        {
#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("TextBlock.GetTightBoundingGeometryFromTextPositions", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            Invariant.Assert(IsLayoutDataValid);
            Invariant.Assert(startPosition != null);
            Invariant.Assert(endPosition != null);
            Invariant.Assert(startPosition.CompareTo(endPosition) <= 0);

            Geometry geometry = null;

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent(); // TextOM access requires complex content.

            int dcpPositionStart = _complexContent.TextContainer.Start.GetOffsetToPosition(startPosition);
            int dcpPositionEnd = _complexContent.TextContainer.Start.GetOffsetToPosition(endPosition);

            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);

            TextRunCache textRunCache = new TextRunCache();
            Line line = CreateLine(lineProperties);

            int dcpLineStart = 0;
            ITextPointer endOfLineTextPointer = _complexContent.TextContainer.Start.CreatePointer(0);
            double lineOffset = 0;

            int lineCount = LineCount;
            for (int i = 0, count = lineCount; i < count; ++i)
            {
                LineMetrics lineMetrics = GetLine(i);

                if (dcpPositionEnd <= dcpLineStart)
                {
                    //  this line starts after the range's end.
                    //  safe to break from the loop.
                    break;
                }

                int dcpLineEnd = dcpLineStart + lineMetrics.Length;
                endOfLineTextPointer.MoveByOffset(lineMetrics.Length);

                if (dcpPositionStart < dcpLineEnd)
                {
                    using (line)
                    {
                        bool ellipsis = ParagraphEllipsisShownOnLine(i, lineOffset);
                        line.Format(dcpLineStart, wrappingWidth, GetLineProperties(dcpLineStart == 0, lineProperties), lineMetrics.TextLineBreak, textRunCache, ellipsis);

                        if (Invariant.Strict)
                        {
                            // Check consistency of line formatting
                            MS.Internal.Invariant.Assert(GetLine(i).Length == line.Length, "Line length is out of sync");
                        }

                        int dcpStart = Math.Max(dcpLineStart, dcpPositionStart);
                        int dcpEnd = Math.Min(dcpLineEnd, dcpPositionEnd);

                        if (dcpStart != dcpEnd)
                        {
                            IList<Rect> aryTextBounds = line.GetRangeBounds(dcpStart, dcpEnd - dcpStart, contentOffset.X, contentOffset.Y + lineOffset);

                            if (aryTextBounds.Count > 0)
                            {
                                int j = 0;
                                int c = aryTextBounds.Count;

                                do
                                {
                                    Rect rect = aryTextBounds[j];

                                    if (j == (c - 1)
                                       && dcpPositionEnd >= dcpLineEnd
                                       && TextPointerBase.IsNextToAnyBreak(endOfLineTextPointer, LogicalDirection.Backward))
                                    {
                                        double endOfParaGlyphWidth = FontSize * CaretElement.c_endOfParaMagicMultiplier;
                                        rect.Width = rect.Width + endOfParaGlyphWidth;
                                    }

                                    RectangleGeometry rectGeometry = new RectangleGeometry(rect);
                                    CaretElement.AddGeometry(ref geometry, rectGeometry);
                                } while (++j < c);
                            }
                        }
                    }
                }

                dcpLineStart += lineMetrics.Length;
                lineOffset += lineMetrics.Height;
            }

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("TextBlock.GetTightBoundingGeometryFromTextPositions", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            return (geometry);
        }

        /// <summary>
        /// Determines if the given position is at the edge of a caret unit
        /// in the specified direction, and returns true if it is and false otherwise.
        /// Used by the ITextView.IsCaretAtUnitBoundary(ITextPointer position) in
        /// TextParagraphView
        /// </summary>
        /// <param name="position">
        /// Position to test.
        /// </param>
        /// <param name="dcp">
        /// Offset of the current position from start of TextContainer
        /// </param>
        /// <param name="lineIndex">
        /// Index of line in which position is found
        /// </param>
        internal bool IsAtCaretUnitBoundary(ITextPointer position, int dcp, int lineIndex)
        {
            Invariant.Assert(IsLayoutDataValid);
            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent();

            TextRunCache textRunCache = new TextRunCache();
            bool isAtCaretUnitBoundary = false;

            int characterIndex = _complexContent.TextContainer.Start.GetOffsetToPosition(position);
            CharacterHit charHit = new CharacterHit();
            if (position.LogicalDirection == LogicalDirection.Backward)
            {
                if (characterIndex > dcp)
                {
                    // Go to trailing edge of previous character
                    charHit = new CharacterHit(characterIndex - 1, 1);
                }
                else
                {
                    // We should not be at line's start dcp with backward context, except in case this is the first line. This is not
                    // a unit boundary
                    return false;
                }
            }
            else if (position.LogicalDirection == LogicalDirection.Forward)
            {
                // Get leading edge of this character index
                charHit = new CharacterHit(characterIndex, 0);
            }

            LineMetrics lineMetrics = GetLine(lineIndex);
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);

            using(Line line = CreateLine(lineProperties))
            {
                // Format line. Set showParagraphEllipsis flag to false since we are not using information about
                // ellipsis to change line offsets in this case.
                line.Format(dcp, wrappingWidth, GetLineProperties(lineIndex == 0, lineProperties), lineMetrics.TextLineBreak, textRunCache, false);

                // Check consistency of line formatting
                MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");
                isAtCaretUnitBoundary = line.IsAtCaretCharacterHit(charHit);
            }

            return isAtCaretUnitBoundary;
        }

        /// <summary>
        /// Finds and returns the next position at the edge of a caret unit in
        /// specified direction.
        /// </summary>
        /// <param name="position">
        /// Initial text position of an object/character.
        /// </param>
        /// <param name="direction">
        /// If Forward, this method returns the "caret unit" position following
        /// the initial position.
        /// If Backward, this method returns the caret unit" position preceding
        /// the initial position.
        /// </param>
        /// <param name="dcp">
        /// Offset of the current position from start of TextContainer
        /// </param>
        /// <param name="lineIndex">
        /// Index of line in which position is found
        /// </param>
        internal ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction, int dcp, int lineIndex)
        {
            Invariant.Assert(IsLayoutDataValid);

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent();

            int characterIndex = _complexContent.TextContainer.Start.GetOffsetToPosition(position);

            // Process special cases
            if (characterIndex == dcp && direction == LogicalDirection.Backward)
            {
                // Start of line
                if (lineIndex == 0)
                {
                    // First line. Cannot go back any further
                    return position;
                }
                else
                {
                    // Change lineIndex and dcp
                    Debug.Assert(lineIndex > 0);
                    --lineIndex;
                    dcp -= GetLine(lineIndex).Length;
                    Debug.Assert(dcp >= 0);
                }
            }
            else if (characterIndex == (dcp + GetLine(lineIndex).Length) && direction == LogicalDirection.Forward)
            {
                // End of line
                int lineCount = LineCount;
                if (lineIndex == lineCount - 1)
                {
                    // Cannot go down any further
                    return position;
                }
                else
                {
                    // Change lineIndex and dcp to next line
                    Debug.Assert(lineIndex < lineCount - 1);
                    dcp += GetLine(lineIndex).Length;
                    ++lineIndex;
                }
            }

            TextRunCache textRunCache = new TextRunCache();

            // Creat CharacterHit from characterIndex and call line APIs
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            CharacterHit textSourceCharacterIndex = new CharacterHit(characterIndex, 0);

            CharacterHit nextCharacterHit;
            LineMetrics lineMetrics = GetLine(lineIndex);

            using(Line line = CreateLine(lineProperties))
            {
                // Format line. Set showParagraphEllipsis flag to false since we are not using information about
                // ellipsis to change line offsets in this case.
                line.Format(dcp, wrappingWidth, GetLineProperties(lineIndex == 0, lineProperties), lineMetrics.TextLineBreak, textRunCache, false);

                // Check consistency of line formatting
                MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");

                if (direction == LogicalDirection.Forward)
                {
                    // Get the next caret position from the line
                    nextCharacterHit = line.GetNextCaretCharacterHit(textSourceCharacterIndex);
                }
                else
                {
                    // Get previous caret position from the line
                    nextCharacterHit = line.GetPreviousCaretCharacterHit(textSourceCharacterIndex);
                }
            }

            // Determine logical direction for next caret index and create TextPointer from it
            LogicalDirection logicalDirection;
            if ((nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength == (dcp + GetLine(lineIndex).Length)) && direction == LogicalDirection.Forward)
            {
                // Going forward brought us to the end of a line, context must be forward for next line
                if (lineIndex == LineCount - 1)
                {
                    // last line so context must stay backward
                    logicalDirection = LogicalDirection.Backward;
                }
                else
                {
                    logicalDirection = LogicalDirection.Forward;
                }
            }
            else if ((nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength == dcp) && direction == LogicalDirection.Backward)
            {
                // Going forward brought us to the start of a line, context must be backward for previous line
                if (dcp == 0)
                {
                    // First line, so we will stay forward
                    logicalDirection = LogicalDirection.Forward;
                }
                else
                {
                    logicalDirection = LogicalDirection.Backward;
                }
            }
            else
            {
                logicalDirection = (nextCharacterHit.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
            }
            ITextPointer nextCaretPosition = _complexContent.TextContainer.Start.CreatePointer(nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength, logicalDirection);


            // Return nextCaretPosition
            return nextCaretPosition;
        }

        /// <summary>
        /// Finds and returns the position after backspace at the edge of a caret unit in
        /// specified direction.
        /// </summary>
        /// <param name="position">
        /// Initial text position of an object/character.
        /// </param>
        /// <param name="dcp">
        /// Offset of the current position from start of TextContainer
        /// </param>
        /// <param name="lineIndex">
        /// Index of line in which position is found
        /// </param>
        internal ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position, int dcp, int lineIndex)
        {
            Invariant.Assert(IsLayoutDataValid);

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent();

            // Get character index for position
            int characterIndex = _complexContent.TextContainer.Start.GetOffsetToPosition(position);

            // Process special cases
            if (characterIndex == dcp)
            {
                if (lineIndex == 0)
                {
                    // Cannot go back any further
                    return position;
                }
                else
                {
                    // Change lineIndex and dcp to previous line
                    Debug.Assert(lineIndex > 0);
                    --lineIndex;
                    dcp -= GetLine(lineIndex).Length;
                    Debug.Assert(dcp >= 0);
                }
            }

            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            CharacterHit textSourceCharacterIndex = new CharacterHit(characterIndex, 0);
            CharacterHit backspaceCharacterHit;
            LineMetrics lineMetrics = GetLine(lineIndex);

            TextRunCache textRunCache = new TextRunCache();
            // Create and Format line
            using(Line line = CreateLine(lineProperties))
            {
                // Format line. Set showParagraphEllipsis flag to false since we are not using information about
                // ellipsis to change line offsets in this case.
                line.Format(dcp, wrappingWidth, GetLineProperties(lineIndex == 0, lineProperties), lineMetrics.TextLineBreak, textRunCache, false);

                // Check consistency of line formatting
                MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");

                backspaceCharacterHit = line.GetBackspaceCaretCharacterHit(textSourceCharacterIndex);
            }
            // Get CharacterHit and call line API

            // Determine logical direction for next caret index and create TextPointer from it
            LogicalDirection logicalDirection;
            if (backspaceCharacterHit.FirstCharacterIndex + backspaceCharacterHit.TrailingLength == dcp)
            {
                // Going forward brought us to the start of a line, context must be backward for previous line
                if (dcp == 0)
                {
                    // First line, so we will stay forward
                    logicalDirection = LogicalDirection.Forward;
                }
                else
                {
                    logicalDirection = LogicalDirection.Backward;
                }
            }
            else
            {
                logicalDirection = (backspaceCharacterHit.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
            }
            ITextPointer backspaceCaretPosition = _complexContent.TextContainer.Start.CreatePointer(backspaceCharacterHit.FirstCharacterIndex + backspaceCharacterHit.TrailingLength, logicalDirection);

            // Return backspaceCaretPosition
            return backspaceCaretPosition;
        }

        #endregion Internal methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        //-------------------------------------------------------------------
        // Text formatter object
        //-------------------------------------------------------------------
        internal TextFormatter TextFormatter
        {
            get
            {
                TextFormattingMode textFormattingMode = TextOptions.GetTextFormattingMode(this);
                if (TextFormattingMode.Display == textFormattingMode)
                {
                    if (_textFormatterDisplay == null)
                    {
                        _textFormatterDisplay = System.Windows.Media.TextFormatting.TextFormatter.FromCurrentDispatcher(textFormattingMode);
                    }
                    return _textFormatterDisplay;
                }
                else
                {
                    if (_textFormatterIdeal == null)
                    {
                        _textFormatterIdeal = System.Windows.Media.TextFormatting.TextFormatter.FromCurrentDispatcher(textFormattingMode);
                    }
                    return _textFormatterIdeal;
                }
            }
        }

        //-------------------------------------------------------------------
        // Text container.
        //-------------------------------------------------------------------
        internal ITextContainer TextContainer
        {
            get
            {
                EnsureComplexContent();
                return _complexContent.TextContainer;
            }
        }

        //-------------------------------------------------------------------
        // TextView
        //-------------------------------------------------------------------
        internal ITextView TextView
        {
            get
            {
                EnsureComplexContent();
                return _complexContent.TextView;
            }
        }

        //-------------------------------------------------------------------
        // Highlights
        //-------------------------------------------------------------------
        internal Highlights Highlights
        {
            get
            {
                EnsureComplexContent();
                return _complexContent.Highlights;
            }
        }

        //-------------------------------------------------------------------
        // TextBlock paragraph properties.
        //-------------------------------------------------------------------
        internal LineProperties ParagraphProperties
        {
            get
            {
                LineProperties lineProperties = GetLineProperties();
                return lineProperties;
            }
        }


        //-------------------------------------------------------------------
        // IsLayoutDataValid
        //-------------------------------------------------------------------
        internal bool IsLayoutDataValid
        {
            get
            {
                return  IsMeasureValid && IsArrangeValid &&         // Measure and Arrange are valid
                        CheckFlags(Flags.HasFirstLine) &&
                        !CheckFlags(Flags.ContentChangeInProgress) &&
                        !CheckFlags(Flags.MeasureInProgress) &&
                        !CheckFlags(Flags.ArrangeInProgress);  // Content is not currently changeing
            }
        }

        //-------------------------------------------------------------------
        // HasComplexContent
        //-------------------------------------------------------------------
        internal bool HasComplexContent
        {
            get
            {
                return (_complexContent != null);
            }
        }

        //-------------------------------------------------------------------
        // IsTypographyDefaultValue
        //-------------------------------------------------------------------
        internal bool IsTypographyDefaultValue
        {
            get
            {
                return !CheckFlags(Flags.IsTypographySet);
            }
        }

        //-------------------------------------------------------------------
        // InlineObjects
        //-------------------------------------------------------------------
        private ArrayList InlineObjects
        {
            get { return (_complexContent == null) ? null : _complexContent.InlineObjects; }
            set { if (_complexContent != null) _complexContent.InlineObjects = value; }
        }

        //-------------------------------------------------------------------
        // Is this TextBlock control being used by a ContentPresenter/ HyperLink
        // to host its content. If it is then TextBlock musn't try to disconnect the
        // logical parent pointer for the content. This flag allows the TextBlock
        // to discover this special scenario and behave differently.
        //-------------------------------------------------------------------
        internal bool IsContentPresenterContainer
        {
            get { return CheckFlags(Flags.IsContentPresenterContainer); }
            set { SetFlags(value, Flags.IsContentPresenterContainer); }
        }

        //  typography properties changed, no cache for this, just reset the flag
        private static void OnTypographyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TextBlock) d).SetFlags(true, Flags.IsTypographySet);
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Raise TextView.Updated event, if TextView is in a valid state.
        /// </summary>
        private object OnValidateTextView(object arg)
        {
            if (IsLayoutDataValid && _complexContent != null)
            {
                _complexContent.TextView.OnUpdated();
            }
            return null;
        }

        // Inserts text run into TextBlock in a form consistent with flow schema requirements.
        //
        // TextBlock has dual role as a text container -
        // plain text for TextBox and rich inline-collection content for everything else.
        // In case of plain text we insert only string into the text container,
        // in all other cases we must wrap eact rext run by Run inline element.
        private static void InsertTextRun(ITextPointer position, string text, bool whitespacesIgnorable)
        {
            // We distinguish these two cases by parent of TextContainer:
            // for plain text case it is TextBox.
            if (!(position is TextPointer) || ((TextPointer)position).Parent == null || ((TextPointer)position).Parent is TextBox)
            {
                position.InsertTextInRun(text);
            }
            else
            {
                if (!whitespacesIgnorable || text.Trim().Length > 0)
                {
                    Run implicitRun = Inline.CreateImplicitRun(((TextPointer)position).Parent);

                    ((TextPointer)position).InsertTextElement(implicitRun);
                    implicitRun.Text = text;
                }
            }
        }

        // ------------------------------------------------------------------
        // Create appropriate line object.
        // a) SimpleLine, if the content is represented by a string.
        // b) ComplexLine, if the content is represented by a TextContainer.
        // ------------------------------------------------------------------
        private Line CreateLine(LineProperties lineProperties)
        {
            Line line;
            if (_complexContent == null)
                line = new SimpleLine(this, Text, lineProperties.DefaultTextRunProperties);
            else
                line = new ComplexLine(this);
            return line;
        }

        // ------------------------------------------------------------------
        // Make sure that complex content is enabled.  Creates a default TextContainer.
        // ------------------------------------------------------------------
        private void EnsureComplexContent()
        {
            EnsureComplexContent(null);
        }

        // ------------------------------------------------------------------
        // Make sure that complex content is enabled.
        // ------------------------------------------------------------------
        private void EnsureComplexContent(ITextContainer textContainer)
        {
            if (_complexContent == null)
            {
                if (textContainer == null)
                {
                    textContainer = new TextContainer(IsContentPresenterContainer ? null : this, false /* plainTextOnly */);
                }

                _complexContent = new ComplexContent(this, textContainer, false, Text);
                _contentCache = null;

                if (CheckFlags(Flags.FormattedOnce))
                {
                    // If we've already measured at least once, hook up the TextContainer
                    // listeners now.
                    Invariant.Assert(!CheckFlags(Flags.PendingTextContainerEventInit));
                    InitializeTextContainerListeners();

                    // Line layout data cached up to this point will become invalid
                    // becasue of content structure change (implicit Run added).
                    // So we need to clear the cache - we call InvalidateMeasure for this
                    // purpose. However, we do not want to produce a side effect
                    // of making layout invalid as a result of touching ContentStart/ContentEnd
                    // and other properties. For that we need to UpdateLayout when it was
                    // dirtied by our switch.
                    bool wasLayoutValid = this.IsMeasureValid && this.IsArrangeValid;
                    InvalidateMeasure();
                    InvalidateVisual(); //ensure re-rendering too
                    if (wasLayoutValid)
                    {
                        UpdateLayout();
                    }
                }
                else
                {
                    // Otherwise, wait until our first measure.
                    // This lets us skip the work for all content invalidation
                    // during load, before the first measure.
                    SetFlags(true, Flags.PendingTextContainerEventInit);
                }
            }
        }

        // ------------------------------------------------------------------
        // Make sure that complex content is cleared.
        // ------------------------------------------------------------------
        private void ClearComplexContent()
        {
            if (_complexContent != null)
            {
                _complexContent.Detach(this);
                _complexContent = null;
                Invariant.Assert(_contentCache == null, "Content cache should be null when complex content exists.");
            }
        }

        // ------------------------------------------------------------------
        // Invalidates a portion of text affected by a highlight change.
        // ------------------------------------------------------------------
        private void OnHighlightChanged(object sender, HighlightChangedEventArgs args)
        {
            Invariant.Assert(args != null);
            Invariant.Assert(args.Ranges != null);
            Invariant.Assert(CheckFlags(Flags.FormattedOnce), "Unexpected Highlights.Changed callback before first format!");

            // The only supported highlight type for TextBlock is SpellerHightlight.
            // TextSelection and HighlightComponent are ignored, because they are handled by
            // separate layer.
            if (args.OwnerType != typeof(SpellerHighlightLayer))
            {
                return;
            }

            // NOTE: Assuming that only rendering only properties are changeing
            //       through highlights.
            InvalidateVisual();
        }

        // ------------------------------------------------------------------
        // Handler for TextContainer changing notifications.
        // ------------------------------------------------------------------
        private void OnTextContainerChanging(object sender, EventArgs args)
        {
            Debug.Assert(sender == _complexContent.TextContainer, "Received text change for foreign TextContainer.");

            if (CheckFlags(Flags.FormattedOnce))
            {
                // Will throw an exception, if during measure/arrange/render process.
                VerifyTreeIsUnlocked();

                // Remember the fact that content is changing.
                // OnTextContainerEndChanging has to be received after this event.
                SetFlags(true, Flags.ContentChangeInProgress);
            }
        }

        // ------------------------------------------------------------------
        // Handler for TextContainer changed notifications.
        // ------------------------------------------------------------------
        private void OnTextContainerChange(object sender, TextContainerChangeEventArgs args)
        {
            Invariant.Assert(args != null);

            if (_complexContent == null)
            {
                // This shouldn't ever happen (we only hook up this handler when we have complex
                // content)... except that it does happen, in cases where TextBlock is part of
                // a style that gets changed in response to TextContainer.Changed events.  In such a case,
                // we're an obsolete text control and we don't want to do anything, so just return.
                return;
            }
            Invariant.Assert(sender == _complexContent.TextContainer, "Received text change for foreign TextContainer.");

            if (args.Count == 0)
            {
                // A no-op for this control.  Happens when IMECharCount updates happen
                // without corresponding SymbolCount changes.
                return;
            }

            if (CheckFlags(Flags.FormattedOnce))
            {
                // Will throw an exception, if during measure/arrange/render process.
                VerifyTreeIsUnlocked();
                // Content has been changed, so reset appropriate flag.
                SetFlags(false, Flags.ContentChangeInProgress);
                // Invalidate measure in responce to invalidated content.
                InvalidateMeasure();
            }

            if (!CheckFlags(Flags.TextContentChanging) && args.TextChange != TextChangeType.PropertyModified)
            {
                SetFlags(true, Flags.TextContentChanging);
                try
                {
                    // Use a DeferredTextReference instead of calculating the new
                    // value now for better performance.  Most of the time no
                    // one cares what the new is, and loading our content into a
                    // string can be expensive.
                    SetDeferredValue(TextProperty, new DeferredTextReference(this.TextContainer));
                }
                finally
                {
                    SetFlags(false, Flags.TextContentChanging);
                }
            }
        }

        private void EnsureTextBlockCache()
        {
            if (null == _textBlockCache)
            {
                _textBlockCache = new TextBlockCache();
                _textBlockCache._lineProperties = GetLineProperties();
                _textBlockCache._textRunCache = new TextRunCache();
            }
        }

        // ------------------------------------------------------------------
        // Refetch and cache line properties, if needed.
        // ------------------------------------------------------------------
        private LineProperties GetLineProperties()
        {
            // For default text properties always set background to null.
            // REASON: If element associated with the text run is TextBlock element, ignore background
            //         brush, because it is handled outside as FrameworkElement's background.

            TextProperties defaultTextProperties = new TextProperties(this, this.IsTypographyDefaultValue);

            // Do not allow hyphenation for plain Text so always pass null for IHyphenate.
            // Pass page width and height as double.MaxValue when creating LineProperties, since TextBlock does not restrict
            // TextIndent or LineHeight
            LineProperties lineProperties = new LineProperties(this, this, defaultTextProperties, null);

            bool isHyphenationEnabled = (bool) this.GetValue(IsHyphenationEnabledProperty);
            if(isHyphenationEnabled)
            {
                lineProperties.Hyphenator = EnsureHyphenator();
            }


            return lineProperties;
        }

        //-------------------------------------------------------------------
        // Get line properties
        //
        //      firstLine - is it for the first line?
        //
        // Returns: Line properties for first/following lines.
        //-------------------------------------------------------------------
        private TextParagraphProperties GetLineProperties(bool firstLine, LineProperties lineProperties)
        {
            return GetLineProperties(firstLine, false, lineProperties);
        }
        private TextParagraphProperties GetLineProperties(bool firstLine, bool showParagraphEllipsis, LineProperties lineProperties)
        {
            GetLineProperties();
            firstLine = firstLine && lineProperties.HasFirstLineProperties;
            if (!showParagraphEllipsis)
            {
                return firstLine ? lineProperties.FirstLineProps : lineProperties;
            }
            else
            {
                return lineProperties.GetParaEllipsisLineProps(firstLine);
            }
        }

        //-------------------------------------------------------------------
        // Calculate line advance distance. This functionality will go away
        // when TextFormatter will be able to handle line height/stacking.
        //
        //      lineHeight - calculated line height
        //
        // Returns: Line advance distance..
        //-------------------------------------------------------------------
        private double CalcLineAdvance(double lineHeight, LineProperties lineProperties)
        {
            return lineProperties.CalcLineAdvance(lineHeight);
        }

        //-------------------------------------------------------------------
        // Calculate offset of the content taking into account horizontal / vertical
        // content alignment.
        //
        // Returns: Content offset value.
        //-------------------------------------------------------------------
        private Vector CalcContentOffset(Size computedSize, double wrappingWidth)
        {
            Vector contentOffset = new Vector();

            Thickness padding = this.Padding;
            Size contentSize = new Size(Math.Max(0.0, computedSize.Width - (padding.Left + padding.Right)),
                                        Math.Max(0.0, computedSize.Height - (padding.Top + padding.Bottom)));

            switch (TextAlignment)
            {
                case TextAlignment.Right:
                    contentOffset.X = contentSize.Width - wrappingWidth;
                    break;

                case TextAlignment.Center:
                    contentOffset.X = (contentSize.Width - wrappingWidth) / 2;
                    break;

                // Default is Left alignment, in this case offset is 0.
            }

            contentOffset.X += padding.Left;
            contentOffset.Y += padding.Top;

            return contentOffset;
        }

        /// <summary>
        /// Returns true if paragraph ellipsis will be rendered on this line
        /// </summary>
        /// <param name="lineIndex">
        /// Index of the line
        /// </param>
        /// <param name="lineVOffset">
        /// Vertical offset at which line starts
        /// </param>
        private bool ParagraphEllipsisShownOnLine(int lineIndex, double lineVOffset)
        {
            if (lineIndex >= LineCount - 1)
            {
                // Last line. No paragraph ellipses
                return false;
            }

            // Find out if this is the last rendered line
            if (!CheckFlags(Flags.HasParagraphEllipses))
            {
                return false;
            }

            // Calculate bottom offset for next line
            double nextLineBottomOffset = GetLine(lineIndex + 1).Height + GetLine(lineIndex).Height + lineVOffset;
            // If the next line will exceed render height by a large margin, we cannot render
            // it at all and so we should show ellipsis on this one. However if the next line
            // almost fits, we will render it and so there should be no ellipsis
            double contentBottom = Math.Max(0.0, RenderSize.Height - Padding.Bottom);
            if (DoubleUtil.GreaterThan(nextLineBottomOffset, contentBottom) && !DoubleUtil.AreClose(nextLineBottomOffset, contentBottom))
            {
                return true;
            }

            return false;
        }

        //-------------------------------------------------------------------
        // Calculate wrapping width for lines.
        //
        // Returns: Wrapping width.
        //-------------------------------------------------------------------
        private double CalcWrappingWidth(double width)
        {
            // Reflowing will not happen when Width is between _previousDesiredSize.Width and ReferenceWidth.
            // In some cases _previousDesiredSize.Width > ReferenceSize, use ReferenceSize in those scenarios.
            if (width < _previousDesiredSize.Width)
            {
                width = _previousDesiredSize.Width;
            }
            if (width > _referenceSize.Width)
            {
                width = _referenceSize.Width;
            }

            bool usingReferenceWidth = DoubleUtil.AreClose(width, _referenceSize.Width);
            double paddingWidth = Padding.Left + Padding.Right;

            width = Math.Max(0.0, width - paddingWidth);

            // We want FormatLine to make the same decisions it made during Measure,
            // otherwise text can be truncated or trimmed when it shouldn't be.
            // The problem arises when the TextBox has no declared width, so its
            // render size is the same as its desired size.
            //
            // During Measure, LineServices computes the text width in "ideal"
            // coordinates (integer), TextLine converts this to "real" coordinates
            // (double-precision floating-point), and TextBlock then adds the
            // padding to yield the desired size.  At render time (or hit-test,
            // or others), this is reversed:  starting with the render size,
            // subtract the padding, then convert from "real" to "ideal".  We want
            // the result to be the same as the original text width, but there are
            // two ways this can fail:
            //   a) in display-mode, conversion from ideal to real involves rounding
            //      to a multiple of the pixel size.  This can cause the final
            //      width to fall short of the original by as much as half a pixel.
            //   b) (width - padding) + padding  might be different from width,
            //      due to floating-point arithmetic error.
            // In either case, if the final width is even slightly smaller than the
            // original text width, LineServices might think there is not enough
            // room to format the entire line.
            //
            // The following code protects against these errors by adjusting
            // the wrapping width upward by a slight amount.  But only if there
            // may have been some loss.

            // No adjustment is needed if we're starting with the same width
            // Measure was given, or if the width is zero
            if (!usingReferenceWidth && width != 0.0)
            {
                TextFormattingMode textFormattingMode = TextOptions.GetTextFormattingMode(this);
                if (textFormattingMode == TextFormattingMode.Display)
                {
                    // case a: rounding to pixel boundaries can lose up to half a pixel,
                    // as adjusted for the current DPI setting
                    width += 0.5 / (GetDpi().DpiScaleY);
                }

                if (paddingWidth != 0.0)
                {
                    // case b: if padding is involved, add a tiny amount to
                    // protect against roundoff error
                    width += 0.00000000001;
                }
            }

            // Make sure that TextFormatter limitations are not exceeded.
            TextDpi.EnsureValidLineWidth(ref width);

            return width;
        }

        // ------------------------------------------------------------------
        // Aborts calculation by throwing exception if world has changed
        // while in measure / arrange / render process.
        // ------------------------------------------------------------------
        private void VerifyTreeIsUnlocked()
        {
            if (CheckFlags(Flags.TreeInReadOnlyMode))
            {
                throw new InvalidOperationException(SR.Get(SRID.IllegalTreeChangeDetected));
            }
        }

        // ------------------------------------------------------------------
        // Decides if Text property needs to be serialized.
        // ------------------------------------------------------------------
        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeText()
        {
            // We have to allow Text property serialization (at least for simple content)
            // to preserve data-binding expressions. If we totally disable serialization
            // data binding expression will be lost.
            bool shouldSerialize = false;

            if (_complexContent == null)
            {
                object localValue = ReadLocalValue(TextProperty);

                if (localValue != null &&
                    localValue != DependencyProperty.UnsetValue &&
                    localValue as string != String.Empty)
                {
                    // if the value is not null AND is not an unset value AND is EITHER an
                    // Expression OR a non-empty string (the last condition covers both of these
                    // possibilities) then we should serialize the text.

                    shouldSerialize = true;
                }
            }

            return shouldSerialize;
        }

        // ------------------------------------------------------------------
        // Decides if Inlines property needs to be serialized.
        // ------------------------------------------------------------------
        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeInlines(XamlDesignerSerializationManager manager)
        {
            return (_complexContent != null) && (manager != null) && (manager.XmlWriter == null);
        }

        // ------------------------------------------------------------------
        // Do content alignment.
        // ------------------------------------------------------------------
        private void AlignContent()
        {
            Debug.Assert(IsLayoutDataValid);
            Debug.Assert(CheckFlags(Flags.RequiresAlignment));

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();

            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);

            // Create / format all lines.
            // Since we are disposing line object, it can be reused to format following lines.
            Line line = CreateLine(lineProperties);
            TextRunCache textRunCache = new TextRunCache();

            int dcp = 0;
            double lineOffset = 0;
            int lineCount = LineCount;
            for (int i = 0; i < lineCount; i++)
            {
Debug.Assert(lineCount == LineCount);
                LineMetrics lineMetrics = GetLine(i);

                using (line)
                {
                    bool ellipsis = ParagraphEllipsisShownOnLine(i, lineOffset);
                    line.Format(dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), lineMetrics.TextLineBreak, textRunCache, ellipsis);
                    double lineHeight = CalcLineAdvance(line.Height, lineProperties);

                    // Check consistency of line formatting
                    MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");
                    Debug.Assert(DoubleUtil.AreClose(lineHeight, lineMetrics.Height), "Line height is out of sync.");

                    // Calculated line width might be different from measure width in following cases:
                    // a) dynamically sized children, when FinalSize != AvailableSize
                    // b) non-default horizontal alignment, when FinalSize != AvailableSize
                    // Hence do not assert about matching line width with cached line metrics.

                    lineMetrics = UpdateLine(i, lineMetrics, line.Start, line.Width);
                    dcp += lineMetrics.Length;
                    lineOffset += lineHeight;
                }
            }
            SetFlags(false, Flags.RequiresAlignment);
        }

        // ------------------------------------------------------------------
        // OnRequestBringIntoView is called from the event handler TextBlock
        // registers for the event.
        // Handle the event for hosted ContentElements, and raise a new BringIntoView
        // event with the following values:
        // * object: (this)
        // * rect: A rect indicating the position of the ContentElement
        //
        //      sender - The instance handling the event.
        //      args   - RequestBringIntoViewEventArgs indicates the element
        //               and region to scroll into view.
        // ------------------------------------------------------------------
        private static void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs args)
        {
            TextBlock textBlock = sender as TextBlock;
            ContentElement child = args.TargetObject as ContentElement;

            if (textBlock != null && child != null)
            {
                if (TextBlock.ContainsContentElement(textBlock, child))
                {
                    // Handle original event.
                    args.Handled = true;

                    // Retrieve the first rectangle representing the child and
                    // raise a new BrightIntoView event with such rectangle.

                    ReadOnlyCollection<Rect> rects = textBlock.GetRectanglesCore(child);
                    Invariant.Assert(rects != null, "Rect collection cannot be null.");
                    if (rects.Count > 0)
                    {
                        textBlock.BringIntoView(rects[0]);
                    }
                    else
                    {
                        textBlock.BringIntoView();
                    }
                }
            }
        }

        private static bool ContainsContentElement(TextBlock textBlock, ContentElement element)
        {
            if (textBlock._complexContent == null || !(textBlock._complexContent.TextContainer is TextContainer))
            {
                return false;
            }
            else if (element is TextElement)
            {
                if (textBlock._complexContent.TextContainer != ((TextElement)element).TextContainer)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        private int LineCount
        {
            get
            {
                if (CheckFlags(Flags.HasFirstLine))
                {
                    return (_subsequentLines == null) ? 1 : _subsequentLines.Count + 1;
                }

                return 0;
            }
        }

        private LineMetrics GetLine(int index)
        {
            return (index == 0) ? _firstLine : _subsequentLines[index - 1];
        }

        private LineMetrics UpdateLine(int index, LineMetrics metrics, double start, double width)
        {
            metrics = new LineMetrics(metrics, start, width);

            if (index == 0)
            {
                _firstLine = metrics;
            }
            else
            {
                _subsequentLines[index - 1] = metrics;
            }

            return metrics;
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

        // ------------------------------------------------------------------
        // Ensures none of our public (or textview) methods can be called during measure/arrange/content change.
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

        /// <summary>
        /// Returns index of the line that starts at the given dcp. Returns -1 if
        /// no line or the line metrics collection starts at the given dcp
        /// </summary>
        /// <param name="dcpLine">
        /// Start dcp of required line
        /// </param>
        private int GetLineIndexFromDcp(int dcpLine)
        {
            Invariant.Assert(dcpLine >= 0);
            int lineIndex = 0;
            int lineStartOffset = 0;

            int lineCount = LineCount;
            while (lineIndex < lineCount)
            {
Debug.Assert(lineCount == LineCount);
                if (lineStartOffset == dcpLine)
                {
                    // Found line that starts at given dcp
                    return lineIndex;
                }
                else
                {
                    lineStartOffset += GetLine(lineIndex).Length;
                    ++lineIndex;
                }
            }

            // No line found starting at this position. Return -1.
            // We should never hit this code
            Invariant.Assert(false, "Dcp passed is not at start of any line in TextBlock");
            return -1;
        }

        // ------------------------------------------------------------------
        // IContentHost Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Searches for an element in the _complexContent.TextContainer. If the element is found, returns the
        /// position at which it is found. Otherwise returns null.
        /// </summary>
        /// <param name="e">
        /// Element to be found.
        /// </param>
        /// <remarks>
        /// We assume that this function is called from within text if the caller knows that _complexContent exists
        /// and contains a TextContainer. Hence we assert for this condition within the function
        /// </remarks>
        private TextPointer FindElementPosition(IInputElement e)
        {
            // Parameter validation
            Debug.Assert(e != null);

            // Validate that this function is only called when a TextContainer exists as complex content
            Debug.Assert(_complexContent.TextContainer is TextContainer);

            TextPointer position;

            // If e is a TextElement we can optimize by checking its TextContainer
            if (e is TextElement)
            {
                if ((e as TextElement).TextContainer == _complexContent.TextContainer)
                {
                    // Element found
                    position = new TextPointer((e as TextElement).ElementStart);
                    return position;
                }
            }

            // Else: search for e in the complex content
            position = new TextPointer((TextPointer)_complexContent.TextContainer.Start);
            while (position.CompareTo((TextPointer)_complexContent.TextContainer.End) < 0)
            {
                // Search each position in _complexContent.TextContainer for the element
                switch (position.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.EmbeddedElement:
                        DependencyObject embeddedObject = position.GetAdjacentElement(LogicalDirection.Forward);
                        if (embeddedObject is ContentElement || embeddedObject is UIElement)
                        {
                            if (embeddedObject == e as ContentElement || embeddedObject == e as UIElement)
                            {
                                return position;
                            }
                        }
                        break;
                    default:
                          break;
                }
                position.MoveByOffset(+1);
            }

            // Reached end of complex content without finding the element
            return null;
        }

        /// <summary>
        /// Called when the child's BaselineOffset value changes.
        /// </summary>
        internal void OnChildBaselineOffsetChanged(DependencyObject source)
        {
            // Ignore this notification, if currently in the measure process.
            if (!CheckFlags(Flags.MeasureInProgress))
            {
                // BaselineOffset,  may affect the
                // size. Hence invalidate measure.
                // There is no need to invalidate TextRunCache, since TextFormatter
                // regets inline object information even if TextRunCache is clean.
                InvalidateMeasure();
                InvalidateVisual(); //ensure re-rendering
            }
        }


        /// <summary>
        /// Property invalidator for baseline offset
        /// </summary>
        /// <param name="d">Dependency Object that the property value is being changed on.</param>
        /// <param name="e">EventArgs that contains the old and new values for this property</param>
        private static void OnBaselineOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Set up our baseline changed event

            //fire event!
            TextElement te = TextElement.ContainerTextElementField.GetValue(d);

            if (te != null)
            {
                DependencyObject parent = te.TextContainer.Parent;
                TextBlock tb = parent as TextBlock;
                if (tb != null)
                {
                    tb.OnChildBaselineOffsetChanged(d);
                }
                else
                {
                    FlowDocument fd = parent as FlowDocument;
                    if (fd != null && d is UIElement)
                    {
                        fd.OnChildDesiredSizeChanged((UIElement)d);
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Setup event handlers.
        // Deferred until the first measure.
        // ------------------------------------------------------------------
        private void InitializeTextContainerListeners()
        {
            _complexContent.TextContainer.Changing += new EventHandler(OnTextContainerChanging);
            _complexContent.TextContainer.Change += new TextContainerChangeEventHandler(OnTextContainerChange);
            _complexContent.Highlights.Changed += new HighlightChangedEventHandler(OnHighlightChanged);
        }

        // ------------------------------------------------------------------
        // Clears out line metrics array, disposes as appropriate
        // ------------------------------------------------------------------
        private void ClearLineMetrics()
        {
            if (CheckFlags(Flags.HasFirstLine))
            {
                if (_subsequentLines != null)
                {
                    int subsequentLineCount = _subsequentLines.Count;
                    for (int i = 0; i < subsequentLineCount; i++)
                    {
                        _subsequentLines[i].Dispose(false);
                    }

                    _subsequentLines = null;
                }

                _firstLine = _firstLine.Dispose(true);
                SetFlags(false, Flags.HasFirstLine);
            }
        }

        // ------------------------------------------------------------------
        // Ensures the hyphenator object is created
        // ------------------------------------------------------------------
        private NaturalLanguageHyphenator EnsureHyphenator()
        {
            if (CheckFlags(Flags.IsHyphenatorSet))
            {
                return HyphenatorField.GetValue(this);
            }

            // initialize hyphenator
            NaturalLanguageHyphenator hyphenator = new NaturalLanguageHyphenator();

            HyphenatorField.SetValue(this, hyphenator);
            SetFlags(true, Flags.IsHyphenatorSet);
            return hyphenator;
        }

        private static bool IsValidTextTrimming(object o)
        {
            TextTrimming value = (TextTrimming)o;
            return value == TextTrimming.CharacterEllipsis
                || value == TextTrimming.None
                || value == TextTrimming.WordEllipsis;
        }

        private static bool IsValidTextWrap(object o)
        {
            TextWrapping value = (TextWrapping)o;
            return value == TextWrapping.Wrap
                || value == TextWrapping.NoWrap
                || value == TextWrapping.WrapWithOverflow;
        }

        #endregion Private methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        //------------------------------------------------------------------
        // Simple content.
        //-------------------------------------------------------------------
        private TextBlockCache _textBlockCache;

        //------------------------------------------------------------------
        // Simple content.
        //-------------------------------------------------------------------
        private string _contentCache;

        //-------------------------------------------------------------------
        // Complex content.
        //-------------------------------------------------------------------
        private ComplexContent _complexContent;

        //-------------------------------------------------------------------
        // Text formatter object.
        //-------------------------------------------------------------------
        private TextFormatter _textFormatterIdeal;

        //-------------------------------------------------------------------
        // Text formatter object.
        //-------------------------------------------------------------------
        private TextFormatter _textFormatterDisplay;

        //-------------------------------------------------------------------
        // This size was the most recent constraint passed to MeasureOverride.
        // this.PreviousConstraintcan not be used because it can be affected
        // by Margin, Width/Min/MaxWidth propreties and ClipToBounds...
        //-------------------------------------------------------------------
        private Size _referenceSize;

        //-------------------------------------------------------------------
        // This size was a result returned by MeasureOverride last time.
        // this.DesiredSize can not be used because it can be affected by Margin,
        // Width/Min/MaxWidth propreties and ClipToBounds...
        //-------------------------------------------------------------------
        private Size _previousDesiredSize;

        //-------------------------------------------------------------------
        // Distance from the top of the Element to its baseline.
        //-------------------------------------------------------------------
        private double _baselineOffset;

        //-------------------------------------------------------------------
        // Hyphenator -- uncommon field given usage
        //-------------------------------------------------------------------
        private  static readonly UncommonField<NaturalLanguageHyphenator> HyphenatorField = new UncommonField<NaturalLanguageHyphenator>();

        //-------------------------------------------------------------------
        // Collection of metrics of each line. For performance reasons, we
        // have separated out the first line from the array, since a single
        // line of text is a very common usage.  The LineCount, GetLine,
        // and UpdateLine members are used to simplify access to this
        // divided data structure.
        //-------------------------------------------------------------------
        private LineMetrics _firstLine;
        private List<LineMetrics> _subsequentLines;

        //-------------------------------------------------------------------
        // Flags reflecting various aspects of TextBlock's state.
        //-------------------------------------------------------------------
        private Flags _flags;
        [System.Flags]
        private enum Flags
        {
            FormattedOnce           = 0x1,      // Element has been formatted at least once.
            MeasureInProgress       = 0x2,      // Measure is in progress.
            TreeInReadOnlyMode      = 0x4,      // Tree (content) is in read only mode.
            RequiresAlignment       = 0x8,      // Content requires alignment process.
            ContentChangeInProgress = 0x10,     // Content change is in progress
                                                //(it has been started, but is is not completed yet).
            IsContentPresenterContainer = 0x20, // Is this Text control being used by a ContentPresenter to host its content
            HasParagraphEllipses    = 0x40,     // Has paragraph ellipses
            PendingTextContainerEventInit = 0x80, // Needs TextContainer event hookup on next Measure call.
            ArrangeInProgress       = 0x100,      // Arrange is in progress.
            IsTypographySet         = 0x200,      // Typography properties are not at default values
            TextContentChanging     = 0x400,    // TextProperty update in progress.
            IsHyphenatorSet         = 0x800,   // used to indicate when HyphenatorField has been set
            HasFirstLine            = 0x1000,
        }

        #endregion Private Fields
        //-------------------------------------------------------------------
        //
        //  Private Types
        //
        //-------------------------------------------------------------------

        #region Private Types

        //-------------------------------------------------------------------
        // Represents complex content.
        //-------------------------------------------------------------------
        private class ComplexContent
        {
            //---------------------------------------------------------------
            // Ctor
            //---------------------------------------------------------------
            internal ComplexContent(TextBlock owner, ITextContainer textContainer, bool foreignTextContianer, string content)
            {
                // Paramaters validation
                Debug.Assert(owner != null);
                Debug.Assert(textContainer != null);

                VisualChildren = new VisualCollection(owner);

                // Store TextContainer that contains content of the element.
                TextContainer = textContainer;
                ForeignTextContainer = foreignTextContianer;

                // Add content
                if (content != null && content.Length > 0)
                {
                    TextBlock.InsertTextRun(this.TextContainer.End, content, /*whitespacesIgnorable:*/false);
                }

                // Create TextView associated with TextContainer.
                this.TextView = new TextParagraphView(owner, TextContainer);

                // Hookup TextContainer to TextView.
                this.TextContainer.TextView = this.TextView;
            }

            //---------------------------------------------------------------
            // Detach event handlers.
            //---------------------------------------------------------------
            internal void Detach(TextBlock owner)
            {
                this.Highlights.Changed -= new HighlightChangedEventHandler(owner.OnHighlightChanged);
                this.TextContainer.Changing -= new EventHandler(owner.OnTextContainerChanging);
                this.TextContainer.Change -= new TextContainerChangeEventHandler(owner.OnTextContainerChange);
            }

            //------------------------------------------------------------------
            // Internal Visual Children.
            //-------------------------------------------------------------------
            internal VisualCollection VisualChildren;

            //---------------------------------------------------------------
            // Highlights associated with TextContainer.
            //---------------------------------------------------------------
            internal Highlights Highlights { get { return this.TextContainer.Highlights; } }

            //---------------------------------------------------------------
            // Text array exposing access to the content.
            //---------------------------------------------------------------
            internal readonly ITextContainer TextContainer;

            //---------------------------------------------------------------
            // Is TextContainer owned by another object?
            //---------------------------------------------------------------
            internal readonly bool ForeignTextContainer;

            //---------------------------------------------------------------
            // TextView object associated with TextContainer.
            //---------------------------------------------------------------
            internal readonly TextParagraphView TextView;

            //---------------------------------------------------------------
            // Collection of inline objects hosted by the TextBlock control.
            //---------------------------------------------------------------
            internal ArrayList InlineObjects;
        }

        //-------------------------------------------------------------------
        // Simple content enumerator.
        //-------------------------------------------------------------------
        private class SimpleContentEnumerator : IEnumerator
        {
            //---------------------------------------------------------------
            // Ctor
            //---------------------------------------------------------------
            internal SimpleContentEnumerator(string content)
            {
                _content = content;
                _initialized = false;
                _invalidPosition = false;
            }

            //---------------------------------------------------------------
            // Sets the enumerator to its initial position, which is before
            // the first element in the collection.
            //---------------------------------------------------------------
            void IEnumerator.Reset()
            {
                _initialized = false;
                _invalidPosition = false;
            }

            //---------------------------------------------------------------
            // Advances the enumerator to the next element of the collection.
            //---------------------------------------------------------------
            bool IEnumerator.MoveNext()
            {
                if (!_initialized)
                {
                    _initialized = true;
                    return true;
                }
                else
                {
                    _invalidPosition = true;
                    return false;
                }
            }

            //---------------------------------------------------------------
            // Gets the current element in the collection.
            //---------------------------------------------------------------
            object IEnumerator.Current
            {
                get
                {
                    if (!_initialized || _invalidPosition)
                    {
                        throw new InvalidOperationException();
                    }
                    return _content;
                }
            }

            //---------------------------------------------------------------
            // Content.
            //---------------------------------------------------------------
            private readonly string _content;
            private bool _initialized;
            private bool _invalidPosition;
        }

        #endregion Private Types

        //-------------------------------------------------------------------
        //
        //  Dependency Property Helpers
        //
        //-------------------------------------------------------------------

        #region Dependency Property Helpers

        private static object CoerceBaselineOffset(DependencyObject d, object value)
        {
            TextBlock tb = (TextBlock) d;

            if(DoubleUtil.IsNaN((double) value))
            {
                return tb._baselineOffset;
            }

            return value;
        }

        /// <summary>
        /// Returns true if we should serialize the BaselineOffset property.
        /// </summary>
        /// <returns>True if we should serialize the BaselineOffset property.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeBaselineOffset()
        {
            object localBaseline = ReadLocalValue(BaselineOffsetProperty);
            return (localBaseline != DependencyProperty.UnsetValue) && !DoubleUtil.IsNaN((double) localBaseline);
        }

        //-------------------------------------------------------------------
        // Text helpers
        //-------------------------------------------------------------------

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnTextChanged(d, (string)e.NewValue);
        }

        private static void OnTextChanged(DependencyObject d, string newText)
        {
            TextBlock text = (TextBlock) d;

            if (text.CheckFlags(Flags.TextContentChanging))
            {
                // The update originated in a TextContainer change -- don't update
                // the TextContainer a second time.
                return;
            }

            if (text._complexContent == null)
            {
                text._contentCache = (newText != null) ? newText : String.Empty;
            }
            else
            {
                text.SetFlags(true, Flags.TextContentChanging);
                try
                {
                    bool exceptionThrown = true;

                    Invariant.Assert(text._contentCache == null, "Content cache should be null when complex content exists.");

                    text._complexContent.TextContainer.BeginChange();
                    try
                    {
                        ((TextContainer)text._complexContent.TextContainer).DeleteContentInternal((TextPointer)text._complexContent.TextContainer.Start, (TextPointer)text._complexContent.TextContainer.End);
                        InsertTextRun(text._complexContent.TextContainer.End, newText, /*whitespacesIgnorable:*/true);
                        exceptionThrown = false;
                    }
                    finally
                    {
                        text._complexContent.TextContainer.EndChange();

                        if (exceptionThrown)
                        {
                            text.ClearLineMetrics();
                        }
                    }
                }
                finally
                {
                    text.SetFlags(false, Flags.TextContentChanging);
                }
            }
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 28; }
        }

        #endregion Dependency Property Helpers
    }
}


// Disable pragma warnings to enable PREsharp pragmas
#pragma warning restore 1634, 1691



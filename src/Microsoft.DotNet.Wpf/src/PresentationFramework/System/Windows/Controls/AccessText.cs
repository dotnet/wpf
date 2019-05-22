// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;
using System.Diagnostics;
using System.Xml;

using MS.Internal;

namespace System.Windows.Controls
{
    /// <summary>
    /// AccessText Element - Register an accesskey specified after the underscore character in the text.
    /// </summary>
    [ContentProperty("Text")]
    public class AccessText : FrameworkElement, IAddChild
    {
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
            ((IAddChild)TextBlock).AddChild(value);
        }

        ///<summary>
        /// Called when text appears under the tag in markup.
        ///</summary>
        ///<param name="text">
        /// Text to Add to the Object
        ///</param> 
        void IAddChild.AddText(string text)
        {
            ((IAddChild)TextBlock).AddText(text);
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
                return new RangeContentEnumerator(TextContainer.Start, TextContainer.End);
            }
        }

        #endregion LogicalTree

        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// AccessText constructor.
        /// </summary>
        public AccessText() : base()
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     Read only access to the key after the first underline character
        /// </summary>
        public char AccessKey
        {
            get
            {
                // future note: AccessKey should be string type to support char pairs like surrogate symbols and marked letters (two unicode chars)
                return (_accessKey != null && _accessKey.Text.Length > 0) ? _accessKey.Text[0] : (char)0;
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
        /// DependencyProperty for <see cref="Text" /> property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = 
                DependencyProperty.Register(
                        "Text", 
                        typeof(string), 
                        typeof(AccessText),
                        new FrameworkPropertyMetadata(
                                string.Empty, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, 
                                new PropertyChangedCallback(OnTextChanged)));

        /// <summary>
        /// The Text property defines the text to be displayed.
        /// </summary>
        [DefaultValue("")]
        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for <see cref="FontFamily" /> property.
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty = 
                TextElement.FontFamilyProperty.AddOwner(typeof(AccessText));

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
        /// DependencyProperty for <see cref="FontStyle" /> property.
        /// </summary>
        public static readonly DependencyProperty FontStyleProperty = 
                TextElement.FontStyleProperty.AddOwner(typeof(AccessText));

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
                TextElement.FontWeightProperty.AddOwner(typeof(AccessText));

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
                TextElement.FontStretchProperty.AddOwner(typeof(AccessText));

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
                TextElement.FontSizeProperty.AddOwner(typeof(AccessText));

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
                TextElement.ForegroundProperty.AddOwner(typeof(AccessText));

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
                        typeof(AccessText),
                        new FrameworkPropertyMetadata(
                                null,
                                FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPropertyChanged)));

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
        public static readonly DependencyProperty TextDecorationsProperty = 
                Inline.TextDecorationsProperty.AddOwner(
                        typeof(AccessText),
                        new FrameworkPropertyMetadata(
                                new FreezableDefaultValueFactory(TextDecorationCollection.Empty), 
                                FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPropertyChanged)));

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
                        typeof(AccessText),
                        new FrameworkPropertyMetadata(
                                new FreezableDefaultValueFactory(TextEffectCollection.Empty), 
                                FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPropertyChanged)));

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
                Block.LineHeightProperty.AddOwner(typeof(AccessText));
        
        /// <summary>
        /// The LineHeight property specifies the height of each generated line box.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double LineHeight
        {
            get { return (double) GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="LineStackingStrategy" /> property.
        /// </summary>
        public static readonly DependencyProperty LineStackingStrategyProperty =
                Block.LineStackingStrategyProperty.AddOwner(typeof(AccessText));

        /// <summary>
        /// The LineStackingStrategy property specifies how lines are placed
        /// </summary>
        public LineStackingStrategy LineStackingStrategy
        {
            get { return (LineStackingStrategy)GetValue(LineStackingStrategyProperty); }
            set { SetValue(LineStackingStrategyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextAlignment" /> property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty = 
                Block.TextAlignmentProperty.AddOwner(typeof(AccessText));

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
                        typeof(AccessText),
                        new FrameworkPropertyMetadata(
                                TextTrimming.None,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPropertyChanged)));

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
                        typeof(AccessText),
                        new FrameworkPropertyMetadata(
                                TextWrapping.NoWrap,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPropertyChanged)));

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
        /// DependencyProperty for <see cref="BaselineOffset" /> property.
        /// </summary>
        public static readonly DependencyProperty BaselineOffsetProperty =
                TextBlock.BaselineOffsetProperty.AddOwner(typeof(AccessText), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnPropertyChanged)));

        /// <summary>
        /// The BaselineOffset property provides an adjustment to baseline offset
        /// </summary>
        public double BaselineOffset
        {
            get { return (double) GetValue(BaselineOffsetProperty); }
            set { SetValue(BaselineOffsetProperty, value); }
        }

        #endregion Public Dynamic Properties

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
            TextBlock.Measure(constraint);
            return TextBlock.DesiredSize;
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="arrangeSize">Size that element should use to arrange itself and its children.</param>
        protected sealed override Size ArrangeOverride(Size arrangeSize)
        {
            TextBlock.Arrange(new Rect(arrangeSize));
            return arrangeSize;
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        internal static bool HasCustomSerialization(object o)
        {
            Run accessKey = o as Run;
            return accessKey != null && HasCustomSerializationStorage.GetValue(accessKey);
        }

        #endregion Internal methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        internal TextBlock TextBlock
        {
            get
            {
                if (_textBlock == null)
                    CreateTextBlock();
                return _textBlock;
            }
        }

      
        internal static char AccessKeyMarker
        {
            get { return _accessKeyMarker; }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private TextContainer TextContainer
        {
            get
            {
                if (_textContainer == null)
                    CreateTextBlock();
                return _textContainer;
            }
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AccessText)d).TextBlock.SetValue(e.Property, e.NewValue);
        }

        /// <summary>
        /// CreateTextBlock - Creates a text block, adds as visual child, databinds properties and sets up appropriate event listener.
        /// </summary>
        private void CreateTextBlock()
        {
            _textContainer = new TextContainer(this, false /* plainTextOnly */);
            _textBlock = new TextBlock();
            AddVisualChild(_textBlock);
            _textBlock.IsContentPresenterContainer = true;
            _textBlock.SetTextContainer(_textContainer);
            InitializeTextContainerListener();
        }

        /// <summary>
        /// Gets the Visual children count of the AccessText control.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        /// <summary>
        /// Gets the Visual child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange)); 
            }
            return TextBlock;
        }

        // Provides custom serialization for this element
        internal static void SerializeCustom(XmlWriter xmlWriter, object o)
        {
            Run inlineScope = o as Run;
            if (inlineScope != null)
            {
                xmlWriter.WriteString(AccessKeyMarker + inlineScope.Text);
            }
        }

        private static Style AccessKeyStyle
        {
            get
            {
                if (_accessKeyStyle == null)
                {
                    Style accessKeyStyle = new Style(typeof(Run));
                    Trigger trigger = new Trigger();
                    trigger.Property = KeyboardNavigation.ShowKeyboardCuesProperty;
                    trigger.Value = true;
                    trigger.Setters.Add(new Setter(TextDecorationsProperty, System.Windows.TextDecorations.Underline));
                    accessKeyStyle.Triggers.Add(trigger);
                    accessKeyStyle.Seal();
                    _accessKeyStyle = accessKeyStyle;
                }
                return _accessKeyStyle;
            }
        }

        /// <summary>
        /// UpdateAccessKey - Scans forward in the tree looking for the access key marker, replacing it with access key element. We only support one find.
        /// </summary>
        private void UpdateAccessKey()
        {
            TextPointer navigator = new TextPointer(TextContainer.Start);

            while (!_accessKeyLocated && navigator.CompareTo(TextContainer.End) < 0 )
            {
                TextPointerContext symbolType = navigator.GetPointerContext(LogicalDirection.Forward);
                switch (symbolType)
                {
                    case TextPointerContext.Text:
                        string text = navigator.GetTextInRun(LogicalDirection.Forward);
                        int index = FindAccessKeyMarker(text);
                        if(index != -1 && index < text.Length - 1)
                        {
                            string keyText = StringInfo.GetNextTextElement(text, index + 1);
                            TextPointer keyEnd = navigator.GetPositionAtOffset(index + 1 + keyText.Length);

                            _accessKey = new Run(keyText);
                            _accessKey.Style = AccessKeyStyle;

                            RegisterAccessKey();

                            HasCustomSerializationStorage.SetValue(_accessKey, true);
                            _accessKeyLocated = true;

                            UninitializeTextContainerListener();

                            TextContainer.BeginChange();
                            try
                            {
                                TextPointer underlineStart = new TextPointer(navigator, index);
                                TextRangeEdit.DeleteInlineContent(underlineStart, keyEnd);
                                _accessKey.RepositionWithContent(underlineStart);
                            }
                            finally
                            {
                                TextContainer.EndChange();
                                InitializeTextContainerListener();
                            }
                        }

                        break;
                }
                navigator.MoveToNextContextPosition(LogicalDirection.Forward);
            }

            // Convert double _ to single _
            navigator = new TextPointer(TextContainer.Start);
            string accessKeyMarker = AccessKeyMarker.ToString();
            string doubleAccessKeyMarker = accessKeyMarker + accessKeyMarker;
            while (navigator.CompareTo(TextContainer.End) < 0)
            {
                TextPointerContext symbolType = navigator.GetPointerContext(LogicalDirection.Forward);
                switch (symbolType)
                {
                    case TextPointerContext.Text:
                        string text = navigator.GetTextInRun(LogicalDirection.Forward);
                        string nexText = text.Replace(doubleAccessKeyMarker, accessKeyMarker);
                        if (text != nexText)
                        {
                            TextPointer keyStart = new TextPointer(navigator, 0);
                            TextPointer keyEnd = new TextPointer(navigator, text.Length);

                            UninitializeTextContainerListener();
                            TextContainer.BeginChange();
                            try
                            {
                                keyEnd.InsertTextInRun(nexText);
                                TextRangeEdit.DeleteInlineContent(keyStart, keyEnd);
                            }
                            finally
                            {
                                TextContainer.EndChange();
                                InitializeTextContainerListener();
                            }
                        }

                        break;
                }
                navigator.MoveToNextContextPosition(LogicalDirection.Forward);
            }
        }

        // Returns the index of _ marker.
        // _ can be escaped by double _
        private static int FindAccessKeyMarker(string text)
        {
            int lenght = text.Length;
            int startIndex = 0;
            while (startIndex < lenght)
            {
                int index = text.IndexOf(AccessKeyMarker, startIndex);
                if (index == -1)
                    return -1;
                // If next char exist and different from _
                if (index + 1 < lenght && text[index + 1] != AccessKeyMarker)
                    return index;
                startIndex = index + 2;
            }

            return -1;
        }

        internal static string RemoveAccessKeyMarker(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                string accessKeyMarker = AccessKeyMarker.ToString();
                string doubleAccessKeyMarker = accessKeyMarker + accessKeyMarker; 
                int index = FindAccessKeyMarker(text);
                if (index >=0 && index < text.Length - 1)
                    text = text.Remove(index, 1);
                // Replace double _ with single _
                text = text.Replace(doubleAccessKeyMarker, accessKeyMarker);
            }
            return text;
        }

        private void RegisterAccessKey()
        {
            if (_currentlyRegistered != null)
            {
                AccessKeyManager.Unregister(_currentlyRegistered, this);
                _currentlyRegistered = null;
            }
            string key = _accessKey.Text;
            if (!string.IsNullOrEmpty(key))
            {
                AccessKeyManager.Register(key, this);
                _currentlyRegistered = key;
            }
        }

        //-------------------------------------------------------------------
        // Text helpers
        //-------------------------------------------------------------------
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AccessText) d).UpdateText((string) e.NewValue);
        }

        private void UpdateText(string text)
        {
            if (text == null)
                text = string.Empty;

            _accessKeyLocated = false;
            _accessKey = null;
            TextContainer.BeginChange();
            try
            {
                TextContainer.DeleteContentInternal((TextPointer)TextContainer.Start, TextContainer.End);
                Run run = Inline.CreateImplicitRun(this);
                ((TextPointer)TextContainer.End).InsertTextElement(run);
                run.Text = text;
            }
            finally
            {
                TextContainer.EndChange();
            }
        }

        // ------------------------------------------------------------------
        // Setup event handler.
        // ------------------------------------------------------------------
        private void InitializeTextContainerListener()
        {
            TextContainer.Changed += new TextContainerChangedEventHandler(OnTextContainerChanged);
        }

        // ------------------------------------------------------------------
        // Clear event handler.
        // ------------------------------------------------------------------
        private void UninitializeTextContainerListener()
        {
            TextContainer.Changed -= new TextContainerChangedEventHandler(OnTextContainerChanged);
        }

        // ------------------------------------------------------------------
        // Handler for TextContainer.Changed notification.
        // ------------------------------------------------------------------
        private void OnTextContainerChanged(object sender, TextContainerChangedEventArgs args)
        {
            // Skip changes that only affect properties.
            if (args.HasContentAddedOrRemoved)
            {
                UpdateAccessKey();
            }
        }

        #endregion Private methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        //---------------------------------------------------------------
        // Text container for actual content
        //---------------------------------------------------------------
        private TextContainer _textContainer;

        //---------------------------------------------------------------
        // Visual tree text block
        //---------------------------------------------------------------
        private TextBlock _textBlock;

        //---------------------------------------------------------------
        // Visual tree Run - created internally if _ is found
        //---------------------------------------------------------------
        private Run _accessKey;

        //---------------------------------------------------------------
        // Flag indicating whether the access key element has been located
        //---------------------------------------------------------------
        private bool _accessKeyLocated;

        //---------------------------------------------------------------
        // Defines the charecter to be used in fron of the access key
        //---------------------------------------------------------------
        private const char _accessKeyMarker = '_';

        //---------------------------------------------------------------
        // Stores the default Style applied on the internal Run
        //---------------------------------------------------------------
        private static Style _accessKeyStyle;

        //---------------------------------------------------------------
        // Flag that indicates if access key is registered with this AccessText
        //---------------------------------------------------------------
        private string _currentlyRegistered;

        //---------------------------------------------------------------
        // Flag that indicates that internal Run should have a custom serialization
        //---------------------------------------------------------------
        private static readonly UncommonField<bool> HasCustomSerializationStorage = new UncommonField<bool>();

        #endregion Private Fields
    }
}




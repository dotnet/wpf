// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: The stock plain text editing control.
//

namespace System.Windows.Controls
{
    using MS.Internal;
    using System.Threading;
    using System.Collections; // IEnumerator
    using System.ComponentModel; // DefaultValue
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Data; // Binding
    using System.Windows.Documents;
    using System.Windows.Automation.Peers;
    using System.Windows.Input; // CanExecuteRoutedEventArgs, ExecuteRoutedEventArgs

    using System.Windows.Controls.Primitives; // TextBoxBase
    using System.Windows.Navigation;
    using System.Windows.Markup; // IAddChild, XamlDesignerSerializer, ContentPropertyAttribute
    using MS.Utility;
    using MS.Internal.Text;
    using MS.Internal.Automation;   // TextAdaptor
    using MS.Internal.Documents;    // Undo
    using MS.Internal.Commands;     // CommandHelpers
    using MS.Internal.Telemetry.PresentationFramework;

    /// <summary>
    /// The stock text editing control.
    /// </summary>
    [Localizability(LocalizationCategory.Text)]
    [ContentProperty("Text")]
    public class TextBox : TextBoxBase, IAddChild, ITextBoxViewHost
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static constructor for TextBox.
        /// </summary>
        static TextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(typeof(TextBox)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(TextBox));

            // Add handlers for height properties so we can manage min/maxLines
            PropertyChangedCallback callback = new PropertyChangedCallback(OnMinMaxChanged);

            HeightProperty.OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(callback));
            MinHeightProperty.OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(callback));
            MaxHeightProperty.OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(callback));
            FontFamilyProperty.OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(callback));
            FontSizeProperty.OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(callback));

            // Registering typography properties metadata
            PropertyChangedCallback onTypographyChanged = new PropertyChangedCallback(OnTypographyChanged);
            DependencyProperty[] typographyProperties = Typography.TypographyPropertiesList;
            for (int i = 0; i < typographyProperties.Length; i++)
            {
                typographyProperties[i].OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(onTypographyChanged));
            }

            HorizontalScrollBarVisibilityProperty.OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(
             ScrollBarVisibility.Hidden,
             new PropertyChangedCallback(OnScrollViewerPropertyChanged), // PropertyChangedCallback
             new CoerceValueCallback(CoerceHorizontalScrollBarVisibility)));

            ControlsTraceLogger.AddControl(TelemetryControls.TextBox);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public TextBox() : base()
        {
            // Register static editing command handlers.
            // This only has an effect that first time we make the call.
            // We don't use the static ctor because there are cases
            // where another control will want to alias our properties
            // but doesn't need this overhead.
            TextEditor.RegisterCommandHandlers(typeof(TextBox), /*acceptsRichContent:*/false, /*readOnly*/false, /*registerEventListeners*/false);

            // Create TextContainer and TextEditor associated with it
            TextContainer container = new TextContainer(this, true /* plainTextOnly */);
            container.CollectTextChanges = true;
            InitializeTextContainer(container);

            // TextBox only accepts plain text, so change TextEditor's default to that.
            this.TextEditor.AcceptsRichContent = false;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        // -----------------------------------------------------------
        //
        // IAddChild interface
        //
        // -----------------------------------------------------------

        ///<summary>
        /// Called to Add the object as a Child.
        ///</summary>
        ///<param name="value">
        /// Object to add as a child
        ///</param>
        ///<remarks>
        /// This method will always throw InvalidOperationException because
        /// the TextBox only accepts plain text.
        ///</remarks>
        void IAddChild.AddChild(Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            // TextBox only accepts plain text, via IAddChild.AddText.
            throw new InvalidOperationException(SR.Get(SRID.TextBoxInvalidChild, value.ToString()));
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

            this.TextContainer.End.InsertTextInRun(text);
        }

        /// <summary>
        /// Select the text in the given position and length.
        /// </summary>
        public void Select(int start, int length)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException("start", SR.Get(SRID.ParameterCannotBeNegative));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", SR.Get(SRID.ParameterCannotBeNegative));
            }

            // Identify new position for selection Start
            int maxStart = TextContainer.SymbolCount;
            if (start > maxStart)
            {
                start = maxStart;
            }
            TextPointer newStart = this.TextContainer.CreatePointerAtOffset(start, LogicalDirection.Forward);

            // Normalize new start in some particular direction, to exclude ambiguity on surrogates bounndaries
            // and to start counting length from appropriate position.
            newStart = newStart.GetInsertionPosition(LogicalDirection.Forward);

            // Identify new position for selection End
            int maxLength = newStart.GetOffsetToPosition(TextContainer.End);
            if (length > maxLength)
            {
                length = maxLength;
            }
            TextPointer newEnd = new TextPointer(newStart, length, LogicalDirection.Forward);

            // Normalize end in some particular direction to exclude ambiguity on surrogate boundaries
            newEnd = newEnd.GetInsertionPosition(LogicalDirection.Forward);

            // Set new selection
            TextSelectionInternal.Select(newStart, newEnd);
        }

        /// <summary>
        /// Clear all the content in the TextBox control.
        /// </summary>
        public void Clear()
        {
            using (this.TextSelectionInternal.DeclareChangeBlock())
            {
                this.TextContainer.DeleteContentInternal((TextPointer)this.TextContainer.Start, (TextPointer)this.TextContainer.End);
                TextSelectionInternal.Select(this.TextContainer.Start, this.TextContainer.Start);
            }
        }

        /// <summary>
        /// Return the 0-based character index of the given point.  If there is no character
        /// at that point and snapToText is false, return -1.
        /// </summary>
        /// <param name="point">Point in TextBox coordinate space</param>
        /// <param name="snapToText">if true and there is no character at the given point, will return the nearest character</param>
        /// <returns>Character index at the given point, or -1</returns>
        public int GetCharacterIndexFromPoint(Point point, bool snapToText)
        {
            if (this.RenderScope == null)
            {
                return -1;
            }

            TextPointer textPointer = GetTextPositionFromPointInternal(point, snapToText);

            if (textPointer != null)
            {
                // offset corresponds to insertion position
                int offset = textPointer.Offset;

                // return character index based on orientation of TextPointer
                return (textPointer.LogicalDirection == LogicalDirection.Backward) ? offset - 1 : offset;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Return the 0-based character index of the first character of lineIndex.
        /// </summary>
        /// <param name="lineIndex">0-based index of the line for which we want the first character index</param>
        /// <returns>0-based index of the first character of lineIndex, or -1 if no layout information is available.</returns>
        public int GetCharacterIndexFromLineIndex(int lineIndex)
        {
            if (this.RenderScope == null)
            {
                return -1;
            }

            if (lineIndex < 0 || lineIndex >= LineCount)
            {
                throw new ArgumentOutOfRangeException("lineIndex");
            }

            TextPointer textPointer = GetStartPositionOfLine(lineIndex);

            // textPointer will be null if there is no layout available.
            return (textPointer == null) ? 0 : textPointer.Offset;
        }

        /// <summary>
        /// Return the 0-based index of the line containing the given character index.
        /// </summary>
        /// <param name="charIndex">index of the character for which a line index is to be returned</param>
        /// <returns>
        /// 0-based index of the line containing the character at charIndex, or -1 if no
        /// layout information is available
        /// </returns>
        public int GetLineIndexFromCharacterIndex(int charIndex)
        {
            if (this.RenderScope == null)
            {
                return -1;
            }

            if (charIndex < 0 || charIndex > this.TextContainer.SymbolCount)
            {
                throw new ArgumentOutOfRangeException("charIndex");
            }

            int line;
            TextPointer position = this.TextContainer.CreatePointerAtOffset(charIndex, LogicalDirection.Forward);

            if (position.ValidateLayout())
            {
                TextBoxView textboxView = (TextBoxView)this.RenderScope;
                line = textboxView.GetLineIndexFromOffset(charIndex);
            }
            else
            {
                line = -1;
            }

            return line;
        }

        /// <summary>
        /// Return the number of characters in the given line.
        /// </summary>
        /// <param name="lineIndex">0-based line index</param>
        /// <returns>number of characters in the given line, or -1 if no layout information is available</returns>
        public int GetLineLength(int lineIndex)
        {
            if (this.RenderScope == null)
            {
                return -1;
            }

            if (lineIndex < 0 || lineIndex >= LineCount)
            {
                throw new ArgumentOutOfRangeException("lineIndex");
            }

            TextPointer textPointerStart = GetStartPositionOfLine(lineIndex);
            TextPointer textPointerEnd = GetEndPositionOfLine(lineIndex);
            int length;

            if (textPointerStart == null || textPointerEnd == null)
            {
                // No layout available.
                length = -1;
            }
            else
            {
                length = textPointerStart.GetOffsetToPosition(textPointerEnd);
            }

            return length;
        }

        /// <summary>
        /// Return the index of the first line that is currently visible in the TextBox.
        /// </summary>
        /// <returns>0-based index of the first visible line, or -1 if no layout information is available</returns>
        public int GetFirstVisibleLineIndex()
        {
            if (this.RenderScope == null)
            {
                return -1;
            }

            // Include an epsilon in the calculation below to account for floating
            // point rounding error.  Example: suppose we're looking for line 10.
            // Because of rounding error, we calculate line 9.9999, take the
            // Floor, and get the previous line.
            const double epsilon = 0.0001;
            double lineHeight = GetLineHeight();
            return (int)Math.Floor((this.VerticalOffset / lineHeight) + epsilon);
        }

        /// <summary>
        /// Return the index of the last line that is currently visible in the TextBox.
        /// </summary>
        /// <returns>0-based index of the last visible line, or -1 if no layout information is available</returns>
        public int GetLastVisibleLineIndex()
        {
            double height;

            if (this.RenderScope == null)
            {
                return -1;
            }

            height = ((IScrollInfo)this.RenderScope).ExtentHeight;

            if (this.VerticalOffset + this.ViewportHeight >= height)
            {
                return this.LineCount - 1;
            }
            else
            {
                return (int)Math.Floor((this.VerticalOffset + this.ViewportHeight - 1) / GetLineHeight());
            }
        }

        /// <summary>
        /// Scroll the minimal amount necessary to bring the given line into full view.
        /// </summary>
        /// <param name="lineIndex">line to scroll into view</param>
        public void ScrollToLine(int lineIndex)
        {
            if (this.RenderScope == null)
            {
                return;
            }

            if (lineIndex < 0 || lineIndex >= LineCount)
            {
                throw new ArgumentOutOfRangeException("lineIndex");
            }

            TextPointer textPointer = GetStartPositionOfLine(lineIndex);
            Rect rect;
            if (GetRectangleFromTextPositionInternal(textPointer, false, out rect))
            {
                this.RenderScope.BringIntoView(rect);
            }
        }

        /// <summary>
        /// Get the text displayed at the given line.
        /// </summary>
        /// <param name="lineIndex">0-based index of the desired line</param>
        /// <returns>String containing a copy of the text at the given line index, or null if no layout information
        /// is available</returns>
        public String GetLineText(int lineIndex)
        {
            string text;
            TextPointer startOfLine;
            TextPointer endOfLine;

            if (this.RenderScope == null)
            {
                return null; // sentinel value
            }

            if (lineIndex < 0 || lineIndex >= LineCount)
            {
                throw new ArgumentOutOfRangeException("lineIndex");
            }

            startOfLine = GetStartPositionOfLine(lineIndex);
            endOfLine = GetEndPositionOfLine(lineIndex);

            // startOfLine/endOfLine will be null if no layout is available.
            if (startOfLine != null && endOfLine != null)
            {
                text = TextRangeBase.GetTextInternal(startOfLine, endOfLine);
            }
            else
            {
                text = this.Text;
            }

            return text;
        }

        /// <summary>
        /// Get the rectangle for the leading edge of the character at the given index.
        /// </summary>
        /// <param name="charIndex">index of the desired character</param>
        /// <returns>leading edge rectangle of the given character, or Rect.Empty if no layout information is available.</returns>
        public Rect GetRectFromCharacterIndex(int charIndex)
        {
            return GetRectFromCharacterIndex(charIndex, /*trailingEdge*/false);
        }

        /// <summary>
        /// Get the rectangle for an edge of the character at the given index.
        /// </summary>
        /// <param name="charIndex">index of the desired character</param>
        /// <param name="trailingEdge">specifies an edge of the character bounding box</param>
        /// <returns>leading or trailing edge rectangle of the given character, or Rect.Empty if no layout information is available.</returns>
        public Rect GetRectFromCharacterIndex(int charIndex, bool trailingEdge)
        {
            if (charIndex < 0 || charIndex > this.TextContainer.SymbolCount)
            {
                throw new ArgumentOutOfRangeException("charIndex");
            }

            // Start by moving to an insertion position in backward direction.
            // This ensures that when the character at charIndex is part of a surrogate pair or multi-byte character,
            // we handle leading/trailing edge correctly.

            TextPointer textPointer = TextContainer.CreatePointerAtOffset(charIndex, LogicalDirection.Backward);
            textPointer = textPointer.GetInsertionPosition(LogicalDirection.Backward);

            if (trailingEdge && charIndex < this.TextContainer.SymbolCount)
            {
                // Get next insertion position
                textPointer = textPointer.GetNextInsertionPosition(LogicalDirection.Forward);
                Invariant.Assert(textPointer != null);

                // Backward gravity for trailing edge
                textPointer = textPointer.GetPositionAtOffset(0, LogicalDirection.Backward);
            }
            else
            {
                // Forward gravity for leading edge
                textPointer = textPointer.GetPositionAtOffset(0, LogicalDirection.Forward);
            }

            // NB: rect will be Rect.Empty if no layout is available.
            Rect rect;
            GetRectangleFromTextPositionInternal(textPointer, /*relativeToTextBox*/true, out rect);
            return rect;
        }

        /// <summary>
        /// Returns the associated IndexedSpellingError at a specified character index.
        /// </summary>
        /// <param name="charIndex">
        /// Index of text to query.
        /// </param>
        /// <remarks>
        /// The charIndex paramter specifies a character to query.
        /// If the specificed character is not part of a misspelled word (or if
        /// IsSpellCheckEnabled == false) then this method will return null.
        /// </remarks>
        public SpellingError GetSpellingError(int charIndex)
        {
            if (charIndex < 0 || charIndex > this.TextContainer.SymbolCount)
            {
                throw new ArgumentOutOfRangeException("charIndex");
            }

            TextPointer position = this.TextContainer.CreatePointerAtOffset(charIndex, LogicalDirection.Forward);
            SpellingError spellingError = this.TextEditor.GetSpellingErrorAtPosition(position, LogicalDirection.Forward);

            if (spellingError == null && charIndex < this.TextContainer.SymbolCount - 1)
            {
                position = this.TextContainer.CreatePointerAtOffset(charIndex + 1, LogicalDirection.Forward);
                spellingError = this.TextEditor.GetSpellingErrorAtPosition(position, LogicalDirection.Backward);
            }

            return spellingError;
        }

        /// <summary>
        /// Returns the start index of the first character of a spelling error
        /// containing a specified char.
        /// </summary>
        /// <param name="charIndex">
        /// Index of the character to query.
        /// </param>
        /// <returns>
        /// Start index of the spelling error containing a specified char, or -1 if
        /// the char is not part of a spelling error.
        /// </returns>
        public int GetSpellingErrorStart(int charIndex)
        {
            SpellingError spellingError = GetSpellingError(charIndex);

            return (spellingError == null) ? -1 : spellingError.Start.Offset;
        }

        /// <summary>
        /// Returns the length of the spelling error containing a specified char.
        /// </summary>
        /// <param name="charIndex">
        /// Index of the character to query.
        /// </param>
        /// <returns>
        /// Length of the spelling error containing a specified char, or 0 if
        /// the char is not part of a spelling error.
        /// </returns>
        public int GetSpellingErrorLength(int charIndex)
        {
            SpellingError spellingError = GetSpellingError(charIndex);

            return (spellingError == null) ? 0 : spellingError.End.Offset - spellingError.Start.Offset;
        }

        /// <summary>
        /// Returns the index of the next character in a specificed direction
        /// that is the start of a misspelled word.
        /// </summary>
        /// <param name="charIndex">
        /// Index of text to query.
        /// </param>
        /// <param name="direction">
        /// Direction to query.
        /// </param>
        /// <remarks>
        /// The charIndex paramter specifies a character at which to start the query.
        /// When direction == LogicalDirection.Forward, the search includes the
        /// spelling error containing charIndex (if any).
        /// When direction == LogicalDirection.Backward, the search does not
        /// include the error containing charIndex (if any).
        ///
        /// If no misspelled word is encountered, the method returns -1.
        /// </remarks>
        public int GetNextSpellingErrorCharacterIndex(int charIndex, LogicalDirection direction)
        {
            if (charIndex < 0 || charIndex > this.TextContainer.SymbolCount)
            {
                throw new ArgumentOutOfRangeException("charIndex");
            }

            if (this.TextContainer.SymbolCount == 0)
            {
                // Early out on an empty doc to keep logic simpler below.
                return -1;
            }

            ITextPointer position = this.TextContainer.CreatePointerAtOffset(charIndex, direction);

            position = this.TextEditor.GetNextSpellingErrorPosition(position, direction);

            return (position == null) ? -1 : position.Offset;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// DependencyProperty for <see cref="TextWrapping" /> property.
        /// </summary>
        public static readonly DependencyProperty TextWrappingProperty =
                TextBlock.TextWrappingProperty.AddOwner(
                        typeof(TextBox),
                        new FrameworkPropertyMetadata(
                                TextWrapping.NoWrap,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnTextWrappingChanged)));

        /// <summary>
        /// The TextWrapping property controls whether or not text wraps
        /// when it reaches the flow edge of its containing block box.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get
            {
                return (TextWrapping)GetValue(TextWrappingProperty);
            }
            set
            {
                SetValue(TextWrappingProperty, value);
            }
        }

        /// <summary>
        /// Dependency ID for the MinLines property
        /// Default value: 1
        /// </summary>
        public static readonly DependencyProperty MinLinesProperty =
                DependencyProperty.Register(
                        "MinLines", // Property name
                        typeof(int), // Property type
                        typeof(TextBox), // Property owner
                        new FrameworkPropertyMetadata(
                                1,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnMinMaxChanged)),
                        new ValidateValueCallback(MinLinesValidateValue));

        /// <summary>
        /// Minimum number of lines to size to.
        /// </summary>
        [DefaultValue(1)]
        public int MinLines
        {
            get { return (int) GetValue(MinLinesProperty); }
            set { SetValue(MinLinesProperty, value); }
        }

        /// <summary>
        /// Dependency ID for the MaxLines property
        /// Default value: MaxInt
        /// </summary>
        public static readonly DependencyProperty MaxLinesProperty =
                DependencyProperty.Register(
                        "MaxLines", // Property name
                        typeof(int), // Property type
                        typeof(TextBox), // Property owner
                        new FrameworkPropertyMetadata(
                                Int32.MaxValue,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnMinMaxChanged)),
                        new ValidateValueCallback(MaxLinesValidateValue));

        /// <summary>
        /// Minimum number of lines to size to.
        /// </summary>
        [DefaultValue(Int32.MaxValue)]
        public int MaxLines
        {
            get { return (int) GetValue(MaxLinesProperty); }
            set { SetValue(MaxLinesProperty, value); }
        }

        /// <summary>
        /// The DependencyID for the Text property.
        /// Default Value:      ""
        /// </summary>
        public static readonly DependencyProperty TextProperty =
                DependencyProperty.Register(
                        "Text", // Property name
                        typeof(string), // Property type
                        typeof(TextBox), // Property owner
                        new FrameworkPropertyMetadata( // Property metadata
                                string.Empty, // default value
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | // Flags
                                    FrameworkPropertyMetadataOptions.Journal,
                                new PropertyChangedCallback(OnTextPropertyChanged),    // property changed callback
                                new CoerceValueCallback(CoerceText),
                                true, // IsAnimationProhibited
                                UpdateSourceTrigger.LostFocus   // DefaultUpdateSourceTrigger
                                ));

        /// <summary>
        /// Contents of the TextBox.
        /// </summary>
        [DefaultValue("")]
        [Localizability(LocalizationCategory.Text)]
        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// The DependencyID for the CharacterCasing property.
        /// Controls whether or not input text is converted to upper or lower case
        /// Flags:              Can be used in style rules
        /// Default Value:      CharacterCasing.Normal
        /// </summary>
        public static readonly DependencyProperty CharacterCasingProperty =
                DependencyProperty.Register(
                        "CharacterCasing", // Property name
                        typeof(CharacterCasing), // Property type
                        typeof(TextBox), // Property owner
                        new FrameworkPropertyMetadata(CharacterCasing.Normal /*default value*/),
                        new ValidateValueCallback(CharacterCasingValidateValue) /*validation callback*/);

        /// <summary>
        /// Character casing of the TextBox
        /// </summary>
        public CharacterCasing CharacterCasing
        {
            get { return (CharacterCasing) GetValue(CharacterCasingProperty); }
            set { SetValue(CharacterCasingProperty, value); }
        }

        /// <summary>
        /// The limit number of characters that the textbox or other editable controls can contain.
        /// if it is 0, means no-limitation.
        /// User can set this value for some simple single line textbox to restrict the text number.
        /// RichTextBox doesn't have this limitation.
        /// By default it is 0.
        /// </summary>
        /// <remarks>
        /// When this property is set to zero, the maximum length of the text that can be entered
        /// in the control is limited only by available memory. You can use this property to restrict
        /// the length of text entered in the control for values such as postal codes and telephone numbers.
        /// You can also use this property to restrict the length of text entered when the data is to be entered
        /// in a database.
        /// You can limit the text entered into the control to the maximum length of the corresponding field in the database.
        /// Note:   In code, you can set the value of the Text property to a value that is larger than
        /// the value specified by the MaxLength property.
        /// This property only affects text entered into the control at runtime.
        /// </remarks>
        public static readonly DependencyProperty MaxLengthProperty =
                DependencyProperty.Register(
                    "MaxLength", // Property name
                    typeof(int), // Property type
                    typeof(TextBox), // Property owner
                    new FrameworkPropertyMetadata(0), /*default value*/
                    new ValidateValueCallback(MaxLengthValidateValue));


        /// <summary>
        /// Maximum number of characters the TextBox can accept
        /// </summary>
        [DefaultValue((int)0)]
        [Localizability(LocalizationCategory.None, Modifiability = Modifiability.Unmodifiable)] // cannot be modified by localizer
        public int MaxLength
        {
            get { return (int) GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextAlignment" /> property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty = Block.TextAlignmentProperty.AddOwner(typeof(TextBox));

        /// <summary>
        /// The TextAlignment property specifies horizontal alignment of the content.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get
            {
                return (TextAlignment)GetValue(TextAlignmentProperty);
            }
            set
            {
                SetValue(TextAlignmentProperty, value);
            }
        }

        /// <summary>
        /// Selected Text
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedText
        {
            get
            {
                return TextSelectionInternal.Text;
            }
            set
            {
                using (this.TextSelectionInternal.DeclareChangeBlock())
                {
                    TextSelectionInternal.Text = value;
                }
            }
        }

        /// <summary>
        /// Character number of the selected text
        /// </summary>
        /// <remarks>
        /// Length is calculated as unicode count, so it counts
        /// eacn \r\n combination as 2 - even though it is actially
        /// one caret position, and it would be illegal to insert
        /// any characters between them or expect selection ends
        /// to stay between them.
        /// Because of that after setting SelectionLength to some value
        /// it can be automatically corrected (by adding 1)
        /// if selection end happens to be between \r and \n.
        /// </remarks>
        [DefaultValue((int)0)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionLength
        {
            get
            {
                return TextSelectionInternal.Start.GetOffsetToPosition(TextSelectionInternal.End);
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterCannotBeNegative));
                }

                // Identify new position for selection end
                int maxLength = TextSelectionInternal.Start.GetOffsetToPosition(TextContainer.End);
                if (value > maxLength)
                {
                    value = maxLength;
                }
                TextPointer newEnd = new TextPointer(TextSelectionInternal.Start, value, LogicalDirection.Forward);

                // Normalize end in some particular direction to exclude ambiguity on surrogate boundaries
                newEnd = newEnd.GetInsertionPosition(LogicalDirection.Forward);

                // Set new selection
                TextSelectionInternal.Select(TextSelectionInternal.Start, newEnd);
            }
        }

        /// <summary>
        /// The start position of the selection.
        /// </summary>
        /// <remarks>
        /// Index is calculated as unicode offset, so it counts
        /// eacn \r\n combination as 2 - even though it is actially
        /// one caret position, and it would be illegal to insert
        /// any characters between them or expect selection ends
        /// to stay between them.
        /// Because of that after setting SelectionStart to some value
        /// it can be automatically corrected (by adding 1)
        /// if it happens to be between \r and \n.
        /// </remarks>
        [DefaultValue((int)0)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionStart
        {
            get
            {
                return this.TextSelectionInternal.Start.Offset;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterCannotBeNegative));
                }

                // Store current length of the selection
                int selectionLength = TextSelectionInternal.Start.GetOffsetToPosition(TextSelectionInternal.End);

                // Identify new position for selection Start
                int maxStart = TextContainer.SymbolCount;
                if (value > maxStart)
                {
                    value = maxStart;
                }
                TextPointer newStart = TextContainer.CreatePointerAtOffset(value, LogicalDirection.Forward);

                // Normalize new start in some particular direction, to exclude ambiguity on surrogates bounndaries
                // and to start counting length from appropriate position.
                newStart = newStart.GetInsertionPosition(LogicalDirection.Forward);

                // Identify new position for selection End
                int maxLength = newStart.GetOffsetToPosition(TextContainer.End);
                if (selectionLength > maxLength)
                {
                    selectionLength = maxLength;
                }
                TextPointer newEnd = new TextPointer(newStart, selectionLength, LogicalDirection.Forward);

                // Normalize end in some particular direction to exclude ambiguity on surrogate boundaries
                newEnd = newEnd.GetInsertionPosition(LogicalDirection.Forward);

                // Set new selection
                TextSelectionInternal.Select(newStart, newEnd);
            }
        }

        /// <summary>
        /// Position of the caret.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CaretIndex
        {
            get
            {
                return SelectionStart;
            }

            set
            {
                Select(value, 0);
            }
        }

        /// <summary>
        /// Number of lines in the TextBox.
        /// </summary>
        /// <value>number of lines in the TextBox, or -1 if no layout information is available</value>
        /// <remarks>
        /// If Wrap == true, changing the width of the TextBox may change this value.
        /// The value returned is the number of lines in the entire TextBox, regardless of how many are
        /// currently in view.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int LineCount
        {
            get
            {
                if (this.RenderScope == null)
                {
                    return -1;
                }

                return GetLineIndexFromCharacterIndex(this.TextContainer.SymbolCount) + 1;
            }
        }

        /// <summary>
        /// DependencyProperty for the TextDecorations property.
        /// </summary>
        public static readonly DependencyProperty TextDecorationsProperty =
                Inline.TextDecorationsProperty.AddOwner(
                        typeof(TextBox),
                        new FrameworkPropertyMetadata(
                            new FreezableDefaultValueFactory(TextDecorationCollection.Empty),
                            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Property used to apply decorations such as underline to content.
        /// </summary>
        public TextDecorationCollection TextDecorations
        {
            get { return (TextDecorationCollection)GetValue(TextDecorationsProperty); }
            set { SetValue(TextDecorationsProperty, value); }
        }


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

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextBoxAutomationPeer(this);
        }

        ///
        /// <see cref="FrameworkElement.OnPropertyChanged"/>
        ///
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            //  always call base.OnPropertyChanged, otherwise Property Engine will not work.
            base.OnPropertyChanged(e);

            if (this.RenderScope != null)
            {
                FrameworkPropertyMetadata fmetadata = e.Property.GetMetadata(typeof(TextBox)) as FrameworkPropertyMetadata;
                if (fmetadata != null)
                {
                    // We need to check for TextAlignmentProperty specifically since a local value change might require a render
                    // update even though e.IsAValueChange is false (see TextBoxView.CalculatedTextAlignment).
                    if (e.IsAValueChange || e.IsASubPropertyChange || e.Property == TextBox.TextAlignmentProperty)
                    {
                        if (fmetadata.AffectsMeasure || fmetadata.AffectsArrange ||
                            fmetadata.AffectsParentMeasure || fmetadata.AffectsParentArrange ||
                            e.Property == Control.HorizontalContentAlignmentProperty || e.Property == Control.VerticalContentAlignmentProperty)
                        {
                            ((TextBoxView)this.RenderScope).Remeasure();
                        }
                        else if (fmetadata.AffectsRender &&
                                (e.IsAValueChange || !fmetadata.SubPropertiesDoNotAffectRender))
                        {
                            ((TextBoxView)this.RenderScope).Rerender();
                        }

                        if (Speller.IsSpellerAffectingProperty(e.Property) &&
                            this.TextEditor.Speller != null)
                        {
                            this.TextEditor.Speller.ResetErrors();
                        }
                    }
                }
            }

            TextBoxAutomationPeer peer = UIElementAutomationPeer.FromElement(this) as TextBoxAutomationPeer;
            if (peer != null)
            {
                if (e.Property == TextProperty)
                {
                    peer.RaiseValuePropertyChangedEvent((string)e.OldValue, (string)e.NewValue);
                }

                if (e.Property == IsReadOnlyProperty)
                {
                    peer.RaiseIsReadOnlyPropertyChangedEvent((bool)e.OldValue, (bool)e.NewValue);
                }
            }
        }

        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                //  We don't need this type of serialization.
                // This is an atavism from TextBoxBase gereric implementation
                // We can simply return a string.
                return new RangeContentEnumerator((TextPointer)this.TextContainer.Start, (TextPointer)this.TextContainer.End);
            }
        }

        /// <summary>
        /// Measurement override. Implement your size-to-content logic here.
        /// </summary>
        /// <param name="constraint">
        /// Sizing constraint.
        /// </param>
        protected override Size MeasureOverride(Size constraint)
        {
            if (MinLines > 1 && MaxLines < MinLines)
            {
                throw new Exception(SR.Get(SRID.TextBoxMinMaxLinesMismatch));
            }

            Size size = base.MeasureOverride(constraint);

            if (_minmaxChanged)
            {
                // If there is a scrollViewer, we'll listen to the ScrollChanged event and
                // handle min/maxLines there.
                if (this.ScrollViewer == null)
                {
                    SetRenderScopeMinMaxHeight();
                }
                else
                {
                    SetScrollViewerMinMaxHeight();
                }
                _minmaxChanged = false;
            }

            return size;
        }

        // Called every time after Wrap property gets new value
        internal void OnTextWrappingChanged()
        {
            CoerceValue(HorizontalScrollBarVisibilityProperty);
        }

        // Allocates the initial render scope for this control.
        internal override FrameworkElement CreateRenderScope()
        {
            return new TextBoxView(this);
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Detaches the editor from old visual tree and attaches it to a new one
        /// </summary>
        internal override void AttachToVisualTree()
        {
            base.AttachToVisualTree();

            if (this.RenderScope == null)
            {
                return;
            }

            // Set TextWrapping property for the new renderScope
            OnTextWrappingChanged();

            // We need to recalculate our min/max story.
            _minmaxChanged = true;
        }

        /// <summary>
        ///     Gives a string representation of this object.
        /// </summary>
        internal override string GetPlainText()
        {
            return this.Text;
        }

        ///
        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get
            {
                return _dType;
            }
        }

        /// <summary>
        /// Scroll content by one line to the top.
        /// </summary>
        internal override void DoLineUp()
        {
            if (this.ScrollViewer != null)
            {
                ScrollViewer.ScrollToVerticalOffset(VerticalOffset - GetLineHeight());
            }
        }

        /// <summary>
        /// Scroll content by one line to the bottom.
        /// </summary>
        internal override void DoLineDown()
        {
            if (this.ScrollViewer != null)
            {
                ScrollViewer.ScrollToVerticalOffset(VerticalOffset + GetLineHeight());
            }
        }

        /// <summary>
        /// Handler for TextContainer.Changed event.
        /// </summary>
        internal override void OnTextContainerChanged(object sender, TextContainerChangedEventArgs e)
        {
            bool resetText = false;
            string newTextValue = null;

            try
            {
                // if there are re-entrant changes, only raise public events
                // after the outermost change completes
                _changeEventNestingCount++;

                // Ignore property changes that originate from OnTextPropertyChange.
                if (!_isInsideTextContentChange)
                {
                    _isInsideTextContentChange = true;

                    // Use a DeferredTextReference instead of calculating the new
                    // value now for better performance.  Most of the time no
                    // one cares what the new value is, and loading our content into a
                    // string can be extremely expensive.
                    DeferredTextReference dtr = new DeferredTextReference(this.TextContainer);
                    _newTextValue = dtr;
                    SetCurrentDeferredValue(TextProperty, dtr);
                }
            }
            finally
            {
                _changeEventNestingCount--;
                if (_changeEventNestingCount == 0)
                {
                    // when Text is data-bound, _newTextValue is converted from a
                    // deferred reference to a string.  The binding writes the string
                    // back to the source, then computes a new value for Text (which
                    // may be different, either because the source normalizes the value
                    // or because of conversion and formatting).  Usually this raises
                    // a change notification for Text, which brings the Text property and
                    // the text container into sync.  But this doesn't happen in one
                    // case:  when the normalized value is the same as the original
                    // value for Text.  The property engine thinks that Text hasn't
                    // changed, and doesn't raise the notification.  It's true that
                    // Text hasn't changed, but we still need to update the text container,
                    // which now displays the wrong value.
                    // We detect that case by checking whether _newTextValue (the
                    // text container value) agrees with Text.
                    if (FrameworkCompatibilityPreferences.GetKeepTextBoxDisplaySynchronizedWithTextProperty())
                    {
                        newTextValue = _newTextValue as String;
                        resetText = (newTextValue != null && newTextValue != Text);
                    }

                    _isInsideTextContentChange = false;
                    _newTextValue = DependencyProperty.UnsetValue;
                }
            }

            if (resetText)
            {
                // The text container holds a new value which round-trips to the
                // old value of Text.  We need to bring the text container into sync.
                try
                {
                    _newTextValue = newTextValue;
                    _isInsideTextContentChange = true;
                    ++ _changeEventNestingCount;

                    OnTextPropertyChanged(newTextValue, Text);
                }
                finally
                {
                    -- _changeEventNestingCount;
                    _isInsideTextContentChange = false;
                    _newTextValue = DependencyProperty.UnsetValue;
                }
            }


            if (_changeEventNestingCount == 0)
            {
                // Let base raise the public TextBoxBase.TextChanged event.
                base.OnTextContainerChanged(sender, e);
            }
        }

        // if the DeferredTextReference is resolved to a string during the previous
        // method, track that here
        internal void OnDeferredTextReferenceResolved(DeferredTextReference dtr, string s)
        {
            if (dtr == _newTextValue)
            {
                _newTextValue = s;
            }
        }

        /// <summary>
        /// Handler for ScrollViewer's OnScrollChanged event.
        /// </summary>
        internal override void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            base.OnScrollChanged(sender, e);

            if (e.ViewportHeightChange != 0)
            {
                SetScrollViewerMinMaxHeight();
            }
        }

        // this method is called by an editable ComboBox to raise a TextChanged event after
        // ComboBox.Text is changed outside the scope of a TextBox.Text change
        // (e.g. when an IME text composition has completed).   It's a courtesy to
        // controls and apps that assume every change to ComboBox.Text will be
        // followed by a TextBox.TextChanged event from the combobox's editable TextBox.
        internal void RaiseCourtesyTextChangedEvent()
        {
            OnTextChanged(new TextChangedEventArgs(TextChangedEvent, UndoAction.None));
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 42; }
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Text Selection (readonly)
        /// </summary>
        internal TextSelection Selection
        {
            get
            {
                return (TextSelection)TextSelectionInternal;
            }
        }

        /// <summary>
        /// TextPointer where the TextBox's text begins (readonly)
        /// </summary>
        internal TextPointer StartPosition
        {
            get
            {
                return (TextPointer)this.TextContainer.Start;
            }
        }

        /// <summary>
        /// TextPointer where the TextBox's text ends (readonly)
        /// </summary>
        internal TextPointer EndPosition
        {
            get
            {
                return (TextPointer)this.TextContainer.End;
            }
        }

        /// <summary>
        /// IsTypographyDefaultValue
        /// </summary>
        internal bool IsTypographyDefaultValue
        {
            get
            {
                return !_isTypographySet;
            }
        }

        // ITextContainer holding the Control content.
        ITextContainer ITextBoxViewHost.TextContainer
        {
            get
            {
                return this.TextContainer;
            }
        }

        // Set true when typography property values are all default values.
        bool ITextBoxViewHost.IsTypographyDefaultValue
        {
            get
            {
                return this.IsTypographyDefaultValue;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Returns false if no layout is available.
        private bool GetRectangleFromTextPositionInternal(TextPointer position, bool relativeToTextBox, out Rect rect)
        {
            if (this.RenderScope == null)
            {
                rect = Rect.Empty;
                return false;
            }

            if (position.ValidateLayout())
            {
                rect = TextPointerBase.GetCharacterRect(position, position.LogicalDirection, relativeToTextBox);
            }
            else
            {
                rect = Rect.Empty;
            }

            return rect != Rect.Empty;
        }

        // Returns null if no layout is available.
        private TextPointer GetStartPositionOfLine(int lineIndex)
        {
            if (this.RenderScope == null)
            {
                return null;
            }

            Point point = new Point();

            // all lines in TextBox are the same height, so get the line height and multiply...
            double lineHeight = GetLineHeight();
            point.Y = lineHeight * lineIndex + (lineHeight / 2) - VerticalOffset;  // use a point in the middle of the line, to be safe
            point.X = -HorizontalOffset;

            TextPointer textPointer;

            if (TextEditor.GetTextView(this.RenderScope).Validate(point))
            {
                textPointer = (TextPointer)TextEditor.GetTextView(this.RenderScope).GetTextPositionFromPoint(point, /* snap to text */ true);
                textPointer = (TextPointer)TextEditor.GetTextView(this.RenderScope).GetLineRange(textPointer).Start.CreatePointer(textPointer.LogicalDirection);
            }
            else
            {
                textPointer = null;
            }

            return textPointer;
        }

        private TextPointer GetEndPositionOfLine(int lineIndex)
        {
            if (this.RenderScope == null)
            {
                return null;
            }

            // all lines in TextBox are the same height, so get the line height and multiply...
            Point point = new Point();

            double lineHeight = GetLineHeight();
            point.Y = lineHeight * lineIndex + (lineHeight / 2) - VerticalOffset;  // use a point in the middle of the line, to be safe
            point.X = 0;

            TextPointer textPointer;

            if (TextEditor.GetTextView(this.RenderScope).Validate(point))
            {
                textPointer = (TextPointer)TextEditor.GetTextView(this.RenderScope).GetTextPositionFromPoint(point, /* snap to text */ true);
                textPointer = (TextPointer)TextEditor.GetTextView(this.RenderScope).GetLineRange(textPointer).End.CreatePointer(textPointer.LogicalDirection);

                // Hit testing ignores line breaks, so the position returned will be between the last visible character
                // and the line break, if any.  We want the position AFTER the line break.
                if (TextPointerBase.IsNextToPlainLineBreak(textPointer, LogicalDirection.Forward))
                {
                    textPointer.MoveToNextInsertionPosition(LogicalDirection.Forward);
                }
            }
            else
            {
                textPointer = null;
            }

            return textPointer;
        }

        private static object CoerceHorizontalScrollBarVisibility(DependencyObject d, object value)
        {
            TextBox textBox = d as TextBox;

            if (textBox != null && (textBox.TextWrapping == TextWrapping.Wrap || textBox.TextWrapping == TextWrapping.WrapWithOverflow))
            {
                return ScrollBarVisibility.Disabled;
            }
            return value;
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        private static bool MaxLengthValidateValue(object value)
        {
            return ((int)value) >= 0;
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        private static bool CharacterCasingValidateValue(object value)
        {
            return (CharacterCasing.Normal <= (CharacterCasing)value && (CharacterCasing)value <= CharacterCasing.Upper);
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        private static bool MinLinesValidateValue(object value)
        {
            return ((int)value > 0);
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        private static bool MaxLinesValidateValue(object value)
        {
            return ((int)value > 0);
        }

        /// <summary>
        /// Callback for changes to the MinLines and MaxLines property
        /// </summary>
        private static void OnMinMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBox textBox = (TextBox)d;

            textBox._minmaxChanged = true;
        }

        /// <summary>
        /// Callback for changes to the Text property
        /// </summary>
        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBox textBox = (TextBox)d;

            if (textBox._isInsideTextContentChange)
            {
                // Ignore property changes that originate from OnTextContainerChanged,
                // unless they contain a different value (indicating that a
                // re-entrant call changed the value)
                if (textBox._newTextValue == DependencyProperty.UnsetValue ||
                    textBox._newTextValue is DeferredTextReference)
                {
                    // in this case, no re-entrant call could have happened, (it
                    // would have set _newTextValue to a string).
                    return;
                }

                // Otherwise we still need to check if the re-entrant call actually
                // changed the value.  That's done in the instance OnTextPropertyChanged method.
            }

            // if we get here, it's OK to ask for e.NewValue, which inflates the
            // deferred reference, because either (a) there is no deferred reference
            // (e.g. user set Text directly), or (b) it came from a re-entrant
            // change and we'll need the actual string anyway.
            textBox.OnTextPropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnTextPropertyChanged(string oldText, string newText)
        {
            bool inReentrantChange = false;
            int savedCaretIndex = 0;
            bool resetCaret = false;

            if (_isInsideTextContentChange)
            {
                // Ignore property changes that originate from OnTextContainerChanged,
                // unless they contain a different value (indicating that a
                // re-entrant call changed the value)
                if (Object.Equals(_newTextValue, newText))
                {
                    return;
                }

                // If we get this far, we're being called re-entrantly with a value
                // different from the one set by OnTextContainerChanged.  We should
                // honor this new value.
                inReentrantChange = true;
            }

            // CoerceText will have already converted null -> String.Empty,
            // but our default CoerceValueCallback could be overridden by a
            // derived class.  So check again here.
            if (newText == null)
            {
                newText = String.Empty;
            }

            bool hasExpression = HasExpression(LookupEntry(TextBox.TextProperty.GlobalIndex), TextBox.TextProperty);
            string oldTextForCaretIndexComputation = oldText;

            // A data-bound textbox sometimes has to display a value different from what
            // the user typed - when the value is changed by the data source or a converter
            // (this is the so-called "$10 feature").  When this happens, we should reposition
            // the caret within the new text at a position matching where it was in the old text,
            // so that the user can continue typing.  This can arise in two ways:
            //  a) re-entrant change (typing causes write-back which produces changed text)
            //  b) delayed change (binding with Delay writes back, producing changed text)
            // The old text is obtained differently in each case - this is handled by the callers.
            if (inReentrantChange)
            {
                resetCaret = true;

                // If this is a re-entrant change then the TextContainer has previously been updated
                // and the CaretIndex corresponds to the Text represented by the TextContainer.
                // Eg. If someone had an IntConverter on the two-way Binding for the TextProperty
                // which converted the string into an int and back, then this roundtripping operation
                // would automatically trim all leading zeros. So if the user typed "01" then the call
                // leading here from OnTextContainerChange's SetDeferredCurrentValue call will have
                // - the oldText as "0"
                // - the newText as "1" (after the conversion)
                // - the _newTextValue as "01" which is what the user typed in
                // - the CaretIndex here as 2 which corresponds to the _newTextValue not the oldText
                oldTextForCaretIndexComputation = (string)_newTextValue;
            }
            else if (hasExpression)
            {
                BindingExpressionBase beb = BindingOperations.GetBindingExpression(this, TextProperty);
                resetCaret = (beb != null) && beb.IsInUpdate && beb.IsInTransfer;
            }

            if (resetCaret)
            {
                savedCaretIndex = ChooseCaretIndex(CaretIndex, oldTextForCaretIndexComputation, newText);
            }

            if (inReentrantChange)
            {
                // we're about to change text container to hold newText.
                // update the cached proposed value accordingly
                _newTextValue = newText;
            }

            _isInsideTextContentChange = true;
            try
            {
                using (TextSelectionInternal.DeclareChangeBlock())
                {
                    // Update the text content with new TextProperty value.
                    TextContainer.DeleteContentInternal((TextPointer)TextContainer.Start, (TextPointer)TextContainer.End);
                    TextContainer.End.InsertTextInRun(newText);

                    // Collapse selection to the beginning of a text box
                    Select(savedCaretIndex, 0);
                }
            }
            finally
            {
                if (!inReentrantChange)
                {
                    _isInsideTextContentChange = false;
                }
            }

            // We need to clear undo stack in case when the value comes from
            // databinding or some other expression.
            if (hasExpression)
            {
                UndoManager undoManager = TextEditor._GetUndoManager();
                if (undoManager != null)
                {
                    if (undoManager.IsEnabled)
                        undoManager.Clear();
                }
            }
        }

        // return an index within newText that best approximates the old index
        // within oldText.   Called when the text container's content is changed
        // re-entrantly.
        private int ChooseCaretIndex(int oldIndex, string oldText, string newText)
        {
            // There is no exact algorithm for this.  Instead we use some heuristics.
            //   First handle some frequent special cases

            // oldText appears within newText, translate the index
            int index = newText.IndexOf(oldText, StringComparison.Ordinal);
            if (oldText.Length > 0 && index >= 0)
                return index + oldIndex;

            // caret was at one edge of oldText, return corresponding edge
            if (oldIndex == 0)
                return 0;
            if (oldIndex == oldText.Length)
                return newText.Length;

            // newText differs from oldText by a small replacement
            // (this is common when doing conversions to numeric types - adding
            // leading or trailing zeros, decimal separators, thousand separators,
            // etc.).
            // The two strings share a common prefix and suffix - find those
            int prefix, suffix;
            for (   prefix = 0;
                    prefix < oldText.Length && prefix < newText.Length;
                    ++prefix)
            {
                if (oldText[prefix] != newText[prefix])
                    break;
            }
            for (   suffix = 0;
                    suffix < oldText.Length && suffix < newText.Length;
                    ++suffix)
            {
                if (oldText[oldText.Length - 1 - suffix ] != newText[newText.Length - 1 - suffix])
                    break;
            }
            // if the prefix and suffix account for enough of the text, treat the
            // rest as a small replacement
            if ( 2*(prefix + suffix) >= Math.Min(oldText.Length, newText.Length))
            {
                // if the caret was in or next to the prefix or suffix, return the
                // corresponding position in newText
                if (oldIndex <= prefix)
                    return oldIndex;
                if (oldIndex >= oldText.Length - suffix)
                    return newText.Length - (oldText.Length - oldIndex);
            }

            // we're left with the hard case - newText is substantially different
            // from oldText.  Look for the longest matching substring that includes
            // the character just before the (old) caret - this is what the user
            // just typed, so it should participate in the match.
            char anchor = oldText[oldIndex - 1];
            int anchorIndex = newText.IndexOf(anchor);
            int bestIndex = -1;
            int bestLength = 1;     // match at least 2 chars

            while (anchorIndex >= 0)
            {
                int matchLength = 1;

                // match backward from the anchor position
                for (   index = anchorIndex - 1;
                        index >=0 && oldIndex - (anchorIndex - index) >= 0;
                        --index)
                {
                    if (newText[index] != oldText[oldIndex - (anchorIndex - index)])
                        break;
                    ++ matchLength;
                }

                // match forward from the anchor position
                for (   index = anchorIndex + 1;
                        index < newText.Length && oldIndex + (index - anchorIndex) < oldText.Length;
                        ++index)
                {
                    if (newText[index] != oldText[oldIndex + (index - anchorIndex)])
                        break;
                    ++ matchLength;
                }

                // remember the best match
                if (matchLength > bestLength)
                {
                    bestIndex = anchorIndex + 1;
                    bestLength = matchLength;
                }

                // advance to the next occurrence of the anchor character
                anchorIndex = newText.IndexOf(anchor, anchorIndex + 1);
            }

            // return the index of the best match.  If none found, put the cursor at the end
            return (bestIndex < 0) ? newText.Length : bestIndex;
        }

        /// <summary>
        /// Callback for changes to the TextWrapping property
        /// </summary>
        private static void OnTextWrappingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox)
            {
                ((TextBox)d).OnTextWrappingChanged();
            }
        }

        /// <summary>
        /// Update the value of ScrollViewer.MinHeight/MaxHeight
        /// </summary>
        private void SetScrollViewerMinMaxHeight()
        {
            if (this.RenderScope == null)
            {
                return;
            }

            if (ReadLocalValue(HeightProperty) != DependencyProperty.UnsetValue ||
                ReadLocalValue(MaxHeightProperty) != DependencyProperty.UnsetValue ||
                ReadLocalValue(MinHeightProperty) != DependencyProperty.UnsetValue)
            {
                // scrub ScrollViewer's min/max height if any height values are set on TextBox
                this.ScrollViewer.ClearValue(MinHeightProperty);
                this.ScrollViewer.ClearValue(MaxHeightProperty);
                return;
            }

            double chrome = this.ScrollViewer.ActualHeight - ViewportHeight;
            double lineHeight = GetLineHeight();
            double value = chrome + (lineHeight * MinLines);

            if (MinLines > 1 && this.ScrollViewer.MinHeight != value)
            {
                this.ScrollViewer.MinHeight = value;
            }

            value = chrome + (lineHeight * MaxLines);

            if (MaxLines < Int32.MaxValue && this.ScrollViewer.MaxHeight != value)
            {
                this.ScrollViewer.MaxHeight = value;
            }
        }


        /// <summary>
        /// Update the value of RenderScope.MinHeight/MaxHeight
        /// </summary>
        private void SetRenderScopeMinMaxHeight()
        {
            if (this.RenderScope == null)
            {
                return;
            }

            if (ReadLocalValue(HeightProperty) != DependencyProperty.UnsetValue ||
                ReadLocalValue(MaxHeightProperty) != DependencyProperty.UnsetValue ||
                ReadLocalValue(MinHeightProperty) != DependencyProperty.UnsetValue)
            {
                RenderScope.ClearValue(MinHeightProperty);
                RenderScope.ClearValue(MaxHeightProperty);
            }
            else
            {
                double lineHeight = GetLineHeight();
                double value = lineHeight * MinLines;

                if (MinLines > 1 && RenderScope.MinHeight != value)
                {
                    RenderScope.MinHeight = value;
                }

                value = lineHeight * MaxLines;

                if (MaxLines < Int32.MaxValue && RenderScope.MaxHeight != value)
                {
                    RenderScope.MaxHeight = value;
                }
            }
        }

        //
        // Called by MeasureOverride to get the height of one line of text in the current font.
        //
        private double GetLineHeight()
        {
            // change Text height based on line size
            FontFamily fontFamily = (FontFamily)this.GetValue(FontFamilyProperty);
            double fontSize = (double)this.GetValue(TextElement.FontSizeProperty);

            // If Ps Task 25254 is completed (not likely in V1), LineStackingStrategy
            // won't be constant and we'll need to call some sort of CalcLineAdvance method.
            double lineHeight;

            if (TextOptions.GetTextFormattingMode(this) == TextFormattingMode.Ideal)
            {
                lineHeight = fontFamily.LineSpacing * fontSize;
            }
            else
            {
                lineHeight = fontFamily.GetLineSpacingForDisplayMode(fontSize, GetDpi().DpiScaleY);
            }

            return lineHeight;
        }


        //
        // Only serialize Text when not using the XamlTextHostSerializer
        //
        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeText(XamlDesignerSerializationManager manager)
        {
            return manager.XmlWriter == null;
        }

        //
        // Callback for command system to verify that the LineUp / LineDown commands should be enabled.
        // ScrollViewer always returns true, so we follow suit.
        //
        private static void OnQueryScrollCommand(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        // Callback from the property system, after a new value is set to the TextProperty.
        // Note we cannot assume value is a string here -- it may be a DeferredTextReference.
        private static object CoerceText(DependencyObject d, object value)
        {
            if (value == null)
            {
                return String.Empty;
            }

            return value;
        }

        //  typography properties changed, no cache for this, just reset the flag
        private static void OnTypographyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBox textbox = (TextBox)d;

            textbox._isTypographySet = true;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private static DependencyObjectType _dType;

        //  We could pack all flags into one word and save some memory per TextBox

        // This flag is set when the MinLines or MaxLines properties are invalidated, and
        // checked in MeasureOverride.  When true, MeasureOverride calls SetMinMaxHeight to
        // make sure the change in min/max height happens immediately.
        private bool _minmaxChanged;

        // Flag used to prevent reentrancy between nested
        // OnTextPropertyChanged/OnTextContainerChanged callbacks.
        private bool _isInsideTextContentChange;
        private object _newTextValue = DependencyProperty.UnsetValue;

        // Flag used to indicate that Typography properties are not at default values
        private bool _isTypographySet;

        // depth of nested calls to OnTextContainerChanged.
        private int _changeEventNestingCount;

        #endregion Private Fields
    }
}

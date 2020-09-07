// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for Text pattern
//


using System;
using System.Diagnostics;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Purpose:
    ///     The TextPattern object is what you get back when you ask an element for text pattern. 
    /// Example usages:
    ///     It is the Interface that represents text like an edit control. This pretty 
    ///     much means any UI elements that contain text.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class TextPattern: BasePattern
#else
    public class TextPattern: BasePattern
#endif
    {
        #region Constructors

        internal TextPattern(AutomationElement el, SafePatternHandle hPattern)
            : base(el, hPattern)
        {
            Debug.Assert(el != null);
            Debug.Assert(!hPattern.IsInvalid);

            _hPattern = hPattern;
            _element = el;
        }
        #endregion Constructors
        
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>
        /// Indicates that a text attribute varies over a range.
        /// </summary>
        public static readonly object MixedAttributeValue = TextPatternIdentifiers.MixedAttributeValue;

        #region TextAttribute

        // IMPORTANT: if you add or remove AutomationTextAttributes be sure to make the corresponding changes in 
        // AutomationComInteropProvider.cs' AutomationConstants struct and AutomationComInteropProvider.InitializeConstants.

        /// <summary>Type of animation applied from AnimationStyle enum. </summary>
        public static readonly AutomationTextAttribute AnimationStyleAttribute = TextPatternIdentifiers.AnimationStyleAttribute;
        /// <summary>Background color as a 32-bit Win32 COLORREF.</summary>
        public static readonly AutomationTextAttribute BackgroundColorAttribute = TextPatternIdentifiers.BackgroundColorAttribute;
        /// <summary>Bullet style from BulletStyle enum. </summary>
        public static readonly AutomationTextAttribute BulletStyleAttribute = TextPatternIdentifiers.BulletStyleAttribute;
        /// <summary>Cap style from CapStyle enum.</summary>
        public static readonly AutomationTextAttribute CapStyleAttribute = TextPatternIdentifiers.CapStyleAttribute;
        /// <summary>CultureInfo of the character down to sub-language level, e.g. Swiss French instead of French. See the CultureInfo in 
        /// .NET Framework for more detail on the language code format. Clients should note that there may be many cases where the 
        /// Language of the character defaults to application UI language because many servers do not support language tag and, 
        /// even when supported, authors may not use it.</summary>
        public static readonly AutomationTextAttribute CultureAttribute = TextPatternIdentifiers.CultureAttribute;
        /// <summary>Non-localized string that represents font face in TrueType. Providers can supply their own. 
        /// Examples include "Arial Black" and "Arial Narrow". </summary>
        public static readonly AutomationTextAttribute FontNameAttribute = TextPatternIdentifiers.FontNameAttribute;
        /// <summary>Point size of the character as a double.</summary>
        public static readonly AutomationTextAttribute FontSizeAttribute = TextPatternIdentifiers.FontSizeAttribute;
        /// <summary>Thickness of font as an int. This is modeled after the lfWeight field in GDI LOGFONT. For consistency, the following values 
        /// have been adopted from LOGFONT:0=DontCare, 100=Thin, 200=ExtraLight or UltraLight, 300=Light, 400=Normal or Regular, 
        /// 500=Medium, 600=SemiBold or DemiBold, 700=Bold, 800=ExtraBold or UltraBold, and 900=Heavy or Black.</summary>
        public static readonly AutomationTextAttribute FontWeightAttribute = TextPatternIdentifiers.FontWeightAttribute;
        /// <summary>Color of the text as a 32-bit Win32 COLORREF.</summary>
        public static readonly AutomationTextAttribute ForegroundColorAttribute = TextPatternIdentifiers.ForegroundColorAttribute;
        /// <summary>Horizontal alignment from HorizontalTextAlignment enum. </summary>
        public static readonly AutomationTextAttribute HorizontalTextAlignmentAttribute = TextPatternIdentifiers.HorizontalTextAlignmentAttribute;
        /// <summary>First-line indentation in points as a double.</summary>
        public static readonly AutomationTextAttribute IndentationFirstLineAttribute = TextPatternIdentifiers.IndentationFirstLineAttribute;
        /// <summary>Leading indentation in points as a double.</summary>
        public static readonly AutomationTextAttribute IndentationLeadingAttribute = TextPatternIdentifiers.IndentationLeadingAttribute;
        /// <summary>Trailing indentation in points as a double.</summary>
        public static readonly AutomationTextAttribute IndentationTrailingAttribute = TextPatternIdentifiers.IndentationTrailingAttribute;
        /// <summary>Is the text hidden?  Boolean.</summary>
        public static readonly AutomationTextAttribute IsHiddenAttribute = TextPatternIdentifiers.IsHiddenAttribute;
        /// <summary>Is the character italicized?  Boolean.</summary>
        public static readonly AutomationTextAttribute IsItalicAttribute = TextPatternIdentifiers.IsItalicAttribute;
        /// <summary>Is the character read-only? If a document/file is read-only, but you can still edit it and save it as another file, 
        /// the text inside is considered not read-only.  Boolean.</summary>
        public static readonly AutomationTextAttribute IsReadOnlyAttribute = TextPatternIdentifiers.IsReadOnlyAttribute;
        /// <summary>Is the character a sub-script? Boolean.</summary>
        public static readonly AutomationTextAttribute IsSubscriptAttribute = TextPatternIdentifiers.IsSubscriptAttribute;
        /// <summary>Is the character a super-script?  Boolean.</summary>
        public static readonly AutomationTextAttribute IsSuperscriptAttribute = TextPatternIdentifiers.IsSuperscriptAttribute;
        /// <summary>Bottom margin in points as a double.</summary>
        public static readonly AutomationTextAttribute MarginBottomAttribute = TextPatternIdentifiers.MarginBottomAttribute;
        /// <summary>Leading margin in points as a double.</summary>
        public static readonly AutomationTextAttribute MarginLeadingAttribute = TextPatternIdentifiers.MarginLeadingAttribute;
        /// <summary>Top margin in points as a double.</summary>
        public static readonly AutomationTextAttribute MarginTopAttribute = TextPatternIdentifiers.MarginTopAttribute;
        /// <summary>Trailing margin in points as a double.</summary>
        public static readonly AutomationTextAttribute MarginTrailingAttribute = TextPatternIdentifiers.MarginTrailingAttribute;
        /// <summary>Outline style from OutlineStyles enum.</summary>
        public static readonly AutomationTextAttribute OutlineStylesAttribute = TextPatternIdentifiers.OutlineStylesAttribute;
        /// <summary>Color of the overline as a Win32 COLORREF. This attribute may not be available if the color is
        /// always the same as the foreground color.</summary>
        public static readonly AutomationTextAttribute OverlineColorAttribute = TextPatternIdentifiers.OverlineColorAttribute;
        /// <summary>Overline style from TextDecorationLineStyle enum.</summary>
        public static readonly AutomationTextAttribute OverlineStyleAttribute = TextPatternIdentifiers.OverlineStyleAttribute;
        /// <summary>Color of the strikethrough as a Win32 COLORREF. This attribute may not be available if the color is
        /// always the same as the foreground color.</summary>
        public static readonly AutomationTextAttribute StrikethroughColorAttribute = TextPatternIdentifiers.StrikethroughColorAttribute;
        /// <summary>Strikethrough style from TextDecorationLineStyle enum.</summary>
        public static readonly AutomationTextAttribute StrikethroughStyleAttribute = TextPatternIdentifiers.StrikethroughStyleAttribute;
        /// <summary>The set of tabs in points relative to the leading margin. Array of double.</summary>
        public static readonly AutomationTextAttribute TabsAttribute = TextPatternIdentifiers.TabsAttribute;
        /// <summary>Text flow direction from FlowDirection flags enum.</summary>
        public static readonly AutomationTextAttribute TextFlowDirectionsAttribute = TextPatternIdentifiers.TextFlowDirectionsAttribute;
        /// <summary>Color of the underline as a Win32 COLORREF. This attribute may not be available if the color is
        /// always the same as the foreground color.</summary>
        public static readonly AutomationTextAttribute UnderlineColorAttribute = TextPatternIdentifiers.UnderlineColorAttribute;
        /// <summary>Underline style from TextDecorationLineStyle enum.</summary>
        public static readonly AutomationTextAttribute UnderlineStyleAttribute = TextPatternIdentifiers.UnderlineStyleAttribute;

        #endregion TextAttribute

        #region Patterns & Events

        /// <summary>Text pattern</summary>

        public static readonly AutomationPattern Pattern = TextPatternIdentifiers.Pattern;

        /// <summary>
        /// Event ID: TextSelectionChangedEvent
        /// When:  Sent to the event handler when the selection changes. 
        /// </summary>

        public static readonly AutomationEvent TextSelectionChangedEvent =
            TextPatternIdentifiers.TextSelectionChangedEvent;

        /// <summary>
        /// Event ID: TextChangedEvent
        /// When:  Sent to the event handler when text changes. 
        /// </summary>

        public static readonly AutomationEvent TextChangedEvent =
            TextPatternIdentifiers.TextChangedEvent;

        #endregion Patterns & Events

        #endregion Public Constants and Readonly Fields

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Retrieves the current selection.
        /// </summary>
        /// <returns>The range of text that is selected, or possibly null if there is
        /// no selection.</returns>
        public TextPatternRange [] GetSelection()
        {
            SafeTextRangeHandle [] hTextRanges = UiaCoreApi.TextPattern_GetSelection(_hPattern);
            return TextPatternRange.Wrap(hTextRanges, this);
        }

        /// <summary>
        /// Retrieves the visible range within the container
        /// </summary>
        /// <returns>The range of text that is visible within the container.  Note that the
        /// text of the range could be obscured by an overlapping window.  Also, portions
        /// of the range at the beginning, in the middle, or at the end may not be visible
        /// because they are scrolled off to the side.</returns>
        public TextPatternRange [] GetVisibleRanges()
        {
            SafeTextRangeHandle [] hTextRanges = UiaCoreApi.TextPattern_GetVisibleRanges(_hPattern);
            return TextPatternRange.Wrap(hTextRanges, this);
        }

        /// <summary>
        /// Retrieves the range of a child object.
        /// </summary>
        /// <param name="childElement">The child element.  If the element is not
        /// a child of the text container then an InvalidOperation exception is 
        /// thrown.</param>
        /// <returns>A range that spans the child element.</returns>
        public TextPatternRange RangeFromChild(AutomationElement childElement)
        {
            if (childElement == null)
            {
                throw new ArgumentNullException("childElement");
            }
            SafeTextRangeHandle hTextRange = UiaCoreApi.TextPattern_RangeFromChild(_hPattern, childElement.RawNode);
            return TextPatternRange.Wrap(hTextRange, this);
        }
        /// <summary>
        /// Finds the range nearest to a screen coordinate.
        /// If the coordinate is within the bounding rectangle of a character then the
        /// range will contain that character.  Otherwise, it will be a degenerate
        /// range near the point, chosen in an implementation-dependent manner.
        /// An InvalidOperation exception is thrown if the point is outside of the
        /// client area of the text container.
        /// </summary>
        /// <param name="screenLocation">The location in screen coordinates.</param>
        /// <returns>A degenerate range nearest the specified location.</returns>
        public TextPatternRange RangeFromPoint(Point screenLocation)
        {
            //If we are not within the client area throw an exception
            Rect rect = (Rect)_element.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
            if (screenLocation.X < rect.Left || screenLocation.X >= rect.Right || screenLocation.Y < rect.Top || screenLocation.Y >= rect.Bottom)
            {
                throw new ArgumentException(SR.Get(SRID.ScreenCoordinatesOutsideBoundingRect));
            }

            SafeTextRangeHandle hTextRange = UiaCoreApi.TextPattern_RangeFromPoint(_hPattern, screenLocation);
            return TextPatternRange.Wrap(hTextRange, this);
        }

        #endregion Public Methods
        
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// A text range that encloses the main text of the document.  Some auxillary text such as 
        /// headers, footnotes, or annotations may not be included. 
        /// </summary>
        public TextPatternRange DocumentRange
        { 
            get
            {
                SafeTextRangeHandle hTextRange = UiaCoreApi.TextPattern_get_DocumentRange(_hPattern);
                return TextPatternRange.Wrap(hTextRange, this);
            }
        }

        /// <summary>
        /// True if the text container supports text selection. If it does then
        /// you may use the GetSelection and TextPatternRange.Select methods.
        /// </summary>
        public SupportedTextSelection SupportedTextSelection
        {
            get
            {
                return UiaCoreApi.TextPattern_get_SupportedTextSelection(_hPattern);
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods
        static internal object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            if (hPattern.IsInvalid)
            {
                throw new InvalidOperationException(SR.Get(SRID.CantPrefetchTextPattern));
            }

            return new TextPattern(el, hPattern);
        }

        // compare two text patterns and return true if they are from the same logical element.
        static internal bool Compare(TextPattern t1, TextPattern t2)
        {
            return Misc.Compare(t1._element, t2._element);
        }

        #endregion Internal Methods
        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private SafePatternHandle _hPattern;
        private AutomationElement _element;

        #endregion Private Fields
    }
}



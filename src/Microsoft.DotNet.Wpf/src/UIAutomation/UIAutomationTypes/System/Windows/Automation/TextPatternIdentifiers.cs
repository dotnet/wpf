// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for Text pattern

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MS.Internal.Automation;


namespace System.Windows.Automation
{
    /// <summary>
    /// SupportedTextSelection indicates whether the document
    /// support simple single-span selection, multiple selections,
    /// or no selection.
    ///</summary>
    [Flags]
    [ComVisible(true)]
    [Guid("3d9e3d8f-bfb0-484f-84ab-93ff4280cbc4")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum SupportedTextSelection
#else
    public enum SupportedTextSelection
#endif
    {
        /// <summary>None.</summary>
        None = 0,
        /// <summary>Single.</summary>
        Single = 1,
        /// <summary>Multiple.</summary>
        Multiple = 2,
    }

    /// <summary>
    /// Purpose:
    ///     The TextPattern object is what you get back when you ask an element for text pattern. 
    /// Example usages:
    ///     It is the Interface that represents text like an edit control. This pretty 
    ///     much means any UI elements that contain text.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class TextPatternIdentifiers
#else
    public static class TextPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>
        /// Indicates that a text attribute varies over a range.
        /// </summary>
        public static readonly object MixedAttributeValue = UiaCoreTypesApi.UiaGetReservedMixedAttributeValue();

        #region TextAttribute

        // IMPORTANT: if you add or remove AutomationTextAttributes be sure to make the corresponding changes in 
        // AutomationComInteropProvider.cs' AutomationConstants struct and AutomationComInteropProvider.InitializeConstants.

        /// <summary>Type of animation applied from AnimationStyle enum. </summary>
        public static readonly AutomationTextAttribute AnimationStyleAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.AnimationStyle, "TextPatternIdentifiers.AnimationStyleAttribute");
        /// <summary>Background color as a 32-bit Win32 COLORREF.</summary>
        public static readonly AutomationTextAttribute BackgroundColorAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.BackgroundColor, "TextPatternIdentifiers.BackgroundColorAttribute");
        /// <summary>Bullet style from BulletStyle enum. </summary>
        public static readonly AutomationTextAttribute BulletStyleAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.BulletStyle, "TextPatternIdentifiers.BulletStyleAttribute");
        /// <summary>Cap style from CapStyle enum.</summary>
        public static readonly AutomationTextAttribute CapStyleAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.CapStyle, "TextPatternIdentifiers.CapStyleAttribute");
        /// <summary>CultureInfo of the character down to sub-language level, e.g. Swiss French instead of French. See the CultureInfo in 
        /// .NET Framework for more detail on the language code format. Clients should note that there may be many cases where the 
        /// Language of the character defaults to application UI language because many servers do not support language tag and, 
        /// even when supported, authors may not use it.</summary>
        public static readonly AutomationTextAttribute CultureAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.Culture, "TextPatternIdentifiers.CultureAttribute");
        /// <summary>Non-localized string that represents font face in TrueType. Providers can supply their own. 
        /// Examples include "Arial Black" and "Arial Narrow". </summary>
        public static readonly AutomationTextAttribute FontNameAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.FontName, "TextPatternIdentifiers.FontNameAttribute");
        /// <summary>Point size of the character as a double.</summary>
        public static readonly AutomationTextAttribute FontSizeAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.FontSize, "TextPatternIdentifiers.FontSizeAttribute");
        /// <summary>Thickness of font as an int. This is modeled after the lfWeight field in GDI LOGFONT. For consistency, the following values 
        /// have been adopted from LOGFONT:0=DontCare, 100=Thin, 200=ExtraLight or UltraLight, 300=Light, 400=Normal or Regular, 
        /// 500=Medium, 600=SemiBold or DemiBold, 700=Bold, 800=ExtraBold or UltraBold, and 900=Heavy or Black.</summary>
        public static readonly AutomationTextAttribute FontWeightAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.FontWeight, "TextPatternIdentifiers.FontWeightAttribute");
        /// <summary>Color of the text as a 32-bit Win32 COLORREF.</summary>
        public static readonly AutomationTextAttribute ForegroundColorAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.ForegroundColor, "TextPatternIdentifiers.ForegroundColorAttribute");
        /// <summary>Horizontal alignment from HorizontalTextAlignment enum. </summary>
        public static readonly AutomationTextAttribute HorizontalTextAlignmentAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.HorizontalTextAlignment, "TextPatternIdentifiers.HorizontalTextAlignmentAttribute");
        /// <summary>First-line indentation in points as a double.</summary>
        public static readonly AutomationTextAttribute IndentationFirstLineAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.IndentationFirstLine, "TextPatternIdentifiers.IndentationFirstLineAttribute");
        /// <summary>Leading indentation in points as a double.</summary>
        public static readonly AutomationTextAttribute IndentationLeadingAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.IndentationLeading, "TextPatternIdentifiers.IndentationLeadingAttribute");
        /// <summary>Trailing indentation in points as a double.</summary>
        public static readonly AutomationTextAttribute IndentationTrailingAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.IndentationTrailing, "TextPatternIdentifiers.IndentationTrailingAttribute");
        /// <summary>Is the text hidden?  Boolean.</summary>
        public static readonly AutomationTextAttribute IsHiddenAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.IsHidden, "TextPatternIdentifiers.IsHiddenAttribute");
        /// <summary>Is the character italicized?  Boolean.</summary>
        public static readonly AutomationTextAttribute IsItalicAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.IsItalic, "TextPatternIdentifiers.IsItalicAttribute");
        /// <summary>Is the character read-only? If a document/file is read-only, but you can still edit it and save it as another file, 
        /// the text inside is considered not read-only.  Boolean.</summary>
        public static readonly AutomationTextAttribute IsReadOnlyAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.IsReadOnly, "TextPatternIdentifiers.IsReadOnlyAttribute");
        /// <summary>Is the character a sub-script? Boolean.</summary>
        public static readonly AutomationTextAttribute IsSubscriptAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.IsSubscript, "TextPatternIdentifiers.IsSubscriptAttribute");
        /// <summary>Is the character a super-script?  Boolean.</summary>
        public static readonly AutomationTextAttribute IsSuperscriptAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.IsSuperscript, "TextPatternIdentifiers.IsSuperscriptAttribute");
        /// <summary>Bottom margin in points as a double.</summary>
        public static readonly AutomationTextAttribute MarginBottomAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.MarginBottom, "TextPatternIdentifiers.MarginBottomAttribute");
        /// <summary>Leading margin in points as a double.</summary>
        public static readonly AutomationTextAttribute MarginLeadingAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.MarginLeading, "TextPatternIdentifiers.MarginLeadingAttribute");
        /// <summary>Top margin in points as a double.</summary>
        public static readonly AutomationTextAttribute MarginTopAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.MarginTop, "TextPatternIdentifiers.MarginTopAttribute");
        /// <summary>Trailing margin in points as a double.</summary>
        public static readonly AutomationTextAttribute MarginTrailingAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.MarginTrailing, "TextPatternIdentifiers.MarginTrailingAttribute");
        /// <summary>Outline style from OutlineStyles enum.</summary>
        public static readonly AutomationTextAttribute OutlineStylesAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.OutlineStyles, "TextPatternIdentifiers.OutlineStylesAttribute");
        /// <summary>Color of the overline as a Win32 COLORREF. This attribute may not be available if the color is
        /// always the same as the foreground color.</summary>
        public static readonly AutomationTextAttribute OverlineColorAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.OverlineColor, "TextPatternIdentifiers.OverlineColorAttribute");
        /// <summary>Overline style from TextDecorationLineStyle enum.</summary>
        public static readonly AutomationTextAttribute OverlineStyleAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.OverlineStyle, "TextPatternIdentifiers.OverlineStyleAttribute");
        /// <summary>Color of the strikethrough as a Win32 COLORREF. This attribute may not be available if the color is
        /// always the same as the foreground color.</summary>
        public static readonly AutomationTextAttribute StrikethroughColorAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.StrikethroughColor, "TextPatternIdentifiers.StrikethroughColorAttribute");
        /// <summary>Strikethrough style from TextDecorationLineStyle enum.</summary>
        public static readonly AutomationTextAttribute StrikethroughStyleAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.StrikethroughStyle, "TextPatternIdentifiers.StrikethroughStyleAttribute");
        /// <summary>The set of tabs in points relative to the leading margin. Array of double.</summary>
        public static readonly AutomationTextAttribute TabsAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.Tabs, "TextPatternIdentifiers.TabsAttribute");
        /// <summary>Text flow direction from FlowDirection flags enum.</summary>
        public static readonly AutomationTextAttribute TextFlowDirectionsAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.TextFlowDirections, "TextPatternIdentifiers.TextFlowDirectionsAttribute");
        /// <summary>Color of the underline as a Win32 COLORREF. This attribute may not be available if the color is
        /// always the same as the foreground color.</summary>
        public static readonly AutomationTextAttribute UnderlineColorAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.UnderlineColor, "TextPatternIdentifiers.UnderlineColorAttribute");
        /// <summary>Underline style from TextDecorationLineStyle enum.</summary>
        public static readonly AutomationTextAttribute UnderlineStyleAttribute = AutomationTextAttribute.Register(AutomationIdentifierConstants.TextAttributes.UnderlineStyle, "TextPatternIdentifiers.UnderlineStyleAttribute");

        #endregion TextAttribute

        #region Patterns & Events

        /// <summary>Text pattern</summary>

        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Text, "TextPatternIdentifiers.Pattern");

        /// <summary>
        /// Event ID: TextSelectionChangedEvent
        /// When:  Sent to the event handler when the selection changes. 
        /// </summary>

        public static readonly AutomationEvent TextSelectionChangedEvent =
            AutomationEvent.Register(AutomationIdentifierConstants.Events.Text_TextSelectionChanged, "TextPatternIdentifiers.TextSelectionChangedEvent");

        /// <summary>
        /// Event ID: TextChangedEvent
        /// When:  Sent to the event handler when text changes. 
        /// </summary>

        public static readonly AutomationEvent TextChangedEvent =
            AutomationEvent.Register(AutomationIdentifierConstants.Events.Text_TextChanged, "TextPatternIdentifiers.TextChangedEvent");

        #endregion Patterns & Events

        #endregion Public Constants and Readonly Fields
    }
}



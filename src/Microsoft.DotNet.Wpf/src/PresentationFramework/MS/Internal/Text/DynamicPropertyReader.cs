// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Helper methods to retrive dynami properties from
//              DependencyObjects.
//


using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Markup;
using MS.Internal.PtsHost;
using MS.Internal.Annotations.Component;

namespace MS.Internal.Text
{
    // ----------------------------------------------------------------------
    // Helper methods to retrive dynami properties from DependencyObjects.
    // ----------------------------------------------------------------------
    internal static class DynamicPropertyReader
    {
        // ------------------------------------------------------------------
        //
        // Property Groups
        //
        // ------------------------------------------------------------------

        #region Property Groups

        // ------------------------------------------------------------------
        // Retrieve typeface properties from specified element.
        // ------------------------------------------------------------------
        internal static Typeface GetTypeface(DependencyObject element)
        {
            Debug.Assert(element != null);

            FontFamily  fontFamily  = (FontFamily)  element.GetValue(TextElement.FontFamilyProperty);
            FontStyle   fontStyle   = (FontStyle)   element.GetValue(TextElement.FontStyleProperty);
            FontWeight  fontWeight  = (FontWeight)  element.GetValue(TextElement.FontWeightProperty);
            FontStretch fontStretch = (FontStretch) element.GetValue(TextElement.FontStretchProperty);

            return new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
        }

        internal static Typeface GetModifiedTypeface(DependencyObject element, FontFamily fontFamily)
        {
            Debug.Assert(element != null);

            FontStyle   fontStyle   = (FontStyle)   element.GetValue(TextElement.FontStyleProperty);
            FontWeight  fontWeight  = (FontWeight)  element.GetValue(TextElement.FontWeightProperty);
            FontStretch fontStretch = (FontStretch) element.GetValue(TextElement.FontStretchProperty);

            return new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
        }

        // ------------------------------------------------------------------
        // Retrieve text properties from specified inline object.
        //
        // WORKAROUND: see PS task #13486 & #3399.
        // For inline object go to its parent and retrieve text decoration
        // properties from there.
        // ------------------------------------------------------------------
        internal static TextDecorationCollection GetTextDecorationsForInlineObject(DependencyObject element, TextDecorationCollection textDecorations)
        {
            Debug.Assert(element != null);

            DependencyObject parent = LogicalTreeHelper.GetParent(element);
            TextDecorationCollection parentTextDecorations = null;

            if (parent != null)
            {
                // Get parent text decorations if it is non-null
                parentTextDecorations = GetTextDecorations(parent);
            }

            // see if the two text decorations are equal.
            bool textDecorationsEqual = (textDecorations == null) ?
                                         parentTextDecorations == null
                                       : textDecorations.ValueEquals(parentTextDecorations);

            if (!textDecorationsEqual)
            {
                if (parentTextDecorations == null)
                {
                    textDecorations = null;
                }
                else
                {
                    textDecorations = new TextDecorationCollection();
                    int count = parentTextDecorations.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        textDecorations.Add(parentTextDecorations[i]);
                    }
                }
            }
            return textDecorations;
        }

        /// <summary>
        /// Helper method to get a TextDecorations property value. It returns null (instead of empty collection)
        /// when the property is not set on the given DO. 
        /// </summary>
        internal static TextDecorationCollection GetTextDecorations(DependencyObject element)
        {
            return GetCollectionValue(element, Inline.TextDecorationsProperty) as TextDecorationCollection;
        }

        /// <summary>
        /// Helper method to get a TextEffects property value. It returns null (instead of empty collection)
        /// when the property is not set on the given DO. 
        /// </summary>
        internal static TextEffectCollection GetTextEffects(DependencyObject element)
        {
            return GetCollectionValue(element, TextElement.TextEffectsProperty) as TextEffectCollection;
        }

        /// <summary>
        /// Helper method to get a collection property value. It returns null (instead of empty collection)
        /// when the property is not set on the given DO. 
        /// </summary>
        /// <remarks>
        /// Property system's GetValue() call creates a mutable empty collection when the property is accessed for the first time. 
        /// To avoids workingset overhead of those empty collections, we return null instead.  
        /// </remarks>
        private static object GetCollectionValue(DependencyObject element, DependencyProperty property)
        {
            bool hasModifiers; 
            if (element.GetValueSource(property, null, out hasModifiers)
                != BaseValueSourceInternal.Default || hasModifiers)
            {
                return element.GetValue(property);
            }

            return null;              
        }        

        #endregion Property Groups

        // ------------------------------------------------------------------
        //
        // Block Properties
        //
        // ------------------------------------------------------------------

        #region Block Properties

        // ------------------------------------------------------------------
        // GetKeepTogether
        // ------------------------------------------------------------------
        internal static bool GetKeepTogether(DependencyObject element)
        {
            Paragraph p = element as Paragraph;
            return (p != null) ? p.KeepTogether : false;
        }

        // ------------------------------------------------------------------
        // GetKeepWithNext
        // ------------------------------------------------------------------
        internal static bool GetKeepWithNext(DependencyObject element)
        {
            Paragraph p = element as Paragraph;
            return (p != null) ? p.KeepWithNext : false;
        }

        // ------------------------------------------------------------------
        // GetMinWidowLines
        // ------------------------------------------------------------------
        internal static int GetMinWidowLines(DependencyObject element)
        {
            Paragraph p = element as Paragraph;
            return (p != null) ? p.MinWidowLines : 0;
        }

        // ------------------------------------------------------------------
        // GetMinOrphanLines
        // ------------------------------------------------------------------
        internal static int GetMinOrphanLines(DependencyObject element)
        {
            Paragraph p = element as Paragraph;
            return (p != null) ? p.MinOrphanLines : 0;
        }

        #endregion Block Properties

        // ------------------------------------------------------------------
        //
        // Misc Properties
        //
        // ------------------------------------------------------------------

        #region Misc Properties


        /// <summary>
        /// Gets actual value of LineHeight property. If LineHeight is Double.Nan, returns FontSize*FontFamily.LineSpacing
        /// </summary>
        internal static double GetLineHeightValue(DependencyObject d)
        {
            double lineHeight = (double)d.GetValue(Block.LineHeightProperty);
            // If LineHeight value is 'Auto', treat it as LineSpacing * FontSize.
            if (DoubleUtil.IsNaN(lineHeight))
            {
                FontFamily fontFamily = (FontFamily)d.GetValue(TextElement.FontFamilyProperty);
                double fontSize = (double)d.GetValue(TextElement.FontSizeProperty);
                lineHeight = fontFamily.LineSpacing * fontSize;
            }
            return Math.Max(TextDpi.MinWidth, Math.Min(TextDpi.MaxWidth, lineHeight));
        }

        // ------------------------------------------------------------------
        // Retrieve background brush property from specified element.
        // If 'element' is the same object as paragraph owner, ignore background
        // brush, because it is handled outside as paragraph's background.
        // NOTE: This method is only used to read background of text content.
        //
        //      element - Element associated with content. Passed only for
        //              performance reasons; it can be extracted from 'position'.
        //      paragraphOwner - Owner of paragraph (usually the parent of 'element').
        //
        // ------------------------------------------------------------------
        internal static Brush GetBackgroundBrush(DependencyObject element)
        {
            Debug.Assert(element != null);
            Brush backgroundBrush = null;

            // If 'element' is FrameworkElement, it is the host of the text content.
            // If 'element' is Block, the content is directly hosted by a block paragraph.
            // In such cases ignore background brush, because it is handled outside as paragraph's background.
            while (backgroundBrush == null && CanApplyBackgroundBrush(element))
            {
                backgroundBrush = (Brush)element.GetValue(TextElement.BackgroundProperty);
                Invariant.Assert(element is FrameworkContentElement);
                element = ((FrameworkContentElement)element).Parent;
            }
            return backgroundBrush;
        }

        // ------------------------------------------------------------------
        // Retrieve background brush property from specified UIElement.
        //
        //      position - Exact position of the content.
        // ------------------------------------------------------------------
        internal static Brush GetBackgroundBrushForInlineObject(StaticTextPointer position)
        {
            object selected;
            Brush backgroundBrush;

            Debug.Assert(!position.IsNull);

            selected = position.TextContainer.Highlights.GetHighlightValue(position, LogicalDirection.Forward, typeof(TextSelection));

            if (selected == DependencyProperty.UnsetValue)
            {
                backgroundBrush = (Brush)position.GetValue(TextElement.BackgroundProperty);
            }
            else
            {
                backgroundBrush = SelectionHighlightInfo.BackgroundBrush;
            }
            return backgroundBrush;
        }

        // ------------------------------------------------------------------
        // GetBaselineAlignment
        // ------------------------------------------------------------------
        internal static BaselineAlignment GetBaselineAlignment(DependencyObject element)
        {
            Inline i = element as Inline;
            BaselineAlignment baselineAlignment = (i != null) ? i.BaselineAlignment : BaselineAlignment.Baseline;

            // Walk up the tree to check if it inherits BaselineAlignment from a parent
            while (i != null && BaselineAlignmentIsDefault(i))
            {
                i = i.Parent as Inline;
            }

            if (i != null)
            {
                // Found an Inline with non-default baseline alignment
                baselineAlignment = i.BaselineAlignment;
            }
            return baselineAlignment;
        }

        // ------------------------------------------------------------------
        // GetBaselineAlignmentForInlineObject
        // ------------------------------------------------------------------
        internal static BaselineAlignment GetBaselineAlignmentForInlineObject(DependencyObject element)
        {
            return GetBaselineAlignment(LogicalTreeHelper.GetParent(element));
        }

        // ------------------------------------------------------------------
        // Retrieve CultureInfo property from specified element.
        // ------------------------------------------------------------------
        internal static CultureInfo GetCultureInfo(DependencyObject element)
        {
            XmlLanguage language = (XmlLanguage) element.GetValue(FrameworkElement.LanguageProperty);
            try
            {
                return language.GetSpecificCulture();
            }
            catch (InvalidOperationException)
            {
                // We default to en-US if no part of the language tag is recognized.
                return System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS;
            }
        }

        // ------------------------------------------------------------------
        // Retrieve Number substitution properties from given element
        // ------------------------------------------------------------------
        internal static NumberSubstitution GetNumberSubstitution(DependencyObject element)
        {
            NumberSubstitution numberSubstitution = new NumberSubstitution();

            numberSubstitution.CultureSource = (NumberCultureSource)element.GetValue(NumberSubstitution.CultureSourceProperty);
            numberSubstitution.CultureOverride = (CultureInfo)element.GetValue(NumberSubstitution.CultureOverrideProperty);
            numberSubstitution.Substitution = (NumberSubstitutionMethod)element.GetValue(NumberSubstitution.SubstitutionProperty);

            return numberSubstitution;
        }

        private static bool CanApplyBackgroundBrush(DependencyObject element)
        {
            // If 'element' is FrameworkElement, it is the host of the text content.
            // If 'element' is Block, the content is directly hosted by a block paragraph.
            // In such cases ignore background brush, because it is handled outside as paragraph's background.
            // We will only apply background on Inline elements that are not AnchoredBlocks.
            // NOTE: We ideally do not need the AnchoredBlock check because when walking up the content tree we should hit a block before
            // an AnchoredBlock. Leaving it in in case this helper is used for other purposes.
            if (!(element is Inline) || element is AnchoredBlock)
            {
                return false;
            }
            return true;
        }

        private static bool BaselineAlignmentIsDefault(DependencyObject element)
        {
            Invariant.Assert(element != null);

            bool hasModifiers;
            if (element.GetValueSource(Inline.BaselineAlignmentProperty, null, out hasModifiers)
                != BaseValueSourceInternal.Default || hasModifiers)
            {
                return false;
            }
            return true;
        }

        #endregion Misc Properties
    }
 }

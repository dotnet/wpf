// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Element representing text typogrpahy properties for
//              Text, FlowDocument, TextRange
//

using System.Windows.Threading;

using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Controls;
using System.ComponentModel;

using MS.Internal.Text;
using MS.Utility;

using System;

namespace System.Windows.Documents
{
    /// <summary>
    /// Provide access to typography porperties of element in syntax of Typography.xxx = yyy;
    /// Actual data is stored in the owner.
    /// </summary>
    public sealed class Typography
    {
        static private readonly Type _typeofThis = typeof(Typography);
        static private readonly Type _typeofBool = typeof(bool);
        
        #region Constructors

        /// <summary>
        /// Typography constructor.
        /// </summary>
        internal Typography(DependencyObject owner)
        {
            //there should be actual owner
            if(owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            _owner = owner;
        }

        // ------------------------------------------------------------------
        // Static ctor.
        // ------------------------------------------------------------------
        static Typography()
        {
            // Set typography properties to values that match those in property
            // definitions in Typography class.
            Default.SetStandardLigatures(true);
            Default.SetContextualAlternates(true);
            Default.SetContextualLigatures(true);
            Default.SetKerning(true);
        }

        #endregion Constructors

        #region Public properties

        /// <summary> StandardLigatures property </summary>
        public bool StandardLigatures
        {
            get { return (bool) _owner.GetValue(StandardLigaturesProperty); }
            set { _owner.SetValue(StandardLigaturesProperty, value); }
        }

        ///<summary> ContextualLigatures Property</summary>
        public bool ContextualLigatures
        {
            get { return (bool) _owner.GetValue(ContextualLigaturesProperty); }
            set { _owner.SetValue(ContextualLigaturesProperty, value); }
        }

        ///<summary> DiscretionaryLigatures Property</summary>
        public bool DiscretionaryLigatures
        {
            get { return (bool) _owner.GetValue(DiscretionaryLigaturesProperty); }
            set { _owner.SetValue(DiscretionaryLigaturesProperty, value); }
        }

        ///<summary> HistoricalLigatures Property</summary>
        public bool HistoricalLigatures
        {
            get { return (bool) _owner.GetValue(HistoricalLigaturesProperty); }
            set { _owner.SetValue(HistoricalLigaturesProperty, value); }
        }

        ///<summary> AnnotationAlternates Property</summary>
        public int AnnotationAlternates
        {
            get { return (int) _owner.GetValue(AnnotationAlternatesProperty); }
            set { _owner.SetValue(AnnotationAlternatesProperty, value); }
        }

        ///<summary> ContextualAlternates Property</summary>
        public bool ContextualAlternates
        {
            get { return (bool) _owner.GetValue(ContextualAlternatesProperty); }
            set { _owner.SetValue(ContextualAlternatesProperty, value); }
        }

        ///<summary> HistoricalForms Property</summary>
        public bool HistoricalForms
        {
            get { return (bool) _owner.GetValue(HistoricalFormsProperty); }
            set { _owner.SetValue(HistoricalFormsProperty, value); }
        }

        ///<summary> Kerning Property</summary>
        public bool Kerning
        {
            get { return (bool) _owner.GetValue(KerningProperty); }
            set { _owner.SetValue(KerningProperty, value); }
        }

        ///<summary> CapitalSpacing Property</summary>
        public bool CapitalSpacing
        {
            get { return (bool) _owner.GetValue(CapitalSpacingProperty); }
            set { _owner.SetValue(CapitalSpacingProperty, value); }
        }

        ///<summary> CaseSensitiveForms Property</summary>
        public bool CaseSensitiveForms
        {
            get { return (bool) _owner.GetValue(CaseSensitiveFormsProperty); }
            set { _owner.SetValue(CaseSensitiveFormsProperty, value); }
        }

        ///<summary> StylisticSet1 Property</summary>
        public bool StylisticSet1
        {
            get { return (bool) _owner.GetValue(StylisticSet1Property); }
            set { _owner.SetValue(StylisticSet1Property, value); }
        }

        ///<summary> StylisticSet2 Property</summary>
        public bool StylisticSet2
        {
            get { return (bool) _owner.GetValue(StylisticSet2Property); }
            set { _owner.SetValue(StylisticSet2Property, value); }
        }

        ///<summary> StylisticSet3 Property</summary>
        public bool StylisticSet3
        {
            get { return (bool) _owner.GetValue(StylisticSet3Property); }
            set { _owner.SetValue(StylisticSet3Property, value); }
        }

        ///<summary> StylisticSet4 Property</summary>
        public bool StylisticSet4
        {
            get { return (bool) _owner.GetValue(StylisticSet4Property); }
            set { _owner.SetValue(StylisticSet4Property, value); }
        }

        ///<summary> StylisticSet5 Property</summary>
        public bool StylisticSet5
        {
            get { return (bool) _owner.GetValue(StylisticSet5Property); }
            set { _owner.SetValue(StylisticSet5Property, value); }
        }

        ///<summary> StylisticSet6 Property</summary>
        public bool StylisticSet6
        {
            get { return (bool) _owner.GetValue(StylisticSet6Property); }
            set { _owner.SetValue(StylisticSet6Property, value); }
        }

        ///<summary> StylisticSet7 Property</summary>
        public bool StylisticSet7
        {
            get { return (bool) _owner.GetValue(StylisticSet7Property); }
            set { _owner.SetValue(StylisticSet7Property, value); }
        }

        ///<summary> StylisticSet8 Property</summary>
        public bool StylisticSet8
        {
            get { return (bool) _owner.GetValue(StylisticSet8Property); }
            set { _owner.SetValue(StylisticSet8Property, value); }
        }

        ///<summary> StylisticSet9 Property</summary>
        public bool StylisticSet9
        {
            get { return (bool) _owner.GetValue(StylisticSet9Property); }
            set { _owner.SetValue(StylisticSet9Property, value); }
        }

        ///<summary> StylisticSet10 Property</summary>
        public bool StylisticSet10
        {
            get { return (bool) _owner.GetValue(StylisticSet10Property); }
            set { _owner.SetValue(StylisticSet10Property, value); }
        }

        ///<summary> StylisticSet11 Property</summary>
        public bool StylisticSet11
        {
            get { return (bool) _owner.GetValue(StylisticSet11Property); }
            set { _owner.SetValue(StylisticSet11Property, value); }
        }

        ///<summary> StylisticSet12 Property</summary>
        public bool StylisticSet12
        {
            get { return (bool) _owner.GetValue(StylisticSet12Property); }
            set { _owner.SetValue(StylisticSet12Property, value); }
        }

        ///<summary> StylisticSet13 Property</summary>
        public bool StylisticSet13
        {
            get { return (bool) _owner.GetValue(StylisticSet13Property); }
            set { _owner.SetValue(StylisticSet13Property, value); }
        }

        ///<summary> StylisticSet14 Property</summary>
        public bool StylisticSet14
        {
            get { return (bool) _owner.GetValue(StylisticSet14Property); }
            set { _owner.SetValue(StylisticSet14Property, value); }
        }

        ///<summary> StylisticSet15 Property</summary>
        public bool StylisticSet15
        {
            get { return (bool) _owner.GetValue(StylisticSet15Property); }
            set { _owner.SetValue(StylisticSet15Property, value); }
        }

        ///<summary> StylisticSet16 Property</summary>
        public bool StylisticSet16
        {
            get { return (bool) _owner.GetValue(StylisticSet16Property); }
            set { _owner.SetValue(StylisticSet16Property, value); }
        }

        ///<summary> StylisticSet17 Property</summary>
        public bool StylisticSet17
        {
            get { return (bool) _owner.GetValue(StylisticSet17Property); }
            set { _owner.SetValue(StylisticSet17Property, value); }
        }

        ///<summary> StylisticSet18 Property</summary>
        public bool StylisticSet18
        {
            get { return (bool) _owner.GetValue(StylisticSet18Property); }
            set { _owner.SetValue(StylisticSet18Property, value); }
        }

        ///<summary> StylisticSet19 Property</summary>
        public bool StylisticSet19
        {
            get { return (bool) _owner.GetValue(StylisticSet19Property); }
            set { _owner.SetValue(StylisticSet19Property, value); }
        }

        ///<summary> StylisticSet20 Property</summary>
        public bool StylisticSet20
        {
            get { return (bool) _owner.GetValue(StylisticSet20Property); }
            set { _owner.SetValue(StylisticSet20Property, value); }
        }

        ///<summary> Fraction Property</summary>
        public FontFraction Fraction
        {
            get { return (FontFraction) _owner.GetValue(FractionProperty); }
            set { _owner.SetValue(FractionProperty, value); }
        }

        ///<summary> SlashedZero Property</summary>
        public bool SlashedZero
        {
            get { return (bool) _owner.GetValue(SlashedZeroProperty); }
            set { _owner.SetValue(SlashedZeroProperty, value); }
        }

        ///<summary> MathematicalGreek Property</summary>
        public bool MathematicalGreek
        {
            get { return (bool) _owner.GetValue(MathematicalGreekProperty); }
            set { _owner.SetValue(MathematicalGreekProperty, value); }
        }

        ///<summary> EastAsianExpertForms Property</summary>
        public bool EastAsianExpertForms
        {
            get { return (bool) _owner.GetValue(EastAsianExpertFormsProperty); }
            set { _owner.SetValue(EastAsianExpertFormsProperty, value); }
        }

        ///<summary> Variants Property</summary>
        public FontVariants Variants
        {
            get { return (FontVariants) _owner.GetValue(VariantsProperty); }
            set { _owner.SetValue(VariantsProperty, value); }
        }

        ///<summary> Capitals Property</summary>
        public FontCapitals Capitals
        {
            get { return (FontCapitals) _owner.GetValue(CapitalsProperty); }
            set { _owner.SetValue(CapitalsProperty, value); }
        }

        ///<summary> NumeralStyle Property</summary>
        public FontNumeralStyle NumeralStyle
        {
            get { return (FontNumeralStyle) _owner.GetValue(NumeralStyleProperty); }
            set { _owner.SetValue(NumeralStyleProperty, value); }
        }

        ///<summary> NumeralAlignment Property</summary>
        public FontNumeralAlignment NumeralAlignment
        {
            get { return (FontNumeralAlignment) _owner.GetValue(NumeralAlignmentProperty); }
            set { _owner.SetValue(NumeralAlignmentProperty, value); }
        }

        ///<summary> EastAsianWidths Property</summary>
        public FontEastAsianWidths EastAsianWidths
        {
            get { return (FontEastAsianWidths) _owner.GetValue(EastAsianWidthsProperty); }
            set { _owner.SetValue(EastAsianWidthsProperty, value); }
        }

        ///<summary> EastAsianLanguage Property</summary>
        public FontEastAsianLanguage EastAsianLanguage
        {
            get { return (FontEastAsianLanguage) _owner.GetValue(EastAsianLanguageProperty); }
            set { _owner.SetValue(EastAsianLanguageProperty, value); }
        }

        ///<summary> StandardSwashes Property</summary>
        public int StandardSwashes
        {
            get { return (int) _owner.GetValue(StandardSwashesProperty); }
            set { _owner.SetValue(StandardSwashesProperty, value); }
        }

        ///<summary> ContextualSwashes Property</summary>
        public int ContextualSwashes
        {
            get { return (int) _owner.GetValue(ContextualSwashesProperty); }
            set { _owner.SetValue(ContextualSwashesProperty, value); }
        }

        ///<summary> StylisticAlternates Property</summary>
        public int StylisticAlternates
        {
            get { return (int) _owner.GetValue(StylisticAlternatesProperty); }
            set { _owner.SetValue(StylisticAlternatesProperty, value); }
        }

        #endregion Public properties

        #region Attached Properties Setters
 
        /// <summary>
        /// Writes the attached property StandardLigatures to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StandardLigaturesProperty" />
        public static void SetStandardLigatures(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StandardLigaturesProperty, value);
        }

        /// <summary>
        /// Returns the attached property StandardLigatures value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StandardLigaturesProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStandardLigatures(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StandardLigaturesProperty);
        }

        /// <summary>
        /// Writes the attached property ContextualLigatures to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.ContextualLigaturesProperty" />
        public static void SetContextualLigatures(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(ContextualLigaturesProperty, value);
        }

        /// <summary>
        /// Returns the attached property ContextualLigatures value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.ContextualLigaturesProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetContextualLigatures(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(ContextualLigaturesProperty);
        }

        /// <summary>
        /// Writes the attached property DiscretionaryLigatures to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.DiscretionaryLigaturesProperty" />
        public static void SetDiscretionaryLigatures(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(DiscretionaryLigaturesProperty, value);
        }

        /// <summary>
        /// Returns the attached property DiscretionaryLigatures value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.DiscretionaryLigaturesProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetDiscretionaryLigatures(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(DiscretionaryLigaturesProperty);
        }

        /// <summary>
        /// Writes the attached property HistoricalLigatures to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.HistoricalLigaturesProperty" />
        public static void SetHistoricalLigatures(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(HistoricalLigaturesProperty, value);
        }

        /// <summary>
        /// Returns the attached property HistoricalLigatures value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.HistoricalLigaturesProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetHistoricalLigatures(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(HistoricalLigaturesProperty);
        }

        /// <summary>
        /// Writes the attached property AnnotationAlternates to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.AnnotationAlternatesProperty" />
        public static void SetAnnotationAlternates(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(AnnotationAlternatesProperty, value);
        }

        /// <summary>
        /// Returns the attached property AnnotationAlternates value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.AnnotationAlternatesProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetAnnotationAlternates(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (int)element.GetValue(AnnotationAlternatesProperty);
        }

        /// <summary>
        /// Writes the attached property ContextualAlternates to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.ContextualAlternatesProperty" />
        public static void SetContextualAlternates(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(ContextualAlternatesProperty, value);
        }

        /// <summary>
        /// Returns the attached property ContextualAlternates value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.ContextualAlternatesProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetContextualAlternates(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(ContextualAlternatesProperty);
        }

        /// <summary>
        /// Writes the attached property HistoricalForms to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.HistoricalFormsProperty" />
        public static void SetHistoricalForms(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(HistoricalFormsProperty, value);
        }

        /// <summary>
        /// Returns the attached property HistoricalForms value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.HistoricalFormsProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetHistoricalForms(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(HistoricalFormsProperty);
        }

        /// <summary>
        /// Writes the attached property Kerning to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.KerningProperty" />
        public static void SetKerning(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(KerningProperty, value);
        }

        /// <summary>
        /// Returns the attached property Kerning value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.KerningProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetKerning(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(KerningProperty);
        }

        /// <summary>
        /// Writes the attached property CapitalSpacing to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.CapitalSpacingProperty" />
        public static void SetCapitalSpacing(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(CapitalSpacingProperty, value);
        }

        /// <summary>
        /// Returns the attached property CapitalSpacing value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.CapitalSpacingProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetCapitalSpacing(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(CapitalSpacingProperty);
        }

        /// <summary>
        /// Writes the attached property CaseSensitiveForms to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.CaseSensitiveFormsProperty" />
        public static void SetCaseSensitiveForms(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(CaseSensitiveFormsProperty, value);
        }

        /// <summary>
        /// Returns the attached property CaseSensitiveForms value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.CaseSensitiveFormsProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetCaseSensitiveForms(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(CaseSensitiveFormsProperty);
        }

        /// <summary>
        /// Writes the attached property StylisticSet1 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet1Property" />
        public static void SetStylisticSet1(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet1Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet1 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet1Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet1(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet1Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet2 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet2Property" />
        public static void SetStylisticSet2(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet2Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet2 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet2Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet2(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet2Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet3 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet3Property" />
        public static void SetStylisticSet3(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet3Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet3 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet3Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet3(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet3Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet4 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet4Property" />
        public static void SetStylisticSet4(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet4Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet4 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet4Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet4(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet4Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet5 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet5Property" />
        public static void SetStylisticSet5(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet5Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet5 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet5Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet5(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet5Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet6 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet6Property" />
        public static void SetStylisticSet6(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet6Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet6 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet6Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet6(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet6Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet7 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet7Property" />
        public static void SetStylisticSet7(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet7Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet7 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet7Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet7(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet7Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet8 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet8Property" />
        public static void SetStylisticSet8(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet8Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet8 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet8Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet8(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet8Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet9 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet9Property" />
        public static void SetStylisticSet9(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet9Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet9 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet9Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet9(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet9Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet10 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet10Property" />
        public static void SetStylisticSet10(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet10Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet10 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet10Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet10(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet10Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet11 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet11Property" />
        public static void SetStylisticSet11(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet11Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet11 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet11Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet11(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet11Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet12 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet12Property" />
        public static void SetStylisticSet12(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet12Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet12 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet12Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet12(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet12Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet13 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet13Property" />
        public static void SetStylisticSet13(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet13Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet13 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet13Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet13(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet13Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet14 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet14Property" />
        public static void SetStylisticSet14(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet14Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet14 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet14Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet14(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet14Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet15 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet15Property" />
        public static void SetStylisticSet15(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet15Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet15 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet15Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet15(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet15Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet16 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet16Property" />
        public static void SetStylisticSet16(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet16Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet16 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet16Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet16(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet16Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet17 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet17Property" />
        public static void SetStylisticSet17(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet17Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet17 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet17Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet17(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet17Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet18 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet18Property" />
        public static void SetStylisticSet18(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet18Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet18 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet18Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet18(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet18Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet19 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet19Property" />
        public static void SetStylisticSet19(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet19Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet19 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet19Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet19(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet19Property);
        }

        /// <summary>
        /// Writes the attached property StylisticSet20 to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticSet20Property" />
        public static void SetStylisticSet20(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticSet20Property, value);
        }

        /// <summary>
        /// Returns the attached property StylisticSet20 value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticSet20Property" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetStylisticSet20(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(StylisticSet20Property);
        }

        /// <summary>
        /// Writes the attached property Fraction to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.FractionProperty" />
        public static void SetFraction(DependencyObject element, FontFraction value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FractionProperty, value);
        }

        /// <summary>
        /// Returns the attached property Fraction value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.FractionProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static FontFraction GetFraction(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontFraction)element.GetValue(FractionProperty);
        }

        /// <summary>
        /// Writes the attached property SlashedZero to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.SlashedZeroProperty" />
        public static void SetSlashedZero(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(SlashedZeroProperty, value);
        }

        /// <summary>
        /// Returns the attached property SlashedZero value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.SlashedZeroProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetSlashedZero(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(SlashedZeroProperty);
        }

        /// <summary>
        /// Writes the attached property MathematicalGreek to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.MathematicalGreekProperty" />
        public static void SetMathematicalGreek(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(MathematicalGreekProperty, value);
        }

        /// <summary>
        /// Returns the attached property MathematicalGreek value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.MathematicalGreekProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetMathematicalGreek(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(MathematicalGreekProperty);
        }

        /// <summary>
        /// Writes the attached property EastAsianExpertForms to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.EastAsianExpertFormsProperty" />
        public static void SetEastAsianExpertForms(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(EastAsianExpertFormsProperty, value);
        }

        /// <summary>
        /// Returns the attached property EastAsianExpertForms value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.EastAsianExpertFormsProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetEastAsianExpertForms(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(EastAsianExpertFormsProperty);
        }

        /// <summary>
        /// Writes the attached property Variants to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.VariantsProperty" />
        public static void SetVariants(DependencyObject element, FontVariants value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(VariantsProperty, value);
        }

        /// <summary>
        /// Returns the attached property Variants value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.VariantsProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static FontVariants GetVariants(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontVariants)element.GetValue(VariantsProperty);
        }

        /// <summary>
        /// Writes the attached property Capitals to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.CapitalsProperty" />
        public static void SetCapitals(DependencyObject element, FontCapitals value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(CapitalsProperty, value);
        }

        /// <summary>
        /// Returns the attached property Capitals value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.CapitalsProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static FontCapitals GetCapitals(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontCapitals)element.GetValue(CapitalsProperty);
        }

        /// <summary>
        /// Writes the attached property NumeralStyle to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.NumeralStyleProperty" />
        public static void SetNumeralStyle(DependencyObject element, FontNumeralStyle value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(NumeralStyleProperty, value);
        }

        /// <summary>
        /// Returns the attached property NumeralStyle value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.NumeralStyleProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static FontNumeralStyle GetNumeralStyle(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontNumeralStyle)element.GetValue(NumeralStyleProperty);
        }

        /// <summary>
        /// Writes the attached property NumeralAlignment to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.NumeralAlignmentProperty" />
        public static void SetNumeralAlignment(DependencyObject element, FontNumeralAlignment value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(NumeralAlignmentProperty, value);
        }

        /// <summary>
        /// Returns the attached property NumeralAlignment value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.NumeralAlignmentProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static FontNumeralAlignment GetNumeralAlignment(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontNumeralAlignment)element.GetValue(NumeralAlignmentProperty);
        }

        /// <summary>
        /// Writes the attached property EastAsianWidths to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.EastAsianWidthsProperty" />
        public static void SetEastAsianWidths(DependencyObject element, FontEastAsianWidths value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(EastAsianWidthsProperty, value);
        }

        /// <summary>
        /// Returns the attached property EastAsianWidths value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.EastAsianWidthsProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static FontEastAsianWidths GetEastAsianWidths(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontEastAsianWidths)element.GetValue(EastAsianWidthsProperty);
        }

        /// <summary>
        /// Writes the attached property EastAsianLanguage to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.EastAsianLanguageProperty" />
        public static void SetEastAsianLanguage(DependencyObject element, FontEastAsianLanguage value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(EastAsianLanguageProperty, value);
        }

        /// <summary>
        /// Returns the attached property EastAsianLanguage value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.EastAsianLanguageProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static FontEastAsianLanguage GetEastAsianLanguage(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontEastAsianLanguage)element.GetValue(EastAsianLanguageProperty);
        }

        /// <summary>
        /// Writes the attached property StandardSwashes to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StandardSwashesProperty" />
        public static void SetStandardSwashes(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StandardSwashesProperty, value);
        }

        /// <summary>
        /// Returns the attached property StandardSwashes value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StandardSwashesProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetStandardSwashes(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (int)element.GetValue(StandardSwashesProperty);
        }

        /// <summary>
        /// Writes the attached property ContextualSwashes to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.ContextualSwashesProperty" />
        public static void SetContextualSwashes(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(ContextualSwashesProperty, value);
        }

        /// <summary>
        /// Returns the attached property ContextualSwashes value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.ContextualSwashesProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetContextualSwashes(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (int)element.GetValue(ContextualSwashesProperty);
        }

        /// <summary>
        /// Writes the attached property StylisticAlternates to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        /// <seealso cref="Typography.StylisticAlternatesProperty" />
        public static void SetStylisticAlternates(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(StylisticAlternatesProperty, value);
        }

        /// <summary>
        /// Returns the attached property StylisticAlternates value for the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <seealso cref="Typography.StylisticAlternatesProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetStylisticAlternates(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (int)element.GetValue(StylisticAlternatesProperty);
        }

        #endregion Attached Groperties Getters and Setters

        #region Dependency Properties

        /// <summary> StandardLigatures Property </summary>
        public static readonly DependencyProperty StandardLigaturesProperty =
                DependencyProperty.RegisterAttached(
                        "StandardLigatures", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                true,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> ContextualLigatures Property </summary>
        public static readonly DependencyProperty ContextualLigaturesProperty =
                DependencyProperty.RegisterAttached(
                        "ContextualLigatures", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                true,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> DiscretionaryLigatures Property </summary>
        public static readonly DependencyProperty DiscretionaryLigaturesProperty =
                DependencyProperty.RegisterAttached(
                        "DiscretionaryLigatures", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> HistoricalLigatures Property </summary>
        public static readonly DependencyProperty HistoricalLigaturesProperty =
                DependencyProperty.RegisterAttached(
                        "HistoricalLigatures", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> AnnotationAlternates Property </summary>
        public static readonly DependencyProperty AnnotationAlternatesProperty =
                DependencyProperty.RegisterAttached(
                        "AnnotationAlternates", 
                        typeof(int), 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                0,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> ContextualAlternates Property </summary>
        public static readonly DependencyProperty ContextualAlternatesProperty =
                DependencyProperty.RegisterAttached(
                        "ContextualAlternates", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                true,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> HistoricalForms Property </summary>
        public static readonly DependencyProperty HistoricalFormsProperty =
                DependencyProperty.RegisterAttached(
                        "HistoricalForms", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> Kerning Property </summary>
        public static readonly DependencyProperty KerningProperty =
                DependencyProperty.RegisterAttached(
                        "Kerning", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                true,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> CapitalSpacing Property </summary>
        public static readonly DependencyProperty CapitalSpacingProperty =
                DependencyProperty.RegisterAttached(
                        "CapitalSpacing", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> CaseSensitiveForms Property </summary>
        public static readonly DependencyProperty CaseSensitiveFormsProperty =
                DependencyProperty.RegisterAttached(
                        "CaseSensitiveForms", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet1 Property </summary>
        public static readonly DependencyProperty StylisticSet1Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet1", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet2 Property </summary>
        public static readonly DependencyProperty StylisticSet2Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet2", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet3 Property </summary>
        public static readonly DependencyProperty StylisticSet3Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet3", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet4 Property </summary>
        public static readonly DependencyProperty StylisticSet4Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet4", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet5 Property </summary>
        public static readonly DependencyProperty StylisticSet5Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet5", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet6 Property </summary>
        public static readonly DependencyProperty StylisticSet6Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet6", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet7 Property </summary>
        public static readonly DependencyProperty StylisticSet7Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet7", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet8 Property </summary>
        public static readonly DependencyProperty StylisticSet8Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet8", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet9 Property </summary>
        public static readonly DependencyProperty StylisticSet9Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet9", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet10 Property </summary>
        public static readonly DependencyProperty StylisticSet10Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet10", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet11 Property </summary>
        public static readonly DependencyProperty StylisticSet11Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet11", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet12 Property </summary>
        public static readonly DependencyProperty StylisticSet12Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet12", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet13 Property </summary>
        public static readonly DependencyProperty StylisticSet13Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet13", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet14 Property </summary>
        public static readonly DependencyProperty StylisticSet14Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet14", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet15 Property </summary>
        public static readonly DependencyProperty StylisticSet15Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet15", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet16 Property </summary>
        public static readonly DependencyProperty StylisticSet16Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet16", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet17 Property </summary>
        public static readonly DependencyProperty StylisticSet17Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet17", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet18 Property </summary>
        public static readonly DependencyProperty StylisticSet18Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet18", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet19 Property </summary>
        public static readonly DependencyProperty StylisticSet19Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet19", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticSet20 Property </summary>
        public static readonly DependencyProperty StylisticSet20Property =
                DependencyProperty.RegisterAttached(
                        "StylisticSet20", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> Fraction Property </summary>
        public static readonly DependencyProperty FractionProperty =
                DependencyProperty.RegisterAttached(
                        "Fraction", 
                        typeof(FontFraction), 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                FontFraction.Normal,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> SlashedZero Property </summary>
        public static readonly DependencyProperty SlashedZeroProperty =
                DependencyProperty.RegisterAttached(
                        "SlashedZero", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> MathematicalGreek Property </summary>
        public static readonly DependencyProperty MathematicalGreekProperty =
                DependencyProperty.RegisterAttached(
                        "MathematicalGreek", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> EastAsianExpertForms Property </summary>
        public static readonly DependencyProperty EastAsianExpertFormsProperty =
                DependencyProperty.RegisterAttached(
                        "EastAsianExpertForms", 
                        _typeofBool, 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> Variants Property </summary>
        public static readonly DependencyProperty VariantsProperty =
                DependencyProperty.RegisterAttached(
                        "Variants", 
                        typeof(FontVariants), 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                FontVariants.Normal,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> Capitals Property </summary>
        public static readonly DependencyProperty CapitalsProperty =
                DependencyProperty.RegisterAttached(
                        "Capitals", 
                        typeof(FontCapitals), 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                FontCapitals.Normal,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> NumeralStyle Property </summary>
        public static readonly DependencyProperty NumeralStyleProperty =
                DependencyProperty.RegisterAttached(
                        "NumeralStyle", 
                        typeof(FontNumeralStyle), 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                FontNumeralStyle.Normal,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> NumeralAlignment Property </summary>
        public static readonly DependencyProperty NumeralAlignmentProperty =
                DependencyProperty.RegisterAttached(
                        "NumeralAlignment", 
                        typeof(FontNumeralAlignment), 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                FontNumeralAlignment.Normal,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> EastAsianWidths Property </summary>
        public static readonly DependencyProperty EastAsianWidthsProperty =
                DependencyProperty.RegisterAttached(
                        "EastAsianWidths", 
                        typeof(FontEastAsianWidths), 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                FontEastAsianWidths.Normal,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> EastAsianLanguage Property </summary>
        public static readonly DependencyProperty EastAsianLanguageProperty =
                DependencyProperty.RegisterAttached(
                        "EastAsianLanguage", 
                        typeof(FontEastAsianLanguage), 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                FontEastAsianLanguage.Normal,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StandardSwashes Property </summary>
        public static readonly DependencyProperty StandardSwashesProperty =
                DependencyProperty.RegisterAttached(
                        "StandardSwashes", 
                        typeof(int), 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                0,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>  ContextualSwashes Property </summary>
        public static readonly DependencyProperty ContextualSwashesProperty =
                DependencyProperty.RegisterAttached(
                        "ContextualSwashes", 
                        typeof(int), 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                0,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary> StylisticAlternates Property </summary>
        public static readonly DependencyProperty StylisticAlternatesProperty =
                DependencyProperty.RegisterAttached(
                        "StylisticAlternates", 
                        typeof(int), 
                        _typeofThis,
                        new FrameworkPropertyMetadata(
                                0,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// List of properties in TypographyProperties group cache
        /// Actual metadata registered in FlowDocument, TextRange, Text elements
        /// </summary>
        internal static DependencyProperty[] TypographyPropertiesList =
                                            new DependencyProperty[] {
                                                    StandardLigaturesProperty,
                                                    ContextualLigaturesProperty,
                                                    DiscretionaryLigaturesProperty,
                                                    HistoricalLigaturesProperty,
                                                    AnnotationAlternatesProperty,
                                                    ContextualAlternatesProperty,
                                                    HistoricalFormsProperty,
                                                    KerningProperty,
                                                    CapitalSpacingProperty,
                                                    CaseSensitiveFormsProperty,
                                                    StylisticSet1Property,
                                                    StylisticSet2Property,
                                                    StylisticSet3Property,
                                                    StylisticSet4Property,
                                                    StylisticSet5Property,
                                                    StylisticSet6Property,
                                                    StylisticSet7Property,
                                                    StylisticSet8Property,
                                                    StylisticSet9Property,
                                                    StylisticSet10Property,
                                                    StylisticSet11Property,
                                                    StylisticSet12Property,
                                                    StylisticSet13Property,
                                                    StylisticSet14Property,
                                                    StylisticSet15Property,
                                                    StylisticSet16Property,
                                                    StylisticSet17Property,
                                                    StylisticSet18Property,
                                                    StylisticSet19Property,
                                                    StylisticSet20Property,
                                                    FractionProperty,
                                                    SlashedZeroProperty,
                                                    MathematicalGreekProperty,
                                                    EastAsianExpertFormsProperty,
                                                    VariantsProperty,
                                                    CapitalsProperty,
                                                    NumeralStyleProperty,
                                                    NumeralAlignmentProperty,
                                                    EastAsianWidthsProperty,
                                                    EastAsianLanguageProperty,
                                                    StandardSwashesProperty,
                                                    ContextualSwashesProperty,
                                                    StylisticAlternatesProperty
                                            };

        #endregion Dependency Properties

        // ------------------------------------------------------------------
        // Internal cache for default property groups.
        // ------------------------------------------------------------------
        internal static readonly TypographyProperties Default = new TypographyProperties();

        private DependencyObject _owner;
    }
}

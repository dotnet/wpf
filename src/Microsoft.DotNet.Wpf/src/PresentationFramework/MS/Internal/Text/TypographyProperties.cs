// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Typography properties. 
//


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace MS.Internal.Text
{
    /// <summary>
    /// Typography properties provider.
    /// </summary>
    internal sealed class TypographyProperties : TextRunTypographyProperties
    {
        /// <summary>
        /// Used as indexes to bitscale for boolean properties
        /// </summary>
        private enum PropertyId
        {
            /// <summary> StandardLigatures property </summary>
            StandardLigatures = 0,
            /// <summary> ContextualLigatures property </summary>
            ContextualLigatures = 1,
            /// <summary> DiscretionaryLigatures property </summary>
            DiscretionaryLigatures = 2,
            /// <summary> HistoricalLigatures property </summary>
            HistoricalLigatures = 3,
            /// <summary> CaseSensitiveForms property </summary>
            CaseSensitiveForms = 4,
            /// <summary> ContextualAlternates property </summary>
            ContextualAlternates = 5,
            /// <summary> HistoricalForms property </summary>
            HistoricalForms = 6,
            /// <summary> Kerning property </summary>
            Kerning = 7,
            /// <summary> CapitalSpacing property </summary>
            CapitalSpacing = 8,
            /// <summary> StylisticSet1 property </summary>
            StylisticSet1 = 9,
            /// <summary> StylisticSet2 property </summary>
            StylisticSet2 = 10,
            /// <summary> StylisticSet3 property </summary>
            StylisticSet3 = 11,
            /// <summary> StylisticSet4 property </summary>
            StylisticSet4 = 12,
            /// <summary> StylisticSet5 property </summary>
            StylisticSet5 = 13,
            /// <summary> StylisticSet6 property </summary>
            StylisticSet6 = 14,
            /// <summary> StylisticSet7 property </summary>
            StylisticSet7 = 15,
            /// <summary> StylisticSet8 property </summary>
            StylisticSet8 = 16,
            /// <summary> StylisticSet9 property </summary>
            StylisticSet9 = 17,
            /// <summary> StylisticSet10 property </summary>
            StylisticSet10 = 18,
            /// <summary> StylisticSet11 property </summary>
            StylisticSet11 = 19,
            /// <summary> StylisticSet12 property </summary>
            StylisticSet12 = 20,
            /// <summary> StylisticSet13 property </summary>
            StylisticSet13 = 21,
            /// <summary> StylisticSet14 property </summary>
            StylisticSet14 = 22,
            /// <summary> StylisticSet15 property </summary>
            StylisticSet15 = 23,
            /// <summary> StylisticSet16 property </summary>
            StylisticSet16 = 24,
            /// <summary> StylisticSet17 property </summary>
            StylisticSet17 = 25,
            /// <summary> StylisticSet18 property </summary>
            StylisticSet18 = 26,
            /// <summary> StylisticSet19 property </summary>
            StylisticSet19 = 27,
            /// <summary> StylisticSet20 property </summary>
            StylisticSet20 = 28,
            /// <summary> SlashedZero property </summary>
            SlashedZero = 29,
            /// <summary> MathematicalGreek property </summary>
            MathematicalGreek = 30,
            /// <summary> EastAsianExpertForms property </summary>
            EastAsianExpertForms = 31,
            /// <summary> 
            /// Total number of properties. Should not be >32. 
            /// Otherwise bitmask field _idPropertySetFlags should be changed to ulong 
            /// </summary>
            PropertyCount = 32
        }

        /// <summary>
        /// Create new typographyProperties with deefault values
        /// </summary>
        public TypographyProperties()
        {
            // Flags are stored in uint (32 bits).
            // Any way to check it at compile time?
            Debug.Assert((uint)PropertyId.PropertyCount <= 32);
            ResetProperties();
        }
 
        #region Public typography properties   
        /// <summary>
        /// 
        /// </summary>
        public override bool StandardLigatures
        {
            get { return IsBooleanPropertySet(PropertyId.StandardLigatures); }
        }

        public void SetStandardLigatures(bool value)
        {
            SetBooleanProperty(PropertyId.StandardLigatures, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool ContextualLigatures
        {
            get { return IsBooleanPropertySet(PropertyId.ContextualLigatures); }
        }

        public void SetContextualLigatures(bool value)
        {
            SetBooleanProperty(PropertyId.ContextualLigatures, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool DiscretionaryLigatures
        {
            get { return IsBooleanPropertySet(PropertyId.DiscretionaryLigatures); }
        }

        public void SetDiscretionaryLigatures(bool value)
        {
            SetBooleanProperty(PropertyId.DiscretionaryLigatures, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool HistoricalLigatures
        {
            get { return IsBooleanPropertySet(PropertyId.HistoricalLigatures); }
        }

        public void SetHistoricalLigatures(bool value)
        {
            SetBooleanProperty(PropertyId.HistoricalLigatures, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool CaseSensitiveForms
        {
            get { return IsBooleanPropertySet(PropertyId.CaseSensitiveForms); }
        }

        public void SetCaseSensitiveForms(bool value)
        {
            SetBooleanProperty(PropertyId.CaseSensitiveForms, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool ContextualAlternates
        {
            get { return IsBooleanPropertySet(PropertyId.ContextualAlternates); }
        }

        public void SetContextualAlternates(bool value)
        {
            SetBooleanProperty(PropertyId.ContextualAlternates, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool HistoricalForms
        {
            get { return IsBooleanPropertySet(PropertyId.HistoricalForms); }
        }

        public void SetHistoricalForms(bool value)
        {
            SetBooleanProperty(PropertyId.HistoricalForms, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool Kerning
        {
            get { return IsBooleanPropertySet(PropertyId.Kerning); }
        }

        public void SetKerning(bool value)
        {
            SetBooleanProperty(PropertyId.Kerning, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool CapitalSpacing
        {
            get { return IsBooleanPropertySet(PropertyId.CapitalSpacing); }
        }

        public void SetCapitalSpacing(bool value)
        {
            SetBooleanProperty(PropertyId.CapitalSpacing, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet1
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet1); }
        }

        public void SetStylisticSet1(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet1, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet2
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet2); }
        }

        public void SetStylisticSet2(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet2, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet3
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet3); }
        }

        public void SetStylisticSet3(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet3, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet4
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet4); }
        }

        public void SetStylisticSet4(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet4, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet5
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet5); }
        }

        public void SetStylisticSet5(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet5, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet6
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet6); }
        }

        public void SetStylisticSet6(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet6, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet7
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet7); }
        }

        public void SetStylisticSet7(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet7, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet8
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet8); }
        }

        public void SetStylisticSet8(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet8, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet9
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet9); }
        }

        public void SetStylisticSet9(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet9, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet10
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet10); }
        }

        public void SetStylisticSet10(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet10, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet11
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet11); }
        }

        public void SetStylisticSet11(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet11, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet12
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet12); }
        }

        public void SetStylisticSet12(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet12, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet13
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet13); }
        }

        public void SetStylisticSet13(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet13, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet14
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet14); }
        }

        public void SetStylisticSet14(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet14, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet15
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet15); }
        }

        public void SetStylisticSet15(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet15, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet16
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet16); }
        }

        public void SetStylisticSet16(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet16, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet17
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet17); }
        }

        public void SetStylisticSet17(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet17, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet18
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet18); }
        }

        public void SetStylisticSet18(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet18, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet19
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet19); }
        }

        public void SetStylisticSet19(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet19, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StylisticSet20
        {
            get { return IsBooleanPropertySet(PropertyId.StylisticSet20); }
        }

        public void SetStylisticSet20(bool value)
        {
            SetBooleanProperty(PropertyId.StylisticSet20, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override FontFraction Fraction
        {
            get { return _fraction; }
        }

        public void SetFraction(FontFraction value)
        {
            _fraction = value;
            OnPropertiesChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool SlashedZero
        {
            get { return IsBooleanPropertySet(PropertyId.SlashedZero); }
        }

        public void SetSlashedZero(bool value)
        {
            SetBooleanProperty(PropertyId.SlashedZero, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool MathematicalGreek
        {
            get { return IsBooleanPropertySet(PropertyId.MathematicalGreek); }
        }

        public void SetMathematicalGreek(bool value)
        {
            SetBooleanProperty(PropertyId.MathematicalGreek, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool EastAsianExpertForms
        {
            get { return IsBooleanPropertySet(PropertyId.EastAsianExpertForms); }
        }

        public void SetEastAsianExpertForms(bool value)
        {
            SetBooleanProperty(PropertyId.EastAsianExpertForms, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override FontVariants Variants
        {
            get { return _variant; }
        }

        public void SetVariants(FontVariants value)
        {
            _variant = value;
            OnPropertiesChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public override FontCapitals Capitals
        {
            get { return _capitals; }
        }

        public void SetCapitals(FontCapitals value)
        {
            _capitals = value;
            OnPropertiesChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public override FontNumeralStyle NumeralStyle
        {
            get { return _numeralStyle; }
        }

        public void SetNumeralStyle(FontNumeralStyle value)
        {
            _numeralStyle = value;
            OnPropertiesChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public override FontNumeralAlignment NumeralAlignment
        {
            get { return _numeralAlignment; }
        }

        public void SetNumeralAlignment(FontNumeralAlignment value)
        {
            _numeralAlignment = value;
            OnPropertiesChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public override FontEastAsianWidths EastAsianWidths
        {
            get { return _eastAsianWidths; }
        }

        public void SetEastAsianWidths(FontEastAsianWidths value)
        {
            _eastAsianWidths = value;
            OnPropertiesChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public override FontEastAsianLanguage EastAsianLanguage
        {
            get { return _eastAsianLanguage; }
        }

        public void SetEastAsianLanguage(FontEastAsianLanguage value)
        {
            _eastAsianLanguage = value;
            OnPropertiesChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public override int StandardSwashes
        {
            get { return _standardSwashes; }
        }

        public void SetStandardSwashes(int value)
        {
            _standardSwashes = value;
            OnPropertiesChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public override int ContextualSwashes
        {
            get { return _contextualSwashes; }
        }

        public void SetContextualSwashes(int value)
        {
            _contextualSwashes = value;
            OnPropertiesChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public override int StylisticAlternates
        {
            get { return _stylisticAlternates; }
        }

        public void SetStylisticAlternates(int value)
        {
            _stylisticAlternates = value;
            OnPropertiesChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public override int AnnotationAlternates
        {
            get { return _annotationAlternates; }
        }

        public void SetAnnotationAlternates(int value)
        {
            _annotationAlternates = value;
            OnPropertiesChanged();
        }

        #endregion Public typography properties
        
        /// <summary>
        /// Check whether two Property sets are equal
        /// </summary>
        /// <param name="other">property to compare</param>
        /// <returns></returns>
        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }

            if (this.GetType() != other.GetType())
            {
                return false;
            }

            TypographyProperties genericOther = (TypographyProperties)other;

            return  //This will cover all boolean properties
                _idPropertySetFlags == genericOther._idPropertySetFlags &&
                //And this will cover the rest
                _variant == genericOther._variant && 
                _capitals == genericOther._capitals && 
                _fraction == genericOther._fraction && 
                _numeralStyle == genericOther._numeralStyle && 
                _numeralAlignment == genericOther._numeralAlignment && 
                _eastAsianWidths == genericOther._eastAsianWidths && 
                _eastAsianLanguage == genericOther._eastAsianLanguage && 
                _standardSwashes == genericOther._standardSwashes && 
                _contextualSwashes == genericOther._contextualSwashes && 
                _stylisticAlternates == genericOther._stylisticAlternates &&
                _annotationAlternates == genericOther._annotationAlternates;
        }

        public override int GetHashCode()
        {
            return (int)(_idPropertySetFlags >> 32) ^ 
                   (int)(_idPropertySetFlags & 0xFFFFFFFF) ^ 
                   (int)_variant << 28 ^ 
                   (int)_capitals << 24 ^ 
                   (int)_numeralStyle << 20 ^ 
                   (int)_numeralAlignment << 18 ^ 
                   (int)_eastAsianWidths << 14 ^ 
                   (int)_eastAsianLanguage << 10 ^ 
                   (int)_standardSwashes << 6 ^ 
                   (int)_contextualSwashes << 2 ^ 
                   (int)_stylisticAlternates ^ 
                   (int)_fraction << 16 ^ 
                   (int)_annotationAlternates << 12;
        }

        public static bool operator ==(TypographyProperties first, TypographyProperties second)
        {
            //Need to cast to object to do null comparision.
            if (((object)first) == null) return (((object)second) == null);

            return first.Equals(second);
        }

        public static bool operator !=(TypographyProperties first, TypographyProperties second)
        {
            return !(first == second);
        }

        #region Private methods
        /// <summary>
        /// Set all properties to default value
        /// </summary>
        private void ResetProperties()
        {
            //clean flags
            _idPropertySetFlags = 0;

            //assign non-trivial(not bolean) values
            _standardSwashes = 0;
            _contextualSwashes = 0;
            _stylisticAlternates = 0;
            _annotationAlternates = 0;
            _variant = FontVariants.Normal;
            _capitals = FontCapitals.Normal;
            _numeralStyle = FontNumeralStyle.Normal;
            _numeralAlignment = FontNumeralAlignment.Normal;
            _eastAsianWidths = FontEastAsianWidths.Normal;
            _eastAsianLanguage = FontEastAsianLanguage.Normal;
            _fraction = FontFraction.Normal;
            OnPropertiesChanged();
        }

        /// <summary>
        /// Check whether boolean property is set to non-default value
        /// </summary>
        /// <param name="propertyId">PropertyId</param>
        /// <returns></returns>
        private bool IsBooleanPropertySet(PropertyId propertyId)
        {
            Debug.Assert((uint)propertyId < (uint)PropertyId.PropertyCount, "Invalid typography property id");

            uint flagMask = (uint)(((uint)1) << ((int)propertyId));

            return (_idPropertySetFlags & flagMask) != 0;
        }

        /// <summary>
        ///     Set/clean flag that property value is non-default
        ///     Used only internally to support quick checks while forming FeatureSet
        ///     
        /// </summary>
        /// <param name="propertyId">Property id</param>
        /// <param name="flagValue">Value of the flag</param>
        private void SetBooleanProperty(PropertyId propertyId, bool flagValue)
        {
            Debug.Assert((uint)propertyId < (uint)PropertyId.PropertyCount, "Invalid typography property id");

            uint flagMask = (uint)(((uint)1) << ((int)propertyId));

            if (flagValue)
                _idPropertySetFlags |= flagMask;
            else
                _idPropertySetFlags &= ~flagMask;

            OnPropertiesChanged();
        }

        private uint _idPropertySetFlags;

        private int _standardSwashes;

        private int _contextualSwashes;

        private int _stylisticAlternates;

        private int _annotationAlternates;

        private FontVariants _variant;

        private FontCapitals _capitals;

        private FontFraction _fraction;

        private FontNumeralStyle _numeralStyle;

        private FontNumeralAlignment _numeralAlignment;

        private FontEastAsianWidths _eastAsianWidths;

        private FontEastAsianLanguage _eastAsianLanguage;
        #endregion Private members
    }
}

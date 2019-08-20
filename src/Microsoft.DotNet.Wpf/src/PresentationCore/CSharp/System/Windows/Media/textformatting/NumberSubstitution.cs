// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Number substitution related types
//
//  Spec:      Cultural%20digit%20substitution.htm
//
//


using System;
using System.Globalization;
using System.ComponentModel;
using System.Windows;
using MS.Internal.FontCache; // for HashFn

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

// Allow suppression of presharp warnings
#pragma warning disable 1634, 1691

namespace System.Windows.Media
{
    /// <summary>
    /// The NumberSubstitution class specifies how numbers in text
    /// are to be displayed.
    /// </summary>
    public class NumberSubstitution
    {
        /// <summary>
        /// Initializes a NumberSubstitution object with default values.
        /// </summary>
        public NumberSubstitution()
        {
            _source = NumberCultureSource.Text;
            _cultureOverride = null;
            _substitution = NumberSubstitutionMethod.AsCulture;
        }

        /// <summary>
        /// Initializes a NumberSubstitution object with explicit values.
        /// </summary>
        /// <param name="source">Specifies how the number culture is determined.</param>
        /// <param name="cultureOverride">Number culture if NumberCultureSource.Override is specified.</param>
        /// <param name="substitution">Type of number substitution to perform.</param>
        public NumberSubstitution(
            NumberCultureSource source,
            CultureInfo cultureOverride,
            NumberSubstitutionMethod substitution)
        {
            _source = source;
            _cultureOverride = ThrowIfInvalidCultureOverride(cultureOverride);
            _substitution = substitution;
        }

        /// <summary>
        /// The CultureSource property specifies how the culture for numbers
        /// is determined. The default value is NumberCultureSource.Text,
        /// which means the number culture is the culture of the text run.
        /// </summary>
        public NumberCultureSource CultureSource
        {
            get { return _source; }

            set
            {
                if ((uint)value > (uint)NumberCultureSource.Override)
                    throw new InvalidEnumArgumentException("CultureSource", (int)value, typeof(NumberCultureSource));

                _source = value;
            }
        }

        /// <summary>
        /// If the CultureSource == NumberCultureSource.Override, this 
        /// property specifies the number culture. A value of null is interpreted 
        /// as US-English. The default value is null. If CultureSource != 
        /// NumberCultureSource.Override, this property is ignored.
        /// </summary>
        [TypeConverter(typeof(System.Windows.CultureInfoIetfLanguageTagConverter))]
        public CultureInfo CultureOverride
        {
            get { return _cultureOverride; }
            set { _cultureOverride = ThrowIfInvalidCultureOverride(value); }
        }

        /// <summary>
        /// Helper function to throw an exception if invalid value is specified for
        /// CultureOverride property.
        /// </summary>
        /// <param name="culture">Culture to validate.</param>
        /// <returns>The value of the culture parameter.</returns>
        private static CultureInfo ThrowIfInvalidCultureOverride(CultureInfo culture)
        {
            if (!IsValidCultureOverride(culture))
            {
                throw new ArgumentException(SR.Get(SRID.SpecificNumberCultureRequired));
            }
            return culture;
        }

        /// <summary>
        /// Determines whether the specific culture is a valid value for the 
        /// CultureOverride property.
        /// </summary>
        /// <param name="culture">Culture to validate.</param>
        /// <returns>Returns true if it's a valid CultureOverride, false if not.</returns>
        private static bool IsValidCultureOverride(CultureInfo culture)
        {
            // Null culture override is OK, but otherwise it must be a specific culture.
            return
                (culture == null) ||
                !(culture.IsNeutralCulture || culture.Equals(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Weakly typed validation callback for CultureOverride dependency property.
        /// </summary>
        /// <param name="value">CultureInfo object to validate; the type is assumed
        /// to be CultureInfo as the type is validated by the property engine.</param>
        /// <returns>Returns true if it's a valid culture, false if not.</returns>
        private static bool IsValidCultureOverrideValue(object value)
        {
            return IsValidCultureOverride((CultureInfo)value);
        }

        /// <summary>
        /// Specifies the type of number substitution to perform, if any.
        /// </summary>
        public NumberSubstitutionMethod Substitution
        {
            get { return _substitution; }

            set
            {
                if ((uint)value > (uint)NumberSubstitutionMethod.Traditional)
                    throw new InvalidEnumArgumentException("Substitution", (int)value, typeof(NumberSubstitutionMethod));

                _substitution = value;
            }
        }

        /// <summary>
        /// DP For CultureSource
        /// </summary>
        public static readonly DependencyProperty CultureSourceProperty =
                    DependencyProperty.RegisterAttached(
                        "CultureSource",
                        typeof(NumberCultureSource),
                        typeof(NumberSubstitution));

        /// <summary>
        /// Setter for NumberSubstitution DependencyProperty
        /// </summary>
        public static void SetCultureSource(DependencyObject target, NumberCultureSource value)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.SetValue(CultureSourceProperty, value);
        }

        /// <summary>
        /// Getter for NumberSubstitution DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static NumberCultureSource GetCultureSource(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return (NumberCultureSource)(target.GetValue(CultureSourceProperty));
        }

        /// <summary>
        /// DP For CultureOverride
        /// </summary>
        public static readonly DependencyProperty CultureOverrideProperty =
                    DependencyProperty.RegisterAttached(
                        "CultureOverride",
                        typeof(CultureInfo),
                        typeof(NumberSubstitution),
                        null, // default property metadata
                        new ValidateValueCallback(IsValidCultureOverrideValue)
                        );

        /// <summary>
        /// Setter for NumberSubstitution DependencyProperty
        /// </summary>
        public static void SetCultureOverride(DependencyObject target, CultureInfo value)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.SetValue(CultureOverrideProperty, value);
        }

        /// <summary>
        /// Getter for NumberSubstitution DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        [TypeConverter(typeof(System.Windows.CultureInfoIetfLanguageTagConverter))]
        public static CultureInfo GetCultureOverride(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return (CultureInfo)(target.GetValue(CultureOverrideProperty));
        }

        /// <summary>
        /// DP For Substitution
        /// </summary>
        public static readonly DependencyProperty SubstitutionProperty =
                    DependencyProperty.RegisterAttached(
                        "Substitution",
                        typeof(NumberSubstitutionMethod),
                        typeof(NumberSubstitution));

        /// <summary>
        /// Setter for NumberSubstitution DependencyProperty
        /// </summary>
        public static void SetSubstitution(DependencyObject target, NumberSubstitutionMethod value)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.SetValue(SubstitutionProperty, value);
        }

        /// <summary>
        /// Getter for NumberSubstitution DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static NumberSubstitutionMethod GetSubstitution(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return (NumberSubstitutionMethod)(target.GetValue(SubstitutionProperty));
        }

        /// <summary>
        /// Computes hash code for this object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            int hash = HashFn.HashMultiply((int)_source) + (int)_substitution;

            if (_cultureOverride != null)
                hash = HashFn.HashMultiply(hash) + _cultureOverride.GetHashCode();

            return HashFn.HashScramble(hash);
        }

        /// <summary>
        /// Checks whether this object is equal to another NumberSubstitution object.
        /// </summary>
        /// <param name="obj">Object to compare with.</param>
        /// <returns>Returns true if the specified object is a NumberSubstitution object with the
        /// same properties as this object, and false otherwise.</returns>
        public override bool Equals(object obj)
        {
            NumberSubstitution sub = obj as NumberSubstitution;

            // Suppress PRESharp warning that sub can be null; apparently PRESharp
            // doesn't understand short circuit evaluation of operator &&.
            #pragma warning disable 6506
            return sub != null &&
                _source == sub._source &&
                _substitution == sub._substitution &&
                (_cultureOverride == null ? (sub._cultureOverride == null) : (_cultureOverride.Equals(sub._cultureOverride)));
            #pragma warning restore 6506
        }

        private NumberCultureSource _source;
        private CultureInfo _cultureOverride;
        private NumberSubstitutionMethod _substitution;
    }


    /// <summary>
    /// Used with the NumberSubstitution class, specifies how the culture for
    /// numbers in a text run is determined.
    /// </summary>
    public enum NumberCultureSource
    {
        /// <summary>
        /// Number culture is TextRunProperties.CultureInfo, i.e., the culture
        /// of the text run. In markup, this is the xml:lang attribute.
        /// </summary>
        Text = 0,

        /// <summary>
        /// Number culture is the culture of the current thread, which by default
        /// is the user default culture.
        /// </summary>
        User = 1,

        /// <summary>
        /// Number culture is NumberSubstitution.CultureOverride.
        /// </summary>
        Override = 2
    }


    // Compatibility warning:
    // If we are ever to change the values of this enum we should revisit
    // wpf\src\Native\DWriteWrapper\TextAnalysisSource.h::ConvertNumberSubstitutionMethod()
    // Which assumes knowledge of the values and order of this enum.
    /// <summary>
    /// Value of the NumberSubstitution.Substitituion property, specifies
    /// the type of number substitution to perform, if any.
    /// </summary>
    public enum NumberSubstitutionMethod
    {
        /// <summary>
        /// Specifies that the substitution method should be determined based
        /// on the number culture's NumberFormat.DigitSubstitution property.
        /// This is the default value.
        /// </summary>
        AsCulture = 0,

        /// <summary>
        /// If the number culture is an Arabic or ---- culture, specifies that
        /// the digits depend on the context. Either traditional or Latin digits
        /// are used depending on the nearest preceding strong character or (if
        /// there is none) the text direction of the paragraph.
        /// </summary>
        Context = 1,

        /// <summary>
        /// Specifies that code points 0x30-0x39 are always rendered as European
        /// digits, i.e., no substitution is performed.
        /// </summary>
        European = 2,

        /// <summary>
        /// Specifies that numbers are rendered using the national digits for
        /// the number culture, as specified by the culture's NumberFormat
        /// property.
        /// </summary>
        NativeNational = 3,

        /// <summary>
        /// Specifies that numbers are rendered using the traditional digits
        /// for the number culture. For most cultures, this is the same as
        /// NativeNational. However, NativeNational results in Latin digits
        /// for some Arabic cultures, whereas this value results in Arabic
        /// digits for all Arabic cultures.
        /// </summary>
        Traditional = 4
    }
}


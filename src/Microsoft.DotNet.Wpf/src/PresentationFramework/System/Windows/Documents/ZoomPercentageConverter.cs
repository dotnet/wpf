// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the ZoomPercentageConverter: TypeConverter for the
//                ZoomPercentage property of DocumentViewer.
//

// Used to support the warnings disabled below
#pragma warning disable 1634, 1691

using System;
using System.Globalization;
using System.Windows.Data;

namespace System.Windows.Documents
{
/// <summary>
/// ValueConverter for DocumentViewer's ZoomPercentage property
/// </summary>
public sealed class ZoomPercentageConverter : IValueConverter
{
    internal const string ZoomPercentageConverterStringFormat = "{0:0.##}%";
    
    //------------------------------------------------------
    //
    //  Constructors
    //
    //------------------------------------------------------

    /// <summary>
    /// Instantiates a new instance of a ZoomPercentageConverter
    /// </summary>
    public ZoomPercentageConverter() {}

    //------------------------------------------------------
    //
    //  Public Methods
    //
    //------------------------------------------------------

    /// <summary>
    /// Convert a value.  Called when moving a value from ZoomPercentage to UI.
    /// </summary>
    /// <param name="value">value produced by the ZoomPercentage property</param>
    /// <param name="targetType">target type</param>
    /// <param name="parameter">converter parameter</param>
    /// <param name="culture">culture information</param>
    /// <returns>
    ///     Converted value.
    ///
    ///     System.Windows.DependencyProperty.UnsetValue may be returned to indicate that
    ///     the converter produced no value and that the fallback (if available)
    ///     or default value should be used instead.
    ///
    ///     Binding.DoNothing may be returned to indicate that the binding
    ///     should not transfer the value or use the fallback or default value.
    /// </returns>
    /// <remarks>
    /// The data binding engine does not catch exceptions thrown by a user-supplied
    /// converter.  Thus any exception thrown by Convert, or thrown by methods
    /// it calls and not caught by the Convert, will be treated as a runtime error
    /// (i.e. a crash).  Convert should handle anticipated problems by returning
    /// DependencyProperty.UnsetValue.
    /// </remarks>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Check if the targetType has been defined correctly.
        if (targetType == null)
        {
            return DependencyProperty.UnsetValue;
        }

        // Ensure that the value given is a double.
        if (value != null
            && value is double)
        {
            double percent = (double)value;

            // If string requested, format string.
            // If object is requested, then return a formated string.  This covers cases
            // similar to ButtonBase.CommandParameter, etc.
            if ((targetType == typeof(string)) || (targetType == typeof(object)))
            {
                // Check that value is a valid double.
                if ((double.IsNaN(percent)) || (double.IsInfinity(percent)))
                {
                    return DependencyProperty.UnsetValue;
                }
                else
                {
                    // Ensure output string is formatted to current globalization standards.
                    return String.Format(CultureInfo.CurrentCulture,
                        ZoomPercentageConverterStringFormat, percent);
                }
            }

            // If double requested, return direct value.
            else if (targetType == typeof(double))
            {
                return percent;
            }
        }
        return DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Convert back a value.  Called when moving a value into a ZoomPercentage.
    /// </summary>
    /// <param name="value">value, as produced by target</param>
    /// <param name="targetType">target type</param>
    /// <param name="parameter">converter parameter</param>
    /// <param name="culture">culture information</param>
    /// <returns>
    ///     Converted back value.
    ///
    ///     Binding.DoNothing may be returned to indicate that no value
    ///     should be set on the source property.
    ///
    ///     System.Windows.DependencyProperty.UnsetValue may be returned to indicate
    ///     that the converter is unable to provide a value for the source
    ///     property, and no value will be set to it.
    /// </returns>
    /// <remarks>
    /// The data binding engine does not catch exceptions thrown by a user-supplied
    /// converter.  Thus any exception thrown by ConvertBack, or thrown by methods
    /// it calls and not caught by the ConvertBack, will be treated as a runtime error
    /// (i.e. a crash).  ConvertBack should handle anticipated problems by returning
    /// DependencyProperty.UnsetValue.
    /// </remarks>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if ((targetType == typeof(double)) && (value != null))
        {
            double zoomValue = 0.0;
            bool isValidArg = false;

            // If value an int, then cast
            if (value is int)
            {
                zoomValue = (double)(int)value;
                isValidArg = true;
            }
            // If value is a double, then cast
            else if (value is double)
            {
                zoomValue = (double)value;
                isValidArg = true;
            }
            // If value is a string, then parse
            else if (value is string)
            {
                try
                {
                    // Remove whitespace on either end of the string.
                    string zoomString = (string)value;
                    if ((culture != null) && !String.IsNullOrEmpty(zoomString))
                    {
                        zoomString = ((string)value).Trim();

                        // If this is not a neutral culture attempt to remove the percent symbol.
                        if ((!culture.IsNeutralCulture) && (zoomString.Length > 0) && (culture.NumberFormat != null))
                        {
                            // This will strip the percent sign (if it exists) depending on the culture information.
                            switch (culture.NumberFormat.PercentPositivePattern)
                            {
                                case 0: // n %
                                case 1: // n%
                                    // Remove the last character if it is a percent sign
                                    if (zoomString.Length - 1 == zoomString.LastIndexOf(
                                                                    culture.NumberFormat.PercentSymbol,
                                                                    StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        zoomString = zoomString.Substring(0, zoomString.Length - 1);
                                    }
                                    break;
                                case 2: // %n
                                    // Remove the first character if it is a percent sign.
                                    if (0 == zoomString.IndexOf(
                                                culture.NumberFormat.PercentSymbol,
                                                StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        zoomString = zoomString.Substring(1);
                                    }
                                    break;
                            }
                        }

                        // If this conversion throws then the string wasn't a valid zoom value.
                        zoomValue = System.Convert.ToDouble(zoomString, culture);
                        isValidArg = true;
                    }
                }
// Allow empty catch statements.
#pragma warning disable 56502

                // Catch only the expected parse exceptions
                catch (ArgumentOutOfRangeException) { }
                catch (ArgumentNullException) { }
                catch (FormatException) { }
                catch (OverflowException) { }

// Disallow empty catch statements.
#pragma warning restore 56502
            }

            // Argument wasn't a valid percent, set error value.
            if (!isValidArg)
            {
                return DependencyProperty.UnsetValue;
            }
            return zoomValue;
        }
        // Requested type is not supported.
        else
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
}

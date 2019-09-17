// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Interface exposed by value converters - used by bindings
//              to convert source values to and from target values
//
// Specs:       Transformer.mht
//

using System;
using System.Windows;
using System.Globalization;
using System.Reflection;

namespace System.Windows.Data
{
/// <summary>
/// Interface for ValueConverter object
/// </summary>
/// <remarks>
/// When implementing this interface it is a good practice to decorate your implementation
/// with a <seealso cref="System.Windows.Data.ValueConversionAttribute"/> attribute
/// to indicate to development tools between what data types your converter can convert.
/// <code>
///     [ValueConversion(typeof(Employee), typeof(Brush))]
///     class MyConverter : IValueConverter
///     {
///         public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
///         {
///             if (value is Dev)    return Brushes.Beige;
///             if (value is Employee)  return Brushes.Salmon;
///             return Brushes.Yellow;
///         }
///     }
/// </code>
/// </remarks>
public interface IValueConverter
{
    /// <summary>
    /// Convert a value.  Called when moving a value from source to target.
    /// </summary>
    /// <param name="value">value as produced by source binding</param>
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
    object Convert(object value, Type targetType, object parameter, CultureInfo culture);

    /// <summary>
    /// Convert back a value.  Called when moving a value from target to source.
    /// This should implement the inverse of Convert.
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
    object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
}

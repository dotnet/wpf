// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Interface exposed by multi-value converters -
//              used by MultiBinding to convert/combine/split
//              source values to and from target values
//
// See spec at Data Binding.mht
//

using System;
using System.Windows;
using System.Globalization;
using System.Reflection;

namespace System.Windows.Data
{
/// <summary>
/// Interface for MultiValueConverter object -
/// used by MultiBinding to convert and combine source values to target values
/// and to convert and split target values to source values.
/// </summary>
public interface IMultiValueConverter
{
    /// <summary>
    ///     Convert a value.  Called when moving values from sources to target.
    /// </summary>
    /// <param name="values">
    ///     Array of values, as produced by source bindings.
    ///     System.Windows.DependencyProperty.UnsetValue may be passed to indicate that
    ///     the source binding has no value to provide for conversion.
    /// </param>
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
    object Convert(object[] values, Type targetType, object parameter, CultureInfo culture);

    /// <summary>
    ///     Convert back a value.  Called when moving a value from target to
    ///     sources.  This should implement the inverse of Convert.
    /// </summary>
    /// <param name="value">value, as produced by target</param>
    /// <param name="targetTypes">
    ///     Array of target types; array length indicates the number and types
    ///     of values suggested for Convert to return.
    /// </param>
    /// <param name="parameter">converter parameter</param>
    /// <param name="culture">culture information</param>
    /// <returns>
    ///     Array of converted back values.  If there are more return values
    ///     than source bindings, the excess portion of return values will
    ///     be ignored.  If there are more source bindings than return values,
    ///     the remaining source bindings will not have any value set to them.
    ///
    ///     Types of return values are not verified against targetTypes;
    ///     the values will be set to source bindings directly.
    ///
    ///     Binding.DoNothing may be returned in position i to indicate that no value
    ///     should be set on the source binding at index i.
    ///
    ///     System.Windows.DependencyProperty.UnsetValue may be returned in position i to indicate
    ///     that the converter is unable to provide a value to the source
    ///     binding at index i, and no value will be set to it.
    ///
    ///     ConvertBack may return null to indicate that the conversion could not
    ///     be performed at all, or that the backward conversion direction is not
    ///     supported by the converter.
    /// </returns>
    /// <remarks>
    /// The data binding engine does not catch exceptions thrown by a user-supplied
    /// converter.  Thus any exception thrown by ConvertBack, or thrown by methods
    /// it calls and not caught by the ConvertBack, will be treated as a runtime error
    /// (i.e. a crash).  ConvertBack should handle anticipated problems by returning
    /// null.
    /// </remarks>
    object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture);
}
}

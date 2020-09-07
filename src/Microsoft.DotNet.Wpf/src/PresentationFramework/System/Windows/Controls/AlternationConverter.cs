// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Convert between index and a list of values.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace System.Windows.Controls
{
    ///<summary>
    /// AlternationConverter is intended to be used by a binding to the
    /// ItemsControl.AlternationIndex attached property.  It converts an integer
    /// into the corresponding item in Values list.
    /// </summary>
    [ContentProperty("Values")]
    public class AlternationConverter : IValueConverter
    {
        ///<summary>
        /// A list of values.
        ///<summary>
        public IList Values
        {
            get { return _values; }
        }

        ///<summary>
        /// Convert an integer to the corresponding value from the Values list.
        ///</summary>
        public object Convert (object o, Type targetType, object parameter, CultureInfo culture)
        {
            if (_values.Count > 0 && o is int)
            {
                int index = ((int)o) % _values.Count;
                if (index < 0)  // Adjust for incorrect definition of the %-operator for negative arguments.
                    index += _values.Count;
                return _values[index];
            }

            return DependencyProperty.UnsetValue;
        }

        ///<summary>
        /// Convert an object to the index in the Values list at which that object appears.
        /// If the object is not in the Values list, return -1.
        ///</summary>
        public object ConvertBack(object o, Type targetType, object parameter, CultureInfo culture)
        {
            return _values.IndexOf(o);
        }

        List<object> _values = new List<object>();
    }
}

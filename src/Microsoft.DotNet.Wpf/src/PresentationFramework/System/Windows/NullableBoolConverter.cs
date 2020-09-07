// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.Collections;

namespace System.Windows
{

    /// <summary>
    /// NullableBoolConverter - Converter class for converting instances of other types to and from bool to Nullable bool.
    /// </summary> 
    public class NullableBoolConverter: NullableConverter
    {
        /// <summary>
        /// Construct NullableConverter converter for bool type
        /// </summary>
        public NullableBoolConverter()
            : base(typeof(bool?))
        {
        }


        /// <summary>
        /// Returns whether this object supports a standard set of values that can be
        /// picked from a list, using the specified context.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <returns>
        ///     true if GetStandardValues should be called to find a common set
        ///     of values the object supports; otherwise, false.
        /// </returns>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>
        ///     As implemented in this class, this method always returns true.
        /// If the list is exclusive, such as in an enumeration data type, then no other values are valid. If the list is not exclusive, then other valid values might exist in addition to the list of standard values that GetStandardValues provides.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Returns whether the collection of standard values returned from GetStandardValues is an exclusive list of possible values, using the specified context.
        /// </returns>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>
        /// StandardValuesCollection method override
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <returns>TypeConverter.StandardValuesCollection</returns>
        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (_standardValues == null)
            {
                ArrayList list1 = new ArrayList(3);
                list1.Add((bool?)true);
                list1.Add((bool?)false);
                list1.Add((bool?)null);
                _standardValues = new TypeConverter.StandardValuesCollection(list1.ToArray());
            }
            return _standardValues;
        }

        /// <summary>
        /// Cached value for GetStandardValues
        /// </summary>
        [ThreadStatic]
        private static TypeConverter.StandardValuesCollection _standardValues;
    }
}

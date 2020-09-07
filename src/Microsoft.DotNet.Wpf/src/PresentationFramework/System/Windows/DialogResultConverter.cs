// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Contains the DialogResultConverter: TypeConverter for the DialogResult class.
//
//

using System;
using System.ComponentModel;
using System.Globalization;

namespace System.Windows
{
    /// <summary>
    /// DialogResultConverter - Converter class for converting instances of other types to and from DialogResult instances.
    /// </summary> 
    public class DialogResultConverter: TypeConverter
    {
        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// CanConvertFrom - We don't support convert from other types to DialogResult
        /// </summary>
        /// <returns>
        /// bool - Always return False
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
            // We don't support ConvertFrom
            return false;
        }

        /// <summary>
        /// CanConvertTo - Returns whether or not this class can convert to a given type.
        /// </summary>
        /// <returns>
        /// bool - Always return False
        /// </returns>
        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType) 
        {
            // We don't support ConvertTo
            return false;
        }

        /// <summary>
        /// ConvertFrom - Attempt to convert to a DialogResult from the given object. 
        /// Always throw InvalidOperation exception 
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, 
                                           CultureInfo cultureInfo, 
                                           object source)
        {
            throw new InvalidOperationException(SR.Get(SRID.CantSetInMarkup));
        }

        /// <summary>
        /// ConvertTo - Attempt to convert a DialogResult to the given type
        /// Always throw InvalidOperation exception 
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, 
                                         CultureInfo cultureInfo,
                                         object value,
                                         Type destinationType)
        {
            throw new InvalidOperationException(SR.Get(SRID.CantSetInMarkup));
        }
        #endregion 
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//
//---------------------------------------------------------------------------

#if PRESENTATION_CORE
#else
using SR=System.Windows.SR;
#endif

namespace System.Windows.Media
{
    internal static partial class ValidateEnums
    {
        /// <summary>
        ///     Returns whether or not an enumeration instance a valid value.
        ///     This method is designed to be used with ValidateValueCallback, and thus
        ///     matches it's prototype.
        /// </summary>
        /// <param name="valueObject">
        ///     Enumeration value to validate.
        /// </param>    
        /// <returns> 'true' if the enumeration contains a valid value, 'false' otherwise. </returns>
        public static bool IsPenLineCapValid(object valueObject)
        {
            PenLineCap value = (PenLineCap)valueObject;

            return (value == PenLineCap.Flat) ||
                   (value == PenLineCap.Square) ||
                   (value == PenLineCap.Round) ||
                   (value == PenLineCap.Triangle);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      ConversionValidationRule is used when a ValidationError is the result of
//      conversion failure, as there is no actual ValidationRule.
//

using System;
using System.Windows;
using System.Globalization;
using System.Windows.Controls;

namespace System.Windows.Controls
{
    /// <summary>
    ///     ConversionValidationRule is used when a ValidationError is the result of
    ///     a conversion failure, as there is no actual ValidationRule.
    /// </summary>
    internal sealed class ConversionValidationRule : ValidationRule
    {
        /// <summary>
        /// ConversionValidationRule ctor.
        /// </summary>
        internal ConversionValidationRule() : base(ValidationStep.ConvertedProposedValue, false)
        {
        }

        /// <summary>
        /// Validate is called when Data binding is updating
        /// </summary>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return ValidationResult.ValidResult;
        }

        internal static readonly ConversionValidationRule Instance = new ConversionValidationRule();
    }
}


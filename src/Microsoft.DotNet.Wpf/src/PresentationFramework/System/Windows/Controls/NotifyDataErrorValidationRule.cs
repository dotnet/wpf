// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      NotifyDataErrorValidationRule is used when a ValidationError is the result of
//      a data error in the source item itself as exposed by INotifyDataErrorInfo.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MS.Internal;

namespace System.Windows.Controls
{
    /// <summary>
    ///     DataErrorValidationRule can be added to the ValidationRulesCollection of a Binding
    ///     or MultiBinding to indicate that data errors in the source object should
    ///     be considered ValidationErrors
    /// </summary>
    public sealed class NotifyDataErrorValidationRule : ValidationRule
    {
        /// <summary>
        /// DataErrorValidationRule ctor.
        /// </summary>
        public NotifyDataErrorValidationRule() : base(ValidationStep.UpdatedValue, true)
        {
        }

        /// <summary>
        /// Validate is called when Data binding is updating
        /// </summary>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            // this rule should never actually be called.  The errors from INotifyDataErrorInfo
            // are obtained by listening to the ErrorsChanged event, not from running a rule.
            // But we need to define this method (it's abstract in the base class), and we
            // need to return something.  ValidResult does the least harm.
            return ValidationResult.ValidResult;
        }

        internal static readonly NotifyDataErrorValidationRule Instance = new NotifyDataErrorValidationRule();
    }
}



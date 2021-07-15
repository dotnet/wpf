// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      ValidationRule is a member of ValidationRules Collection.
//      ValidationRulesCollection is a collection of ValidationRule
//      instances on either a Binding or a MultiBinding.  Each of the ValidationRules'
//      Validate is checked for validity on update
//
//
// See specs at Validation.mht
//

using System;
using System.Globalization;
using System.Windows.Data;

namespace System.Windows.Controls
{
    /// <summary>
    ///      ValidationRule is a member of ValidationRules Collection.
    ///      ValidationRulesCollection is a collection of ValidationRule
    ///      instances on either a Binding or a MultiBinding.  Each of the ValidationRules'
    ///      Validate is checked for validity on update
    /// </summary>
    public abstract class ValidationRule
    {
        /// <summary>
        /// Initialize a new instance of ValidationRule.
        /// </summary>
        // the step defaults to RawProposedValue for compat with WPF 3.0/3.5.
        // A more useful default would be ConvertedProposedValue.  Usually people
        // want to validate the value about to be set into the source property,
        // not the UI representation of that value.
        protected ValidationRule() : this(ValidationStep.RawProposedValue, false)
        {
        }

        /// <summary>
        /// Initialize a new instance of ValidationRule with the given validation
        /// step and target-update behavior.
        /// </summary>
        protected ValidationRule(ValidationStep validationStep, bool validatesOnTargetUpdated)
        {
            _validationStep = validationStep;
            _validatesOnTargetUpdated = validatesOnTargetUpdated;
        }

        /// <summary>
        /// Validate is called when Data binding is updating
        /// </summary>
        public abstract ValidationResult Validate(object value, CultureInfo cultureInfo);

        public virtual ValidationResult Validate(object value, CultureInfo cultureInfo, BindingExpressionBase owner)
        {
            switch (_validationStep)
            {
                case ValidationStep.UpdatedValue:
                case ValidationStep.CommittedValue:
                    value = owner;
                    break;
            }

            return Validate(value, cultureInfo);
        }

        public virtual ValidationResult Validate(object value, CultureInfo cultureInfo, BindingGroup owner)
        {
            return Validate(owner, cultureInfo);
        }

        /// <summary>
        /// The step at which the rule should be called.
        /// </summary>
        public ValidationStep ValidationStep
        {
            get { return _validationStep; }
            set { _validationStep = value; }
        }

        /// <summary>
        /// When true, the validation rule is also called during source-to-target data
        /// transfer.  This allows invalid data in the source to be highlighted
        /// as soon as it appears in the UI, without waiting for the user to edit it.
        /// </summary>
        public bool ValidatesOnTargetUpdated
        {
            get { return _validatesOnTargetUpdated; }
            set { _validatesOnTargetUpdated = value; }
        }

        ValidationStep  _validationStep;
        bool            _validatesOnTargetUpdated;
    }
}


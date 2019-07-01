// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(ValidationRuleFactory))]
    internal class ValidationRuleFactory : DiscoverableFactory<ValidationRuleForBinding>
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int MaxInt { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int MinInt { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double MaxDouble { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double MinDouble { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int MaxStringLen { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int MinStringLen { get; set; }

        #endregion

        #region Override Members

        public override ValidationRuleForBinding Create(DeterministicRandom random)
        {
            return new ValidationRuleForBinding(MaxInt, MinInt, MaxDouble, MinDouble, MaxStringLen, MinStringLen);
        }

        #endregion
    }
}

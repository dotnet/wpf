// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(ConverterForBindingFactory))]
    internal class ConverterForBindingFactory : DiscoverableFactory<ConverterForBinding>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public string RandomString { get; set; }

        #region Override Members

        public override ConverterForBinding Create(DeterministicRandom random)
        {
            return new ConverterForBinding(random.Next(), RandomString, random.NextBool());
        }

        #endregion
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// This factory create a custom control with specific Dependency properties for animation
    /// </summary>
    internal class CustomControlForAnimaionFactory : DiscoverableFactory<CustomControlForAnimaion>
    {
        public override CustomControlForAnimaion Create(DeterministicRandom random)
        {
            CustomControlForAnimaion customControl = new CustomControlForAnimaion();
            return customControl;
        }
    }
}

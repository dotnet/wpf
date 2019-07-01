// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ObjectFactory : DiscoverableFactory<Object>
    {
        public override Object Create(DeterministicRandom random)
        {
            return (random.NextBool() ? null : new Object());
        }
    }
}

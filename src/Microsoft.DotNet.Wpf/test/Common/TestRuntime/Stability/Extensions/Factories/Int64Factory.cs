// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class Int32Factory : DiscoverableFactory<Int32>
    {
        public override Int32 Create(DeterministicRandom random)
        {
            return (Int32)random.Next();
        }
    }

    class Int64Factory : DiscoverableFactory<Int64>
    {
        public override Int64 Create(DeterministicRandom random)
        {
            return (Int64)random.Next() * (Int64)UInt32.MaxValue + random.Next();
        }
    }
}

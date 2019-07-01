// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using Microsoft.Test.Stability.Core;


namespace Microsoft.Test.Stability.Extensions.Factories
{
    class EnumFactory : DiscoverableFactory
    {
        public override bool CanCreate(Type desiredType)
        {
            return desiredType.IsEnum;
        }

        public override object Create(Type desiredtype, DeterministicRandom random)
        {
            Array values = Enum.GetValues(desiredtype);
            return values.GetValue(random.Next(values.Length));
        }
    }
}

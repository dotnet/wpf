// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class Int32RectFactory : DiscoverableFactory<Int32Rect>
    {
        public override Int32Rect Create(DeterministicRandom random)
        {
            return new Int32Rect(random.Next(400), random.Next(400), random.Next(400), random.Next(400));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class DashStyleFactory : DiscoverableFactory<DashStyle>
    {
        public override DashStyle Create(DeterministicRandom random)
        {
            return random.NextStaticProperty<DashStyle>(typeof(DashStyles));
        }
    }
}

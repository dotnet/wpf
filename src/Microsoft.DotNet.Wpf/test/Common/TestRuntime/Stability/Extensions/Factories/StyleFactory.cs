// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    //TODO: Generic approach for Style is not implemented yet. Require some exploratory work and likely extensions to the stress framework. 
    class StyleFactory : DiscoverableFactory<Style>
    {
        public List<Setter> Setters { get; set; }

        public override Style Create(DeterministicRandom random)
        {           
            Style style = null;
            if (random.NextBool())
            {
                style = new Style();
                HomelessTestHelpers.Merge(style.Setters, Setters);
            }
            return style;
        }
    }
}

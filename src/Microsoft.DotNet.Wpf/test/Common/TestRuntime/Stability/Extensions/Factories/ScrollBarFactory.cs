// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ScrollBarFactory : RangeBaseFactory<ScrollBar>
    {
        public override ScrollBar Create(DeterministicRandom random)
        {
            ScrollBar scrollBar = new ScrollBar();
            ApplyRangeBaseProperties(scrollBar, random);
            scrollBar.Orientation = random.NextEnum<Orientation>();
            return scrollBar;
        }
    }
}

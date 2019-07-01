// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class TabControlFactory : SelectorFactory<TabControl>
    {
        public override TabControl Create(DeterministicRandom random)
        {
            TabControl tabControl = new TabControl();
            ApplySelectorProperties(tabControl, random);
            return tabControl;
        }
    }
}

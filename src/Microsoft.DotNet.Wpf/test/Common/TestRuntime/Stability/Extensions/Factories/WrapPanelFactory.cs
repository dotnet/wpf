// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class WrapPanelFactory : PanelFactory<WrapPanel>
    {
        public override WrapPanel Create(DeterministicRandom random)
        {
            WrapPanel wrapPanel = new WrapPanel();
            ApplyCommonProperties(wrapPanel, random);
            wrapPanel.Orientation = random.NextEnum<Orientation>();
            return wrapPanel;
        }
    }
}

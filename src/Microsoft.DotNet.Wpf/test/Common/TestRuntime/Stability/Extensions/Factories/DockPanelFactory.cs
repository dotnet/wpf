// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class DockPanelFactory : PanelFactory<DockPanel>
    {
        public override DockPanel Create(DeterministicRandom random)
        {
            DockPanel dockPanel = new DockPanel();
            ApplyCommonProperties(dockPanel, random);
            SetChildrenLayout(dockPanel, random);
            dockPanel.LastChildFill = random.NextBool();
            return dockPanel;
        }

        private void SetChildrenLayout(DockPanel panel,DeterministicRandom random)
        {
            foreach (UIElement item in panel.Children)
            {
                DockPanel.SetDock(item, random.NextEnum<Dock>());
                DockPanel.SetFlowDirection(item, random.NextEnum<FlowDirection>());
            }
        }
    }
}

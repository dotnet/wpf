// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create TabPanel.
    /// </summary>
    internal class TabPanelFactory : PanelFactory<TabPanel>
    {
        /// <summary>
        /// Create a TabPanel.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override TabPanel Create(DeterministicRandom random)
        {
            TabPanel tabPanel = new TabPanel();

            ApplyCommonProperties(tabPanel, random);

            return tabPanel;
        }
    }
}

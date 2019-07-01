// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create ToolBarOverflowPanel.
    /// </summary>
    internal class ToolBarOverflowPanelFactory : PanelFactory<ToolBarOverflowPanel>
    {
        /// <summary>
        /// Create a ToolBarOverflowPanel.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override ToolBarOverflowPanel Create(DeterministicRandom random)
        {
            ToolBarOverflowPanel panel = new ToolBarOverflowPanel();

            ApplyCommonProperties(panel, random);
            panel.WrapWidth = random.NextDouble() * 1000;

            return panel;
        }
    }
}

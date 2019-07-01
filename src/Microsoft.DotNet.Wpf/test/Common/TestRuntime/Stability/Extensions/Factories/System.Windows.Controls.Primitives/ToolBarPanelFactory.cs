// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create ToolBarPanel.
    /// </summary>
    internal class ToolBarPanelFactory : PanelFactory<ToolBarPanel>
    {
        /// <summary>
        /// Create a ToolBarPanel.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override ToolBarPanel Create(DeterministicRandom random)
        {
            ToolBarPanel panel = new ToolBarPanel();

            ApplyCommonProperties(panel, random);

            return panel;
        }
    }
}

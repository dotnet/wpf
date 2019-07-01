// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create VirtualizingStackPanel.
    /// </summary>
    internal class VirtualizingStackPanelFactory : AbstractVirtualizingStackPanelFactory<VirtualizingStackPanel>
    {
        /// <summary>
        /// Create a VirtualizingStackPanel.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override VirtualizingStackPanel Create(DeterministicRandom random)
        {
            VirtualizingStackPanel panel = new VirtualizingStackPanel();

            ApplyVirtualizingStackPanelProperties(panel, random);

            return panel;
        }
    }
}

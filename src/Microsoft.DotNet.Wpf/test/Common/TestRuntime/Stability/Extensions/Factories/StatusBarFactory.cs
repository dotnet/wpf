// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create StatusBar.
    /// </summary>
    internal class StatusBarFactory : ItemsControlFactory<StatusBar>
    {
        /// <summary>
        /// Create a StatusBar.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override StatusBar Create(DeterministicRandom random)
        {
            StatusBar statusBar = new StatusBar();

            ApplyItemsControlProperties(statusBar, random);

            return statusBar;
        }
    }
}

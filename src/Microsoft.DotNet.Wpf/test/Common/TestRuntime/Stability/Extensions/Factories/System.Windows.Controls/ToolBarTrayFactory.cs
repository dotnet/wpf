// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create ToolBarTray.
    /// </summary>
    internal class ToolBarTrayFactory : DiscoverableFactory<ToolBarTray>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Brush to set ToolBarTray Background property.
        /// </summary>
        public Brush Background { get; set; }

        /// <summary>
        /// Gets or sets a list of ToolBar to set ToolBarTray ToolBars property.
        /// </summary>
        public List<ToolBar> ToolBars { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a ToolBarTray.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override ToolBarTray Create(DeterministicRandom random)
        {
            ToolBarTray toolBarTray = new ToolBarTray();

            toolBarTray.Background = Background;
            toolBarTray.IsLocked = random.NextBool();
            toolBarTray.Orientation = random.NextEnum<Orientation>();
            HomelessTestHelpers.Merge(toolBarTray.ToolBars, ToolBars);

            return toolBarTray;
        }

        #endregion
    }
}

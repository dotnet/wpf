// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create ToolBar.
    /// </summary>
    [TargetTypeAttribute(typeof(ToolBar))]
    internal class ToolBarFactory : HeaderedItemsControlFactory<ToolBar>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a value to set ToolBar Band property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 Band { get; set; }

        /// <summary>
        /// Gets or sets a value to set ToolBar BandIndex property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 BandIndex { get; set; }

        #endregion

        #region Override Members

        public override ToolBar Create(DeterministicRandom random)
        {
            ToolBar toolbar = new ToolBar();

            ApplyHeaderedItemsControlProperties(toolbar, random);
            toolbar.Band = Band;
            toolbar.BandIndex = BandIndex;
            toolbar.IsOverflowOpen = random.NextBool();

            return toolbar;
        }

        #endregion
    }
}

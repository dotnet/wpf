// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete VirtualizingStackPanel factory.
    /// </summary>
    /// <typeparam name="PanelType"></typeparam>
    internal abstract class AbstractVirtualizingStackPanelFactory<PanelType> : PanelFactory<PanelType> where PanelType : VirtualizingStackPanel
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a ScrollViewer to set VirtualizingStackPanel ScrollOwner property.
        /// </summary>
        public ScrollViewer ScrollOwner { get; set; }

        #endregion

        #region Protected Members

        /// <summary>
        /// Apply common VirtualizingStackPanel properties.
        /// </summary>
        /// <param name="panel"/>
        /// <param name="random"/>
        protected void ApplyVirtualizingStackPanelProperties(PanelType panel, DeterministicRandom random)
        {
            ApplyCommonProperties(panel, random);
            panel.CanHorizontallyScroll = random.NextBool();
            panel.CanVerticallyScroll = random.NextBool();
            panel.Orientation = random.NextEnum<Orientation>();
            panel.ScrollOwner = ScrollOwner;
        }

        #endregion
    }
}

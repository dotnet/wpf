// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create TickBar.
    /// </summary>
    internal class TickBarFactory : DiscoverableFactory<TickBar>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Brush to set Brush Fill property.
        /// </summary>
        public Brush Brush { get; set; }

        /// <summary>
        /// Gets or sets a DoubleCollection to set Brush Ticks property.
        /// </summary>
        public DoubleCollection Ticks { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a TickBar.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override TickBar Create(DeterministicRandom random)
        {
            TickBar tickBar = new TickBar();

            tickBar.Fill = Brush;
            tickBar.IsDirectionReversed = random.NextBool();
            tickBar.IsSelectionRangeEnabled = random.NextBool();
            tickBar.Minimum = (random.NextDouble() - 0.5) * 10000;
            tickBar.Maximum = (random.NextDouble() - 0.5) * 10000;
            tickBar.Placement = random.NextEnum<TickBarPlacement>();
            tickBar.ReservedSpace = (random.NextDouble() - 0.5) * 10000;
            if (tickBar.IsSelectionRangeEnabled)
            {
                tickBar.SelectionStart = (random.NextDouble() - 0.5) * 10000;
                tickBar.SelectionStart = (random.NextDouble() - 0.5) * 10000;
            }
            tickBar.TickFrequency = random.NextDouble() * 10000;
            tickBar.Ticks = Ticks;

            return tickBar;
        }

        #endregion
    }
}

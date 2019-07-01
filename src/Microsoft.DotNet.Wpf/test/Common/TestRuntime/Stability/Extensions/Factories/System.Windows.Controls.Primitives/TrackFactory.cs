// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create Track.
    /// </summary>
    internal class TrackFactory : DiscoverableFactory<Track>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a RepeatButton to set Track DecreaseRepeatButton property.
        /// </summary>
        public RepeatButton DecreaseRepeatButton { get; set; }

        /// <summary>
        /// Gets or sets a RepeatButton to set Track IncreaseRepeatButton property.
        /// </summary>
        public RepeatButton IncreaseRepeatButton { get; set; }

        /// <summary>
        /// Gets or sets a Thumb to set Track Thumb property..
        /// </summary>
        public Thumb Thumb { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a Track.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override Track Create(DeterministicRandom random)
        {
            Track track = new Track();

            track.DecreaseRepeatButton = DecreaseRepeatButton;
            track.IncreaseRepeatButton = IncreaseRepeatButton;
            track.IsDirectionReversed = random.NextBool();
            track.Minimum = (random.NextDouble() - 0.5) * 10000;
            track.Maximum = (random.NextDouble() - 0.5) * 10000;
            track.Orientation = random.NextEnum<Orientation>();
            track.Thumb = Thumb;
            track.Value = (random.NextDouble() - 0.5) * 10000;
            if (random.Next(20) == 1) //Set ViewporSize value = double.NaN as a probability of 1/20. 
            {
                track.ViewportSize = double.NaN;
            }
            else
            {
                track.ViewportSize = random.NextDouble() * 10000;
            }

            return track;
        }

        #endregion
    }
}

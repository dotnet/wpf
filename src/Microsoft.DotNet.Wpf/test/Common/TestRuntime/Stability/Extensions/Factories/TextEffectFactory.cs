// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create TextEffect.
    /// </summary>
    internal class TextEffectFactory : DiscoverableFactory<TextEffect>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Transform to initialize a TextEffect.
        /// </summary>
        public Transform Transform { get; set; }

        /// <summary>
        /// Gets or sets a Brush to initialize a TextEffect.
        /// </summary>
        public Brush Foreground { get; set; }

        /// <summary>
        /// Gets or sets a Geometry to initialize a TextEffect.
        /// </summary>
        public Geometry Clip { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a TextEffect.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override TextEffect Create(DeterministicRandom random)
        {
            return new TextEffect(Transform, Foreground, Clip, random.Next(5), random.Next(20));
        }

        #endregion
    }
}

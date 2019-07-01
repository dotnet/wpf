// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Ink;
using System.Windows.Input;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create Stroke.
    /// </summary>
    internal class StrokeFactory : DiscoverableFactory<Stroke>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a StylusPointCollection to initialize a Stroke. 
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public StylusPointCollection StylusPoints { get; set; }

        /// <summary>
        /// Gets or sets a DrawingAttributes to initialize a Stroke.
        /// </summary>
        public DrawingAttributes DrawingAttributes { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a Stroke.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override Stroke Create(DeterministicRandom random)
        {
            Stroke stroke;

            if (DrawingAttributes == null)
            {
                stroke = new Stroke(StylusPoints);
            }
            else
            {
                stroke = new Stroke(StylusPoints, DrawingAttributes);
            }

            return stroke;
        }

        #endregion
    }
}

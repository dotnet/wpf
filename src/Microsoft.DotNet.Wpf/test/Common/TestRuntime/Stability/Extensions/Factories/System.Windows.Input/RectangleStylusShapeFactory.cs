// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Ink;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create RectangleStylusShape.
    /// </summary>
    internal class RectangleStylusShapeFactory : DiscoverableFactory<RectangleStylusShape>
    {
        /// <summary>
        /// Create a RectangleStylusShape.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override RectangleStylusShape Create(DeterministicRandom random)
        {
            //Set Width and Height value (0, 10000].
            double width = (1 - random.NextDouble()) * 10000;
            double height = (1 - random.NextDouble()) * 10000;
            //Set Rotation value [-720, 720).
            double rotation = (random.NextDouble() - 0.5) * 1440;

            RectangleStylusShape rectangleStylusShpae = new RectangleStylusShape(width, height, rotation);

            return rectangleStylusShpae;
        }
    }
}

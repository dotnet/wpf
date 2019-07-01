// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class KeySplineFactory : DiscoverableFactory<KeySpline>
    {
        #region Public Members

        public double Point1X { get; set; }

        public double Point1Y { get; set; }

        public double Point2X { get; set; }

        public double Point2Y { get; set; }

        #endregion

        #region Override Members

        public override KeySpline Create(DeterministicRandom random)
        {
            KeySpline keySpline = new KeySpline(Point1X, Point1Y, Point2X, Point2Y);
            return keySpline;
        }

        #endregion
    }
}

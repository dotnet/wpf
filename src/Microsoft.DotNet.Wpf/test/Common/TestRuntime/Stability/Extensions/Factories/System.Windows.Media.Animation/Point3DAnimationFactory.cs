// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create a Point3DAnimation.
    /// </summary>
    internal class Point3DAnimationFactory : TimelineFactory<Point3DAnimation>
    {
        #region Public Members

        public Point3D FromValue { get; set; }

        public Point3D ToValue { get; set; }

        public Point3D ByValue { get; set; }

        #endregion

        #region Override Members

        public override Point3DAnimation Create(DeterministicRandom random)
        {
            Point3DAnimation point3DAnimation = new Point3DAnimation();

            /*
             *Randomly combinate From, To, By 
             */
            if (random.NextBool())
            {
                point3DAnimation.By = ByValue;
            }

            if (random.NextBool())
            {
                point3DAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                point3DAnimation.To = ToValue;
            }

            point3DAnimation.IsAdditive = random.NextBool();
            point3DAnimation.IsCumulative = random.NextBool();
            
            ApplyTimelineProperties(point3DAnimation, random);

            return point3DAnimation;
        }

        #endregion Members
    }
}

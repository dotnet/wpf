// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create a Rotation3DAnimation.
    /// </summary>
    internal class Rotation3DAnimationFactory : TimelineFactory<Rotation3DAnimation>
    {
        #region Public Members

        public Rotation3D ByValue { get; set; }

        public Rotation3D FromValue { get; set; }

        public Rotation3D ToValue { get; set; }

        #endregion

        public override Rotation3DAnimation Create(DeterministicRandom random)
        {
            Rotation3DAnimation rotation3DAnimation = new Rotation3DAnimation();

            /*
             *Randomly combinate From, To, By 
             */
            if (random.NextBool())
            {
                rotation3DAnimation.By = ByValue;
            }

            if (random.NextBool())
            {
                rotation3DAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                rotation3DAnimation.To = ToValue;
            }

            rotation3DAnimation.IsAdditive = random.NextBool();
            rotation3DAnimation.IsCumulative = random.NextBool();

            ApplyTimelineProperties(rotation3DAnimation, random);

            return rotation3DAnimation;
        }
    }
}

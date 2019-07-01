// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create a Vector3DAnimation.
    /// </summary>
    internal class Vector3DAnimationFactory : TimelineFactory<Vector3DAnimation>
    {
        #region Public Members

        public Vector3D ByValue { get; set; }

        public Vector3D FromValue { get; set; }

        public Vector3D ToValue { get; set; }

        #endregion

        #region Override Members

        public override Vector3DAnimation Create(DeterministicRandom random)
        {
            Vector3DAnimation vector3DAnimation = new Vector3DAnimation();

            /*
             *Randomly combinate From, To, By 
             */
            if (random.NextBool())
            {
                vector3DAnimation.By = ByValue;
            }

            if (random.NextBool())
            {
                vector3DAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                vector3DAnimation.To = ToValue;
            }

            vector3DAnimation.IsAdditive = random.NextBool();
            vector3DAnimation.IsCumulative = random.NextBool();

            ApplyTimelineProperties(vector3DAnimation, random);

            return vector3DAnimation;
        }

        #endregion
    }
}

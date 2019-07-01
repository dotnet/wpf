// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteObjectKeyFrameFactory : DiscoverableFactory<DiscreteObjectKeyFrame>
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Freezable Object { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        #region Override Members

        public override DiscreteObjectKeyFrame Create(DeterministicRandom random)
        {
            DiscreteObjectKeyFrame discreteObjectKeyFrame = new DiscreteObjectKeyFrame();

            //Before do object animation, the object value will be frozen
            //Workaround: if the object is null or can't be frozen, set it with a brush so that can be frozen.
            if (Object == null || !Object.CanFreeze)
            {
                discreteObjectKeyFrame.Value = new SolidColorBrush();
            }
            else
            {
                discreteObjectKeyFrame.Value = Object;
            }

            discreteObjectKeyFrame.KeyTime = KeyTime;

            return discreteObjectKeyFrame;
        }

        #endregion
    }
}

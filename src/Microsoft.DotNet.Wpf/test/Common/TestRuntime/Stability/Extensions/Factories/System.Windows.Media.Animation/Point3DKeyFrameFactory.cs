// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class Point3DKeyFrameFactory<Point3DKeyFrameType> : DiscoverableFactory<Point3DKeyFrameType> where Point3DKeyFrameType : Point3DKeyFrame
    {
        #region Public Members

        public Point3D Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplyPoint3DKeyFrameProperties(Point3DKeyFrame point3DKeyFrame, DeterministicRandom random)
        {
            point3DKeyFrame.Value = Value;
            point3DKeyFrame.KeyTime = KeyTime;
        }
    }
}

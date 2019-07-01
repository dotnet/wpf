// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Point3DAnimation ProjectionCamera PositionProperty.
    /// </summary>
    public class Point3DAnimateCameraAction : SimpleDiscoverableAction
    {
        #region Public Members

        /// <summary/>
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        /// <summary/>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Point3DAnimation Point3DAnimation { get; set; }

        /// <summary/>
        public HandoffBehavior HandoffBehavior { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        public override void Perform()
        {
            ((ProjectionCamera)(Viewport3D.Camera)).BeginAnimation(ProjectionCamera.PositionProperty, Point3DAnimation, HandoffBehavior);
        }

        public override bool CanPerform()
        {
            if (Viewport3D != null && Viewport3D.Camera != null)
            {
                return (Viewport3D.Camera.GetType().IsSubclassOf(typeof(ProjectionCamera)));
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}

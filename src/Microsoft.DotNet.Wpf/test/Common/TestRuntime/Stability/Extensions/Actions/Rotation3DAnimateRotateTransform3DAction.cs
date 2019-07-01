// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Rotation3DAnimate RotateTransform3D RotationProperty.
    /// </summary>
    public class Rotation3DAnimateRotateTransform3DAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        /// <summary/>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Rotation3DAnimation Rotation3DAnimation { get; set; }

        /// <summary/>
        public HandoffBehavior HandoffBehavior { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {   // change  Rotation3DAnimation to RotateTransform3D here
            GetRotateTransform3DFromViewport3D(Viewport3D).BeginAnimation(RotateTransform3D.RotationProperty, Rotation3DAnimation, HandoffBehavior);
        }

        public override bool CanPerform()
        {
            return (GetRotateTransform3DFromViewport3D(Viewport3D) != null);
        }

        #endregion

        /// <summary>
        /// Get RotateTransform3D from Viewport3D, since get from ObjectTree directly will takes too much time and resource
        /// Work around bug 24204: http://vstfdevdiv:8080/web/wi.aspx?pcguid=22f9acc9-569a-41ff-b6ac-fac1b6370209&id=24204
        /// </summary>
        private RotateTransform3D GetRotateTransform3DFromViewport3D(Viewport3D Viewport3D)
        {
            if (Viewport3D != null && Viewport3D.Children.Count != 0)
            {
                foreach (Visual3D visual3D in Viewport3D.Children)
                {
                    Transform3D transform3D = ((Visual3D)(visual3D)).Transform;
                    if (transform3D != null && transform3D.GetType() == typeof(RotateTransform3D))
                    {
                        return transform3D as RotateTransform3D;
                    }
                }
            }

            return null;
        }
    }
}

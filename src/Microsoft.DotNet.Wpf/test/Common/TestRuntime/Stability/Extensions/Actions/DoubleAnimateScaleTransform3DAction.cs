// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// DoubleAnimate ScaleTransform3D CenterXProperty, CenterYProperty, CenterZProperty, ScaleXProperty, ScaleXProperty, ScaleZProperty.
    /// </summary>
    public class DoubleAnimateScaleTransform3DAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        public Properties PropertyToAnimate { get; set; }

        /// <summary/>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public DoubleAnimation DoubleAnimation { get; set; }

        /// <summary/>
        public HandoffBehavior HandoffBehavior { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            ScaleTransform3D ScaleTransform3D = GetScaleTransform3DFromViewport3D(Viewport3D);

            switch (PropertyToAnimate)
            {
                case Properties.CenterXProperty:
                    ScaleTransform3D.BeginAnimation(ScaleTransform3D.CenterXProperty, DoubleAnimation, HandoffBehavior);
                    break;
                case Properties.CenterYProperty:
                    ScaleTransform3D.BeginAnimation(ScaleTransform3D.CenterYProperty, DoubleAnimation, HandoffBehavior);
                    break;
                case Properties.CenterZProperty:
                    ScaleTransform3D.BeginAnimation(ScaleTransform3D.CenterZProperty, DoubleAnimation, HandoffBehavior);
                    break;
                case Properties.ScaleXProperty:
                    ScaleTransform3D.BeginAnimation(ScaleTransform3D.ScaleXProperty, DoubleAnimation, HandoffBehavior);
                    break;
                case Properties.ScaleYProperty:
                    ScaleTransform3D.BeginAnimation(ScaleTransform3D.ScaleYProperty, DoubleAnimation, HandoffBehavior);
                    break;
                case Properties.ScaleZProperty:
                    ScaleTransform3D.BeginAnimation(ScaleTransform3D.ScaleZProperty, DoubleAnimation, HandoffBehavior);
                    break;
            }
        }

        public override bool CanPerform()
        {
            return (GetScaleTransform3DFromViewport3D(Viewport3D) != null);
        }

        #endregion

        public enum Properties
        {
            CenterXProperty,
            CenterYProperty,
            CenterZProperty,
            ScaleXProperty,
            ScaleYProperty,
            ScaleZProperty
        }

        /// <summary>
        /// Get ScaleTransform3D from Viewport3D, since get from ObjectTree directly will takes too much time and resource
        /// Work around bug 24204: http://vstfdevdiv:8080/web/wi.aspx?pcguid=22f9acc9-569a-41ff-b6ac-fac1b6370209&id=24204
        /// </summary>
        private ScaleTransform3D GetScaleTransform3DFromViewport3D(Viewport3D Viewport3D)
        {
            if (Viewport3D != null && Viewport3D.Children.Count != 0)
            {
                foreach (Visual3D visual3D in Viewport3D.Children)
                {
                    Transform3D transform3D = ((Visual3D)(visual3D)).Transform;
                    if (transform3D != null && transform3D.GetType() == typeof(ScaleTransform3D))
                    {
                        return transform3D as ScaleTransform3D;
                    }
                }
            }

            return null;
        }
    }
}

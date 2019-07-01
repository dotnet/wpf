// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// DoubleAnimate ProjectionCamera NearPlaneDistanceProperty, FarPlaneDistanceProperty, PerspectiveCamera.FieldOfViewProperty, OrthographicCamera.WidthProperty.
    /// </summary>
    public class DoubleAnimateCameraAction : SimpleDiscoverableAction
    {
        #region Public Members

        /// <summary/>
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        /// <summary/>
        public Properties PropertyToAnimate { get; set; }

        /// <summary/>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public DoubleAnimation DoubleAnimation { get; set; }

        /// <summary/>
        public HandoffBehavior HandoffBehavior { get; set; }

        public int FarPlaneDistance { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        public override void Perform()
        {
            ProjectionCamera Camera = Viewport3D.Camera as ProjectionCamera;

            switch (PropertyToAnimate)
            {
                case Properties.NearPlaneDistanceProperty:
                    Camera.BeginAnimation(ProjectionCamera.NearPlaneDistanceProperty, DoubleAnimation, HandoffBehavior);
                    break;
                case Properties.FarPlaneDistanceProperty:
                    //DoubleAnimation cannot use FarPlaneDistance default origin value of 'Infinity'.
                    //Changed it to [0-49].
                    Camera.FarPlaneDistance = FarPlaneDistance % 50;
                    Camera.BeginAnimation(ProjectionCamera.FarPlaneDistanceProperty, DoubleAnimation, HandoffBehavior);
                    break;
                case Properties.OtherProperty:
                    if (Camera is PerspectiveCamera)
                    {
                        Camera.BeginAnimation(PerspectiveCamera.FieldOfViewProperty, DoubleAnimation, HandoffBehavior);
                        break;
                    }
                    if (Camera is OrthographicCamera)
                    {
                        Camera.BeginAnimation(OrthographicCamera.WidthProperty, DoubleAnimation, HandoffBehavior);
                        break;
                    }
                    break;
            }
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

        public enum Properties
        {
            NearPlaneDistanceProperty,
            FarPlaneDistanceProperty,
            OtherProperty
        }
    }
}

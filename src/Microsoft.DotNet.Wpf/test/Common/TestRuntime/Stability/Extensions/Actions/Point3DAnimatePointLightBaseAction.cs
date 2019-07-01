// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Point3DAnimate PointLightBase.PositionProperty.
    /// </summary>
    public class Point3DAnimatePointLightBaseAction : SimpleDiscoverableAction
    {
        #region Public Members

        /// <summary/>
        //[InputAttribute(ContentInputSource.GetFromObjectTree)]
        //public PointLightBase PointLightBase { get; set; }
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        /// <summary/>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Point3DAnimation Point3DAnimation { get; set; }

        /// <summary/>
        public HandoffBehavior HandoffBehavior { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            GetPointLightBaseFromViewport3D(Viewport3D).BeginAnimation(PointLightBase.PositionProperty, Point3DAnimation, HandoffBehavior);
        }

        public override bool CanPerform()
        {
            return (GetPointLightBaseFromViewport3D(Viewport3D) != null);
        }

        #endregion

        /// <summary>
        /// Get PointLightBase from Viewport3D, since get from ObjectTree directly will takes too much time and resource
        /// Work around bug 24204: http://vstfdevdiv:8080/web/wi.aspx?pcguid=22f9acc9-569a-41ff-b6ac-fac1b6370209&id=24204
        /// </summary>
        private PointLightBase GetPointLightBaseFromViewport3D(Viewport3D Viewport3D)
        {
            if (Viewport3D != null && Viewport3D.Children.Count != 0)
            {
                foreach (Visual3D visual3D in Viewport3D.Children)
                {
                    if (visual3D.GetType() == typeof(ModelVisual3D))
                    {
                        Model3D visualContent = ((ModelVisual3D)(visual3D)).Content;
                        if ((visualContent != null) && visualContent.GetType().IsSubclassOf(typeof(PointLightBase)))
                        {
                            return visualContent as PointLightBase;
                        }
                    }

                    if (visual3D.GetType() == typeof(ModelUIElement3D))
                    {
                        Model3D visualModel = ((ModelUIElement3D)(visual3D)).Model;
                        if (visualModel != null && visualModel.GetType().IsSubclassOf(typeof(PointLightBase)))
                        {
                            return visualModel as PointLightBase;
                        }
                    }
                }
            }

            return null;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Apply methods of DrawingContext
    /// </summary>
    public abstract class SetDrawingContextAction : SimpleDiscoverableAction
    {
        #region Public Members
        
        public DrawingGroup DrawingGroup { get; set; }

        public bool IsOpen { get; set; }

        //The clip region to apply to subsequent drawing commands.
        public Geometry ClipGeometry { get; set; }

        //The guideline set to apply to subsequent drawing commands.
        public GuidelineSet Guideline { get; set; }

        public bool IsAnimation { get; set; }

        //The opacity factor to apply to subsequent drawing commands.
        public double Opacity { get; set; }

        public DoubleAnimation DoubleAnimation { get; set; }

        //The opacity mask to apply to subsequent drawings.
        public Brush OpacityMask { get; set; }

        //The transform to apply to subsequent drawing commands.
        public Transform Transform { get; set; }
        
        #endregion

        protected DrawingContext SetDrawingContext()
        {
            DrawingContext Target = null;
            //The clock with which to animate the opacity value.
            AnimationClock opacityAnimations = DoubleAnimation.CreateClock();

            if (IsOpen)
            {
                //Obtain a DrawingContext from the DrawingGroup by Open().
                Target = DrawingGroup.Open();
            }
            else
            {
                //Obtain a DrawingContext from the DrawingGroup by Append().
                Target = DrawingGroup.Append();
            }

            //Pushes the specified clip region onto the drawing context.
            Target.PushClip(ClipGeometry);

            //Pushes the specified GuidelineSet onto the drawing context.
            Target.PushGuidelineSet(Guideline);

            if (IsAnimation)
            {
                //Pushes the specified opacity setting onto the drawing context and applies the specified animation clock.
                Target.PushOpacity(Opacity, opacityAnimations);
            }
            else
            {
                //Pushes the specified opacity setting onto the drawing context. 
                Target.PushOpacity(Opacity);
            }

            //Pushes the specified opacity mask onto the drawing context. 
            Target.PushOpacityMask(OpacityMask);

            //Pushes the specified Transform onto the drawing context.
            Target.PushTransform(Transform);
            
            return Target;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Draws an ellipse.
    /// </summary>
    public class DrawEllipseAction : SetDrawingContextAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public Control Control { get; set; }

        public Brush Brush { get; set; }

        public Pen Pen { get; set; }

        //The location of the center of the ellipse.
        public Point Center { get; set; }

        //The horizontal radius of the ellipse.
        public double RadiusX { get; set; }

        //The vertical radius of the ellipse.
        public double RadiusY { get; set; }

        public bool IsDrawAnimation { get; set; }

        public PointAnimationUsingPath PointAnimationUsingPath { get; set; }

        #endregion

        public override void Perform()
        {
            //The clock with which to animate the ellipse's center position.
            AnimationClock centerAnimations = PointAnimationUsingPath.CreateClock();
            //The clock with which to animate the ellipse's x-radius.
            AnimationClock radiusXAnimations = DoubleAnimation.CreateClock();
            //The clock with which to animate the ellipse's y-radius.
            AnimationClock radiusYAnimations = DoubleAnimation.CreateClock();

            DrawingContext Target = SetDrawingContext();

            if (IsDrawAnimation)
            {
                //Draws an ellipse with the specified Brush and Pen and applies the specified animation clocks. 
                Target.DrawEllipse(Brush, Pen, Center, centerAnimations, RadiusX, radiusXAnimations, RadiusY, radiusYAnimations);
            }
            else
            {
                //Draws an ellipse with the specified Brush and Pen.
                Target.DrawEllipse(Brush, Pen, Center, RadiusX, RadiusY);
            }

            Target.Close();

            //Add DrawingGroup to DrawingBrush's Drawing property
            ((DrawingBrush)(Control.Background)).Drawing = DrawingGroup;
        }

        /// <summary>
        /// Only can perform when the control's background type is DrawingBrush
        /// </summary>
        public override bool CanPerform()
        {
            if (Control.Background != null)
            {
                return Control.Background.GetType() == typeof(DrawingBrush);
            }
            else
            {
                return false;
            }
        }
    }
}

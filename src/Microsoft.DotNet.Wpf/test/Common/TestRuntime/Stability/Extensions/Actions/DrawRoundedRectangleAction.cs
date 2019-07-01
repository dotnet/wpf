// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Draws a rounded rectangle. 
    /// </summary>
    [TargetTypeAttribute(typeof(DrawRoundedRectangleAction))]
    public class DrawRoundedRectangleAction : SetDrawingContextAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public Control Control { get; set; }

        public bool IsDrawAnimation { get; set; }

        //The brush used to fill the rectangle.
        public Brush Brush { get; set; }

        //The pen used to stroke the rectangle.
        public Pen Pen { get; set; }

        //The rectangle to draw.
        public Rect Rectangle { get; set; }

        //The radius in the X dimension of the rounded corners.
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double RadiusX { get; set; }

        //The radius in the Y dimension of the rounded corners.
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double RadiusY { get; set; }

        public RectAnimation RectAnimation { get; set; }

        #endregion

        public override void Perform()
        {
            //The clock with which to animate the rectangle's size and dimensions.
            AnimationClock rectangleAnimations = RectAnimation.CreateClock();
            //The clock with which to animate the rectangle's radiusX value.
            AnimationClock radiusXAnimations = DoubleAnimation.CreateClock();
            //The clock with which to animate the rectangle's radiusY value.
            AnimationClock radiusYAnimations = DoubleAnimation.CreateClock();

            DrawingContext Target = SetDrawingContext();

            if (IsDrawAnimation)
            {
                //Draws a rounded rectangle with the specified Brush and Pen and applies the specified animation clocks.
                Target.DrawRoundedRectangle(Brush, Pen, Rectangle, rectangleAnimations, RadiusX, radiusXAnimations, RadiusY, radiusYAnimations);
            }
            else
            {
                //Draws a rounded rectangle with the specified Brush and Pen. 
                Target.DrawRoundedRectangle(Brush, Pen, Rectangle, RadiusX, RadiusY);
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

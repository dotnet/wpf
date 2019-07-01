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
    /// Draws a rectangle. 
    /// </summary>
    public class DrawRectangleAction : SetDrawingContextAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public Control Control { get; set; }

        public bool IsDrawAnimation { get; set; }

        //The brush with which to fill the rectangle.
        public Brush Brush { get; set; }

        //The pen with which to stroke the rectangle.
        public Pen Pen { get; set; }

        //The rectangle to draw.
        public Rect Rectangle { get; set; }

        public RectAnimation RectAnimation { get; set; }

        #endregion

        public override void Perform()
        {
            //The clock with which to animate the rectangle's size and dimensions, or null for no animation.
            AnimationClock rectangleAnimations = RectAnimation.CreateClock();

            DrawingContext Target = SetDrawingContext();

            if (IsDrawAnimation)
            {
                //Draws a rectangle with the specified Brush and Pen and applies the specified animation clocks.
                Target.DrawRectangle(Brush, Pen, Rectangle, rectangleAnimations);
            }
            else
            {
                //Draws a rectangle with the specified Brush and Pen.
                Target.DrawRectangle(Brush, Pen, Rectangle);
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

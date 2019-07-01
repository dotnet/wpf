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
    /// Draws a line with the specified Pen. 
    /// </summary>
    public class DrawLineAction : SetDrawingContextAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public Control Control { get; set; }

        //The pen to stroke the line.
        public Pen Pen { get; set; }

        //The start point of the line.
        public Point StartPoint { get; set; }

        //The end point of the line.
        public Point EndPoint { get; set; }

        public bool IsDrawAnimation { get; set; }

        public PointAnimationUsingPath PointAnimationUsingPath { get; set; }

        #endregion

        public override void Perform()
        {
            //The clock with which to animate the start point of the line, or null for no animation.
            AnimationClock startPointAnimations = PointAnimationUsingPath.CreateClock();

            //The clock with which to animate the end point of the line, or null for no animation.
            AnimationClock endPointAnimations = PointAnimationUsingPath.CreateClock();

            DrawingContext Target = SetDrawingContext();
            if (IsDrawAnimation)
            {
                //Draws a line between the specified points using the specified Pen.
                Target.DrawLine(Pen, StartPoint, EndPoint);
            }
            else
            {
                //Draws a line between the specified points using the specified Pen and applies the specified animation clocks.
                Target.DrawLine(Pen, StartPoint, startPointAnimations, EndPoint, endPointAnimations);
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

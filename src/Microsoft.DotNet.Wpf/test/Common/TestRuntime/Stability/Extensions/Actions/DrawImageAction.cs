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
    /// Draws an image into the region defined by the specified Rect. 
    /// </summary>
    public class DrawImageAction : SetDrawingContextAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public Control Control { get; set; }

        //The image to draw.
        public ImageSource ImageSource { get; set; }

        //The region in which to draw bitmapSource.
        public Rect Rectangle { get; set; }

        public bool IsDrawAnimation { get; set; }

        public RectAnimation RectAnimation { get; set; }

        #endregion

        public override void Perform()
        {
            //The clock with which to animate the rectangle's size and dimensions, or null for no animation.
            AnimationClock rectangleAnimations = RectAnimation.CreateClock();

            DrawingContext Target = SetDrawingContext();

            if (IsDrawAnimation)
            {
                //Draws an image into the region defined by the specified Rect and applies the specified animation clock.
                Target.DrawImage(ImageSource, Rectangle, rectangleAnimations);
            }
            else
            {
                //Draws an image into the region defined by the specified Rect.
                Target.DrawImage(ImageSource, Rectangle);
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

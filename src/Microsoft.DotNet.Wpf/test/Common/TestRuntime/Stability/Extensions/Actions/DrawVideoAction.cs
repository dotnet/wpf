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
    /// Draws a video into the specified region.
    /// </summary>
    public class DrawVideoAction : SetDrawingContextAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public Control Control { get; set; }

        //The area in which to draw the media.
        public Rect Rectangle { get; set; }

        //The media to draw.
        public MediaPlayer Player { get; set; }

        public bool IsDrawAnimation { get; set; }

        public RectAnimation RectAnimation { get; set; }

        #endregion

        public override void Perform()
        {
            //The clock with which to animate the rectangle's size and dimensions.
            AnimationClock RectangleAnimations = RectAnimation.CreateClock();

            DrawingContext Target = SetDrawingContext();

            if (IsDrawAnimation)
            {
                //Draws a video into the specified region and applies the specified animation clock.
                Target.DrawVideo(Player, Rectangle, RectangleAnimations);
            }
            else
            {
                //Draws a video into the specified region.
                Target.DrawVideo(Player, Rectangle);
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

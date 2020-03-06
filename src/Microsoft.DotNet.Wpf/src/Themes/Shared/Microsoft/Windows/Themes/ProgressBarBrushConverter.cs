// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Globalization;
using System.Threading;

using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// The ProgressBarBrushConverter class
    /// </summary>
    public class ProgressBarBrushConverter : IMultiValueConverter
    {
        /// <summary>
        ///      Creates the brush for the ProgressBar
        /// </summary>
        /// <param name="values">ForegroundBrush, IsIndeterminate, Indicator Width, Indicator Height, Track Width</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Brush for the ProgressBar</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //
            // Parameter Validation
            //
            Type doubleType = typeof(double);
            if (values == null ||
                (values.Length != 5) ||
                (values[0] == null)  ||
                (values[1] == null)  ||
                (values[2] == null)  ||
                (values[3] == null) ||
                (values[4] == null) ||
                !typeof(Brush).IsAssignableFrom(values[0].GetType()) || 
                !typeof(bool).IsAssignableFrom(values[1].GetType()) ||
                !doubleType.IsAssignableFrom(values[2].GetType()) ||
                !doubleType.IsAssignableFrom(values[3].GetType()) ||
                !doubleType.IsAssignableFrom(values[4].GetType()))
            {
                return null;
            }
         
            //
            // Conversion
            //

            Brush brush = (Brush)values[0];
            bool isIndeterminate = (bool)values[1];
            double width = (double)values[2];
            double height = (double)values[3];
            double trackWidth = (double)values[4];

            // if an invalid height, return a null brush
            if (width <= 0.0 || Double.IsInfinity(width) || Double.IsNaN(width) ||
                height <= 0.0 || Double.IsInfinity(height) || Double.IsNaN(height) )
            {
                return null;
            }


            DrawingBrush newBrush = new DrawingBrush();

            // Set the viewport and viewbox to the size of the progress region
            newBrush.Viewport = newBrush.Viewbox = new Rect(0, 0, width, height);
            newBrush.ViewportUnits = newBrush.ViewboxUnits = BrushMappingMode.Absolute;
                        
            newBrush.TileMode = TileMode.None;
            newBrush.Stretch = Stretch.None;

            DrawingGroup myDrawing = new DrawingGroup();
            DrawingContext myDrawingContext = myDrawing.Open();

            double drawnWidth = 0.0; // The total width drawn to the brush so far

            double blockWidth = 6.0;
            double blockGap = 2.0;
            double blockTotal = blockWidth + blockGap;

            // For the indeterminate case, just draw a portion of the width
            // And animate the brush
            if (isIndeterminate)
            {
                int blocks = (int)Math.Ceiling(width / blockTotal);

                // The left (X) starting point of the brush
                double left = -blocks * blockTotal;

                // Only draw 30% of the blocks
                double indeterminateWidth = width * .3;

                // Generate the brush so it wraps correctly
                // The brush is larger than the rectangle to fill like so:
                //                +-------------+
                // [] [] [] __ __ |[] [] [] __ _|      
                //                +-------------+
                // Translate Brush =>>
                // To have the marquee line up on the left as the blocks are scrolled off to the right
                // we need to have the second set of blocks offset from the first by the width of the rect
                newBrush.Viewport = newBrush.Viewbox = new Rect(left, 0, indeterminateWidth - left, height);

                // Add an animated translate transfrom
                TranslateTransform translation = new TranslateTransform();

                double milliseconds = blocks * 100; // 100 milliseconds on each position

                DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
                animation.Duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));  // Repeat every 3 seconds
                animation.RepeatBehavior = RepeatBehavior.Forever;

                // Add a keyframe to translate by each block
                for (int i = 1; i <= blocks; i++)
                {
                    double x = i * blockTotal;

                    animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(x, KeyTime.Uniform));   
                }

                // Set the animation to the XProperty
                translation.BeginAnimation(TranslateTransform.XProperty, animation);

                // Set the animated translation on the brush
                newBrush.Transform = translation;

                // Draw the Blocks to the left of the brush that are translated into view 
                // during the animation
                
                // While able to draw complete blocks,
                while ((drawnWidth + blockWidth) < indeterminateWidth)
                {
                    // Draw a block
                    myDrawingContext.DrawRectangle(
                                brush,
                                null,
                                new Rect(left + drawnWidth, 0, blockWidth, height));

                    drawnWidth += blockTotal;
                }

                width = indeterminateWidth; //only need to draw 30% of the blocks
                drawnWidth = 0.0; //reset drawn width and draw the left blocks
            }

            // Draw as many blocks 
            // While able to draw complete blocks,
            while ( (drawnWidth + blockWidth) < width )
            {
                // Draw a block
                myDrawingContext.DrawRectangle(
                            brush,
                            null,
                            new Rect(drawnWidth, 0, blockWidth, height)); 
                
                drawnWidth += blockTotal;
            }

            double remainder = width - drawnWidth;
            // Draw portion of last block when ProgressBar is 100% (ie indicatorWidth == trackWidth)
            if (!isIndeterminate && remainder > 0.0 && Math.Abs(width - trackWidth) < 1.0e-5)
            {
                // Draw incomplete block to fill progress indicator area
                myDrawingContext.DrawRectangle(
                            brush,
                            null,
                            new Rect(drawnWidth, 0, remainder, height));
            }
                       
            myDrawingContext.Close();
            newBrush.Drawing = myDrawing;

            return newBrush;
        }

        /// <summary>
        /// Not Supported
        /// </summary>
        /// <param name="value">value, as produced by target</param>
        /// <param name="targetTypes">target types</param>
        /// <param name="parameter">converter parameter</param>
        /// <param name="culture">culture information</param>
        /// <returns>Nothing</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}

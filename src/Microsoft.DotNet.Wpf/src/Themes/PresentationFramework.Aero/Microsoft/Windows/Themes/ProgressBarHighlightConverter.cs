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
    /// The ProgressBarHighlightConverter class
    /// </summary>
    public class ProgressBarHighlightConverter : IMultiValueConverter
    {
        /// <summary>
        ///      Creates the brush for the ProgressBar
        /// </summary>
        /// <param name="values">ForegroundBrush, IsIndeterminate, Width, Height</param>
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
                (values.Length != 3) ||
                (values[0] == null)  ||
                (values[1] == null)  ||
                (values[2] == null)  ||
                !typeof(Brush).IsAssignableFrom(values[0].GetType()) || 
                !doubleType.IsAssignableFrom(values[1].GetType()) ||
                !doubleType.IsAssignableFrom(values[2].GetType()))
            {
                return null;
            }
         
            //
            // Conversion
            //

            Brush brush = (Brush)values[0];
            double width = (double)values[1];
            double height = (double)values[2];

            // if an invalid height, return a null brush
            if (width <= 0.0 || Double.IsInfinity(width) || Double.IsNaN(width) ||
                height <= 0.0 || Double.IsInfinity(height) || Double.IsNaN(height) )
            {
                return null;
            }

            DrawingBrush newBrush = new DrawingBrush();

            // Create a Drawing Brush that is 2x longer than progress bar track
            //
            // +-------------+..............
            // | highlight   | empty       :  
            // +-------------+.............:
            //
            //  This brush will animate to the right.

            double twiceWidth = width * 2.0;

            // Set the viewport and viewbox to the 2*size of the progress region
            newBrush.Viewport = newBrush.Viewbox = new Rect(-width, 0, twiceWidth, height);
            newBrush.ViewportUnits = newBrush.ViewboxUnits = BrushMappingMode.Absolute;
                        
            newBrush.TileMode = TileMode.None;
            newBrush.Stretch = Stretch.None;

            DrawingGroup myDrawing = new DrawingGroup();
            DrawingContext myDrawingContext = myDrawing.Open();

            // Draw the highlight
            myDrawingContext.DrawRectangle(brush, null, new Rect(-width, 0, width, height));


            // Animate the Translation

            TimeSpan translateTime = TimeSpan.FromSeconds(twiceWidth / 200.0); // travel at 200px /second
            TimeSpan pauseTime = TimeSpan.FromSeconds(1.0);  // pause 1 second between animations

            DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
            animation.BeginTime = TimeSpan.Zero;
            animation.Duration = new Duration(translateTime + pauseTime);
            animation.RepeatBehavior = RepeatBehavior.Forever;
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(twiceWidth, translateTime));

            TranslateTransform translation = new TranslateTransform();

            // Set the animation to the XProperty
            translation.BeginAnimation(TranslateTransform.XProperty, animation);

            // Set the animated translation on the brush
            newBrush.Transform = translation;

                       
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

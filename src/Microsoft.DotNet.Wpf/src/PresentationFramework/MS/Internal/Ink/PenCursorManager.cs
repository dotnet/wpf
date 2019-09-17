// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description:
//      PenCursorManager is helper class which creates Cursor object for InkCanvas' Pen and Eraser
//


//#define CURSOR_DEBUG

using MS.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MS.Internal.AppModel;

namespace MS.Internal.Ink
{
    /// <summary>
    /// A static class which generates the cursors for InkCanvas
    /// </summary>
    internal static class PenCursorManager
    {
        //-------------------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Create a pen cursor from DrawingAttributes object
        /// </summary>
        internal static Cursor GetPenCursor(DrawingAttributes drawingAttributes, bool isHollow, bool isRightToLeft, double dpiScaleX, double dpiScaleY)
        {
            // Create pen Drawing.
            Drawing penDrawing = CreatePenDrawing(drawingAttributes, isHollow, isRightToLeft, dpiScaleX, dpiScaleY);

            // Create Cursor from Drawing
            return CreateCursorFromDrawing(penDrawing, new Point(0, 0));
        }

        /// <summary>
        /// Create a point eraser cursor from StylusShape
        /// </summary>
        /// <param name="stylusShape">Eraser Shape</param>
        /// <param name="tranform">Transform</param>
        /// <returns></returns>
        internal static Cursor GetPointEraserCursor(StylusShape stylusShape, Matrix tranform, double dpiScaleX, double dpiScaleY)
        {
            Debug.Assert(DoubleUtil.IsZero(tranform.OffsetX) && DoubleUtil.IsZero(tranform.OffsetY), "The EraserShape cannot be translated.");
            Debug.Assert(tranform.HasInverse, "The transform has to be invertable.");

            // Create a DA with IsHollow being set. A point eraser will be rendered to a hollow stroke.
            DrawingAttributes da = new DrawingAttributes();
            if (stylusShape.GetType() == typeof(RectangleStylusShape))
            {
                da.StylusTip = StylusTip.Rectangle;
            }
            else
            {
                da.StylusTip = StylusTip.Ellipse;
            }

            da.Height = stylusShape.Height;
            da.Width = stylusShape.Width;
            da.Color = Colors.Black;

            if ( !tranform.IsIdentity )
            {
                // Apply the LayoutTransform and/or RenderTransform
                da.StylusTipTransform *= tranform;
            }

            if ( !DoubleUtil.IsZero(stylusShape.Rotation) )
            {
                // Apply the tip rotation
                Matrix rotationMatrix = Matrix.Identity;
                rotationMatrix.Rotate(stylusShape.Rotation);
                da.StylusTipTransform *= rotationMatrix;
            }

            // Forward to GetPenCursor.
            return GetPenCursor(da, true, false/*isRightToLeft*/, dpiScaleX, dpiScaleY);
        }

        /// <summary>
        /// Create a stroke eraser cursor
        /// </summary>
        /// <returns></returns>
        internal static Cursor GetStrokeEraserCursor()
        {
            if ( s_StrokeEraserCursor == null )
            {
                // Get Drawing
                Drawing drawing = CreateStrokeEraserDrawing();
                s_StrokeEraserCursor = CreateCursorFromDrawing(drawing, new Point(5, 5));
            }

            // Return cursor.
            return s_StrokeEraserCursor;
        }

        /// <summary>
        /// Retrieve selection cursor
        /// </summary>
        /// <param name="hitResult">hitResult</param>
        /// <param name="isRightToLeft">True if InkCanvas.FlowDirection is RightToLeft, false otherwise</param>
        /// <returns></returns>
        internal static Cursor GetSelectionCursor(InkCanvasSelectionHitResult hitResult, bool isRightToLeft)
        {
            Cursor cursor;

            switch ( hitResult )
            {
                case InkCanvasSelectionHitResult.TopLeft:
                case InkCanvasSelectionHitResult.BottomRight:
                    {
                        if (isRightToLeft)
                        {
                            cursor = Cursors.SizeNESW;
                        }
                        else
                        {
                            cursor = Cursors.SizeNWSE;
                        }
                        break;
                    }

                case InkCanvasSelectionHitResult.Bottom:
                case InkCanvasSelectionHitResult.Top:
                    {
                        cursor = Cursors.SizeNS;
                        break;
                    }

                case InkCanvasSelectionHitResult.BottomLeft:
                case InkCanvasSelectionHitResult.TopRight:
                    {
                        if (isRightToLeft)
                        {
                            cursor = Cursors.SizeNWSE;
                        }
                        else
                        {
                            cursor = Cursors.SizeNESW;
                        }
                        break;
                    }

                case InkCanvasSelectionHitResult.Left:
                case InkCanvasSelectionHitResult.Right:
                    {
                        cursor = Cursors.SizeWE;
                        break;
                    }
                case InkCanvasSelectionHitResult.Selection:
                    {
                        cursor = Cursors.SizeAll;
                        break;
                    }
                default:
                    {
                        // By default, use the Cross cursor.
                        cursor = Cursors.Cross;
                        break;
                    }
            }

            return cursor;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Create a Cursor from a Drawing object
        /// </summary>
        /// <param name="drawing">Drawing</param>
        /// <param name="hotspot">Cursor Hotspot</param>
        /// <returns></returns>
        private static Cursor CreateCursorFromDrawing(Drawing drawing, Point hotspot)
        {
            // A default cursor.
            Cursor cursor = Cursors.Arrow;

            Rect drawingBounds = drawing.Bounds;

            double originalWidth = drawingBounds.Width;
            double originalHeight = drawingBounds.Height;

            // Cursors like to be multiples of 8 in dimension.
            int width = IconHelper.AlignToBytes(drawingBounds.Width, 1);
            int height = IconHelper.AlignToBytes(drawingBounds.Height, 1);

            // Now inflate the drawing bounds to the new dimension.
            drawingBounds.Inflate((width - originalWidth) / 2, (height - originalHeight) / 2);

            // Translate the hotspot accordingly.
            int xHotspot = (int)Math.Round(hotspot.X - drawingBounds.Left);
            int yHotspot = (int)Math.Round(hotspot.Y - drawingBounds.Top);

            // Create a DrawingVisual which represents the cursor drawing.
            DrawingVisual cursorDrawingVisual = CreateCursorDrawingVisual(drawing, width, height);

            // Render the cursor visual to a bitmap
            RenderTargetBitmap rtb = RenderVisualToBitmap(cursorDrawingVisual, width, height);

            // Get pixel data in Bgra32 fromat from the bitmap
            byte[] pixels = GetPixels(rtb, width, height);

            NativeMethods.IconHandle finalCursor = IconHelper.CreateIconCursor(pixels, width, height, xHotspot, yHotspot, false);

            if ( finalCursor.IsInvalid )
            {
                // Return the default cursor if above is failed.
                return Cursors.Arrow;
            }

            cursor = CursorInteropHelper.CriticalCreate(finalCursor);
            return cursor;
        }

        /// <summary>
        /// Create a DrawingVisual from a Drawing
        /// </summary>
        /// <param name="drawing"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static DrawingVisual CreateCursorDrawingVisual(Drawing drawing, int width, int height)
        {
            // Create a drawing brush with the drawing as its content.
            DrawingBrush db = new DrawingBrush(drawing);
            db.Stretch = Stretch.None;
            db.AlignmentX = AlignmentX.Center;
            db.AlignmentY = AlignmentY.Center;

            // Create a drawing visual with our drawing brush.
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext dc = null;
            try
            {
                dc = drawingVisual.RenderOpen();
                dc.DrawRectangle(db, null, new Rect(0, 0, width, height));
            }
            finally
            {
                if ( dc != null )
                {
                    dc.Close();
                }
            }

            return drawingVisual;
        }

        /// <summary>
        /// Renders a visual into a bitmap
        /// </summary>
        /// <param name="visual">visual</param>
        /// <param name="width">Bitmap width</param>
        /// <param name="height">Bitmap height</param>
        /// <returns>A bitmap object</returns>
        private static RenderTargetBitmap RenderVisualToBitmap(Visual visual, int width, int height)
        {
            // Use RenderTargetBitmap and BitmapVisualManager to render drawing visual into
            // a bitmap
            RenderTargetBitmap rtb =
                    new RenderTargetBitmap  (width, height,
                                            96, 96,
                                            PixelFormats.Pbgra32);
            rtb.Render(visual);
            return rtb;
        }

        /// <summary>
        /// Get bitmap pixel data in Bgra32 format from a custom Drawing.
        /// </summary>
        /// <param name="rtb">A bitmap</param>
        /// <param name="width">Bitmap width</param>
        /// <param name="height">Bitmap height</param>
        /// <returns></returns>
        private static byte[] GetPixels(RenderTargetBitmap rtb, int width, int height)
        {
            int strideColorBitmap = width * 4 /* 32 BitsPerPixel */;

            // Convert the bitmap from Pbgra32 to Bgra32
            FormatConvertedBitmap converter = new FormatConvertedBitmap();
            converter.BeginInit();
            converter.Source = rtb;
            converter.DestinationFormat = PixelFormats.Bgra32;
            converter.EndInit();

            byte[] pixels = new byte[strideColorBitmap * height];

            // Call the internal method which skips the MediaPermission Demand
            converter.CriticalCopyPixels(Int32Rect.Empty, pixels, strideColorBitmap, 0);

            return pixels;
        }


        /// <summary>
        /// Custom Pen Drawing
        /// </summary>
        private static Drawing CreatePenDrawing(DrawingAttributes drawingAttributes, bool isHollow, bool isRightToLeft, double dpiScaleX, double dpiScaleY)
        {
            // Create a single point stroke.
            StylusPointCollection stylusPoints = new StylusPointCollection();
            stylusPoints.Add(new StylusPoint(0f, 0f));

            DrawingAttributes da = new DrawingAttributes();
            da.Color = drawingAttributes.Color;
            da.Width = drawingAttributes.Width;
            da.Height = drawingAttributes.Height;
            da.StylusTipTransform = drawingAttributes.StylusTipTransform;
            da.IsHighlighter = drawingAttributes.IsHighlighter;
            da.StylusTip = drawingAttributes.StylusTip;

            Stroke singleStroke = new Stroke(stylusPoints, da);
            // 
            // We should draw our cursor in the device unit since it's device dependent object.
            singleStroke.DrawingAttributes.Width = ConvertToPixel(singleStroke.DrawingAttributes.Width, dpiScaleX);
            singleStroke.DrawingAttributes.Height = ConvertToPixel(singleStroke.DrawingAttributes.Height, dpiScaleY);

            double maxLength = Math.Min(SystemParameters.PrimaryScreenWidth / 2, SystemParameters.PrimaryScreenHeight / 2);

            //
            // NOTE: there are two ways to set the width / height of a stroke
            // 1) using .Width and .Height
            // 2) using StylusTipTransform and specifying a scale
            // these two can multiply and we need to prevent the size from ever going
            // over maxLength or under 1.0.  The simplest way to check if we're too big
            // is by checking the bounds of the stroke, which takes both into account
            //
            Rect strokeBounds = singleStroke.GetBounds();
            bool outOfBounds = false;

            // Make sure that the cursor won't exceed the minimum or the maximum boundary.
            if ( DoubleUtil.LessThan(strokeBounds.Width, 1.0) )
            {
                singleStroke.DrawingAttributes.Width = 1.0;
                outOfBounds = true;
            }
            else if ( DoubleUtil.GreaterThan(strokeBounds.Width, maxLength) )
            {
                singleStroke.DrawingAttributes.Width = maxLength;
                outOfBounds = true;
            }

            if ( DoubleUtil.LessThan(strokeBounds.Height, 1.0) )
            {
                singleStroke.DrawingAttributes.Height = 1.0;
                outOfBounds = true;
            }
            else if ( DoubleUtil.GreaterThan(strokeBounds.Height, maxLength) )
            {
                singleStroke.DrawingAttributes.Height = maxLength;
                outOfBounds = true;
            }

            //drop the StylusTipTransform if we're out of bounds.  we might
            //consider trying to preserve any transform but this is such a rare
            //case (scaling over or under with a STT) that we don't care.
            if (outOfBounds)
            {
                singleStroke.DrawingAttributes.StylusTipTransform = Matrix.Identity;
            }

            if (isRightToLeft)
            {
                //reverse left to right to right to left
                Matrix xf = singleStroke.DrawingAttributes.StylusTipTransform;
                xf.Scale(-1, 1);

                //only set the xf if it has an inverse or the STT will throw
                if (xf.HasInverse)
                {
                    singleStroke.DrawingAttributes.StylusTipTransform = xf;
                }
            }

            DrawingGroup penDrawing = new DrawingGroup();
            DrawingContext dc = null;

            try
            {
                dc = penDrawing.Open();

                // Call the internal drawing method on Stroke to draw as hollow if isHollow == true
                if ( isHollow )
                {
                    singleStroke.DrawInternal(dc, singleStroke.DrawingAttributes, isHollow);
                }
                else
                {
                    // Invoke the public Draw method which will handle the Highlighter correctly.
                    singleStroke.Draw(dc, singleStroke.DrawingAttributes);
                }
            }
            finally
            {
                if ( dc != null )
                {
                    dc.Close();
                }
            }

            return penDrawing;
        }

        /// <summary>
        /// Custom StrokeEraser Drawing
        /// </summary>
        /// <returns></returns>
        private static Drawing CreateStrokeEraserDrawing()
        {
            DrawingGroup drawingGroup = new DrawingGroup();
            DrawingContext dc = null;

            try
            {
                dc = drawingGroup.Open();
                LinearGradientBrush brush1 = new LinearGradientBrush(
                                                    Color.FromRgb(240, 242, 255),   // Start Color
                                                    Color.FromRgb(180, 207, 248),   // End Color
                                                    45f                             // Angle
                                                    );
                brush1.Freeze();

                SolidColorBrush brush2 = new SolidColorBrush(Color.FromRgb(180, 207, 248));
                brush2.Freeze();

                Pen pen1 = new Pen(Brushes.Gray, 0.7);
                pen1.Freeze();

                PathGeometry pathGeometry = new PathGeometry();

                PathFigure path = new PathFigure();
                path.StartPoint = new Point(5, 5);

                LineSegment segment = new LineSegment(new Point(16, 5), true);
                segment.Freeze();
                path.Segments.Add(segment);

                segment = new LineSegment(new Point(26, 15), true);
                segment.Freeze();
                path.Segments.Add(segment);

                segment = new LineSegment(new Point(15, 15), true);
                segment.Freeze();
                path.Segments.Add(segment);

                segment = new LineSegment(new Point(5, 5), true);
                segment.Freeze();
                path.Segments.Add(segment);

                path.IsClosed = true;
                path.Freeze();

                pathGeometry.Figures.Add(path);

                path = new PathFigure();
                path.StartPoint = new Point(5, 5);

                segment = new LineSegment(new Point(5, 10), true);
                segment.Freeze();
                path.Segments.Add(segment);

                segment = new LineSegment(new Point(15, 19), true);
                segment.Freeze();
                path.Segments.Add(segment);

                segment = new LineSegment(new Point(15, 15), true);
                segment.Freeze();
                path.Segments.Add(segment);

                segment = new LineSegment(new Point(5, 5), true);
                segment.Freeze();
                path.Segments.Add(segment);
                path.IsClosed = true;
                path.Freeze();

                pathGeometry.Figures.Add(path);
                pathGeometry.Freeze();

                PathGeometry pathGeometry1 = new PathGeometry();
                path = new PathFigure();
                path.StartPoint = new Point(15, 15);

                segment = new LineSegment(new Point(15, 19), true);
                segment.Freeze();
                path.Segments.Add(segment);

                segment = new LineSegment(new Point(26, 19), true);
                segment.Freeze();
                path.Segments.Add(segment);

                segment = new LineSegment(new Point(26, 15), true);
                segment.Freeze();
                path.Segments.Add(segment);
                segment.Freeze();
                segment = new LineSegment(new Point(15, 15), true);

                path.Segments.Add(segment);
                path.IsClosed = true;
                path.Freeze();

                pathGeometry1.Figures.Add(path);
                pathGeometry1.Freeze();

                dc.DrawGeometry(brush1, pen1, pathGeometry);
                dc.DrawGeometry(brush2, pen1, pathGeometry1);
                dc.DrawLine(pen1, new Point(5, 5), new Point(5, 0));
                dc.DrawLine(pen1, new Point(5, 5), new Point(0, 5));
                dc.DrawLine(pen1, new Point(5, 5), new Point(2, 2));
                dc.DrawLine(pen1, new Point(5, 5), new Point(8, 2));
                dc.DrawLine(pen1, new Point(5, 5), new Point(2, 8));
            }
            finally
            {
                if ( dc != null )
                {
                    dc.Close();
                }
            }

            return drawingGroup;
        }

        /// <summary>
        /// Convert values from Avalon unit to the current display unit.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static double ConvertToPixel(double value, double dpiScale)
        {
            if ( dpiScale != 0 )
            {
                return value * dpiScale;
            }

            return value;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private static Cursor s_StrokeEraserCursor;

        #endregion Private Fields
    }
}

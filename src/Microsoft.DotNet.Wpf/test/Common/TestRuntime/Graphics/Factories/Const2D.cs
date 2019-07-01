// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

using Microsoft.Test.Graphics.TestTypes;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// Constants used throughout testing
    /// </summary>
    public class Const2D
    {

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point p0 { get { return new Point(0, 0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point p10 { get { return new Point(10, -10); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point pInf { get { return new Point(negInf, posInf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point pMaxMin { get { return new Point(max, min); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point pNan { get { return new Point(nan, nan); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point pEpsilon { get { return new Point(epsilon, epsilon); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point[] points
        {
            get
            {
                Point[] point = new Point[5];
                for (int i = 0; i < point.Length; i++)
                {
                    point[i].X = i - 2;
                    point[i].Y = i - 2;
                }

                return point;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Vector v0 { get { return new Vector(0, 0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Vector v10 { get { return new Vector(10, -10); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Vector vInf { get { return new Vector(negInf, posInf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Vector vMaxMin { get { return new Vector(max, min); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Vector vNan { get { return new Vector(nan, nan); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Vector vEpsilon { get { return new Vector(epsilon, epsilon); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Vector[] vectors
        {
            get
            {
                Vector[] vector = new Vector[5];
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i].X = i - 2;
                    vector[i].Y = i - 2;
                }

                return vector;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Int32Rect int32Rect0 { get { return new Int32Rect(0, 0, 0, 0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Int32Rect int32RectMax
        {
            get
            {
                return new Int32Rect(System.Int32.MinValue, System.Int32.MinValue,
                                      System.Int32.MaxValue, System.Int32.MaxValue);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Int32Rect int32RectMin
        {
            get
            {
                return new Int32Rect(System.Int32.MaxValue, System.Int32.MaxValue,
                                      System.Int32.MinValue, System.Int32.MinValue);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect rect1 { get { return new Rect(1, 1, 1, 1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect rectInf { get { return new Rect(negInf, negInf, posInf, posInf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect rectMax { get { return new Rect(min, min, max, max); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect rectNan { get { return new Rect(nan, nan, nan, nan); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect rectEpsilon { get { return new Rect(epsilon, epsilon, epsilon, epsilon); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect rectEmpty { get { return Rect.Empty; } }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size size1 { get { return new Size(1, 1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size sizeInf { get { return new Size(posInf, posInf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size sizeMax { get { return new Size(max, max); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size sizeNan { get { return new Size(nan, nan); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size sizeEpsilon { get { return new Size(epsilon, epsilon); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size sizeEmpty { get { return Size.Empty; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static double posInf { get { return System.Double.PositiveInfinity; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static double negInf { get { return System.Double.NegativeInfinity; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static double max { get { return System.Double.MaxValue; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static double min { get { return System.Double.MinValue; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static double nan { get { return System.Double.NaN; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static double epsilon { get { return System.Double.Epsilon; } }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Matrix matrix0 { get { return new Matrix(0, 0, 0, 0, 0, 0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Matrix matrix2 { get { return new Matrix(posInf, negInf, epsilon, min, max, nan); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Matrix matrix10 { get { return new Matrix(10, 10, 10, 10, 10, 10); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Matrix matrix20 { get { return new Matrix(20, 20, 20, 20, 20, 20); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Matrix typeIdentity
        {
            get
            {
                Matrix matrix = new Matrix(11.11, -22.22, 33.33, -44.44, 55.55, -66.66);
                matrix.SetIdentity();

                return matrix;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Transform identityTransform { get { return Transform.Identity; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static TranslateTransform translate10 { get { return new TranslateTransform(10, 10); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static TranslateTransform translate20 { get { return new TranslateTransform(20, 20); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static RotateTransform rotate10 { get { return new RotateTransform(10, p0.X, p0.Y); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static RotateTransform rotate20 { get { return new RotateTransform(20, p0.X, p0.Y); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ScaleTransform scale10 { get { return new ScaleTransform(10, 10, p0.X, p0.Y); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ScaleTransform scale20 { get { return new ScaleTransform(20, 20, p0.X, p0.Y); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static SkewTransform skew10 { get { return new SkewTransform(10, 10, p0.X, p0.Y); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static SkewTransform skew20 { get { return new SkewTransform(20, 20, p0.X, p0.Y); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MatrixTransform matrixTransform10 { get { return new MatrixTransform(10, 10, 10, 10, 10, 10); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MatrixTransform matrixTransform20 { get { return new MatrixTransform(20, 20, 20, 20, 20, 20); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static TransformCollection transformCollection1
        {
            get
            {
                TransformCollection transformCollection = new TransformCollection();
                transformCollection.Add(translate10);
                transformCollection.Add(rotate20);
                transformCollection.Add(scale20);
                transformCollection.Add(skew10);
                transformCollection.Add(matrixTransform10);

                return transformCollection;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static TransformGroup transformGroup1
        {
            get
            {
                TransformGroup transformGroup1 = new TransformGroup();
                transformGroup1.Children = transformCollection1;

                TransformGroup transformGroup2 = new TransformGroup();
                transformGroup2.Children = transformCollection1;

                transformGroup1.Children.Add(transformGroup2);

                return transformGroup1;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PointCollection pCollection
        {
            get
            {
                PointCollection c = new PointCollection();

                c.Add(new Point(0, 0));
                c.Add(new Point(11.11, 0.11));
                c.Add(new Point(-11.11, -0.11));
                c.Add(new Point(max, min));

                return c;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PointCollection pCollection2
        {
            get
            {
                PointCollection c = new PointCollection();

                c.Add(p0);
                c.Add(p10);
                c.Add(pInf);
                c.Add(pMaxMin);
                c.Add(pNan);
                c.Add(pEpsilon);

                return c;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static VectorCollection vCollection
        {
            get
            {
                VectorCollection c = new VectorCollection();

                c.Add(new Vector(0, 0));
                c.Add(new Vector(11.11, 0.11));
                c.Add(new Vector(-11.11, -0.11));
                c.Add(new Vector(max, min));

                return c;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static VectorCollection vCollection2
        {
            get
            {
                VectorCollection c = new VectorCollection();

                c.Add(v0);
                c.Add(v10);
                c.Add(vInf);
                c.Add(vMaxMin);
                c.Add(vNan);
                c.Add(vEpsilon);

                return c;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Int32Collection intCollection
        {
            get
            {
                Int32Collection c = new Int32Collection();

                c.Add(0);
                c.Add(-10);
                c.Add(10);
                c.Add(System.Int32.MaxValue);
                c.Add(System.Int32.MinValue);

                return c;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DoubleCollection doubleCollection
        {
            get
            {
                DoubleCollection c = new DoubleCollection();

                c.Add(0);
                c.Add(-11.11);
                c.Add(11.11);
                c.Add(max);
                c.Add(min);
                c.Add(posInf);
                c.Add(negInf);
                c.Add(epsilon);
                c.Add(nan);

                return c;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static GuidelineSet guidelineSet1
        {
            get
            {
                GuidelineSet gc = new GuidelineSet();
                gc.GuidelinesX = doubleCollection;
                gc.GuidelinesY = doubleCollection;

                return gc;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FormattedText formattedText1 { get { return new FormattedText("formatted text", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 22.22, Brushes.Red); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static GlyphRun glyphRun1
        {
            get
            {
                return CreateGlyphRun(new Point(1.5, 1.5), // origin
                                       100.0f, //font size
                                       "abcdef" // string
                                       );
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static GlyphRunDrawing glyphRunDrawing1 { get { return new GlyphRunDrawing(new SolidColorBrush(Colors.Red), glyphRun1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static GeometryDrawing geometryDrawing1 { get { return new GeometryDrawing(new SolidColorBrush(Colors.Red), new Pen(new SolidColorBrush(Colors.Green), 1.5), new RectangleGeometry(new Rect(new Point(-1.5, -1.5), new Point(1.5, 1.5)))); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ImageDrawing imageDrawing1 { get { return new ImageDrawing(new BitmapImage(new Uri("wood.bmp", UriKind.RelativeOrAbsolute)), new Rect(new Point(-1.5, -1.5), new Point(1.5, 1.5))); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MediaPlayer MediaPlayer
        {
            get
            {
                MediaPlayer mp = new MediaPlayer();
                mp.Clock = new MediaTimeline(new Uri(EnvironmentWrapper.CurrentDirectory + @"\ep2.wmv")).CreateClock();
                return mp;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static VideoDrawing videoDrawing1
        {
            get
            {
                VideoDrawing videoDrawing = new VideoDrawing();
                videoDrawing.Player = MediaPlayer;
                videoDrawing.Rect = new Rect(1.5, -1.5, 1.5, 1.5);

                return videoDrawing;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DrawingCollection drawingCollection1
        {
            get
            {
                DrawingCollection drawingCollection = new DrawingCollection();
                drawingCollection.Add(geometryDrawing1);
                drawingCollection.Add(glyphRunDrawing1);
                drawingCollection.Add(imageDrawing1);
                return drawingCollection;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DrawingGroup drawingGroup1
        {
            get
            {
                DrawingGroup drawingGroup = new DrawingGroup();
                drawingGroup.Opacity = 0.5;
                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(new Point(-1.5, -1.5), new Point(1.5, 1.5)));
                drawingGroup.Transform = translate10;
                drawingGroup.Children = drawingCollection1;
                return drawingGroup;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DrawingGroup drawingGroupNullChildren
        {
            get
            {
                DrawingGroup drawingGroup = new DrawingGroup();
                drawingGroup.Children = null;
                return drawingGroup;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AnimationClock doubleAnimationClock
        {
            get
            {
                DoubleAnimation doubleAnimation = new DoubleAnimation();
                doubleAnimation.From = -10;
                doubleAnimation.To = 20;
                doubleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(2000));
                doubleAnimation.BeginTime = TimeSpan.FromMilliseconds(500);
                doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
                doubleAnimation.AutoReverse = true;

                return (AnimationClock)doubleAnimation.CreateClock();
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AnimationClock pointAnimationClock
        {
            get
            {
                PointAnimation pointAnimation = new PointAnimation();
                pointAnimation.From = new Point(-200, -200);
                pointAnimation.To = new Point(200, 200);
                pointAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(2000));
                pointAnimation.BeginTime = TimeSpan.FromMilliseconds(500);
                pointAnimation.RepeatBehavior = RepeatBehavior.Forever;
                pointAnimation.AutoReverse = true;

                return (AnimationClock)pointAnimation.CreateClock();
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AnimationClock rectAnimationClock
        {
            get
            {
                RectAnimation rectAnimation = new RectAnimation();
                rectAnimation.From = new Rect(0, 0, 100, 100);
                rectAnimation.To = new Rect(-100, -100, 200, 50);
                rectAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(2000));
                rectAnimation.BeginTime = TimeSpan.FromMilliseconds(500);
                rectAnimation.RepeatBehavior = RepeatBehavior.Forever;
                rectAnimation.AutoReverse = true;

                return (AnimationClock)rectAnimation.CreateClock();
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PathFigure pathFigure1
        {
            get
            {
                PathFigure pathFigure = new PathFigure();
                pathFigure.StartPoint = new Point(-1.2, 10000);
                pathFigure.IsClosed = true;
                return pathFigure;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PathFigure pathFigure2
        {
            get
            {
                PathFigure pathFigure = new PathFigure();
                pathFigure.StartPoint = new Point(0, 0);
                pathFigure.Segments.Add(new LineSegment(new Point(90, 10.223), true));
                pathFigure.Segments.Add(new ArcSegment(new Point(180, 23), new Size(20, 40), 49, true, SweepDirection.Counterclockwise, true));
                pathFigure.Segments.Add(new BezierSegment(new Point(-10, 32), new Point(2.33, 10), new Point(-10, 33), true));
                return pathFigure;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PathFigureCollection pathFigureCollection1 { get { return new PathFigureCollection(); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PathFigureCollection pathFigureCollection2
        {
            get
            {
                PathFigureCollection pfc = new PathFigureCollection();
                pfc.Add(pathFigure1);
                pfc.Add(pathFigure2);
                return pfc;
            }
        }

        private static GradientStopCollection GetGradient()
        {
            GradientStopCollection GSColl = new GradientStopCollection();
            GSColl.Add(new GradientStop(Colors.Red, 0.0));
            GSColl.Add(new GradientStop(Colors.Green, 0.5));
            GSColl.Add(new GradientStop(Colors.Blue, 1.0));
            return GSColl;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static LinearGradientBrush linearGradientBrush1 { get { return new LinearGradientBrush(GetGradient()); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static RadialGradientBrush radialGradientBrush1 { get { return new RadialGradientBrush(GetGradient()); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ImageBrush imageBrush1 { get { return new ImageBrush(new BitmapImage(new Uri("wood.bmp", UriKind.RelativeOrAbsolute))); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DrawingBrush drawingBrush1 { get { return new DrawingBrush(drawingGroup1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static VisualBrush visualBrush1
        {
            get
            {
                VisualBrush visualBrush = new VisualBrush();

                DrawingVisual drawingVisual = new DrawingVisual();
                DrawingContext drawingContext = drawingVisual.RenderOpen();
                drawingContext.DrawRectangle(Brushes.Red, new Pen(Brushes.Black, 3), new Rect(0, 0, 100, 100));
                drawingContext.Close();

                visualBrush.Visual = drawingVisual;
                return visualBrush;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DrawingImage drawingImage1
        {
            get
            {
                DrawingImage drawingImage = new DrawingImage();
                drawingImage.Drawing = drawingGroup1;
                return drawingImage;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static BitmapEffect bitmapEffect1
        {
            get
            {
                BitmapEffect bitmapEffect = new DropShadowBitmapEffect();
                ((DropShadowBitmapEffect)bitmapEffect).ShadowDepth = 100.23;
                return bitmapEffect;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static BitmapEffectInput bitmapEffectInput1
        {
            get
            {
                BitmapEffectInput bitmapEffectInput = new BitmapEffectInput();
                bitmapEffectInput.AreaToApplyEffect = Rect.Empty;
                return bitmapEffectInput;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static LineGeometry lineGeometry { get { return new LineGeometry(p0, p10); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static RectangleGeometry rectangleGeometry { get { return new RectangleGeometry(rect1, 10, 10); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static EllipseGeometry ellipseGeometry { get { return new EllipseGeometry(p10, 5, 10.23); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static CombinedGeometry combinedGeometry
        {
            get
            {
                CombinedGeometry cg = new CombinedGeometry();
                cg.GeometryCombineMode = GeometryCombineMode.Intersect;
                cg.Geometry1 = lineGeometry;
                cg.Geometry2 = rectangleGeometry;
                return cg;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static GeometryGroup geometryGroup
        {
            get
            {
                GeometryGroup gg = new GeometryGroup();
                gg.Children.Add(lineGeometry);
                gg.Children.Add(rectangleGeometry);
                gg.Children.Add(ellipseGeometry);
                return gg;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PathGeometry pathGeometry
        {
            get
            {
                PathGeometry pg = new PathGeometry();
                pg.Figures.Add(pathFigure1);
                pg.Figures.Add(pathFigure2);
                return pg;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static StreamGeometry streamGeometry
        {
            get
            {
                StreamGeometry sg = new StreamGeometry();
                using (StreamGeometryContext sgc = sg.Open())
                {
                    sgc.BeginFigure(new Point(10, 10), true, true);
                    sgc.LineTo(new Point(100, 100), true, false);
                }
                return sg;
            }
        }

        private static GlyphRun CreateGlyphRun(Point origin, double fontSize, string unicodeString)
        {
            Uri fontUri = new Uri(EnvironmentWrapper.ExpandEnvironmentVariables(@"%windir%\fonts\cour.ttf"));
            GlyphTypeface glyphTypeface = new GlyphTypeface(fontUri);

            IList<ushort> glyphIndices = new List<ushort>();
            for (int i = 0; i < unicodeString.Length; ++i)
            {
                glyphIndices.Add(glyphTypeface.CharacterToGlyphMap[unicodeString[i]]);
            }

            IList<double> advanceWidths = new List<double>();
            for (int i = 0; i < glyphIndices.Count; ++i)
            {
                advanceWidths.Add(glyphTypeface.AdvanceWidths[glyphIndices[i]] * fontSize);
            }
            GlyphRun glyphRun = new GlyphRun(glyphTypeface,   //glyph typeface
                                               0,               //bidi level
                                               false,           //sideways
                                               fontSize,        //font size
                                               glyphIndices,    //glyph indices
                                               origin,          //baseline origin
                                               advanceWidths,   //advance widths
                                               null,            //glyph offsets
                                               unicodeString.ToCharArray(),   //test string
                                               null,            //device font name
                                               null,            //cmap
                                               null,            //caret stop
                                               null             //culture info
                                             );
            return glyphRun;
        }
    }
}

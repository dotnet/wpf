// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using System.Windows.Media.Animation;

using Microsoft.Test.Graphics.TestTypes;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// This summary has not been prepared yet. NOSUMMARY - pantal07
    /// </summary>
    public class BrushFactory
    {
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush MakeBrush(string brush)
        {
            string[] parsedBrush = brush.Split(' ');

            switch (parsedBrush[0])
            {
                case "SolidRed":
                    return Brushes.Red.Clone();

                case "SolidColorBrush":
                    return new SolidColorBrush(StringConverter.ToColor(parsedBrush[1]));

                case "LinearGradientBrush":
                    if (parsedBrush.Length <= 4)
                    {
                        return new LinearGradientBrush(
                                StringConverter.ToColor(parsedBrush[1]),    // 1st Color
                                StringConverter.ToColor(parsedBrush[2]),    // 2nd Color
                                StringConverter.ToDouble(parsedBrush[3])); // Angle
                    }
                    return new LinearGradientBrush(
                            StringConverter.ToColor(parsedBrush[1]),        // 1st Color
                            StringConverter.ToColor(parsedBrush[2]),        // 2nd Color
                            StringConverter.ToPoint(parsedBrush[3]),        // Start Point
                            StringConverter.ToPoint(parsedBrush[4]));      // End Point

                case "RadialGradientBrush":
                    return new RadialGradientBrush(
                            StringConverter.ToColor(parsedBrush[1]),        // 1st Color
                            StringConverter.ToColor(parsedBrush[2]));      // 2nd Color

                case "ImageBrush":
                    return new ImageBrush(MakeImageSource(parsedBrush[1]));

                case "DrawingBrush": return BrushDrawing;
                case "DrawingBrushImage": return BrushDrawingImage(parsedBrush[1]);
                case "DrawingBrushShapes": return BrushDrawingShapes;
                case "DrawingBrushShapesOpacity": return BrushDrawingShapesOpacity;
                case "DrawingBrushText": return BrushDrawingText;
                case "DrawingBrushTextOpacity": return BrushDrawingTextOpacity;
                case "VisualBrush": return BrushVisual;
                case "VisualBrushButton": return BrushVisual;     // VisualBrushButton is the same as VisualBrush
                case "VisualBrushOpacity": return BrushVisualOpacity;
                case "VisualBrushButtonOpacity": return BrushVisualOpacity;
                case "VisualBrush3D": return BrushVisual3D;
                case "VisualBrush3DOpacity": return BrushVisual3DOpacity;
                case "VisualBrushShapes": return BrushVisualShapes;
                case "VisualBrushShapesOpacity": return BrushVisualShapesOpacity;
                case "VisualBrushDrawing": return BrushVisualDrawing;

                case "VideoBrush":
                    Uri videoUri = new Uri(parsedBrush[1], UriKind.RelativeOrAbsolute);
                    return MakeVideoBrush(videoUri, StringConverter.ToInt(parsedBrush[2]));

                default:
                    return MakeBrushColorImage(brush);
            }
        }

        private static Brush MakeBrushColorImage(string brush)
        {
            // The string we pass in is either:
            //  - A Color - in which case we create a SolidColorBrush
            //  - A Filename - in which case we create an ImageBrush from that file

            try
            {
                Color c = StringConverter.ToColor(brush);
                return new SolidColorBrush(c);
            }
            catch (ArgumentException)
            {
                // We must have asked for another brush
            }

            try
            {
                return new ImageBrush(MakeImageSource(brush));
            }
            catch (Exception)
            {
                throw new ArgumentException(String.Format("Specified Brush ({0}) cannot be created", brush));
            }
        }

        private static ImageSource MakeImageSource(string filename)
        {
            return new BitmapImage(new Uri(filename, UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushSolidColor
        {
            get
            {
                return new SolidColorBrush(Colors.Orange);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushLinearGradient
        {
            get
            {
                return new LinearGradientBrush(Colors.Orange, Colors.Blue, new Point(0, 0), new Point(1, 1));
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushRadialGradient
        {
            get
            {
                return new RadialGradientBrush(Colors.Orange, Colors.Blue);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushImage
        {
            get
            {
                // generate image on the fly
                int pixelWidth = 200;
                int pixelHeight = 150;
                byte[] pixels = new byte[4 * pixelWidth * pixelHeight]; // ARGB32

                for (int y = 0; y < pixelHeight; y++)
                {
                    for (int x = 0; x < pixelWidth; x++)
                    {
                        int offset = (x + (y * pixelWidth)) * 4;

                        // image is mostly solid black
                        byte color = 0x00;

                        // except for some white stripes
                        switch (x % 20)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                                color = 0xff;
                                break;
                        }
                        switch (y % 20)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                                color = 0xff;
                                break;
                        }

                        // the format is BGRA ...
                        pixels[offset + 0] = color;  // B
                        pixels[offset + 1] = color;  // G
                        pixels[offset + 2] = color;  // R
                        pixels[offset + 3] = 0xff;   // A
                    }
                }
                BitmapSource id = BitmapSource.Create(
                    pixelWidth,
                    pixelHeight,
                    96, // 96 dpi is default
                    96, // 96 dpi is default
                    PixelFormats.Bgra32,
                    null,
                    pixels,
                    pixelWidth * 4);

                ImageBrush brush = new ImageBrush(id);

                return brush;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushDrawing
        {
            get
            {
                DrawingGroup dg = new DrawingGroup();
                dg.Children.Add(new GeometryDrawing(
                        Brushes.Yellow, null,
                        new RectangleGeometry(new Rect(0, 0, 2, 1.4))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        Brushes.Red, null,
                        new RectangleGeometry(new Rect(0, 0, 2, 1))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        Brushes.Blue, null,
                        new RectangleGeometry(new Rect(0.5, 0.25, .75, .5))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        Brushes.Green, null,
                        new RectangleGeometry(new Rect(0.25, 0.5, .5, .75))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        new LinearGradientBrush(Colors.Pink, Colors.Silver, 22.5), null,
                        new EllipseGeometry(new Point(0.5, 0.5), 0.2, 0.3)
                        ));

                DrawingGroup dgText = new DrawingGroup();
                using (DrawingContext ctx = dgText.Open())
                {
                    ctx.DrawText(new FormattedText(
                        "Hello!",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        1, // TODO <federics> (Task# missing): investigate issues with setting this to something like .4
                        Brushes.Plum),
                        new Point(0.0, 0.25));
                }
                dg.Children.Add(dgText);

                DrawingBrush db = new DrawingBrush(dg);
                db.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
                db.Stretch = Stretch.Fill;
                db.TileMode = TileMode.Tile;
                return db;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushDrawingImage(string image)
        {
            Drawing d = new ImageDrawing(MakeImageSource(image), new Rect(0, 0, 1, 1));
            return new DrawingBrush(d);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushDrawingShapes
        {
            get
            {
                DrawingGroup dg = new DrawingGroup();
                dg.Children.Add(new GeometryDrawing(
                        Brushes.Yellow, null,
                        new RectangleGeometry(new Rect(0, 0, 100, 100))));
                dg.Children.Add(new GeometryDrawing(
                        Brushes.Blue, new Pen(Brushes.Black, 1.0),
                        new RectangleGeometry(new Rect(15, 15, 40, 75))));
                dg.Children.Add(new GeometryDrawing(
                        new LinearGradientBrush(Colors.Red, Colors.Silver, 22.5), null,
                        new EllipseGeometry(new Point(50, 40), 40, 20)));

                return new DrawingBrush(dg);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushDrawingShapesOpacity
        {
            get
            {
                DrawingGroup dg = new DrawingGroup();
                dg.Children.Add(new GeometryDrawing(
                        new SolidColorBrush(Color.FromArgb(100, 255, 255, 0)), null,
                        new RectangleGeometry(new Rect(0, 0, 100, 100))));
                dg.Children.Add(new GeometryDrawing(
                        new SolidColorBrush(Color.FromArgb(189, 0, 0, 255)), new Pen(Brushes.Black, 1.0),
                        new RectangleGeometry(new Rect(15, 15, 40, 75))));
                dg.Children.Add(new GeometryDrawing(
                        new LinearGradientBrush(Colors.Transparent, Colors.Silver, 22.5), null,
                        new EllipseGeometry(new Point(50, 40), 40, 20)));

                return new DrawingBrush(dg);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushDrawingText
        {
            get
            {
                DrawingGroup dg = new DrawingGroup();
                dg.Children.Add(new GeometryDrawing(
                        Brushes.LightGreen, null,
                        new RectangleGeometry(new Rect(0, 0, 3, 1.2))));

                DrawingGroup dgText = new DrawingGroup();
                using (DrawingContext ctx = dgText.Open())
                {
                    ctx.DrawText(new FormattedText(
                        "Hello!",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        1, // TODO <federics> (Task# missing): investigate issues with setting this to something like .4
                        Brushes.Purple),
                        new Point(0.0, 0.0));
                }
                dg.Children.Add(dgText);

                return new DrawingBrush(dg);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushDrawingTextOpacity
        {
            get
            {
                DrawingGroup dg = new DrawingGroup();
                dg.Children.Add(new GeometryDrawing(
                        new SolidColorBrush(Color.FromArgb(128, 0, 255, 0)), null,
                        new RectangleGeometry(new Rect(0, 0, 3, 1.2))));

                DrawingGroup dgText = new DrawingGroup();
                using (DrawingContext ctx = dgText.Open())
                {
                    ctx.DrawText(new FormattedText(
                        "Hello!",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        1, // TODO <federics> (Task# missing): investigate issues with setting this to something like .4
                        new SolidColorBrush(Color.FromArgb(199, 255, 0, 255))),
                        new Point(0.0, 0.0));
                }
                dg.Children.Add(dgText);

                return new DrawingBrush(dg);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushVisual
        {
            get
            {
                Button b = new Button();
                b.Content = "#$Hello&!";

                Grid g = new Grid();
                g.Children.Add(b);
                VisualBrush vb = new VisualBrush(g);
                return vb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushVisualOpacity
        {
            get
            {
                Button b = new Button();
                b.Content = "#$Hello&!";
                b.Opacity = 0.6;

                Grid g = new Grid();
                g.Children.Add(b);
                VisualBrush vb = new VisualBrush(g);
                return vb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushVisual3D
        {
            get
            {
                Viewport3D v = new Viewport3D();
                v.Camera = CameraFactory.PerspectiveDefault;
                ModelVisual3D mv = new ModelVisual3D();
                mv.Content = SceneFactory.SpherePlusBackdrop;
                v.Children.Add(mv);

                Grid g = new Grid();
                g.Width = 100;
                g.Height = 100;
                g.Children.Add(v);
                VisualBrush vb = new VisualBrush(g);
                return vb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushVisual3DOpacity
        {
            get
            {
                Viewport3D v = new Viewport3D();
                v.Camera = CameraFactory.PerspectiveDefault;
                ModelVisual3D mv = new ModelVisual3D();
                mv.Content = SceneFactory.SpherePlusBackdrop;
                v.Children.Add(mv);
                v.Opacity = 0.7;

                Grid g = new Grid();
                g.Width = 100;
                g.Height = 100;
                g.Children.Add(v);
                VisualBrush vb = new VisualBrush(g);
                return vb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushVisualShapes
        {
            get
            {
                System.Windows.Shapes.Rectangle r = new System.Windows.Shapes.Rectangle();
                r.Fill = Brushes.LightGreen;
                System.Windows.Shapes.Ellipse e = new System.Windows.Shapes.Ellipse();
                e.Fill = Brushes.Red;

                Grid g = new Grid();
                g.Width = 150;
                g.Height = 100;
                g.Children.Add(r);
                g.Children.Add(e);
                VisualBrush vb = new VisualBrush(g);
                return vb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushVisualShapesOpacity
        {
            get
            {
                System.Windows.Shapes.Rectangle r = new System.Windows.Shapes.Rectangle();
                r.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
                System.Windows.Shapes.Ellipse e = new System.Windows.Shapes.Ellipse();
                e.Fill = new SolidColorBrush(Color.FromArgb(189, 255, 0, 0));

                Grid g = new Grid();
                g.Width = 150;
                g.Height = 100;
                g.Children.Add(r);
                g.Children.Add(e);
                VisualBrush vb = new VisualBrush(g);
                return vb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushVisualDrawing
        {
            get
            {
                DrawingGroup dg = new DrawingGroup();
                dg.Children.Add(new GeometryDrawing(
                        Brushes.Yellow, null,
                        new RectangleGeometry(new Rect(0, 0, 2, 1.4))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        Brushes.Red, null,
                        new RectangleGeometry(new Rect(0, 0, 2, 1))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        Brushes.Blue, null,
                        new RectangleGeometry(new Rect(0.5, 0.25, .75, .5))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        Brushes.Green, null,
                        new RectangleGeometry(new Rect(0.25, 0.5, .5, .75))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        new LinearGradientBrush(Colors.Pink, Colors.Silver, 22.5), null,
                        new EllipseGeometry(new Point(0.5, 0.5), 0.2, 0.3)
                        ));

                DrawingGroup dgText = new DrawingGroup();
                using (DrawingContext ctx = dgText.Open())
                {
                    ctx.DrawText(new FormattedText(
                        "Hello!",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        1, // TODO <federics> (Task# missing): investigate issues with setting this to something like .4
                        Brushes.Plum),
                        new Point(0.0, 0.25));
                }
                dg.Children.Add(dgText);

                DrawingVisual dv = new DrawingVisual();
                DrawingContext dc = dv.RenderOpen();
                dc.DrawDrawing(dg);
                dc.Close();
                VisualBrush vb = new VisualBrush(dv);
                return vb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush MakeVideoBrush(Uri videoUri, int seekTimeInMs)
        {
            VideoDrawing vd = new VideoDrawing();
            MediaTimeline mt = new MediaTimeline(videoUri);
            MediaClock mc = mt.CreateClock();

            //changes for MediaAPI BC - shakeels
            //mc.MediaOpened += new EventHandler( MediaClock_MediaOpened );
            //vd.MediaClock = mc;
            MediaPlayer mp = new MediaPlayer();
            mp.Clock = mc;
            mp.MediaOpened += new EventHandler(MediaClock_MediaOpened);
            vd.Player = mp;
            //end change

            vd.Rect = new Rect(0, 0, 256, 256);
            DrawingBrush db = new DrawingBrush();
            db.Drawing = vd;
            return db;
        }

        static void MediaClock_MediaOpened(object sender, EventArgs e)
        {
            MediaClock mc = sender as MediaClock;
            if (mc != null)
            {
                // Video has been opened at this point
            }
        }
        // END TODO:

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushSolidColorAnimated
        {
            get
            {
                ColorAnimation ca = new ColorAnimation();
                ca.From = Colors.Orange;
                ca.To = Colors.Green;
                ca.Duration = new Duration(TimeSpan.FromMilliseconds(2000));
                ca.RepeatBehavior = RepeatBehavior.Forever;
                ca.AutoReverse = true;
                SolidColorBrush scb = new SolidColorBrush(Colors.Orange);
                scb.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                return scb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushLinearGradientAnimated
        {
            get
            {
                PointAnimation pax = new PointAnimation();
                pax.Duration = new Duration(TimeSpan.FromMilliseconds(1000));
                pax.RepeatBehavior = RepeatBehavior.Forever;
                pax.AutoReverse = true;
                pax.From = new Point(0, 0);
                pax.To = new Point(1, 0);
                PointAnimation pay = new PointAnimation();
                pay.Duration = new Duration(TimeSpan.FromMilliseconds(1950));
                pay.RepeatBehavior = RepeatBehavior.Forever;
                pay.AutoReverse = true;
                pay.From = new Point(1, 1);
                pay.To = new Point(1, 0);

                ColorAnimation cax = new ColorAnimation();
                cax.From = Colors.Blue;
                cax.To = Colors.Red;
                cax.Duration = new Duration(TimeSpan.FromMilliseconds(1200));
                cax.RepeatBehavior = RepeatBehavior.Forever;
                cax.AutoReverse = true;

                ColorAnimation cay = new ColorAnimation();
                cay.From = Colors.Orange;
                cay.To = Colors.Green;
                cay.Duration = new Duration(TimeSpan.FromMilliseconds(1750));
                cay.RepeatBehavior = RepeatBehavior.Forever;
                cay.AutoReverse = true;

                LinearGradientBrush lgb = new LinearGradientBrush();
                lgb.GradientStops = new GradientStopCollection();
                GradientStop gs1 = new GradientStop(Colors.Purple, 0.0);
                gs1.BeginAnimation(GradientStop.ColorProperty, cay);
                lgb.GradientStops.Add(gs1);
                GradientStop gs2 = new GradientStop(Colors.Pink, 1.0);
                gs2.BeginAnimation(GradientStop.ColorProperty, cax);
                lgb.GradientStops.Add(gs2);
                lgb.MappingMode = BrushMappingMode.RelativeToBoundingBox;
                lgb.BeginAnimation(LinearGradientBrush.StartPointProperty, pax);
                lgb.BeginAnimation(LinearGradientBrush.EndPointProperty, pay);

                return lgb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushRadialGradientAnimated
        {
            get
            {
                PointAnimation pax = new PointAnimation();
                pax.Duration = new Duration(TimeSpan.FromMilliseconds(1000));
                pax.RepeatBehavior = RepeatBehavior.Forever;
                pax.AutoReverse = true;
                pax.From = new Point(0.5, 0.5);
                pax.To = new Point(0.25, 0.75);
                PointAnimation pay = new PointAnimation();
                pay.Duration = new Duration(TimeSpan.FromMilliseconds(1950));
                pay.RepeatBehavior = RepeatBehavior.Forever;
                pay.AutoReverse = true;
                pay.From = new Point(0.25, 0.25);
                pay.To = new Point(0.81, 0.75);
                ColorAnimation cax = new ColorAnimation();
                cax.From = Colors.Blue;
                cax.To = Colors.Red;
                cax.Duration = new Duration(TimeSpan.FromMilliseconds(1200));
                cax.RepeatBehavior = RepeatBehavior.Forever;
                cax.AutoReverse = true;
                ColorAnimation cay = new ColorAnimation();
                cay.From = Colors.Orange;
                cay.To = Colors.Green;
                cay.Duration = new Duration(TimeSpan.FromMilliseconds(1750));
                cay.RepeatBehavior = RepeatBehavior.Forever;
                cay.AutoReverse = true;
                DoubleAnimation dax = new DoubleAnimation();
                dax.From = 0.8;
                dax.To = 1.1;
                dax.Duration = new Duration(TimeSpan.FromMilliseconds(850));
                dax.RepeatBehavior = RepeatBehavior.Forever;
                dax.AutoReverse = true;
                DoubleAnimation day = new DoubleAnimation();
                day.From = 1.0;
                day.To = 0.5;
                day.Duration = new Duration(TimeSpan.FromMilliseconds(1550));
                day.RepeatBehavior = RepeatBehavior.Forever;
                day.AutoReverse = true;

                RadialGradientBrush rgb = new RadialGradientBrush();
                rgb.BeginAnimation(RadialGradientBrush.CenterProperty, pax);
                rgb.BeginAnimation(RadialGradientBrush.GradientOriginProperty, pay);
                rgb.BeginAnimation(RadialGradientBrush.RadiusXProperty, dax);
                rgb.BeginAnimation(RadialGradientBrush.RadiusYProperty, day);
                rgb.GradientStops = new GradientStopCollection();
                GradientStop gs1 = new GradientStop(Colors.Purple, 0.0);
                gs1.BeginAnimation(GradientStop.ColorProperty, cay);
                rgb.GradientStops.Add(gs1);
                GradientStop gs2 = new GradientStop(Colors.Pink, 1.0);
                gs2.BeginAnimation(GradientStop.ColorProperty, cax);
                rgb.GradientStops.Add(gs2);
                rgb.MappingMode = BrushMappingMode.RelativeToBoundingBox;

                return rgb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushImageAnimated
        {
            get
            {
                ImageBrush brush = (ImageBrush)BrushImage;

                RectAnimation rvp = new RectAnimation();
                rvp.From = new Rect(0, 0, 2, 2);
                rvp.To = new Rect(0, 0, 1, 1);
                rvp.Duration = new Duration(TimeSpan.FromMilliseconds(1200));
                rvp.RepeatBehavior = RepeatBehavior.Forever;
                rvp.AutoReverse = true;

                brush.BeginAnimation(ImageBrush.ViewboxProperty, rvp);

                return brush;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushDrawingAnimated
        {
            get
            {
                DrawingGroup dg = new DrawingGroup();
                dg.Children.Add(new GeometryDrawing(
                        BrushLinearGradientAnimated, null,
                        new RectangleGeometry(new Rect(0, 0, 2, 1))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        Brushes.Blue, null,
                        new RectangleGeometry(new Rect(0.5, 0.25, .75, .5))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        BrushSolidColorAnimated, null,
                        new RectangleGeometry(new Rect(0.25, 0.5, .5, .75))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        new LinearGradientBrush(Colors.Pink, Colors.Silver, 22.5), null,
                        new EllipseGeometry(new Point(0.5, 0.5), 0.2, 0.3)
                        ));
                DrawingBrush db = new DrawingBrush(dg);
                db.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
                db.Stretch = Stretch.Fill;
                db.TileMode = TileMode.Tile;
                return db;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushVisualAnimated
        {
            get
            {
                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                grid.Width = 200;
                grid.Height = 150;

                System.Windows.Shapes.Rectangle red = new System.Windows.Shapes.Rectangle();
                red.Fill = Brushes.Red;
                System.Windows.Shapes.Rectangle yellow = new System.Windows.Shapes.Rectangle();
                yellow.Fill = Brushes.Yellow;
                System.Windows.Shapes.Rectangle green = new System.Windows.Shapes.Rectangle();
                green.Fill = Brushes.Green;
                System.Windows.Shapes.Rectangle blue = new System.Windows.Shapes.Rectangle();
                blue.Fill = Brushes.Blue;
                System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                ellipse.Fill = Brushes.White;

                yellow.SetValue(Grid.ColumnProperty, 1);
                blue.SetValue(Grid.ColumnProperty, 1);
                green.SetValue(Grid.RowProperty, 1);
                blue.SetValue(Grid.RowProperty, 1);
                ellipse.SetValue(Grid.ColumnSpanProperty, 2);
                ellipse.SetValue(Grid.RowSpanProperty, 2);
                ellipse.SetValue(Grid.MarginProperty, new Thickness(30));

                grid.Children.Add(red);
                grid.Children.Add(yellow);
                grid.Children.Add(green);
                grid.Children.Add(blue);
                grid.Children.Add(ellipse);

                VisualBrush vb = new VisualBrush(grid);

                RectAnimation rvp = new RectAnimation();
                rvp.From = new Rect(-0.25, 0.25, 1.5, 0.5);
                rvp.To = new Rect(0.25, -0.25, 0.5, 1.5);
                rvp.Duration = new Duration(TimeSpan.FromMilliseconds(2000));
                rvp.RepeatBehavior = RepeatBehavior.Forever;
                rvp.AutoReverse = true;
                vb.BeginAnimation(VisualBrush.ViewportProperty, rvp);

                return vb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush BrushVisualAnimatedDrawing
        {
            get
            {
                DrawingGroup dg = new DrawingGroup();
                dg.Children.Add(new GeometryDrawing(
                        Brushes.YellowGreen, null,
                        new RectangleGeometry(new Rect(0, 0, 2, 1.4))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        BrushLinearGradientAnimated, null,
                        new RectangleGeometry(new Rect(0, 0, 2, 1))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        BrushSolidColorAnimated, null,
                        new RectangleGeometry(new Rect(0.5, 0.25, .75, .5))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        BrushRadialGradientAnimated, null,
                        new RectangleGeometry(new Rect(0.25, 0.5, .5, .75))
                        ));
                dg.Children.Add(new GeometryDrawing(
                        new LinearGradientBrush(Colors.Pink, Colors.Silver, 22.5), null,
                        new EllipseGeometry(new Point(0.5, 0.5), 0.2, 0.3)
                        ));
                DrawingVisual dv = new DrawingVisual();
                DrawingContext dc = dv.RenderOpen();
                dc.DrawDrawing(dg);
                dc.Close();
                VisualBrush vb = new VisualBrush(dv);

                RectAnimation rvp = new RectAnimation();
                rvp.From = new Rect(-.2, -.2, 1.4, 1.4);
                rvp.To = new Rect(0, 0, 1, 1);
                rvp.Duration = new Duration(TimeSpan.FromMilliseconds(1200));
                rvp.RepeatBehavior = RepeatBehavior.Forever;
                rvp.AutoReverse = true;
                vb.BeginAnimation(VisualBrush.ViewportProperty, rvp);

                return vb;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomSolidColorBrush()
        {
            return GetRandomSolidColorBrush(EnvironmentWrapper.TickCount, false);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomSolidColorBrush(int seed, bool opaque)
        {
            return new SolidColorBrush(GetRandomColor(seed, opaque));
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomLinearGradientBrush()
        {
            return GetRandomLinearGradientBrush(EnvironmentWrapper.TickCount, false);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomLinearGradientBrush(int seed, bool opaque)
        {
            Random rand = new Random(seed);
            LinearGradientBrush lgb = new LinearGradientBrush(
                GetRandomColor(rand.Next(), opaque),
                GetRandomColor(rand.Next(), opaque),
                new Point(rand.NextDouble(), rand.NextDouble()),
                new Point(rand.NextDouble(), rand.NextDouble())
                );
            return lgb;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomRadialGradientBrush()
        {
            return GetRandomRadialGradientBrush(EnvironmentWrapper.TickCount, false);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomRadialGradientBrush(int seed, bool opaque)
        {
            Random rand = new Random(seed);
            RadialGradientBrush rgb = new RadialGradientBrush(
                GetRandomColor(rand.Next(), opaque),
                GetRandomColor(rand.Next(), opaque)
                );
            return rgb;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomImageBrush()
        {
            return GetRandomImageBrush(EnvironmentWrapper.TickCount, false);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomImageBrush(int seed, bool opaque)
        {
            Random rand = new Random(seed);
            int pixelWidth = (int)rand.Next(300) + 10;
            int pixelHeight = (int)rand.Next(300) + 10;
            byte[] pixels = new byte[4 * pixelWidth * pixelHeight]; // ARGB32

            // Better to have a single uniform alpha value than to jitter it
            byte alphaValue = (opaque) ? (byte)0xff : (byte)rand.Next(256); // A

            for (int y = 0; y < pixelHeight; y++)
            {
                for (int x = 0; x < pixelWidth; x++)
                {
                    int offset = (x + (y * pixelWidth)) * 4;
                    int offsetLeft = ((x - 1) + (y * pixelWidth)) * 4;
                    int offsetUp = ((x - 1) + ((y - 1) * pixelWidth)) * 4;

                    if (y > 0 && x > 0)
                    {
                        pixels[offset + 0] = NextSmoothValue(pixels, offsetLeft + 0, offsetUp + 0, rand);
                        pixels[offset + 1] = NextSmoothValue(pixels, offsetLeft + 1, offsetUp + 1, rand);
                        pixels[offset + 2] = NextSmoothValue(pixels, offsetLeft + 2, offsetUp + 2, rand);
                    }
                    else
                    {
                        pixels[offset + 0] = (byte)rand.Next(256);  // B
                        pixels[offset + 1] = (byte)rand.Next(256);  // G
                        pixels[offset + 2] = (byte)rand.Next(256);  // R
                    }
                    pixels[offset + 3] = alphaValue;
                }
            }
            BitmapSource id = BitmapSource.Create(
                pixelWidth,
                pixelHeight,
                96, // 96 dpi is default
                96, // 96 dpi is default
                PixelFormats.Bgra32,
                null,
                pixels,
                pixelWidth * 4);

            ImageBrush brush = new ImageBrush(id);
            return brush;
        }

        private static byte NextSmoothValue(byte[] pixels, int leftOffset, int upOffset, Random rand)
        {
            int leftDelta = rand.Next(5) * rand.Next(-5, 5); // * ( -1 * rand.Next() % 2 );
            int upDelta = rand.Next(5) * rand.Next(-5, 5); // * ( -1 * rand.Next() % 2 );

            int currrent = (
                    5 * ((int)pixels[leftOffset] + leftDelta) +
                    6 * ((int)pixels[upOffset] + upDelta) +
                    rand.Next(256)) / 12;

            return (byte)Math.Max(currrent, 0);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomVisualBrush()
        {
            return GetRandomVisualBrush(EnvironmentWrapper.TickCount, false);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomVisualBrush(int seed, bool opaque)
        {
            DrawingGroup dg = GetRandomDrawingGroup(seed, opaque);
            DrawingVisual dv = new DrawingVisual();
            DrawingContext dc = dv.RenderOpen();
            dc.DrawDrawing(dg);
            dc.Close();
            VisualBrush vb = new VisualBrush(dv);
            vb.Viewbox = new Rect(0, 0, 1, 1);
            vb.ViewboxUnits = BrushMappingMode.Absolute;
            return vb;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomDrawingBrush()
        {
            return GetRandomDrawingBrush(EnvironmentWrapper.TickCount, false);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Brush GetRandomDrawingBrush(int seed, bool opaque)
        {
            DrawingGroup dg = GetRandomDrawingGroup(seed, opaque);
            DrawingBrush db = new DrawingBrush(dg);
            db.TileMode = TileMode.None;
            db.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
            db.ViewboxUnits = BrushMappingMode.Absolute;
            db.Viewbox = new Rect(0, 0, 1, 1);
            return db;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DrawingGroup GetRandomDrawingGroup(int seed, bool opaque)
        {
            DrawingGroup dg = new DrawingGroup();
            Random rand = new Random(seed);
            int drawingSize = rand.Next(0, 5);

            // for solid ones, add at least one that is solid and covers the entire brush
            if (opaque)
            {
                // add solid background
                dg.Children.Add(new GeometryDrawing(
                        new SolidColorBrush(GetRandomColor(seed, opaque)), null,
                        new RectangleGeometry(new Rect(-2, -2, 4, 4))
                        ));
                seed++;
            }

            // now add any number of random ones
            for (int i = 0; i < drawingSize; i++)
            {
                switch (rand.Next() % 3)
                {
                    case 0:
                        dg.Children.Add(new GeometryDrawing(
                                new SolidColorBrush(GetRandomColor(seed + i, opaque)), null,
                                new RectangleGeometry(
                                        new Rect(rand.NextDouble(), rand.NextDouble(),
                                                rand.NextDouble(), rand.NextDouble()))
                                ));
                        break;

                    case 1:
                        dg.Children.Add(new GeometryDrawing(
                                new SolidColorBrush(GetRandomColor(seed + i, opaque)), null,
                                new EllipseGeometry(
                                        new Point(rand.NextDouble(), rand.NextDouble()),
                                                rand.NextDouble(), rand.NextDouble())
                                ));
                        break;

                    case 2:
                        dg.Children.Add(new GeometryDrawing(
                                null,
                                new Pen(new SolidColorBrush(GetRandomColor(seed + i, opaque)),
                                        rand.NextDouble() * 0.2),
                                new LineGeometry(
                                        new Point(rand.NextDouble(), rand.NextDouble()),
                                        new Point(rand.NextDouble(), rand.NextDouble()))
                                ));
                        break;

                    //
                    // Text causes too many tolerance problems.  Let's not use it for now...
                    //
                    //case 3:
                    //    DrawingGroup dgText = new DrawingGroup();
                    //    using ( DrawingContext ctx = dgText.Open() )
                    //    {
                    //        ctx.DrawText( new FormattedText(
                    //            "^T~he quick,&\n brown fox jumped \nover the lazy fox@@#$%!",
                    //            System.Globalization.CultureInfo.CurrentCulture,
                    //            FlowDirection.LeftToRight,
                    //            new Typeface( "Arial" ),
                    //            rand.NextDouble(),
                    //            GetRandomLinearGradientBrush( rand.Next(), opaque ) ),
                    //            new Point( rand.NextDouble(), rand.NextDouble() ) );
                    //    }
                    //    dg.Children.Add( dgText );
                    //    break;

                    default:
                        break;
                }
            }
            return dg;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Color GetRandomColor()
        {
            return GetRandomColor(EnvironmentWrapper.TickCount);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Color GetRandomColor(int seed)
        {
            return GetRandomColor(seed, false);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Color GetRandomColor(int seed, bool opaque)
        {
            Random rand = new Random(seed);
            Color solidColor = Color.FromArgb(
                (opaque) ? (byte)0xff : (byte)rand.Next(0, 256),
                (byte)rand.Next(0, 256),
                (byte)rand.Next(0, 256),
                (byte)rand.Next(0, 256));
            return solidColor;
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

#if !STANDALONE_BUILD
using TrustedEnvironment = Microsoft.Test.Security.Wrappers.EnvironmentSW;
#else
using TrustedEnvironment = System.Environment;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// DrawingVisual wrappers for easier object creation
    /// </summary>
    public abstract class VisualObject
    {
        /// <summary/>
        public VisualObject(Rect r)
        {
            bounds = r;
            brush = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));
            visual = new DrawingVisual();
        }

        /// <summary/>
        public VisualObject(Rect r, Color c)
        {
            bounds = r;
            brush = new SolidColorBrush(c);
            visual = new DrawingVisual();
        }

        /// <summary/>
        public VisualObject(Rect r, Brush b)
        {
            bounds = r;
            brush = b;
            visual = new DrawingVisual();
        }

        /// <summary/>
        public Color MyColor
        {
            set
            {
                brush = new SolidColorBrush(value);
                UpdateVisual();
            }
        }

        /// <summary/>
        public Brush MyBrush
        {
            set
            {
                brush = value;
                UpdateVisual();
            }
        }

        /// <summary/>
        public DrawingVisual Visual { get { return visual; } }

        /// <summary/>
        protected abstract void UpdateVisual();

        /// <summary/>
        protected Rect bounds;
        /// <summary/>
        protected Brush brush;
        /// <summary/>
        private DrawingVisual visual;
    }    
    
    /// <summary>
    /// A solid rectangle
    /// </summary>
    public class Rectangle : VisualObject
    {
        /// <summary/>
        public Rectangle(Rect r, Color c)
            : base(r, c)
        {
            UpdateVisual();
        }

        /// <summary/>
        public Rectangle(Rect r, Brush b)
            : base(r, b)
        {
            UpdateVisual();
        }

        /// <summary/>
        protected override void UpdateVisual()
        {
            using (DrawingContext ctx = Visual.RenderOpen())
            {
                ctx.DrawRectangle(brush, null, bounds);
            }
        }
    }

    /// <summary>
    /// A solid ellipse
    /// </summary>
    public class Ellipse : VisualObject
    {
        /// <summary/>
        public Ellipse(Rect r, Color c)
            : base(r, c)
        {
            UpdateVisual();
        }

        /// <summary/>
        protected override void UpdateVisual()
        {
            using (DrawingContext ctx = Visual.RenderOpen())
            {
                ctx.DrawGeometry(brush, null, new EllipseGeometry(bounds));
            }
        }
    }
  
    /// <summary>
    /// A rectangular outline
    /// pantal(Dec/06) - Renamed to CGTBorder to avoid type mismatch
    /// </summary>
    public class CGTBorder : VisualObject
    {

        /// <summary/>
        public CGTBorder(Rect r, Color c, double d)
            : base(r, c)
        {
            thickness = d;
            UpdateVisual();
        }

        /// <summary/>
        protected override void UpdateVisual()
        {
            using (DrawingContext ctx = Visual.RenderOpen())
            {
                ctx.DrawGeometry(null, new Pen(brush, thickness), new RectangleGeometry(bounds));
            }
        }

        /// <summary/>
        protected double thickness;
    }
    
    /// <summary>
    /// A line of text
    /// </summary>
    public class Text
    {
        /// <summary/>
        public Text(string str, Point p, Color c)
        {
            offset = p;
            text = new FormattedText(
                            str,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Georgia"),
                            12,
                            new SolidColorBrush(c)
                            );
            visual = new DrawingVisual();
            using (DrawingContext ctx = visual.RenderOpen())
            {
                ctx.DrawText(text, offset);
            }
        }

        /// <summary/>
        public DrawingVisual Visual { get { return visual; } }

        /// <summary/>
        private Point offset;
        /// <summary/>
        private FormattedText text;
        /// <summary/>
        private DrawingVisual visual;
    }    
   
    /// <summary>
    /// A video
    /// </summary>
    public class Video : VisualObject
    {
        /// <summary/>
        public Video(Rect r, string vf)
            : base(r)
        {
            videoFileName = new Uri(TrustedEnvironment.CurrentDirectory + @"\" + vf);
            video = new MediaTimeline(videoFileName);
            video.CurrentStateInvalidated += new System.EventHandler(OnEnd);
            video.CurrentStateInvalidated += new System.EventHandler(OnBegun);
            videoClock = (MediaClock)video.CreateClock();

            //changes for MediaAPI BC - shakeels
            mediaplayer = new MediaPlayer();
            mediaplayer.Clock = videoClock;
            //end change

            done = false;
            videoPlayingSemaphore = null;

            UpdateVisual();
        }

        /// <summary/>
        public void WaitForVideoToFinish()
        {
            if (videoPlayingSemaphore == null)
            {
                videoPlayingSemaphore = new AutoResetEvent(false);
            }
            videoPlayingSemaphore.WaitOne();
        }

        /// <summary/>
        protected override void UpdateVisual()
        {
            using (DrawingContext ctx = Visual.RenderOpen())
            {
                ctx.DrawVideo(mediaplayer, bounds);
            }
        }

        private void OnBegun(object sender, System.EventArgs e)
        {
            if (((Clock)sender).CurrentState == System.Windows.Media.Animation.ClockState.Active)
            {
                timer = new Stopwatch();
                timer.Start();
            }
        }

        private void OnEnd(object sender, System.EventArgs e)
        {

            if (((Clock)sender).CurrentState != System.Windows.Media.Animation.ClockState.Active)
            {
                timer.Stop();
                actualPlayTime = (int)timer.Elapsed.TotalMilliseconds;
                done = true;
                if (videoPlayingSemaphore != null)
                {
                    videoPlayingSemaphore.Set();
                }
            }
        }

        /// <summary/>
        public bool Done { get { return done; } }
        /// <summary/>
        public int ExpectedVideoLength { get { return videoLength; } }
        /// <summary/>
        public int ActualPlayTime { get { return actualPlayTime; } }

        /// <summary/>
        protected MediaTimeline video;
        /// <summary/>
        protected MediaClock videoClock;
        /// <summary/>
        protected MediaPlayer mediaplayer; //changes for MediaAPI BC - shakeels
        /// <summary/>
        protected Uri videoFileName;
        /// <summary/>
        private bool done;
        /// <summary/>
        protected Stopwatch timer;
        /// <summary/>
        private int actualPlayTime;
        /// <summary/>
        protected int videoLength;

        private AutoResetEvent videoPlayingSemaphore;
    }
}

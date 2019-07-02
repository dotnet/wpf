// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System;
using System.Timers;
using System.Drawing;
using System.Collections;

namespace Microsoft.Test.RenderingVerification
{
    #region CaptureCompleteEventArgs class defined for the custom event
        /// <summary>
        /// Custom EventArg for the Capture event
        /// </summary>
        public class CaptureCompleteEventArgs : EventArgs
        {
            private ArrayList _frames = null;
            /// <summary>
            /// Create an instance of hte feedbackEventArgs class
            /// </summary>
            public CaptureCompleteEventArgs(ArrayList frames)
            {
                _frames = frames;
            }
            /// <summary>
            /// The Bitmaps to be sent to listeners
            /// </summary>
            /// <value></value>
            public ArrayList Frames
            {
                get 
                {
                    return _frames;
                }
            }
        }
    #endregion CaptureCompleteEventArgs class defined for the custom event

    /// <summary>
    /// Represent the Method that will handle the VScan.CaptureComplete event of a VScan.VideoCapture
    /// </summary> 
    public delegate void CaptureCompleteEventHandler(object sender, CaptureCompleteEventArgs e);

    /// <summary>
    /// Take a serie of sreenshot at a specify frequency.
    /// </summary>
    public class VideoCapture : IDisposable
    {
        #region Properties
            #region Private Properties
                /// <summary>
                /// Timer that will call the Method to take snapshot
                /// </summary>
                private Timer _timerSnapshot = null;
                /// <summary>
                /// Interval for the shapshot timer
                /// </summary>
                private double _interval = double.NaN;
                /// <summary>
                /// Store if video capture started already
                /// </summary>
                private bool _timerIsRunning = false;
                /// <summary>
                /// The duration of the video capture (default to 10 sec)
                /// </summary>
                private TimeSpan _captureDuration = TimeSpan.FromSeconds(10);
                /// <summary>
                /// Timer that will Stop the snapshots
                /// </summary>
                private Timer _timerDuration = null;
                /// <summary>
                /// Hold the bitmaps (default starting size = 300 : 10 sec * 30 fps)
                /// </summary>
                private ArrayList _frames = new ArrayList(300);
                /// <summary>
                /// The region to get the snapshot for.
                /// </summary>
                private Rectangle _area;
                /// <summary>
                /// AutoResetEvent, use to synchronize the Start method.
                /// </summary>
                private System.Threading.AutoResetEvent _waitEvent = null;
                /// <summary>
                /// Store if an exception should be thrown if a glitch is detected.
                /// </summary>
                private bool _throwOnGlitch;
            #endregion Private Properties
            #region Public Properties (Get/Set)
                /// <summary>
                /// Get or set the frequency of snapshot (number of Frame per seconds)
                /// This cannot be changed after the video capture has began.
                /// </summary>
                public double FramesPerSecond
                {
                    get
                    {
                        return Math.Ceiling((1.0 / _interval));
                    }
                    set
                    {
                        if (value <= 0)
                        {
                            throw new ArgumentOutOfRangeException("Interval must be bigger a positive non null (number > 0)");
                        }
                        _interval = 1000.0 / value;

                        if (_timerIsRunning)
                        {
                            throw new RenderingVerificationException("Cannot modify the FramesPerSecond value when the video capture is running");
                        }
                        _timerSnapshot = new Timer(_interval);
                    }
                }

                /// <summary>
                /// Get or set the duration of the video capture
                /// This cannot be changed after the video capture has began.
                /// </summary>
                public TimeSpan CaptureDuration
                {
                    get
                    {
                        return _captureDuration;
                    }
                    set
                    {
                        if (_timerIsRunning)
                        {
                            throw new RenderingVerificationException("Cannot modify the CaptureDuration value when the video capture is running");
                        }
                        _captureDuration = value;
                        _timerDuration = new Timer(value.TotalMilliseconds);
                    }
                }

                /// <summary>
                /// Get or Set the area to get the snapshot of
                /// Can be changed during video capture -- thread-safe.
                /// </summary>
                public Rectangle Area
                {
                    get
                    {
                        lock (this)
                        {
                            return _area;
                        }
                    }
                    set
                    {
                        // most likely to be called from another thread, lock so we don't want to mess up the rect value
                        lock (this)
                        {
                            _area = value;
                        }
                    }
                }

                /// <summary>
                ///  An array of Bitmap representing the frames taken by video capture.
                /// </summary>
                public ArrayList Frames
                {
                    get
                    {
                        return _frames;
                    }
                }

                /// <summary>
                /// Get or set the ability for the video capture to throw an Exception if a glitch is detected
                /// Default is to throw.
                /// </summary>
                public bool ThrowOnGlitch
                {
                    get
                    {
                        return _throwOnGlitch;
                    }
                    set
                    {
                        _throwOnGlitch = value;
                    }
                }
            #endregion Public Properties (Get/set)
        #endregion Properties

        #region Constructors / Finalizer
            /// <summary>
            /// Create an instance of the VideoCapture Class. Used to capture frames.
            /// </summary>
            /// <param name="areaToMonitor">(System.Drawing.Rectangle) Only the specified area will be captured</param>
            public VideoCapture(Rectangle areaToMonitor)
            {
                _throwOnGlitch = true;
                _waitEvent = new System.Threading.AutoResetEvent(false);
                _area = areaToMonitor;
            }
            /// <summary>
            /// Create an instance of the VideoCapture Class. Used to capture frames.
            /// </summary>
            /// <param name="areaToMonitor">(System.Drawing.Rectangle) Only the specified area will be captured</param>
            /// <param name="framesPerSecond">(double) The snapshot frequency (number of frames per second) </param>
            public VideoCapture(Rectangle areaToMonitor, double framesPerSecond) : this(areaToMonitor)
            {
                if (framesPerSecond == 0)
                {
                    throw new ArgumentException("argument must be a positive non null integer (max = " + ushort.MaxValue + ")", "framesPerSecond" );
                }
                FramesPerSecond = framesPerSecond;
                _timerSnapshot = new Timer(_interval);
            }
            /// <summary>
            /// Create an instance of the VideoCapture Class. Used to capture frames.
            /// </summary>
            /// <param name="areaToMonitor">(System.Drawing.Rectangle) Only the specified area will be captured</param>
            /// <param name="framesPerSecond">(double) The snapshot frequency (number of frames per second) </param>
            /// <param name="captureDuration">(System.TimeSpan) The duration of the video capture</param>
            public VideoCapture(Rectangle areaToMonitor, double framesPerSecond, TimeSpan captureDuration) : this(areaToMonitor, framesPerSecond)
            {
                CaptureDuration = captureDuration;
            }
            /// <summary>
            /// Create an instance of the VideoCapture Class. Used to capture frames.
            /// </summary>
            /// <param name="areaToMonitor">(System.Drawing.Rectangle) Only the specified area will be captured</param>
            /// <param name="framesPerSecond">(double) The snapshot frequency (number of frames per second) </param>
            /// <param name="durationMilliseconds">(int) The duration of the video capture in milliseconds</param>
            public VideoCapture(Rectangle areaToMonitor, double framesPerSecond, int durationMilliseconds) : this(areaToMonitor, framesPerSecond, TimeSpan.FromMilliseconds(durationMilliseconds))
            {
            }
            /// <summary>
            /// Free all resources when the GC calls the Finalizer
            /// You should not rely on this for the cleanup though, the caller should call Dispose() when done.
            /// </summary>
            ~VideoCapture()
            {
                FreeResources();
            }
        #endregion Constructors / Finalizer

        #region Event
            /// <summary>
            /// Occur when video capture stops
            /// </summary>
            public event CaptureCompleteEventHandler CaptureComplete;
        #endregion Event

        #region Methods
            #region Public Methods
                /// <summary>
                /// Start the video capture
                /// </summary>
                /// <param name="waitForCompletion">(bool) Synchronization - Wait for the capture to be finished before returing to the calling method</param>
                public void Start(bool waitForCompletion)
                {
                    if (_timerIsRunning == true)
                    {
                        throw new RenderingVerificationException("VideoSnapshot is already running");
                    }
                    _timerDuration.AutoReset = false;
                    _timerDuration.Elapsed += new ElapsedEventHandler(StopTimer);
                    _timerSnapshot.AutoReset = true;
                    _timerSnapshot.Elapsed += new ElapsedEventHandler(TakeSnapshot);
                    _timerDuration.Start();
                    _timerSnapshot.Start();
                    if (waitForCompletion)
                    {
                        if (_waitEvent.WaitOne((int)_captureDuration.TotalMilliseconds * 2 + 1000, false) == false)
                        {
                            // Taking to long;
                            throw new ApplicationException("Area too big and/or Frequency too high, cannot capture video at this rate");
                        }
                    }
                }
                /// <summary>
                /// Stop the running Video Capture before it terminates
                /// </summary>
                /// <returns>(bool) true if the video was stopped, false otherwise (already finished or previously stopped)</returns>
                public bool Stop()
                {
                    if (_timerIsRunning == false)
                    {
                        return false;
                    }
                    _timerIsRunning = false;
                    _timerSnapshot.Stop();
                    _timerDuration.Stop();
                    return true;
                }

            #endregion Public Methods
            #region Private Methods
                /// <summary>
                /// API Called back by _timer to take the screen snapshot
                /// </summary>
                /// <param name="sender">(object) the timer that called it</param>
                /// <param name="e">(ElapsedEventArgs) Data from timer event </param>
                private void TakeSnapshot(object sender, ElapsedEventArgs e)
                {
                    // Note : Glitch might occur if:
                    // * Cannot take snapshot fast enough (Frequency too hight and/or Area too large)
                    // * User do something else simultaneously (WM_TIMER is a low priority message, if CPU does something else, timer message will be delayed)

                    if (_throwOnGlitch)
                    {
                        double interval = DateTime.Now.Subtract(e.SignalTime).TotalMilliseconds;
                        if (interval > _interval)
                        {
                            throw new RenderingVerificationException("Glitch - this configuration do not support what you specified -- Try a snaller area and/or a lower frequency");
                        }
                    }
                    _frames.Add(ImageUtility.CaptureScreen(_area));
                }
                /// <summary>
                /// API called back by _timerDuration to Stop the video capture.
                /// </summary>
                /// <param name="sender">(object) the timer that called it</param>
                /// <param name="e">(ElapsedEventArgs) Data from timer event </param>
                private void StopTimer(object sender, ElapsedEventArgs e)
                {
                    _timerIsRunning = false;
                    _timerSnapshot.Stop();
                    _waitEvent.Set();
                    // Fire event to notify calling call we are done
                    CaptureComplete(this, new CaptureCompleteEventArgs(Frames));
                }
                /// <summary>
                /// Release all resources (called by Dispose and Finalizer)
                /// </summary>
                private void FreeResources()
                {
                    if (_timerDuration != null) { _timerDuration.Stop(); _timerDuration.Close(); _timerDuration.Dispose(); _timerDuration = null; }
                    if (_timerSnapshot != null) { _timerSnapshot.Stop(); _timerSnapshot.Close(); _timerSnapshot.Dispose(); _timerSnapshot = null; }
                    if (_waitEvent != null)
                    {
                        _waitEvent.Close();
                        _waitEvent = null;
                        if (_frames != null)
                        {
                            foreach (Bitmap bitmap in _frames) { bitmap.Dispose(); }
                            _frames.Clear();
                            _frames = null;
                        }
                    }
                }

            #endregion Private Methods
        #endregion Methods

        #region IDisposable Implentation
            /// <summary>
            /// Release all resources used by this VScan.VideoCapture object
            /// </summary>
            public void Dispose()
            {
                FreeResources();
                GC.SuppressFinalize(this);
            }
        #endregion IDisposable Implentation
    }
}

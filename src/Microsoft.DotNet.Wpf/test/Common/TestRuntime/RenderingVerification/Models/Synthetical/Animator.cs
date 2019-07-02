// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Threading; 
    #endregion using

    /// <summary>
    /// The event arg class for the Animation Tick event
    /// </summary>
    public class AnimationTickEventArgs: EventArgs
    {
        /// <summary>
        /// The counter
        /// </summary>
        public long Counter = 0;

        private AnimationTickEventArgs() { } // block default instancitation
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="counter"></param>
        public AnimationTickEventArgs(long counter) 
        {
            Counter = counter;
        }
    }

    /// <summary>
    /// Summary description for Animator.
    /// </summary>
    public class Animator
    {
        #region Delegates
            /// <summary>
            /// Animation tick delegate
            /// </summary>
            public delegate void AnimationTickEventHandler(object sender, AnimationTickEventArgs e);
        #endregion Delegates

        #region Events
            /// <summary>
            /// Animation tick event
            /// </summary>
            public event AnimationTickEventHandler OnTick;
        #endregion Events
        
        #region Properties
            #region Private Properties
                private int _sleep = 100;
                private long _count = 0;
                private Thread _thread = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Set the sleeping time (in milliseconds)
                /// </summary>
                public int DelayBetweenTicks
                {
                    get { return _sleep; }
                    set
                    {
                        if (value < 0) { throw new ArgumentOutOfRangeException("Sleep", "Value must be positive"); }
                        _sleep = value;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Default Constructor
            /// </summary>
            public Animator()
            {
                _thread = new Thread(new ThreadStart(EntryPoint));
            }
            /// <summary>
            /// Cosntructor
            /// </summary>
            public Animator(int sleep) : this()
            {
                DelayBetweenTicks = sleep;
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                /// <summary>
                /// Spawns the event 
                /// </summary>
                private void Trigger()
                {
                    _count++;
                    if (OnTick != null)
                    {
                        OnTick(this, new AnimationTickEventArgs(_count));
                    }
                }
                /// <summary>
                /// The ticker itself (Thread Entry point)
                /// </summary>
                private void EntryPoint()
                {
                    try
                    {
                        while (true)
                        {
                            Thread.Sleep(_sleep);
                            Trigger();
                        }
                    }
                    catch (ThreadAbortException)
                    { 
                        // Do nothing
                    }
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Start the Animation
                /// </summary>
                public void Start()
                {
                    _thread.Start();
                }
                /// <summary>
                /// Stop the Animation
                /// </summary>
                public void Stop()
                {
                    _thread.Abort();
                }
            #endregion Public Methods
        #endregion Methods
    }
}

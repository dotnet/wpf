// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections;
using System.Windows.Threading;
using Microsoft.Test.Logging;

/******************************************************************************
 * 
 * Contains code that helps waiting for a signal sent asynchronously.
 * 
 *****************************************************************************/

namespace Microsoft.Test.Threading
{
    /// <summary>
    /// Contains code that helps waiting for a signal sent asynchronously.
    /// </summary>
    public class DispatcherSignalHelper
    {
        // private data
        private SignalTable signals = new SignalTable();
        const int defaultTimeout = 30000;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TestResult WaitForSignal()
        {
            return WaitForSignal("Default", 30000);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public TestResult WaitForSignal(string name)
        {
            return WaitForSignal(name, defaultTimeout);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public TestResult WaitForSignal(int timeout)
        {
            return WaitForSignal("Default", timeout);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public TestResult WaitForSignal(string name, int timeout)
        {
            TimeoutFrame frame = new TimeoutFrame();
            AutoSignal signal = signals[name];
            signal.Frame = frame;

            FrameTimer timeoutTimer = new FrameTimer(frame, timeout, new DispatcherOperationCallback(TimeoutFrameOperation), DispatcherPriority.Send);
            timeoutTimer.Start();

            //Pump the dispatcher
            DispatcherHelper.PushFrame(frame);

            //abort the operations that did not get processed
            signal.Frame = null;
            if (!timeoutTimer.IsCompleted)
            {
                timeoutTimer.Stop();
            }

            if (frame.TimedOut)
            {
                GlobalLog.LogStatus("A Timeout occurred.");
            }

            TestResult result = signal.Result;
            signal.Reset();
            return result;
        }


        static object TimeoutFrameOperation(object obj)
        {
            TimeoutFrame frame = obj as TimeoutFrame;
            frame.Continue = false;
            frame.TimedOut = true;
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        public void Signal(TestResult result)
        {
            Signal("Default", result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        public void Signal(string name, TestResult result)
        {
            signals[name].Signal(result);
        }

        class AutoSignal
        {
            DispatcherFrame frame;
            TestResult signalResult = TestResult.Unknown;
            bool isSet = false;

            public DispatcherFrame Frame
            {
                get { return frame; }
                set
                {
                    frame = value;
                    if (value != null)
                        frame.Continue = !isSet;
                }
            }

            public TestResult Result
            {
                get { return signalResult; }
            }

            public void Signal(TestResult result)
            {
                isSet = true;
                signalResult = result;
                if (frame != null)
                    frame.Continue = false;
            }

            public void Reset()
            {
                isSet = false;
                signalResult = TestResult.Unknown;
            }
        }

        class SignalTable
        {
            Hashtable table = new Hashtable();

            public AutoSignal this[string name]
            {
                get
                {
                    AutoSignal signal;
                    lock (table)
                    {
                        signal = table[name] as AutoSignal;
                        if (signal == null)
                            table[name] = signal = new AutoSignal();
                    }
                    return signal;
                }
            }
        }
    }


    class FrameTimer : DispatcherTimer
    {
        DispatcherFrame frame;
        DispatcherOperationCallback callback;
        bool isCompleted = false;

        public FrameTimer(DispatcherFrame frame, int milliseconds, DispatcherOperationCallback callback, DispatcherPriority priority)
            : base(priority)
        {
            this.frame = frame;
            this.callback = callback;
            Interval = TimeSpan.FromMilliseconds(milliseconds);
            Tick += new EventHandler(OnTick);
        }

        public DispatcherFrame Frame
        {
            get { return frame; }
        }

        public bool IsCompleted
        {
            get { return isCompleted; }
        }

        void OnTick(object sender, EventArgs args)
        {
            isCompleted = true;
            Stop();
            callback(frame);
        }
    }

    class TimeoutFrame : DispatcherFrame
    {
        bool timedout = false;

        public bool TimedOut
        {
            get { return timedout; }
            set { timedout = value; }
        }
    }
}

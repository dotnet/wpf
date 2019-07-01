// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Test.Logging;
using System.Threading;

namespace Microsoft.Test.Stability.Core
{
    /// <summary>
    /// Implements a Flight Data Recorder style trace of last sequence of messages leading up to crash, logged to a pair of Files.
    /// </summary>
    internal class TraceManager : TraceListener
    {
        #region Private Variables
        private readonly int threadId;
        private static readonly int maxLogSize = 40000;
        private int currentLogSize;
        private DateTime lastFlush;

        private TextWriter writer;

        private int streamIndex = 0;
        private static readonly string[] paths = { "StressTraceLogA", "StressTraceLogB" };
        #endregion

        #region Public Members

        public TraceManager(int threadId)
        {
            this.threadId = threadId;
            CreateNewWriter(threadId);
        }

        public override void Flush()
        {
            lastFlush = DateTime.Now;
            writer.Flush();
        }

        public override void Write(string message)
        {
            WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            lock (writer)
            {
                //HACK: Assumes that worker thread names contain threadId in them
                if (Thread.CurrentThread.Name == null ||
                    Thread.CurrentThread.Name.Contains(threadId.ToString()))
                {
                    foreach (string line in message.Split('\n'))
                    {
                        writer.WriteLine(String.Format("{0:G}\t{1}\t{2}", DateTime.Now, Thread.CurrentThread.Name, line));
                        ++currentLogSize;
                    }
                }
                if ((DateTime.Now - lastFlush) > TimeSpan.FromMilliseconds(250))
                {
                    Flush();
                }
            }

            if (currentLogSize > maxLogSize)
            {
                ToggleNewWriter();
            }
        }

        /// <summary>
        /// Generates the file name for the specified stream index and thread.
        /// This is used to allow the Stress Scheduler to report this back to the Test harness on the primary Appdomain
        /// </summary>
        /// <param name="streamIndex"></param>
        /// <param name="threadId"></param>
        /// <returns></returns>
        public static string GetFileName(int streamIndex, int threadId)
        {
            return paths[streamIndex] + threadId + ".log";
        }

        /// <summary>
        /// Remove listeners of type TraceToLoggerAdaptor from Trace.Listeners
        /// Infra add TraceToLoggerAdaptor, which sends Trace to Logger. Stress test doesn't need that, 
        /// thus needs a method to remove such a listener. 
        /// </summary>
        public static void RemoveTraceToLoggerAdaptor()
        {
            TraceListener traceToLoggerAdaptor = null;

            foreach (TraceListener listener in Trace.Listeners)
            {
                if (listener is TraceToLoggerAdaptor)
                {
                    traceToLoggerAdaptor = listener;
                    break;
                }
            }

            if (traceToLoggerAdaptor != null)
            {
                Trace.Listeners.Remove(traceToLoggerAdaptor);
            }
        }

        #endregion

        #region Private Implementation

        private void CreateNewWriter(int threadId)
        {
            writer = new StreamWriter(GetFileName(streamIndex, threadId), false);
            writer.WriteLine("Started log @ " + streamIndex + DateTime.Now);
            currentLogSize = 0;
            Flush();
            streamIndex = (++streamIndex) % 2;
        }

        //Switch between output Streams
        private void ToggleNewWriter()
        {
            lock (writer)
            {
                writer.WriteLine("Closed log @ " + DateTime.Now);
                Flush();
                writer.Close();
                CreateNewWriter(threadId);
            }
        }

        #endregion
    }
}

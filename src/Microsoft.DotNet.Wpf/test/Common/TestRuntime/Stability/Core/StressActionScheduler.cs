// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Test.Logging;
using Microsoft.Test.Threading;

namespace Microsoft.Test.Stability.Core
{
    internal class StressActionScheduler : IActionScheduler
    {
        #region Private Variables
        /// <summary>
        /// This defines the interval of time allocated to the WPF Dispatcher if it is idle, per test cycle.
        /// </summary>
        private static TimeSpan idleDuration;
        #endregion

        #region Public Members

        /// <summary>
        /// Run the Stress Scheduler. This will not terminate.
        /// </summary>
        public void Run(StabilityTestDefinition metadata)
        {            
            RegisterLogs(metadata.NumWorkerThreads);
            idleDuration = metadata.IdleDuration;
            
            List<Thread> threads = new List<Thread>();
            
            // Don't sends Trace to Logger.
            TraceManager.RemoveTraceToLoggerAdaptor();

            if (metadata.NumWorkerThreads != 0)
            {
                //Spin up a set of worker threads to do cool stress stuff.
                for (int i = 0; i < metadata.NumWorkerThreads; i++)
                {
                    threads.Add(InvokeThread(metadata, i));
                }

                //Let worker threads do their thing until execution time limit completes
                Thread.Sleep(metadata.ExecutionTime);
            }
            else //In case of zero worker thread, just do the job in existing thread. 
            {
                RegisterLog(TraceManager.GetFileName(0, -1));
                WorkInSharedAppdomain(metadata, -1);
            }

            //Do blocking joins on workers. If they don't exit promptly, that's an epic fail.
            TimeSpan timeoutDelay = TimeSpan.FromSeconds(900);
            
            while (threads.Count > 0)
            {
                bool joinSuccessfully = threads[threads.Count - 1].Join(timeoutDelay);

                if (!joinSuccessfully)
                {
                    Trace.WriteLine(String.Format("Threads failed to terminate, after waiting for {0}:\n", timeoutDelay.ToString())); 

                    //Debug Instruction
                    Trace.WriteLine(@"Please open the trace file under the working directory. 
                                     If the iteration in each thread just started shortly, 
                                     click 'g' to continue. Otherwise, debug the remote to 
                                     figure out what has caused the worker thread(s) to stop responding. ");

                    // break in the jit debugger. 
                    Debugger.Break();
                }
                else
                {
                    threads.RemoveAt(threads.Count - 1);
                }
            }
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Register all stress log files for consumption by the test infrastructure
        /// </summary>
        /// <param name="threads"></param>
        private void RegisterLogs(int threads)
        {
            for (int i = 0; i < threads; i++)
            {
                RegisterLog(TraceManager.GetFileName(0, i));
                RegisterLog(TraceManager.GetFileName(1, i));
            }
        }

        /// <summary>
        /// Register specific file for logging consumption
        /// </summary>
        /// <param name="filename"></param>
        private void RegisterLog(string filename)
        {            
            //HACK: Create file and immediately close(to "touch")on behalf of logger
            File.CreateText(filename).Close();            
            TestLog.Current.LogFileDeferred(filename);
        }

        /// <summary>
        /// Runs a thread configured with the specified metadata.
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="threadId"></param>
        /// <returns></returns>
        private Thread InvokeThread(StabilityTestDefinition metadata, int threadId)
        {
            Thread t = new Thread(delegate() 
                {
                    if (metadata.IsolateThreadsInAppdomains)
                    {
                        WorkInNewAppDomain(metadata, threadId);                        
                    }
                    else
                    {
                        WorkInSharedAppdomain(metadata, threadId);
                    }
                });
            t.SetApartmentState(ApartmentState.STA);

            //HACK: It is important that worker thread names contain threadID in them.
            //It is used to filter trace messages by thread and write to separate log files.
            //See TraceManager.WriteLine(). 
            t.Name = String.Format("{0} Worker: {1}", DriverState.TestName, threadId);

            t.Start();
            return t;
        }

        private void WorkInSharedAppdomain(StabilityTestDefinition metadata, int threadId)
        {
            //The Marshaled worker class should behave normally in context of it's own domain
            MarshaledWorker worker = new MarshaledWorker();
            worker.DoWork(metadata, threadId);
        }

        /// <summary>
        /// Configures a new Appdomain for executing the stress test within
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="threadId"></param>
        private void WorkInNewAppDomain(StabilityTestDefinition metadata, int threadId)
        {
            AppDomainSetup ads = new AppDomainSetup();
            ads.ApplicationBase = System.Environment.CurrentDirectory;
            ads.DisallowBindingRedirects = false;
            ads.DisallowCodeDownload = false;
            ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            string exeAssembly = Assembly.GetEntryAssembly().FullName;
            string callingDomainName = Thread.GetDomain().FriendlyName;

            AppDomain a = AppDomain.CreateDomain("Worker "+threadId.ToString(), null, ads);
            MarshaledWorker worker = (MarshaledWorker)a.CreateInstanceAndUnwrap(Assembly.GetCallingAssembly().FullName, typeof(MarshaledWorker).FullName);

            worker.DoWork(metadata, threadId);
            AppDomain.Unload(a);
        }

        private class MarshaledWorker : MarshalByRefObject
        {
            public void DoWork(StabilityTestDefinition metadata, int threadId)
            {
                int iteration = 0;
                DateTime startTime = DateTime.Now;
                DateTime endTime = startTime.Add(metadata.ExecutionTime);
                ExecutionContext executionContext = new ExecutionContext(metadata.ExecutionContext);
                executionContext.ResetState(threadId);

                //Add a stress trace listener so we can capture traces from test and Fail events
                Trace.Listeners.Add(new TraceManager(threadId));

                //make sure that exceptions are not caught by the dispatcher
                Dispatcher.CurrentDispatcher.UnhandledExceptionFilter += delegate(object sender, DispatcherUnhandledExceptionFilterEventArgs e) { e.RequestCatch = false; };
                while (DateTime.Now < endTime)
                {
                    Trace.WriteLine("[Scheduler] Starting Iteration:" + iteration);
                    Trace.WriteLine(String.Format("[Scheduler] StartTime: {0}, Current Time: {1} End Time: {2} ", startTime, DateTime.Now, endTime));

                    //Set up the iteration's randomness
                    DeterministicRandom random = new DeterministicRandom(metadata.RandomSeed, threadId, iteration);
                    DoNextActionSequence(random, executionContext, threadId);

                    Trace.WriteLine("[Scheduler] Pushing Frame onto Dispatcher");
                    DispatcherHelper.DoEvents((int)idleDuration.TotalMilliseconds,DispatcherPriority.Background);

                    Trace.WriteLine("[Scheduler] Dispatcher queue has been executed."); 

                    //Visual Space for Logging readability
                    Trace.WriteLine("");
                    Trace.WriteLine("");
                    iteration++;
                }
                Trace.WriteLine(String.Format("[Scheduler] Test is ending at {0}, after running for {1}.", endTime, metadata.ExecutionTime));

            }

            private void DoNextActionSequence(DeterministicRandom random, ExecutionContext currentExecution, int threadId)
            {
                currentExecution.GetSequence(random);

                //Synchronously Execute each action in the sequence on Execution Context. 
                while (currentExecution.DoNext(random))
                {
                    //Wait between each action in a sequence
                    Trace.WriteLine("[Scheduler] Pushing Frame onto Dispatcher");
                    DispatcherHelper.DoEvents((int)idleDuration.TotalMilliseconds, DispatcherPriority.Background);
                    Trace.WriteLine("[Scheduler] Dispatcher queue has been executed.");
                }

                //Assess execution state for metrics reset if beyond complexity threshold, or if we've gone through test for a while.
                //Iteration based mechanism to compensate for memory growth in lieu of metrics
                if (currentExecution.IsStateBeyondConstraints())
                {
                    Trace.WriteLine("[Scheduler] Resetting State");
                    currentExecution.ResetState(threadId);
                    //After resetting, this is a good time to clean house
                    GC.Collect();
                }

                //De-activate the random object instance for this cycle. Continued use will force-crash the test.
                random.Dispose();
            }
        }
        #endregion
    }
}

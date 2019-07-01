// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Threading;
using Microsoft.Test.Logging;
using Microsoft.Test.Threading;

/******************************************************************************
 * 
 * This file contains the Avalon specific Framework implementation of a testcase.
 * 
 *****************************************************************************/

namespace Microsoft.Test.TestTypes
{    
    /// <summary>
    /// Testcase base class for creating an Avalon test with steps.
    /// </summary>
    public abstract class AvalonTest : StepsTest
    {
        #region Private Data

        private DispatcherPriority stepPriority = DispatcherPriority.Send;
        private DispatcherSignalHelper dispatcherSignalHelper;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for AvalonTest.
        /// </summary>
        protected AvalonTest()
        {
            dispatcherSignalHelper = new DispatcherSignalHelper();
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Gets or Sets DispatcherPriority in which test Steps will be invoked.
        /// </summary>
        public DispatcherPriority StepPriority 
        {
            get { return stepPriority; }
            set { stepPriority = value; }
        }

        /// <summary>
        /// Waits for an asynchronous event to send a signal (default timeout 30 sec)
        /// </summary>
        /// <returns>
        /// Result.Unknown if a signal was not sent in the timeout period,
        /// otherwise, the Result set by the Signal()
        /// </returns>
        public TestResult WaitForSignal()
        {
            return dispatcherSignalHelper.WaitForSignal();
        }

        /// <summary>
        /// Waits for an asynchronous event to send a signal (default timeout 30 sec)
        /// </summary>
        /// <param name="name">Name of the signal</param>
        /// <returns>
        /// Result.Unknown if a signal was not sent in the timeout period,
        /// otherwise, the Result set by the Signal()
        /// </returns>
        public TestResult WaitForSignal(string name)
        {
            return dispatcherSignalHelper.WaitForSignal(name);
        }

        /// <summary>
        /// Waits for an asynchronous event to send a signal
        /// </summary>
        /// <param name="timeout">number of milliseconds to wait for the signal before timing-out</param>
        /// <returns>
        /// Result.Unknown if a signal was not sent in the timeout period,
        /// otherwise, the Result set by the Signal()
        /// </returns>
        public TestResult WaitForSignal(int timeout)
        {
            return dispatcherSignalHelper.WaitForSignal(timeout);
        }

        /// <summary>
        /// Waits for an asynchronous event to send a signal
        /// </summary>
        /// <param name="name">Name of the signal</param>
        /// <param name="timeout">number of milliseconds to wait for the signal before timing-out</param>
        /// <returns>
        /// Result.Unknown if a signal was not sent in the timeout period,
        /// otherwise, the Result set by the Signal()
        /// </returns>
        public TestResult WaitForSignal(string name, int timeout)
        {
            return dispatcherSignalHelper.WaitForSignal(name, timeout);
        }

        /// <summary>
        /// Signals a result back to the WaitForSignal() method
        /// </summary>
        /// <param name="result">the result to return from the TestStep</param>
        public void Signal(TestResult result)
        {
            dispatcherSignalHelper.Signal(result);
        }

        /// <summary>
        /// Signals a result back to the WaitForSignal() method
        /// </summary>
        /// <param name="name">Name of the signal</param>
        /// <param name="result">the result to return from the TestStep</param>
        public void Signal(string name, TestResult result)
        {
            dispatcherSignalHelper.Signal(name, result);
        }

        /// <summary>
        /// Waits for a specific Dispatcher Priority to occur
        /// </summary>
        /// <param name="priority">Dispatcher Priority to wait for</param>
        /// <returns>true if sucessfull otherwise false when a timeout occurs</returns>
        public void WaitForPriority(DispatcherPriority priority)
        {
            DispatcherHelper.DoEvents(0, priority);
        }

        /// <summary>
        /// Processes events from the dispatcher event queue for a specified number of milliseconds.
        /// </summary>
        /// <param name="milliseconds">number of milliseconds to process events for</param>
        public void WaitFor(int milliseconds)
        {
            DispatcherHelper.DoEvents(milliseconds);
        }

        #endregion

        #region Overridden Members

        /// <summary>
        /// Override. Waits for a certain priority before invoking the step.
        /// </summary>
        /// <param name="step">Step to invole</param>
        /// <returns>TestResult that results from the invocation of the step.</returns>
        protected override TestResult InvokeStep(TestStep step)
        {
            WaitForPriority(this.StepPriority);
            return step();
        }

        /// <summary>
        /// Called after all Run steps are completed, and before we start executing cleanup steps.
        /// Added to make /Pause work and the UI be responsive while pausing
        /// </summary>
        protected override void OnRunStageComplete()
        {
        }

        #endregion
    }
}

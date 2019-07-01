// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using Microsoft.Test.Logging;

/******************************************************************************
 * 
 * This file contains the base class of any test case that does not require a 
 * markup file.
 * It creates a Window on initialize and closes it on clean up.
 *  
 *****************************************************************************/

namespace Microsoft.Test.TestTypes
{
    /// <summary>
    /// Testcase base class for testing Avalon within a Window.
    /// </summary>
    public abstract class WindowTest : AvalonTest
    {
        #region Private Data

        private Window window;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an Testcase that will create a Window.
        /// Passing true will make the window have a Style of None and set AllowsTransparency to true.
        /// This is for layered transparent window support. These have to be set here, between creation and showing the window.
        /// </summary>        
        protected WindowTest() : this(false){}
        protected WindowTest(bool transparent)
        {
            isTransparent = transparent;
            InitializeSteps += new TestStep(InitializeTestStep);
            CleanUpSteps += new TestStep(CleanUpTestStep);
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Gets the Window used by this test case.
        /// </summary>
        public Window Window
        {
            get { return window; }
        }

        #endregion

        #region Private Members

        private TestResult InitializeTestStep()
        {
            window = WindowUtil.CreateWindow(isTransparent);
            return TestResult.Pass;
        }

        private TestResult CleanUpTestStep()
        {
            WindowUtil.CloseWindow(window);
            return TestResult.Pass;
        }

        bool isTransparent;

        #endregion
    }
}


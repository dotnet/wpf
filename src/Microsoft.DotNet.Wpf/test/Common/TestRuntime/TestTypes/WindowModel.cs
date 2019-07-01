// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;

/******************************************************************************
 * 
 * This file contains the base class of any model that does not require a 
 * markup file.
 * It creates a Window on initialize and closes it on clean up.
 * The .xtc file with the state machine should be created using MDE.
 * 
 *****************************************************************************/

namespace Microsoft.Test.TestTypes
{
    /// <summary>
    /// Base class for using model-based testing to test Avalon when test case does not require a markup file.
    /// </summary>
    public abstract class WindowModel : AvalonModel
    {
        #region Private Data

        private Window window;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for WindowModel. Model will have the same name as the .xtc file.
        /// </summary>
        /// <param name="xtcFileName">Name of xtc file.</param>
        protected WindowModel(string xtcFileName)
            : this(xtcFileName, -1)
        {
        }

        /// <summary>
        /// Constructor for WindowModel. Model will have the same name as the .xtc file.
        /// </summary>
        /// <param name="xtcFileName">Name of xtc file.</param>
        /// <param name="testCaseNumber">Number of test case to run.</param>
        protected WindowModel(string xtcFileName, int testCaseNumber)
            : this(xtcFileName, testCaseNumber, testCaseNumber)
        {
        }

        /// <summary>
        /// Constructor for WindowModel. Model will have the same name as the .xtc file.
        /// </summary>
        /// <param name="xtcFileName">Name of xtc file.</param>
        /// <param name="beginTestCaseNumber">Number of first test case to run.</param>
        /// <param name="endTestCaseNumber">Number of last test case to run.</param>
        protected WindowModel(string xtcFileName, int beginTestCaseNumber, int endTestCaseNumber)
            : this("", xtcFileName, beginTestCaseNumber, endTestCaseNumber)
        {
        }

        /// <summary>
        /// Constructor for WindowModel. 
        /// </summary>
        /// <param name="modelName">Name of the model.</param>
        /// <param name="xtcFileName">Name of xtc file.</param>
        /// <param name="beginTestCaseNumber">Number of first test case to run.</param>
        /// <param name="endTestCaseNumber">Number of last test case to run.</param>
        protected WindowModel(string modelName, string xtcFileName, int beginTestCaseNumber, int endTestCaseNumber)
            : base(modelName, xtcFileName, beginTestCaseNumber, endTestCaseNumber)
        {
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Gets the Window used by this test case.
        /// </summary>
        /// <return>Window used by this test.</return>
        public Window Window
        {
            get { return window; }
        }

        #endregion

        #region Overridden Members

        /// <summary>
        /// Creates the Window.
        /// Fires OnInitialize.
        /// </summary>
        public override bool Initialize()
        {
            window = WindowUtil.CreateWindow();
            // base.Initialize fires the OnInitialize event.
            return base.Initialize();
        }

        /// <summary>
        /// Fires OnCleanUp event.
        /// Closes the Window.
        /// </summary>
        public override bool CleanUp()
        {
            bool boolResult = base.CleanUp();
            WindowUtil.CloseWindow(window);
            return boolResult;
        }

        #endregion
    }
}


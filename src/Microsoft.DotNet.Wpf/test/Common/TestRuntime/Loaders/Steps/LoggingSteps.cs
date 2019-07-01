// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.Loaders;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Loaders.Steps 
{

    /// <summary>
    /// Creates and Closes a TestLog
    /// </summary>
    /// <remarks>
    /// You can use this step to manage the logging of test results
    /// within in correlation with multiple steps.  You should write
    /// validation Steps to set TestLog.Current.Result and add them
    /// as child steps of this Step.  If you would like to create
    /// multiple results you must put this step within a VariationContextStep.
    /// </remarks>
    
    public class TestLogStep : LoaderStep
    {
        #region Private Members

        TestLog log;

        #endregion

        #region Public Members

        /// <summary>
        /// Gets or sets the Name of the TestLog
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Gets or sets the whether the step should handle closing the test log.
        /// Used when 
        /// </summary>
        public bool CloseTestLog = true;

        #endregion

        #region Step Implementation
        /// <summary>
        /// Creates a TestLog with the Name specified by the Name property
        /// </summary>
        /// <returns>true</returns>
        protected override bool BeginStep() 
        {
            log = new TestLog(Name);
            return true;
        }

        /// <summary>
        /// Closes the TestLog
        /// </summary>
        /// <returns>true</returns>
        protected override bool EndStep() 
        {
            if ((log != null) && (CloseTestLog))
            {
                log.Close();
            }
            return true;
        }
        #endregion
    }
}

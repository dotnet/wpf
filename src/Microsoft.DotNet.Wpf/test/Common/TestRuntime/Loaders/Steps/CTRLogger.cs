// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.IO;
using Microsoft.Test.Logging;
using Microsoft.Test.Loaders;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// Helper class for calling logging APIs.  Does formatting, and can log to file
    /// </summary>
    public class CTRLogger
    {
        #region Private Members

        VariationContext vctx = null;
        TestLog log = null;
        static CTRLogger self = null;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for MSBuildStep Logger Wrapper class
        /// </summary>
        public CTRLogger()
        {
            // Harness.Current.Publish();

            log = TestLog.Current;
            if (log == null)
            {
                vctx = new VariationContext("CompilationContext");
                log = new TestLog("MSBuildStep Log");
            }

            self = this;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets instance of special logger for use by MSBuild step 
        /// </summary>
        public static CTRLogger Logger
        {
            get
            {
                return self;
            }
        }
        /// <summary>
        /// Wrapper for error logging for MSBuild compilation test step.
        /// </summary>
        public string LogError
        {
            set
            {
                log.Result = TestResult.Fail;
                log.LogEvidence(value);
            }
        }
        /// <summary>
        ///  Wrapper for using the correct logger (internally)
        /// </summary>
        public string Log
        {
            set
            {
                log.LogEvidence(value);
            }
        }
        /// <summary>
        ///  Wrapper to combine all logging calls into one point that can be updated for infrastructure changes.
        /// </summary>
        /// <param name="comment"></param>
        public void LogComment(string comment)
        {
            log.LogEvidence(comment);
        }

        /// <summary>
        /// Wrapper to abstract harness property bag values that can be updated for infrastructure changes.
        /// </summary>
        /// <param name="propertyvalue"></param>
        /// <returns></returns>
        public string this[string propertyvalue]
        {
            set
            {
                if (String.IsNullOrEmpty(propertyvalue) == false && String.IsNullOrEmpty(value) == false )
                {
                    Harness.Current[propertyvalue] = value;
                }
            }
            get
            {
				string propval = Harness.Current[propertyvalue];
				if (String.IsNullOrEmpty(propval))
				{
					return propval;
				}

				return Harness.Current[propertyvalue].ToString();
            }
        }

        /// <summary>
        ///  Sets result of test log and logs this fact.
        /// </summary>
        /// <param name="result">Pass/Fail of test.  True=Passed</param>
        public void LogResult(bool result)
        {
            if (result)
            {
                LogComment("Current test passed");
                log.Result = TestResult.Pass;                
            }
            else
            {
                LogComment("Current test failed");
                log.Result = TestResult.Fail;
            }
        }

        /// <summary>
        /// Wrapper for Logging files
        /// </summary>
        /// <param name="filename">Path of file to log</param>
        public void Save(string filename)
        {
            if (File.Exists(filename))
            {
                log.LogFile(filename);
            }
        }

        #endregion 
    }
}

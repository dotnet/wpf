// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.Test.Utilities;
using Microsoft.Test.Logging;
using Microsoft.Test.Loaders;
using Microsoft.Test.MSBuildEngine;

namespace Microsoft.Test.Loaders.Steps
{
    // Arguments Help:
    // /l:<filename>                        Log to file named "filename"
    // /e:<errorxmlfilename> :              Use Error description file specified in "ErrorXMLFileName"
    // /err:                                Ignore specified compilation errors
    // /wrn:                                Ignore specified compilation warnings
    // /d:<debugLevel>                      Enable debugging.  Can be quiet, diagnostic, or verbose
    // /p:<propertyname>=<propertyvalue>    Sets value "PropertyValue" at index "PropertyName" in the current Harness Prop bag
    // /t:<targetname>                      Target arguments (Identical to MSBuild /t: arguments)

    // Variation Generation:
    // /s: Apply scenario in the Variation template.
    // /v: Apply variation(s) in the Variation template.
    // /st:Steps file that outlines the steps that need to be taken.

    /// <summary>
    /// WPF Compilation Test LoaderStep
    /// </summary>
    public class MSBuildStep : LoaderStep
    {
        #region Public Members

        /// <summary>
        /// Argument string as originally used by LHCompiler loader
        /// </summary>
        public string Arguments = "";
                
        /// <summary>
        /// If set, puts the log into the indicated stage.
        /// Can be ignored.
        /// </summary>
        public TestStage CurrentStage = TestStage.Unknown;

        #endregion

        #region Step Implementation
        /// <summary>
        /// Runs original sources for LHCompiler loader as an AppMonitor LoaderStep, returns a value for success.
        /// </summary>
        public override bool DoStep()
        {
            GlobalLog.LogEvidence("AppMonitor Compilation Test step started");
            string[] args = Arguments.Trim().Split(' ');

            ProjectFileExecutor filegenerator = new ProjectFileExecutor();
            if (filegenerator.ParseCommandLine(args) == false)
            {
                GlobalLog.LogEvidence("Error encountered parsing command line args: \"" + Arguments + "\"");
                return false;
            }

            filegenerator.InitializeLogger();
            
            if (filegenerator.ReadSteps() == false)
            {
                TestLog.Current.Result = TestResult.Fail;
                Microsoft.Test.Utilities.Logger.LoggerInstance.Result(false);
                return false;
            }

            // Let the config file specify what log stage we're in.
            // So now Build can = Initialize, Cleanup can = Cleanup.
            // Not necessary but it makes for nicely formatted logging.
            if (CurrentStage != TestStage.Unknown)
            {
                GlobalLog.LogEvidence("AppMonitor Compilation Test - Entering " + CurrentStage.ToString() + " stage");
                TestLog.Current.Stage = CurrentStage;
            }
            
            if (filegenerator.Execute() == false)                
            {
                filegenerator.CleanGeneratedFiles();
                return false;
            }

            GlobalLog.LogEvidence("AppMonitor Compilation Test - Cleaning generated files");
            filegenerator.CleanGeneratedFiles();

            return true;
        }
        #endregion
    }
}

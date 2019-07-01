// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Stability.Core
{

    /// <summary>
    /// Entry point for Stress Testing
    /// </summary>
    public static class DriverContract
    {
        #region Public methods

        public static void Run()
        {
            // Enable autoflush of traces at the framework level
            Trace.AutoFlush = true;

            Log log = LogManager.CurrentLog;
            new TestLog(DriverState.TestName); //Hack: need a test log instance to record presence of not yet existent files
            StabilityTestDefinition testDefinition = new StabilityTestDefinition(DriverState.DriverParameters);
            IActionScheduler scheduler = (IActionScheduler)Activator.CreateInstance(testDefinition.SchedulerType);            

            // HACK: Quality Vault doesn't support get arguments from command line. Passing StressRunHours through Environment variable instead.
            // Workaround TFS 822025
            double stressRunHours = 0;

            if(double.TryParse(Environment.GetEnvironmentVariable("StressRunHours"), out stressRunHours))
            {
                 testDefinition.ExecutionTime = TimeSpan.FromHours(stressRunHours);
            }


            log.CurrentVariation.LogMessage("Starting Stress Run at: {0}.", DateTime.Now);
            log.CurrentVariation.LogMessage("Run should end at: {0}", DateTime.Now + testDefinition.ExecutionTime);
            log.CurrentVariation.LogMessage("Test name: {0}", DriverState.TestName);
            scheduler.Run(testDefinition);

            log.CurrentVariation.LogMessage("The test has ended itself.");
            log.CurrentVariation.LogResult(Result.Pass);
            log.CurrentVariation.Close();
        }
    

        #endregion
    }

}

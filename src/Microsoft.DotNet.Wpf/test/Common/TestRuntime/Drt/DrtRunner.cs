// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Drt
{
    public class DrtRunner
    {
        // DRTs only log their status via exit codes.
        // With tests such as BrowserHostedMediaApp, there are scenarios where the DRT is running on an incompatible system,
        // So pass/fail is not an acceptable response.  By returning -123456, the DRT can return "ignore" status and still log it. 
        // (Sort of like a warning)
        const int ignoreTestExitCode = -123456;

        public static void Main(string wait)
        {
            DrtRunner driver = new DrtRunner();
            try
            {
                string exeName = DriverState.DriverParameters["exe"];
                string exeArgs = DriverState.DriverParameters["args"];

                if (exeArgs == null)
                {
                    exeArgs = "-catchexceptions";
                }
                else if (!exeArgs.Contains("-catchexceptions"))
                {
                    exeArgs += " -catchexceptions";
                }

                exeArgs += $" {wait}";

                TestLog log = new TestLog(DriverState.TestName);
                long startTime = DateTime.Now.Ticks;
                driver.Run(log, exeName, exeArgs);
                long endTime = DateTime.Now.Ticks;
                log.Close();
            }
            catch (Exception e)
            {
                // Driver should always handle its exceptions becuase it is vastly more performant
                // then making a JitDebugger do it.
                GlobalLog.LogEvidence("Driver Error: " + e.ToString());
            }
        }

        public void Run(TestLog log, string exeName, string args)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(exeName, args);
                processInfo.UseShellExecute = false;
                processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processInfo.RedirectStandardOutput = true;
                processInfo.CreateNoWindow = true;

                Process p = Process.Start(processInfo);                
                LogManager.LogProcessDangerously(p.Id);
                while (!p.StandardOutput.EndOfStream)
                {
                    log.LogStatus(p.StandardOutput.ReadLine());
                }

                if (p.ExitCode == 0)
                {
                    log.Result = TestResult.Pass;
                }
                else if (p.ExitCode == ignoreTestExitCode)
                {
                    log.LogEvidence("Ignore exit code seen (" + ignoreTestExitCode + ")... setting test result to ignore.");
                    log.Result = TestResult.Ignore;
                }
                else
                {
                    log.LogEvidence("Non-zero exit code: " + p.ExitCode);
                    log.Result = TestResult.Fail;
                }
            }
            catch (Exception e)
            {
                log.LogEvidence(e.ToString());
                log.Result = TestResult.Fail;
            }
        }
    }
}

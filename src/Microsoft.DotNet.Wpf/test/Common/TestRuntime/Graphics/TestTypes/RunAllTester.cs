// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

#if !STANDALONE_BUILD
using TrustedAssembly = Microsoft.Test.Security.Wrappers.AssemblySW;
using TrustedDirectory = Microsoft.Test.Security.Wrappers.DirectorySW;
using TrustedEnvironment = Microsoft.Test.Security.Wrappers.EnvironmentSW;
using TrustedPath = Microsoft.Test.Security.Wrappers.PathSW;
using TrustedType = Microsoft.Test.Security.Wrappers.TypeSW;
#else
using TrustedAssembly = System.Reflection.Assembly;
using TrustedDirectory = System.IO.Directory;    
using TrustedPath = System.IO.Path;
using TrustedType = System.Type;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Run all the scripts in the local directory
    /// </summary>
    internal sealed class RunAllLoader : GraphicsTestLoader
    {
        /// <summary/>
        public RunAllLoader(TokenList tokens)
            : this(tokens, true)
        {
        }

        /// <summary/>
        public RunAllLoader(TokenList tokens, bool doUnitTests)
            : base(tokens)
        {
            // We don't want RunAll to crash, so make sure to catch exceptions.
            RenderingTest.catchExceptionsOnDispatcherThread = true;

            // This signals the ClientTestRunTime logging not to terminate the test too early.
            GraphicsTestLoader.IsRunAll = true;

            scripts = GetScriptsFromLocalDirectory();
            if (doUnitTests)
            {
                unitTests = GetUnitTestsFromLoader();
            }
            else
            {
                unitTests = new string[0];
            }
        }

        private string[] GetScriptsFromLocalDirectory()
        {
            string currentDirectory = EnvironmentWrapper.CurrentDirectory;
            return TrustedDirectory.GetFiles(currentDirectory, "*.xml");
        }

        private string[] GetUnitTestsFromLoader()
        {
            ArrayList tests = new ArrayList();

            //Load unit test list from feature team loader
            tests.AddRange(GetUnitTestsFromAssembly(TrustedAssembly.GetEntryAssembly()));

            if (tests.Count != 0)
            {
                string[] array = new string[tests.Count];
                tests.CopyTo(array);
                return array;
            }
            return new string[0];
        }

        private ICollection GetUnitTestsFromAssembly(TrustedAssembly testExe)
        {
            TrustedType[] testTypes = testExe.GetTypes();
            ArrayList tests = new ArrayList();
            foreach (TrustedType t in testTypes)
            {
                if (t.Namespace != null && t.IsSubclassOf(typeof(CoreGraphicsTest)) &&
                    (t.Namespace.Contains("UnitTests") || t.Namespace.Contains("Generated")))
                {
                    tests.Add(t.Name);
                }
            }
            return tests;
        }

        /// <summary/>
        public override bool RunMyTests()
        {
            int numTests = scripts.Length + unitTests.Length;
            skippedTests = new ArrayList();
            failedTests = new ArrayList();

            foreach (string script in scripts)
            {
                string localScript = TrustedPath.GetFileName(script);
                try
                {
                    RunATest(tokens, localScript, false);
                }
                catch (InvalidScriptFileException)
                {
                    Logger.Log(string.Format("  {0} is not a valid script file - Skipping...", localScript));
                    Logger.Log("");
                    numTests--;
                    skippedTests.Add(localScript);
                }
                catch (Exception ex)
                {
                    Logger.Log("  The test script threw an exception:");
                    Logger.Log(ex.ToString());

                    if (RenderingTest.window != null)
                    {
                        RenderingTest.window.Dispose();
                        RenderingTest.window = null;
                    }

                    failedTests.Add(localScript);
                }
            }
            foreach (string testName in unitTests)
            {
                try
                {
                    RunATest(tokens, testName, true);
                }
                catch (Exception ex)
                {
                    Logger.Log("  The test threw an exception:");
                    Logger.Log(ex.ToString());

                    failedTests.Add(testName);
                }
            }

            PrintRunAllResults();

            Logger.LogRunAllResults(failedTests.Count, numTests);

            return (failedTests.Count == 0);
        }

        private void RunATest(TokenList tokens, string testName, bool isUnitTest)
        {
            Logger.Log("--------------------------------------------------");
            Logger.Log("  Running " + (isUnitTest ? "unit test " : "script = ") + testName);
            Logger.Log("");

            CoreGraphicsTest.variationsFailed = 0;

            GraphicsTestLoader tester = null;
            if (isUnitTest)
            {
                tokens.SetClassName(testName);
                tester = new RunScriptLoader(tokens, null);
            }
            else
            {
                tester = new RunScriptLoader(tokens, testName);
            }

            if (!tester.RunMyTests())
            {
                failedTests.Add(testName);
            }
        }

        private void PrintRunAllResults()
        {
            Logger.Log("--------------------------------------------------");
            Logger.Log("--------------------------------------------------");
            Logger.Log("  Failed tests:");
            foreach (string script in failedTests)
            {
                Logger.Log("      " + script);
            }
            Logger.Log("");
            Logger.Log("--------------------------------------------------");
            Logger.Log("  Skipped scripts:");
            foreach (string script in skippedTests)
            {
                Logger.Log("      " + script);
            }
            Logger.Log("");
        }

        private string[] scripts;
        private string[] unitTests;
        private ArrayList skippedTests;
        private ArrayList failedTests;
    }
}

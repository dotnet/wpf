// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.Remoting;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Windows;
using System.Xml;
using Microsoft.Test.Diagnostics;
using Microsoft.Test.Loaders;
using Microsoft.Test.Logging;
using Microsoft.Test.Utilities.Reflection;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// Singleton exposes test objects - one instance per test case
    /// </summary>
    public sealed class Singleton
    {
        private Singleton()
        {
        }

        static Singleton instance = null;

        /// <summary>
        /// instance
        /// </summary>
        public static Singleton Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Singleton();
                }
                return instance;
            }
        }

        /// <summary>
        /// ILog is the interface for the log classes implementations.
        /// </summary>
        public ILog Log = LogFactory.Create();
    }

    /// <summary>
    /// ILog is the interface for the log classes implementations.
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Set Status Message
        /// </summary>
        string StatusMessage { set; }

        /// <summary>
        /// Set Pass Message
        /// </summary>
        string PassMessage { set; }

        /// <summary>
        /// Set Fail Message
        /// </summary>
        string FailMessage { set; }

        /// <summary>
        /// Clear the log
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// LogFactory class encapsulates a static method that creates the requested log, given the type and the file.
    /// Type must be one of the following: "Console", "AutomationFramework", "File" or "Multiple".
    /// </summary>
    public class LogFactory
    {
        /// <summary>
        /// Creates a multiple log that logs to aut fr, log.txt in the desktop and the console
        /// </summary>
        /// <returns>the log object</returns>
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        public static ILog Create()
        {
            // set the path of the log file in the desktop
            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            string logFile = desktopPath + @"\log.txt";

            // set the log type
            string logType = "Multiple";

            // return the log
            return (Create(logType, logFile));
        }

        /// <summary>
        /// Creates the requested log type
        /// </summary>
        /// <param name="logType">the log type</param>
        /// <param name="file">log file</param>
        /// <returns>the log object</returns>
        private static ILog Create(string logType, string file)
        {
            // construct the corresponding log
            ILog log = null;
            switch (logType)
            {
                case "Console":
                    log = new LogConsole();
                    break;

                case "AutomationFramework":
                    log = new LogAutomationFramework();
                    break;

                case "File":
                    log = new LogFile(file);
                    break;

                case "Multiple":
                    log = new LogMultiple(file);
                    break;

                default:
                    log = new LogConsole();
                    break;
            }

            // return the created log
            return (log);
        }
    }


    /// <summary>
    /// LogMultiple class wraps logging functionality to AutomationFramework, a file in the Desktop and the Console
    /// </summary>
    [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
    public class LogMultiple : MarshalByRefObject, ILog
    {
        LogAutomationFramework logAF = null;
        LogFile logFile = null;
        LogConsole logConsole = null;


        /// <summary>
        /// Constructor. It takes the automation framework assembly. The file path is the desktop.
        /// </summary>
        /// <param name="file"></param>
        public LogMultiple(string file)
        {
            // create the log console
            //AutomationFramework already has Console output
            //logConsole = new LogConsole();

            // create the log file
            logFile = new LogFile(file);

            // create the log aut framework
            try
            {
                logAF = new LogAutomationFramework();
            }
            catch
            {
                // it's ok to not create the af log. maybe the machine doesn't have the tools installed
            }
        }

        /// <summary>
        /// when StatusMessage is set, it logs a message appropiately
        /// </summary>
        /// <value>message string</value>
        public string StatusMessage
        {
            set
            {
                if (logAF != null)
                {
                    logAF.StatusMessage = value;
                }
                if (logFile != null)
                {
                    logFile.StatusMessage = value;
                }
                if (logConsole != null)
                {
                    logConsole.StatusMessage = value;
                }
            }
        }


        /// <summary>
        /// when PassMessage is set, it logs a message and logs the test as 'pass' appropiately
        /// </summary>
        /// <value>message string</value>
        public string PassMessage
        {
            set
            {
                if (logAF != null)
                {
                    logAF.PassMessage = value;
                }
                if (logFile != null)
                {
                    logFile.PassMessage = value;
                }
                if (logConsole != null)
                {
                    logConsole.PassMessage = value;
                }
            }
        }


        /// <summary>
        /// when FailMessage is set, it logs a message and logs the test as 'pass' appropiately
        /// </summary>
        /// <value>message string</value>
        public string FailMessage
        {
            set
            {
                if (logAF != null)
                {
                    logAF.FailMessage = value;
                }
                if (logFile != null)
                {
                    logFile.FailMessage = value;
                }
                if (logConsole != null)
                {
                    logConsole.FailMessage = value;
                }
            }
        }

        /// <summary>
        /// Clear clear the file
        /// </summary>
        public void Clear()
        {
            logFile.Clear();
        }
    }


    /// <summary>
    /// Log class wraps logging functionality
    /// </summary>
    public class LogFile : MarshalByRefObject, ILog
    {
        private string path = null;


        /// <summary>
        /// Log constructor. Sets log type as console
        /// </summary>
        public LogFile(string path)
        {
            this.path = path;
        }


        /// <summary>
        /// LogFile destructor
        /// </summary>
        ~LogFile()
        {
        }


        /// <summary>
        /// when StatusMessage is set, it logs a message appropiately
        /// </summary>
        /// <value>message string</value>
        public string StatusMessage
        {
            set
            {
                FileHandler.AppendLine(path, value);
            }
        }


        /// <summary>
        /// when PassMessage is set, it logs a message and logs the test as 'pass' appropiately
        /// </summary>
        /// <value>message string</value>
        public string PassMessage
        {
            set
            {
                LogTest(true, value);
            }
        }


        /// <summary>
        /// when FailMessage is set, it logs a message and logs the test as 'pass' appropiately
        /// </summary>
        /// <value>message string</value>
        public string FailMessage
        {
            set
            {
                LogTest(false, value);
            }
        }


        /// <summary>
        /// Logs test result
        /// </summary>
        /// <param name="TestPassed">true if test passed, false if failed</param>
        /// <param name="Message">message string</param>
        private void LogTest(bool TestPassed, string Message)
        {
            // set the pass/fail string
            string result = "### FAIL ###";
            if (TestPassed)
            {
                result = "### PASS ###";
            }

            // output to the file
            FileHandler.AppendLine(path, result + " - " + Message );
        }

        /// <summary>
        /// Clear clears the log file
        /// </summary>
        public void Clear()
        {
            File.Delete(path);
        }
    }


    /// <summary>
    /// Log class wraps logging functionality.
    /// To make this work with the new logging model, status messages are
    /// logged outside of any specific context - if there is no current
    /// variation, they go to the test level log. These messages are cached,
    /// however, and when a Pass or Fail message is set we open a variation,
    /// log the cached messages, then log the pass/fail message, then log
    /// the result. This makes sure that the area level reports have the log
    /// info at the variation level for analysis, but also makes sure that
    /// if a variation crashes we still get the logging up to that point, and
    /// logging is still done in real-time for user monitoring. The drawback
    /// is that during proper operation we basically double-log. The root
    /// cause of all this is that in the new logging model you are only
    /// supposed to log one variation at a time, but this legacy logging
    /// framework tries to do nesting, across process boundaries no less.
    /// </summary>
    public class LogAutomationFramework : MarshalByRefObject, ILog
    {
        private StringBuilder cachedLog;

        /// <summary>
        /// Log constructor. Sets log type as console
        /// </summary>
        public LogAutomationFramework()
        {
            cachedLog = new StringBuilder();
            if (LogManager.CurrentLog == null)
            {
                LogManager.BeginTest(DriverState.TestName);
            }
        }

        /// <summary>
        /// when StatusMessage is set, it logs a message appropiately
        /// </summary>
        /// <value>message string</value>
        [LoggingSupportFunction]
        public string StatusMessage
        {
            set
            {
                cachedLog.AppendLine(value);
                LogManager.LogMessageDangerously(value);
            }
        }

        /// <summary>
        /// when PassMessage is set, it logs a message and logs the test as 'pass' appropiately
        /// </summary>
        /// <value>message string</value>
        [LoggingSupportFunction]
        public string PassMessage
        {
            set
            {
                Log.Current.CreateVariation("PartialTrustTest.log");
                Variation.Current.LogMessage(cachedLog.ToString());
                cachedLog.Length = 0;
                Variation.Current.LogMessage(value);
                Variation.Current.LogResult(Result.Pass);
                Variation.Current.Close();
            }
        }


        /// <summary>
        /// when FailMessage is set, it logs a message and logs the test as 'fail' appropiately
        /// </summary>
        /// <value>message string</value>
        [LoggingSupportFunction]
        public string FailMessage
        {
            set
            {
                Log.Current.CreateVariation("PartialTrustTest.log");
                Variation.Current.LogMessage(cachedLog.ToString());
                cachedLog.Length = 0;
                Variation.Current.LogMessage(value);
                Variation.Current.LogResult(Result.Fail);
                Variation.Current.Close();
            }
        }

        /// <summary>
        /// Clear in this case does nothing
        /// </summary>
        public void Clear()
        {
        }
    }

    /// <summary>
    /// LogConsole wraps logging functionality to console
    /// </summary>
    public class LogConsole : MarshalByRefObject, ILog
    {
        /// <summary>
        /// LogConsole constructor
        /// </summary>
        public LogConsole()
        {
        }

        /// <summary>
        /// when StatusMessage is set, it logs a message appropiately
        /// </summary>
        /// <value>message string</value>
        public string StatusMessage
        {
            set
            {
                Console.WriteLine(value);
            }
        }

        /// <summary>
        /// when PassMessage is set, it logs a message and logs the test as 'pass' appropiately
        /// </summary>
        /// <value>message string</value>
        public string PassMessage
        {
            set
            {
                LogTest(true, value);
            }
        }

        /// <summary>
        /// when FailMessage is set, it logs a message and logs the test as 'pass' appropiately
        /// </summary>
        /// <value>message string</value>
        public string FailMessage
        {
            set
            {
                LogTest(false, value);
            }
        }

        /// <summary>
        /// Logs test result
        /// </summary>
        /// <param name="TestPassed">true if test passed, false if failed</param>
        /// <param name="Message">message string</param>
        private void LogTest(bool TestPassed, string Message)
        {
            // set the pass/fail string
            string result = "### FAIL ###";
            if (TestPassed)
            {
                result = "### PASS ###";
            }
            // output to console
            Console.WriteLine(result + " - " + Message);
        }

        /// <summary>
        /// Clear in this case does nothing
        /// </summary>
        public void Clear()
        {
        }
    }

    /// <summary>
    /// ITestCase interface defines the interface that test cases run by TestHarness must implement.
    /// </summary>
    public interface ITestCase
    {
        /// <summary>
        /// Run testcase
        /// </summary>
        bool Run(Hashtable Parameters, ILog log);
    }

    /// <summary>
    /// TestCaseRunner exposes methods to run test cases based on a configuration file like this:
    /// <code>
    /// <Tests
    ///  LogType="AutomationFramework"
    ///  Assembly="AutomationFramework, Version=1.1.0.0, Culture=neutral, PublicKeyToken=a29c01bbd4e39ac5, ProcessorArchitecture=Neutral"
    ///  File="SampleTest.log"
    ///  >
    /// 
    ///     <!--
    ///     LogType=[Console|File|AutomationFramework]
    ///     Required parameters:
    ///     for Console: no parameters
    ///     for File: File=log file path
    ///     for AutomationFramework: Assembly=AutomationFramework assembly full name
    ///     -->
    /// 
    ///     <Test Assembly="SampleTest" Class="MyTestCases.MyTest" Description="This is a test">
    ///         <Parameter name="Param1" value="Val1" />
    ///         <Parameter name="Param2" value="Val2" />
    ///     </Test>
    /// 
    /// </Tests>
    /// </code>
    /// The class that performs the test, in this case MyTestCases.MyTest, must implement the ITestCase interface.
    /// </summary>
    [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
    public class TestCaseRunner
    {
        /// <summary>
        /// Runs the test cases configured in the given file.
        /// </summary>
        /// <param name="configFile">Test configuration file. See the configuration file sample in this project.</param>
        /// <returns>true if the configuration file was found, false otherwise</returns>
        public static void RunTestCases(string configFile)
        {
            // create a reader to read the config file
            XmlTextReader reader = new XmlTextReader(configFile);
            reader.WhitespaceHandling = WhitespaceHandling.None;

            // create the object to hold the parameters for each test
            Hashtable varval = new Hashtable(StringComparer.InvariantCultureIgnoreCase);

            // read the xml configuration file
            reader.Read();
            if (reader.Name == "Tests")
            {
                // declare the log
                ILog log = null;

                // construct the corresponding log
                log = LogFactory.Create();

                // execute test cases
                while (reader.Read())
                {
                    if (reader.Name == "Test")
                    {
                        // get test attributes
                        string asm = reader.GetAttribute("Assembly");
                        string cls = reader.GetAttribute("Class");
                        string desc = reader.GetAttribute("Description");
                        string permSet = reader.GetAttribute("PermissionSet");
                        string fullTrustAsm = reader.GetAttribute("FullTrustAssemblies");

                        // log test info
                        log.StatusMessage = "Test found -> " + desc;
                        log.StatusMessage = "Assembly = " + asm;
                        log.StatusMessage = "Class = " + cls;

                        // get test parameters
                        log.StatusMessage = "Parameters:";
                        while (reader.Read())
                        {
                            if (reader.Name != "Parameter")
                            {
                                break;
                            }

                            string name = reader.GetAttribute("name");
                            string val = reader.GetAttribute("value");

                            log.StatusMessage = name + " = " + val;

                            // add test parameters
                            varval.Add(name, val);
                        }

                        // run this test
                        log.StatusMessage = "Calling test...";
                        RunTestCase(asm, cls, permSet, fullTrustAsm, varval, log);
                        log.StatusMessage = "-------------------------------------------";

                        // clear the pairs table for the next test
                        varval.Clear();
                    }
                }
            }
        }


        /// <summary>
        /// Runs one test case found in the configuration file.
        /// </summary>
        /// <param name="Asm">Assembly where the ITestCase to run lives</param>
        /// <param name="Class">The class to instantiate</param>
        /// <param name="PermSet">Path of the file containing the permission set used to sandbox the test</param>
        /// <param name="fullTrustAsm">Path of the file containing full trust assemblies public key blobs file</param>
        /// <param name="Parameters">Hashtable with pairs of the type 'param=value'</param>
        /// <param name="log">ILog object to log to</param>
        /// <returns></returns>
        public static void RunTestCase(string Asm, string Class, string PermSet, string fullTrustAsm, Hashtable Parameters, ILog log)
        {
            // create the sandbox to run the test in
            //TODO-Miguep: AppDomain stuff, uncomment
            //Sandbox sandbox = new Sandbox(PermSet, fullTrustAsm);
            //ObjectHandle handle = sandbox.CreateInstance(Asm, Class);
            //object obj = handle.Unwrap();

            // check ITestCase interface is implemented
            //ITestCase testCase = obj as ITestCase;

            //if (testCase == null)
            //{
            //    log.StatusMessage = "the type " + Class + " doesn't implement ITestCase interface";
            //    return;
            //}

            //// run the test case
            //try
            //{
            //    testCase.Run(Parameters, log);
            //    log.StatusMessage = "The test case in type '" + Class + "' in assembly '" + Asm + "' was run";
            //}
            //catch (Exception e)
            //{
            //    log.StatusMessage = "An exception was caught running the test case in type '" + Class + "' in assembly '" + Asm + "':";
            //    log.StatusMessage = e.ToString();
            //}

            throw new Exception();
        }
    }

    /// <summary>
    /// TestEnvironmentProxy is a bridge between tests and test tools
    /// </summary>
    public class TestEnvironmentProxy
    {
        /// <summary>
        /// End test case
        /// </summary>
        public static void Shutdown()
        {
            // tell appmonitor to end test
            Microsoft.Test.Loaders.ApplicationMonitor.NotifyStopMonitoring();
        }

        /// <summary>
        /// End test case
        /// </summary>
        [UIPermission(SecurityAction.Assert, Unrestricted=true)]
        public static void ShutdownApplication(Application app)
        {
            AssemblyProxy ap = new AssemblyProxy();
            //This now works because PKT and version #'s have been static for multiple releases.
#if TESTBUILD_CLR40
            ap.Load("PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=MSIL");
#endif
#if TESTBUILD_CLR20
            ap.Load("PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=MSIL");
#endif
            //This works, but only if the assembly's already loaded.
            //This turns out not to be true in our basic scenarios 1-6 (no framework loaded yet).
            //ap.Load("PresentationFramework");

            //This works in all cases.  There's just the problem of the API being deprecated.
            //Hopefully they don't remove it entirely.
            //ap.LoadWithPartialName("PresentationFramework");

            ap.Invoke("System.Windows.Application", "ShutdownImpl", app);
        }

        /// <summary>
        /// Clears security cache
        /// </summary>
        public static void ClearSecurityCache()
        {
            //TODO-Miguep: probably not needed
             //if (ApplicationSecurityManager.UserApplicationTrusts != null)
             //{
             //   ApplicationSecurityManager.UserApplicationTrusts.Clear();
             //}
        }
    }

    /// <summary>
    /// UnhandledExceptionPageResourcesHelper
    /// </summary>
    public class UnhandledExceptionPageResourcesHelper
    {
        /// <summary>
        /// ErrorPageResourceName
        /// </summary>
        public static string ErrorPageResourceName
        {
            get
            {
                string errorPageResourceName = null;

                // set appropriate resource depending on IE version
                int IEVersion = IEHelper.GetIEVersion();
                switch (IEVersion)
                {
                    case 6:
                        errorPageResourceName = "ERRORPAGE.HTML";
                        break;
                    case 7:
                    case 8:
                        errorPageResourceName = "IE7ERRORPAGE.HTML";
                        break;
                    default:// to IE7
                        errorPageResourceName = "IE7ERRORPAGE.HTML";
                        break;
                }

                // return it
                return (errorPageResourceName);
            }
        }

        /// <summary>
        /// HostResourcesFile
        /// </summary>
        public static string HostResourcesFile
        {
            get
            {
                return(SystemInformation.Current.FrameworkWpfPath + CultureInfo.CurrentUICulture.ToString() + ApplicationDeploymentHelper.PresentationHostDllMui);
            }
        }

        /// <summary>
        /// ShortNameHostResourcesFile
        /// </summary>
        internal static string ShortNameHostResourcesFile
        {
            get
            {
                return(SystemInformation.Current.FrameworkWpfPath + CultureInfo.CurrentUICulture.TwoLetterISOLanguageName + ApplicationDeploymentHelper.PresentationHostDllMui);
            }
        }

        /// <summary>
        /// FallbackHostResourcesFile
        /// </summary>
        internal static string FallbackHostResourcesFile
        {
            get
            {
                return(SystemInformation.Current.FrameworkWpfPath + "en-us" + ApplicationDeploymentHelper.PresentationHostDllMui);
            }
        }
       

        /// <summary>
        /// HostResourcesFile
        /// </summary>
        public enum Resource
        {
            /// <summary>
            /// MoreInfoButtonText
            /// </summary>
            MoreInfoButtonText,

            /// <summary>
            /// LessInfoButtonText
            /// </summary>
            LessInfoButtonText,

            /// <summary>
            /// PageTitle
            /// </summary>
            PageTitle
        }

        /// <summary>
        /// ExtractMoreInfoButtonTextFromUnhandledExceptionPage
        /// </summary>
        /// <returns></returns>
        public static string ExtractFromUnhandledExceptionPage(Resource resource)
        {
            UnmanagedResourceHelper urh = new UnmanagedResourceHelper(HostResourcesFile);
            string page = "";
            try
            {
                page = urh.GetResource(ErrorPageResourceName, UnmanagedResourceHelper.ResourceType.HTML);
            }
            catch (ArgumentException)
            {
                try
                {
                    //work around bug 171748
                    Singleton.Instance.Log.StatusMessage = "Extracting from local culture failed, retrying with short culture name";
                    urh = new UnmanagedResourceHelper(ShortNameHostResourcesFile);
                    page = urh.GetResource(ErrorPageResourceName, UnmanagedResourceHelper.ResourceType.HTML);
                }
                catch (ArgumentException)
                {
                    // try again, this time with en-us culture
                    Singleton.Instance.Log.StatusMessage = "Extracting from local culture failed, retrying with en-us";
                    urh = new UnmanagedResourceHelper(FallbackHostResourcesFile);
                    page = urh.GetResource(ErrorPageResourceName, UnmanagedResourceHelper.ResourceType.HTML);
                }
            }
            
            string pattern = null;
            switch (resource)
            {
                case Resource.MoreInfoButtonText:
                    pattern = @"var L_MoreInfo_TEXT = ""(.*)""";
                    break;
                case Resource.LessInfoButtonText:
                    pattern = @"var L_LessInfo_TEXT = ""(.*)""";
                    break;
                case Resource.PageTitle:
                    pattern = @"id=""title"">(.*)</title>";
                    break;
                default:
                    throw(new ArgumentException("resource"));
            }
            string[] matches = StringUtils.GetMatches(page, pattern);
            if (matches.Length != 1)
            {
                throw (new System.Exception("Resource not found or multiple ocurrences found"));
            }
            string r = matches[0];
            Singleton.Instance.Log.StatusMessage = "Resource extracted: '" + r + "'";
            return (r);
        }
    }

    /// <summary>
    /// IEHelper
    /// </summary>
    public class IEHelper
    {
        /// <summary>
        /// GetIEVersion
        /// </summary>
        /// <returns></returns>
        public static int GetIEVersion()
        {
            string key = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Internet Explorer\Version Vector";
            string value = "IE";
            string version = (string)RegistryHelper.Read(key, value, 0);

            // take only 1st character of the version, which can be stg like '7.0000b'
            return (int.Parse(version[0].ToString().Substring(0, 1)));
        }
    }

    /// <summary>
    /// Runner
    /// </summary>
    public class Runner
    {
        /// <summary>
        /// InvokeUntilExpectedValueDelegate
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public delegate bool InvokeUntilExpectedValueDelegate(params object[] p);

        /// <summary>
        /// WaitUntilIsEnabled
        /// </summary>
        /// <param name="d"></param>
        /// <param name="expected"></param>
        /// <param name="maxSeconds"></param>
        /// <param name="p"></param>
        /// <returns>true if expected value was got during the given timeline; false otherwise</returns>
        public static bool InvokeUntilExpectedValue(InvokeUntilExpectedValueDelegate d, bool expected, int maxSeconds, params object[] p)
        {
            // wait for the element to be enabled
            DateTime start = DateTime.Now;
            TimeSpan maxDuration = new TimeSpan(0, 0, maxSeconds);
            while (d(p) != expected)
            {
                // break if this is taking much time...
                DateTime now = DateTime.Now;
                TimeSpan duration = now - start;
                if (duration > maxDuration)
                {
                    Singleton.Instance.Log.StatusMessage = "Giving up invoking delegate";
                    return (!expected);
                }

                Thread.Sleep(1000);
                Singleton.Instance.Log.StatusMessage = "Invocation at " + now.ToLongTimeString();
            }

            Singleton.Instance.Log.StatusMessage = "Invocation succedded";
            return (expected);
        }
    }
}

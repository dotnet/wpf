// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
#if TESTBUILD_CLR20
// These includes are for the different Assembly.LoadFrom() overload used to make CLR 2.0 CAS policy work
using System.Security;
using System.Security.Policy;
#endif
using Microsoft.Test.CDFInfrastructure;

[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CDF", Scope = "type", Target = "Microsoft.Test.Discovery.CDFTestHostAdaptor")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom", Scope = "member", Target = "Microsoft.Test.Discovery.CDFTestHostAdaptor.#LoadTestCasesFromAssembly(System.String)")]

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// The TestHostAdaptor discovers test cases using attributes in the assembly passed to its constructor.
    /// For testhost details and samples see http://etcm
    /// </summary>
    public class CDFTestHostAdaptor : DiscoveryAdaptor
    {

        #region Private Fields

        private Assembly testAssembly = null;
        private TestInfo defaultTestInfo;
        const string cdfBaseNamespace = "CDF.Test.TestCases.Xaml.";

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CDFTestHostAdaptor()
        {
        }

        #endregion

        #region IDiscoveryAdaptor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="testManifestPath"></param>
        /// <param name="defaultTestInfo"></param>
        /// <returns></returns>
        public override IEnumerable<TestInfo> Discover(FileInfo testManifestPath, TestInfo defaultTestInfo)
        {
            if (testManifestPath == null)
                throw new ArgumentNullException("testManifestPath");
            if (defaultTestInfo == null)
                throw new ArgumentNullException("defaultTestInfo");

            string targetFilename = testManifestPath.FullName;

            this.defaultTestInfo = defaultTestInfo;

            List<TestInfo> testCases;
            try
            {
                testCases = LoadTestCasesFromAssembly(targetFilename);
                Trace.TraceInformation(String.Format(CultureInfo.InvariantCulture, "{0} Testcases found in assembly {1}",
                    testCases.Count, targetFilename));
            }
            catch (ReflectionTypeLoadException typeLoadException)
            {
                Trace.TraceError(String.Format(CultureInfo.InvariantCulture, "Exception loading testcases from assembly {0}: {1}",
                    typeLoadException.ToString(), targetFilename));
                Trace.TraceError("Loader exceptions are :");
                foreach (Exception ex in typeLoadException.LoaderExceptions)
                {
                    Trace.TraceError(ex.ToString());
                }

                throw;
            }
            catch (Exception exception)
            {
                Trace.TraceError(String.Format(CultureInfo.InvariantCulture, "Exception loading testcases from assembly {0}: {1}",
                    exception.ToString(), targetFilename));
                throw;
            }

            return testCases;
        }

        private List<TestInfo> LoadTestCasesFromAssembly(string assemblyPath)
        {
            List<TestInfo> testCases = new List<TestInfo>();


#if TESTBUILD_CLR20
            Evidence evidence = new Evidence();
            evidence.AddHost(new Zone(SecurityZone.MyComputer));
            this.testAssembly = Assembly.LoadFrom(assemblyPath, evidence);
#endif
#if TESTBUILD_CLR40
            this.testAssembly = Assembly.LoadFrom(assemblyPath);
#endif

            Type[] testClasses = this.testAssembly.GetTypes();

            foreach (Type type in testClasses)
            {
                if (type != null)
                {
                    foreach (MethodInfo methodInfo in type.GetMethods())
                    {
                        TestMethodAttribute[] testMethodAttributes = (TestMethodAttribute[])methodInfo.GetCustomAttributes(typeof(TestMethodAttribute), true);
                        if (testMethodAttributes.Length > 0)
                        {
                            TestInfo testInfo = this.CreateTestInfo(methodInfo, false);
                            testCases.Add(testInfo);
                        }
                        object[] customAttributes = methodInfo.GetCustomAttributes(typeof(TestCaseGeneratorAttribute), true);
                        if (customAttributes.Length > 0)
                        {
                            TestInfo testInfo = this.CreateTestInfo(methodInfo, true);
                            testCases.Add(testInfo);
                        }
                    }
                }
            }

            return testCases;
        }

        private List<Bug> GetBugs(MethodInfo methodInfo)
        {
            List<Bug> bugs = new List<Bug>();
            object[] bugAttributes = methodInfo.GetCustomAttributes(typeof(BugAttribute), true);
            if (bugAttributes.Length != 0)
            {
                List<int> bugNumbers = ((BugAttribute)bugAttributes[0]).BugNumbers;
                if (bugNumbers != null)
                {
                    foreach (int bugNumber in bugNumbers)
                    {
                        bugs.Add(new Bug() { Id = bugNumber, Source = "DevDiv" });
                    }
                }
            }
            return bugs;
        }

        private string GetSecurityLevel(MethodInfo methodInfo)
        {
            object[] securityAttributes = methodInfo.GetCustomAttributes(typeof(SecurityLevelAttribute), true);
            if (securityAttributes.Length != 0)
            {
                return ((SecurityLevelAttribute)securityAttributes[0]).SecurityLevel.ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Create a test case info - we consider a generator test as one test case.
        /// The CDFTestMethod test type handles expanding over test in a generator test
        /// </summary>
        /// <param name="methodInfo">MethodInfo of method to execute for method test or the 
        /// generator method in the case of a generator test</param>
        /// <param name="isGenerator">Is the method a generator method</param>
        public TestInfo CreateTestInfo(MethodInfo methodInfo, bool isGenerator)
        {
            // Create a clone of the default so that 
            // we use the values provided in DiscoveryInfo.xml
            TestInfo testInfo = this.defaultTestInfo.Clone();

            testInfo.Name = methodInfo.Name;
            if (isGenerator)
            {
                testInfo.Name += "_Generator";
                testInfo.DriverParameters["IsGenenerator"] = "True";
            }

            testInfo.DriverParameters["Assembly"] = "XamlCommon";
            testInfo.DriverParameters["Class"] = "Microsoft.Test.Xaml.Framework.XamlTestRunner";
            testInfo.DriverParameters["Method"] = "RunTest";
            testInfo.DriverParameters["XamlTestType"] = "CDFMethodTest";
            testInfo.DriverParameters["TestAssembly"] = this.testAssembly.GetName().Name;
            testInfo.DriverParameters["TestClass"] = methodInfo.DeclaringType.FullName;
            testInfo.DriverParameters["TestMethod"] = methodInfo.Name;
            string securityLevel = GetSecurityLevel(methodInfo);
            if (!string.IsNullOrEmpty(securityLevel))
            {
                testInfo.DriverParameters["SecurityLevel"] = securityLevel;
            }

            testInfo.Type = TestType.Functional;
            testInfo.SubArea = methodInfo.DeclaringType.FullName.Replace(cdfBaseNamespace, "");
            testInfo.Keywords = new Collection<string>();

            // if "Keywords" exist, add to test info.
            TestCaseAttribute[] testCasedAttributes = (TestCaseAttribute[])methodInfo.GetCustomAttributes(typeof(TestCaseAttribute), true);
            if (testCasedAttributes.Length > 0)
            {
                foreach (TestCaseAttribute testAttr in testCasedAttributes)
                {
                    if (!string.IsNullOrEmpty(testAttr.Keywords))
                    {
                        string[] keywords = testAttr.Keywords.Split(',');
                        foreach (string keyword in keywords)
                        {
                            testInfo.Keywords.Add(keyword);
                        }
                    }          
                }
            }

            List<Bug> bugs = GetBugs(methodInfo);
            if (bugs.Count > 0)
            {
                if (testInfo.Bugs == null)
                {
                    testInfo.Bugs = new Collection<Bug>(bugs);
                }
                else
                {
                    foreach (Bug bug in bugs)
                    {
                        testInfo.Bugs.Add(bug);
                    }
                }
            }

            return testInfo;
        }

        #endregion
    }
}

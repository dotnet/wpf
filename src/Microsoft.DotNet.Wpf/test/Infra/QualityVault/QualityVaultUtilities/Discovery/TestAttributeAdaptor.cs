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
using System.Linq;
using System.Reflection;
#if TESTBUILD_CLR20
// These includes are for the different Assembly.LoadFrom() overload used to make CLR 2.0 CAS policy work
using System.Security;
using System.Security.Policy;
#endif

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// The TestAttributeAdaptor discovers tests using a dll test manifest in which test cases
    /// are marked with a TestAttribute.
    /// </summary>
    public class TestAttributeAdaptor : DiscoveryAdaptor
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TestAttributeAdaptor()
        {
            this.AttributeType = typeof(TestAttribute);
            this.Tests = new List<TestInfo>();
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Attribute type searched for by this adaptor. 
        /// Adaptor does not find derived types of this attribute.
        /// Subclass adaptor to support finding new types.
        /// </summary>
        protected Type AttributeType { get; set; }

        /// <summary>
        /// Tests collection
        /// </summary>
        protected ICollection<TestInfo> Tests { get; private set; }

        /// <summary>
        /// Create TestInfo from a TestAttribute, Type and default TestInfo.
        /// New test info will be added to tests List.
        /// </summary>
        protected virtual ICollection<TestInfo> BuildTestInfo(TestAttribute testAttribute, Type ownerType, TestInfo defaultTestInfo)
        {
            // NOTE:
            // The old implementation restricted this method to only building a
            // TestInfo from the exact AttributeType, not any class inheriting
            // from TestAttribute. This logic was already being enforced by
            // SearchClass/SearchMethods, so this restriction here has been
            // lifted. It allows subclasses to delegate population of inherited
            // properties to the base class and focus on just new properties.

            List<TestInfo> newTests = new List<TestInfo>();

            TestInfo testInfo = defaultTestInfo.Clone();

            // General Test metadata
            ApplyGeneralMetadata(testAttribute, ownerType, testInfo);

            ApplyVersions(testAttribute, testInfo);

            ApplyBugs(testAttribute, testInfo);

            ApplyKeywords(testAttribute, testInfo);

            ApplyConfigurations(testAttribute, testInfo);

            ApplyDeployments(testAttribute, testInfo);

            ApplySupportFiles(testAttribute, testInfo);

            // Driver data. This is part of an implicit contract with sti.exe.
            if (!ApplyDriverSettings(testAttribute, ownerType, testInfo))
            {
                return newTests;
            }

            newTests.Add(testInfo);

            return newTests;
        }

        #endregion

        #region Override Members

        /// <summary>
        /// Discover a set of test cases from a binary that uses TestAttribute
        /// to mark tests.
        /// </summary>
        /// <param name="testManifestPath">Dll to examine for tests.</param>
        /// <param name="defaultTestInfo">Default TestInfo to use as a basis for discovered tests.</param>
        /// <returns>Collection of discovered tests.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        public override IEnumerable<TestInfo> Discover(FileInfo testManifestPath, TestInfo defaultTestInfo)
        {
            if (testManifestPath == null)
            {
                throw new ArgumentNullException("testManifestPath");
            }
            if (!testManifestPath.Exists)
            {
                throw new FileNotFoundException("testManifestPath", testManifestPath.FullName);
            }
            if (defaultTestInfo == null)
            {
                throw new ArgumentNullException("defaultTestInfo");
            }

            Assembly assembly = Assembly.LoadFrom(testManifestPath.FullName);

            // Parse TestDefaults attributes on the assembly, these override the default TestInfo passed to the adapter.
            TestDefaultsAttribute[] assemblyAttributes = (TestDefaultsAttribute[])assembly.GetCustomAttributes(typeof(TestDefaultsAttribute), false);
            if (assemblyAttributes.Length > 0)
            {
                ApplyTestDefaultsAttribute(ref defaultTestInfo, assemblyAttributes[0]);
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                throw new ApplicationException("Reflection on Assembly failed due to:" + e.LoaderExceptions.ToCommaSeparatedList());
            }

            foreach (Type currentType in types)
            {
                // Get TestDefaults and Test attributes on the current type (not ancestors).
                TestDefaultsAttribute[] testDefaultsAttributes = (TestDefaultsAttribute[])currentType.GetCustomAttributes(typeof(TestDefaultsAttribute), false);
                TestAttribute[] classTestAttributes = (TestAttribute[])currentType.GetCustomAttributes(this.AttributeType, false);

                // Can't have TestDefaults and Test attribute on same class.
                if (testDefaultsAttributes.Length > 0 && classTestAttributes.Length > 0)
                {
                    Trace.TraceWarning("Test and TestDefaults attribute are not allowed on the same class definition.");
                    continue;
                }

                // Save a default test info specific to this type.
                TestInfo typeDefaultTestInfo = defaultTestInfo.Clone();

                // Apply TestDefaults found on this type AND any of its ancestors.
                ApplyAncestorTestDefaults(currentType, ref typeDefaultTestInfo);

                if (testDefaultsAttributes.Length > 0)
                {
                    SearchMethods(currentType, typeDefaultTestInfo);
                }
                else // classTestAttributes.Length > 0
                {
                    // Handle per-type test case attributes. 
                    SearchClass(classTestAttributes, currentType, typeDefaultTestInfo);
                }
            }

            return Tests;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Register test cases defined on a class. Multiple tests may be defined on a class.
        /// </summary>
        private void SearchClass(TestAttribute[] classAttributes, Type ownerType, TestInfo defaultTestInfo)
        {
            if (String.IsNullOrEmpty(defaultTestInfo.Name))
            {
                defaultTestInfo.Name = ownerType.Name;
            }

            foreach (TestAttribute testAttribute in classAttributes)
            {
                // We only want to search attributes of the exact type, not subclasses also.
                if (testAttribute.GetType() == AttributeType)
                {
                    foreach (TestInfo testInfo in BuildTestInfo(testAttribute, ownerType, defaultTestInfo))
                    {
                        Tests.Add(testInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Register test cases defined on methods. Search each public method on a type for TestAttribute. 
        /// Multiple tests may be defined on a method.
        /// </summary>
        private void SearchMethods(Type ownerType, TestInfo defaultTestInfo)
        {
            MethodInfo[] methods = ownerType.GetMethods();

            if (methods.Length == 0)
            {
                Trace.TraceWarning("No TestAttributes found on methods of class: " + ownerType.FullName);
                return;
            }

            foreach (MethodInfo method in methods)
            {
                TestAttribute[] testCaseAttributes = (TestAttribute[])method.GetCustomAttributes(this.AttributeType, false);

                // Multiple TestAttributes allowed per method.
                foreach (TestAttribute testCaseAttribute in testCaseAttributes)
                {
                    // We only want to search attributes of the exact type, not subclasses also.
                    if (testCaseAttribute.GetType() == AttributeType)
                    {
                        testCaseAttribute.MethodName = method.Name;
                        foreach (TestInfo testInfo in BuildTestInfo(testCaseAttribute, ownerType, defaultTestInfo))
                        {
                            Tests.Add(testInfo);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies driver related settings to the TestInfo.
        /// </summary>
        /// <param name="testAttribute">Attribute specifying test case metadata.</param>
        /// <param name="ownerType">Type that attribute is specified on.</param>
        /// <param name="testInfo">TestInfo to modify.</param>
        /// <returns>Application success.</returns>
        private static bool ApplyDriverSettings(TestAttribute testAttribute, Type ownerType, TestInfo testInfo)
        {
            testInfo.DriverParameters["Assembly"] = ownerType.Assembly.FullName;
            testInfo.DriverParameters["Class"] = ownerType.FullName;

            if (testAttribute.MethodName != null)
            {
                testInfo.DriverParameters["Method"] = testAttribute.MethodName;
            }
            if (testAttribute.MethodParameters != null)
            {
                testInfo.DriverParameters["MethodParams"] += testAttribute.MethodParameters;
            }
            if (testAttribute.SecurityLevel != TestCaseSecurityLevel.Unset)
            {
                testInfo.DriverParameters["SecurityLevel"] = testAttribute.SecurityLevel.ToString();
            }
            if (testAttribute.MtaThread)
            {
                testInfo.DriverParameters["MtaThread"] = String.Empty;
            }
            if (testAttribute.TestParameters != null)
            {
                if (!ParseKeyValues(testAttribute.TestParameters, testInfo.DriverParameters))
                {
                    // Failure parsing key values, return an empty list.
                    Trace.TraceWarning("Test on class " + ownerType.FullName + " will not be discovered.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Applies support files property on TestAttribute to TestInfo.
        /// </summary>
        /// <param name="testAttribute">Attribute specifying test case metadata.</param>
        /// <param name="testInfo">TestInfo to modify.</param>
        private static void ApplySupportFiles(TestAttribute testAttribute, TestInfo testInfo)
        {
            if (testAttribute.SupportFiles != null)
            {
                foreach (string supportFile in testAttribute.SupportFiles.Split(','))
                {
                    TestSupportFile testSupportFile = new TestSupportFile();
                    testSupportFile.Source = supportFile.Trim();
                    testInfo.SupportFiles.Add(testSupportFile);
                }
            }
        }

        /// <summary>
        /// Applies keywords property on TestAttribute to TestInfo.
        /// </summary>
        /// <param name="testAttribute">Attribute specifying test case metadata.</param>
        /// <param name="testInfo">TestInfo to modify.</param>
        private static void ApplyKeywords(TestAttribute testAttribute, TestInfo testInfo)
        {
            if (testAttribute.Keywords != null)
            {
                if (testInfo.Keywords == null)
                {
                    testInfo.Keywords = new Collection<string>();
                }

                foreach (string keywordString in testAttribute.Keywords.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    testInfo.Keywords.Add(keywordString.Trim());
                }
            }
        }

        /// <summary>
        /// Applies bugs property on TestAttribute to TestInfo.
        /// </summary>
        /// <param name="testAttribute">Attribute specifying test case metadata.</param>
        /// <param name="testInfo">TestInfo to modify.</param>
        private static void ApplyBugs(TestAttribute testAttribute, TestInfo testInfo)
        {
            if (testAttribute.DevDivBugs != null)
            {
                // We want to override the default bugs. Either create new
                // empty collection is prototype TestInfo's bugs collection
                // was null, or clear it.
                if (testInfo.Bugs == null)
                {
                    testInfo.Bugs = new Collection<Bug>();
                }
                else
                {
                    testInfo.Bugs.Clear();
                }

                foreach (string bugId in testAttribute.DevDivBugs.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    Bug bug = new Bug();
                    bug.Id = int.Parse(bugId.Trim(), CultureInfo.InvariantCulture);
                    bug.Source = "DevDiv";
                    testInfo.Bugs.Add(bug);
                }
            }
        }

        private void ApplyConfigurations(TestAttribute testAttribute, TestInfo testInfo)
        {
            if (testAttribute.Configurations != null)
            {
                if (testInfo.Configurations == null)
                {
                    testInfo.Configurations = new Collection<String>();
                }

                foreach (string configurationString in testAttribute.Configurations.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    testInfo.Configurations.Add(configurationString.Trim());
                }
            }
        }

        private void ApplyDeployments(TestAttribute testAttribute, TestInfo testInfo)
        {
            if (testAttribute.Deployments != null)
            {
                if (testInfo.Deployments == null)
                {
                    testInfo.Deployments = new Collection<String>();
                }

                foreach (string deploymentString in testAttribute.Deployments.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    testInfo.Deployments.Add(deploymentString.Trim());
                }
            }
        }

        /// <summary>
        /// Applies versions property on TestAttribute to TestInfo.
        /// </summary>
        /// <param name="testAttribute">Attribute specifying test case metadata.</param>
        /// <param name="testInfo">TestInfo to modify.</param>
        private static void ApplyVersions(TestAttribute testAttribute, TestInfo testInfo)
        {
            if (testAttribute.Versions != null)
            {
                // We want to override the default versions.
                testInfo.Versions.Clear();

                foreach (string versionString in testAttribute.Versions.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    testInfo.Versions.Add(versionString.Trim());
                }
            }
        }

        /// <summary>
        /// Applies general metadata properties to TestInfo.
        /// </summary>
        /// <param name="testAttribute">Attribute specifying test case metadata.</param>
        /// <param name="ownerType">Type that attribute is specified on.</param>
        /// <param name="testInfo">TestInfo to modify.</param>
        private static void ApplyGeneralMetadata(TestAttribute testAttribute, Type ownerType, TestInfo testInfo)
        {
            if (testAttribute.Name != null)
            {
                // If the TestAttribute Name property is String.Empty, it means
                // to use the name of the class as the test name. (See remark
                // in TestAttribute ctor that doesn't take a name)
                if (testAttribute.Name == String.Empty)
                {
                    testInfo.Name = ownerType.FullName;
                }
                else
                {
                    testInfo.Name = testAttribute.Name;
                }
            }
            if (testAttribute.Priority != TestAttribute.UnsetInt)
            {
                testInfo.Priority = testAttribute.Priority;
            }
            if (testAttribute.Area != null)
            {
                testInfo.Area = testAttribute.Area;
            }
            if (testAttribute.SubArea != null)
            {
                testInfo.SubArea = testAttribute.SubArea;
            }
            if (testAttribute.Disabled)
            {
                testInfo.Disabled = testAttribute.Disabled;
            }
            if (testAttribute.Timeout != TestAttribute.UnsetInt)
            {
                testInfo.Timeout = TimeSpan.FromSeconds(testAttribute.Timeout);
            }
            if (testAttribute.ExecutionGroupingLevel != ExecutionGroupingLevel.InternalDefault_DontUseThisLevel)
            {
                testInfo.ExecutionGroupingLevel = testAttribute.ExecutionGroupingLevel;
            }
        }

        #endregion

        #region Static Members

        /// <summary>
        /// Get TestDefaultsAttributes on a type and all its ancestors and apply
        /// it to a TestInfo.
        /// </summary>
        private static void ApplyAncestorTestDefaults(Type currentType, ref TestInfo typeDefaultTestInfo)
        {
            // Apply TestDefaults attributes from ancestors of current type.
            TestDefaultsAttribute[] testDefaultsAttributes = (TestDefaultsAttribute[])currentType.GetCustomAttributes(typeof(TestDefaultsAttribute), true);
            foreach (TestDefaultsAttribute testDefaultOnAncestor in testDefaultsAttributes)
            {
                ApplyTestDefaultsAttribute(ref typeDefaultTestInfo, testDefaultOnAncestor);
            }
        }

        /// <summary>
        /// Assign TestDefaults attribute values to unset values in of TestInfo.
        /// Used on assembly and class TestDefaults attributes.
        /// </summary>
        private static void ApplyTestDefaultsAttribute(ref TestInfo testInfo, TestDefaultsAttribute defaultsAttribute)
        {
            if (testInfo == null)
            {
                throw new ArgumentNullException("testInfo");
            }
            if (defaultsAttribute == null)
            {
                throw new ArgumentNullException("defaultsAttribute");
            }
            if (defaultsAttribute.DefaultSubArea != null)
            {
                testInfo.SubArea = defaultsAttribute.DefaultSubArea;
            }
            if (defaultsAttribute.DefaultPriority != -1)
            {
                testInfo.Priority = defaultsAttribute.DefaultPriority;
            }
            if (defaultsAttribute.DefaultName != null)
            {
                testInfo.Name = defaultsAttribute.DefaultName;
            }
            if (defaultsAttribute.DefaultTimeout != -1)
            {
                testInfo.Timeout = TimeSpan.FromSeconds(defaultsAttribute.DefaultTimeout);
            }
            if (defaultsAttribute.SupportFiles != null)
            {
                foreach (string supportFileName in defaultsAttribute.SupportFiles.Split(','))
                {
                    TestSupportFile testSupportFile = new TestSupportFile();
                    testSupportFile.Source = supportFileName;
                    testInfo.SupportFiles.Add(testSupportFile);
                }
            }
            if (defaultsAttribute.DefaultMethodName != null)
            {
                testInfo.DriverParameters["Method"] = defaultsAttribute.DefaultMethodName;
            }
        }

        /// <summary>
        /// Parse key/key=value string into a property bag.
        /// </summary>
        //TODO - Separate out Key/Value parsing
        // I know this must be done all over the infrastructure. We should do it in one single place.
        private static bool ParseKeyValues(string keyValues, PropertyBag paramBag)
        {
            foreach (string keyValueString in keyValues.Split(new Char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // Split on '=', if any.
                string[] arg = keyValueString.Trim().Split('=');

                //If an "=" is provided there must be something after it.
                if (arg.Length == 2 && arg[1].Length == 0)
                {
                    Trace.TraceInformation("Error parsing key=value string '" + keyValueString + "' is not in the correct format: <name>[=<value>].");
                    return false;
                }

                // Put together value=value=value...
                string val = String.Empty;
                for (int j = 1; j < arg.Length; j++)
                {
                    val += arg[j] + "=";
                }
                val = val.TrimEnd('=');

                string key = arg[0];

                paramBag[key] = val;
            }

            return true;
        }

        #endregion
    }
}

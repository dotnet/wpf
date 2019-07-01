// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
#if TESTBUILD_CLR20
// These includes are for the different Assembly.LoadFrom() overload used to make CLR 2.0 CAS policy work
using System.Security;
using System.Security.Policy;
#endif

[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom", Scope = "member", Target = "Microsoft.Test.Discovery.AnnotationAdaptor.#Discover(System.IO.FileInfo,Microsoft.Test.TestInfo)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)", Scope = "member", Target = "Microsoft.Test.Discovery.AnnotationAdaptor.#Discover(System.IO.FileInfo,Microsoft.Test.TestInfo)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object,System.Object)", Scope = "member", Target = "Microsoft.Test.Discovery.AnnotationAdaptor.#Discover(System.IO.FileInfo,Microsoft.Test.TestInfo)")]

// Explanatory preface:
// The original AnnotationsAdaptor did a number of things which were against the
// spirit of the DiscoveryAdaptor design. Due to the fact that the adaptors were
// built into TestRuntime.dll, however, the fact that the adaptor and the runtime
// framework were closely coupled was not made apparent. The TestSuite class
// conflates runtime utilities, such as Assert based testing, alongside discovery
// metadata. The 'correct' solution would be to either move to a standardized
// adaptor, or to refactor the annotations framework to distinguish between
// discovery metadata and runtime framework. The later could be accomplished by
// creating discovery metadata base classes, from whom the runtime framework can
// inherit. (That is just one possiblity) Given the expense of doing so, when this
// is a team specific adaptor that is not used elsewhere, the adaptor has instead
// been made to access properties via reflection. The original lines of code are
// commented out, and the reflection-based version is on the subsequent line. This
// will hopefully make it more clear what is being done, and make it easier to
// convert to a discovery metadata base class model if resources were ever to be
// made available for such an endeavour. This approach is of course brittle, but
// the annotations framework is in maintenance mode. If annotations were to become
// funded again, then developing a sane discovery system would be a good thing to do.
// This adaptor implementation is best disclaimed as being held together by duct
// tape and baling wire, and is not a good sample of an adaptor implementation.
// While reflection is slow and brittle, anecdotal timing places discovery time of
// the annotations feature area at only a couple seconds.

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// 
    /// </summary>
    public class AnnotationAdaptor : DiscoveryAdaptor
    {
        #region Constructor

        /// <summary/>
        public AnnotationAdaptor()
        {
        }

        #endregion

        #region IDiscoveryAdaptor Members

        /// <summary />
        public override IEnumerable<TestInfo> Discover(FileInfo testManifestPath, TestInfo defaultTestInfo)
        {
            if (!(testManifestPath.Exists))
                throw new FileNotFoundException(testManifestPath.FullName);

            string targetFilename = testManifestPath.FullName;

            Assembly testAssembly = Assembly.LoadFrom(targetFilename);

            List<TestInfo> discoveredTests = new List<TestInfo>();

            Type[] allTypes = testAssembly.GetTypes();
            foreach (Type type in allTypes)
            {
                //if (type.IsSubclassOf(typeof(TestSuite)) && !type.IsAbstract)
                if (HackyIsSubclassOf(type, "TestSuite") && !type.IsAbstract)
                {
                    // Trace.TraceInformation("Discovering Suite '" + type.Name + "'.");
                    //TestSuite suite = (TestSuite)Activator.CreateInstance(type);
                    object suite = Activator.CreateInstance(type);
                    //IList<TestCase> tests = suite.TestCases;
                    IEnumerable tests = (IEnumerable)HackyGetProperty(suite, "TestCases");
                    //foreach (TestCase test in tests)
                    foreach (object test in tests)
                    {
                        //IList<TestVariation> variations = test.Variations;
                        IEnumerable variations = (IEnumerable)HackyGetProperty(test, "Variations");
                        //foreach (TestVariation variation in variations)
                        foreach (object variation in variations)
                        {
                            TestInfo testInfo = defaultTestInfo.Clone();
                            //testInfo.Disabled = test.IsDisabled;
                            testInfo.Disabled = (bool)HackyGetProperty(test, "IsDisabled");

                            //testInfo.Name = string.Format("{0}.{1}{2}", type.Name, test.Id,
                            //    (variation.Parameters == null || variation.Parameters.Length == 0) ?
                            //    string.Empty : string.Format(".{0}", string.Join(".", variation.Parameters)));
                            testInfo.Name = string.Format("{0}.{1}{2}", type.Name, HackyGetProperty(test, "Id"),
                                (HackyGetProperty(variation, "Parameters") == null || ((string[])HackyGetProperty(variation, "Parameters")).Length == 0) ?
                                string.Empty : string.Format(".{0}", string.Join(".", ((string[])HackyGetProperty(variation, "Parameters")))));


                            //testInfo.Priority = test.Priority;
                            testInfo.Priority = (int)HackyGetProperty(test, "Priority");
                            testInfo.SubArea = type.Name; // TODO: formalize Subarea concept.
                            //testInfo.Keywords.Add(test.Keywords);
                            if (testInfo.Keywords == null)
                            {
                                testInfo.Keywords = new Collection<string>();
                            }
                            testInfo.Keywords.Add((string)HackyGetProperty(test, "Keywords"));

                            // Group all tests defined in the same TestSuite in a single ExecutionGroup.
                            // TODO: Kill when we are confident in ExecutionGroups replacement.
                            //object[] compatibleAtt = type.GetCustomAttributes(typeof(ExecutionGroupCompatible), true);
                            //if (compatibleAtt != null && compatibleAtt.Length > 0)
                            //{
                            //    testInfo.ExecutionGroup = type.FullName + tests.IndexOf(test).ToString();
                            //}
                            //else
                            //{
                            //    testInfo.ExecutionGroup = type.FullName;
                            //}

                            //testInfo.DriverParameters = AnnotationsTestSettings.ToDriverParameters(testAssembly, variation);
                            testInfo.DriverParameters = ToDriverParameters(testAssembly, variation);

                            //if (!(testInfo.Disabled.HasValue && testInfo.Disabled.Value) && test.Bugs.Length <= 0)
                            //    discoveredTests.Add(testInfo);

                            if (!(testInfo.Disabled.HasValue && testInfo.Disabled.Value) && ((int[])HackyGetProperty(test, "Bugs")).Length <= 0)
                                discoveredTests.Add(testInfo);
                        }
                    }
                }
            }

            return discoveredTests;
        }

        // SUCK
        private bool HackyIsSubclassOf(Type t, string s)
        {
            return RecursiveHackyIsSubclassOf(t.BaseType, s);
        }
        private bool RecursiveHackyIsSubclassOf(Type t, string s)
        {
            if (t == null)
            {
                return false;
            }

            if (t.Name == s)
            {
                return true;
            }

            return RecursiveHackyIsSubclassOf(t.BaseType, s);
        }
        private object HackyGetProperty(object o, string s)
        {
            PropertyDescriptor pd = TypeDescriptor.GetProperties(o)[s];
            if (pd != null)
            {
                return pd.GetValue(o);
            }
            else
            {
                PropertyInfo pi = o.GetType().GetProperty(s, BindingFlags.NonPublic | BindingFlags.Instance);
                if (pi != null)
                {
                    return pi.GetValue(o, null);
                }
                else
                {
                    FieldInfo fi = o.GetType().GetField(s);
                    return fi.GetValue(o);
                }
            }
        }

        // This comes from the AnnotationsTestSettings class that used to be in the adaptor file.
        //private ContentPropertyBag ToDriverParameters(Assembly testAssembly, TestVariation variation)
        private ContentPropertyBag ToDriverParameters(Assembly testAssembly, object variation)
        {
            ContentPropertyBag driverParams = new ContentPropertyBag();
            // Use assembly partial name because we will be loading assembly from the
            // local directory, not the GAC.
            driverParams[targetAssemblyKey] = testAssembly.GetName().Name;
            //driverParams[suiteKey] = variation.TestCase.Suite.GetType().FullName;
            driverParams[suiteKey] = HackyGetProperty(HackyGetProperty(variation, "TestCase"), "Suite").GetType().FullName;
            //driverParams[testIdKey] = variation.TestCase.Id;
            driverParams[testIdKey] = (string)HackyGetProperty(HackyGetProperty(variation, "TestCase"), "Id");
            //driverParams[commandLineKey] = string.Join(" ", variation.Parameters);
            driverParams[commandLineKey] = string.Join(" ", ((string[])HackyGetProperty(variation, "Parameters")));
            return driverParams;
        }

        private const string targetAssemblyKey = "targetassembly";
        private const string suiteKey = "suite";
        private const string testIdKey = "testid";
        private const string commandLineKey = "commandline";

        #endregion
    }
}

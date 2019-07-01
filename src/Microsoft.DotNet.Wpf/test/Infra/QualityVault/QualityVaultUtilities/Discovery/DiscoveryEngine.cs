// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
#if TESTBUILD_CLR20
// This include is for the different Assembly.LoadFrom() overload used to make CLR 2.0 CAS policy work
using System.Security.Policy;
#endif
using System.Xml;
using Microsoft.Test.Filtering;

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// Exposes a static method for executing a set of DiscoveryTargets defined
    /// in a DiscoveryInfo and returning a collection of discovered test cases.
    /// </summary>
    internal static class DiscoveryEngine
    {
        #region Public Members

        /// <summary>
        /// Performs test case discovery from a DiscoveryInfo.
        /// </summary>
        /// <param name="discoveryInfoFilePath">Fully qualified path to the DiscoveryInfo file.</param>
        /// <param name="filteringSettings">What filtering settings are being specified, used for policy decisions. Area is used to decide whether to run a target or not, for example.</param>
        /// <returns>Collection of TestInfos.</returns>
        public static IEnumerable<TestInfo> Discover(FileInfo discoveryInfoFilePath, FilteringSettings filteringSettings)
        {
            CleanGacState();

            return RunDiscoveryViaAppDomain(discoveryInfoFilePath, filteringSettings);
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Policyful method - Clean up the Gac on behalf of Attribute based Adaptors.
        /// </summary>
        private static void CleanGacState()
        {
            // TODO: Remove this, Core does not have a GAC
            //Silently Remove TestContracts from GAC to prevent issues in event of periodic breaking changes.
            //Microsoft.Test.Execution.StateManagement.GacUtilities.AssemblyCache.UninstallAssemblySilently(@"Infra\TestContracts.dll");

            //We may want to scale this kind of logic into the attribute Adaptors which are victims of stale dependent dlls being in GAC
        }

        /// <summary>
        /// Sets up an AppDomain and runs a discovery callback inside of it.
        /// </summary>
        private static IEnumerable<TestInfo> RunDiscoveryViaAppDomain(FileInfo discoveryInfoFilePath, FilteringSettings filteringSettings)
        {
            IEnumerable<TestInfo> discoveredTests = DiscoverInternal(discoveryInfoFilePath, filteringSettings);

            return discoveredTests;
        }

        /// <summary>
        /// Internal discovery implementation.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        private static IEnumerable<TestInfo> DiscoverInternal(FileInfo discoveryInfoFilePath, FilteringSettings filteringSettings)
        {
            ResolveEventHandler reh = delegate(object sender, ResolveEventArgs args)
            {
                return OnReflectionAssemblyResolve(args.Name, discoveryInfoFilePath);
            };

            AppDomain.CurrentDomain.AssemblyResolve += reh;
            DiscoveryInfo discoveryInfo;

            using (XmlTextReader reader = new XmlTextReader(discoveryInfoFilePath.FullName))
            {
                discoveryInfo = (DiscoveryInfo)ObjectSerializer.Deserialize(reader, typeof(DiscoveryInfo), null);
            }

            // DiscoveryInfo points to data that drives a VersionMatcher.
            // We need a matcher both in the discovery AppDomain and in the
            // main AppDomain.  Create one here, and record the path so that
            // the main AppDomain can create its own.
            //filteringSettings.EnsureVersions();
            //filteringSettings.VersionMatcher = VersionMatcher.Merge(null, discoveryInfoFilePath.DirectoryName, discoveryInfo.VersionOrder);

            Assembly adaptorsAssembly = Assembly.LoadFrom(Path.Combine(discoveryInfoFilePath.DirectoryName, discoveryInfo.AdaptorsAssembly));

            IEnumerable<Type> discoveryAdaptorTypes = GetDiscoveryAdaptors(adaptorsAssembly, typeof(DiscoveryAdaptor));

            List<TestInfo> discoveredTests = new List<TestInfo>();

            string previousCurrentDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = discoveryInfoFilePath.DirectoryName;
            foreach (DiscoveryTarget discoveryTarget in discoveryInfo.Targets)
            {
                if (!ShouldRunTarget(discoveryTarget, filteringSettings.Area))
                {
                    continue;
                }

                // Get the first discovery adaptor whose name matches the name specified by the DiscoveryTarget.
                Type discoveryAdaptorType = discoveryAdaptorTypes.First(candidateAdaptorType => candidateAdaptorType.Name == discoveryTarget.Adaptor);
                if (discoveryAdaptorType == null)
                {
                    throw new TypeLoadException(discoveryTarget.Adaptor + " could not be found.");
                }

                IEnumerable<TestInfo> testInfosFromTarget = Discover(discoveryTarget, discoveryAdaptorType, discoveryInfoFilePath.Directory, discoveryInfo.DefaultTestInfo, filteringSettings);

                VerifyAreaIndex(discoveryTarget, testInfosFromTarget);

                discoveredTests.AddRange(testInfosFromTarget);
            }
            Environment.CurrentDirectory = previousCurrentDirectory;
            AppDomain.CurrentDomain.AssemblyResolve -= reh;
            return discoveredTests;
        }



        /// <summary>
        /// Performs test case discovery from a DiscoveryTarget.
        /// </summary>
        /// <param name="discoveryTarget">Contains adaptor, manifest path, and default TestInfo data.</param>
        /// <param name="discoveryAdaptorType">Type DiscoveryTarget.Adaptor maps to.</param>
        /// <param name="discoveryRootDirectoryPath">Directory DiscoveryTarget.Path is relative to.</param>
        /// <param name="defaultTestInfo">Default to info to use as prototype.</param>
        /// <param name="filteringSettings">Filtering settings.</param>
        /// <returns>Collection of TestInfos.</returns>
        private static IEnumerable<TestInfo> Discover(DiscoveryTarget discoveryTarget, Type discoveryAdaptorType, DirectoryInfo discoveryRootDirectoryPath, TestInfo defaultTestInfo, FilteringSettings filteringSettings)
        {
            // Start with the default test info passed into the adaptor, and clone it.
            // then merge in local values from the discovery target's default test info.
            TestInfo compositedTestInfo = defaultTestInfo.Clone();
            compositedTestInfo.Merge(discoveryTarget.DefaultTestInfo);

            List<TestInfo> discoveredTests = new List<TestInfo>();

            DiscoveryAdaptor discoveryAdaptor = (DiscoveryAdaptor)Activator.CreateInstance(discoveryAdaptorType);

            IEnumerable<FileInfo> discoveryTargetFileNames = GetFiles(discoveryRootDirectoryPath, discoveryTarget.Path, compositedTestInfo, filteringSettings);

            foreach (FileInfo discoveryTargetFileName in discoveryTargetFileNames)
            {
                IEnumerable<TestInfo> discoveryTargetTests;
                try
                {
                    discoveryTargetTests = discoveryAdaptor.Discover(discoveryTargetFileName, compositedTestInfo);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Exception when running " + discoveryAdaptor.GetType().Name + " against test manifest " + discoveryTargetFileName.FullName + ": " + e.Message);
                }
                discoveredTests.AddRange(discoveryTargetTests);
            }

            return discoveredTests;
        }


        /// <summary>
        /// Get all of the types from assembly which are subclasses of specified DiscoveryAdaptor.
        /// </summary>
        private static IEnumerable<Type> GetDiscoveryAdaptors(Assembly adaptorsAssembly, Type discoveryAdaptorType)
        {
            try
            {
                return adaptorsAssembly.GetTypes().Where(candidateType => candidateType.IsSubclassOf(discoveryAdaptorType));
            }
            catch (ReflectionTypeLoadException e)
            {
                throw new TypeLoadException("Failed to load Discovery Adaptors:" + e.LoaderExceptions.ToCommaSeparatedList());
            }
        }

        /// <summary>
        /// Given a directory info and a path relative to that directory that
        /// can include wildcards, expand that expression to get a collection
        /// of files. This is similar to Directory.GetFiles natively, except
        /// that Path.Combine where the second item is a file expression
        /// without any directory (such as *.xtc in the root directory) causes
        /// an exception to be thrown. This method encapsulates robustness
        /// logic to support that scenario.
        /// </summary>
        /// <param name="discoveryRootDirectoryPath">DirectoryInfo describing the root from where discovery is performed.</param>
        /// <param name="relativePath">A file expression path relative to the discovery root directory.</param>
        /// <param name="defaultTestInfo">Default TestInfo.</param>
        /// <param name="filteringSettings">Filtering settings.</param>
        /// <returns>Collection of concrete files from the expanded expression.</returns>
        private static IEnumerable<FileInfo> GetFiles(DirectoryInfo discoveryRootDirectoryPath, string relativePath, TestInfo defaultTestInfo, FilteringSettings filteringSettings)
        {
            // A DiscoveryTarget Path is allowed to use wildcards, such as
            // "*.xtc", so we need to separate the path into directory and
            // file name pattern components in order to call
            // Directory.GetFiles.
            //TODO - Clean up algorithm
            // The Relative/Absolute stuff is because Path.Combine where the
            // second item is a file expression without any directory causes
            // an exception. I might be able to tweak this to be three lines
            // and a little more concise.
            string discoveryTargetRelativeDirectory = Path.GetDirectoryName(relativePath);
            string discoveryTargetAbsoluteDirectory = Path.Combine(discoveryRootDirectoryPath.FullName, discoveryTargetRelativeDirectory);
            string discoveryTargetFileNamePattern = Path.GetFileName(relativePath);
            string[] discoveryTargetFileNames;

            if (Directory.Exists(discoveryTargetAbsoluteDirectory))
            {
                discoveryTargetFileNames = Directory.GetFiles(discoveryTargetAbsoluteDirectory, discoveryTargetFileNamePattern);

                if (discoveryTargetFileNames.Length == 0)
                {
                    if (DllMayNotHaveBeenBuilt(defaultTestInfo, filteringSettings))
                    {
                        LogWarning("No files found matching the DiscoveryTarget's path '"
                            + discoveryTargetAbsoluteDirectory + "\\" + discoveryTargetFileNamePattern
                            + "' could be found, but this may be ok if they were intentionally not built for a test run using an incompatible product version.");
                        return new Collection<FileInfo>();
                    }
                    else
                    {
                        throw new FileNotFoundException("No files found matching the DiscoveryTarget's path. Did you forget to build " + discoveryTargetAbsoluteDirectory + "\\" + discoveryTargetFileNamePattern + "?", discoveryTargetFileNamePattern);
                    }
                }
            }
            else
            {
                if (DllMayNotHaveBeenBuilt(defaultTestInfo, filteringSettings))
                {
                    LogWarning(discoveryTargetAbsoluteDirectory + " could not be found, but this may be ok if it was intentionally not built for a test run using an incompatible product version.");
                    return new Collection<FileInfo>();
                }
                else
                {
                    throw new DirectoryNotFoundException(discoveryTargetAbsoluteDirectory + " could not be found. Did you forget to build it?");
                }
            }

            return discoveryTargetFileNames.Select(fileName => new FileInfo(fileName));
        }

        private static void LogWarning(string warningMessage)
        {
            Console.WriteLine("DISCOVERY WARNING: " + warningMessage);
        }

        private static bool DllMayNotHaveBeenBuilt(TestInfo defaultTestInfo, FilteringSettings filteringSettings)
        {
            // Asks whether a version filter was set and if so, whether none of those specified versions matched any of the default testinfo's versions.
            // If so, it may be the case that the dll was not built because we are building tests against an older product version
            // than the tests in that dll are targeted to. In this case we return true, saying that the dll may not
            // have been built. Callers can use this method to determine whether they should throw or warn if they don't
            // see the expected dll.
            if (filteringSettings.Versions != null && !filteringSettings.Versions.Any(filterVersion => filteringSettings.VersionMatcher.VersionMatches(defaultTestInfo.Versions, filterVersion)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// If an Adaptor does an Assembly.GetTypes(), reflection will need to
        /// look up the inheritance tree. So if a type in a test dll has any
        /// reference in it's public API to something in TestRuntime,
        /// for example, it will need to load that assembly. Reflection can by
        /// default find references that are in the same directory or in the GAC.
        /// For shared dlls like TestRuntime they will not be in the same
        /// directory, and we don't want to GAC dlls simply for this purpose.
        /// Instead, we can intercept the AssemblyResolve event which is raised
        /// when reflection can't figure out the reference via either of the two
        /// aforementioned methods. In that case, we are able to provide additional
        /// assistance in finding the dll.
        /// </summary>
        /// <param name="name">Fully qualified of assembly being searched for.</param>
        /// <param name="discoveryInfoFilePath">Location of DiscoveryInfo file, which is the root to search from.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        private static Assembly OnReflectionAssemblyResolve(string name, FileInfo discoveryInfoFilePath)
        {
            string fileName = name.Substring(0, name.IndexOf(','));
            string filePath;
            // Right now we have a hard-coded set of known common dlls that we
            // support. Since this logic is cleanly contained, we can expand to
            // a more robust approach as needed.
            switch (fileName)
            {
                case "TestRuntime":
                    filePath = Path.Combine(discoveryInfoFilePath.DirectoryName, @"Common\TestRuntime.dll");
                    break;
                default:
                    return null;
            }

            Assembly assembly= Assembly.LoadFrom(filePath);

            return assembly;
        }

        #endregion

        #region Target Area Filtering

        private static bool ShouldRunTarget(DiscoveryTarget discoveryTarget, IEnumerable<string> areasToDiscover)
        {
            bool shouldRunTarget = false;
            IEnumerable<string> areasExpectedFromTarget = ComputeExpectedAreas(discoveryTarget);

            if (discoveryTarget.SkipTargetAreaFiltering)
            {
                shouldRunTarget = true;
            }
            else if (areasToDiscover == null)
            {
                shouldRunTarget = true;
            }
            else if (areasExpectedFromTarget == null)
            {
                shouldRunTarget = false;
            }
            else
            {
                shouldRunTarget = areasToDiscover.Any(areaToDiscover => areasExpectedFromTarget.ContainsSubstring(areaToDiscover, StringComparison.OrdinalIgnoreCase));
            }

            return shouldRunTarget;
        }

        private static bool ContainsSubstring(this IEnumerable<string> collection, string value, StringComparison comparisonType)
        {
            foreach (string item in collection)
            {
                if (item.IndexOf(value, comparisonType) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Verify that if the DiscoveryTarget did not opt out of target
        /// filtering, that all TestInfos come from one of the expected areas, and
        /// that each of the expected areas is found in at least one TestInfo.
        /// If either of these is not true, trace an error.
        /// </summary>
        private static void VerifyAreaIndex(DiscoveryTarget discoveryTarget, IEnumerable<TestInfo> testInfosFromTarget)
        {
            IEnumerable<string> areasExpectedFromTarget = ComputeExpectedAreas(discoveryTarget);
            IEnumerable<string> areasDiscovered = testInfosFromTarget.Select(testInfo => testInfo.Area).Distinct();

            if (!discoveryTarget.SkipTargetAreaFiltering)
            {
                // If any of the tests discovered came from an area other than one specified by the target, that is a violation.
                // This is super dangerous because it means that if we had been doing target filtering, this
                // case would have missed. This violation will always get caught in the lab.
                foreach (string areaDiscovered in areasDiscovered)
                {
                    if (!areasExpectedFromTarget.Contains(areaDiscovered))
                    {
                        Trace.TraceError("A test from area '{0}' was returned, which was not one of the possible areas specified by the target.", areaDiscovered);
                    }
                }

                // If for any of the areas specified by the target no tests were discovered for that area, that is a violation.
                // This isn't dangerous like above, but is wasteful since the target is being run unnecessarily.
                foreach (string areaExpected in areasExpectedFromTarget)
                {
                    if (!areasDiscovered.Contains(areaExpected))
                    {
                        Trace.TraceError("Target specified '{0}' as one of the areas it returns, which was not one of the actual returned test areas.", areaExpected);
                    }
                }
            }
        }

        private static IEnumerable<string> ComputeExpectedAreas(DiscoveryTarget discoveryTarget)
        {
            IEnumerable<string> expectedAreas = null;

            if (discoveryTarget.Areas != null)
            {
                expectedAreas = discoveryTarget.Areas;
            }
            // If the DiscoveryTarget doesn't specify an areas collection, we can filter
            // off of the Area value of its DefaultTestInfo.
            else if (discoveryTarget.DefaultTestInfo != null && !String.IsNullOrEmpty(discoveryTarget.DefaultTestInfo.Area))
            {
                expectedAreas = new string[] { discoveryTarget.DefaultTestInfo.Area };
            }
            else if (!discoveryTarget.SkipTargetAreaFiltering)
            {
                Trace.TraceError("Target for path '{0}' had filtering enabled, but the expected areas could not be determined because it did not specify a set of expected areas, nor did it specify a DefaultTestInfo with a default area value.", discoveryTarget.Path);
            }

            return expectedAreas;
        }

        #endregion
    }
}

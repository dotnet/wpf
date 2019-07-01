// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Permissions;
using System.Xml;
using Microsoft.Test.Logging;

namespace Microsoft.Test
{
    /// <summary>
    /// Provides Driver/Test Process a means of gathering state from the invoker of the test process.
    /// This is achieved by exposing a subset of TestInfo data + Environment variables for execution only data.
    /// </summary>
    public static class DriverState
    {
        /// <summary>
        /// Initializes the state of the driver. 
        /// This is an implementation detail owned by the Infrastructure team, which is liable to change to support Execution Groups.
        /// Only Drivers are to use this method.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        public static void Initialize()
        {
            using (XmlTextReader reader = new XmlTextReader(Path.Combine(ExecutionDirectory, "TestInfos.xml")))
            {
                TestInfos = (List<TestInfo>)ObjectSerializer.Deserialize(reader, typeof(List<TestInfo>), null);
            }
        }

        /// <summary>
        /// Name of the Test Case.
        /// </summary>
        public static string TestName
        {
            get
            {
                return Current.Name;
            }
        }

        /// <summary>
        /// Area of the Test Case.
        /// </summary>
        public static string TestArea
        {
            get
            {
                return Current.Area;
            }
        }

        /// <summary>
        /// SubArea of the Test Case.
        /// </summary>
        public static string TestSubArea
        {
            get
            {
                return Current.SubArea;
            }
        }

        /// <summary>
        /// A string Dictionary that is used by the Driver to Launch the Test.
        /// </summary>
        public static ContentPropertyBag DriverParameters
        {
            get
            {
                return Current.DriverParameters;
            }
        }

        /// <summary>
        /// Checks whether this is another set of DriverState information.
        /// </summary>
        public static bool HasNext()
        {
            return (index + 1 < TestInfos.Count);
        }

        /// <summary>
        /// Move ahead to the next set of DriverState information.
        /// </summary>
        public static void Next()
        {
            if (index + 1 < TestInfos.Count)
            {
                index++;
                current = TestInfos[index];
            }
            else
            {
                throw new IndexOutOfRangeException();
            }            
        }

        /// <summary>
        /// Legacy stabilization feature. Tests should be migrated to binplace their file input dependencies.
        /// </summary>
        public static string TestBinRoot
        {
            [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
            get
            {
                return DriverLaunchSettings.GetTestBinRoot();
            }
        }

        /// <summary>
        /// Provides the Path of the Test Execution Directory.
        /// </summary>
        public static string ExecutionDirectory
        {
            [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
            get
            {
                // TODO:  Maybe we don't need this, re-evaluate after initial testing
                if (Directory.Exists(DriverLaunchSettings.GetExecutionDirectory()))
                {
                    return (string)DriverLaunchSettings.GetExecutionDirectory();
                }
                return @".\";
            }
        }

        /// <summary>
        /// Provides the current TestInfo object as loaded by the Driver.
        /// </summary>
        private static TestInfo Current
        {
            get
            {
                if (TestInfos == null)
                {
                    Initialize();
                    current = TestInfos[index];

                    // Temporary testing hack for getting at current
                    if (Log.Current != null && !String.Equals(Log.Current.Name, current.Name))
                    {
                        for (int i = 0; i < TestInfos.Count; i++)
                        {
                            if (String.Equals(Log.Current.Name, TestInfos[i].Name))
                            {
                                index = i;
                                current = TestInfos[index];
                                break;
                            }
                        }
                    }
                }
                return current;
            }
        }
        private static TestInfo current;
        private static int index = 0;

        /// <summary>
        /// Provides the current TestInfo object as loaded by the Driver.
        /// </summary>
        private static List<TestInfo> TestInfos { get; set; }
    }
}
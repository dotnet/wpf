// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Loader;

namespace Microsoft.Test
{
    /// <summary>
    /// Data structure to present a strongly typed representation of the DriverParameters.
    /// </summary>
    internal class StiDriverParameters
    {
        /// <summary>
        /// Constructs data structure from a loosely typed PropertyBag.
        /// </summary>
        /// <param name="driverParameters">PropertyBag containing string key/value pairs.</param>
        public StiDriverParameters(PropertyBag driverParameters, string[] args)
        {
            MethodParams = new string[0];
            CtorParams = new string[0];

            // I tested this standalone and it seemed to work, but it might need to be full path, and
            // I'm not sure when it does/doesn't work overall.
            // The original call was Assembly.LoadFrom and LoadWithPartialName, both of which are FxCop violations.
            // Check whether a file exists with a matching name. If so, get corresponding
            // AssemblyName which Assembly.Load can accept.
            if (File.Exists(driverParameters["Assembly"]))
            {
                Trace.WriteLine("Opening " + driverParameters["Assembly"]);
                AssemblyName assemblyName = AssemblyName.GetAssemblyName(driverParameters["Assembly"]);
                Assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            }

            //HACK: Folks like Appmodel don't include .dll which breaks on Assembly.Load!
            //Need to decide policy here... ie- fix all the data?
            else if (File.Exists(driverParameters["Assembly"] + ".dll"))
            {
                Trace.WriteLine("Opening " + driverParameters["Assembly"] + ".dll");
                AssemblyName assemblyName = AssemblyName.GetAssemblyName(driverParameters["Assembly"] + ".dll");
                Assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            }
            // If the driver parameter for Assembly was a fully qualified reference, no
            // file would have matched that value. So we call Assembly.Load directly with
            // the fully qualified name.
            // NOTE: There is a scenario where the assembly name was not fully qualified,
            //       but the dll wasn't found because the tester failed to include it as
            //       a support file. Since Assembly.Load in this case will throw a file
            //       not found exception, that's good enough.
            else
            {
                Assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(driverParameters["Assembly"]));
            }

            Class = Assembly.GetType(driverParameters["Class"], true, true);

            if (driverParameters["MethodParams"] != null)
            {
                MethodParams = driverParameters["MethodParams"].FromCommaSeparatedList<string>().ToArray();
            }

            if (args.Length > 0)
            {
                MethodParams = MethodParams.Concat(args).ToArray();
            }

            if (driverParameters["CtorParams"] != null)
            {
                CtorParams = driverParameters["CtorParams"].FromCommaSeparatedList<string>().ToArray();
            }

            Method = driverParameters["Method"];

            if (driverParameters["SecurityLevel"] != null)
            {
                SecurityLevel = (TestCaseSecurityLevel)Enum.Parse(typeof(TestCaseSecurityLevel), driverParameters["SecurityLevel"]);
            }
        }

        public Assembly Assembly { get; private set; }
        public Type Class { get; private set; }
        public string[] MethodParams { get; private set; }
        public string[] CtorParams { get; private set; }
        public string Method { get; private set; }
        public TestCaseSecurityLevel SecurityLevel { get; private set; }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.Test.Distribution
{
    /// <summary>
    /// DistributionStrategy: Define the strategy to distribute tests. 
    /// </summary>
    public abstract class DistributionStrategy
    {
        /// <summary>
        /// Distribute tests. 
        /// </summary>
        /// <param name="tests">Tests to distribute</param>
        /// <param name="machines">Machines to distribute to</param>
        /// <param name="testBinariesDirectory">Location of test binaries</param>
        /// <returns>List of TestCollection partitioned</returns>
        public abstract List<TestRecords> PartitionTests(TestRecords tests, MachineRecord[] machines, DirectoryInfo testBinariesDirectory);

        /// <summary>
        /// Create a DistributionStrategy based on the prefix. 
        /// </summary>
        /// <param name="strategyPrefix">Prefix of the DistributionStrategy</param>
        /// <returns>DistributionStrategy created</returns>
        public static DistributionStrategy CreateDistributionStrategy(string strategyPrefix)
        {
            if (String.IsNullOrEmpty(strategyPrefix))
            {
                strategyPrefix = "Functional";
            }

            // Assume that Strategy is defined in the same namespace and assembly with DistributionStrategy, 
            // and named as stategyPrefix + "DistributionStrategy". 
            string strategyTypeName = strategyPrefix + "DistributionStrategy";
            strategyTypeName = String.Format("{0}.{1}", typeof(DistributionStrategy).Namespace, strategyTypeName);
            Assembly strategyAssembly = Assembly.GetAssembly(typeof(DistributionStrategy));
            Type strategyType = strategyAssembly.GetType(strategyTypeName);

            if (strategyType == null)
            {
                throw new NotSupportedException(String.Format("Type: {0} not found in assembly {1}.", strategyTypeName, strategyAssembly.FullName));
            }

            DistributionStrategy strategy = (DistributionStrategy)Activator.CreateInstance(strategyType);

            return strategy;
        }
    }
}

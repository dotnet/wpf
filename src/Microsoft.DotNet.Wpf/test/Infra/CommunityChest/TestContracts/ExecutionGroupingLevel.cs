// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test
{
    /// <summary>
    /// Specifies the degree of execution grouping optimizations permitted for
    /// the test. The default isolation level is FullIsolation.
    /// </summary>
    public enum ExecutionGroupingLevel
    {
        /// <summary>
        /// Test needs to be run in complete isolation - this should only be
        /// used for unmanageable tests which are flaky in the presence of
        /// others. Tests in this category "Don't play well with others."
        /// </summary>
        FullIsolation = 0,

        /// <summary>
        /// Infra can apply State Management operations once for tests with
        /// matching requirements, rather than on a per-test basis.
        /// </summary>
        SharedStateManagement = 1,

        /// <summary>
        /// Infra can binplace support files for matching tests once, rather
        /// than on a per-test basis. State management is also shared at this
        /// level.
        /// </summary>
        SharedSupportFiles = 2,

        /// <summary>
        /// This is used as a default value on Attribute discovery. 
        /// This should not ever be used directly in any test definitions.
        /// </summary>
        InternalDefault_DontUseThisLevel = 3,

        /// <summary>
        /// Infra can run all the matching STI tests under a single AppDomain,
        /// as well as sharing support files and state management for high
        /// performance.
        /// </summary>
        SharedAppDomains = 4,

        /// <summary>
        /// Infra can exploit all possible caching/performance enhancements to
        /// maximize execution performance.
        /// </summary>
        MaximalPerformance = 10,
    }
}
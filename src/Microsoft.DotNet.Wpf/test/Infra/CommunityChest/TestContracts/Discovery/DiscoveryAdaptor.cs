// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Test.Discovery
{
    // Base class for a DiscoveryAdaptor. Original implementation was IDisposable with some
    // reflection hackery to shutdown the wpf dispatcher in case an adaptor spun up a
    // dispatcher. (TestExtenderAdaptor might be one culprit - I see it uses XamlReader)
    // Adaptors should be responsible citizens. We can always bring it back, but it's cut
    // for now.

    /// <summary>
    /// Abstract base class for a DiscoveryAdaptor.
    /// </summary>
    public abstract class DiscoveryAdaptor
    {
        #region Public and Protected Members

        /// <summary>
        /// Produce a set of tests from a manifest and default TestInfo. Each adaptor understands
        /// a particular type of test manifest, whether it is a binary, xml file, etc. The default
        /// TestInfo contains default values for each test the adaptor produces, where the adaptor
        /// overrides particular values based upon the test manifest. The test manifest is for
        /// discovery only in this context. If a test manifest contains data that is needed at
        /// execution time, the test manifest itself of the default TestInfo should specify the test
        /// manifest as a support file. The adaptor itself should not automatically make the
        /// test manifest a support file.
        /// </summary>
        /// <param name="testManifestPath">Test manifest.</param>
        /// <param name="defaultTestInfo">TestInfo with default values to use for discovered test.</param>
        /// <returns>Collection of TestInfos.</returns>
        public abstract IEnumerable<TestInfo> Discover(FileInfo testManifestPath, TestInfo defaultTestInfo);

        #endregion
    }
}

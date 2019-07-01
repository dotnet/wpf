// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// Information on the discoverability of one or more test manifests.
    /// </summary>
    // Comparing two DiscoveryTarget instances is not an important scenario.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes"), Serializable()]
    public class DiscoveryTarget
    {
        #region Public Members

        /// <summary>
        /// Name of the adaptor to use to discover the test manifests.
        /// </summary>
        [XmlAttribute()]
        public string Adaptor { get; set; }
        
        /// <summary>
        /// Relative path to the test manifest(s), which permits wildcards.
        /// Path is relative to the DiscoveryInfo.
        /// </summary>
        [XmlAttribute()]
        public string Path { get; set; }

        /// <summary>
        /// Data which is used as the initial TestInfo value for the set of
        /// discovered tests. This may be null.
        /// </summary>
        public TestInfo DefaultTestInfo { get; set; }

        /// <summary>
        /// Index of Areas that this target will return, used for target filtering.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227")]
        public Collection<string> Areas { get; set; }

        /// <summary>
        /// True if you want to skip target area filtering on an adaptor.
        /// The consequence of true is that the adaptor will not
        /// participate in target filtering and will always be run. This makes sense
        /// for scenarios such as the Drts where the adaptor returns cases from all
        /// areas, but an individual team's target should not do this, as it
        /// defeats the point of target filtering.
        ///
        /// The default value of false means that the target does participate in
        /// target area filtering. In this case, the Areas collection lists the
        /// various areas the target promises to return. (Should be an exact match)
        /// If the target returns only one area (which is preferable, actually) then
        /// instead of specifying an Areas collection you can simply specify an
        /// area value on the DefaultTestInfo.
        /// </summary>
        [XmlAttribute()]
        public bool SkipTargetAreaFiltering { get; set; }

        #endregion
    }
}

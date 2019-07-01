// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// Contains information about the adaptors assembly and DiscoveryTargets.
    /// </summary>
    // Comparing two DiscoveryInfo instances is not an important scenario.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    [Serializable()]
    public struct DiscoveryInfo
    {
        #region Public Members

        /// <summary>
        /// Relative path to the assembly containing the DiscoveryAdaptors.
        /// Path is relative to the DiscoveryInfo location.
        /// </summary>
        [XmlAttribute()]
        public string AdaptorsAssembly { get; set; }

        /// <summary>
        /// Relative path to the file describing the partial order of version IDs.
        /// Path is relative to the DiscoveryInfo location.
        /// </summary>
        [XmlAttribute()]
        public string VersionOrder { get; set; }

        /// <summary>
        /// Collection of targets
        /// </summary>
        // The suppression is because without a setter an infinite loop occurs. Another option would be to
        // not use an automatic property and make the default value an empty collection. Real XML Serialization
        // can handle this scenario, but our homebrew ObjectSerializer doesn't.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Collection<DiscoveryTarget> Targets { get; set; }

        /// <summary>
        /// Data which is used as the initial TestInfo value for the set of
        /// discovered tests. This may be null.
        /// </summary>
        public TestInfo DefaultTestInfo { get; set; }

        #endregion
    }
}

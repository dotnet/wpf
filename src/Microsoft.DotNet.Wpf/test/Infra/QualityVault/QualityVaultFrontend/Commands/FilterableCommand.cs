// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.CommandLineParsing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Test.Filtering;

namespace Microsoft.Test.Commands
{
    /// <summary/>
    [Description("Provides filtering properties that concrete commands can inherit.")]
    public abstract class FilterableCommand : Command
    {
        /// <summary>
        /// Filter TestRecords such that only TestRecords whose TestInfo's Name
        /// property matches one of the specified values.
        /// </summary>
        [Description("Only execute TestRecords whose TestInfo's Name property matches one of these comma separated values.")]
        new public IEnumerable<string> Name { get; set; }

        /// <summary>
        /// Filter TestRecords such that only TestRecords whose TestInfo's Area
        /// property matches one of the specified values. Also engages target filtering.
        /// </summary>
        [Description("Only execute TestRecords whose TestInfo's Area property matches one of these comma separated values. Also engages DiscoveryTarget Area filtering")]
        //TODO: There is some controversy as to the name 'Area' instead of 'Areas', since the name
        // is singular whereas the value is plural, which stands in contrast to Versions. To change
        // this would be a breaking change, however, so it remains Area for now. Revisit.
        public IEnumerable<string> Area { get; set; }

        /// <summary>
        /// Filter TestRecords such that only TestRecords whose TestInfo's SubArea
        /// property matches the specified value.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        [Description("Only execute TestRecords whose TestInfo's SubArea property matches this value.")]
        public IEnumerable<string> SubArea { get; set; }

        /// <summary>
        /// Filter TestRecords such that only TestRecords whose TestInfo's Priority
        /// property matches one of the specified values.
        /// </summary>
        [Description("Only execute TestRecords whose TestInfo's Priority property matches one of these comma separated values.")]
        public IEnumerable<int> Priority { get; set; }

        /// <summary>
        /// Filter TestRecords such that only TestRecords whose TestInfo's Versions
        /// collection includes at least one value which matches one of the specified values.
        /// </summary>
        [Description("Only execute TestRecords whose TestInfo's Versions collection contains one of these comma separated values.")]
        public IEnumerable<string> Versions { get; set; }

        /// <summary>
        /// Filter TestRecords such that only TestRecords whose TestInfo's Keywords
        /// collection includes at least one value which matches one of the specified values.
        /// </summary>
        [Description("Only execute TestRecords whose TestInfo's Keywords collection contains one of these comma separated values.")]
        public IEnumerable<string> Keywords { get; set; }

        /// <summary>
        /// Filter TestRecords such that only TestRecords whose TestInfo's Disabled
        /// property matches the specified value.
        /// </summary>
        [Description("Only execute TestRecords whose TestInfo's Disabled property matches this value.")]
        public bool? Disabled { get; set; }

        /// <summary>
        /// This property is not meant to be specified at the command line like
        /// the others (hence it is protected) but is instead used by
        /// subclasses to transform the policy-filled Command into a policy-
        /// free data structure that simply communicates filtering settings for
        /// the filtering engine to consume.
        /// </summary>
        protected FilteringSettings FilteringSettings
        {
            get
            {
                FilteringSettings settings = new FilteringSettings();
                settings.Area = Area;
                settings.Disabled = Disabled;
                settings.Keywords = Keywords;
                settings.Name = Name;
                settings.Priority = Priority;
                settings.SubArea = SubArea;
                settings.Versions = Versions;
                return settings;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Test.Filtering
{
    /// <summary>
    /// Describes filtering restrictions to apply with the Filtering Engine.
    /// </summary>
    public class FilteringSettings
    {
        /// <summary/>
        public IEnumerable<string> Name { get; set; }

        /// <summary/>
        public IEnumerable<string> Area { get; set; }

        /// <summary/>
        public IEnumerable<string> SubArea { get; set; }

        /// <summary/>
        public IEnumerable<int> Priority { get; set; }

        /// <summary/>
        public IEnumerable<string> Versions { get; set; }

        /// <summary/>
        public IEnumerable<string> Keywords { get; set; }

        /// <summary/>
        public bool? Disabled { get; set; }

        /// <summary/>
        internal VersionMatcher VersionMatcher { get; set; }

        /// <summary/>
        internal void EnsureVersions()
        {
            if (Versions == null)
            {
                List<String> list = new List<String>(1);
                Versions = list;
            }
        }
    }
}

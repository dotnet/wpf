// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.Test
{
    /// <summary>
    /// Class which contains the info regarding a single variation execution.
    /// </summary>
    public class VariationRecord
    {
        /// <summary/>
        public VariationRecord()
        {
            LoggedFiles = new Collection<FileInfo>();
        }

        /// <summary>
        /// The Index of the variation. Unique identifier in scope of each test.
        /// </summary>
        public int VariationId { get; set; }

        /// <summary/>
        public string VariationName { get; set; }

        /// <summary/>
        public Result Result { get; set; }

        /// <summary>
        /// Log from Variation.
        /// </summary>
        public string Log { get; set; }

        /// <summary/>
        public Collection<FileInfo> LoggedFiles { get; private set; }

        /// <summary>
        /// Time at which the test started logging the variation.
        /// Use InfraTime to get enough precision and workaround bug 824978
        /// </summary>
        public InfraTime StartTime { get; set; }

        /// <summary>
        /// Time at which the variation was recorded as over.
        /// Use InfraTime to get enough precision and workaround bug 824978
        /// </summary>
        public InfraTime EndTime { get; set; }

    }
}

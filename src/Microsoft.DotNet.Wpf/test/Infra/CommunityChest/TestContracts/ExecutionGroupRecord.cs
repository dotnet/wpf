// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.IO;

namespace Microsoft.Test
{
    /// <summary>
    /// Provides bookkeeping record of time spent in Execution Groups
    /// The design is liable to evolve as we tie this more closely with the TestRecord concept.
    /// </summary>
    [Serializable()]
    [ObjectSerializerAttribute(typeof(FastObjectSerializer))]
    public class ExecutionGroupRecord
    {
        /// <summary/>
        public ExecutionGroupRecord()
        {
        }

        /// <summary/>
        public static ExecutionGroupRecord Begin(ExecutionGroupType type, string Area)
        {
            ExecutionGroupRecord group = new ExecutionGroupRecord();
            group.StartTime = new InfraTime(DateTime.Now);
            group.ExecutionGroupType = type;
            group.Area = Area;
            group.ExecutionGroupRecords = new Collection<ExecutionGroupRecord>();
            return group;
        }

        /// <summary/>
        public void End()
        {
            EndTime = new InfraTime(DateTime.Now);
        }

        /// <summary/>
        public ExecutionGroupType ExecutionGroupType { get; set; }

        /// <summary/>
        public string Area { get; set; }

        /// <summary/>
        public InfraTime StartTime { get; set; }

        /// <summary/>
        public InfraTime EndTime { get; set; }

        /// <summary/>
        public Collection<FileInfo> LoggedFiles { get; set; }

        /// <summary/>
        public Collection<ExecutionGroupRecord> ExecutionGroupRecords { get; set; }
    }
}
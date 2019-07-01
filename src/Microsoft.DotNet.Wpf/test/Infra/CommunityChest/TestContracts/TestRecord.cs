// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.Test
{    
    /// <summary>
    /// This type forms the nucleus for executing and reporting contract.
    /// It provides the payload of log information from Test Execution to Reporting/Visualization tools.
    /// This purely a passive data structure.
    /// </summary>   
    [Serializable()]
    [ObjectSerializerAttribute(typeof(FastObjectSerializer))]
    public class TestRecord
    {

        /// <summary/>
        public TestRecord()
        {
            Variations = new Collection<VariationRecord>();
            LoggedFiles = new Collection<FileInfo>();
            ExecutionLogFiles = new Collection<FileInfo>();
            ExecutionEnabled = true;
        }

        /// <summary>
        /// Provides invariant test metadata pertinent to filtering and how to run the test.
        /// </summary>
        public TestInfo TestInfo { get; set; }

        /// <summary>
        /// Information about the machine the test ran on.
        /// </summary>
        public MachineRecord Machine { get; set; }

        /// <summary>
        /// Log from harness driver - This does not cover variation logging.
        /// </summary>
        [XmlAttribute()]
        public string Log { get; set; }

        /// <summary>
        /// Set of files that were logged by the test.
        /// </summary>
        public Collection<FileInfo> LoggedFiles { get; private set; }

        /// <summary>
        /// Set of Execution Group Log Files that detail the context in which
        /// the test was executed.
        /// </summary>
        public Collection<FileInfo> ExecutionLogFiles { get; private set; }

        /// <summary>
        /// Indicates if a Test should run or not. Execution only uses this field to ignore tests.
        /// </summary>
        [XmlAttribute()]
        public bool ExecutionEnabled { get; set; }

        /// <summary>
        /// Explains why a test was not enabled for execution.
        /// </summary>
        [XmlAttribute()]
        public string FilteringExplanation { get; set; }

        /// <summary>
        /// All of the variations for this particular test
        /// </summary>
        public Collection<VariationRecord> Variations { get; private set; }

        /// <summary>
        /// Execution Duration for the test. 
        /// This includes summation of time spent in test variations + proportion of process overhead shared by all the tests which ran in the Execution Group process.        
        /// </summary>
        [XmlAttribute()]
        public TimeSpan Duration { get; set; }
    }
}

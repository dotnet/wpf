// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// This attribute tells the Adaptor to search a type for tests on its methods. It is also used to set
    /// default metadata values for a type or assembly.
    /// </summary>
    /// <remarks>
    /// TestDefaultsAttribute MAY NOT appear on the same class as a TestAttribute but 
    /// TestDefaultsAttribute CAN appear on a base class of a class with a TestAttribute.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, Inherited = true, AllowMultiple = false)]
    public sealed class TestDefaultsAttribute : Attribute
    {
        #region Public and Protected Members

        /// <summary>
        /// Default test priority.
        /// </summary>
        public int DefaultPriority
        {
            get { return defaultPriority; }
            set { defaultPriority = value; }
        }
        private int defaultPriority = -1;
        
        /// <summary>
        /// Default test SubArea. 
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public string DefaultSubArea { get; set; }
        
        /// <summary>
        /// Default test name. ie Element Services Test
        /// </summary>
        public string DefaultName { get; set; }
        
        /// <summary>
        /// Default test timeout.
        /// </summary>
        public int DefaultTimeout
        {
            get { return defaultTimeout; }
            set { defaultTimeout = value; }
        }
        private int defaultTimeout = -1;
        
        /// <summary>
        /// Default support files. Unlike other properties, support files will
        /// combine with support files in assembly TestDefaults and adaptor default
        /// test info.
        /// </summary>
        public string SupportFiles { get; set; }
        
        /// <summary>
        /// Default test method name.
        /// </summary>
        public string DefaultMethodName { get; set; }

        #endregion
    }
}

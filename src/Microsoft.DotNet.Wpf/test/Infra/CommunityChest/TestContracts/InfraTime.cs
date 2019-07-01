// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;

namespace Microsoft.Test
{
    /// <summary>
    /// Custom DateTime for QV. 
    /// </summary>
    [TypeConverter(typeof(InfraTimeConverter))]
    public class InfraTime
    {
        /// <summary>
        /// Constructor with a DateTime
        /// </summary>
        /// <param name="dateTime">DateTime</param>
        public InfraTime(DateTime dateTime)
        {
            DateTime = dateTime;
        }

        /// <summary/>
        public DateTime DateTime { get; set; }
    }
}
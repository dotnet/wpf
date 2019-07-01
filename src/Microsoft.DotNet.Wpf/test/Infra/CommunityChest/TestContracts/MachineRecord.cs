// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace Microsoft.Test
{
    /// <summary>
    /// A record of the state of a machine.
    /// </summary>
    public class MachineRecord
    {
        /// <summary>
        /// Architecture of machine.
        /// </summary>
        public string Architecture { get; set; }

        /// <summary>
        /// Culture of machine.
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// Number of monitors enabled on the machine.
        /// </summary>
        public int MonitorCount { get; set; }

        /// <summary>
        /// Name of machine.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// OS of machine.
        /// </summary>
        public string OperatingSystem { get; set; }
    }
}
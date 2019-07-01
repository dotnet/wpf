// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Configures an out-of-process automated application.
    /// </summary>
    [Serializable]
    public class OutOfProcessApplicationSettings : ApplicationSettings
    {
        /// <summary>
        /// The ProcessStartInfo to start a process.
        /// </summary>
        public ProcessStartInfo ProcessStartInfo
        {
            get;
            set;
        }
    }
}

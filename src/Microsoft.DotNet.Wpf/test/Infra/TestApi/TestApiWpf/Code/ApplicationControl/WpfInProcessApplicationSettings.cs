// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Configures a WPF in-process test application.
    /// </summary>
    [Serializable]
    public class WpfInProcessApplicationSettings : InProcessApplicationSettings
    {
        /// <summary>
        /// The window class to start. 
        /// </summary>
        /// <remarks>
        /// This must be the full class name.
        /// </remarks>
        public string WindowClassName
        {
            get;
            set;
        }
    }
}

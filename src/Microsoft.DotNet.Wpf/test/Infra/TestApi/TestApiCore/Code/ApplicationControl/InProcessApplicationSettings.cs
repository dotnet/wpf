// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Configures an in-process automated application.
    /// </summary>
    [Serializable]
    public class InProcessApplicationSettings : ApplicationSettings
    {
        /// <summary>
        /// Path to the application. 
        /// </summary>
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// The type of application to create.
        /// </summary>
        public InProcessApplicationType InProcessApplicationType
        {
            get;
            set;
        }        
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Defines the Thread and AppDomain properties of an InProcessApplication.
    /// </summary>
    [Serializable]
    public enum InProcessApplicationType
    {
        /// <summary>
        /// The application runs in-process and on a separate thread.
        /// </summary>
        InProcessSeparateThread = 0,

        /// <summary>
        /// The application runs in-process, on a separate thread and 
        /// in a separate AppDomain.
        /// </summary>
        InProcessSeparateThreadAndAppDomain,

        /// <summary>
        /// The application runs in-process and on the same thread.
        /// </summary>
        InProcessSameThread
    }
}

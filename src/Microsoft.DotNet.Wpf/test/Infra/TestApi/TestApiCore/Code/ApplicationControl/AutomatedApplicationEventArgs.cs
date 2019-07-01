// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Represents the event args passed to AutomatedApplication events.
    /// </summary>
    public class AutomatedApplicationEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs an AutomatedApplicationEventArgs instance with the given
        /// AutomatedApplication.
        /// </summary>
        /// <param name="automatedApp">The AutomatedApplication data to pass to the listeners.</param>
        public AutomatedApplicationEventArgs(AutomatedApplication automatedApp)
        {
            AutomatedApplication = automatedApp;
        }

        /// <summary>
        /// The AutomatedApplication data passed to listeners.
        /// </summary>
        public AutomatedApplication AutomatedApplication
        {
            get;
            set;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Specifies the supported AutomatedApplication events.
    /// </summary>
    [Serializable]
    public enum AutomatedApplicationEventType
    {
        /// <summary>
        /// The test application's main window opened event.
        /// </summary>
        MainWindowOpenedEvent,

        /// <summary>
        /// The test application's closed event.
        /// </summary>
        ApplicationExitedEvent,

        /// <summary>
        /// The test application's main window's focus changed event.
        /// </summary>
        FocusChangedEvent
    }
}

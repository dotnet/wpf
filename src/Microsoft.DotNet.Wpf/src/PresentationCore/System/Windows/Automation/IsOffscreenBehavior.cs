// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Automation
{
    /// <summary>
    ///     This enum offers different ways of evaluating the IsOffscreen AutomationProperty
    /// </summary>
    public enum IsOffscreenBehavior
    {
        /// <summary>
        ///     The AutomationProperty IsOffscreen is calculated based on IsVisible.
        /// </summary>
        Default,
        /// <summary>
        ///     The AutomationProperty IsOffscreen is false.
        /// </summary>
        Onscreen,
        /// <summary>
        ///     The AutomationProperty IsOffscreen if true.
        /// </summary>
        Offscreen,
        /// <summary>
        ///     The AutomationProperty IsOffscreen is calculated based on clip regions.
        /// </summary>
        FromClip,
    }
}


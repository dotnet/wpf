// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using Microsoft.Test.Display;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Dwm Actions enable or disable DWM which support by vista or above
    /// </summary>
    public class DwmAction : SimpleDiscoverableAction
    {
        public double OccurrenceIndicator { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to enable DWM, or to disable DWM. 
        /// </summary>
        public bool IsEnablingDwm { get; set; }

        #region IAction Members

        /// <summary>
        /// Whether or not the action can be performed. 
        /// </summary>
        /// <returns>true is action can be performed, false otherwise.</returns>
        public override bool CanPerform()
        {
            OperatingSystem os = Environment.OSVersion;
            
            // Only vista or above can support DWM
            bool isSupportOS = os.Version.Major >= 6;
            
            // Do action only when OccurrenceIndicator is less than 0.002(1/500) and the OS is support the DWM
            return isSupportOS && OccurrenceIndicator < 0.002;
        }

        /// <summary>
        /// Perform the action. 
        /// </summary>
        public override void Perform()
        {
            DwmApi.DwmEnableComposition(IsEnablingDwm);
        }

        #endregion
    }
}

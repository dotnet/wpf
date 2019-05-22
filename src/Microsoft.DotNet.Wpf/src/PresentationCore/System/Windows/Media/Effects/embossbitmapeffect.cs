// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using SecurityHelper=MS.Internal.SecurityHelper; 

#endregion

namespace System.Windows.Media.Effects
{
    /// <summary>
    /// The class definition for EmbossBitmapEffect
    /// </summary>
    public partial class EmbossBitmapEffect
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public EmbossBitmapEffect()
        {
}

        /// <summary>
        /// Creates the unmanaged effect handle
        /// </summary>
        /// <SecurityNote>
        /// Critical - returns a security critical type SafeHandle.
        /// Safe     - Always returns null.
        /// </SecurityNote>
        [SecuritySafeCritical]
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        unsafe protected override SafeHandle CreateUnmanagedEffect()
        {
            return null;
        }

        /// <summary>
        /// /// Update (propagetes) properties to the unmanaged effect
        /// </summary>                    
        /// <SecurityNote>
        /// This method demands permission because effects should not be run
        /// in partial trust.
        /// 
        /// SecurityCritical - because SetValue has a link demand
        /// SecutiryTreatAsSafe - because it demans UIWindow permission
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        protected override void UpdateUnmanagedPropertyState(SafeHandle unmanagedEffect)
        {
            SecurityHelper.DemandUIWindowPermission();
        }
    }
}

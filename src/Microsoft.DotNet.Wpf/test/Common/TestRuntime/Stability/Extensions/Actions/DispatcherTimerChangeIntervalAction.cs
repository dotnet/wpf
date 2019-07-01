// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Threading;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class DispatcherTimerChangeIntervalAction : SimpleDiscoverableAction
    {
        #region Public Members

        public DispatcherTimer DispatcherTimer { get; set; }

        public TimeSpan TimeSpan { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            DispatcherTimer.Interval = TimeSpan;
        }

        #endregion
    }
}

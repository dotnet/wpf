// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Threading;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class DispatcherTimerChangeIsEnabledAction : SimpleDiscoverableAction
    {
        #region Public Members

        public DispatcherTimer DispatcherTimer { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            DispatcherTimer.IsEnabled = !DispatcherTimer.IsEnabled;
        }

        #endregion
    }
}

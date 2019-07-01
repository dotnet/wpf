// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Threading;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class DispatcherOperationWaitAction : SimpleDiscoverableAction
    {
        #region Public Members

        public DispatcherOperation DispatcherOperation { get; set; }

        public bool IsWaitForever { get; set; }

        public TimeSpan TimeSpan { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            if (IsWaitForever)
            {
                DispatcherOperation.Wait();
            }
            else
            {
                DispatcherOperation.Wait(TimeSpan);
            }
        }

        #endregion
    }
}

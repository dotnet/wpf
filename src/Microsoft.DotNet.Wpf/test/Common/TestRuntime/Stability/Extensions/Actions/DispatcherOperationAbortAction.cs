// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Threading;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class DispatcherOperationAbortAction : SimpleDiscoverableAction
    {
        #region Public Members

        public DispatcherOperation DispatcherOperation { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            DispatcherOperation.Abort();
        }

        #endregion
    }
}

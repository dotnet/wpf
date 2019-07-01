// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Threading;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class DispatcherOperationChangePriorityAction : SimpleDiscoverableAction
    {
        #region Public Members

        public DispatcherOperation DispatcherOperation { get; set; }

        public int NewPriorityValue { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            DispatcherOperation.Priority = ChooseValidPriority(NewPriorityValue);
        }

        #endregion

        #region Private Members

        private DispatcherPriority ChooseValidPriority(int priorityValue)
        {
            DispatcherPriority[] validPriorities = { DispatcherPriority.SystemIdle, DispatcherPriority.ApplicationIdle, DispatcherPriority.ContextIdle, DispatcherPriority.Background, DispatcherPriority.Input, DispatcherPriority.Loaded, DispatcherPriority.Render, DispatcherPriority.DataBind, DispatcherPriority.Normal, DispatcherPriority.Send };

            return validPriorities[priorityValue % validPriorities.Length];
        }

        #endregion
    }
}

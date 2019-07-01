// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Threading;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class DispatcherHooksSetEventAction : SimpleDiscoverableAction
    {
        #region Public Members

        public int EventType { get; set; }

        public bool IsAddHandler { get; set; }

        public Dispatcher Dispatcher { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            switch (EventType % 5)
            {
                case 0:
                    if (IsAddHandler)
                    {
                        Dispatcher.Hooks.DispatcherInactive += new EventHandler(HooksDispatcherInactive);
                    }
                    else
                    {
                        Dispatcher.Hooks.DispatcherInactive -= HooksDispatcherInactive;
                    }
                    break;
                case 1:
                    if (IsAddHandler)
                    {
                        Dispatcher.Hooks.OperationAborted += new DispatcherHookEventHandler(HooksOperationAborted);
                    }
                    else
                    {
                        Dispatcher.Hooks.OperationAborted -= HooksOperationAborted;
                    }
                    break;
                case 2:
                    if (IsAddHandler)
                    {
                        Dispatcher.Hooks.OperationCompleted += new DispatcherHookEventHandler(HooksOperationCompleted);
                    }
                    else
                    {
                        Dispatcher.Hooks.OperationCompleted -= HooksOperationCompleted;
                    }
                    break;
                case 3:
                    if (IsAddHandler)
                    {
                        Dispatcher.Hooks.OperationPosted += new DispatcherHookEventHandler(HooksOperationPosted);
                    }
                    else
                    {
                        Dispatcher.Hooks.OperationPosted -= HooksOperationPosted;
                    }
                    break;
                case 4:
                    if (IsAddHandler)
                    {
                        Dispatcher.Hooks.OperationPriorityChanged += new DispatcherHookEventHandler(HooksOperationPriorityChanged);
                    }
                    else
                    {
                        Dispatcher.Hooks.OperationPriorityChanged -= HooksOperationPriorityChanged;
                    }
                    break;
            }
        }

        #endregion

        #region Private Members

        private void HooksOperationPriorityChanged(object sender, DispatcherHookEventArgs e) { }

        private void HooksOperationPosted(object sender, DispatcherHookEventArgs e) { }

        private void HooksOperationCompleted(object sender, DispatcherHookEventArgs e) { }

        private void HooksOperationAborted(object sender, DispatcherHookEventArgs e) { }

        private void HooksDispatcherInactive(object sender, EventArgs e) { }

        #endregion
    }
}

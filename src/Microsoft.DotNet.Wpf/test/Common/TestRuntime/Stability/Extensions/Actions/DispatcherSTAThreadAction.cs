// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Threading;
using System.Windows.Threading;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class DispatcherSTAThreadAction : SimpleDiscoverableAction
    {
        #region Public Members

        public TimeSpan TimeOut { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Thread thread = new Thread(new ParameterizedThreadStart(AvalonWorkerThread));
            thread.Name = "Ownership Thread Avalon";
            thread.SetApartmentState(ApartmentState.STA);

            thread.Start(TimeOut);
        }

        #endregion

        #region Private Members

        private static void AvalonWorkerThread(object o)
        {
            TimeSpan timeSpan = (TimeSpan)o;
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = timeSpan;
            dispatcherTimer.Tick += delegate
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            };
            dispatcherTimer.Start();
            Dispatcher.Run();
        }

        #endregion
    }
}


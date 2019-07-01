// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections.Generic;
using System.Windows.Threading;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    public class DispatcherTimerFactory : DiscoverableFactory<DispatcherTimer>
    {
        #region Public Members

        public int DispatcherTimerConstructorType { get; set; }

        public Dispatcher Dispatcher { get; set; }

        public int PriorityValue { get; set; }

        public TimeSpan TimeSpan { get; set; }

        public int ExecuteTimes { get; set; }

        #endregion

        #region Public Override Members

        public override DispatcherTimer Create(DeterministicRandom random)
        {
            //DispatcherTimer timer = null;
            DispatcherPriority priority = ChooseValidPriority(PriorityValue);
            switch (DispatcherTimerConstructorType % 4)
            {
                case 0:
                    timer = new DispatcherTimer();
                    timer.Tick += new EventHandler(DispatcherTimerTick);
                    timer.Interval = TimeSpan;
                    break;
                case 1:
                    timer = new DispatcherTimer(priority);
                    timer.Tick += new EventHandler(DispatcherTimerTick);
                    timer.Interval = TimeSpan;
                    break;
                case 2:
                    timer = new DispatcherTimer(priority, Dispatcher);
                    timer.Tick += new EventHandler(DispatcherTimerTick);
                    timer.Interval = TimeSpan;
                    break;
                case 3:
                    timer = new DispatcherTimer(TimeSpan, priority, new EventHandler(DispatcherTimerTick), Dispatcher);
                    break;
            }

            //Save the DispatcherTimer, Some combinations of actions need do on the same DispatcherTimer.
            lock (DispatcherTimers)
            {
                DispatcherTimers.Add(timer);
                if (DispatcherTimers.Count > 10)
                {
                    int removeIndex = random.Next(DispatcherTimers.Count);
                    DispatcherTimer removeTimer = DispatcherTimers[removeIndex];
                    //Stop the DispatcherTimer before remove it.
                    removeTimer.Stop();
                    DispatcherTimers.Remove(removeTimer);
                }

                return DispatcherTimers[random.Next(DispatcherTimers.Count)];
            }
        }

        #endregion

        #region Private Members

        private DispatcherPriority ChooseValidPriority(int priorityValue)
        {
            DispatcherPriority[] validPriorities = { DispatcherPriority.SystemIdle, DispatcherPriority.ApplicationIdle, DispatcherPriority.ContextIdle, DispatcherPriority.Background, DispatcherPriority.Input, DispatcherPriority.Loaded, DispatcherPriority.Render, DispatcherPriority.DataBind, DispatcherPriority.Normal, DispatcherPriority.Send };

            return validPriorities[priorityValue % validPriorities.Length];
        }

        private void DispatcherTimerTick(object sender, EventArgs e)
        {
            ExecuteTimes %= 3;
            if (currentTimes++ > ExecuteTimes)
            {
                timer.Stop();
            }
        }

        #endregion

        #region Private Data

        private DispatcherTimer timer = null;
        private int currentTimes = 0;
        private static List<DispatcherTimer> DispatcherTimers = new List<DispatcherTimer>();

        #endregion
    }
}

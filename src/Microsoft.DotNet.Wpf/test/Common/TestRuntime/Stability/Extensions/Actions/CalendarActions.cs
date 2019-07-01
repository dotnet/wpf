// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Security.Wrappers;
using System.Reflection;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40   
    [TargetTypeAttribute(typeof(Calendar))]
    abstract class SelectionCalendarAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Window { get; set; }

        public Calendar Calendar { get; set; }

        /// <summary>
        /// Used to select different days on the Calendar
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int NumLeftKeyPresses { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int NumNextButtonClicks { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int NumPrevButtonClicks { get; set; }

        protected void ClickOnNextButton(int numClicks)
        {
            TypeSW type = TypeSW.Wrap(typeof(Calendar));

            for (int i = 0; i < numClicks; i++)
            {
                type.InvokeMember("OnNextClick", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, Calendar, null);
            }
        }

        protected void ClickOnPreviousButton(int numClicks)
        {
            TypeSW type = TypeSW.Wrap(typeof(Calendar));

            for (int i = 0; i < numClicks; i++)
            {
                type.InvokeMember("OnPreviousClick", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, Calendar, null);
            }
        }
    }

    [TargetTypeAttribute(typeof(Calendar))]
    class SelectCalendarItemAction : SelectionCalendarAction
    {
        public override void Perform()
        {
            Window.Content = Calendar;

            ClickOnNextButton(NumNextButtonClicks);
            ClickOnPreviousButton(NumPrevButtonClicks);

            // press right arrow key <x> times to select a random day on the Calendar
            for (int i = 0; i < NumLeftKeyPresses; i++)
            {
                HomelessTestHelpers.KeyPress(Key.Right);
            }
        }
    }
#endif
}

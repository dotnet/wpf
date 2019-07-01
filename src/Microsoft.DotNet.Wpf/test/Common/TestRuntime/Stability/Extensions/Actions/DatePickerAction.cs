// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40   
    [TargetTypeAttribute(typeof(DatePicker))]
    public class SelectDateFromCalendarAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Window { get; set; }

        public DatePicker DatePicker { get; set; }

        /// <summary>
        /// Used to select different days on the Calendar
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int NumLeftKeyPresses { get; set; }

        public override void Perform()
        {
            Window.Content = DatePicker;

            // open the drop down
            DatePicker.IsDropDownOpen = true;

            // press right arrow key <x> times to select a random date on the Calendar
            for (int i = 0; i < NumLeftKeyPresses; i++)
            {
                HomelessTestHelpers.KeyPress(Key.Right);
            }

            // close the dropdown
            DatePicker.IsDropDownOpen = false;
        }
    }

    [TargetTypeAttribute(typeof(DatePicker))]
    public class TypeInDatePositiveAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Window { get; set; }

        public DatePicker DatePicker { get; set; }

        /// <summary>
        /// Used to select different days on the Calendar
        /// </summary>        
        public ConstrainedDateTime DateToInput { get; set; }

        public override void Perform()
        {
            Window.Content = DatePicker;

            DatePicker.Text = ((DateTime)DateToInput.GetData(null)).ToShortDateString();            
        }
    }

    [TargetTypeAttribute(typeof(DatePicker))]
    public class TypeInDateNegativeAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Window { get; set; }

        public DatePicker DatePicker { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]        
        public string StringToInput { get; set; }

        public override void Perform()
        {
            Window.Content = DatePicker;

            DatePicker.Text = StringToInput;
        }
    }
#endif
}

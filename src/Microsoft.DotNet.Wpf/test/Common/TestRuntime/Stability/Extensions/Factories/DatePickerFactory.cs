// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    [TargetTypeAttribute(typeof(DatePicker))]
    class DatePickerFactory : DiscoverableFactory<DatePicker>
    {
        public ConstrainedDateTime DisplayDate { get; set; }
        public ConstrainedDateTime DisplayDateEnd { get; set; }
        public ConstrainedDateTime DisplayDateStart { get; set; }
        public ConstrainedDateTime SelectedDate { get; set; }
        public bool IsTodayHighlighted { get; set; }
        public DayOfWeek FirstDayOfWeek { get; set; }
        public DatePickerFormat SelectedDateFormat { get; set; }

        public override DatePicker Create(DeterministicRandom random)
        {
            DatePicker datePicker = new DatePicker();

            if (DisplayDate != null)
            {
                datePicker.DisplayDate = (DateTime)DisplayDate.GetData(random);
            }

            if (DisplayDateEnd != null)
            {
                datePicker.DisplayDateEnd = (DateTime)DisplayDateEnd.GetData(random);
            }

            if (DisplayDateStart != null)
            {
                datePicker.DisplayDateStart = (DateTime)DisplayDateStart.GetData(random);
            }

            datePicker.FirstDayOfWeek = FirstDayOfWeek;
            datePicker.IsTodayHighlighted = IsTodayHighlighted;

            if (SelectedDate != null)
            {
                datePicker.SelectedDate = (DateTime)SelectedDate.GetData(random);
            }

            datePicker.SelectedDateFormat = SelectedDateFormat;
            return datePicker;
        }
    }   
#endif
}

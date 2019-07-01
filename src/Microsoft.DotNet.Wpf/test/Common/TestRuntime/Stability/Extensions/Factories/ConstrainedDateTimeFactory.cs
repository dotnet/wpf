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
    [TargetTypeAttribute(typeof(ConstrainedDateTime))]
    class ConstrainedDateTimeFactory : DiscoverableFactory<ConstrainedDateTime>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int Year { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int Month { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int Day { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int Hour { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int Minute { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int Second { get; set; }

        public override ConstrainedDateTime Create(DeterministicRandom random)
        {
            ConstrainedDateTime dateTime = new ConstrainedDateTime();
            dateTime.Year = Year;
            dateTime.Month = Month;

            // need to handle case for days that do not exist in particular months
            int daysInMonth = DateTime.DaysInMonth(Year, Month);
            if (Day > daysInMonth)
            {
                Day = daysInMonth;
            }
            dateTime.Day = Day;

            dateTime.Hour = Hour;
            dateTime.Minute = Minute;
            dateTime.Second = Second;

            

            return dateTime;
        }
    }
  
}

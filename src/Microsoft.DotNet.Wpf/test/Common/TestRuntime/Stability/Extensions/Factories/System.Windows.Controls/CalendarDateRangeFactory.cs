// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    /// <summary>
    /// A factory which create CalendarDateRange.
    /// </summary>
    internal class CalendarDateRangeFactory : DiscoverableFactory<CalendarDateRange>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a ConstrainedDateTime to set CalendarDateRange Start property. 
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ConstrainedDateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets a ConstrainedDateTime to set CalendarDateRange End property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ConstrainedDateTime EndTime { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a CalendarDateRange.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override CalendarDateRange Create(DeterministicRandom random)
        {
            CalendarDateRange calendarDateRange;

            DateTime startTime = (DateTime)StartTime.GetData(random);
            DateTime endTime = (DateTime)EndTime.GetData(random);
            bool useDefaultConstructor = random.NextBool();
            if (useDefaultConstructor)
            {
                calendarDateRange = new CalendarDateRange();
                calendarDateRange.Start = startTime;
                calendarDateRange.End = endTime;
            }
            else
            {
                calendarDateRange = new CalendarDateRange(startTime, endTime);
            }

            return calendarDateRange;
        }

        #endregion
    }
#endif
}

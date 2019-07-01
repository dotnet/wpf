// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    /// <summary>
    /// A factory which create CalendarButton.
    /// </summary>
    internal class CalendarButtonFactory : AbstractButtonFactory<CalendarButton>
    {
        /// <summary>
        /// Create a CalendarButton.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override CalendarButton Create(DeterministicRandom random)
        {
            CalendarButton calendarButton = new CalendarButton();

            ApplyButtonProperties(calendarButton, random);

            return calendarButton;
        }
    }
#endif
}

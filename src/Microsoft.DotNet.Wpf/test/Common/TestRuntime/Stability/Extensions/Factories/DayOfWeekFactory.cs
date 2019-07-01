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
    class DayOfWeekFactory : DiscoverableFactory<DayOfWeek>
    {
        public override DayOfWeek Create(DeterministicRandom random)
        {
            return random.NextEnum<DayOfWeek>();
        }
    }
#endif
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedDateTime : ConstrainedDataSource
    {
        public ConstrainedDateTime() 
        { 
        }

        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Second { get; set; }

        public override object GetData(DeterministicRandom r)
        {
            return new DateTime(Year, Month, Day, Hour, Minute, Second);
        }
    }
}

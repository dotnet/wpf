// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Input;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create StylusPointPropertyInfo.
    /// </summary>
    internal class StylusPointPropertyInfoFactory : DiscoverableFactory<StylusPointPropertyInfo>
    {
        /// <summary>
        /// Create a StylusPointPropertyInfo.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override StylusPointPropertyInfo Create(DeterministicRandom random)
        {
            StylusPointProperty stylusPointProperty = random.NextStaticField<StylusPointProperty>(typeof(StylusPointProperties));
            int minimum = random.Next(10000);
            int maximum = random.Next(10000) + minimum;
            StylusPointPropertyUnit unit = random.NextEnum<StylusPointPropertyUnit>();
            float resolution = random.Next(100) / 10f;

            StylusPointPropertyInfo stylusPointPropertyInfo = new StylusPointPropertyInfo(stylusPointProperty, minimum, maximum, unit, resolution);
            return stylusPointPropertyInfo;
        }
    }
}

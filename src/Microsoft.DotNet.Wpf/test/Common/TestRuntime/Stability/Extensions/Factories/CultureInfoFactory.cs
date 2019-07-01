// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class CultureInfoFactory : DiscoverableFactory<CultureInfo>
    {
        public override CultureInfo Create(DeterministicRandom random)
        {
            List<CultureInfo> cultures = new List<CultureInfo>();
            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                cultures.Add(ci);
            }
            return random.NextItem<CultureInfo>(cultures);
        }
    }
}

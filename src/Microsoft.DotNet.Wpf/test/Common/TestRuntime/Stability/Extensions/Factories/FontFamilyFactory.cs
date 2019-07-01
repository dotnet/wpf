// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class FontFamilyFactory : DiscoverableFactory<FontFamily>
    {
        public override FontFamily Create(DeterministicRandom random)
        {
            List<FontFamily> fontsFamilies = new List<FontFamily>();
            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
            {
                fontsFamilies.Add(fontFamily);
            }
            return random.NextItem<FontFamily>(fontsFamilies);
        }
    }
}

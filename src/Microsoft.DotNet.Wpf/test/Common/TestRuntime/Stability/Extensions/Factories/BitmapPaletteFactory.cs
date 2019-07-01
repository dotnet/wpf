// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class BitmapPaletteFactory : DiscoverableFactory<BitmapPalette>
    {
        public List<Color> Colors { get; set; }

        public override BitmapPalette Create(DeterministicRandom random)
        {
            if(Colors == null) { Colors = new List<Color>(); }
            
            if (random.NextBool())
            {
                for (int i = 1; i <= random.Next(256) + 1; i++)
                {
                    Colors.Add(Color.FromScRgb(
                            (float)random.NextDouble(),
                            (float)random.NextDouble(),
                            (float)random.NextDouble(),
                            (float)random.NextDouble()));
                }
            }
            else
            {
                for (int i = 1; i <= random.Next(256) + 1; i++)
                {
                    Colors.Add(random.NextStaticProperty<Colors, Color>());
                } 
            }

            return new BitmapPalette(Colors);
        }
    }

    class StaticBitmapPaletteFactory : DiscoverableFactory<BitmapPalette>
    {
        public override BitmapPalette Create(DeterministicRandom random)
        {
            return random.NextStaticProperty<BitmapPalette>(typeof(BitmapPalettes));                       
        }
    }
}

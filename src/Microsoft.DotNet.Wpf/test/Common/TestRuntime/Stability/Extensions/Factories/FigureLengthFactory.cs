// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class FigureLengthFactory: DiscoverableFactory<FigureLength>
    {
        public override FigureLength Create(DeterministicRandom random)
        {
            double randomDouble = random.NextDouble();
            switch (random.NextEnum<FigureUnitType>())
            {
                case FigureUnitType.Auto:
                    return new FigureLength();
                case FigureUnitType.Pixel:
                    return new FigureLength(randomDouble * 300, FigureUnitType.Pixel);
                case FigureUnitType.Column:
                    return new FigureLength(randomDouble * 3, FigureUnitType.Column);
                case FigureUnitType.Content:
                    return new FigureLength(randomDouble, FigureUnitType.Content);
                case FigureUnitType.Page:
                    return new FigureLength(randomDouble, FigureUnitType.Page);
                default:
                    goto case 0;
            }
        }
    }
}

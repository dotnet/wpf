// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class PixelFormatFactory : DiscoverableFactory<PixelFormat>
    {
        public override PixelFormat Create(DeterministicRandom random)
        {
            // filter out the PixelFormats.Default format,
            // as it is invalid for consumers of this factory
            PixelFormat format;
            do
            {
                format = random.NextStaticProperty<PixelFormat>(typeof(PixelFormats));
            } while (format == PixelFormats.Default);

            return format;
        }
    }
}

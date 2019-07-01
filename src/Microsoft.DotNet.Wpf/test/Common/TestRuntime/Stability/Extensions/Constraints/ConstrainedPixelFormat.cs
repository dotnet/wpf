// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using System.Collections;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class PixelFormatsList : List<PixelFormat> {}
    public class ConstrainedPixelFormat : ConstrainedDataSource
    {
        public ConstrainedPixelFormat() { }

        public PixelFormatsList ExcludedFormats { get; set; }

        public override object GetData(DeterministicRandom r)
        {
            PixelFormat format;
            do
            {
                format = r.NextStaticProperty<PixelFormat>(typeof(PixelFormats));
            } while (ExcludedFormats.Contains(format));

            return format;
        }
    }
}

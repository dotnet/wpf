// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class RenderOptionsAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromVisualTree)]
        public DependencyObject Target { get; set; }

        public BitmapScalingMode BitmapScalingMode { get; set; }
        public EdgeMode EdgeMode { get; set; }
#if TESTBUILD_CLR40
        public ClearTypeHint ClearTypeHint { get; set; }
#endif
        public override void Perform()
        {
            RenderOptions.SetBitmapScalingMode(Target, BitmapScalingMode);
            RenderOptions.SetEdgeMode(Target, EdgeMode);
#if TESTBUILD_CLR40
            RenderOptions.SetClearTypeHint(Target, ClearTypeHint);
#endif
        }
    }
}

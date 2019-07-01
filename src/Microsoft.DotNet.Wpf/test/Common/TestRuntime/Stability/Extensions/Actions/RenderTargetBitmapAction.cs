// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class RenderTargetBitmapAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Control Target { get; set; }

        public override void Perform()
        {
            //The Target must be of actual size in order to render.
            if ((int)Target.ActualWidth > 0 && (int)Target.ActualHeight > 0)
            {
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)Target.ActualWidth % 5000, (int)Target.ActualHeight % 5000, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(Target);
            }
        }
    }
}

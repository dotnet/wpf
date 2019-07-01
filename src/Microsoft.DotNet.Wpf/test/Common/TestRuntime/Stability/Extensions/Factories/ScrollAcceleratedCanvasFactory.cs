// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ScrollAcceleratedCanvasFactory : PanelFactory<ScrollAcceleratedCanvas>
    {
        public override ScrollAcceleratedCanvas Create(DeterministicRandom random)
        {
            ScrollAcceleratedCanvas canvas = new ScrollAcceleratedCanvas();
            ApplyCommonProperties(canvas, random);
            return canvas;
        }
    }

    #region ScrollAcceleratedCanvas
    /// <summary>
    /// Implements a canvas where the scroll-acceleration feature can be
    /// turned on or off by setting its VisualScrollableAreaClip.
    /// </summary>
    public class ScrollAcceleratedCanvas : Canvas
    {
        #region Constructor

        public ScrollAcceleratedCanvas()
        {
#if TESTBUILD_CLR40
            // No scrollable area is provided, so disable Scroll Acceleration by default.
            SetVisualScrollableAreaClip(null);
#endif
        }

        #endregion

        #region Methods

        public void SetVisualScrollableAreaClip(Rect? scrollableArea)
        {
#if TESTBUILD_CLR40
            VisualScrollableAreaClip = scrollableArea;
#endif
            }

        #endregion
    }
    #endregion
}

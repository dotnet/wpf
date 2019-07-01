// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

#if !STANDALONE_BUILD
using TrustedHwndSource = Microsoft.Test.Security.Wrappers.HwndSourceSW;
#else
using TrustedHwndSource = System.Windows.Interop.HwndSource;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// A window with any Visual as its content
    /// </summary>
    public class VisualWindow : RenderingWindow
    {
        /// <summary/>
        public VisualWindow(Variation v)
            : base(v)
        {
        }

        /// <summary/>
        public override void Dispose()
        {
            RenderingTest.Invoke(DispatcherPriority.Normal, Dispose, null);
        }

        private object Dispose(object o)
        {
            windowSource.Dispose();
            windowSource = null;
            return null;
        }

        /// <summary/>
        public override IntPtr Handle { get { return windowHandle; } }

        /// <summary/>
        public override object SetContentDelegate(object test)
        {
            RenderingTest renderingTest = (RenderingTest)test;
            Visual v = renderingTest.GetWindowContent();

            if (RendersToImage)
            {
                ImageBrush brush = RenderToImage(v);
                Rectangle r = new Rectangle(new Rect(0, 0, WindowWidth, WindowHeight), brush);
                rootVisual.Visual = r.Visual;
            }
            else
            {
                rootVisual.Visual = v;
            }
            return null;
        }

        /// <summary/>
        protected override object SetBackgroundDelegate(object color)
        {
            rootVisual.BackgroundColor = (Color)color;
            return null;
        }

        /// <summary/>
        public override object ModifyContentDelegate(object test)
        {
            RenderingTest renderingTest = (RenderingTest)test;
            renderingTest.ModifyWindowContent(rootVisual.Visual);
            return null;
        }

        /// <summary/>
        protected override object CreateWindow(object o)
        {
            // HwndSource windows do not automatically adjust for DPI.
            // Change the values parsed to reflect what the Avalon window class would do for us automatically.

            int w = (int)MathEx.ConvertToAbsolutePixelsX(WindowWidth);
            int h = (int)MathEx.ConvertToAbsolutePixelsY(WindowHeight);
            HwndSourceParameters parameters = new HwndSourceParameters("Rendering Window", w, h);
            parameters.WindowStyle = unchecked((int)0x90000000);
            parameters.ExtendedWindowStyle = unchecked((int)0x8);

            if (IsWindowPositionValid)
            {
                Point p = MathEx.ConvertToAbsolutePixels(WindowPosition);
                parameters.SetPosition((int)p.X, (int)p.Y);
            }

            windowSource = new TrustedHwndSource(parameters);

            rootVisual = new VisualWrapper(WindowSize);
            windowSource.RootVisual = rootVisual;
            windowHandle = windowSource.Handle;

            return null;
        }

        /// <summary/>
        protected override HwndTarget HwndTarget
        {
            get
            {
                return PT.Untrust(windowSource).CompositionTarget;
            }
        }

        private TrustedHwndSource windowSource;
        /// <summary/>
        protected VisualWrapper rootVisual;
        private IntPtr windowHandle;
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using BindingFlags = System.Reflection.BindingFlags;

#if !STANDALONE_BUILD
using TrustedAssembly = Microsoft.Test.Security.Wrappers.AssemblySW;
using TrustedType = Microsoft.Test.Security.Wrappers.TypeSW;
#else
using TrustedAssembly = System.Reflection.Assembly;
using TrustedType = System.Type;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// A container for the window that renders our visuals
    /// </summary>
    public abstract class RenderingWindow : IDisposable
    {
        /// <summary/>
        protected RenderingWindow(Variation v)
        {
            variation = v;
            photographer = new Photographer();

            RenderingTest.Invoke(DispatcherPriority.Normal, CreateWindow, null);

            // If the window position is not valid, that means the system placed it somewhere on the primary display.
            // (0,0) will get us the primary display.
            Point windowPosition = v.IsWindowPositionValid ? v.WindowPosition : new Point(0, 0);
            ColorOperations.DiscoverBitDepth(windowPosition);

            if (v.RenderingMode != null)
            {
                RenderingTest.Invoke(DispatcherPriority.Normal, SetRenderingMode, v.RenderingMode);
            }
        }

        /// <summary/>
        public static RenderingWindow Create(Variation v)
        {
            if (v.UseViewport3D)
            {
                return new ViewportWindow(v);
            }

            return new VisualWindow(v);
        }

        /// <summary/>
        public abstract void Dispose();

        /// <summary/>
        public void SetBackgroundColor(Color c)
        {
            RenderingTest.Invoke(DispatcherPriority.Normal, SetBackgroundDelegate, c);
        }

        /// <summary/>
        public abstract IntPtr Handle { get; }

        /// <summary/>
        protected abstract HwndTarget HwndTarget { get; }

        /// <summary/>
        protected abstract object CreateWindow(object o);
        /// <summary/>
        protected abstract object SetBackgroundDelegate(object color);
        /// <summary/>
        public abstract object SetContentDelegate(object test);
        /// <summary/>
        public abstract object ModifyContentDelegate(object test);

        /// <summary/>
        public object CaptureContentDelegate(object notUsed)
        {
            System.Drawing.Bitmap bitmap = photographer.TakeScreenCapture(Handle);
            return PhotoConverter.ToColorArray(bitmap);
        }

        /// <summary>
        /// Set to "Hardware", "Software", or "HardwareReference" depending on mode
        /// </summary>        
        protected object SetRenderingMode(object mode)
        {
            RenderingModeHelper.SetRenderingMode(HwndTarget, (string)mode);
            return null;
        }

        /// <summary/>
        protected ImageBrush RenderToImage(Visual v)
        {
            // Create image data
            RenderTargetBitmap bitmap = new RenderTargetBitmap(WindowWidth, WindowHeight, 96, 96, PixelFormats.Pbgra32);

            // render to image
            bitmap.Render(v);

            // create ImageBrush
            return new ImageBrush(bitmap);
        }

        internal static Color DefaultBackgroundColor
        {
            get
            {
                return defaultBackgroundColor;
            }
        }

        /// <summary/>
        protected int WindowWidth { get { return variation.WindowWidth; } }

        /// <summary/>
        protected int WindowHeight { get { return variation.WindowHeight; } }

        /// <summary/>
        protected Size WindowSize { get { return variation.WindowSize; } }

        /// <summary/>
        protected Point WindowPosition { get { return variation.WindowPosition; } }

        /// <summary/>
        protected bool RendersToImage { get { return variation.RenderToImage; } }

        /// <summary/>
        protected bool IsWindowPositionValid { get { return variation.IsWindowPositionValid; } }

        /// <summary/>
        protected FlowDirection FlowDirection { get { return variation.FlowDirection; } }

        private Photographer photographer;
        private Variation variation;
        private static Color defaultBackgroundColor = Colors.White;
    }
}

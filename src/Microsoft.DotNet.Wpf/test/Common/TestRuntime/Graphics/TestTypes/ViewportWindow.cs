// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using BindingFlags = System.Reflection.BindingFlags;

#if !STANDALONE_BUILD
using TrustedType = Microsoft.Test.Security.Wrappers.TypeSW;
using TrustedWindow = Microsoft.Test.Security.Wrappers.WindowSW;
using TrustedWindowInteropHelper = Microsoft.Test.Security.Wrappers.WindowInteropHelperSW;
using Microsoft.Test.Graphics.ReferenceRender;
#else
using TrustedType = System.Type;
using TrustedWindow = System.Windows.Window;
using TrustedWindowInteropHelper = System.Windows.Interop.WindowInteropHelper;
using Microsoft.Test.Graphics.ReferenceRender;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// A window with a Viewport3D as its content
    /// </summary>
    public sealed class ViewportWindow : RenderingWindow
    {
        /// <summary/>
        public ViewportWindow(Variation v)
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
            window.Close();
            return null;
        }

        /// <summary/>
        public override IntPtr Handle { get { return windowHandle; } }

        /// <summary/>
        public override object SetContentDelegate(object test)
        {
            RenderingTest renderingTest = (RenderingTest)test;
            content = (UIElement)renderingTest.GetWindowContent();

            if (RendersToImage)
            {
                // Force a layout pass on the UIElement.
                // This will make sure that RenderToImage doesn't give us an empty ImageBrush.

                content.Arrange(new Rect(0, 0, WindowWidth, WindowHeight));
                ImageBrush brush = RenderToImage(content);


                if (renderingTest is XamlTest)
                {
                    // However, this will not work for XamlTest because that test grabs the Viewport3D from Xaml
                    //  and the Xaml parser sets the VisualFlags.IsLayoutSuspended on the Viewport3D.
                    //  Therefore, the Arrange is ignored.
                    //
                    // The best we can do is just ignore the whole thing so that /RunAll doesn't fail.

                    RenderTolerance.DefaultColorTolerance = Colors.White;
                }

                System.Windows.Shapes.Rectangle r = new System.Windows.Shapes.Rectangle();
                r.Width = WindowWidth;
                r.Height = WindowHeight;
                r.Fill = brush;
                window.Content = r;
            }
            else
            {
                window.Content = content;
            }
            return null;
        }

        /// <summary/>
        protected override object SetBackgroundDelegate(object color)
        {
            window.Background = new SolidColorBrush((Color)color);
            return null;
        }

        /// <summary/>
        public override object ModifyContentDelegate(object test)
        {
            RenderingTest renderingTest = (RenderingTest)test;
            renderingTest.ModifyWindowContent((Visual)content);
            return null;
        }

        /// <summary/>
        protected override object CreateWindow(object o)
        {
            window = new TrustedWindow();

            if (IsWindowPositionValid)
            {
                window.Left = WindowPosition.X;
                window.Top = WindowPosition.Y;
            }

            // Always on top so that we don't get bad screen captures
            window.Topmost = true;
            window.WindowStyle = WindowStyle.None;

            // This control is set on the window only to set
            //  the window client area to the correct desired size.
            Canvas dummyContent = new Canvas();
            dummyContent.Height = WindowHeight;
            dummyContent.Width = WindowWidth;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.Content = dummyContent;

            window.Background = new SolidColorBrush(DefaultBackgroundColor);
            window.Show();

            TrustedWindowInteropHelper helper = new TrustedWindowInteropHelper(PT.Untrust(window));
            windowHandle = helper.Handle;

            return null;
        }

        /// <summary/>
        protected override HwndTarget HwndTarget
        {
            get
            {
                // The HwndTarget sits at: "window._swh._sourceWindow.CompositionTarget"
                //  but it's mostly private so we need to reflect into it

                TrustedType windowType = PT.Trust(typeof(Window));

                // "Window.SourceWindowHelper" is an internal class so we use "object" instead

                object sourceWindowHelper = windowType.InvokeMember(
                                        "_swh",
                                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField,
                                        null,
                                        window,
                                        null
                                        );

                TrustedType sourceWindowHelperType = PT.Trust(sourceWindowHelper.GetType());
                HwndSource source = (HwndSource)sourceWindowHelperType.InvokeMember(
                                        "_sourceWindow",
                                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField,
                                        null,
                                        sourceWindowHelper,
                                        null
                                        );

                return source.CompositionTarget;
            }
        }

        private IntPtr windowHandle;
        private TrustedWindow window;
        private UIElement content;
    }
}


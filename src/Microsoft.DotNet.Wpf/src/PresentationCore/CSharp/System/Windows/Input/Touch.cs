// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Windows;
using System.Windows.Input;

namespace System.Windows.Input
{
    public static class Touch
    {
        internal static readonly RoutedEvent PreviewTouchDownEvent = EventManager.RegisterRoutedEvent("PreviewTouchDown", RoutingStrategy.Tunnel, typeof(EventHandler<TouchEventArgs>), typeof(Touch));
        internal static readonly RoutedEvent TouchDownEvent = EventManager.RegisterRoutedEvent("TouchDown", RoutingStrategy.Bubble, typeof(EventHandler<TouchEventArgs>), typeof(Touch));
        
        internal static readonly RoutedEvent PreviewTouchMoveEvent = EventManager.RegisterRoutedEvent("PreviewTouchMove", RoutingStrategy.Tunnel, typeof(EventHandler<TouchEventArgs>), typeof(Touch));
        internal static readonly RoutedEvent TouchMoveEvent = EventManager.RegisterRoutedEvent("TouchMove", RoutingStrategy.Bubble, typeof(EventHandler<TouchEventArgs>), typeof(Touch));
        
        internal static readonly RoutedEvent PreviewTouchUpEvent = EventManager.RegisterRoutedEvent("PreviewTouchUp", RoutingStrategy.Tunnel, typeof(EventHandler<TouchEventArgs>), typeof(Touch));
        internal static readonly RoutedEvent TouchUpEvent = EventManager.RegisterRoutedEvent("TouchUp", RoutingStrategy.Bubble, typeof(EventHandler<TouchEventArgs>), typeof(Touch));

        internal static readonly RoutedEvent GotTouchCaptureEvent = EventManager.RegisterRoutedEvent("GotTouchCapture", RoutingStrategy.Bubble, typeof(EventHandler<TouchEventArgs>), typeof(Touch));
        internal static readonly RoutedEvent LostTouchCaptureEvent = EventManager.RegisterRoutedEvent("LostTouchCapture", RoutingStrategy.Bubble, typeof(EventHandler<TouchEventArgs>), typeof(Touch));

        internal static readonly RoutedEvent TouchEnterEvent = EventManager.RegisterRoutedEvent("TouchEnter", RoutingStrategy.Direct, typeof(EventHandler<TouchEventArgs>), typeof(Touch));
        internal static readonly RoutedEvent TouchLeaveEvent = EventManager.RegisterRoutedEvent("TouchLeave", RoutingStrategy.Direct, typeof(EventHandler<TouchEventArgs>), typeof(Touch));

        /// <summary>
        ///     Raised when there is an update to the list of touch devices.
        /// </summary>
        /// <remarks>
        ///     This API is provided for compatibility with Silverlight, but due to different
        ///     device implementations, this event will be called in WPF whenever any change
        ///     occurs to any touch device instead of on a frame basis.
        /// </remarks>
        public static event TouchFrameEventHandler FrameReported;

        internal static void ReportFrame()
        {
            if (FrameReported != null)
            {
                TouchFrameEventArgs args = new TouchFrameEventArgs(Environment.TickCount);
                FrameReported(null, args);
            }
        }
    }
}

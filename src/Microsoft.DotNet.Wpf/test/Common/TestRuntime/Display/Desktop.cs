// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using Microsoft.Test.Diagnostics;

namespace Microsoft.Test.Display
{
    /// <summary>
    /// Methods used in the Test Runtime that have a dependency on WPF
    /// </summary>
	public static class Desktop
	{
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Size GetVirtualScreenSize()
        {
            int width = SystemMetrics.GetSystemMetric(SystemMetric.VirtualScreenWidth);
            int height = SystemMetrics.GetSystemMetric(SystemMetric.VirtualScreenHeight);

            Size size = new Size(width, height);

            return size;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Point GetVirtualScreenTopLeftPoint()
        {
            int x = SystemMetrics.GetSystemMetric(SystemMetric.VirtualScreenX);
            int y = SystemMetrics.GetSystemMetric(SystemMetric.VirtualScreenY);

            Point point = new Point(x, y);
            return point;
        }
	}
}

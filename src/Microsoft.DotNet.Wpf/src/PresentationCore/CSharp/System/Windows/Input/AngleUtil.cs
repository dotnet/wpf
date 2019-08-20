// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Input 
{
    internal static class AngleUtil
    {
        /// <summary>
        ///     Method to convert from degrees to radians
        /// </summary>
        public static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        ///     Method to convert from radians to degrees
        /// </summary>
        public static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }
    }
}


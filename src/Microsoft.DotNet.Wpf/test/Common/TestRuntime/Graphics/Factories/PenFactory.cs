// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// This summary has not been prepared yet. NOSUMMARY - pantal07
    /// </summary>
    public class PenFactory
    {
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Pen MakePen(string pen)
        {
            string[] parsedPen = pen.Split(' ');

            switch (parsedPen[0])
            {
                case "SolidBlack5":
                    return new Pen(Brushes.Black, 5.0);

                // TO DO:  Create for general pen objects.

                default:
                    throw new ArgumentException("Specified pen (" + pen + ") cannot be created");

            }
        }
    }
}
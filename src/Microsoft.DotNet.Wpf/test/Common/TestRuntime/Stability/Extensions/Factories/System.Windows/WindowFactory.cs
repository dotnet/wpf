// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class WindowFactory : DiscoverableFactory<Window>
    {
        #region Public Members

        public Brush Brush { get; set; }

        #endregion

        #region Override Members

        public override bool CanCreate(Type desiredType)
        {
            return desiredType == typeof(Window);
        }

        /// <summary>
        /// Creates a new Window with random size, position, style, and background.
        /// Currently only used to create an Owner window for the window we are started with.
        /// </summary>
        public override Window Create(DeterministicRandom random)
        {
            Window window = new Window();

            window.Top = random.Next((int)(SystemParameters.PrimaryScreenHeight * 0.8));
            window.Left = random.Next((int)(SystemParameters.PrimaryScreenWidth * 0.8));
            window.Height = random.Next((int)(SystemParameters.PrimaryScreenHeight * 0.4));
            window.Width = random.Next((int)(SystemParameters.PrimaryScreenWidth * 0.4));
            window.WindowStyle = random.NextEnum<WindowStyle>();
            window.Background = Brush;
            window.Show();

            return window;
        }

        #endregion
    }
}

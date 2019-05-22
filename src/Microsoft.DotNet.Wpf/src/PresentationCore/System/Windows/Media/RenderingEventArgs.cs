// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;

namespace System.Windows.Media
{
    /// <summary>
    /// The RenderingEventArgs class is passed as the argument into the CompositionTarget.Rendering 
    /// event.  It provides the estimated next render time.  
    /// </summary>
    public class RenderingEventArgs : EventArgs
    {
        /// <summary>
        /// Internal constructor
        /// </summary>
        /// <param name="renderingTime"></param>
        internal RenderingEventArgs(TimeSpan renderingTime)
        {
            _renderingTime = renderingTime;
        }

        /// <summary>
        /// Returns the time at which we expect to render the next frame
        /// to the screen.  This is the same time used by the TimeManager.
        /// </summary>
        public TimeSpan RenderingTime
        {
            get
            {
                return _renderingTime;
            }
        }

        private TimeSpan _renderingTime; 
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This file defines a class intended to be passed as a parameter to Measure.  It contains
//  available size and viewport information.
//

using MS.Internal;
using System;
using System.Windows.Media;

namespace System.Windows
{
    /// <summary>
    /// Provides all the data we need during the Measure pass (most notably viewport information).  Because of backwards
    /// compat we can't pass it in as a parameter to Measure so it's set as a property on UIElement directly before the call
    /// instead.
    /// </summary>
    internal class MeasureData
    {
        public MeasureData(Size availableSize, Rect viewport)
        {
            _availableSize = availableSize;
            _viewport = viewport;
        }

        public MeasureData(MeasureData data) : this (data.AvailableSize, data.Viewport)
        {
        }

        public bool HasViewport
        {
            get
            {
                return Viewport != Rect.Empty;
            }
        }


        public bool IsCloseTo(MeasureData other)
        {
            if (other == null)
            {
                return false;
            }

            bool isClose = DoubleUtil.AreClose(AvailableSize, other.AvailableSize);
            isClose &= DoubleUtil.AreClose(Viewport, other.Viewport);
            
            return isClose;
        }


        public Size AvailableSize
        {
            get
            {
                return _availableSize;
            }

            set
            {
                _availableSize = value;
            }
        }

        public Rect Viewport
        {
            get
            {
                return _viewport;
            }

            set
            {
                _viewport = value;
            }
        }

        private Size _availableSize;
        private Rect _viewport;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows
{
    /// <summary>
    ///  The SizeChangedEventArgs class is used by SizeChangedEventHandler.
    ///  This handler is used for ComputedWidthChanged and ComputedHeightChanged events 
    ///  on UIElement. 
    /// </summary>
    public class SizeChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the SizeChangedEventArgs class.
        /// </summary>
        /// <param name="element">
        ///     The UIElement which has its size changed by layout engine/>.
        /// </param>
        /// <param name="info">
        ///     The SizeChangeInfo that is used by <seealso cref="UIElement.OnRenderSizeChanged"/>.
        /// </param>
        internal SizeChangedEventArgs(UIElement element, SizeChangedInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            _element = element;
            _previousSize = info.PreviousSize;
            if(info.WidthChanged) _bits |= _widthChangedBit;
            if(info.HeightChanged) _bits |= _heightChangedBit;
        }

        /// <summary>
        /// Read-only access to the previous Size
        /// </summary>
        public Size PreviousSize
        {
            get { return _previousSize; }
        }

        /// <summary>
        /// Read-only access to the new Size
        /// </summary>
        public Size NewSize
        {
            get { return _element.RenderSize; }
        }

        /// <summary>
        /// Read-only access to the flag indicating that Width component of the size changed.
        /// Note that due to double math 
        /// effects, the it may be (previousSize.Width != newSize.Width) and widthChanged = true.
        /// This may happen in layout when sizes of objects are fluctuating because of a precision "jitter" of
        /// the input parameters, but the overall scene is considered to be "the same" so no visible changes 
        /// will be detected. Typically, the handler of SizeChangedEvent should check this bit to avoid 
        /// invalidation of layout if the dimension didn't change.
        /// </summary>
        public bool WidthChanged
        {
            get { return ((_bits & _widthChangedBit) != 0); }
        }

        /// <summary>
        /// Read-only access to the flag indicating that Height component of the size changed.
        /// Note that due to double math 
        /// effects, the it may be (previousSize.Height != newSize.Height) and heightChanged = true.
        /// This may happen in layout when sizes of objects are fluctuating because of a precision "jitter" of
        /// the input parameters, but the overall scene is considered to be "the same" so no visible changes 
        /// will be detected. Typically, the handler of SizeChangedEvent should check this bit to avoid 
        /// invalidation of layout if the dimension didn't change.
        /// </summary>
        public bool HeightChanged
        {
            get { return ((_bits & _heightChangedBit) != 0); }
        }

        private Size _previousSize;
        private UIElement _element;
        private byte _bits;
        
        private static byte _widthChangedBit = 0x1;
        private static byte _heightChangedBit = 0x2;

        /// <summary>
        ///     The mechanism used to call the type-specific handler on the
        ///     target.
        /// </summary>
        /// <param name="genericHandler">
        ///     The generic handler to call in a type-specific way.
        /// </param>
        /// <param name="genericTarget">
        ///     The target to call the handler on.
        /// </param>
        /// <ExternalAPI/> 
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            SizeChangedEventHandler handler = (SizeChangedEventHandler) genericHandler;
            
            handler(genericTarget, this);
        }
    }
}



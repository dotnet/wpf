// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows
{
    /// <summary>
    ///  The SizeChangedinfo class is used as a parameter to OnSizeRenderChanged.
    /// </summary>
    public class SizeChangedInfo 
    {
        /// <summary>
        ///     Initializes a new instance of the SizeChangedinfo class.
        /// </summary>
        /// <param name="element">
        ///     The element which size is changing.
        /// </param>
        /// <param name="previousSize">
        ///     The size of the object before update. New size is element.RenderSize
        /// </param>
        /// <param name="widthChanged">
        /// The flag indicating that width component of the size changed. Note that due to double math 
        /// effects, the it may be (previousSize.Width != newSize.Width) and widthChanged = true.
        /// This may happen in layout when sizes of objects are fluctuating because of a precision "jitter" of
        /// the input parameters, but the overall scene is considered to be "the same" so no visible changes 
        /// will be detected. Typically, the handler of SizeChangedEvent should check this bit to avoid 
        /// invalidation of layout if the dimension didn't change.
        /// </param>
        /// <param name="heightChanged">
        /// The flag indicating that height component of the size changed. Note that due to double math 
        /// effects, the it may be (previousSize.Height != newSize.Height) and heightChanged = true.
        /// This may happen in layout when sizes of objects are fluctuating because of a precision "jitter" of
        /// the input parameters, but the overall scene is considered to be "the same" so no visible changes 
        /// will be detected. Typically, the handler of SizeChangedEvent should check this bit to avoid 
        /// invalidation of layout if the dimension didn't change.
        /// </param>
        public SizeChangedInfo(UIElement element, Size previousSize, bool widthChanged, bool heightChanged)
        {
            _element = element;
            _previousSize = previousSize;
            _widthChanged = widthChanged;
            _heightChanged = heightChanged;
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
            get { return _widthChanged; }
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
            get { return _heightChanged; }
        }

        //this method is used by UIElement to "accumulate" several cosequitive layout updates
        //into the single args object cahced on UIElement. Since the SizeChanged is deferred event,
        //there could be several size changes before it will actually fire.
        internal void Update(bool widthChanged, bool heightChanged)
        {
            _widthChanged = _widthChanged | widthChanged;
            _heightChanged = _heightChanged | heightChanged;
        }

        internal UIElement Element
        {
            get { return _element; }
        }


        private UIElement _element; 
        private Size _previousSize;
        private bool _widthChanged;
        private bool _heightChanged;

        internal SizeChangedInfo Next;
}
}




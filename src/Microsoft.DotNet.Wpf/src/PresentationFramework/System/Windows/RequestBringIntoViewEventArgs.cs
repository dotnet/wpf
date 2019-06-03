// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows
{
    /// <summary>
    ///  The BringIntoViewEventArgs class is used by BringIntoViewEventHandler.
    /// </summary>
    public class RequestBringIntoViewEventArgs : RoutedEventArgs
    {
        /// <summary>Initializes a new instance of the BringIntoViewEventArgs class.</summary>
        internal RequestBringIntoViewEventArgs(DependencyObject target, Rect targetRect)
        {
            _target = target;
            _rcTarget = targetRect;
        }

        /// <summary>
        /// The object to make visible.
        /// </summary>
        public DependencyObject TargetObject
        {
            get { return _target; }
        }

        /// <summary>
        /// The rectangular region in the object's coordinate space which should be made visible.
        /// </summary>
        public Rect TargetRect
        {
            get { return _rcTarget; }
        }

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
            RequestBringIntoViewEventHandler handler = (RequestBringIntoViewEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        private DependencyObject _target;   // The object to Bring Into View
        private Rect _rcTarget;             // Rectange in the object's coordinate space to bring into view.
    }
}



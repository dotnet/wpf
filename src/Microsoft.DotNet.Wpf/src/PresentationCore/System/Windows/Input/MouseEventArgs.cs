// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

using System;

namespace System.Windows.Input
{
    /// <summary>
    ///     The MouseEventArgs class provides access to the logical
    ///     Mouse device for all derived event args.
    /// </summary>
    public class MouseEventArgs : InputEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the MouseEventArgs class.
        /// </summary>
        /// <param name="mouse">
        ///     The logical Mouse device associated with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occurred.
        /// </param>
        public MouseEventArgs(MouseDevice mouse, int timestamp) : base(mouse, timestamp)
        {
            if( mouse == null )
            {
                throw new System.ArgumentNullException("mouse");
            }
            _stylusDevice = null;
        }

        /// <summary>
        ///     Initializes a new instance of the MouseEventArgs class.
        /// </summary>
        /// <param name="mouse">
        ///     The logical Mouse device associated with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occurred.
        /// </param>
        /// <param name="stylusDevice">
        ///     The stylus device that was involved with this event.
        /// </param>
        public MouseEventArgs(MouseDevice mouse, int timestamp, StylusDevice stylusDevice) : base(mouse, timestamp)
        {
            if( mouse == null )
            {
                throw new System.ArgumentNullException("mouse");
            }
            _stylusDevice = stylusDevice;
        }

        /// <summary>
        ///     Read-only access to the mouse device associated with this
        ///     event.
        /// </summary>
        public MouseDevice MouseDevice
        {
            get {return (MouseDevice) this.Device;}
        }

        /// <summary>
        ///     Read-only access to the stylus Mouse associated with this event.
        /// </summary>
        public StylusDevice StylusDevice
        {
            get {return _stylusDevice;}
        }

        /// <summary>
        ///     Calculates the position of the mouse relative to
        ///     a particular element.
        /// </summary>
        public Point GetPosition(IInputElement relativeTo)
        {
            return this.MouseDevice.GetPosition(relativeTo);
        }

        /// <summary>
        ///     The state of the left button.
        /// </summary>
        public MouseButtonState LeftButton
        { 
            get
            {
                return this.MouseDevice.LeftButton;
            }
        }

        /// <summary>
        ///     The state of the right button.
        /// </summary>
        public MouseButtonState RightButton
        { 
            get
            {
                return this.MouseDevice.RightButton;
            }
        }

        /// <summary>
        ///     The state of the middle button.
        /// </summary>
        public MouseButtonState MiddleButton
        { 
            get
            {
                return this.MouseDevice.MiddleButton;
            }
        }

        /// <summary>
        ///     The state of the first extended button.
        /// </summary>
        public MouseButtonState XButton1
        { 
            get
            {
                return this.MouseDevice.XButton1;
            }
        }

        /// <summary>
        ///     The state of the second extended button.
        /// </summary>
        public MouseButtonState XButton2
        { 
            get
            {
                return this.MouseDevice.XButton2;
            }
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
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            MouseEventHandler handler = (MouseEventHandler) genericHandler;
            handler(genericTarget, this);
        }

        private StylusDevice _stylusDevice;
    }
}

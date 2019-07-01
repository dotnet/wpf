// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Input;

namespace Microsoft.Test.Input
{
    /// <summary>
    ///     The RawMouseState class encapsulates the state of the mouse.
    /// </summary>
    public class RawMouseState
    {
        /// <summary>
        ///     Constructs an instance of the RawMouseState class.
        /// </summary>
        /// <param name="x">
        ///     If horizontal position of the mouse.
        /// </param>
        /// <param name="y">
        ///     If vertical position of the mouse.
        /// </param>
        /// <param name="button1">
        ///     The state of the first button of the mouse.
        /// </param>
        /// <param name="button2">
        ///     The state of the second button of the mouse.
        /// </param>
        /// <param name="button3">
        ///     The state of the third button of the mouse.
        /// </param>
        /// <param name="button4">
        ///     The state of the fourth button of the mouse.
        /// </param>
        /// <param name="button5">
        ///     The state of the fifth button of the mouse.
        /// </param>
        public RawMouseState(
            int x, 
            int y,
            MouseButtonState button1,
            MouseButtonState button2,
            MouseButtonState button3,
            MouseButtonState button4,
            MouseButtonState button5)
        {
            _x = x;
            _y = y;

            if (!IsValidMouseButtonState(button1))
                throw new System.ComponentModel.InvalidEnumArgumentException("button1", (int)button1, typeof(MouseButtonState));
           
            if (!IsValidMouseButtonState(button2))
                throw new System.ComponentModel.InvalidEnumArgumentException("button2", (int)button2, typeof(MouseButtonState));
           
            if (!IsValidMouseButtonState(button3))
                throw new System.ComponentModel.InvalidEnumArgumentException("button3", (int)button3, typeof(MouseButtonState));
           
            if (!IsValidMouseButtonState(button4))
                throw new System.ComponentModel.InvalidEnumArgumentException("button4", (int)button4, typeof(MouseButtonState));
           
            if (!IsValidMouseButtonState(button5))
                throw new System.ComponentModel.InvalidEnumArgumentException("button5", (int)button5, typeof(MouseButtonState));


            _button1 = button1;
            _button2 = button2;
            _button3 = button3;
            _button4 = button4;
            _button5 = button5;
        }

        /// <summary>
        ///     Read-only access to the horizontal position that was reported.
        /// </summary>
        public int X {get {return _x;}}

        /// <summary>
        ///     Read-only access to the vertical position that was reported.
        /// </summary>
        public int Y {get {return _y;}}

        /// <summary>
        ///     Read-only access to the status of the first button.
        /// </summary>
        public MouseButtonState Button1 {get {return _button1;}}

        /// <summary>
        ///     Read-only access to the status of the second button.
        /// </summary>
        public MouseButtonState Button2 {get {return _button2;}}

        /// <summary>
        ///     Read-only access to the status of the third button.
        /// </summary>
        public MouseButtonState Button3 {get {return _button3;}}
        
        /// <summary>
        ///     Read-only access to the status of the fourth button.
        /// </summary>
        public MouseButtonState Button4 {get {return _button4;}}

        /// <summary>
        ///     Read-only access to the status of the fifth button.
        /// </summary>
        public MouseButtonState Button5 {get {return _button5;}}

        internal static bool IsValidMouseButtonState(MouseButtonState button)
        {
            return (button == MouseButtonState.Pressed || button == MouseButtonState.Released);
        }

        private int _x;
        private int _y;
        private MouseButtonState _button1;
        private MouseButtonState _button2;
        private MouseButtonState _button3;
        private MouseButtonState _button4;
        private MouseButtonState _button5;
    }    
}


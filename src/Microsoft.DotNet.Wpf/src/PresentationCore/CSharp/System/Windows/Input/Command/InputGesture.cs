// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: InputGesture class acts as base class for all input device  gestures
//
//              See spec at : http://avalon/coreUI/Specs/Commanding%20--%20design.htm 
// 
//
//

using System;
using System.Windows;

namespace System.Windows.Input 
{
    /// <summary>
    /// InputGesture - abstract base class for individual input device gestures.
    ///                For Ex: KeyGesture (Keyboard), MouseGesture (Mouse) derived from this.
    ///                
    /// </summary>
    public abstract class InputGesture
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
#region Public Methods
        /// <summary>
        /// Sees if the InputGesture matches the input associated with the inputEventArgs
        /// </summary>
        /// <remarks>
        /// Compares an InputEventArgs value to Gesture inside.
        /// This method when overriden by derived classes, will match
        /// InputEventArgs with its internal values and return a true/false.
        /// </remarks>
        /// <param name="targetElement">the element to receive the command</param>
        /// <param name="inputEventArgs">inputEventArgs to compare to</param>
        /// <returns>True if matched, false otherwise.
        /// </returns>
        public abstract bool Matches(object targetElement, InputEventArgs inputEventArgs);

#endregion Public Methods
    }
 }


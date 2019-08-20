// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Security;
using System.Windows;

namespace System.Windows.Input
{
    /// <summary>
    ///     Represents an input to a manipulation processor.
    /// </summary>
    public interface IManipulator
    {
        /// <summary>
        ///     An ID that identifies the manipulator.
        /// </summary>
        /// <remarks>
        ///     This ID should be unique within the set of a specific type of IManipulator implementation.
        ///     For instance if both class A and class B implement IManipulator, then for all instances
        ///     of A, Id should be unique, and for all instnaces of B, Id should be unique. However,
        ///     there may be instances of A and instances of B that share the same Id value.
        /// </remarks>
        int Id
        {
            get;
        }

        /// <summary>
        ///     Returns the position of the manipulator.
        /// </summary>
        /// <param name="relativeTo">Defines the coordinate space of the return value.</param>
        /// <returns>The position of the manipulator relative to the parameter.</returns>
        Point GetPosition(IInputElement relativeTo);

        /// <summary>
        ///     Raised when the position has changed.
        /// </summary>
        /// <remarks>
        ///     It is up to the implementor of the interface to decide when the position
        ///     has changed and to call Updated.
        /// </remarks>
        event EventHandler Updated;

        /// <summary>
        ///     Called when the Manipulation ends
        /// </summary>
        /// <param name="cancel">Flag indicating Cancel</param>
        void ManipulationEnded(bool cancel);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Base class from which all inertia parameter classes
    /// are derived.
    /// </summary>
    public abstract class InertiaParameters2D
    {
        /// <summary>
        /// Fires when a property changes.
        /// </summary>
        internal event Action<InertiaParameters2D, string> Changed;

        /// <summary>
        /// Changes a property value, firing the Changed event, if appropriate.
        /// </summary>
        /// <remarks>
        /// This is intended to be called only by derived classes, but we can't put
        /// the "protected" qualifier on it because doing so would make the method
        /// visible outside this assembly, despite the "internal" keyword.
        /// </remarks>
        internal void ProtectedChangeProperty(Func<bool> isEqual, Action setNewValue, string paramName)
        {
            Debug.Assert(isEqual != null);
            Debug.Assert(setNewValue != null);
            Debug.Assert(paramName != null);

            if (!isEqual())
            {
                setNewValue();
                if (Changed != null)
                {
                    Changed(this, paramName);
                }
            }
        }

        /// <summary>
        /// Internal constructor, to prevent deriving new
        /// subclasses outside this assembly.
        /// </summary>
        internal InertiaParameters2D()
        {
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Base class from which all manipulation parameter classes
    /// are derived.
    /// </summary>
    public abstract class ManipulationParameters2D
    {
        /// <summary>
        /// Internal constructor, to prevent derivation of new
        /// subclasses outside this assembly.
        /// </summary>
        internal ManipulationParameters2D()
        {
        }

        /// <summary>
        /// Called when this object is passed into the SetParameters
        /// method on a manipulation processor.
        /// </summary>
        /// <param name="processor"></param>
        internal abstract void Set(ManipulationProcessor2D processor);
    }
}
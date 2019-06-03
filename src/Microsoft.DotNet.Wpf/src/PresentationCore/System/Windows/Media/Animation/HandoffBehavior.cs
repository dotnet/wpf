// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal.PresentationCore;     // FriendAccessAllowed

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// Used to specify how new animations will interact with any current
    /// animations already applied to a property.
    /// </summary>
    public enum HandoffBehavior
    {
        /// <summary>
        /// New animations will completely replace all current animations
        /// on a property. The current value at the time of replacement
        /// will be passed into the first new animation as the 
        /// defaultOriginValue parameter to allow for smooth handoff.
        /// </summary>
        SnapshotAndReplace,

        /// <summary>
        /// New animations will compose with the current animations. The new
        /// animations will be added after the current animations in the
        /// composition chain.
        /// </summary>
        Compose
    }

    internal static class HandoffBehaviorEnum
    {
        // FxCop doesn't like people using Enum.IsDefined for enum validation
        //    http://fxcop/CostlyCallAlternatives/EnumIsDefined.html
        //
        // We have this to have the validation code alongside the enum
        //  definition.  (Rather than spread throughtout the codebase causing
        //  maintenance headaches in the future.)
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal static bool IsDefined( HandoffBehavior handoffBehavior )
        {
            if( handoffBehavior < HandoffBehavior.SnapshotAndReplace ||
                handoffBehavior > HandoffBehavior.Compose )
            {
                return false;
            }
            else
            {
                return true;
            }               
        }
    }
}

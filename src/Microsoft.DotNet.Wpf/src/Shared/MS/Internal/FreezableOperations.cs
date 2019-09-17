// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
//
// Description: FreezableOperations class definition.
//
//
//
//
//---------------------------------------------------------------------------

using System;
using System.Windows;

using MS.Internal.PresentationCore;

namespace MS.Internal
{
    /// <summary>
    ///     Internal static class which provides helper methods for common
    ///     Freezable operations.
    /// </summary>
    [FriendAccessAllowed] // Built into Core, also used by Framework.
    internal static class FreezableOperations
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods
        
        /// <summary>
        ///     A null-safe wrapper around Freezable.Clone().  (If a null
        ///     is encountered it returns null.)
        /// </summary>
        internal static Freezable Clone(Freezable freezable)
        {
            if (freezable == null)
            {
                return null;
            }

            return freezable.Clone();
        }

        /// <summary>
        ///     Semantically equivilent to Freezable.Clone().Freeze() except that
        ///     GetAsFrozen avoids copying any portions of the Freezable graph
        ///     which are already frozen.
        /// </summary>
        public static Freezable GetAsFrozen(Freezable freezable)
        {
            if (freezable == null)
            {
                return null;
            }

            return freezable.GetAsFrozen();
        }
        
        /// <summary>
        /// If freezable is already frozen, it returns freezable
        /// If freezable is not frozen, it returns a copy that is frozen if possible
        /// </summary>
        internal static Freezable GetAsFrozenIfPossible(Freezable freezable)
        {
            if (freezable == null)
            {
                return null;
            }

            if (freezable.CanFreeze)
            {
                freezable = freezable.GetAsFrozen();
            }

            return freezable;
        }

        /// <summary>
        ///     Moves the specified changed handler from the old value
        ///     to the new value correctly no-oping nulls.  This is useful
        ///     for non-Freezables which expose a Freezable property.
        /// </summary>
        internal static void PropagateChangedHandlers(
            Freezable oldValue,
            Freezable newValue,
            EventHandler changedHandler)
        {
            if (newValue != null && !newValue.IsFrozen)
            {
                newValue.Changed += changedHandler;
            }

            if (oldValue != null && !oldValue.IsFrozen)
            {
                oldValue.Changed -= changedHandler;
            }
        }

        #endregion Internal Methods
    }
}

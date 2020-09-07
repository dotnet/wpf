// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using MS.Internal.Automation;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{
    /// <summary>
    /// Condition that checks whether a pattern is currently present for a LogicalElement
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class OrCondition : Condition
#else
    public class OrCondition : Condition
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        /// <summary>
        /// Constructor to create a condition that is true if any of the sub-conditions are true
        /// </summary>
        /// <param name="conditions">One or more sub-condition</param>
        public OrCondition( params Condition [ ] conditions )
        {
            Misc.ValidateArgumentNonNull( conditions, "conditions" );
            Misc.ValidateArgument( conditions.Length >= 2, SRID.MustBeAtLeastTwoConditions );
            foreach( Condition condition in conditions )
            {
                Misc.ValidateArgumentNonNull( condition, "conditions" );
            }

            // clone array to prevent accidental tampering
            _conditions = (Condition[])conditions.Clone();
            _conditionArrayHandle = SafeConditionMemoryHandle.AllocateConditionArrayHandle(_conditions);
            // DangerousGetHandle() reminds us that the IntPtr we get back could be collected/released/recycled. We're safe here,
            // because the Conditions are structured in a tree, with the root one (which gets passed to the Uia API) keeping all
            // others - and their associated data - alive. (Recycling isn't an issue as these are immutable classes.)
            SetMarshalData(new UiaCoreApi.UiaAndOrCondition(UiaCoreApi.ConditionType.Or, _conditionArrayHandle.DangerousGetHandle(), _conditions.Length));
        }
        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Returns an array of the sub conditions for this condition.
        /// </summary>
        /// <remarks>
        /// The returned array is a copy; modifying it will not affect the
        /// state of the condition.
        /// </remarks>
        public Condition [ ] GetConditions()
        {
            return (Condition []) _conditions.Clone();
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        Condition [ ] _conditions;
        SafeConditionMemoryHandle _conditionArrayHandle;

        #endregion Private Fields
    }
}

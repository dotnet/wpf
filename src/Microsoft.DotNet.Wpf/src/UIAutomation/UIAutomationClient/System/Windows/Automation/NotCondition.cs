// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using MS.Internal.Automation;
using System.Windows.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Condition that checks whether a pattern is currently present for a LogicalElement
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class NotCondition : Condition
#else
    public class NotCondition : Condition
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        /// <summary>
        /// Constructor to create a condition that negates the specified condition
        /// </summary>
        /// <param name="condition">Condition to negate</param>
        public NotCondition( Condition condition )
        {
            Misc.ValidateArgumentNonNull( condition, "condition" );

            _condition = condition;

            // DangerousGetHandle() reminds us that the IntPtr we get back could be collected/released/recycled. We're safe here,
            // because the Conditions are structured in a tree, with the root one (which gets passed to the Uia API) keeping all
            // others - and their associated data - alive. (Recycling isn't an issue as these are immutable classes.)
            SetMarshalData(new UiaCoreApi.UiaNotCondition(_condition._safeHandle.DangerousGetHandle()));
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Returns the sub condition that this condition is negating.
        /// </summary>
        public Condition Condition
        {
            get
            {
                return _condition;
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        Condition _condition;

        #endregion Private Fields
    }
}

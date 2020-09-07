// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*  TargetType property and event setting base class.
*
*
\***************************************************************************/
namespace System.Windows
{
    /// <summary>
    ///     TargetType property and event setting base class.
    /// </summary>
    [Localizability(LocalizationCategory.Ignore)]
    public abstract class SetterBase
    {
        /// <summary>
        ///     SetterBase construction
        /// </summary>
        internal SetterBase()
        {
        }

        /// <summary>
        ///     Returns the sealed state of this object.  If true, any attempt
        /// at modifying the state of this object will trigger an exception.
        /// </summary>
        public bool IsSealed
        {
            get
            {
                return _sealed;
            }
        }

        internal virtual void Seal()
        {
            _sealed = true;
        }

        /// <summary>
        ///  Subclasses need to call this method before any changes to their state.
        /// </summary>
        protected void CheckSealed()
        {
            if ( _sealed )
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "SetterBase"));
            }            
        }

        // Derived
        private bool _sealed;
    }
        
}


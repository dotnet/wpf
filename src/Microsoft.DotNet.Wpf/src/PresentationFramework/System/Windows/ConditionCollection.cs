// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* A collection of Condition-derived classes. See use in Style.cs and other
* places.
*
*
\***************************************************************************/
using System.Collections.ObjectModel; // Collection<T>
using System.Diagnostics;   // Debug.Assert
using System.Windows.Data;  // Binding knowledge
using System.Windows.Media; // Visual knowledge
using System.Windows.Markup; // MarkupExtension

namespace System.Windows
{
    /// <summary>
    ///     A collection of Condition objects to be used 
    ///     in Template and its trigger classes
    /// </summary>
    public sealed class ConditionCollection : Collection<Condition>
    {
        #region ProtectedMethods

        /// <summary>
        ///     ClearItems override
        /// </summary>
        protected override void ClearItems()
        {
            CheckSealed();
            base.ClearItems();
        }

        /// <summary>
        ///     InsertItem override
        /// </summary>
        protected override void InsertItem(int index, Condition item)
        {
            CheckSealed();
            ConditionValidation(item);
            base.InsertItem(index, item);
        }

        /// <summary>
        ///     RemoveItem override
        /// </summary>
        protected override void RemoveItem(int index)
        {
            CheckSealed();
            base.RemoveItem(index);
        }

        /// <summary>
        ///     SetItem override
        /// </summary>
        protected override void SetItem(int index, Condition item)
        {
            CheckSealed();
            ConditionValidation(item);
            base.SetItem(index, item);
        }

        #endregion ProtectedMethods

        #region PublicMethods
        
        /// <summary>
        ///     Returns the sealed state of this object.  If true, any attempt
        ///     at modifying the state of this object will trigger an exception.
        /// </summary>
        public bool IsSealed
        {
            get
            {
                return _sealed;
            }
        }
        
        #endregion PublicMethods

        #region InternalMethods
        
        internal void Seal(ValueLookupType type)
        {
            _sealed = true;

            // Seal all the conditions
            for (int i=0; i<Count; i++)
            {
                this[i].Seal(type);
            }
        }

        #endregion InternalMethods
        
        #region PrivateMethods

        private void CheckSealed()
        {
            if (_sealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "ConditionCollection"));
            }
        }
        
        private void ConditionValidation(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            
            Condition condition = value as Condition;
            if (condition == null)
            {
                throw new ArgumentException(SR.Get(SRID.MustBeCondition));
            }
        }

        #endregion PrivateMethods

        #region Data
    
        private bool _sealed;

        #endregion Data
    }
}



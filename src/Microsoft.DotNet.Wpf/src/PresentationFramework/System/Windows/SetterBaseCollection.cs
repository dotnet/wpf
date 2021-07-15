// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* A collection of SetterBase-derived classes. See use in Style.cs and other
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
    ///     A collection of SetterBase objects to be used
    ///     in Template and its trigger classes
    /// </summary>
    public sealed class SetterBaseCollection : Collection<SetterBase>
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
        protected override void InsertItem(int index, SetterBase item)
        {
            CheckSealed();
            SetterBaseValidation(item);
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
        protected override void SetItem(int index, SetterBase item)
        {
            CheckSealed();
            SetterBaseValidation(item);
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

        internal void Seal()
        {
            _sealed = true;

            // Seal all the setters
            for (int i=0; i<Count; i++)
            {
                this[i].Seal();
            }
        }

        #endregion InternalMethods

        #region PrivateMethods

        private void CheckSealed()
        {
            if (_sealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "SetterBaseCollection"));
            }
        }

        private void SetterBaseValidation(SetterBase setterBase)
        {
            if (setterBase == null)
            {
                throw new ArgumentNullException("setterBase");
            }
        }

        #endregion PrivateMethods

        #region Data

        private bool _sealed;

        #endregion Data
    }
}


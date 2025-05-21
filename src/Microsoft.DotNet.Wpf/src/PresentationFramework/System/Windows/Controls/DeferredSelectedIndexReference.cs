// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Proxy object passed to the property system to delay load
//              Selector.SelectedIndex values.
//

using System.Windows.Controls.Primitives;

namespace System.Windows.Controls
{
    // Proxy object passed to the property system to delay load Selector.SelectedIndex
    // values.
    internal class DeferredSelectedIndexReference : DeferredReference
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal DeferredSelectedIndexReference(Selector selector)
        {
            _selector = selector;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Does the real work to calculate the current SelectedIndexProperty value.
        internal override object GetValue(BaseValueSourceInternal valueSource)
        {
            return _selector.InternalSelectedIndex;
        }
        
        // Gets the type of the value it represents
        internal override Type GetValueType()
        {
            return typeof(int);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Selector mapped to this object.
        private readonly Selector _selector;

        #endregion Private Fields
     }
}

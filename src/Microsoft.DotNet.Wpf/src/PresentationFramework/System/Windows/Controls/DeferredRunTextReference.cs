// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Proxy object passed to the property system to delay load
//              TextProperty values.
//

namespace System.Windows.Controls
{
    using System.Windows.Documents;

    // Proxy object passed to the property system to delay load TextProperty
    // values.
    internal class DeferredRunTextReference : DeferredReference
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal DeferredRunTextReference(Run run)
        {
            _run = run;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Does the real work to calculate the current TextProperty value.
        internal override object GetValue(BaseValueSourceInternal valueSource)
        {
            return TextRangeBase.GetTextInternal(_run.ContentStart, _run.ContentEnd);
        }

        // Gets the type of the value it represents
        internal override Type GetValueType()
        {
            return typeof(string);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Run mapped to this object.
        private readonly Run _run;

        #endregion Private Fields
    }
}

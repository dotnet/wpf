// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Documents;

//
// Description: Proxy object passed to the property system to delay load
//              TextProperty values.
//

namespace System.Windows.Controls
{
    // Proxy object passed to the property system to delay load TextProperty
    // values.
    internal class DeferredTextReference : DeferredReference
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal DeferredTextReference(ITextContainer textContainer)
        {
            _textContainer = textContainer;
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
            string s = TextRangeBase.GetTextInternal(_textContainer.Start, _textContainer.End);

            TextBox tb = _textContainer.Parent as TextBox;
            tb?.OnDeferredTextReferenceResolved(this, s);

            return s;
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

        // TextContainer mapped to this object.
        private readonly ITextContainer _textContainer;

        #endregion Private Fields
     }
}

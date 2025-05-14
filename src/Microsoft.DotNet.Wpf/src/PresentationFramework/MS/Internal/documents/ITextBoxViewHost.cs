// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Documents;

//
// Description: Interface for Controls hosting TextBoxView.
//

namespace System.Windows.Controls
{
    // Controls built on TextBoxView must implement this interface
    // which is passed to the TextBoxView ctor.
    // TextBoxView requires that the object implementing ITextBoxViewHost
    // is additionally a Control.
    internal interface ITextBoxViewHost
    {
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // ITextContainer holding the Control content.
        ITextContainer TextContainer { get; }

        // Set true when typography property values are all default values.
        bool IsTypographyDefaultValue { get; }

        #endregion Internal Properties
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace MS.Internal.Documents
{
    /// <summary>
    /// A set of resources that describe overall document status
    /// for a particular layer (RM, DigSig, etc.) designed to be directly
    /// injected into the UI.
    /// </summary>
    internal struct DocumentStatusResources
    {
        /// <summary>
        /// The image representing the current document status for the UI.
        /// </summary>
        internal System.Windows.Media.DrawingBrush Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
            }
        }

        /// <summary>
        /// The text representing the current document status for the UI.
        /// </summary>
        internal string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
            }
        }

        /// <summary>
        /// The ToolTip text for the hover states of any controls representing this status
        /// for the UI.
        /// </summary>
        internal string ToolTip
        {
            get
            {
                return _toolTip;
            }
            set
            {
                _toolTip = value;
            }
        }

        // Private Fields
        private System.Windows.Media.DrawingBrush _image;
        private string _text;
        private string _toolTip;
    }
}

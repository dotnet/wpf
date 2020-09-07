// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Rtf control word information that specify the Rtf control word, 
//              name and flag.
//

namespace System.Windows.Documents
{
    /// <summary>
    /// Rtf control word information that specify the Rtf control word, name and flag.
    /// </summary>
    internal class RtfControlWordInfo
    {
        // ---------------------------------------------------------------------
        //
        // Internal Methods
        //
        // ---------------------------------------------------------------------

        #region Internal Methods

        internal RtfControlWordInfo(RtfControlWord controlWord, string controlName, uint flags)
        {
            _controlWord = controlWord;
            _controlName = controlName;
            _flags = flags;
        }

        #endregion Internal Methods

        // ---------------------------------------------------------------------
        //
        // Internal Properties
        //
        // ---------------------------------------------------------------------

        #region Internal Properties

        internal RtfControlWord Control
        {
            get
            {
                return _controlWord;
            }
        }

        internal string ControlName
        {
            get
            {
                return _controlName;
            }
        }

        internal uint Flags
        {
            get
            {
                return _flags;
            }
        }

        #endregion Internal Properties

        // ---------------------------------------------------------------------
        //
        // Private Fields
        //
        // ---------------------------------------------------------------------

        #region Private Fields

        private RtfControlWord _controlWord;
        private string _controlName;
        private uint _flags;

        #endregion Private Fields
    }
}


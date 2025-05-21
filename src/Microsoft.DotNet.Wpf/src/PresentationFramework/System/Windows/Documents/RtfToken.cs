// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Rtf token that will specify the rtf token type, control and name.
//

namespace System.Windows.Documents
{
    /// <summary>
    /// Rtf token that include rtf token type, control, name and parameter value.
    /// </summary>
    internal class RtfToken
    {
        #region Internal Consts

        //------------------------------------------------------
        //
        //  Internal Consts
        //
        //------------------------------------------------------

        internal const long INVALID_PARAMETER = 0x10000000;

        #endregion Internal Consts

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        internal RtfToken()
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal void Empty()
        {
            _type = RtfTokenType.TokenInvalid;
            _rtfControlWordInfo = null;
            _parameter = INVALID_PARAMETER;
            _text = "";
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal RtfTokenType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        internal RtfControlWordInfo RtfControlWordInfo
        {
            get
            {
                return _rtfControlWordInfo;
            }
            set
            {
                _rtfControlWordInfo = value;
            }
        }

        internal long Parameter
        {
            get
            {
                return HasParameter ? _parameter : 0;
            }
            set
            {
                _parameter = value;
            }
        }

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

        internal long ToggleValue
        {
            get
            {
                return HasParameter ? Parameter : 1;
            }
        }

        internal bool FlagValue
        {
            get
            {
                return (!HasParameter || (HasParameter && Parameter > 0) ? true : false);
            }
        }

        internal bool HasParameter
        {
            get
            {
                return _parameter != INVALID_PARAMETER;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private RtfTokenType _type;

        private RtfControlWordInfo _rtfControlWordInfo;

        private long _parameter;

        private string _text;

        #endregion Private Fields
    }
}

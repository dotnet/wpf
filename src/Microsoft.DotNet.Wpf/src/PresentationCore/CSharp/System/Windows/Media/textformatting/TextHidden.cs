// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Implementation of text hidden content
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Collections;
using System.Windows;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// Specialized text run used to mark a range of hidden characters
    /// </summary>
    public class TextHidden : TextRun
    {
        private int     _length;

        #region Constructors


        /// <summary>
        /// Construct a hidden text run
        /// </summary>
        /// <param name="length">number of characters</param>
        public TextHidden(
            int     length
            )
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException("length", SR.Get(SRID.ParameterMustBeGreaterThanZero));
            }

            _length = length;
        }

        #endregion


        /// <summary>
        /// Reference to character buffer
        /// </summary>
        public sealed override CharacterBufferReference CharacterBufferReference
        {
            get { return new CharacterBufferReference(); }
        }

        
        /// <summary>
        /// Character length
        /// </summary>
        public sealed override int Length
        {
            get { return _length; }
        }


        /// <summary>
        /// A set of properties shared by every characters in the run
        /// </summary>
        public sealed override TextRunProperties Properties
        {
            get { return null; }
        }
    }
}


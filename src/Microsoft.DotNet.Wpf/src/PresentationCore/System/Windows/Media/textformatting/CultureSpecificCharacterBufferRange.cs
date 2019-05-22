// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: CultureSpecificCharacterBufferRange class
//

using System;
using System.Globalization;
using System.Windows.Media.TextFormatting;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// Represents a range of characters that are associated with a culture
    /// </summary>
    public class CultureSpecificCharacterBufferRange
    {
        private CultureInfo _culture;
        private CharacterBufferRange _characterBufferRange;

        /// <summary>
        /// Construct a CultureSpecificCharacterBufferRange class
        /// </summary>
        public CultureSpecificCharacterBufferRange(CultureInfo culture, CharacterBufferRange characterBufferRange)
        {        
            _culture = culture;
            _characterBufferRange = characterBufferRange;
        }

        /// <summary>
        /// Culture of the containing range of characters 
        /// </summary>
        public CultureInfo CultureInfo 
        {
            get { return _culture; }
        }

        /// <summary>
        /// The character range
        /// </summary>
        public CharacterBufferRange CharacterBufferRange
        {
            get { return _characterBufferRange; }
        }
    }
}
  

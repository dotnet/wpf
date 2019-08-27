// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __ITEMIZER_HELPER_H
#define __ITEMIZER_HELPER_H

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{

    private ref class ItemizerHelper
    {
    internal:
        /// <summary>
        /// Helper function to set ItemFlags.HasExtendedCharacter.
        /// </summary>
        /// <remarks>
        /// The old WPF Itemizer used to set ItemFlags.HasExtendedCharacter flag to TRUE for 
        /// all characters that represent extended Unicode range (>U+FFFF), this includes Surrogates Area.
        ///     Surrogate Area: U+D800  - U+DFFF
        ///     Extended Area:  U+10000 - U+10FFFF
        /// </remarks>
        /// <param name="ch">Character to investigate.</param>
        /// <returns>Value indicating whether the character is in extended UTF-16 range or not.</returns>
        static bool IsExtendedCharacter(WCHAR ch)
        {
            // NOTE: char is 16 bit, so values > U+FFFF cannot be expressed by char.
            //       Hence check for ((ch & 0x1F0000) > 0) is not necessary.
            return ((ch & 0xF800) == 0xD800);
        }
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__ITEMIZER_HELPER_H

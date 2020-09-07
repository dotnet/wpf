// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   Static Helper methods used for Serialization.
//
//

using System;
using MS.Internal.PresentationCore;

namespace System.Windows.Markup
{
    /// <summary>
    ///     Static helper methods used for Serialization process
    /// </summary>
    internal static class XamlCoreSerializerUtil
    {
        static XamlCoreSerializerUtil()
        {
            //
            // Dummy code to keep IAddChildInternal from being optimized out of
            // PresentationCore. PLEASE REMOVE WHEN IAddChildInternal DISAPPEARS.
            //
            ThrowIfIAddChildInternal("not IAddChildInternal");
        }
        

        internal static void ThrowIfIAddChildInternal(object o)
        {
            //
            // Dummy code to keep IAddChildInternal from being optimized out of
            // PresentationCore. PLEASE REMOVE WHEN IAddChildInternal DISAPPEARS.
            //
            if ( o is IAddChildInternal)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        ///     Throw an exception if the passed string is not empty and is not
        ///     all whitespace.  This is used to check IAddChild.AddText calls for
        ///     objects that don't handle text, but may get some whitespace if
        ///     if xml:space="preserve" is set in xaml.
        /// </summary>
        internal static void ThrowIfNonWhiteSpaceInAddText(string s)
        {
            if (s != null)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    if (!Char.IsWhiteSpace(s[i]))
                    {
                        throw new ArgumentException(SR.Get(SRID.NonWhiteSpaceInAddText, s));
                    }
                }
            }
        }
    }
}


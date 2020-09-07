// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   Static Helper methods used for building NamespaceTables
//   and during actual Serialization.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Windows;
using System.Globalization;
using System.Windows.Data;
using MS.Utility;
using MS.Internal;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /// <summary>
    ///     Static helper methods used for building
    ///     NamespaceTables and during the actual
    ///     Serialization process
    /// </summary>
    internal static class XamlSerializerUtil
    {
        #region Helpers

        /// <summary>
        ///     Throw an exception if the passed string is not empty and is not
        ///     all whitespace.  This is used to check IAddChild.AddText calls for
        ///     object that don't handle text, but may get some whitespace if
        ///     if xml:space="preserve" is set in xaml.
        /// </summary>
        internal static void ThrowIfNonWhiteSpaceInAddText(string s, object parent)
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

        #endregion Helpers
    }
}


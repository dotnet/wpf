// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Define IInputLanguageSource interface.
//
//

using System.Collections;
using System.Globalization;

using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///     An interface for controlling the input language source.
    /// </summary>
    // We may need to public this interface for a custom dispather.
    public interface IInputLanguageSource 
    {
        /// <summary>
        ///     This access to the current input language.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     This access to the current input language.
        /// </summary>
        void Uninitialize();

        /// <summary>
        ///     This access to the current input language.
        /// </summary>
        CultureInfo CurrentInputLanguage
        {
            get;
            set;
        }

        /// <summary>
        ///     This returns the list of available input languages.
        /// </summary>
        IEnumerable InputLanguageList
        {
            get;
        }
    }
}

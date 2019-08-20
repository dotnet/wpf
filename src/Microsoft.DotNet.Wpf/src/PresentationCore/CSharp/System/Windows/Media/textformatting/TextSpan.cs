// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  A simple pairing of an object of type T and a run length
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// A simple pairing of an object of type T and a run length
    /// </summary>
    public class TextSpan<T>
    {
        private int     _length;
        private T       _value;


        /// <summary>
        /// Construct an object/length pairing
        /// </summary>
        /// <param name="length">run length</param>
        /// <param name="value">value</param>
        public TextSpan(
            int     length,
            T       value
            )
        {
            _length = length;
            _value = value;
        }


        /// <summary>
        /// Number of characters in span
        /// </summary>
        public int Length
        {
            get { return _length; }
        }


        /// <summary>
        /// Value    associated with span
        /// </summary>
        public T Value
        {
            get { return _value; }
        }
    }
}


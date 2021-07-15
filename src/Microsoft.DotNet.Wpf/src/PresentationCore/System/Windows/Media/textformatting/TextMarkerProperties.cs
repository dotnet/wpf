// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Definition of text marker properties
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Collections;
using System.Windows;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// </summary>
    public abstract class TextMarkerProperties
    {
        /// <summary>
        /// Distance from line start to the end of the marker symbol
        /// </summary>
        public abstract double Offset
        { get; }


        /// <summary>
        /// Source of text runs used for text marker
        /// </summary>
        public abstract TextSource TextSource
        { get; }
    }
}


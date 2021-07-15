// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Definition of tab properties and related types
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
    /// Properties of user-defined tab
    /// </summary>
    public class TextTabProperties
    {
        private TextTabAlignment    _alignment;
        private double              _location;
        private int                 _tabLeader;
        private int                 _aligningChar;


        /// <summary>
        /// Construct tab properties
        /// </summary>
        /// <param name="alignment">alignment of text at tab location</param>
        /// <param name="location">tab location</param>
        /// <param name="tabLeader">tab leader</param>
        /// <param name="aligningChar">specific character in text that is aligned at tab location</param>
        public TextTabProperties(
            TextTabAlignment    alignment,
            double              location,
            int                 tabLeader,
            int                 aligningChar
            )
        {
            _alignment = alignment;
            _location = location;
            _tabLeader = tabLeader;
            _aligningChar = aligningChar;
        }


        /// <summary>
        /// Property to determine how text is aligned at tab location
        /// </summary>
        public TextTabAlignment Alignment
        {
            get { return _alignment; }
        }


        /// <summary>
        /// Tab location
        /// </summary>
        public double Location
        {
            get { return _location; }
        }


        /// <summary>
        /// Character used to display tab leader
        /// </summary>
        public int TabLeader
        {
            get { return _tabLeader; }
        }


        /// <summary>
        /// Specific character in text that is aligned at specified tab location
        /// </summary>
        public int AligningCharacter
        {
            get { return _aligningChar; }
        }
    }


    /// <summary>
    /// This property determines how text is aligned at tab location
    /// </summary>
    public enum TextTabAlignment
    {
        /// <summary>
        /// Text is left-aligned at tab location
        /// </summary>
        Left,

        /// <summary>
        /// Text is center-aligned at tab location
        /// </summary>
        Center,

        /// <summary>
        /// Text is right-aligned at tab location
        /// </summary>
        Right,

        /// <summary>
        /// Text is aligned at tab location at a specified character
        /// </summary>
        Character,
    }
}


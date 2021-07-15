// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Marker properties. 
//


using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.TextFormatting;
using MS.Internal.PtsHost.UnsafeNativeMethods; // Relative line height from PTS

namespace MS.Internal.Text
{
    // ----------------------------------------------------------------------
    // Marker properties.
    // ----------------------------------------------------------------------
    internal sealed class MarkerProperties
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// The listWidth parameter gives the width of the list element, and is used to clip the MarkerOffset value
        /// </remarks>
        internal MarkerProperties(List list, int index)
        {
            _offset = list.MarkerOffset;
            // Negative value for offset because it is required by TextFormatter line box model.
            // If offset is NaN - default value - set it as 0.5 * line height
            if (Double.IsNaN(_offset))
            {
                // Obtain list's line height to set defualt marker offsert
                double lineHeight = DynamicPropertyReader.GetLineHeightValue(list);
                _offset = - 0.5 * lineHeight;
            }
            else
            {
                _offset = -_offset;
            }

            _style = list.MarkerStyle;
            _index = index;
        }

        // ------------------------------------------------------------------
        // GetTextMarkerProperties
        // ------------------------------------------------------------------
        internal TextMarkerProperties GetTextMarkerProperties(TextParagraphProperties textParaProps)
        {
            return new TextSimpleMarkerProperties(_style, _offset, _index, textParaProps);
        }

        // ------------------------------------------------------------------
        // Marker style
        // ------------------------------------------------------------------
        private TextMarkerStyle _style;

        // ------------------------------------------------------------------
        // Distance from line start to the end of the marker symbol.
        // ------------------------------------------------------------------
        private double _offset;

        // ------------------------------------------------------------------
        // Autonumbering counter of counter-style marker.
        // ------------------------------------------------------------------
        private int _index;
}
}

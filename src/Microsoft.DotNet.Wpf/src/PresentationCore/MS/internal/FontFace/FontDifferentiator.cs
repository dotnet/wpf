// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: FontDifferentiator class handles parsing font family and face names
// and adjusting stretch, weight and style values.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Markup;    // for XmlLanguage

namespace MS.Internal.FontFace
{
    /// <summary>
    /// FontDifferentiator class handles parsing font family and face names
    /// and adjusting stretch, weight and style values.
    /// </summary>
    internal static class FontDifferentiator
    {
        internal static IDictionary<XmlLanguage, string> ConstructFaceNamesByStyleWeightStretch(
            FontStyle style,
            FontWeight weight,
            FontStretch stretch)
        {
            string faceName = BuildFaceName(style, weight, stretch);

            // Default comparer calls CultureInfo.Equals, which works for our purposes.
            Dictionary<XmlLanguage, string> faceNames = new Dictionary<XmlLanguage, string>(1);
            faceNames.Add(XmlLanguage.GetLanguage("en-us"), faceName);
            return faceNames;
        }

        private static string BuildFaceName(
            FontStyle fontStyle,
            FontWeight fontWeight,
            FontStretch fontStretch
            )
        {
            string parsedStyleName   = null;
            string parsedWeightName  = null;
            string parsedStretchName = null;
            string regularFaceName   = "Regular";
            if (fontWeight != FontWeights.Normal)
                parsedWeightName = ((IFormattable)fontWeight).ToString(null, CultureInfo.InvariantCulture);

            if (fontStretch != FontStretches.Normal)
                parsedStretchName = ((IFormattable)fontStretch).ToString(null, CultureInfo.InvariantCulture);

            if (fontStyle != FontStyles.Normal)
                parsedStyleName = ((IFormattable)fontStyle).ToString(null, CultureInfo.InvariantCulture);

            // Build correct face string.
            // Set the initial capacity to be able to hold the word "Regular".
            StringBuilder faceNameBuilder = new StringBuilder(7);

            if (parsedStretchName != null)
            {
                faceNameBuilder.Append(parsedStretchName);
            }

            if (parsedWeightName != null)
            {
                if (faceNameBuilder.Length > 0)
                {
                    faceNameBuilder.Append(" ");
                }
                faceNameBuilder.Append(parsedWeightName);
            }

            if (parsedStyleName != null)
            {
                if (faceNameBuilder.Length > 0)
                {
                    faceNameBuilder.Append(" ");
                }
                faceNameBuilder.Append(parsedStyleName);
            }

            if (faceNameBuilder.Length == 0)
            {
                faceNameBuilder.Append(regularFaceName);
            }

            return faceNameBuilder.ToString();
        }         
    }
}

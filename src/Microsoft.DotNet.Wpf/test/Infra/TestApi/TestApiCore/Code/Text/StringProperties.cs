// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Microsoft.Test.Text
{
    /// <summary>
    /// Contains types for the generation, manipulation and validation of strings and text, for testing purposes.  
    /// </summary>
    // Suppressed the warning that the class is never instantiated.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
    [System.Runtime.CompilerServices.CompilerGenerated()]
    class NamespaceDoc
    {
        // Empty class used only for generation of namespace comments.
    }

    /// <summary>
    /// Defines the desired properties of a character string. 
    /// For more information on character strings, see <a href="http://msdn.microsoft.com/en-us/library/dd317711(VS.85).aspx">this article</a>.
    /// </summary>
    /// <remarks>
    /// Note that this class is used as <i>"a filter"</i> when generating character strings with  
    /// <see cref="StringFactory"/>. Upon instantiation, all properties except CultureInfo of a <see cref="StringProperties"/>  
    /// object (which are all <a href="http://msdn.microsoft.com/en-us/library/system.nullable.aspx">Nullables</a>)   
    /// have null values, which means that the object does not impose any filtering limitations on  
    /// the generated strings. 
    /// <para>
    /// Setting properties to non-null values means that the value of the property should be taken  
    /// into account by <see cref="StringFactory"/> during  string generation. For example, setting  
    /// <see cref="MaxNumberOfCodePoints"/> to 10 means <i>"generate strings with up to 10 code points"</i>.</para>
    /// </remarks>
    ///
    /// <example>
    /// The following example demonstrates how to generate a Cyrillic string that 
    /// contains between 10 and 30 characters.
    /// <code>
    /// StringProperties properties = new StringProperties();
    ///
    /// properties.MinNumberOfCodePoints = 10;
    /// properties.MaxNumberOfCodePoints = 30;
    /// properties.UnicodeRanges.Add(new UnicodeRange(UnicodeChart.Cyrillic));
    ///
    /// string s = StringFactory.GenerateRandomString(properties, 1234);
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// The following example demonstrates how to generate a string which 
    /// contains characters from more than one Unicode chart.
    /// <code>
    /// StringProperties sp = new StringProperties();
    /// 
    /// sp.MinNumberOfCodePoints = 10;
    /// sp.MaxNumberOfCodePoints = 20;
    /// sp.UnicodeRanges.Add(new UnicodeRange(UnicodeChart.BraillePatterns));
    /// sp.UnicodeRanges.Add(new UnicodeRange(UnicodeChart.Cyrillic));
    ///
    /// string s = StringFactory.GenerateRandomString(sp, 1234);
    /// </code>
    /// </example>
    public class StringProperties
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public StringProperties()
        {
            UnicodeRanges = new Collection<UnicodeRange>();
            MinNumberOfCombiningMarks = null;
            HasNumbers = null;
            IsBidirectional = null;
            NormalizationForm = null;
            MinNumberOfCodePoints = MaxNumberOfCodePoints = null;
            MinNumberOfEndUserDefinedCodePoints = null;
            MinNumberOfLineBreaks = null;
            MinNumberOfSurrogatePairs = null;
            MinNumberOfTextSegmentationCodePoints = null;
        }

        /// <summary>
        /// Determines whether the string belongs to one or more <see cref="UnicodeRange"/>.
        /// </summary>
        public Collection<UnicodeRange> UnicodeRanges { get; private set; }

        /// <summary>
        /// Determines whether the string contains formatted numbers. 
        /// </summary>
        public bool? HasNumbers { get; set; }

        /// <summary>
        /// Determines whether the string is <a href="http://en.wikipedia.org/wiki/Bi-directional_text">bi-directional</a>. 
        /// </summary>
        public bool? IsBidirectional { get; set; }

        /// <summary>
        /// Determines the type of normalization to perform on the string. 
        /// For more information, see <a href="http://www.unicode.org/reports/tr15">this article</a>.
        /// </summary>
        public NormalizationForm? NormalizationForm { get; set; }

        /// <summary>
        /// Determines the minimum number of combining marks in the string. 
        /// <a href="http://www.unicode.org/reports/tr15/">Combining marks</a> (and combining 
        /// characters in general) are characters that are intended to modify other characters (e.g. accents, etc.)
        /// </summary>
        public int? MinNumberOfCombiningMarks { get; set; }

        /// <summary>
        /// Determines the minimum number of code points (characters) in the string.
        /// </summary>
        public int? MinNumberOfCodePoints { get; set; }

        /// <summary>
        /// Determines the maximum number of code points (characters) in the string.
        /// </summary>
        public int? MaxNumberOfCodePoints { get; set; }

        /// <summary>
        /// Determines the minimum number of <a href="http://msdn.microsoft.com/en-us/library/dd317802(VS.85).aspx">end-user-defined characters</a> (EUDC) in the string.
        /// </summary>
        public int? MinNumberOfEndUserDefinedCodePoints { get; set; }

        /// <summary>
        /// Determines the minimum number of line breaks in the string.
        /// </summary>
        public int? MinNumberOfLineBreaks { get; set; }

        /// <summary>
        /// Determines the minimum number of <a href="http://en.wikipedia.org/wiki/Surrogate_pair">surrogate pairs</a> in the string. 
        /// </summary>
        public int? MinNumberOfSurrogatePairs { get; set; }

        /// <summary>
        /// Determines the minimum number of <a href="http://en.wikipedia.org/wiki/Text_segmentation">text segmentation code points</a> in the string.
        /// </summary>
        public int? MinNumberOfTextSegmentationCodePoints { get; set; }
    }
}

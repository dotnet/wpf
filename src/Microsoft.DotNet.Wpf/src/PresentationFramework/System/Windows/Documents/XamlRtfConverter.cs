// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Xaml-Rtf Converter.
//

using System.IO;
using System.Text;

namespace System.Windows.Documents
{
    /// <summary>
    /// XamlRtfConverter is a static class that convert from/to rtf content to/from xaml content.
    /// </summary>
    internal class XamlRtfConverter
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// create new instance of XamlRtfConverter that convert the content between xaml and rtf.
        /// </summary>
        internal XamlRtfConverter()
        {
        }

        #endregion Constructors

        // ---------------------------------------------------------------------
        //
        // Internal Methods
        //
        // ---------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Converts an xaml content to rtf content.
        /// </summary>
        /// <param name="xamlContent">
        /// The source xaml text content to be converted into Rtf content.
        /// </param>
        /// <returns>
        /// Well-formed representing rtf equivalent string for the source xaml content.
        /// </returns>
        internal string ConvertXamlToRtf(string xamlContent)
        {
            // Check the parameter validation
            if (xamlContent == null)
            {
                throw new ArgumentNullException("xamlContent");
            }

            string rtfContent = string.Empty;

            if (xamlContent != string.Empty)
            {
                // Creating the converter that process the content data from Xaml to Rtf
                XamlToRtfWriter xamlToRtfWriter = new XamlToRtfWriter(xamlContent);

                // Set WpfPayload package that contained the image for the specified Xaml
                if (WpfPayload != null)
                {
                    xamlToRtfWriter.WpfPayload = WpfPayload;
                }

                // Process the converting from xaml to rtf
                xamlToRtfWriter.Process();

                // Set rtf content that representing resulting from Xaml to Rtf converting.
                rtfContent = xamlToRtfWriter.Output;
            }

            return rtfContent;
        }

        /// <summary>
        /// Converts an rtf content to xaml content.
        /// </summary>
        /// <param name="rtfContent">
        /// The source rtf content that to be converted into xaml content.
        /// </param>
        /// <returns>
        /// Well-formed xml representing XAML equivalent content for the input rtf content string.
        /// </returns>
        internal string ConvertRtfToXaml(string rtfContent)
        {
            // Check the parameter validation
            if (rtfContent == null)
            {
                throw new ArgumentNullException("rtfContent");
            }

            // xaml content to be converted from rtf
            string xamlContent = string.Empty;

            if (rtfContent != string.Empty)
            {
                // Create RtfToXamlReader instance for converting the content 
                // from rtf to xaml and set ForceParagraph
                RtfToXamlReader rtfToXamlReader = new RtfToXamlReader(rtfContent);
                rtfToXamlReader.ForceParagraph = ForceParagraph;

                // Set WpfPayload package that contained the image for the specified Xaml
                if (WpfPayload != null)
                {
                    rtfToXamlReader.WpfPayload = WpfPayload;
                }

                //Process the converting from rtf to xaml
                rtfToXamlReader.Process();

                // Set Xaml content string that representing resulting Rtf-Xaml converting
                xamlContent = rtfToXamlReader.Output;
            }

            return xamlContent;
        }

        #endregion Internal Methods

        // ---------------------------------------------------------------------
        //
        // Internal Properties
        //
        // ---------------------------------------------------------------------

        #region Internal Properties

        // ForceParagraph property indicates whether ForcePagraph for RtfToXamlReader.
        internal bool ForceParagraph
        {
            get
            {
                return _forceParagraph;
            }
            set
            {
                _forceParagraph = value;
            }
        }

        // WpfPayload package property for getting or placing image data for Xaml content
        internal WpfPayload WpfPayload
        {
            get
            {
                return _wpfPayload;
            }
            set
            {
                _wpfPayload = value;
            }
        }

        #endregion Internal Properties

        // ---------------------------------------------------------------------
        //
        // Internal Fields
        //
        // ---------------------------------------------------------------------

        #region Internal Fields

        // Rtf encoding codepage that is 1252 ANSI
        internal const int RtfCodePage = 1252;

        #endregion Internal Fields

        // ---------------------------------------------------------------------
        //
        // Private Fields
        //
        // ---------------------------------------------------------------------

        #region Private Fields

        // Flag that indicate the forcing paragragh for RtfToXamlReader
        private bool _forceParagraph;

        // The output WpfPayload package for placing image data into it
        private WpfPayload _wpfPayload;

        #endregion Private Fields
    }
}
